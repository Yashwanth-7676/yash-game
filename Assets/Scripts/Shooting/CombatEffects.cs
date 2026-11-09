using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YashGame.Shooting
{
    /// <summary>
    /// Centralized combat visual effects, screen shake, tracers, and impact pooling.
    /// Keeps effects lightweight and optimized for mobile devices.
    /// </summary>
    public class CombatEffects : MonoBehaviour
    {
        public static CombatEffects Instance { get; private set; }

        #region Serialized Settings

        [Header("Object Pooling")]
        [SerializeField] private int _tracerPoolSize = 16;
        [SerializeField] private int _impactPoolSize = 20;
        [SerializeField] private int _explosionPoolSize = 4;

        #endregion

        #region Private Fields

        private Queue<LineRenderer> _tracerPool = new Queue<LineRenderer>();
        private Queue<GameObject> _impactPool = new Queue<GameObject>();
        private Queue<GameObject> _explosionPool = new Queue<GameObject>();

        private Transform _cameraTransform;
        private Vector3 _originalCamLocalPos;
        private Coroutine _shakeCoroutine;

        private AudioSource _audioSource;
        private AudioClip _explosionClip;
        private AudioClip _hitMarkerClip;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f;

            _explosionClip = Resources.Load<AudioClip>("Audio/Shooting/Explosion");
            _hitMarkerClip = Resources.Load<AudioClip>("Audio/Shooting/HitMarker");

            if (Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }

            InitializePools();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Object Pools Initialization

        private void InitializePools()
        {
            // Tracers
            for (int i = 0; i < _tracerPoolSize; i++)
            {
                GameObject obj = new GameObject($"Pooled_Tracer_{i}");
                obj.transform.SetParent(transform);
                LineRenderer lr = obj.AddComponent<LineRenderer>();
                lr.startWidth = 0.045f;
                lr.endWidth = 0.015f;
                lr.material = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
                lr.material.color = new Color(1f, 0.9f, 0.5f, 0.9f);
                lr.enabled = false;
                _tracerPool.Enqueue(lr);
            }

            // Impacts (Sparks / Dust quads)
            for (int i = 0; i < _impactPoolSize; i++)
            {
                GameObject imp = new GameObject($"Pooled_Impact_{i}");
                imp.transform.SetParent(transform);

                GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.transform.SetParent(imp.transform, false);
                quad.transform.localScale = Vector3.one * 0.35f;
                Material m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Sprites/Default"));
                m.color = new Color(1f, 0.85f, 0.4f);
                quad.GetComponent<Renderer>().material = m;
                Destroy(quad.GetComponent<Collider>());

                imp.SetActive(false);
                _impactPool.Enqueue(imp);
            }

            // Explosions (Lightweight shockwave + fireball sphere)
            for (int i = 0; i < _explosionPoolSize; i++)
            {
                GameObject exp = new GameObject($"Pooled_Explosion_{i}");
                exp.transform.SetParent(transform);

                // Fireball sphere
                GameObject ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ball.name = "Fireball";
                ball.transform.SetParent(exp.transform, false);
                ball.transform.localScale = Vector3.one * 2.8f;
                Material m = new Material(Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Standard"));
                m.color = new Color(1f, 0.45f, 0.1f);
                ball.GetComponent<Renderer>().material = m;
                Destroy(ball.GetComponent<Collider>());

                // Flash Point Light
                Light l = exp.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.5f, 0.2f);
                l.range = 14f;
                l.intensity = 4.5f;

                exp.SetActive(false);
                _explosionPool.Enqueue(exp);
            }
        }

        #endregion

        #region Public Effect Triggers

        /// <summary>
        /// Spawns a high-speed bullet tracer line.
        /// </summary>
        public void SpawnBulletTracer(Vector3 start, Vector3 end, Color color)
        {
            if (_tracerPool.Count == 0) return;

            LineRenderer lr = _tracerPool.Dequeue();
            _tracerPool.Enqueue(lr);

            lr.material.color = color;
            StartCoroutine(TracerRoutine(lr, start, end));
        }

        private IEnumerator TracerRoutine(LineRenderer lr, Vector3 start, Vector3 end)
        {
            lr.enabled = true;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);

            yield return new WaitForSeconds(0.045f);
            lr.enabled = false;
        }

        /// <summary>
        /// Spawns an impact spark at hit location.
        /// </summary>
        public void SpawnImpact(Vector3 pos, Vector3 normal, bool isEnemy)
        {
            if (_impactPool.Count == 0) return;

            GameObject imp = _impactPool.Dequeue();
            _impactPool.Enqueue(imp);

            imp.transform.position = pos + normal * 0.02f;
            imp.transform.rotation = Quaternion.LookRotation(normal);

            Renderer r = imp.GetComponentInChildren<Renderer>();
            if (r != null)
            {
                r.material.color = isEnemy ? new Color(1f, 0.18f, 0.25f) : new Color(1f, 0.85f, 0.35f);
            }

            imp.SetActive(true);
            StartCoroutine(DisableAfterDelay(imp, 0.15f));
        }

        /// <summary>
        /// Spawns a lightweight explosion sphere and point light shockwave.
        /// </summary>
        public void SpawnExplosion(Vector3 position, float radius = 6f, float damage = 40f)
        {
            if (_explosionPool.Count == 0) return;

            GameObject exp = _explosionPool.Dequeue();
            _explosionPool.Enqueue(exp);

            exp.transform.position = position;
            exp.SetActive(true);

            if (_explosionClip != null && _audioSource != null)
            {
                _audioSource.pitch = Random.Range(0.92f, 1.08f);
                _audioSource.PlayOneShot(_explosionClip, 0.95f);
            }

            TriggerScreenShake(0.6f, 0.35f);

            // Area of effect damage to nearby enemies and player
            Collider[] hits = Physics.OverlapSphere(position, radius);
            foreach (var hit in hits)
            {
                PlayerHealth ph = hit.GetComponentInParent<PlayerHealth>();
                if (ph != null)
                {
                    float dist = Vector3.Distance(position, ph.transform.position);
                    float dmg = Mathf.Lerp(damage, damage * 0.25f, dist / radius);
                    ph.TakeDamage(dmg);
                }

                EnemyHealth eh = hit.GetComponentInParent<EnemyHealth>();
                if (eh != null && eh.IsAlive)
                {
                    float dist = Vector3.Distance(position, eh.transform.position);
                    float dmg = Mathf.Lerp(damage * 1.5f, damage * 0.4f, dist / radius);
                    eh.TakeDamage(dmg, hit.transform.position, (hit.transform.position - position).normalized);
                }
            }

            StartCoroutine(AnimateExplosion(exp));
        }

        private IEnumerator AnimateExplosion(GameObject exp)
        {
            Transform ball = exp.transform.Find("Fireball");
            Light l = exp.GetComponent<Light>();

            float elapsed = 0f;
            float duration = 0.35f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (ball != null)
                {
                    float scale = Mathf.Lerp(0.8f, 3.8f, Mathf.Sin(t * Mathf.PI * 0.5f));
                    ball.localScale = Vector3.one * scale;
                }

                if (l != null)
                {
                    l.intensity = Mathf.Lerp(4.5f, 0f, t);
                }

                yield return null;
            }

            exp.SetActive(false);
        }

        /// <summary>
        /// Triggers smooth camera screen shake on weapon fire, damage, or explosions.
        /// </summary>
        public void TriggerScreenShake(float intensity, float duration)
        {
            if (_cameraTransform == null && Camera.main != null)
            {
                _cameraTransform = Camera.main.transform;
            }

            if (_cameraTransform == null) return;

            if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
            _shakeCoroutine = StartCoroutine(ScreenShakeRoutine(intensity, duration));
        }

        private IEnumerator ScreenShakeRoutine(float intensity, float duration)
        {
            float elapsed = 0f;
            Vector3 lastOffset = Vector3.zero;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float damp = 1f - Mathf.Clamp01(elapsed / duration);

                // Remove previous frame offset
                _cameraTransform.localPosition -= lastOffset;

                // Random 3D offset
                lastOffset = new Vector3(
                    (Random.value * 2f - 1f) * intensity * damp * 0.12f,
                    (Random.value * 2f - 1f) * intensity * damp * 0.12f,
                    0f
                );

                _cameraTransform.localPosition += lastOffset;
                yield return null;
            }

            _cameraTransform.localPosition -= lastOffset;
            _shakeCoroutine = null;
        }

        public void PlayHitMarkerChime()
        {
            if (_hitMarkerClip != null && _audioSource != null)
            {
                _audioSource.pitch = 1.0f;
                _audioSource.PlayOneShot(_hitMarkerClip, 0.75f);
            }
        }

        private IEnumerator DisableAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null) obj.SetActive(false);
        }

        #endregion
    }
}

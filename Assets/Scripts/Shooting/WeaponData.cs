using UnityEngine;

namespace YashGame.Shooting
{
    public enum WeaponType
    {
        AssaultRifle,
        Pistol,
        Shotgun,
        SniperRifle
    }

    /// <summary>
    /// ScriptableObject defining specifications, ballistics, audio, and visual attributes for a weapon.
    /// Supports both ScriptableObject asset instances and runtime factory generation.
    /// </summary>
    [CreateAssetMenu(fileName = "NewWeaponData", menuName = "Yash Game/Weapon Data", order = 1)]
    public class WeaponData : ScriptableObject
    {
        #region General & Identifiers

        [Header("General")]
        public string weaponName = "Assault Rifle";
        public WeaponType weaponType = WeaponType.AssaultRifle;
        public string abbreviation = "AR";
        public Color themeColor = new Color(0f, 0.85f, 1f); // Cyan

        #endregion

        #region Ballistics & Fire Rate

        [Header("Ballistics")]
        public float damage = 25f;
        public float fireRateRPM = 600f; // Rounds per minute
        public float range = 150f;
        public int pelletCount = 1; // 1 for bullet, 8 for shotgun
        public bool isAutomatic = true;

        [Header("Accuracy & Recoil")]
        public float spreadAngle = 0.02f;
        public float aimSpreadAngle = 0.005f;
        public float runningSpreadAngle = 0.045f;
        public float cameraRecoilKick = 0.8f;
        public float screenShakeIntensity = 0.25f;

        #endregion

        #region Ammunition & Reloading

        [Header("Ammunition")]
        public int magazineSize = 30;
        public int maxReserveAmmo = 120;
        public float reloadDuration = 1.8f;

        #endregion

        #region Aim Down Sights (ADS)

        [Header("Aim Down Sights")]
        public float aimZoomFOV = 40f; // 60 default, 40 AR/Shotgun, 45 Pistol, 20 Sniper
        public Vector3 aimCameraOffset = new Vector3(0.40f, 0.1f, -1.6f);

        #endregion

        #region Audio & Resource Paths

        [Header("Audio")]
        public string gunshotAudioResource = "Audio/Shooting/Gunshot_Rifle";
        public string reloadAudioResource = "Audio/Shooting/Weapon_Reload";

        #endregion

        #region Factory Presets

        /// <summary>Creates a balanced Assault Rifle (M4A1).</summary>
        public static WeaponData CreateAssaultRifle()
        {
            var data = CreateInstance<WeaponData>();
            data.weaponName = "M4A1 TACTICAL RIFLE";
            data.weaponType = WeaponType.AssaultRifle;
            data.abbreviation = "M4A1";
            data.themeColor = new Color(0f, 0.85f, 1f); // Cyan
            data.damage = 25f;
            data.fireRateRPM = 600f;
            data.range = 150f;
            data.pelletCount = 1;
            data.isAutomatic = true;
            data.spreadAngle = 0.02f;
            data.aimSpreadAngle = 0.005f;
            data.runningSpreadAngle = 0.045f;
            data.cameraRecoilKick = 0.8f;
            data.screenShakeIntensity = 0.22f;
            data.magazineSize = 30;
            data.maxReserveAmmo = 120;
            data.reloadDuration = 1.8f;
            data.aimZoomFOV = 40f;
            data.aimCameraOffset = new Vector3(0.40f, 0.1f, -1.6f);
            data.gunshotAudioResource = "Audio/Shooting/Gunshot_Rifle";
            data.reloadAudioResource = "Audio/Shooting/Weapon_Reload";
            return data;
        }

        /// <summary>Creates an accurate, lightweight Combat Pistol (9mm).</summary>
        public static WeaponData CreatePistol()
        {
            var data = CreateInstance<WeaponData>();
            data.weaponName = "APEX 9MM COMBAT PISTOL";
            data.weaponType = WeaponType.Pistol;
            data.abbreviation = "9MM";
            data.themeColor = new Color(0.4f, 1f, 0.4f); // Neon Green
            data.damage = 18f;
            data.fireRateRPM = 340f;
            data.range = 80f;
            data.pelletCount = 1;
            data.isAutomatic = false; // Semi-auto tap
            data.spreadAngle = 0.012f;
            data.aimSpreadAngle = 0.003f;
            data.runningSpreadAngle = 0.03f;
            data.cameraRecoilKick = 0.5f;
            data.screenShakeIntensity = 0.14f;
            data.magazineSize = 15;
            data.maxReserveAmmo = 60;
            data.reloadDuration = 1.2f;
            data.aimZoomFOV = 48f;
            data.aimCameraOffset = new Vector3(0.35f, 0.1f, -1.8f);
            data.gunshotAudioResource = "Audio/Shooting/Gunshot_Pistol";
            data.reloadAudioResource = "Audio/Shooting/Weapon_Reload";
            return data;
        }

        /// <summary>Creates a devastating close-quarters Shotgun (12-Gauge).</summary>
        public static WeaponData CreateShotgun()
        {
            var data = CreateInstance<WeaponData>();
            data.weaponName = "STRIKER 12-GAUGE SHOTGUN";
            data.weaponType = WeaponType.Shotgun;
            data.abbreviation = "SG-12";
            data.themeColor = new Color(1f, 0.55f, 0f); // Orange
            data.damage = 14f; // 14 x 8 pellets = 112 maximum close damage
            data.fireRateRPM = 90f; // 0.66s interval
            data.range = 45f;
            data.pelletCount = 8;
            data.isAutomatic = false;
            data.spreadAngle = 0.065f;
            data.aimSpreadAngle = 0.035f;
            data.runningSpreadAngle = 0.09f;
            data.cameraRecoilKick = 2.4f;
            data.screenShakeIntensity = 0.55f;
            data.magazineSize = 8;
            data.maxReserveAmmo = 32;
            data.reloadDuration = 2.2f;
            data.aimZoomFOV = 45f;
            data.aimCameraOffset = new Vector3(0.42f, 0.1f, -1.7f);
            data.gunshotAudioResource = "Audio/Shooting/Gunshot_Shotgun";
            data.reloadAudioResource = "Audio/Shooting/Weapon_Reload";
            return data;
        }

        /// <summary>Creates a high-caliber, long-range Sniper Rifle (.50 Cal).</summary>
        public static WeaponData CreateSniperRifle()
        {
            var data = CreateInstance<WeaponData>();
            data.weaponName = "VANGUARD .50 CAL SNIPER";
            data.weaponType = WeaponType.SniperRifle;
            data.abbreviation = "V-50";
            data.themeColor = new Color(1f, 0.2f, 0.35f); // Crimson
            data.damage = 110f; // One-shot kill on standard 100 HP enemies!
            data.fireRateRPM = 45f; // 1.33s interval
            data.range = 300f;
            data.pelletCount = 1;
            data.isAutomatic = false;
            data.spreadAngle = 0.025f;
            data.aimSpreadAngle = 0.0005f; // Pinpoint accurate when ADS
            data.runningSpreadAngle = 0.12f;
            data.cameraRecoilKick = 3.2f;
            data.screenShakeIntensity = 0.75f;
            data.magazineSize = 5;
            data.maxReserveAmmo = 20;
            data.reloadDuration = 2.6f;
            data.aimZoomFOV = 20f; // Extreme high-magnification scope zoom!
            data.aimCameraOffset = new Vector3(0.25f, 0.05f, -1.0f);
            data.gunshotAudioResource = "Audio/Shooting/Gunshot_Sniper";
            data.reloadAudioResource = "Audio/Shooting/Weapon_Reload";
            return data;
        }

        #endregion
    }
}

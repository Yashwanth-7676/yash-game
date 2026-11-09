using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YashGame.Shared;

namespace YashGame.Shooting
{
    /// <summary>
    /// Controls the 3D Map Selection screen.
    /// Manages browsing the 4 battlefield maps:
    /// 1. Desert Warfare
    /// 2. Modern City
    /// 3. Military Base
    /// 4. Forest Survival
    /// Features difficulty selector (Easy, Normal, Hard), unlock state verification,
    /// star progression readouts, personal best records, and async deployment.
    /// </summary>
    public class MapSelectionController : MonoBehaviour
    {
        public static MapSelectionController Instance { get; private set; }

        #region Serialized UI References

        [Header("Card Visuals")]
        [SerializeField] private TMP_Text _mapNameText;
        [SerializeField] private TMP_Text _mapSubtitleText;
        [SerializeField] private TMP_Text _mapDescriptionText;
        [SerializeField] private TMP_Text _tacticalIntelText;
        [SerializeField] private Image _mapBannerImage;
        [SerializeField] private Outline _cardOutline;

        [Header("Status & Stars")]
        [SerializeField] private TMP_Text _statusBadgeText;
        [SerializeField] private TMP_Text _starsText;
        [SerializeField] private TMP_Text _bestTimeText;
        [SerializeField] private TMP_Text _enemyCountText;

        [Header("Difficulty Buttons")]
        [SerializeField] private Button _btnEasy;
        [SerializeField] private Button _btnNormal;
        [SerializeField] private Button _btnHard;
        [SerializeField] private Image[] _diffButtonBgs;
        [SerializeField] private Outline[] _diffButtonOutlines;

        [Header("Navigation & Deploy")]
        [SerializeField] private Button _prevMapButton;
        [SerializeField] private Button _nextMapButton;
        [SerializeField] private Button _deployButton;
        [SerializeField] private TMP_Text _deployButtonText;
        [SerializeField] private Button _backButton;
        [SerializeField] private TMP_Text _pageDotsText;

        #endregion

        #region Private Fields

        private readonly List<MapData> _maps = new List<MapData>();
        private int _currentMapIndex = 0;
        private DifficultyLevel _selectedDifficulty = DifficultyLevel.Normal;

        private MapUnlockManager _unlockManager;
        private MissionProgressManager _progressManager;
        private DifficultyManager _difficultyManager;

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

            EnsureManagers();
            LoadMapConfigs();
            EnsureUIHierarchy();
        }

        private void Start()
        {
            if (_difficultyManager != null)
            {
                _selectedDifficulty = _difficultyManager.CurrentDifficulty;
            }

            // Restore last selected map index if available
            string savedMapId = PlayerPrefs.GetString("YashGame_SelectedMap", "desert");
            for (int i = 0; i < _maps.Count; i++)
            {
                if (_maps[i].id == savedMapId)
                {
                    _currentMapIndex = i;
                    break;
                }
            }

            UpdateDisplay();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        #endregion

        #region Manager Initialization

        private void EnsureManagers()
        {
            _unlockManager = MapUnlockManager.Instance ?? FindObjectOfType<MapUnlockManager>();
            if (_unlockManager == null)
            {
                GameObject obj = new GameObject("MapUnlockManager");
                _unlockManager = obj.AddComponent<MapUnlockManager>();
            }

            _progressManager = MissionProgressManager.Instance ?? FindObjectOfType<MissionProgressManager>();
            if (_progressManager == null)
            {
                GameObject obj = new GameObject("MissionProgressManager");
                _progressManager = obj.AddComponent<MissionProgressManager>();
            }

            _difficultyManager = DifficultyManager.Instance ?? FindObjectOfType<DifficultyManager>();
            if (_difficultyManager == null)
            {
                GameObject obj = new GameObject("DifficultyManager");
                _difficultyManager = obj.AddComponent<DifficultyManager>();
            }
        }

        private void LoadMapConfigs()
        {
            _maps.Clear();
            _maps.Add(MapData.CreateDesertWarfare());    // 0: Desert Warfare
            _maps.Add(MapData.CreateModernCity());       // 1: Modern City
            _maps.Add(MapData.CreateMilitaryBase());     // 2: Military Base
            _maps.Add(MapData.CreateForestSurvival());   // 3: Forest Survival
        }

        #endregion

        #region UI Updates & Presentation

        public void UpdateDisplay()
        {
            if (_maps == null || _maps.Count == 0) return;

            MapData map = _maps[_currentMapIndex];
            bool isUnlocked = _unlockManager != null && _unlockManager.IsMapUnlocked(map.id);
            bool isCleared = _unlockManager != null && _unlockManager.IsMapCleared(map.id);
            int stars = _progressManager != null ? _progressManager.GetMapStars(map.id) : 0;
            float bestTime = _progressManager != null ? _progressManager.GetMapBestTime(map.id) : 0f;

            // Map Info
            if (_mapNameText != null) _mapNameText.text = map.displayName;
            if (_mapSubtitleText != null)
            {
                _mapSubtitleText.text = map.subtitle;
                _mapSubtitleText.color = map.themeAccentColor;
            }

            if (_mapDescriptionText != null) _mapDescriptionText.text = map.description;
            if (_tacticalIntelText != null) _tacticalIntelText.text = $"⚡ INTEL: {map.tacticalIntel}";

            if (_mapBannerImage != null) _mapBannerImage.color = map.themeAccentColor * 0.45f;
            if (_cardOutline != null) _cardOutline.effectColor = isUnlocked ? map.themeAccentColor : new Color(0.4f, 0.4f, 0.4f, 0.6f);

            // Status Badge
            if (_statusBadgeText != null)
            {
                if (isCleared)
                {
                    _statusBadgeText.text = "CLEARED ✔";
                    _statusBadgeText.color = new Color(0.2f, 0.95f, 0.45f);
                }
                else if (isUnlocked)
                {
                    _statusBadgeText.text = "UNLOCKED";
                    _statusBadgeText.color = map.themeAccentColor;
                }
                else
                {
                    string reqMap = (_currentMapIndex > 0) ? _maps[_currentMapIndex - 1].displayName : "PREVIOUS MAP";
                    _statusBadgeText.text = $"LOCKED (CLEAR {reqMap})";
                    _statusBadgeText.color = new Color(1.0f, 0.3f, 0.3f);
                }
            }

            // Stars Display
            if (_starsText != null)
            {
                string starDisplay = "";
                for (int s = 1; s <= 3; s++)
                {
                    starDisplay += (s <= stars) ? "<color=#FFD700>★</color> " : "<color=#404855>☆</color> ";
                }
                _starsText.text = starDisplay.TrimEnd();
            }

            // Best Time
            if (_bestTimeText != null)
            {
                if (bestTime > 0.01f)
                {
                    int m = Mathf.FloorToInt(bestTime / 60f);
                    int s = Mathf.FloorToInt(bestTime % 60f);
                    _bestTimeText.text = $"BEST: <color=#00D9FF>{m:00}:{s:00}</color>";
                }
                else
                {
                    _bestTimeText.text = "BEST: <color=#8090A0>--:--</color>";
                }
            }

            // Enemy Count
            if (_enemyCountText != null)
            {
                float multiplier = _difficultyManager != null ? _difficultyManager.EnemyCountMultiplier : 1f;
                int enemyCount = Mathf.RoundToInt(map.baseEnemyCount * multiplier);
                _enemyCountText.text = $"THREAT: <color=#FF2E59>{enemyCount} HOSTILES</color>";
            }

            // Deploy Button
            if (_deployButton != null && _deployButtonText != null)
            {
                _deployButton.interactable = isUnlocked;
                if (isUnlocked)
                {
                    _deployButtonText.text = "▶ DEPLOY TO BATTLEFIELD";
                    _deployButton.image.color = new Color(0.0f, 0.42f, 0.70f);
                }
                else
                {
                    _deployButtonText.text = "🔒 MAP LOCKED";
                    _deployButton.image.color = new Color(0.18f, 0.20f, 0.24f);
                }
            }

            // Page Dots
            if (_pageDotsText != null)
            {
                string dots = "";
                for (int i = 0; i < _maps.Count; i++)
                {
                    dots += (i == _currentMapIndex) ? "<color=#00D9FF>●</color> " : "<color=#404855>○</color> ";
                }
                _pageDotsText.text = dots.TrimEnd();
            }

            UpdateDifficultyButtonsUI();
        }

        private void UpdateDifficultyButtonsUI()
        {
            if (_diffButtonBgs == null) return;

            Color[] activeGlows = new Color[]
            {
                new Color(0.2f, 0.85f, 0.4f),  // Easy: Green
                new Color(0.0f, 0.85f, 1.0f),  // Normal: Cyan
                new Color(1.0f, 0.25f, 0.35f)  // Hard: Red
            };

            for (int i = 0; i < 3; i++)
            {
                bool isSelected = (int)_selectedDifficulty == i;
                if (_diffButtonBgs[i] != null)
                {
                    _diffButtonBgs[i].color = isSelected ? activeGlows[i] * 0.4f : new Color(0.08f, 0.11f, 0.16f, 0.85f);
                }

                if (_diffButtonOutlines != null && _diffButtonOutlines.Length > i && _diffButtonOutlines[i] != null)
                {
                    _diffButtonOutlines[i].effectColor = isSelected ? activeGlows[i] : new Color(0.25f, 0.32f, 0.42f, 0.5f);
                }
            }
        }

        #endregion

        #region User Actions

        public void NextMap()
        {
            _currentMapIndex = (_currentMapIndex + 1) % _maps.Count;
            UpdateDisplay();
        }

        public void PreviousMap()
        {
            _currentMapIndex = (_currentMapIndex - 1 + _maps.Count) % _maps.Count;
            UpdateDisplay();
        }

        public void SelectDifficulty(int levelInt)
        {
            _selectedDifficulty = (DifficultyLevel)Mathf.Clamp(levelInt, 0, 2);
            if (_difficultyManager != null)
            {
                _difficultyManager.SetDifficulty(_selectedDifficulty);
            }
            UpdateDisplay();
        }

        public void OnDeployClicked()
        {
            MapData map = _maps[_currentMapIndex];
            if (_unlockManager != null && !_unlockManager.IsMapUnlocked(map.id))
            {
                Debug.LogWarning($"[MapSelectionController] Map {map.id} is still locked.");
                return;
            }

            // Save player's chosen map
            PlayerPrefs.SetString("YashGame_SelectedMap", map.id);
            PlayerPrefs.Save();

            Debug.Log($"[MapSelectionController] Deploying to '{map.displayName}' on difficulty '{_selectedDifficulty}'...");

            // Use LoadingScreenManager if present for smooth async transition
            if (LoadingScreenManager.Instance != null)
            {
                LoadingScreenManager.Instance.LoadBattlefieldAsync(SceneLoader.SHOOTING, map);
            }
            else if (SceneLoader.HasInstance)
            {
                SceneLoader.Instance.LoadScene(SceneLoader.SHOOTING);
            }
            else
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(SceneLoader.SHOOTING);
            }
        }

        public void OnBackToMainMenuClicked()
        {
            // Close selection modal
            MainMenuController mmc = FindObjectOfType<MainMenuController>();
            if (mmc != null)
            {
                mmc.CloseShootingSelection();
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        #endregion

        #region Procedural UI Hierarchy Assembly

        private void EnsureUIHierarchy()
        {
            if (transform.Find("MapSelectionRoot") != null) return;

            GameObject root = new GameObject("MapSelectionRoot", typeof(RectTransform));
            root.transform.SetParent(transform, false);
            RectTransform rRt = root.GetComponent<RectTransform>();
            rRt.anchorMin = Vector2.zero; rRt.anchorMax = Vector2.one; rRt.sizeDelta = Vector2.zero;

            // Background Overlay
            GameObject bgObj = new GameObject("BgOverlay", typeof(RectTransform), typeof(Image));
            bgObj.transform.SetParent(root.transform, false);
            RectTransform bgRt = bgObj.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one; bgRt.sizeDelta = Vector2.zero;
            bgObj.GetComponent<Image>().color = new Color(0.02f, 0.04f, 0.08f, 0.96f);

            // Header Title: — BATTLEFIELD OPERATIONS —
            CreateText("Header", root.transform, "— BATTLEFIELD OPERATIONS —", new Vector2(0f, 840f), new Vector2(900f, 60f), 38, TextAlignmentOptions.Center, Color.white);

            // Center Card Container
            GameObject cardObj = new GameObject("MapCard", typeof(RectTransform), typeof(Image), typeof(Outline));
            cardObj.transform.SetParent(root.transform, false);
            RectTransform cRt = cardObj.GetComponent<RectTransform>();
            cRt.anchoredPosition = new Vector2(0f, 130f);
            cRt.sizeDelta = new Vector2(880f, 1020f);
            cardObj.GetComponent<Image>().color = new Color(0.05f, 0.07f, 0.12f, 0.95f);
            _cardOutline = cardObj.GetComponent<Outline>();
            _cardOutline.effectDistance = new Vector2(3f, -3f);

            // Banner Visual Block
            GameObject bannerObj = new GameObject("Banner", typeof(RectTransform), typeof(Image), typeof(Outline));
            bannerObj.transform.SetParent(cardObj.transform, false);
            RectTransform bRt = bannerObj.GetComponent<RectTransform>();
            bRt.anchoredPosition = new Vector2(0f, 320f);
            bRt.sizeDelta = new Vector2(820f, 300f);
            _mapBannerImage = bannerObj.GetComponent<Image>();
            bannerObj.GetComponent<Outline>().effectColor = new Color(0f, 0f, 0f, 0.6f);

            // Status Badge inside Banner
            _statusBadgeText = CreateText("StatusBadge", bannerObj.transform, "UNLOCKED", new Vector2(0f, 105f), new Vector2(760f, 40f), 24, TextAlignmentOptions.Right, Color.white);

            // Stars inside Banner
            _starsText = CreateText("Stars", bannerObj.transform, "★ ★ ☆", new Vector2(0f, -100f), new Vector2(760f, 40f), 28, TextAlignmentOptions.Right, Color.white);

            // Map Name & Subtitle
            _mapNameText = CreateText("MapName", cardObj.transform, "DESERT WARFARE", new Vector2(0f, 120f), new Vector2(820f, 50f), 36, TextAlignmentOptions.Center, Color.white);
            _mapSubtitleText = CreateText("MapSubtitle", cardObj.transform, "OPERATION: SCORCHED SANDS", new Vector2(0f, 75f), new Vector2(820f, 35f), 22, TextAlignmentOptions.Center, Color.cyan);

            // Threat & Best Time Row
            GameObject statsRow = new GameObject("StatsRow", typeof(RectTransform));
            statsRow.transform.SetParent(cardObj.transform, false);
            RectTransform srRt = statsRow.GetComponent<RectTransform>();
            srRt.anchoredPosition = new Vector2(0f, 25f);
            srRt.sizeDelta = new Vector2(820f, 40f);

            _enemyCountText = CreateText("Threat", statsRow.transform, "THREAT: 6 HOSTILES", new Vector2(-200f, 0f), new Vector2(380f, 35f), 20, TextAlignmentOptions.Left, Color.white);
            _bestTimeText = CreateText("BestTime", statsRow.transform, "BEST: 01:14", new Vector2(200f, 0f), new Vector2(380f, 35f), 20, TextAlignmentOptions.Right, Color.white);

            // Description Box
            _mapDescriptionText = CreateText("Description", cardObj.transform, "Large desert combat zone...", new Vector2(0f, -65f), new Vector2(820f, 110f), 20, TextAlignmentOptions.Center, new Color(0.85f, 0.88f, 0.95f));

            // Tactical Intel Box
            _tacticalIntelText = CreateText("TacticalIntel", cardObj.transform, "⚡ INTEL: Long sightlines favor Sniper Rifle...", new Vector2(0f, -155f), new Vector2(820f, 65f), 19, TextAlignmentOptions.Center, new Color(1.0f, 0.85f, 0.35f));

            // Difficulty Selection Row
            GameObject diffBox = new GameObject("DifficultySelector", typeof(RectTransform));
            diffBox.transform.SetParent(cardObj.transform, false);
            RectTransform dRt = diffBox.GetComponent<RectTransform>();
            dRt.anchoredPosition = new Vector2(0f, -240f);
            dRt.sizeDelta = new Vector2(820f, 60f);

            _diffButtonBgs = new Image[3];
            _diffButtonOutlines = new Outline[3];

            _btnEasy = CreateButton("Btn_Easy", diffBox.transform, new Vector2(-260f, 0f), new Vector2(250f, 52f), "RECRUIT", 20, () => SelectDifficulty(0), out _diffButtonBgs[0], out _diffButtonOutlines[0]);
            _btnNormal = CreateButton("Btn_Normal", diffBox.transform, new Vector2(0f, 0f), new Vector2(250f, 52f), "OPERATIVE", 20, () => SelectDifficulty(1), out _diffButtonBgs[1], out _diffButtonOutlines[1]);
            _btnHard = CreateButton("Btn_Hard", diffBox.transform, new Vector2(260f, 0f), new Vector2(250f, 52f), "VETERAN", 20, () => SelectDifficulty(2), out _diffButtonBgs[2], out _diffButtonOutlines[2]);

            // Deploy Action Button
            Image depBg; Outline depOutl;
            _deployButton = CreateButton("DeployButton", cardObj.transform, new Vector2(0f, -340f), new Vector2(800f, 75f), "▶ DEPLOY TO BATTLEFIELD", 28, OnDeployClicked, out depBg, out depOutl);
            _deployButtonText = _deployButton.GetComponentInChildren<TMP_Text>();

            // Navigation Arrows (Left/Right)
            Image pBg; Outline pOut;
            _prevMapButton = CreateButton("PrevButton", root.transform, new Vector2(-480f, 130f), new Vector2(70f, 110f), "◀", 36, PreviousMap, out pBg, out pOut);

            Image nBg; Outline nOut;
            _nextMapButton = CreateButton("NextButton", root.transform, new Vector2(480f, 130f), new Vector2(70f, 110f), "▶", 36, NextMap, out nBg, out nOut);

            // Page Dots
            _pageDotsText = CreateText("PageDots", root.transform, "● ○ ○ ○", new Vector2(0f, -440f), new Vector2(400f, 40f), 26, TextAlignmentOptions.Center, Color.white);

            // Return / Back to Hub Button
            Image bBg; Outline bOut;
            _backButton = CreateButton("BackButton", root.transform, new Vector2(0f, -510f), new Vector2(420f, 65f), "⟵ RETURN TO MENU", 24, OnBackToMainMenuClicked, out bBg, out bOut);
        }

        private TMP_Text CreateText(string name, Transform parent, string text, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align, Color color)
        {
            GameObject txtObj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            txtObj.transform.SetParent(parent, false);
            RectTransform rt = txtObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;

            TMP_Text tmp = txtObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        private Button CreateButton(string name, Transform parent, Vector2 pos, Vector2 size, string text, float fontSize, UnityEngine.Events.UnityAction onClick, out Image bgOut, out Outline outlineOut)
        {
            GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
            btnObj.transform.SetParent(parent, false);
            RectTransform rt = btnObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos; rt.sizeDelta = size;

            bgOut = btnObj.GetComponent<Image>();
            bgOut.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);

            outlineOut = btnObj.GetComponent<Outline>();
            outlineOut.effectColor = new Color(0.0f, 0.85f, 1.0f);
            outlineOut.effectDistance = new Vector2(2f, -2f);

            Button btn = btnObj.GetComponent<Button>();
            btn.onClick.AddListener(onClick);

            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(btnObj.transform, false);
            RectTransform trt = textObj.GetComponent<RectTransform>();
            trt.anchoredPosition = Vector2.zero; trt.sizeDelta = size;

            TMP_Text tmp = textObj.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        #endregion
    }
}

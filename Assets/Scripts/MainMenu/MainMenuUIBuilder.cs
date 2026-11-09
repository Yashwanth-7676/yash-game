using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using YashGame.Shared;

namespace YashGame.MainMenu
{
    /// <summary>
    /// Procedurally builds, styles, and links the entire premium mobile Main Menu UI hierarchy
    /// if not already baked in the scene. Ensures 100% immediate playability, responsive portrait
    /// scaling (1080x1920), touch-friendly cards, animations, and modal panels.
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class MainMenuUIBuilder : MonoBehaviour
    {
        #region Palette & Styling Constants

        private static readonly Color ColorDarkBg = new Color(0.024f, 0.035f, 0.082f, 1f); // #060915
        private static readonly Color ColorCardDark = new Color(0.045f, 0.065f, 0.14f, 0.95f);
        private static readonly Color ColorCyanNeon = new Color(0f, 0.85f, 1f, 1f); // #00D9FF
        private static readonly Color ColorCyanDark = new Color(0f, 0.35f, 0.55f, 1f);
        private static readonly Color ColorRedNeon = new Color(1f, 0.18f, 0.35f, 1f); // #FF2E59
        private static readonly Color ColorRedDark = new Color(0.55f, 0.08f, 0.16f, 1f);
        private static readonly Color ColorTextWhite = new Color(0.96f, 0.97f, 1f, 1f);
        private static readonly Color ColorTextMuted = new Color(0.65f, 0.72f, 0.85f, 1f);

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            EnsureEventSystem();
            BuildUIHierarchy();
        }

        #endregion

        #region UI Construction

        public void BuildUIHierarchy()
        {
            Canvas canvas = GetComponentInChildren<Canvas>();
            if (canvas != null && canvas.transform.Find("MainPanel") != null)
            {
                Debug.Log("[MainMenuUIBuilder] UI Canvas already exists. Skipping procedural generation.");
                return;
            }

            Debug.Log("[MainMenuUIBuilder] Procedurally constructing premium mobile Main Menu UI...");

            // 1. Root Canvas
            GameObject canvasObj = new GameObject("MainMenuCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObj.transform.SetParent(transform, false);

            Canvas rootCanvas = canvasObj.GetComponent<Canvas>();
            rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            rootCanvas.sortingOrder = 10;

            CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            // 2. Main Background
            GameObject bgObj = CreateUIElement("Background", canvasObj.transform);
            SetStretchFull(bgObj.GetComponent<RectTransform>());
            Image bgImage = bgObj.AddComponent<Image>();
            bgImage.color = ColorDarkBg;

            // Load Texture Sprites
            Sprite logoSprite = LoadSprite("Textures/YashGame_Logo");
            Sprite shootSprite = LoadSprite("Textures/Shooting_Banner");
            Sprite raceSprite = LoadSprite("Textures/Racing_Banner");
            Sprite avatarSprite = LoadSprite("Textures/Avatar_Default");

            // 3. Main Panel (CanvasGroup)
            GameObject mainPanelObj = CreateUIElement("MainPanel", canvasObj.transform);
            SetStretchFull(mainPanelObj.GetComponent<RectTransform>());
            CanvasGroup mainPanelCg = mainPanelObj.AddComponent<CanvasGroup>();

            // Setup MainMenuController
            MainMenuController controller = gameObject.GetComponent<MainMenuController>();
            if (controller == null)
            {
                controller = gameObject.AddComponent<MainMenuController>();
            }

            // 3.1 Header Bar
            GameObject headerObj = CreateUIElement("HeaderBar", mainPanelObj.transform);
            RectTransform headerRt = headerObj.GetComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -40f);
            headerRt.sizeDelta = new Vector2(-80f, 120f);

            // Profile Button (Pill at top-left)
            GameObject profileBtnObj = CreateButton("ProfileButton", headerObj.transform, ColorCardDark, ColorCyanNeon);
            RectTransform profileRt = profileBtnObj.GetComponent<RectTransform>();
            profileRt.anchorMin = new Vector2(0f, 0.5f);
            profileRt.anchorMax = new Vector2(0f, 0.5f);
            profileRt.pivot = new Vector2(0f, 0.5f);
            profileRt.anchoredPosition = new Vector2(0f, 0f);
            profileRt.sizeDelta = new Vector2(400f, 100f);

            // Avatar inside profile button
            if (avatarSprite != null)
            {
                GameObject avIcon = CreateUIElement("AvatarIcon", profileBtnObj.transform);
                RectTransform avRt = avIcon.GetComponent<RectTransform>();
                avRt.anchorMin = new Vector2(0f, 0.5f);
                avRt.anchorMax = new Vector2(0f, 0.5f);
                avRt.pivot = new Vector2(0f, 0.5f);
                avRt.anchoredPosition = new Vector2(10f, 0f);
                avRt.sizeDelta = new Vector2(80f, 80f);
                Image avImg = avIcon.AddComponent<Image>();
                avImg.sprite = avatarSprite;
            }

            CreateText("ProfileText", profileBtnObj.transform, "COMMANDER YASH\n<size=22><color=#00D9FF>LVL 24 • APEX WARRIOR</color></size>",
                new Vector2(100f, 0f), new Vector2(280f, 80f), 28, TextAlignmentOptions.Left);

            // Sound Toggle (Top-right)
            GameObject soundBtnObj = CreateButton("SoundToggle", headerObj.transform, ColorCardDark, ColorCyanNeon);
            RectTransform soundRt = soundBtnObj.GetComponent<RectTransform>();
            soundRt.anchorMin = new Vector2(1f, 0.5f);
            soundRt.anchorMax = new Vector2(1f, 0.5f);
            soundRt.pivot = new Vector2(1f, 0.5f);
            soundRt.anchoredPosition = new Vector2(0f, 0f);
            soundRt.sizeDelta = new Vector2(100f, 100f);

            TMP_Text soundText = CreateText("SoundIcon", soundBtnObj.transform, "🔊", Vector2.zero, new Vector2(90f, 90f), 44, TextAlignmentOptions.Center);

            // 3.2 Logo & Title Section
            GameObject logoSection = CreateUIElement("LogoSection", mainPanelObj.transform);
            RectTransform logoSecRt = logoSection.GetComponent<RectTransform>();
            logoSecRt.anchorMin = new Vector2(0.5f, 1f);
            logoSecRt.anchorMax = new Vector2(0.5f, 1f);
            logoSecRt.pivot = new Vector2(0.5f, 1f);
            logoSecRt.anchoredPosition = new Vector2(0f, -170f);
            logoSecRt.sizeDelta = new Vector2(920f, 320f);

            if (logoSprite != null)
            {
                GameObject logoImgObj = CreateUIElement("LogoImage", logoSection.transform);
                RectTransform logoRt = logoImgObj.GetComponent<RectTransform>();
                logoRt.anchorMin = new Vector2(0.5f, 0.5f);
                logoRt.anchorMax = new Vector2(0.5f, 0.5f);
                logoRt.pivot = new Vector2(0.5f, 0.5f);
                logoRt.sizeDelta = new Vector2(860f, 300f);
                Image img = logoImgObj.AddComponent<Image>();
                img.sprite = logoSprite;
                img.preserveAspect = true;
            }
            else
            {
                CreateText("TitleText", logoSection.transform, "<color=#00D9FF>YASH</color> <color=#FF2E59>GAME</color>",
                    new Vector2(0f, 40f), new Vector2(800f, 120f), 76, TextAlignmentOptions.Center);
                CreateText("TaglineText", logoSection.transform, "— TWO WORLDS. ONE GAME. —",
                    new Vector2(0f, -50f), new Vector2(800f, 50f), 28, TextAlignmentOptions.Center);
            }

            // 3.3 Cards Container (Shooting & Racing)
            GameObject cardsContainer = CreateUIElement("CardsContainer", mainPanelObj.transform);
            RectTransform cardsRt = cardsContainer.GetComponent<RectTransform>();
            cardsRt.anchorMin = new Vector2(0.5f, 0.5f);
            cardsRt.anchorMax = new Vector2(0.5f, 0.5f);
            cardsRt.pivot = new Vector2(0.5f, 0.5f);
            cardsRt.anchoredPosition = new Vector2(0f, -40f);
            cardsRt.sizeDelta = new Vector2(940f, 950f);

            // Card 1: SHOOTING GAME CARD
            GameObject shootCard = CreateGameCard("ShootingCard", cardsContainer.transform,
                new Vector2(0f, 240f), 450f, shootSprite, "TACTICAL SHOOTER", "SECTOR 7 • 3RD PERSON COMBAT",
                ColorCyanNeon, ColorCyanDark, "ENTER WARZONE");

            Button shootBtn = shootCard.GetComponentInChildren<Button>();

            // Card 2: RACING GAME CARD
            GameObject raceCard = CreateGameCard("RacingCard", cardsContainer.transform,
                new Vector2(0f, -240f), 450f, raceSprite, "HYPER RACING", "MIDNIGHT VELOCITY • SUPERCARS",
                ColorRedNeon, ColorRedDark, "START RACING");

            Button raceBtn = raceCard.GetComponentInChildren<Button>();

            // 3.4 Footer Bar (Settings & Exit)
            GameObject footerObj = CreateUIElement("FooterBar", mainPanelObj.transform);
            RectTransform footerRt = footerObj.GetComponent<RectTransform>();
            footerRt.anchorMin = new Vector2(0.5f, 0f);
            footerRt.anchorMax = new Vector2(0.5f, 0f);
            footerRt.pivot = new Vector2(0.5f, 0f);
            footerRt.anchoredPosition = new Vector2(0f, 50f);
            footerRt.sizeDelta = new Vector2(940f, 120f);

            // Settings Button
            GameObject settingsBtnObj = CreateButton("SettingsButton", footerObj.transform, ColorCardDark, ColorCyanNeon);
            RectTransform setRt = settingsBtnObj.GetComponent<RectTransform>();
            setRt.anchorMin = new Vector2(0f, 0.5f);
            setRt.anchorMax = new Vector2(0f, 0.5f);
            setRt.pivot = new Vector2(0f, 0.5f);
            setRt.anchoredPosition = new Vector2(0f, 0f);
            setRt.sizeDelta = new Vector2(450f, 110f);
            CreateText("SetText", settingsBtnObj.transform, "⚙️ SETTINGS", Vector2.zero, new Vector2(420f, 80f), 34, TextAlignmentOptions.Center);

            // Exit Button
            GameObject exitBtnObj = CreateButton("ExitButton", footerObj.transform, ColorCardDark, ColorRedNeon);
            RectTransform exitRt = exitBtnObj.GetComponent<RectTransform>();
            exitRt.anchorMin = new Vector2(1f, 0.5f);
            exitRt.anchorMax = new Vector2(1f, 0.5f);
            exitRt.pivot = new Vector2(1f, 0.5f);
            exitRt.anchoredPosition = new Vector2(0f, 0f);
            exitRt.sizeDelta = new Vector2(450f, 110f);
            CreateText("ExitText", exitBtnObj.transform, "✖ EXIT GAME", Vector2.zero, new Vector2(420f, 80f), 34, TextAlignmentOptions.Center);

            // 4. Overlays / Sub-Screens
            // 4.1 Shooting Selection Screen
            GameObject shootSelObj = CreateSubScreenModal("ShootingSelectionModal", canvasObj.transform, "SECTOR 7: URBAN WARFARE", ColorCyanNeon);
            ShootingSelectionPanel shootSelPanel = shootSelObj.AddComponent<ShootingSelectionPanel>();
            PopulateSelectionModal(shootSelObj, shootSprite,
                "MISSION BRIEFING:\n• Weapons: M4A1 Tactical Assault Rifle & 9mm Combat Pistol\n• Arena: Abandoned Industrial Sector 7\n• Threat: 5 Hostile Cybernetic Bots\n• Controls: Dual Touch Joysticks (Move + Cam)",
                "DEPLOY TO ARENA", ColorCyanNeon, ColorCyanDark,
                out Button shootPlayBtn, out Button shootBackBtn);

            // 4.2 Racing Selection Screen
            GameObject raceSelObj = CreateSubScreenModal("RacingSelectionModal", canvasObj.transform, "NEO-TOKYO: MIDNIGHT DRIFT", ColorRedNeon);
            RacingSelectionPanel raceSelPanel = raceSelObj.AddComponent<RacingSelectionPanel>();
            PopulateSelectionModal(raceSelObj, raceSprite,
                "RACE TELEMETRY:\n• Vehicle: Apex GT-R Hybrid Twin-Turbo V8 (920 HP)\n• Track: Asphalt City Highway Night Circuit (3.8 KM)\n• Features: Dual-Stage Cryo Nitro & Precision Drifting\n• Controls: Touch Steering / Accel / Brake / Nitro",
                "START RACE ENGINE", ColorRedNeon, ColorRedDark,
                out Button racePlayBtn, out Button raceBackBtn);

            // 4.3 Settings Screen
            GameObject settingsModalObj = CreateSubScreenModal("SettingsModal", canvasObj.transform, "SYSTEM SETTINGS", ColorCyanNeon);
            SettingsPanel settingsPanel = settingsModalObj.AddComponent<SettingsPanel>();
            PopulateSettingsModal(settingsModalObj, out Button settingsCloseBtn, out Button settingsApplyBtn, out Button settingsResetBtn);

            // 4.4 Profile Screen
            GameObject profileModalObj = CreateSubScreenModal("ProfileModal", canvasObj.transform, "OPERATIVE DOSSIER", ColorCyanNeon);
            PlayerProfilePanel profilePanel = profileModalObj.AddComponent<PlayerProfilePanel>();
            PopulateProfileModal(profileModalObj, avatarSprite, out Button profileCloseBtn);

            // Wire all references into MainMenuController
            SetPrivateField(controller, "_mainPanel", mainPanelCg);
            SetPrivateField(controller, "_shootingButton", shootBtn);
            SetPrivateField(controller, "_racingButton", raceBtn);
            SetPrivateField(controller, "_settingsButton", settingsBtnObj.GetComponent<Button>());
            SetPrivateField(controller, "_profileButton", profileBtnObj.GetComponent<Button>());
            SetPrivateField(controller, "_exitButton", exitBtnObj.GetComponent<Button>());
            SetPrivateField(controller, "_soundToggleButton", soundBtnObj.GetComponent<Button>());
            SetPrivateField(controller, "_soundToggleLabel", soundText);
            SetPrivateField(controller, "_settingsPanel", settingsModalObj.GetComponent<CanvasGroup>());
            SetPrivateField(controller, "_profilePanel", profileModalObj.GetComponent<CanvasGroup>());
            SetPrivateField(controller, "_shootingSelectionPanel", shootSelObj.GetComponent<CanvasGroup>());
            SetPrivateField(controller, "_racingSelectionPanel", raceSelObj.GetComponent<CanvasGroup>());

            // Wire references into sub-controllers
            SetPrivateField(shootSelPanel, "_playButton", shootPlayBtn);
            SetPrivateField(shootSelPanel, "_backButton", shootBackBtn);
            SetPrivateField(shootSelPanel, "_mainMenuController", controller);

            SetPrivateField(raceSelPanel, "_playButton", racePlayBtn);
            SetPrivateField(raceSelPanel, "_backButton", raceBackBtn);
            SetPrivateField(raceSelPanel, "_mainMenuController", controller);

            SetPrivateField(settingsPanel, "_closeButton", settingsCloseBtn);
            SetPrivateField(settingsPanel, "_applyButton", settingsApplyBtn);
            SetPrivateField(settingsPanel, "_resetButton", settingsResetBtn);
            SetPrivateField(settingsPanel, "_menuController", controller);

            SetPrivateField(profilePanel, "_closeButton", profileCloseBtn);
            SetPrivateField(profilePanel, "_mainMenuController", controller);

            // Attach animator if not present
            if (gameObject.GetComponent<MainMenuUIAnimator>() == null)
            {
                MainMenuUIAnimator animator = gameObject.AddComponent<MainMenuUIAnimator>();
                SetPrivateField(controller, "_uiAnimator", animator);
            }

            Debug.Log("[MainMenuUIBuilder] Main Menu UI successfully constructed and bound!");
        }

        #endregion

        #region Helper Creators

        private GameObject CreateUIElement(string name, Transform parent)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private void SetStretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;
        }

        private GameObject CreateButton(string name, Transform parent, Color bgColor, Color outlineColor)
        {
            GameObject btnObj = CreateUIElement(name, parent);
            Image img = btnObj.AddComponent<Image>();
            img.color = bgColor;

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);

            Button btn = btnObj.AddComponent<Button>();
            ColorBlock cb = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.2f, 1.2f, 1.2f, 1f);
            cb.pressedColor = new Color(0.7f, 0.7f, 0.7f, 1f);
            btn.colors = cb;

            return btnObj;
        }

        private TMP_Text CreateText(string name, Transform parent, string text, Vector2 pos, Vector2 size, float fontSize, TextAlignmentOptions align)
        {
            GameObject textObj = CreateUIElement(name, parent);
            RectTransform rt = textObj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            TMP_Text tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = ColorTextWhite;
            tmp.enableWordWrapping = true;
            return tmp;
        }

        private GameObject CreateGameCard(string name, Transform parent, Vector2 pos, float height, Sprite banner, string title, string subtitle, Color accentColor, Color darkColor, string buttonText)
        {
            GameObject cardObj = CreateUIElement(name, parent);
            RectTransform cardRt = cardObj.GetComponent<RectTransform>();
            cardRt.anchoredPosition = pos;
            cardRt.sizeDelta = new Vector2(920f, height);

            Image cardBg = cardObj.AddComponent<Image>();
            cardBg.color = ColorCardDark;

            Outline outline = cardObj.AddComponent<Outline>();
            outline.effectColor = accentColor;
            outline.effectDistance = new Vector2(3f, -3f);

            // Banner Image
            if (banner != null)
            {
                GameObject bannerObj = CreateUIElement("Banner", cardObj.transform);
                RectTransform bannerRt = bannerObj.GetComponent<RectTransform>();
                bannerRt.anchorMin = new Vector2(0.5f, 1f);
                bannerRt.anchorMax = new Vector2(0.5f, 1f);
                bannerRt.pivot = new Vector2(0.5f, 1f);
                bannerRt.anchoredPosition = new Vector2(0f, -15f);
                bannerRt.sizeDelta = new Vector2(890f, 260f);

                Image img = bannerObj.AddComponent<Image>();
                img.sprite = banner;
                img.preserveAspect = false;

                Outline bannerOutline = bannerObj.AddComponent<Outline>();
                bannerOutline.effectColor = new Color(0f, 0f, 0f, 0.6f);
                bannerOutline.effectDistance = new Vector2(1f, -1f);
            }

            // Title & Subtitle
            CreateText("CardTitle", cardObj.transform, title, new Vector2(0f, -60f), new Vector2(880f, 50f), 42, TextAlignmentOptions.Center);
            CreateText("CardSub", cardObj.transform, $"<color=#{ColorUtility.ToHtmlStringRGBA(accentColor)}>{subtitle}</color>",
                new Vector2(0f, -105f), new Vector2(880f, 35f), 24, TextAlignmentOptions.Center);

            // Action Button
            GameObject btnObj = CreateButton("CardActionButton", cardObj.transform, darkColor, accentColor);
            RectTransform btnRt = btnObj.GetComponent<RectTransform>();
            btnRt.anchorMin = new Vector2(0.5f, 0f);
            btnRt.anchorMax = new Vector2(0.5f, 0f);
            btnRt.pivot = new Vector2(0.5f, 0f);
            btnRt.anchoredPosition = new Vector2(0f, 20f);
            btnRt.sizeDelta = new Vector2(860f, 85f);

            CreateText("BtnText", btnObj.transform, $"▶ {buttonText}", Vector2.zero, new Vector2(800f, 70f), 34, TextAlignmentOptions.Center);

            return cardObj;
        }

        private GameObject CreateSubScreenModal(string name, Transform parent, string title, Color accentColor)
        {
            GameObject modalObj = CreateUIElement(name, parent);
            SetStretchFull(modalObj.GetComponent<RectTransform>());

            CanvasGroup cg = modalObj.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
            modalObj.SetActive(false);

            // Dark tinted background
            Image modalBg = modalObj.AddComponent<Image>();
            modalBg.color = new Color(0.02f, 0.03f, 0.06f, 0.98f);

            // Header Title
            CreateText("ModalHeader", modalObj.transform, $"— {title} —",
                new Vector2(0f, 860f), new Vector2(980f, 80f), 46, TextAlignmentOptions.Center);

            return modalObj;
        }

        private void PopulateSelectionModal(GameObject modal, Sprite banner, string briefing, string playBtnText, Color accentColor, Color darkColor, out Button playBtn, out Button backBtn)
        {
            // Banner Image
            if (banner != null)
            {
                GameObject bannerObj = CreateUIElement("ModalBanner", modal.transform);
                RectTransform bannerRt = bannerObj.GetComponent<RectTransform>();
                bannerRt.anchoredPosition = new Vector2(0f, 520f);
                bannerRt.sizeDelta = new Vector2(960f, 520f);
                Image img = bannerObj.AddComponent<Image>();
                img.sprite = banner;

                Outline outl = bannerObj.AddComponent<Outline>();
                outl.effectColor = accentColor;
                outl.effectDistance = new Vector2(3f, -3f);
            }

            // Briefing Box
            GameObject briefingBox = CreateUIElement("BriefingBox", modal.transform);
            RectTransform boxRt = briefingBox.GetComponent<RectTransform>();
            boxRt.anchoredPosition = new Vector2(0f, 40f);
            boxRt.sizeDelta = new Vector2(960f, 380f);
            Image boxImg = briefingBox.AddComponent<Image>();
            boxImg.color = ColorCardDark;
            Outline boxOutl = briefingBox.AddComponent<Outline>();
            boxOutl.effectColor = new Color(0.3f, 0.4f, 0.6f, 0.5f);

            CreateText("BriefingText", briefingBox.transform, briefing, Vector2.zero, new Vector2(900f, 340f), 28, TextAlignmentOptions.Left);

            // Play / Deploy Button
            GameObject playBtnObj = CreateButton("PlayButton", modal.transform, darkColor, accentColor);
            RectTransform playRt = playBtnObj.GetComponent<RectTransform>();
            playRt.anchoredPosition = new Vector2(0f, -280f);
            playRt.sizeDelta = new Vector2(960f, 130f);
            CreateText("PlayText", playBtnObj.transform, $"⚡ {playBtnText}", Vector2.zero, new Vector2(900f, 100f), 42, TextAlignmentOptions.Center);
            playBtn = playBtnObj.GetComponent<Button>();

            // Back Button
            GameObject backBtnObj = CreateButton("BackButton", modal.transform, ColorCardDark, ColorTextMuted);
            RectTransform backRt = backBtnObj.GetComponent<RectTransform>();
            backRt.anchoredPosition = new Vector2(0f, -460f);
            backRt.sizeDelta = new Vector2(960f, 110f);
            CreateText("BackText", backBtnObj.transform, "◀ RETURN TO MAIN HUB", Vector2.zero, new Vector2(900f, 90f), 32, TextAlignmentOptions.Center);
            backBtn = backBtnObj.GetComponent<Button>();
        }

        private void PopulateSettingsModal(GameObject modal, out Button closeBtn, out Button applyBtn, out Button resetBtn)
        {
            GameObject container = CreateUIElement("SettingsContainer", modal.transform);
            RectTransform contRt = container.GetComponent<RectTransform>();
            contRt.anchoredPosition = new Vector2(0f, 40f);
            contRt.sizeDelta = new Vector2(960f, 1300f);

            // Sliders & settings list
            float startY = 520f;
            float stepY = -150f;

            CreateSettingLabel("QualityLabel", container.transform, "GRAPHICS QUALITY:", new Vector2(0f, startY));
            CreateSettingLabel("QualityDesc", container.transform, "<size=24><color=#8FA0C0>LOW (Battery) • MEDIUM (60 FPS Balanced) • HIGH (URP Max)</color></size>", new Vector2(0f, startY - 45f));

            CreateSettingLabel("FPSLabel", container.transform, "FRAME RATE PREFERENCE:", new Vector2(0f, startY + stepY));
            CreateSettingLabel("FPSDesc", container.transform, "<size=24><color=#8FA0C0>30 FPS (Power Saver) • 60 FPS (Ultra Smooth) • 90/120 FPS</color></size>", new Vector2(0f, startY + stepY - 45f));

            CreateSettingLabel("MasterLabel", container.transform, "MASTER AUDIO VOLUME", new Vector2(0f, startY + stepY * 2));
            CreateSettingLabel("MusicLabel", container.transform, "BACKGROUND SOUNDTRACK VOLUME", new Vector2(0f, startY + stepY * 3));
            CreateSettingLabel("SFXLabel", container.transform, "COMBAT & ENGINE SFX VOLUME", new Vector2(0f, startY + stepY * 4));
            CreateSettingLabel("SensLabel", container.transform, "TOUCH / JOYSTICK SENSITIVITY", new Vector2(0f, startY + stepY * 5));

            // Apply Button
            GameObject applyBtnObj = CreateButton("ApplyButton", modal.transform, ColorCyanDark, ColorCyanNeon);
            RectTransform appRt = applyBtnObj.GetComponent<RectTransform>();
            appRt.anchoredPosition = new Vector2(-250f, -700f);
            appRt.sizeDelta = new Vector2(440f, 110f);
            CreateText("AppText", applyBtnObj.transform, "✔ APPLY CHANGES", Vector2.zero, new Vector2(400f, 80f), 30, TextAlignmentOptions.Center);
            applyBtn = applyBtnObj.GetComponent<Button>();

            // Reset Button
            GameObject resetBtnObj = CreateButton("ResetButton", modal.transform, ColorCardDark, ColorRedNeon);
            RectTransform rstRt = resetBtnObj.GetComponent<RectTransform>();
            rstRt.anchoredPosition = new Vector2(250f, -700f);
            rstRt.sizeDelta = new Vector2(440f, 110f);
            CreateText("RstText", resetBtnObj.transform, "↺ RESET DEFAULTS", Vector2.zero, new Vector2(400f, 80f), 30, TextAlignmentOptions.Center);
            resetBtn = resetBtnObj.GetComponent<Button>();

            // Close Button
            GameObject closeBtnObj = CreateButton("CloseButton", modal.transform, ColorCardDark, ColorTextMuted);
            RectTransform clsRt = closeBtnObj.GetComponent<RectTransform>();
            clsRt.anchoredPosition = new Vector2(0f, -840f);
            clsRt.sizeDelta = new Vector2(940f, 100f);
            CreateText("ClsText", closeBtnObj.transform, "◀ BACK TO MENU", Vector2.zero, new Vector2(900f, 80f), 32, TextAlignmentOptions.Center);
            closeBtn = closeBtnObj.GetComponent<Button>();
        }

        private void CreateSettingLabel(string name, Transform parent, string text, Vector2 pos)
        {
            GameObject obj = CreateUIElement(name, parent);
            RectTransform rt = obj.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(900f, 50f);
            TMP_Text tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 30;
            tmp.alignment = TextAlignmentOptions.Left;
            tmp.color = ColorTextWhite;
        }

        private void PopulateProfileModal(GameObject modal, Sprite avatar, out Button closeBtn)
        {
            GameObject dossierBox = CreateUIElement("DossierBox", modal.transform);
            RectTransform dRt = dossierBox.GetComponent<RectTransform>();
            dRt.anchoredPosition = new Vector2(0f, 40f);
            dRt.sizeDelta = new Vector2(960f, 1300f);
            Image dImg = dossierBox.AddComponent<Image>();
            dImg.color = ColorCardDark;
            Outline dOutl = dossierBox.AddComponent<Outline>();
            dOutl.effectColor = ColorCyanNeon;

            // Avatar
            if (avatar != null)
            {
                GameObject avObj = CreateUIElement("DossierAvatar", dossierBox.transform);
                RectTransform avRt = avObj.GetComponent<RectTransform>();
                avRt.anchoredPosition = new Vector2(0f, 460f);
                avRt.sizeDelta = new Vector2(260f, 260f);
                Image img = avObj.AddComponent<Image>();
                img.sprite = avatar;
            }

            CreateText("DossierName", dossierBox.transform, "COMMANDER YASH", new Vector2(0f, 280f), new Vector2(800f, 60f), 48, TextAlignmentOptions.Center);
            CreateText("DossierRank", dossierBox.transform, "<color=#00D9FF>APEX WARRIOR ★★★ • LEVEL 24</color>", new Vector2(0f, 220f), new Vector2(800f, 50f), 32, TextAlignmentOptions.Center);

            // Stats
            string statsText =
                "<color=#00D9FF>COMBAT TELEMETRY:</color>\n" +
                "• Matches Played: 48\n" +
                "• Enemy Bots Neutralized: 136\n" +
                "• Tactical Accuracy: 68.4%\n" +
                "• Win Ratio: 72.9%\n\n" +
                "<color=#FF2E59>RACING TELEMETRY:</color>\n" +
                "• Grand Prix Entered: 56\n" +
                "• 1st Place Podiums: 38\n" +
                "• Top Speed Record: 345 KM/H\n" +
                "• Total Drift Score: 184,200 PTS";

            CreateText("DossierStats", dossierBox.transform, statsText, new Vector2(0f, -140f), new Vector2(860f, 580f), 30, TextAlignmentOptions.Left);

            // Close Button
            GameObject closeBtnObj = CreateButton("CloseButton", modal.transform, ColorCardDark, ColorCyanNeon);
            RectTransform clsRt = closeBtnObj.GetComponent<RectTransform>();
            clsRt.anchoredPosition = new Vector2(0f, -760f);
            clsRt.sizeDelta = new Vector2(900f, 110f);
            CreateText("CloseText", closeBtnObj.transform, "◀ CLOSE DOSSIER", Vector2.zero, new Vector2(860f, 90f), 32, TextAlignmentOptions.Center);
            closeBtn = closeBtnObj.GetComponent<Button>();
        }

        private Sprite LoadSprite(string resourcePath)
        {
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null)
            {
                // Try direct Assets load if in editor or streaming
                tex = null;
            }
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            return null;
        }

        private void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() == null)
            {
                GameObject es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }

        private void SetPrivateField(object target, string fieldName, object value)
        {
            if (target == null) return;
            var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (field != null)
            {
                field.SetValue(target, value);
            }
        }

        #endregion
    }
}

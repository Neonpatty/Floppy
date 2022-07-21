/* Color Studio by Ramiro Oliva (Kronnect)   /
/  Premium assets for Unity on kronnect.com */


using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Globalization;

namespace ColorStudio {

    enum ViewMode {
        ColorWheel = 0,
        Palette = 1,
        Tools = 2,
        Compact = 3,
        Inventory = 4,
        Help = 5
    }

#if UNITY_2019_4_OR_NEWER
    enum ColorEncoding {
        [InspectorName("RGB (0-1)")]
        RGB01 = 0,
        [InspectorName("RGB (0-255)")]
        RGB255 = 1,
        HSL = 2,
        HEX = 3
    }
#else
    enum ColorEncoding {
        RGB01 = 0,
        RGB255 = 1,
        HSL = 2,
        HEX = 3
    }
#endif

    public delegate void CSEvent();

    public partial class CSWindow : EditorWindow {

        const string PREFS_SETTINGS = "Color Studio Settings";
        const float CENTER_WIDTH = 0.2f;
        const float PI2 = Mathf.PI * 2f;
        const float HALF_PI = Mathf.PI * 0.5f;

        public static Color currentPrimaryColor = Color.white;
        public static Color currentSecondaryColor;
        public static event CSEvent onColorChange;

        [SerializeField] ViewMode viewMode = ViewMode.ColorWheel;
        [SerializeField] Vector2 scrollPos;
        [SerializeField] Color selectionColor;
        [SerializeField] float selectionLightness;
        [SerializeField] ColorEncoding selectionInputMode;
        [SerializeField] string selectionInput;
        [SerializeField] ColorEncoding primaryInputMode;
        [SerializeField] string primaryInput;
        [SerializeField] Color customColorPicker = Color.white;
        [SerializeField] int selectedCustomColor = -1;
        [SerializeField] Vector2 selectionPos;
        [SerializeField] CSPalette otherPalette;
        [SerializeField] int selectedKey = -1;
        [SerializeField] float clickedAngle;
        [SerializeField] Color selectedObjectColor = Color.white;
        [SerializeField] GameObject selectedObject;
        [SerializeField] Color nearestColor;
        [SerializeField] ColorMatchMode colorMatchMode;
        [SerializeField] Texture2D nearestTexture;
        [SerializeField] Texture2D referenceTexture;

        public static CSPalette palette;
        GUIContent[] schemeTexts;
        GUIContent[] viewModeTexts;
        Material cwMat;
        bool mouseDown;
        double startAnimation;
        CSPalette[] projectPalettes;
        Texture2D duplicateIcon, trashIcon, upIcon, downIcon, kronnect;
        Vector4[] cwCustomColors;
        double lastClickTime;

        public static CSWindow ShowWindow(int tabIndex = -1) {
            Vector3 size = new Vector3(400, 600);
            Vector3 position = new Vector3(Screen.width / 2 - size.x / 2, Screen.height / 2 - size.y / 2);
            Rect rect = new Rect(position, size);
            CSWindow window = GetWindowWithRect<CSWindow>(rect, false, "Palette Studio", true);
            if (tabIndex >= 0) {
                window.viewMode = (ViewMode)tabIndex;
            }
            window.maxSize = new Vector2(2048, 2048);
            window.minSize = new Vector2(205, 20);
            return window;
        }

        void OnEnable() {
            string data = EditorPrefs.GetString(PREFS_SETTINGS, JsonUtility.ToJson(this, false));
            if (!string.IsNullOrEmpty(data)) {
                JsonUtility.FromJsonOverwrite(data, this);
            }

            schemeTexts = new GUIContent[] {
                new GUIContent ("Monochromatic"),
                new GUIContent ("Complementary"),
                new GUIContent ("Gradient"),
                new GUIContent ("Analogous"),
                new GUIContent ("Split\nComplementary"),
                new GUIContent ("Accented\nAnalogous"),
                new GUIContent ("Triadic"),
                new GUIContent ("Tetradic"),
                new GUIContent ("Square"),
                new GUIContent ("Spectrum"),
                new GUIContent ("Custom")
            };
            string iconsPath = "Color Studio/Icons/";
            viewModeTexts = new GUIContent[] {
                new GUIContent (Resources.Load<Texture2D> (iconsPath + "colorWheel"), "Color Wheel"),
                new GUIContent (Resources.Load<Texture2D> (iconsPath + "palette"), "Palette"),
                new GUIContent (Resources.Load<Texture2D> (iconsPath + "brush"), "Tools"),
                new GUIContent (Resources.Load<Texture2D> (iconsPath + "compact"), "Compact Mode"),
                new GUIContent (Resources.Load<Texture2D> (iconsPath + "inventory"), "Project Palettes"),
                new GUIContent (Resources.Load<Texture2D> (iconsPath + "help"), "Help & Support")
            };
            duplicateIcon = Resources.Load<Texture2D>(iconsPath + "duplicate");
            trashIcon = Resources.Load<Texture2D>(iconsPath + "trash");
            upIcon = Resources.Load<Texture2D>(iconsPath + "up");
            downIcon = Resources.Load<Texture2D>(iconsPath + "down");
            kronnect = Resources.Load<Texture2D>(iconsPath + "kronnect");
            titleContent = new GUIContent("Color Studio", Resources.Load<Texture2D>(iconsPath + "icon"));
            if (cwCustomColors == null || cwCustomColors.Length == 0) {
                cwCustomColors = new Vector4[CSPalette.MAX_KEY_COLORS];
            }

            if (palette == null) {
                palette = Resources.Load<CSPalette>("Color Studio/DraftPalette");
                if (palette == null) {
                    palette = CreateInstance<CSPalette>();
                }
                if (otherPalette != null) {
                    LoadPalette(otherPalette);
                }
            }

            UpdateCWMaterial();
            SetColorKeys();
            FindProjectPalettes();
            if (selectionColor.a == 0) {
                currentPrimaryColor = Color.white;
            } else {
                currentPrimaryColor = selectionColor;
            }
        }

        void OnDisable() {
            if (palette) {
                EditorUtility.SetDirty(palette);
            }
            // autosave
            if (otherPalette != null) {
                SavePalette();
            }
            string data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString(PREFS_SETTINGS, data);
        }

        private void OnDestroy() {
            NewPalette();
            EditorPrefs.SetString(PREFS_SETTINGS, "");
        }



        void OnGUI() {

            bool issueRepaint = false;
            bool paletteChanges = false;

            if (cwMat == null || palette.material == null || palette.material.GetColorArray("_Colors") == null) {
                palette.UpdateMaterial();
                UpdateCWMaterial();
                paletteChanges = true;
            }
            EditorGUIUtility.labelWidth = 80;

            EditorGUI.BeginChangeCheck();
            viewMode = (ViewMode)GUILayout.SelectionGrid((int)viewMode, viewModeTexts, 6, GUILayout.MaxHeight(28));
            if (EditorGUI.EndChangeCheck()) {
                if (viewMode == ViewMode.Compact) {
                    maxSize = new Vector2(maxSize.x, 105);
                    minSize = new Vector2(minSize.x, 105);
                } else {
                    maxSize = new Vector2(4000, 4000);
                    minSize = new Vector2(minSize.x, 200);
                }
                if (viewMode == ViewMode.Inventory) {
                    FindProjectPalettes();
                }
            }

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

            Rect space = new Rect();
            Vector3 pos;
            float maxWidth = EditorGUIUtility.currentViewWidth - 12;
            if (Screen.height < 772) maxWidth -= 8;

            EditorGUILayout.Separator();

            if (viewMode == ViewMode.ColorWheel) {

                GUILayout.Label("C O L O R   W H E E L");

                // Color sheme selector
                EditorGUI.BeginChangeCheck();
                ColorScheme prevScheme = palette.scheme;
                int xCount = Mathf.Clamp((int)(maxWidth / 95), 2, schemeTexts.Length);
                palette.scheme = (ColorScheme)GUILayout.SelectionGrid((int)palette.scheme, schemeTexts, xCount, GUILayout.MaxWidth(maxWidth));
                if (EditorGUI.EndChangeCheck()) {
                    if (prevScheme == palette.scheme) {
                        palette.primaryAngle = Random.Range(0, Mathf.PI * 2f);
                    } else {
                        palette.splitAmount = 0.6f;
                        palette.hueCount = palette.scheme.recommendedHues();
                    }
                    UpdateCWMaterial();
                    paletteChanges = true;
                }

                // Draw color wheel
                if (palette.scheme != ColorScheme.Spectrum) {
                    EditorGUILayout.Separator();

                    space = EditorGUILayout.BeginVertical();
                    GUILayout.Space(EditorGUIUtility.currentViewWidth);
                    EditorGUILayout.EndVertical();
                    space.xMin += 10;
                    space.xMax -= 10;
                    space.yMin += 10;
                    space.yMax -= 10;

                    GUI.BeginGroup(space);
                    pos = Event.current.mousePosition;
                    GUI.EndGroup();

                    pos.x /= space.width;
                    pos.y /= space.height;
                    pos.y = 1f - pos.y;
                    Vector2 cursorPos = pos;
                    pos.x -= 0.5f;
                    pos.y -= 0.5f;
                    float d = pos.x * pos.x + pos.y * pos.y;

                    if (d < 0.25f) {
                        float cursorAngle = Mathf.Atan2(pos.y, pos.x) + Mathf.PI;
                        int cursorKey = -1;
                        if (!mouseDown) {
                            bool changes = false;
                            for (int k = 0; k < palette.keyColors.Length; k++) {
                                if (palette.keyColors[k].visible) {
                                    float distance = (cursorPos - palette.keyColors[k].pos).sqrMagnitude;
                                    if (distance < 0.0008f) {
                                        if (!palette.keyColors[k].highlighted) {
                                            changes = true;
                                        }
                                        palette.keyColors[k].highlighted = true;
                                        cursorKey = k;
                                    } else {
                                        if (palette.keyColors[k].highlighted) {
                                            changes = true;
                                        }
                                        palette.keyColors[k].highlighted = false;
                                    }
                                }
                            }
                            if (changes) {
                                UpdateCWMaterial();
                            }
                        }
                        if (cursorKey < 0 && selectedKey < 0 && Event.current.type == EventType.MouseUp && Event.current.control) {
                            // Add key
                            AddCustomColor(cursorAngle);
                            paletteChanges = true;
                        }
                        if (Event.current.type == EventType.MouseDown && !Event.current.control) {
                            double now = EditorApplication.timeSinceStartup;
                            if (cursorKey >= CSPalette.START_INDEX_CUSTOM_COLOR && now - lastClickTime < 0.3f) {
                                palette.keyColors[cursorKey].visible = false;
                                cursorKey = -1;
                                paletteChanges = true;
                            }
                            mouseDown = true;
                            selectedKey = cursorKey;
                            clickedAngle = cursorAngle;
                            lastClickTime = now;
                        }
                        if (mouseDown && selectedKey >= 0) {
                            float delta = cursorAngle - clickedAngle;
                            if (delta < -Mathf.PI) {
                                delta = Mathf.PI * 2 + delta;
                            } else if (delta > Mathf.PI) {
                                delta = Mathf.PI * 2 - delta;
                            }
                            if (delta != 0) {
                                switch (palette.keyColors[selectedKey].type) {
                                    case KeyColorType.Primary:
                                        palette.primaryAngle += delta;
                                        break;
                                    case KeyColorType.Complementary:
                                        switch (palette.scheme.keyAdjustment(selectedKey)) {
                                            case KeyAdjustment.RotateComplementary:
                                                palette.splitAmount += delta;
                                                break;
                                            case KeyAdjustment.RotateComplementaryInverted:
                                                palette.splitAmount -= delta;
                                                break;
                                            case KeyAdjustment.RotatePrimary:
                                                palette.primaryAngle += delta;
                                                break;
                                        }
                                        if (palette.scheme != ColorScheme.Gradient) {
                                            palette.splitAmount = Mathf.Clamp(palette.splitAmount, 0, Mathf.PI * 0.5f);
                                        }
                                        break;
                                    case KeyColorType.Custom:
                                        SetKey(selectedKey, KeyColorType.Custom, palette.keyColors[selectedKey].angle + delta);
                                        break;
                                }
                                clickedAngle = cursorAngle;
                                paletteChanges = true;

                                UpdatePrimaryInput();
                            }
                        }
                    }

                    EditorGUI.DrawPreviewTexture(space, Texture2D.whiteTexture, cwMat, ScaleMode.ScaleToFit);

                    if (palette.customColorsCount > 0) {
                        space.x = space.xMax - 50;
                        space.height = 20;
                        space.width = 50;
                        if (GUI.Button(space, "Clear", EditorStyles.miniButton)) {
                            ClearCustomColors();
                            paletteChanges = true;
                        }
                    }
                }
            }

            if (viewMode == ViewMode.Palette || viewMode == ViewMode.ColorWheel) {
                GUILayout.Label("P R I M A R Y");
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginChangeCheck();
                primaryInputMode = (ColorEncoding)EditorGUILayout.EnumPopup(primaryInputMode, GUILayout.MaxWidth(60));
                if (EditorGUI.EndChangeCheck()) {
                    UpdatePrimaryInput();
                }

                primaryInput = EditorGUILayout.TextField(GUIContent.none, primaryInput, GUILayout.MaxWidth(maxWidth - 110));
                GUI.enabled = !string.IsNullOrEmpty(primaryInput);
                if (GUILayout.Button("Select", EditorStyles.miniButtonRight, GUILayout.MaxWidth(50))) {
                    if (SelectPrimaryInput()) {
                        paletteChanges = true;
                    }
                }
                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Separator();

                GUILayout.Label("C U R R E N T   P A L E T T E");

                // Palette modifiers
                EditorGUI.BeginChangeCheck();
                if (palette.scheme != ColorScheme.Monochromatic && palette.scheme != ColorScheme.Custom) {
                    palette.hueCount = EditorGUILayout.IntSlider("Hues", palette.hueCount, palette.scheme.minHues(), 128);
                }
                palette.shades = EditorGUILayout.IntSlider("Shades", palette.shades, 1, 256);
                palette.saturation = EditorGUILayout.Slider("Saturation", palette.saturation, 0, 1);
                float minb = palette.minBrightness, maxb = palette.maxBrightness;
                EditorGUILayout.MinMaxSlider("Brightness", ref minb, ref maxb, 0, 1);
                palette.minBrightness = minb;
                palette.maxBrightness = maxb;
                EditorGUILayout.BeginHorizontal();
                palette.kelvin = EditorGUILayout.IntField(new GUIContent("Kelvin", "Color temperature (1000-40.000 ºKelvin)"), palette.kelvin);
                palette.kelvin = Mathf.Clamp(palette.kelvin, 1000, 40000);
                palette.colorTempStrength = EditorGUILayout.Slider(palette.colorTempStrength, 0, 1f);
                EditorGUILayout.EndHorizontal();

                if (EditorGUI.EndChangeCheck()) {
                    UpdateCWMaterial();
                    paletteChanges = true;
                }
            }

            if (viewMode == ViewMode.Tools) {
                DrawConversionTools();
            }

            // Draw palette

            if (viewMode == ViewMode.ColorWheel || viewMode == ViewMode.Palette || viewMode == ViewMode.Tools) {
                EditorGUILayout.BeginVertical(GUI.skin.box);
            }

            if (viewMode != ViewMode.Inventory && viewMode != ViewMode.Help) {
                space = EditorGUILayout.BeginVertical();
                float paletteRowSize;
                if (viewMode == ViewMode.Palette) {
                    paletteRowSize = Mathf.Max(64, EditorGUIUtility.currentViewWidth);
                } else {
                    paletteRowSize = 64;
                }
                GUILayout.Space(paletteRowSize);
                EditorGUILayout.EndVertical();
            }

            if (viewMode == ViewMode.ColorWheel || viewMode == ViewMode.Palette || viewMode == ViewMode.Tools) {
                GUI.BeginGroup(space);
                Vector3 palettePos = Event.current.mousePosition;
                palettePos.x /= space.width;
                palettePos.y /= space.height;
                palettePos.y = 1f - palettePos.y;
                GUI.EndGroup();
                if (space.height != 0) {
                    palette.material.SetFloat("_Aspect", (float)space.width / space.height);
                }

                EditorGUI.DrawPreviewTexture(space, Texture2D.whiteTexture, palette.material);

                if (viewMode != ViewMode.Tools) {
                    palette.material.SetVector("_CursorPos", selectionPos);

                    EditorGUILayout.BeginHorizontal();
                    EditorGUI.BeginChangeCheck();
                    selectionInputMode = (ColorEncoding)EditorGUILayout.EnumPopup(selectionInputMode, GUILayout.Width(60));
                    if (EditorGUI.EndChangeCheck()) {
                        UpdateSelectionInput();
                    }

                    selectionInput = EditorGUILayout.TextField(GUIContent.none, selectionInput, GUILayout.MaxWidth(maxWidth - 116));
                    GUI.enabled = !string.IsNullOrEmpty(selectionInput);
                    if (GUILayout.Button("Add", EditorStyles.miniButtonRight, GUILayout.MaxWidth(50))) {
                        AddSelectionInput();
                    }
                    GUI.enabled = true;
                    EditorGUILayout.EndHorizontal();

                    if (selectedCustomColor >= 0) {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("Delete Color", EditorStyles.miniButton)) {
                            DeleteColor();
                        }
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndHorizontal();
                    }
                }
                if (viewMode == ViewMode.Palette) {

                    EditorGUILayout.BeginHorizontal();
                    customColorPicker = EditorGUILayout.ColorField("Color Picker", customColorPicker);
                    if (GUILayout.Button("Add", EditorStyles.miniButtonRight, GUILayout.MaxWidth(50))) {
                        AddCustomColor(customColorPicker);
                        paletteChanges = true;
                    }
                    EditorGUILayout.EndHorizontal();
                    CSPalette prevPalette = otherPalette;
                    otherPalette = (CSPalette)EditorGUILayout.ObjectField("Palette", otherPalette, typeof(CSPalette), false);
                    if (otherPalette != prevPalette && otherPalette != null) {
                        LoadPalette(otherPalette);
                        paletteChanges = true;
                    }

                }
                if (palettePos.x >= 0 && palettePos.x <= 1f && palettePos.y >= 0 && palettePos.y <= 1f) {
                    bool click = Event.current.type == EventType.MouseDown;
                    if (click) {
                        mouseDown = true;
                    }
                    if (mouseDown) {
                        issueRepaint = true;
                        selectedCustomColor = -1;
                        selectionPos = palettePos;
                        int colorIndex = Mathf.FloorToInt(palettePos.x * palette.colorsCount);
                        float y = palette.minBrightness + (palette.maxBrightness - palette.minBrightness) * palettePos.y;
                        selectionLightness = Mathf.Clamp01((Mathf.FloorToInt(y * palette.shades) + 0.5f) / palette.shades);
                        if (palette.scheme == ColorScheme.Custom) {
                            selectedCustomColor = colorIndex + CSPalette.START_INDEX_CUSTOM_COLOR;
                            selectionColor = palette.keyColors[selectedCustomColor].color;
                        } else if (palette.shades == 1 && colorIndex >= palette.hueCount) {
                            selectedCustomColor = colorIndex + CSPalette.START_INDEX_CUSTOM_COLOR - palette.hueCount;
                            selectionColor = palette.keyColors[selectedCustomColor].color;
                        } else {
                            if (colorIndex >= palette.hueCount) {
                                selectedCustomColor = colorIndex + CSPalette.START_INDEX_CUSTOM_COLOR - palette.hueCount;
                            }
                            selectionColor = ColorConversion.GetColorFromRGBSL(palette.colors[colorIndex].r, palette.colors[colorIndex].g, palette.colors[colorIndex].b, palette.saturation, selectionLightness);
                        }
                        if (click) {
                            if (Event.current.button == 0) {
                                currentPrimaryColor = selectionColor;
                                if (onColorChange != null) onColorChange();
                            } else {
                                currentSecondaryColor = selectionColor;
                                if (onColorChange != null) onColorChange();
                            }
                        }

                        UpdateSelectionInput();
                    }
                }
                EditorGUILayout.EndVertical();

            } else if (viewMode == ViewMode.Compact) {
                palette.material.SetVector("_CursorPos", Vector3.left);
                EditorGUI.DrawPreviewTexture(space, Texture2D.whiteTexture, palette.material);
            } else if (viewMode == ViewMode.Inventory) {
                palette.material.SetVector("_CursorPos", Vector3.left);
                if (projectPalettes == null) {
                    FindProjectPalettes();
                }

                GUILayout.Label("A L L   P A L E T T E S (" + projectPalettes.Length + ")");

                for (int k = 0; k < projectPalettes.Length; k++) {
                    CSPalette pal = projectPalettes[k];
                    EditorGUILayout.BeginVertical(GUI.skin.box);
                    if (pal == null) {
                        GUI.enabled = false;
                        GUILayout.Button("(Deleted)");
                        EditorGUILayout.BeginVertical();
                        GUILayout.Space(64);
                        EditorGUILayout.EndVertical();
                        GUI.enabled = true;

                    } else {
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button(new GUIContent(pal.name, "Click to load " + pal.name))) {
                            EditorGUIUtility.PingObject(pal);
                            LoadPalette(pal);
                            viewMode = ViewMode.Palette;
                            GUIUtility.ExitGUI();
                        }
                        if (GUILayout.Button(new GUIContent("?", "Locate"), GUILayout.MaxWidth(24))) {
                            EditorGUIUtility.PingObject(pal);
                            Selection.activeObject = pal;
                        }
                        GUI.enabled = k > 0;
                        if (GUILayout.Button(new GUIContent(upIcon, "Move Up"), GUILayout.MaxWidth(24), GUILayout.MaxHeight(18))) {
                            pal.order = projectPalettes[k - 1].order - 1;
                            EditorUtility.SetDirty(pal);
                            ResortProjectPalettes();
                        }
                        GUI.enabled = k < projectPalettes.Length - 1;
                        if (GUILayout.Button(new GUIContent(downIcon, "Move Down"), GUILayout.MaxWidth(24), GUILayout.MaxHeight(18))) {
                            pal.order = projectPalettes[k + 1].order + 1;
                            EditorUtility.SetDirty(pal);
                            ResortProjectPalettes();
                        }
                        GUI.enabled = true;
                        if (GUILayout.Button(new GUIContent(duplicateIcon, "Duplicate"), GUILayout.MaxWidth(24), GUILayout.MaxHeight(18))) {
                            if (DuplicatePalette(pal)) {
                                FindProjectPalettes();
                                GUIUtility.ExitGUI();
                            }
                        }
                        if (GUILayout.Button(new GUIContent(trashIcon, "Delete"), GUILayout.MaxWidth(24), GUILayout.MaxHeight(18))) {
                            if (DeletePalette(pal)) {
                                projectPalettes[k] = null;
                            }
                        }
                        EditorGUILayout.EndHorizontal();
                        space = EditorGUILayout.BeginVertical();
                        GUILayout.Space(64);
                        EditorGUILayout.EndVertical();
                        pal.UpdateMaterial();
                        EditorGUI.DrawPreviewTexture(space, Texture2D.whiteTexture, pal.material);
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.Separator();
                }
            }

            if (viewMode == ViewMode.Palette) {

                float width = Mathf.Max(20, EditorGUIUtility.currentViewWidth / 3f - 6f);
                bool twoColumns = false;
                if (width < 105) {
                    twoColumns = true;
                    width = Mathf.Max(20, EditorGUIUtility.currentViewWidth / 2f - 7f);
                }
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("New", "Creates a new palette."), GUILayout.Width(width))) {
                    NewPalette();
                }
                GUI.enabled = otherPalette != null;
                if (GUILayout.Button(new GUIContent("Save", "Replace existing palette file."), GUILayout.Width(width))) {
                    if (EditorUtility.DisplayDialog("Confirmation", "Replace existing palette?", "Yes", "No")) {
                        SavePalette();
                    }
                }
                GUI.enabled = true;
                if (twoColumns) {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
                if (GUILayout.Button(new GUIContent("Save As New", "Saves current palette to a new file."), GUILayout.Width(width))) {
                    SaveAsNewPalette(palette);
                }
                if (!twoColumns) {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
                GUI.enabled = otherPalette != null;
                if (GUILayout.Button(new GUIContent("Load", "Loads palette from file."), GUILayout.Width(width))) {
                    LoadPalette(otherPalette);
                    paletteChanges = true;
                }
                GUI.enabled = true;
                if (twoColumns) {
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
                if (GUILayout.Button(new GUIContent("Export LUT", "Generates a LUT (color look-up texture) which converts RGB colors to current palette. This LUT can be used with Beautify as a full-screen image effect color converter."), GUILayout.Width(width))) {
                    ExportLUT();
                }
                if (GUILayout.Button(new GUIContent("Generate Code", "Generates C# code with the palette color information."), GUILayout.Width(width))) {
                    ExportCode();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(new GUIContent("Import ASE", "Imports a palette from a file in ASE format."), GUILayout.Width(width))) {
                    ImportASE();
                    paletteChanges = true;
                }
                if (GUILayout.Button(new GUIContent("Export Texture", "Exports a texture containing the palette colors."), GUILayout.Width(width))) {
                    ExportTexture();
                }
                EditorGUILayout.EndHorizontal();

            }

            if (viewMode == ViewMode.Help) {
                GUILayout.Label("Q U I C K   H E L P");

                EditorGUILayout.BeginVertical(GUI.skin.box);
                var headerStyle = new GUIStyle(EditorStyles.boldLabel);
                headerStyle.richText = true;
                var textStyle = new GUIStyle(EditorStyles.label);
                textStyle.wordWrap = true;
                GUILayout.Label("<size=24>Color Studio</size>\n© 2019-2021 Kronnect", headerStyle);
                GUILayout.Label("For support and questions please visit kronnect.com forum.\nIf you like Color Studio, please rate it on the Asset Store.", textStyle);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginVertical(GUI.skin.box);
                var titleStyle = new GUIStyle(EditorStyles.boldLabel);
                titleStyle.richText = true;
                GUILayout.Label("<size=12>Quick Interface Description</size>", titleStyle);
                var sectionStyle = new GUIStyle(EditorStyles.boldLabel);
                sectionStyle.normal.textColor = new Color(1f, 1f, 0.5f, 0.9f);
                GUILayout.Label("Color Wheel Tab", sectionStyle);
                GUILayout.Label("■ Use predefined color schemes to quickly generate palettes.\n■ Click several times on the scheme button to generate random combinations.\n■ The black dot represents the primary color.\n■ White dots are complementary colors.\n■ Drag the dots around the color wheel to customize your palette.\n■ Add additional colors holding CONTROL key and clicking on the color wheel.\n■ Remove additional colors double-clicking on the dots.", textStyle);
                EditorGUILayout.Separator();
                GUILayout.Label("Palette Tab", sectionStyle);
                GUILayout.Label("■ Expanded view of your current palette.\n■ Customize palette adjusting hue count, shades, brightness and color temperature (kelvin).\n■ Add/Remove custom colors.\n■ Load/save palettes and export to LUT and C# code.\n■ Import ASE palette files.", textStyle);
                EditorGUILayout.Separator();
                GUILayout.Label("Compact View Tab", sectionStyle);
                GUILayout.Label("■ Tiny view of your current palette. Useful to keep your palette visible along other tabs.", textStyle);
                EditorGUILayout.Separator();
                GUILayout.Label("Project Palettes Tab", sectionStyle);
                GUILayout.Label("■ Shows and manage existing palettes in the entire project.\n■ Quickly load a palette clicking on the name.\n■ Locate, duplicate or remove any palette.", textStyle);
                EditorGUILayout.Separator();
                GUILayout.Label("Coloring At Runtime", sectionStyle);
                GUILayout.Label("■ Add script Recolor to any GameObject or Sprite.", textStyle);
                EditorGUILayout.Separator();
                EditorGUILayout.Separator();
                GUILayout.Label(kronnect);
                EditorGUILayout.EndHorizontal();
            }


            EditorGUILayout.EndScrollView();

            if (Event.current.type == EventType.MouseUp) {
                mouseDown = false;
                selectedKey = -1;
                // Clear cursor
                for (int k = 0; k < palette.keyColors.Length; k++) {
                    palette.keyColors[k].highlighted = false;
                }
                UpdateCWMaterial();
            }

            if (paletteChanges) {
                SetColorKeys();
            }

            if (issueRepaint) {
                Repaint();
            }
        }

        void Update() {
            if (cwMat != null) {
                float t = (float)((EditorApplication.timeSinceStartup - startAnimation) / 0.4);
                t = Mathf.Clamp01(t);
                cwMat.SetFloat("_AnimTime", t);
            }
            if (viewMode == ViewMode.ColorWheel) {
                Repaint();
            }

        }

        void UpdateCWMaterial() {
            string[] keywords;

            if (cwMat == null) {
                cwMat = new Material(Shader.Find("Color Studio/ColorWheel"));
            }
            switch ((ColorScheme)palette.scheme) {
                case ColorScheme.Complementary:
                case ColorScheme.Gradient:
                    keywords = new string[] { "CW_COMPLEMENTARY" };
                    break;
                case ColorScheme.SplitComplementary:
                case ColorScheme.Triadic:
                    keywords = new string[] { "CW_SPLIT_COMPLEMENTARY" };
                    break;
                case ColorScheme.Analogous:
                    keywords = new string[] { "CW_ANALOGOUS" };
                    break;
                case ColorScheme.Tetradic:
                case ColorScheme.Square:
                case ColorScheme.AccentedAnalogous:
                    keywords = new string[] { "CW_TETRADIC" };
                    break;
                case ColorScheme.Custom:
                    keywords = new string[] { "CW_CUSTOM" };
                    break;
                default:
                    keywords = null;
                    break;
            }
            cwMat.shaderKeywords = keywords;
            cwMat.SetFloat("_CenterWidth", CENTER_WIDTH);
            cwMat.SetFloat("_Saturation", palette.saturation);
            cwMat.SetFloat("_MinBrightness", palette.minBrightness);
            cwMat.SetFloat("_MaxBrightness", palette.maxBrightness);

            int customColorsCount = 0;
            for (int k = 0; k < palette.keyColors.Length; k++) {
                KeyColor c = palette.keyColors[k];
                if (c.visible) {
                    cwCustomColors[customColorsCount].x = c.pos.x;
                    cwCustomColors[customColorsCount].y = c.pos.y;
                    cwCustomColors[customColorsCount].z = c.type.dotColor() + (c.highlighted ? 256 : 0);
                    cwCustomColors[customColorsCount].w = c.hue;
                    customColorsCount++;
                }
            }
            cwMat.SetVectorArray("_KeyColorsData", cwCustomColors);
            cwMat.SetInt("_KeyColorsCount", customColorsCount);
        }

        bool SelectPrimaryInput() {
            float hue = 0;
            switch (primaryInputMode) {
                case ColorEncoding.RGB01: {
                        float[] rgb = StringTo3Floats(primaryInput);
                        if (rgb == null) return false;
                        hue = ColorConversion.GetHue(rgb[0], rgb[1], rgb[2]);
                        break;
                    }
                case ColorEncoding.RGB255: {
                        float[] rgb = StringTo3Floats(primaryInput);
                        if (rgb == null) return false;
                        hue = ColorConversion.GetHue(rgb[0] / 255f, rgb[1] / 255f, rgb[2] / 255f);
                        break;
                    }
                case ColorEncoding.HSL: {
                        float[] hsl = StringTo3Floats(primaryInput);
                        if (hsl == null || hsl.Length == 0) return false;
                        hue = hsl[0];
                        break;
                    }
                case ColorEncoding.HEX: {
                        Color color;
                        if (!ColorConversion.GetColorFromHex(primaryInput, out color)) return false;
                        hue = ColorConversion.GetHue(color.r, color.g, color.b);
                        break;
                    }
            }
            palette.saturation = 1f;
            palette.primaryAngle = hue * PI2;
            clickedAngle = hue;
            return true;

        }

        void UpdatePrimaryInput() {
            Color color = palette.primaryColor;
            switch (primaryInputMode) {
                case ColorEncoding.RGB01:
                    primaryInput = color.r.ToString("F3", CultureInfo.InvariantCulture) + ", " + color.g.ToString("F3", CultureInfo.InvariantCulture) + ", " + color.b.ToString("F3", CultureInfo.InvariantCulture);
                    break;
                case ColorEncoding.RGB255:
                    primaryInput = ((int)(color.r * 255)).ToString(CultureInfo.InvariantCulture) + ", " + ((int)(color.g * 255)).ToString(CultureInfo.InvariantCulture) + ", " + ((int)(color.b * 255)).ToString(CultureInfo.InvariantCulture);
                    break;
                case ColorEncoding.HEX:
                    primaryInput = "#" + ((int)(color.r * 255)).ToString("X2") + ((int)(color.g * 255)).ToString("X2") + ((int)(color.b * 255)).ToString("X2");
                    break;
                case ColorEncoding.HSL:
                    float hue = ColorConversion.GetHue(color.r, color.g, color.b);
                    primaryInput = hue.ToString("F3", CultureInfo.InvariantCulture) + ", " + palette.saturation.ToString("F3", CultureInfo.InvariantCulture) + ", 1.0";
                    break;
            }
        }

        void UpdateSelectionInput() {
            Color color = selectionColor;
            switch (selectionInputMode) {
                case ColorEncoding.RGB01:
                    selectionInput = color.r.ToString("F3", CultureInfo.InvariantCulture) + ", " + color.g.ToString("F3", CultureInfo.InvariantCulture) + ", " + color.b.ToString("F3", CultureInfo.InvariantCulture);
                    break;
                case ColorEncoding.RGB255:
                    selectionInput = ((int)(color.r * 255)).ToString(CultureInfo.InvariantCulture) + ", " + ((int)(color.g * 255)).ToString(CultureInfo.InvariantCulture) + ", " + ((int)(color.b * 255)).ToString(CultureInfo.InvariantCulture);
                    break;
                case ColorEncoding.HEX:
                    selectionInput = "#" + ((int)(color.r * 255)).ToString("X2") + ((int)(color.g * 255)).ToString("X2") + ((int)(color.b * 255)).ToString("X2");
                    break;
                case ColorEncoding.HSL:
                    float hue = ColorConversion.GetHue(color.r, color.g, color.b);
                    selectionInput = hue.ToString("F3", CultureInfo.InvariantCulture) + ", " + palette.saturation.ToString("F3", CultureInfo.InvariantCulture) + ", " + selectionLightness.ToString("F3", CultureInfo.InvariantCulture);
                    break;
            }
        }

        Vector2 GetCWPos(float angle) {
            Vector2 pos = new Vector3();
            pos.x = Mathf.Cos(angle);
            pos.y = Mathf.Sin(angle);
            float d = 0.25f + CENTER_WIDTH * 0.5f;
            pos.x = 0.5f - pos.x * d;
            pos.y = 0.5f - pos.y * d;
            return pos;
        }

        void SetColorKeys() {
            SetColorKeys(palette);
        }


        void SetColorKeys(CSPalette palette) {
            selectionPos.x = -1;
            selectionInput = "";
            selectedCustomColor = -1;
            selectionColor.a = 0;
            startAnimation = (float)EditorApplication.timeSinceStartup;

            // Set primary color
            for (int k = 0; k < CSPalette.START_INDEX_CUSTOM_COLOR; k++) {
                palette.keyColors[k].visible = false;
            }

            if (palette.scheme != ColorScheme.Spectrum && palette.scheme != ColorScheme.Custom) {
                SetKey(0, KeyColorType.Primary, palette.primaryAngle);
            } else {
                cwMat.SetVector("_Key0Pos", Vector3.zero);
            }

            switch (palette.scheme) {
                case ColorScheme.Monochromatic:
                case ColorScheme.Custom:
                    break;
                case ColorScheme.Complementary: {
                        SetKey(1, KeyColorType.Complementary, palette.primaryAngle + Mathf.PI);
                    }
                    break;
                case ColorScheme.Gradient: {
                        SetKey(1, KeyColorType.Complementary, palette.splitAmount + Mathf.PI);
                    }
                    break;
                case ColorScheme.SplitComplementary: {
                        SetKey(1, KeyColorType.Complementary, palette.primaryAngle + palette.splitAmount + Mathf.PI);
                        SetKey(2, KeyColorType.Complementary, palette.primaryAngle - palette.splitAmount + Mathf.PI);
                    }
                    break;
                case ColorScheme.Analogous: {
                        SetKey(1, KeyColorType.Complementary, palette.primaryAngle + palette.splitAmount);
                        SetKey(2, KeyColorType.Complementary, palette.primaryAngle - palette.splitAmount);
                    }
                    break;
                case ColorScheme.Triadic: {
                        SetKey(1, KeyColorType.Complementary, palette.primaryAngle + Mathf.PI * 2f / 3f);
                        SetKey(2, KeyColorType.Complementary, palette.primaryAngle - Mathf.PI * 2f / 3f);
                    }
                    break;
                case ColorScheme.Tetradic: {
                        SetKey(1, KeyColorType.Complementary, palette.primaryAngle + palette.splitAmount);
                        SetKey(2, KeyColorType.Complementary, palette.primaryAngle + Mathf.PI);
                        SetKey(3, KeyColorType.Complementary, palette.primaryAngle + palette.splitAmount + Mathf.PI);
                    }
                    break;
                case ColorScheme.Square: {
                        SetKey(1, KeyColorType.Complementary, palette.primaryAngle + HALF_PI);
                        SetKey(2, KeyColorType.Complementary, palette.primaryAngle + Mathf.PI);
                        SetKey(3, KeyColorType.Complementary, palette.primaryAngle + HALF_PI + Mathf.PI);
                    }
                    break;
                case ColorScheme.AccentedAnalogous: {
                        SetKey(1, KeyColorType.Complementary, palette.primaryAngle + palette.splitAmount);
                        SetKey(2, KeyColorType.Complementary, palette.primaryAngle + Mathf.PI);
                        SetKey(3, KeyColorType.Complementary, palette.primaryAngle - palette.splitAmount);
                    }
                    break;
            }

            UpdateCWMaterial();
            palette.BuildHueColors();
        }

        void AddCustomColor(float hue) {
            for (int k = CSPalette.START_INDEX_CUSTOM_COLOR; k < palette.keyColors.Length; k++) {
                if (!palette.keyColors[k].visible) {
                    SetKey(k, KeyColorType.Custom, hue);
                    break;
                }
            }
            UpdateCWMaterial();
            palette.BuildHueColors();
        }

        void AddCustomColor(Color color) {
            for (int k = CSPalette.START_INDEX_CUSTOM_COLOR; k < palette.keyColors.Length; k++) {
                if (!palette.keyColors[k].visible) {
                    SetKey(k, KeyColorType.Custom, color);
                    break;
                }
            }
            UpdateCWMaterial();
            palette.BuildHueColors();
        }


        void SetKey(int index, KeyColorType keyColorType, float angle) {
            Vector2 prevPos = palette.keyColors[index].pos;
            float prevHue = palette.keyColors[index].hue;
            float hue = ((angle % PI2 + PI2) % PI2) / PI2;
            Vector2 pos = GetCWPos(angle);
            palette.keyColors[index].angle = angle;
            palette.keyColors[index].pos = pos;
            palette.keyColors[index].hue = hue;
            palette.keyColors[index].type = keyColorType;
            palette.keyColors[index].color = ColorConversion.GetColorFromHSL(hue, palette.saturation, 0.5f);
            palette.keyColors[index].visible = true;

            if (prevPos.x == 0) {
                prevPos = pos;
                prevHue = hue;
            }
            cwMat.SetVector("_Key" + index + "PosPrev", prevPos);
            cwMat.SetFloat("_Key" + index + "HuePrev", prevHue);
            cwMat.SetVector("_Key" + index + "Pos", pos);
            cwMat.SetFloat("_Key" + index + "Hue", hue);
        }


        void SetKey(int index, KeyColorType keyColorType, Color color) {
            Vector2 prevPos = palette.keyColors[index].pos;
            float prevHue = palette.keyColors[index].hue;
            float hue = ColorConversion.GetHue(color.r, color.g, color.b);
            float angle = hue * PI2;
            Vector2 pos = GetCWPos(angle);
            palette.keyColors[index].angle = angle;
            palette.keyColors[index].pos = pos;
            palette.keyColors[index].hue = hue;
            palette.keyColors[index].type = keyColorType;
            palette.keyColors[index].color = color;
            palette.keyColors[index].visible = true;

            if (prevPos.x == 0) {
                prevPos = pos;
                prevHue = hue;
            }
            cwMat.SetVector("_Key" + index + "PosPrev", prevPos);
            cwMat.SetFloat("_Key" + index + "HuePrev", prevHue);
            cwMat.SetVector("_Key" + index + "Pos", pos);
            cwMat.SetFloat("_Key" + index + "Hue", hue);
        }


        void ClearCustomColors() {
            for (int k = CSPalette.START_INDEX_CUSTOM_COLOR; k < palette.keyColors.Length; k++) {
                palette.keyColors[k].visible = false;
            }
            UpdateCWMaterial();
            palette.BuildHueColors();
        }


        string GetExportsPath(string subfolder) {
            string path = "Assets/Color Studio/" + subfolder;
            Directory.CreateDirectory(path);
            return path;
        }

        void FindProjectPalettes() {
            string[] guids = AssetDatabase.FindAssets("t:" + typeof(CSPalette).Name);
            List<CSPalette> found = new List<CSPalette>();
            projectPalettes = new CSPalette[guids.Length];
            for (int i = 0; i < guids.Length; i++) {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CSPalette pal = projectPalettes[i] = AssetDatabase.LoadAssetAtPath<CSPalette>(path);
                if (pal == null || pal == palette) continue;
                if (pal.material == null) {
                    pal.UpdateMaterial();
                }
                pal.material = Instantiate<Material>(palette.material);
                pal.BuildHueColors();
                found.Add(pal);
            }
            projectPalettes = found.ToArray();
            ResortProjectPalettes();
        }

        void ResortProjectPalettes() {
            System.Array.Sort(projectPalettes, comparer);
        }

        int comparer(CSPalette a, CSPalette b) {
            return a.order.CompareTo(b.order);
        }

        void DrawConversionTools() {
            GUILayout.Label("C O N V E R T   C O L O R S");

            GameObject prev = selectedObject;
            selectedObject = (GameObject)EditorGUILayout.ObjectField("GameObject", selectedObject, typeof(GameObject), true);
            if (selectedObject != prev) {
                SelectedObjectChanged();
                GUIUtility.ExitGUI();
            }
            Color prevColor = selectedObjectColor;
            selectedObjectColor = EditorGUILayout.ColorField("Color", selectedObjectColor);
            if (nearestColor.a > 0) {
                EditorGUILayout.ColorField("Suggested", nearestColor);
            }
            if (selectedObjectColor != prevColor) {
                nearestColor = palette.GetNearestColor(selectedObjectColor, colorMatchMode);
            }
            EditorGUILayout.Separator();
            Texture2D prevTexture = referenceTexture;
            referenceTexture = (Texture2D)EditorGUILayout.ObjectField("Texture", referenceTexture, typeof(Texture2D), false);
            if (referenceTexture != prevTexture && referenceTexture != null) {
                nearestTexture = palette.GetNearestTexture(referenceTexture, colorMatchMode);
            }
            if (nearestTexture != null) {
                EditorGUILayout.ObjectField(new GUIContent("Suggested"), nearestTexture, typeof(Texture2D), false);
                if (GUILayout.Button(new GUIContent("Save Suggested Texture", "Exports suggested texture to disk."))) {
                    ExportColoredTexture();
                }
            }

            EditorGUILayout.Separator();
            colorMatchMode = (ColorMatchMode)EditorGUILayout.EnumPopup("Match Mode", colorMatchMode);

            EditorGUILayout.Separator();
            EditorGUILayout.HelpBox("To change colors of an object at runtime, add the 'Recolor' script to it instead.", MessageType.Info);

            EditorGUILayout.Separator();
            GUILayout.Label("C U R R E N T   P A L E T T E");
            palette.material.SetVector("_CursorPos", Vector3.left);


        }

        void SelectedObjectChanged() {
            referenceTexture = null;
            nearestTexture = null;
            nearestColor = Color.white;
            if (selectedObject != null) {
                Renderer r = selectedObject.GetComponent<Renderer>();
                if (r != null) {
                    Material mat = r.sharedMaterial;
                    if (mat != null) {
                        if (mat.HasProperty("_Color")) {
                            selectedObjectColor = mat.color;
                            nearestColor = palette.GetNearestColor(selectedObjectColor, colorMatchMode);
                        }
                        if (r is SpriteRenderer) {
                            SpriteRenderer spr = (SpriteRenderer)r;
                            if (spr.sprite != null && spr.sprite.texture != null) {
                                referenceTexture = spr.sprite.texture;
                                nearestTexture = palette.GetNearestTexture(referenceTexture, colorMatchMode);
                            }
                        } else if (mat.HasProperty("_MainTex")) {
                            // Ensure texture is readable
                            if (mat.mainTexture is Texture2D) {
                                referenceTexture = (Texture2D)mat.mainTexture;
                                nearestTexture = palette.GetNearestTexture(referenceTexture, colorMatchMode);
                            }
                        }
                    }
                }
            }
        }

        float[] StringTo3Floats(string s) {
            float[] xyz = new float[3];
            try {
                string[] values = s.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (values.Length >= 3) {
                    if (float.TryParse(values[0], NumberStyles.Any, CultureInfo.InvariantCulture, out xyz[0]) && float.TryParse(values[1], NumberStyles.Any, CultureInfo.InvariantCulture, out xyz[1]) && float.TryParse(values[2], NumberStyles.Any, CultureInfo.InvariantCulture, out xyz[2])) {
                        return xyz;
                    }
                }
            } catch {
            }
            return null;
        }


        void AddSelectionInput() {
            switch (selectionInputMode) {
                case ColorEncoding.RGB01: AddRGB01(); break;
                case ColorEncoding.RGB255: AddRGB(); break;
                case ColorEncoding.HSL: AddHSL(); break;
                case ColorEncoding.HEX: AddHEX(); break;
            }
        }

        void AddRGB01() {
            float[] rgb = StringTo3Floats(selectionInput);
            if (rgb != null) {
                AddCustomColor(new Color(rgb[0], rgb[1], rgb[2]));
            }
        }

        void AddRGB() {
            float[] rgb = StringTo3Floats(selectionInput);
            if (rgb != null) {
                AddCustomColor(new Color(rgb[0] / 255, rgb[1] / 255, rgb[2] / 255));
            }
        }

        void AddHSL() {
            float[] hsl = StringTo3Floats(selectionInput);
            if (hsl != null) {
                Color color = ColorConversion.GetColorFromHSL(hsl[0], hsl[1], hsl[2]);
                AddCustomColor(color);
            }
        }

        void AddHEX() {
            Color color;
            if (ColorConversion.GetColorFromHex(selectionInput, out color)) {
                AddCustomColor(color);
            }
        }

        void DeleteColor() {
            if (selectedCustomColor >= 0) {
                palette.DeleteKeyColor(selectedCustomColor);
                selectedCustomColor = -1;
                SetColorKeys();
            }

        }

    }

}

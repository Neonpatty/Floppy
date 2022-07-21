/* Pixel Painter by Ramiro Oliva (Kronnect)   /
/  Premium assets for Unity on kronnect.com */

using UnityEngine;
using UnityEditor;

namespace ColorStudio {

    public static class BrushExtensions {
        public static bool usesBrush(this PPWindow.Brush tool) {
            return tool != PPWindow.Brush.Replace && tool != PPWindow.Brush.FloodFill;
        }

        public static string opDescription(this PPWindow.Brush tool) {
            switch (tool) {
                case PPWindow.Brush.Eraser: return "Erase pixel";
                case PPWindow.Brush.Darken: return "Darken";
                case PPWindow.Brush.Lighten: return "Lighten";
                case PPWindow.Brush.Dry: return "Dry";
                case PPWindow.Brush.Vivid: return "Vivid";
                case PPWindow.Brush.Noise: return "Noise";
                case PPWindow.Brush.NoiseTone: return "Noise Tone";
                case PPWindow.Brush.Gradient: return "Gradient";
                case PPWindow.Brush.FloodFill: return "Flood Fill";
            }
            return "Paint pixel";
        }
    }

    public partial class PPWindow : EditorWindow {

        public enum Brush {
            Pen = 0,
            Eraser = 1,
            Replace = 2,
            FloodFill = 3,
            Darken = 4,
            Lighten = 5,
            Dry = 6,
            Vivid = 7,
            Noise = 8,
            NoiseTone = 9,
            Gradient = 10
        }

        enum MirrorMode {
            None = 0,
            Horizontal = 1,
            Vertical = 2,
            Quad = 3
        }

        enum Zoom {
            [InspectorName("x1")]
            _100 = 100,
            [InspectorName("x2")]
            _200 = 200,
            [InspectorName("x4")]
            _400 = 400,
            [InspectorName("x8")]
            _800 = 800,
            [InspectorName("x16")]
            _1600 = 1600
        }


        const string PREFS_SETTINGS = "Pixel Painter Settings";
        const int ICON_COLOR_PICKER = 0;
        const int ICON_SAVE_TEXTURE = 1;
        const int ICON_SAVE_SPRITE = 2;
        const int ICON_TRANSPARENT = 3;
        const int ICON_UNDO = 4;
        const int ICON_REDO = 5;
        const int ICON_FLIP_VERT = 6;
        const int ICON_FLIP_HORIZ = 7;
        const int ICON_DISP_LEFT = 8;
        const int ICON_DISP_RIGHT = 9;
        const int ICON_DISP_UP = 10;
        const int ICON_DISP_DOWN = 11;
        const int ICON_ROTATE_LEFT = 12;
        const int ICON_ROTATE_RIGHT = 13;

        const int BRUSH_SHAPE_SOLID = 0;
        const int BRUSH_SHAPE_CIRCLE = 1;
        const int BRUSH_SHAPE_DITHER_1 = 2;
        const int BRUSH_SHAPE_DITHER_2 = 3;
        const int BRUSH_SHAPE_CROSS = 4;
        const int BRUSH_SHAPE_X = 5;

        [SerializeField] int width = 16;
        [SerializeField] int height = 16;
        [SerializeField] Texture2D texture;
        [SerializeField] Sprite sprite;
        [SerializeField] Texture2D canvasTexture;
        [SerializeField] Color[] colors;
        [SerializeField] Color _brushColor, lastDrawingColor;
        [SerializeField] Brush currentBrush;
        [SerializeField] Zoom currentZoom = Zoom._100;
        [SerializeField] Vector2 panningPos;
        [SerializeField] Vector2Int panningPosTexel;
        [SerializeField] int _brushWidth;
        [SerializeField] int _brushShape = BRUSH_SHAPE_SOLID;
        [SerializeField] MirrorMode _mirrorMode;
        [SerializeField] bool _showGrid = true;

        Material canvasMaterial, previewMaterial;
        Texture2D colorsFromTexture;
        Vector2Int currentTexelPos, lastPaintedTexelPos, startTexelPos;
        Texture2D brushColorTexture;
        Color[] brushColorTexColors;
        bool isCursorOnTexture, isBrushing;
        GUIContent[] brushIcons, brushWidths, brushShapes, mirrorModes;
        GUIContent[] icons;
        int customWidth, customHeight;
        bool canShowGrid = true;

        Color brushColor {
            get { return _brushColor; }
            set {
                if (_brushColor != value) {
                    _brushColor = value;
                    lastDrawingColor = _brushColor;
                    UpdateBrushColorTex();
                }
            }
        }

        int brushPixelWidth { get { return _brushWidth + 1; } }

        public static PPWindow ShowWindow() {
            Vector3 size = new Vector3(400, 600);
            Vector3 position = new Vector3(Screen.width / 2 - size.x / 2, Screen.height / 2 - size.y / 2);
            Rect rect = new Rect(position, size);
            PPWindow window = GetWindowWithRect<PPWindow>(rect, false, "Pixel Painter", true);
            window.minSize = new Vector2(100, 100);
            window.maxSize = new Vector2(2000, 2000);
            return window;
        }


        void OnEnable() {
            if (canvasTexture == null) {
                string data = EditorPrefs.GetString(PREFS_SETTINGS, JsonUtility.ToJson(this, false));
                if (!string.IsNullOrEmpty(data)) {
                    JsonUtility.FromJsonOverwrite(data, this);
                }
                LoadFile();
            }
            brushIcons = new GUIContent[] {
                new GUIContent("Pen", "Paints a single pixel"),
                new GUIContent("Eraser", "Paints with transparent color"),
                new GUIContent("Replace", "Replaces all pixels of same color"),
                new GUIContent("Flood Fill", "Fills a contiguous area with same color"),
                new GUIContent("Darken", "Makes pixels darker"),
                new GUIContent("Lighten", "Makes pixels brighter"),
                new GUIContent("Dry", "Makes pixels less saturared"),
                new GUIContent("Vivid", "Makes pixels more colorful"),
                new GUIContent("Noise", "Randomly makes pixels darker or brighter"),
                new GUIContent("Noise Tone", "Randomly changes pixel tone"),
                new GUIContent("Gradient", "Transition to darker colors as you paint pixels")
            };
            brushWidths = new GUIContent[] {
                new GUIContent("1"),
                new GUIContent("2"),
                new GUIContent("3"),
                new GUIContent("4"),
                new GUIContent("5") };
            string iconsPath = "Color Studio/Icons/";
            brushShapes = new GUIContent[] {
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "shape_solid"), "Solid shape"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "shape_circle"), "Circle shape"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "shape_dither1"), "Dither 1 shape"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "shape_dither2"), "Dither 2 shape"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "shape_cross"), "Cross hape"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "shape_x"), "X shape")
                };
            icons = new GUIContent[] {
                new GUIContent("", "Open Color Studio Palette Manager. You can also right-click on any pixel to pick that color"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "save"), "Replaces or create new texture file"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "save"), "Replaces or create new sprite file"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "transparent"), "Selects transparent color. You can also right-click on any pixel to pick that color"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "undo"), "Undo last texture operation"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "redo"), "Redo last texture operation"),

                new GUIContent(Resources.Load<Texture2D>(iconsPath + "cmd_flip_vert"), "Flip image vertically"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "cmd_flip_horiz"), "Flip image horizontally"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "cmd_disp_left"), "Displace image to the left"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "cmd_disp_right"), "Displace image to the right"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "cmd_disp_up"), "Displace image up"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "cmd_disp_down"), "Displace image down"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "cmd_rot_left"), "Rotate image 90º CCW"),
                new GUIContent(Resources.Load<Texture2D>(iconsPath + "cmd_rot_right"), "Rotate image 90º CW")
        };
            mirrorModes = new GUIContent[] {
                new GUIContent("None", "No mirroring"),
                new GUIContent("Horiz", "Mirror horizontally"),
                new GUIContent("Vertical", "Mirror vertically"),
                new GUIContent("Quad", "Mirror both horizontal and vertically")
            };
            currentTexelPos = new Vector2Int();
            UpdateBrushColorTex();
            Undo.undoRedoPerformed += MyUndoCallback;
            CSWindow.onColorChange += MyColorChangeCallback;
        }


        void MyColorChangeCallback() {
            brushColor = CSWindow.currentPrimaryColor;
        }

        void MyUndoCallback() {
            if (canvasTexture != null && colors != null) {
                canvasTexture.SetPixels(colors);
                canvasTexture.Apply();
            }
        }

        void OnDisable() {
            CSWindow.onColorChange -= MyColorChangeCallback;
            Undo.undoRedoPerformed -= MyUndoCallback;
        }

        void OnDestroy() {
            canvasTexture = null;
            string data = JsonUtility.ToJson(this, false);
            EditorPrefs.SetString(PREFS_SETTINGS, data);
        }

        void CheckMaterials() {
            if (canvasTexture == null) {
                CreateNew(width, height);
            }
            ReadColors();

            if (canvasMaterial == null) {
                canvasMaterial = new Material(Shader.Find("Color Studio/PixelPainterCanvas"));
                UpdateCanvasMaterialProperties();
            }
            if (previewMaterial == null) {
                previewMaterial = new Material(Shader.Find("Color Studio/PixelPainterPreview"));
            }
            if (brushColorTexture == null) {
                UpdateBrushColorTex();
            }
        }

        void UpdateBrushColorTex() {
            if (brushColorTexture == null) {
                brushColorTexture = new Texture2D(1, 1, TextureFormat.ARGB32, false);
            }
            if (brushColorTexColors == null || brushColorTexColors.Length != 1) {
                brushColorTexColors = new Color[1];
            }
            brushColorTexColors[0] = _brushColor;
            brushColorTexture.SetPixels(brushColorTexColors);
            brushColorTexture.Apply();
        }

        Vector2 scrollPos;
        void OnGUI() {

            CheckMaterials();

            scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
            DrawTopRowButtons();
            DrawCanvasArea();
            EditorGUILayout.EndScrollView();
            DrawBottomRowButtons();


            if (Event.current.type == EventType.Repaint) {
                if (isBrushing && currentTexelPos != lastPaintedTexelPos) {
                    lastPaintedTexelPos = currentTexelPos;
                    PaintPixel();
                }
                Repaint();
            }
        }

        void DrawTopRowButtons() {

            EditorGUILayout.BeginVertical();

            Rect space = EditorGUILayout.BeginHorizontal();
            float width = EditorGUIUtility.currentViewWidth;

            const int w = 32;
            const int h = 32;

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("New");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("16", GUILayout.Width(w), GUILayout.Height(h))) CreateNew(16, 16);
            if (GUILayout.Button("32", GUILayout.Width(w), GUILayout.Height(h))) CreateNew(32, 32);
            if (GUILayout.Button("64", GUILayout.Width(w), GUILayout.Height(h))) CreateNew(64, 64);
            if (GUILayout.Button("128", GUILayout.Width(w), GUILayout.Height(h))) CreateNew(128, 128);
            if (GUILayout.Button("256", GUILayout.Width(w), GUILayout.Height(h))) CreateNew(256, 256);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Texture");
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            texture = (Texture2D)EditorGUILayout.ObjectField(GUIContent.none, texture, typeof(Texture2D), false, GUILayout.Width(w), GUILayout.Height(h));
            if (EditorGUI.EndChangeCheck()) {
                sprite = null;
                LoadFile();
            }
            GUI.enabled = sprite == null;
            if (GUILayout.Button(icons[ICON_SAVE_TEXTURE], GUILayout.Width(w), GUILayout.Height(h))) {
                SaveTexture(TextureImporterType.Default);
                GUIUtility.ExitGUI();
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Sprite");
            EditorGUILayout.BeginHorizontal();
            EditorGUI.BeginChangeCheck();
            sprite = (Sprite)EditorGUILayout.ObjectField(GUIContent.none, sprite, typeof(Sprite), false, GUILayout.Width(w), GUILayout.Height(h));
            if (EditorGUI.EndChangeCheck()) {
                texture = null;
                LoadFile();
            }
            GUI.enabled = texture == null;
            if (GUILayout.Button(icons[ICON_SAVE_SPRITE], GUILayout.Width(w), GUILayout.Height(h))) {
                SaveTexture(TextureImporterType.Sprite);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            Color prevContentColor = GUI.contentColor;
            if (!EditorGUIUtility.isProSkin) {
                GUI.contentColor = Color.black;
            }
            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Undo / Redo");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(icons[ICON_UNDO], GUILayout.Width(w), GUILayout.Height(h))) EditorApplication.delayCall += () => Undo.PerformUndo();
            if (GUILayout.Button(icons[ICON_REDO], GUILayout.Width(w), GUILayout.Height(h))) EditorApplication.delayCall += () => Undo.PerformRedo();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();


            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Brush Color");
            space = EditorGUILayout.BeginHorizontal();
            space.xMin += 5;
            space.yMin += 5;
            space.height -= 5;
            space.width = space.height;
            if (GUILayout.Button(icons[ICON_COLOR_PICKER], GUILayout.Width(w), GUILayout.Height(h))) CSWindow.ShowWindow(1);
            GUI.DrawTexture(space, brushColorTexture, ScaleMode.StretchToFill);
            if (GUILayout.Button(icons[ICON_TRANSPARENT], GUILayout.Width(w), GUILayout.Height(h))) brushColor = new Color(0, 0, 0, 0);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            int mw = 32 * brushWidths.Length;
            GUILayout.Label("Brush Width", GUILayout.MaxWidth(mw));
            _brushWidth = GUILayout.SelectionGrid(_brushWidth, brushWidths, brushWidths.Length, GUILayout.MaxWidth(mw), GUILayout.Height(h));
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            mw = 32 * brushShapes.Length;
            GUILayout.Label("Brush Shape", GUILayout.MaxWidth(mw));
            _brushShape = GUILayout.SelectionGrid(_brushShape, brushShapes, brushShapes.Length, GUILayout.MaxWidth(mw), GUILayout.Height(h));
            EditorGUILayout.EndVertical();

            GUI.contentColor = prevContentColor;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal(); // Row


            EditorGUILayout.EndVertical(); // top buttons
        }

        void DrawCanvasArea() {

            EditorGUILayout.BeginHorizontal();

            // First column
            const int cw = 90;
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(cw));
            // Paint brushes
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Brushes", GUILayout.MaxWidth(cw));
            EditorGUI.BeginChangeCheck();
            currentBrush = (Brush)GUILayout.SelectionGrid((int)currentBrush, brushIcons, 1, GUILayout.MaxWidth(cw), GUILayout.Height(32 * brushIcons.Length));
            if (EditorGUI.EndChangeCheck()) {
                SelectBrush();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();

            // Mirror
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Mirror", GUILayout.MaxWidth(cw));
            _mirrorMode = (MirrorMode)GUILayout.SelectionGrid((int)_mirrorMode, mirrorModes, 1, GUILayout.MaxWidth(cw), GUILayout.Height(32 * mirrorModes.Length));
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();


            // Column: Commands
            // Tools

            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(cw));
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Canvas", GUILayout.MaxWidth(cw));

            EditorGUIUtility.labelWidth = 50;
            customWidth = EditorGUILayout.IntField("Width", customWidth, GUILayout.MaxWidth(cw));
            customHeight = EditorGUILayout.IntField("Height", customHeight, GUILayout.MaxWidth(cw));
            if (customWidth <= 0) customWidth = width;
            if (customHeight <= 0) customHeight = height;
            GUI.enabled = customWidth != width || customHeight != height;
            if (GUILayout.Button("Set Size", GUILayout.Height(32))) {
                if (EditorUtility.DisplayDialog("Set Canvas Size", "Change canvas size to " + customWidth + "x" + customHeight + "?", "Yes", "No")) {
                    SetSize(customWidth, customHeight);
                }
            }
            GUI.enabled = true;

            EditorGUILayout.Separator();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear", GUILayout.Height(32))) ClearAll();
            if (GUILayout.Button("Fill", GUILayout.Height(32))) FillAll();
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("Fit Palette", GUILayout.Height(32))) {
                FitPalette();
            }

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.padding = new RectOffset(6, 6, 6, 6);

            const int hw = cw / 2;
            Color prevContentColor = GUI.contentColor;
            if (!EditorGUIUtility.isProSkin) {
                GUI.contentColor = Color.black;
            }
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(icons[ICON_FLIP_VERT], buttonStyle, GUILayout.MaxWidth(hw), GUILayout.Height(32))) FlipVert();
            if (GUILayout.Button(icons[ICON_FLIP_HORIZ], buttonStyle, GUILayout.MaxWidth(hw), GUILayout.Height(32))) FlipHoriz();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(icons[ICON_DISP_LEFT], buttonStyle, GUILayout.MaxWidth(hw), GUILayout.Height(32))) Displace(-1, 0);
            if (GUILayout.Button(icons[ICON_DISP_RIGHT], buttonStyle, GUILayout.MaxWidth(hw), GUILayout.Height(32))) Displace(1, 0);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(icons[ICON_DISP_UP], buttonStyle, GUILayout.MaxWidth(hw), GUILayout.Height(32))) Displace(0, 1);
            if (GUILayout.Button(icons[ICON_DISP_DOWN], buttonStyle, GUILayout.MaxWidth(hw), GUILayout.Height(32))) Displace(0, -1);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(icons[ICON_ROTATE_LEFT], buttonStyle, GUILayout.MaxWidth(hw), GUILayout.Height(32))) RotateLeft();
            if (GUILayout.Button(icons[ICON_ROTATE_RIGHT], buttonStyle, GUILayout.MaxWidth(hw), GUILayout.Height(32))) RotateRight();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Separator();

            EditorGUI.BeginChangeCheck();

            _showGrid = GUILayout.Toggle(_showGrid, "Toggle Grid", "Button", GUILayout.Height(32));
            EditorGUIUtility.labelWidth = 40;
            currentZoom = (Zoom)EditorGUILayout.EnumPopup("Zoom", currentZoom, GUILayout.Width(cw));

            if (EditorGUI.EndChangeCheck()) {
                UpdateCanvasMaterialProperties();
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();

            GUI.contentColor = prevContentColor;

            // Column: Canvas
            Rect space = EditorGUILayout.BeginVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();

            float min = Mathf.Min(space.width, space.height);

            float h = min;
            float w = min * width / height;

            if (w > space.width) {
                h = h * space.width / w;
                w = space.width;
            }
            space.height = (int)h;
            space.width = (int)w;

            Rect canvasSpace = space;
            int horizTilesVisible = width;
            int vertTilesVisible = height;
            float zoomFactor = 100f / (int)currentZoom;

            if ((int)currentZoom > 100) {
                canvasSpace.yMax -= 16;
                canvasSpace.xMax -= 16;
                horizTilesVisible = (int)(width * zoomFactor);
                vertTilesVisible = (int)(height * zoomFactor);
            }

            canShowGrid = (canvasSpace.width / horizTilesVisible) > 8;

            if (Event.current.type == EventType.Repaint) {
                Vector3 canvasPos = Event.current.mousePosition;
                canvasPos.x -= canvasSpace.xMin;
                canvasPos.y -= canvasSpace.yMin;
                if (canvasPos.x >= 0 && canvasPos.x < canvasSpace.width && canvasPos.y >= 0 && canvasPos.y < canvasSpace.height) {
                    canvasPos.x /= canvasSpace.width;
                    canvasPos.y /= canvasSpace.height;

                    canvasPos.y = 1f - canvasPos.y;

                    currentTexelPos.x = (int)(canvasPos.x * horizTilesVisible) + panningPosTexel.x;
                    currentTexelPos.y = (int)(canvasPos.y * vertTilesVisible) + panningPosTexel.y;

                    UpdateCanvasMaterialProperties();
                    isCursorOnTexture = true;
                } else {
                    isCursorOnTexture = false;
                    currentTexelPos.x = currentTexelPos.y = -1000;
                    UpdateCanvasMaterialProperties();
                }
            }

            if (Event.current.isMouse) {
                if (isCursorOnTexture && Event.current.type == EventType.MouseDown) {
                    if (Event.current.button == 0) {
                        ExecuteBrush();
                    } else {
                        brushColor = GetCursorColor();
                    }
                } else if (Event.current.type == EventType.MouseUp) {
                    isBrushing = false;
                }
            }

            EditorGUI.DrawPreviewTexture(canvasSpace, canvasTexture, canvasMaterial);
            if ((int)currentZoom > 100) {
                Rect horizBarPos = new Rect(new Vector2(space.xMin, space.yMax - 16), new Vector2(space.width - 16, 24));
                float panX = GUI.HorizontalScrollbar(horizBarPos, panningPos.x, horizTilesVisible, 0, width);
                panningPos.x = panX;
                panningPosTexel.x = (int)(panX);

                Rect vertBarPos = new Rect(new Vector2(space.xMax - 16, space.yMin), new Vector2(24, space.height - 16));
                float panY = GUI.VerticalScrollbar(vertBarPos, panningPos.y, vertTilesVisible, height, 0);
                panningPos.y = panY;
                panningPosTexel.y = (int)(panY);
            } else {
                panningPosTexel = Vector2Int.zero;
            }

            // Column: preview
            EditorGUILayout.BeginVertical(GUILayout.MaxWidth(cw * 3));
            EditorGUILayout.BeginVertical(GUI.skin.box);
            EditorGUILayout.LabelField("Seamless Preview", GUILayout.MaxWidth(cw * 2));
            space = EditorGUILayout.BeginVertical(GUILayout.Width(cw * 3), GUILayout.Height(cw * 3));
            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndVertical();

            EditorGUIUtility.labelWidth = 60;
            EditorGUILayout.LabelField("Width:", width.ToString());
            EditorGUILayout.LabelField("Height:", height.ToString());

            if (currentTexelPos.x >= 0) {
                EditorGUILayout.LabelField("Cursor:", currentTexelPos.x + ", " + currentTexelPos.y);
            } else {
                EditorGUILayout.LabelField("");
            }
            EditorGUILayout.Separator();

            EditorGUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Support & Suggestions");
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Forum", GUILayout.Height(32))) Application.OpenURL("https://kronnect.com/support");
            if (GUILayout.Button("Kronnect Assets", GUILayout.Height(32))) Application.OpenURL("https://assetstore.unity.com/publishers/15018?aid=1101lGsd");
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();

            // Paint canvas
            for (int y = 0; y < 3; y++) {
                for (int x = 0; x < 3; x++) {
                    Rect subSpace = new Rect(space.x + x * cw, space.y + y * cw, cw, cw);
                    EditorGUI.DrawPreviewTexture(subSpace, canvasTexture, previewMaterial);
                }
            }

            EditorGUILayout.EndHorizontal(); // row
        }

        void DrawBottomRowButtons() {
            EditorGUILayout.Separator();
        }

        void UpdateCanvasMaterialProperties() {
            float zoomFactor = 100f / (int)currentZoom;

            float cursorSize = currentBrush.usesBrush() ? brushPixelWidth * 0.5f : 0.5f;
            float pixelOffset = currentBrush.usesBrush() ? (brushPixelWidth % 2) * 0.5f : 0.5f;

            float h = (int)(height * zoomFactor);
            float w = (int)(width * zoomFactor);

            Vector4 cursorPos = new Vector4((currentTexelPos.x - panningPosTexel.x + pixelOffset) / w, (currentTexelPos.y - panningPosTexel.y + pixelOffset) / h, cursorSize / w, cursorSize / h);

            Vector4 cursorPos1, cursorPos2, cursorPos3;
            cursorPos1 = cursorPos2 = cursorPos3 = cursorPos;
            if (!currentBrush.usesBrush() || _mirrorMode == MirrorMode.None) {
                cursorPos1.x = cursorPos2.x = cursorPos3.x = -1000;
                cursorPos1.y = cursorPos2.y = cursorPos3.y = -1000;
            } else {
                switch (_mirrorMode) {
                    case MirrorMode.Horizontal:
                        cursorPos1.x = (width - 1 - currentTexelPos.x - panningPosTexel.x + pixelOffset) / w;
                        cursorPos2.x = cursorPos2.y = cursorPos3.x = -1;
                        break;
                    case MirrorMode.Vertical:
                        cursorPos2.y = (height - 1 - currentTexelPos.y - panningPosTexel.y + pixelOffset) / h;
                        cursorPos1.x = cursorPos3.x = -1;
                        break;
                    case MirrorMode.Quad:
                        cursorPos1.x = (width - 1 - currentTexelPos.x - panningPosTexel.x + pixelOffset) / w;
                        cursorPos2.y = (height - 1 - currentTexelPos.y - panningPosTexel.y + pixelOffset) / h;
                        cursorPos3.x = cursorPos1.x;
                        cursorPos3.y = cursorPos2.y;
                        break;
                }
            }
            canvasMaterial.SetVector("_CursorPos", cursorPos);
            canvasMaterial.SetVector("_CursorPos1", cursorPos1);
            canvasMaterial.SetVector("_CursorPos2", cursorPos2);
            canvasMaterial.SetVector("_CursorPos3", cursorPos3);
            canvasMaterial.SetColor("_CursorColor", _brushColor);

            canvasMaterial.SetFloat("_GridWidth", _showGrid ? 0.002f * zoomFactor : 0);
            float uvXmin = (float)panningPosTexel.x / width;
            float uvYmin = (float)panningPosTexel.y / height;

            float uvXextents = Mathf.FloorToInt(width * zoomFactor) / (float)width;
            float uvXmax = uvXmin + uvXextents;
            float uvYextents = Mathf.FloorToInt(height * zoomFactor) / (float)height;
            float uvYmax = uvYmin + uvYextents;

            canvasMaterial.SetVector("_TileSize", new Vector4(1f / (int)(width * zoomFactor), 1f / (int)(height * zoomFactor), zoomFactor, canShowGrid ? 1: 0));
            canvasMaterial.SetVector("_ZoomRect", new Vector4(uvXmin, uvYmin, uvXmax, uvYmax));
        }


        void ReadColors() {
            if (colorsFromTexture == canvasTexture || canvasTexture == null) return;
            colors = canvasTexture.GetPixels();
            colorsFromTexture = texture;
        }

        void SelectBrush() {
            switch (currentBrush) {
                case Brush.Pen: _brushColor = lastDrawingColor; break;
                case Brush.Replace: _brushColor = lastDrawingColor; break;
                case Brush.FloodFill: _brushColor = lastDrawingColor; break;
                case Brush.Eraser: _brushColor = new Color(0, 0, 0, 0); break;
                case Brush.Darken: _brushColor = new Color(0, 0, 0, 1); break;
                case Brush.Lighten: _brushColor = new Color(1, 1, 1, 1); break;
                case Brush.Dry: _brushColor = new Color(0.2f, 0.2f, 0.2f, 1); break;
                case Brush.Vivid: _brushColor = new Color(1f, 0.7f, 0.5f, 1); break;
                case Brush.Noise: _brushColor = new Color(0.5f, 0.5f, 0.5f, 1); break;
                case Brush.NoiseTone: _brushColor = new Color(0.7f, 0.5f, 0.7f, 1); break;
                case Brush.Gradient: _brushColor = lastDrawingColor; break;
            }
        }

        void ExecuteBrush() {
            isBrushing = false;
            lastPaintedTexelPos.x = -1000;
            startTexelPos = currentTexelPos;
            switch (currentBrush) {
                case Brush.Pen:
                case Brush.Eraser:
                case Brush.Darken:
                case Brush.Lighten:
                case Brush.Dry:
                case Brush.Vivid:
                case Brush.Noise:
                case Brush.NoiseTone:
                case Brush.Gradient:
                    isBrushing = true;
                    break;
                case Brush.Replace:
                    ReplaceColors();
                    break;
                case Brush.FloodFill:
                    FloodFill();
                    break;
            }
        }

        void PostPaintPixel() {
        }


    }

}

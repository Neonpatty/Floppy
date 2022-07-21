using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;
using System.Threading.Tasks;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace ColorStudio {

    public enum KeyColorType {
        Primary = 0,
        Complementary = 1,
        Custom = 2
    }

    public enum ColorMatchMode {
        RGB = 0,
        Red = 1,
        Green = 2,
        Blue = 3,
        HSL = 10,
        Hue = 11,
        Saturation = 12,
        Lightness = 13
    }

    public static class KeyColorTypeExtensions {
        public static float dotColor(this KeyColorType keyColorType) {
            switch (keyColorType) {
                case KeyColorType.Primary:
                    return 0;
                case KeyColorType.Complementary:
                    return 9;
            }
            return 3;
        }
    }

    [Serializable]
    public struct KeyColor {
        public float angle, hue;
        public Color color;
        public Vector2 pos;
        public bool visible, highlighted;
        public KeyColorType type;
    }


    public partial class CSPalette : ScriptableObject {
        public const int MAX_KEY_COLORS = 128;
        public const int START_INDEX_CUSTOM_COLOR = 5;

        public int order;
        public int hueCount = 4;
        public int shades = 3;
        public float saturation = 0.85f;
        public float minBrightness = 0;
        public float maxBrightness = 1f;
        public int kelvin = 4000;
        public float colorTempStrength = 0;
        public float splitAmount = 0.6f;
        public ColorScheme scheme = ColorScheme.Complementary;
        public KeyColor[] keyColors;

#if UNITY_EDITOR
        [NonSerialized]
        public Material material;
#endif

        [NonSerialized]
        public Color[] colors;

        [NonSerialized]
        public int colorsCount;

        public Vector2 primaryPos { get { return keyColors[0].pos; } set { keyColors[0].pos = value; } }

        public float primaryAngle { get { return keyColors[0].angle; } set { keyColors[0].angle = value; } }

        public Color primaryColor { get { return keyColors[0].color; } set { keyColors[0].color = value; } }

        public Color complementary1Color { get { return keyColors[1].color; } set { keyColors[1].color = value; } }

        public Color complementary2Color { get { return keyColors[2].color; } set { keyColors[2].color = value; } }

        public Color complementary3Color { get { return keyColors[3].color; } set { keyColors[3].color = value; } }

        Texture2D currentLUT;

        const float PI2 = Mathf.PI * 2f;
        const float HALF_PI = Mathf.PI * 0.5f;

        static Dictionary<Texture2D, Color[]> lutColorsCache = new Dictionary<Texture2D, Color[]>();
        Color[] lutColors;

        Color[] paletteColors; // all colors, including shades

        public static CSPalette CreateEmptyPalette() {
            CSPalette palette = CreateInstance<CSPalette>();
            palette.scheme = ColorScheme.Custom;
            palette.colorsCount = 0;
            palette.hueCount = 0;
            return palette;
        }

        public int customColorsCount {
            get {
                int count = 0;
                for (int k = START_INDEX_CUSTOM_COLOR; k < keyColors.Length; k++) {
                    if (keyColors[k].visible)
                        count++;
                }
                return count;
            }
        }

        void Awake() {
            Init();
        }

        void Init() {
            if (keyColors == null || keyColors.Length < MAX_KEY_COLORS) {
                keyColors = new KeyColor[MAX_KEY_COLORS];
            }
            BuildHueColors();
        }

        public void Clear() {
            for (int k = 0; k < keyColors.Length; k++) {
                keyColors[k].visible = false;
            }
        }

        /// <summary>
        /// Configures this palette using a factory scheme and primary color
        /// </summary>
        public void ConfigurePalette(ColorScheme scheme, Color primaryColor, float splitAmount = 0.6f) {
            this.scheme = scheme;
            this.primaryColor = primaryColor;
            this.saturation = 1f;
            this.hueCount = scheme.recommendedHues();
            this.splitAmount = splitAmount;
            float hue = ColorConversion.GetHue(primaryColor.r, primaryColor.g, primaryColor.b);
            this.primaryAngle = hue * PI2;
            SetSchemeKeyColors();
        }

        void SetSchemeKeyColors() {
            switch (scheme) {
                case ColorScheme.Monochromatic:
                case ColorScheme.Custom:
                    break;
                case ColorScheme.Complementary: {
                        SetKeyColor(1, KeyColorType.Complementary, primaryAngle + Mathf.PI);
                    }
                    break;
                case ColorScheme.Gradient: {
                        SetKeyColor(1, KeyColorType.Complementary, splitAmount + Mathf.PI);
                    }
                    break;
                case ColorScheme.SplitComplementary: {
                        SetKeyColor(1, KeyColorType.Complementary, primaryAngle + splitAmount + Mathf.PI);
                        SetKeyColor(2, KeyColorType.Complementary, primaryAngle - splitAmount + Mathf.PI);
                    }
                    break;
                case ColorScheme.Analogous: {
                        SetKeyColor(1, KeyColorType.Complementary, primaryAngle + splitAmount);
                        SetKeyColor(2, KeyColorType.Complementary, primaryAngle - splitAmount);
                    }
                    break;
                case ColorScheme.Triadic: {
                        SetKeyColor(1, KeyColorType.Complementary, primaryAngle + Mathf.PI * 2f / 3f);
                        SetKeyColor(2, KeyColorType.Complementary, primaryAngle - Mathf.PI * 2f / 3f);
                    }
                    break;
                case ColorScheme.Tetradic: {
                        SetKeyColor(1, KeyColorType.Complementary, primaryAngle + splitAmount);
                        SetKeyColor(2, KeyColorType.Complementary, primaryAngle + Mathf.PI);
                        SetKeyColor(3, KeyColorType.Complementary, primaryAngle + splitAmount + Mathf.PI);
                    }
                    break;
                case ColorScheme.Square: {
                        SetKeyColor(1, KeyColorType.Complementary, primaryAngle + HALF_PI);
                        SetKeyColor(2, KeyColorType.Complementary, primaryAngle + Mathf.PI);
                        SetKeyColor(3, KeyColorType.Complementary, primaryAngle + HALF_PI + Mathf.PI);
                    }
                    break;
                case ColorScheme.AccentedAnalogous: {
                        SetKeyColor(1, KeyColorType.Complementary, primaryAngle + splitAmount);
                        SetKeyColor(2, KeyColorType.Complementary, primaryAngle + Mathf.PI);
                        SetKeyColor(3, KeyColorType.Complementary, primaryAngle - splitAmount);
                    }
                    break;
            }
        }


        void SetKeyColor(int index, KeyColorType keyColorType, float angle) {
            float hue = ((angle % PI2 + PI2) % PI2) / PI2;
            keyColors[index].angle = angle;
            keyColors[index].hue = hue;
            keyColors[index].type = keyColorType;
            keyColors[index].color = ColorConversion.GetColorFromHSL(hue, saturation, 0.5f);
            keyColors[index].visible = true;
        }

        /// <summary>
        /// Builds the hue colors
        /// </summary>
        public void BuildHueColors() {
            if (colors == null || colors.Length != 256) {
                colors = new Color[256];
            }

            if (scheme == ColorScheme.Custom) {
                colorsCount = 0;
            } else {
                colorsCount = Mathf.Max(hueCount, scheme.minHues());
            }

            switch (scheme) {
                case ColorScheme.Custom:
                    break;
                case ColorScheme.Monochromatic:
                    for (int k = 0; k < colorsCount; k++) {
                        colors[k] = primaryColor;
                    }
                    break;
                case ColorScheme.Complementary: {
                        for (int k = 0; k < colorsCount; k++) {
                            colors[k] = Color.Lerp(primaryColor, complementary1Color, (float)k / (colorsCount - 1));
                        }
                    }
                    break;
                case ColorScheme.Gradient: {
                        for (int k = 0; k < colorsCount; k++) {
                            colors[k] = Color.Lerp(primaryColor, complementary1Color, (float)k / (colorsCount - 1));
                        }
                    }
                    break;
                case ColorScheme.SplitComplementary: {
                        const float third = 1f / 3f;
                        const float twoThirds = 2f / 3f;
                        for (int k = 0; k < colorsCount; k++) {
                            float t = (float)k / colorsCount;
                            if (t < third) {
                                colors[k] = Color.Lerp(primaryColor, complementary1Color, t / third);
                            } else if (t < twoThirds) {
                                colors[k] = Color.Lerp(complementary1Color, complementary2Color, (t - third) / third);
                            } else {
                                colors[k] = Color.Lerp(complementary2Color, primaryColor, (t - twoThirds) / third);
                            }
                        }
                    }
                    break;
                case ColorScheme.Analogous: {
                        for (int k = 0; k < colorsCount; k++) {
                            float t = (float)k / (colorsCount - 1);
                            if (t < 0.5f) {
                                colors[k] = Color.Lerp(complementary1Color, primaryColor, t / 0.5f);
                            } else {
                                colors[k] = Color.Lerp(primaryColor, complementary2Color, (t - 0.5f) / 0.5f);
                            }
                        }
                    }
                    break;
                case ColorScheme.Triadic: {
                        const float third = 1f / 3f;
                        const float twoThirds = 2f / 3f;
                        for (int k = 0; k < colorsCount; k++) {
                            float t = (float)k / colorsCount;
                            if (t < third) {
                                colors[k] = Color.Lerp(primaryColor, complementary1Color, t / third);
                            } else if (t < twoThirds) {
                                colors[k] = Color.Lerp(complementary1Color, complementary2Color, (t - third) / third);
                            } else {
                                colors[k] = Color.Lerp(complementary2Color, primaryColor, (t - twoThirds) / third);
                            }
                        }
                    }
                    break;
                case ColorScheme.Tetradic: {
                        for (int k = 0; k < colorsCount; k++) {
                            float t = (float)k / colorsCount;
                            if (t < 0.25f) {
                                colors[k] = Color.Lerp(primaryColor, complementary1Color, t / 0.25f);
                            } else if (t < 0.5f) {
                                colors[k] = Color.Lerp(complementary1Color, complementary2Color, (t - 0.25f) / 0.25f);
                            } else if (t < 0.75f) {
                                colors[k] = Color.Lerp(complementary2Color, complementary3Color, (t - 0.5f) / 0.25f);
                            } else {
                                colors[k] = Color.Lerp(complementary3Color, primaryColor, (t - 0.75f) / 0.25f);
                            }
                        }
                    }
                    break;
                case ColorScheme.Square: {
                        for (int k = 0; k < colorsCount; k++) {
                            float t = (float)k / colorsCount;
                            if (t < 0.25f) {
                                colors[k] = Color.Lerp(primaryColor, complementary1Color, t / 0.25f);
                            } else if (t < 0.5f) {
                                colors[k] = Color.Lerp(complementary1Color, complementary2Color, (t - 0.25f) / 0.25f);
                            } else if (t < 0.75f) {
                                colors[k] = Color.Lerp(complementary2Color, complementary3Color, (t - 0.5f) / 0.25f);
                            } else {
                                colors[k] = Color.Lerp(complementary3Color, primaryColor, (t - 0.75f) / 0.25f);
                            }
                        }
                    }
                    break;
                case ColorScheme.AccentedAnalogous: {
                        for (int k = 0; k < colorsCount; k++) {
                            float t = (float)k / colorsCount;
                            if (t < 0.25f) {
                                colors[k] = Color.Lerp(primaryColor, complementary1Color, t / 0.25f);
                            } else if (t < 0.5f) {
                                colors[k] = Color.Lerp(complementary1Color, complementary2Color, (t - 0.25f) / 0.25f);
                            } else if (t < 0.75f) {
                                colors[k] = Color.Lerp(complementary2Color, complementary3Color, (t - 0.5f) / 0.25f);
                            } else {
                                colors[k] = Color.Lerp(complementary3Color, primaryColor, (t - 0.75f) / 0.25f);
                            }
                        }

                    }
                    break;
                case ColorScheme.Spectrum:
                    for (int k = 0; k < colorsCount; k++) {
                        colors[k] = ColorConversion.GetColor((float)k / colorsCount);
                    }
                    break;
            }

            // Add custom colors
            for (int k = START_INDEX_CUSTOM_COLOR; k < keyColors.Length; k++) {
                if (colorsCount < colors.Length && keyColors[k].visible) {
                    colors[colorsCount++] = keyColors[k].color;
                }
            }

            // Apply color temp correction
            if (colorTempStrength > 0) {
                Color ct = Mathf.CorrelatedColorTemperatureToRGB(kelvin);
                float ac = 1f - colorTempStrength;
                for (int k = 0; k < colorsCount; k++) {
                    colors[k].r = colors[k].r * (ct.r * colorTempStrength + ac);
                    colors[k].g = colors[k].g * (ct.g * colorTempStrength + ac);
                    colors[k].b = colors[k].b * (ct.b * colorTempStrength + ac);
                }
            }
            UpdateMaterial();
        }

        public void Load(CSPalette otherPalette) {
            order = otherPalette.order;
            hueCount = otherPalette.hueCount;
            shades = otherPalette.shades;
            saturation = otherPalette.saturation;
            minBrightness = otherPalette.minBrightness;
            maxBrightness = otherPalette.maxBrightness;
            splitAmount = otherPalette.splitAmount;
            scheme = otherPalette.scheme;
            kelvin = otherPalette.kelvin;
            colorTempStrength = otherPalette.colorTempStrength;
            keyColors = new KeyColor[otherPalette.keyColors.Length];
            for (int k = 0; k < keyColors.Length; k++) {
                keyColors[k] = otherPalette.keyColors[k];
            }
            BuildHueColors();
        }

        public void UpdateMaterial() {
#if UNITY_EDITOR
            if (material == null) {
                Shader shader = Shader.Find("Color Studio/Palette");
                if (shader == null) return;
                material = new Material(shader);
            }
            material.SetFloat("_Shades", shades);
            material.SetFloat("_Saturation", saturation);
            material.SetFloat("_MinBrightness", minBrightness);
            material.SetFloat("_MaxBrightness", maxBrightness);
            if (colors == null || colors.Length == 0) {
                Init();
            }
            if (colors != null && colors.Length > 0) {
                material.SetColorArray("_Colors", colors);
            }
            material.SetInt("_ColorCount", colorsCount);
#endif
        }

        /// <summary>
        /// Builds entire palette considering hues and shades
        /// </summary>
        /// <returns></returns>
        public Color[] BuildPaletteColors() {
            if (colorsCount == 0) {
                Init();
            }
            int totalColorCount = colorsCount * shades;
            if (paletteColors == null || paletteColors.Length != totalColorCount) {
                paletteColors = new Color[totalColorCount];
            }
            for (int i = 0, c = 0; c < colorsCount; c++) {
                for (int s = 0; s < shades; s++, i++) {
                    float t = (s + 0.5f) / shades;
                    t = minBrightness + (maxBrightness - minBrightness) * t;
                    paletteColors[i] = ColorConversion.GetColorFromRGBSL(this.colors[c].r, this.colors[c].g, this.colors[c].b, saturation, t);
                }
            }
            return paletteColors;
        }

        /// <summary>
        /// Creates a LUT using the palette colors
        /// </summary>
        public Texture2D ExportLUT() {
            Texture2D tex = new Texture2D(1024, 32, TextureFormat.ARGB32, false, true);
            tex.anisoLevel = 0;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] palette = BuildPaletteColors();

            // Build LUT
            Color[] lutColors = new Color[1024 * 32];
            for (int i = 0, y = 0; y < 32; y++) {
                float g = (y << 3) / 255f;
                for (int x = 0; x < 1024; x++, i++) {
                    float b = ((x >> 5) << 3) / 255f;
                    float r = ((x & 0x1F) << 3) / 255f;
                    float md = float.MaxValue;
                    for (int c = 0; c < palette.Length; c++) {
                        float dr = (palette[c].r - r) * 0.299f;
                        float dg = (palette[c].g - g) * 0.587f;
                        float db = (palette[c].b - b) * 0.114f;
                        float d = dr * dr + dg * dg + db * db;
                        if (d < md) {
                            md = d;
                            lutColors[i] = palette[c];
                        }
                    }
                }
            }
            tex.SetPixels(lutColors);
            tex.Apply();
            return tex;
        }




        /// <summary>
        /// Creates a LUT using the palette colors
        /// </summary>
        public Texture2D ExportTexture() {
            Color[] palette = BuildPaletteColors();
            int width = Mathf.Min(palette.Length, 64);
            int height = Mathf.CeilToInt(palette.Length / width);

            Texture2D tex = new Texture2D(width, height, TextureFormat.ARGB32, false, true);
            tex.anisoLevel = 0;
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] colors = new Color[width * height];
            for (int k = 0; k < colors.Length && k < palette.Length; k++) {
                colors[k] = palette[k];
            }
            tex.SetPixels(colors);
            tex.Apply();
            return tex;
        }

        Vector3 lutST = new Vector3(1f / 1024f, 1f / 32f, 32f - 1);

        Color ApplyLUT(Color color, Texture2D LUT) {
            if (currentLUT != LUT || lutColors == null || lutColors.Length == 0) {
                // Decode LUT
                currentLUT = LUT;
                if (!lutColorsCache.TryGetValue(LUT, out lutColors)) {
                    lutColors = LUT.GetPixels();
                    lutColorsCache[LUT] = lutColors;
                }
            }

            Vector3 lookUp = new Vector3(color.r * lutST.z, color.g * lutST.z, color.b * lutST.z);
            lookUp.x = lutST.x * (lookUp.x + 0.5f);
            lookUp.y = lutST.y * (lookUp.y + 0.5f);
            float slice = (int)(lookUp.z);
            lookUp.x += slice * lutST.y;
            Vector2 lookUpNextSlice = new Vector2(lookUp.x + lutST.y, lookUp.y);
            int index0 = (int)(lookUp.y * 32) * 1024 + (int)(lookUp.x * 1024);
            if (index0 >= lutColors.Length) index0 = lutColors.Length - 1;
            int index1 = (int)(lookUpNextSlice.y * 32) * 1024 + (int)(lookUpNextSlice.x * 1024);
            if (index1 >= lutColors.Length) index1 = lutColors.Length - 1;

            float t = lookUp.z - slice;
            Color result = lutColors[index0];
            result.r = result.r * (1f - t) + lutColors[index1].r * t;
            result.g = result.g * (1f - t) + lutColors[index1].g * t;
            result.b = result.b * (1f - t) + lutColors[index1].b * t;
            return result;
        }

        /// <summary>
        /// Returns the nearest color in the palette
        /// </summary>
        public Color GetNearestColor(Color color, ColorMatchMode colorMatchMode) {
            return GetNearestColor(color, colorMatchMode, 0, null, false, ColorAdjustments.None);
        }

        /// <summary>
        /// Returns the nearest color in the palette
        /// </summary>
        public Color GetNearestColor(Color color, ColorMatchMode colorMatchMode, float threshold, ColorEntry[] colorOperations, bool enableColorAdjustments, ColorAdjustments colorAdjustments) {
            Color[] palette = BuildPaletteColors();
            return GetNearestColor(palette, color, colorMatchMode, threshold, colorOperations, enableColorAdjustments, colorAdjustments);

        }

        /// <summary>
        /// Returns the nearest color in the palette
        /// </summary>
        public Color GetNearestColor(Color[] palette, Color color, ColorMatchMode colorMatchMode, float threshold, ColorEntry[] colorOperations, bool enableColorAdjustments, ColorAdjustments colorAdjustments) {
            if (colorOperations != null && colorOperations.Length > 0) {

                HSLColor hsl = ColorConversion.GetHSLFromRGB(color.r, color.g, color.b);

                for (int k = 0; k < colorOperations.Length; k++) {
                    switch (colorMatchMode) {
                        case ColorMatchMode.RGB: {
                                float dr = colorOperations[k].color.r < color.r ? color.r - colorOperations[k].color.r : colorOperations[k].color.r - color.r;
                                float dg = colorOperations[k].color.g < color.g ? color.g - colorOperations[k].color.g : colorOperations[k].color.g - color.g;
                                float db = colorOperations[k].color.b < color.b ? color.b - colorOperations[k].color.b : colorOperations[k].color.b - color.b;
                                if (dr <= threshold && dg <= threshold && db <= threshold) {
                                    if (colorOperations[k].operation == ColorOperation.Preserve) {
                                        return color;
                                    } else if (colorOperations[k].operation == ColorOperation.Replace) {
                                        return colorOperations[k].replaceColor;
                                    }
                                }
                            }
                            break;
                        case ColorMatchMode.Red: {
                                float dr = colorOperations[k].color.r < color.r ? color.r - colorOperations[k].color.r : colorOperations[k].color.r - color.r;
                                if (dr <= threshold) {
                                    if (colorOperations[k].operation == ColorOperation.Preserve) {
                                        return color;
                                    } else if (colorOperations[k].operation == ColorOperation.Replace) {
                                        return colorOperations[k].replaceColor;
                                    }
                                }
                            }
                            break;
                        case ColorMatchMode.Green: {
                                float dg = colorOperations[k].color.g < color.g ? color.g - colorOperations[k].color.g : colorOperations[k].color.g - color.g;
                                if (dg <= threshold) {
                                    if (colorOperations[k].operation == ColorOperation.Preserve) {
                                        return color;
                                    } else if (colorOperations[k].operation == ColorOperation.Replace) {
                                        return colorOperations[k].replaceColor;
                                    }
                                }
                            }
                            break;
                        case ColorMatchMode.Blue: {
                                float db = colorOperations[k].color.b < color.b ? color.b - colorOperations[k].color.b : colorOperations[k].color.b - color.b;
                                if (db <= threshold) {
                                    if (colorOperations[k].operation == ColorOperation.Preserve) {
                                        return color;
                                    } else if (colorOperations[k].operation == ColorOperation.Replace) {
                                        return colorOperations[k].replaceColor;
                                    }
                                }
                            }
                            break;
                        case ColorMatchMode.HSL: {
                                HSLColor hslOp = ColorConversion.GetHSLFromRGB(colorOperations[k].color.r, colorOperations[k].color.g, colorOperations[k].color.b);
                                float dh = hslOp.h < hsl.h ? hsl.h - hslOp.h : hslOp.h - hsl.h;
                                float ds = hslOp.s < hsl.s ? hsl.s - hslOp.s : hslOp.s - hsl.s;
                                float dl = hslOp.l < hsl.l ? hsl.l - hslOp.l : hslOp.l - hsl.l;
                                if (dh <= threshold && ds <= threshold && dl <= threshold) {
                                    if (colorOperations[k].operation == ColorOperation.Preserve) {
                                        return color;
                                    } else if (colorOperations[k].operation == ColorOperation.Replace) {
                                        return colorOperations[k].replaceColor;
                                    }
                                }
                            }
                            break;
                        case ColorMatchMode.Hue: {
                                float hue = ColorConversion.GetHue(colorOperations[k].color.r, colorOperations[k].color.g, colorOperations[k].color.b);
                                float dh = hue < hsl.h ? hsl.h - hue : hue - hsl.h;
                                if (dh <= threshold) {
                                    if (colorOperations[k].operation == ColorOperation.Preserve) {
                                        return color;
                                    } else if (colorOperations[k].operation == ColorOperation.Replace) {
                                        return colorOperations[k].replaceColor;
                                    }
                                }
                            }
                            break;
                        case ColorMatchMode.Saturation: {
                                float sat = ColorConversion.GetSaturation(colorOperations[k].color.r, colorOperations[k].color.g, colorOperations[k].color.b);
                                float ds = sat < hsl.s ? hsl.s - sat : sat - hsl.s;
                                if (ds <= threshold) {
                                    if (colorOperations[k].operation == ColorOperation.Preserve) {
                                        return color;
                                    } else if (colorOperations[k].operation == ColorOperation.Replace) {
                                        return colorOperations[k].replaceColor;
                                    }
                                }
                            }
                            break;
                        case ColorMatchMode.Lightness: {
                                float l = ColorConversion.GetLightness(colorOperations[k].color.r, colorOperations[k].color.g, colorOperations[k].color.b);
                                float dl = l < hsl.l ? hsl.l - l : l - hsl.l;
                                if (dl <= threshold) {
                                    if (colorOperations[k].operation == ColorOperation.Preserve) {
                                        return color;
                                    } else if (colorOperations[k].operation == ColorOperation.Replace) {
                                        return colorOperations[k].replaceColor;
                                    }
                                }
                            }
                            break;
                    }
                }
            }

            float md = float.MaxValue;
            Color nearest = color;
            switch (colorMatchMode) {
                case ColorMatchMode.RGB: {
                        for (int c = 0; c < palette.Length; c++) {
                            float dr = (palette[c].r - color.r) * 0.299f;
                            float dg = (palette[c].g - color.g) * 0.587f;
                            float db = (palette[c].b - color.b) * 0.114f;
                            float d = dr * dr + dg * dg + db * db;
                            if (d < md) {
                                md = d;
                                nearest = palette[c];
                            }
                        }
                    }
                    break;
                case ColorMatchMode.Red: {
                        for (int c = 0; c < palette.Length; c++) {
                            float dr = palette[c].r - color.r;
                            float d = dr * dr;
                            if (d < md) {
                                md = d;
                                nearest = palette[c];
                            }
                        }
                    }
                    break;
                case ColorMatchMode.Green: {
                        for (int c = 0; c < palette.Length; c++) {
                            float dg = palette[c].g - color.g;
                            float d = dg * dg;
                            if (d < md) {
                                md = d;
                                nearest = palette[c];
                            }
                        }
                    }
                    break;
                case ColorMatchMode.Blue: {
                        for (int c = 0; c < palette.Length; c++) {
                            float db = palette[c].b - color.b;
                            float d = db * db;
                            if (d < md) {
                                md = d;
                                nearest = palette[c];
                            }
                        }
                    }
                    break;
                case ColorMatchMode.HSL: {
                        HSLColor hsl = ColorConversion.GetHSLFromRGB(color.r, color.g, color.b);
                        for (int c = 0; c < palette.Length; c++) {
                            HSLColor paletteHSL = ColorConversion.GetHSLFromRGB(palette[c].r, palette[c].g, palette[c].b);
                            float dh = (paletteHSL.h - hsl.h);
                            float ds = (paletteHSL.s - hsl.s);
                            float dl = (paletteHSL.l - hsl.l);
                            float d = dh * dh + ds * ds + dl * dl;
                            if (d < md) {
                                md = d;
                                nearest = palette[c];
                            }

                        }
                    }
                    break;
                case ColorMatchMode.Hue: {
                        float h = ColorConversion.GetHue(color.r, color.g, color.b);
                        for (int c = 0; c < palette.Length; c++) {
                            float paletteH = ColorConversion.GetHue(palette[c].r, palette[c].g, palette[c].b);
                            float dh = (paletteH - h);
                            float d = dh * dh;
                            if (d < md) {
                                md = d;
                                nearest = palette[c];
                            }

                        }
                    }
                    break;
                case ColorMatchMode.Saturation: {
                        float s = ColorConversion.GetSaturation(color.r, color.g, color.b);
                        for (int c = 0; c < palette.Length; c++) {
                            float paletteS = ColorConversion.GetSaturation(palette[c].r, palette[c].g, palette[c].b);
                            float dh = (paletteS - s);
                            float d = dh * dh;
                            if (d < md) {
                                md = d;
                                nearest = palette[c];
                            }

                        }
                    }
                    break;
                case ColorMatchMode.Lightness: {
                        float l = ColorConversion.GetLightness(color.r, color.g, color.b);
                        for (int c = 0; c < palette.Length; c++) {
                            float paletteL = ColorConversion.GetLightness(palette[c].r, palette[c].g, palette[c].b);
                            float dh = (paletteL - l);
                            float d = dh * dh;
                            if (d < md) {
                                md = d;
                                nearest = palette[c];
                            }

                        }
                    }
                    break;
            }
            nearest.a = color.a;
            color = nearest;

            if (enableColorAdjustments) {
                if (colorAdjustments.applyLUT && colorAdjustments.LUT != null) {
                    color = ApplyLUT(color, colorAdjustments.LUT);
                }
                // vibrance
                float vibrance = colorAdjustments.vibrance;
                if (vibrance > 0) {
                    float maxComponent = color.r > color.g ? color.r : color.g;
                    maxComponent = color.b > maxComponent ? color.b : maxComponent;
                    float minComponent = color.r < color.g ? color.r : color.g;
                    minComponent = color.b < minComponent ? color.b : minComponent;
                    float sat = maxComponent - minComponent;
                    if (sat < 0) sat = 0; else if (sat > 1f) sat = 1f;
                    float luma = color.GetLuma();
                    color.r *= 1f + vibrance * (1f - sat) * (color.r - luma);
                    color.g *= 1f + vibrance * (1f - sat) * (color.g - luma);
                    color.b *= 1f + vibrance * (1f - sat) * (color.b - luma);
                }
                // tinting
                float tintAmount = colorAdjustments.tintAmount;
                if (tintAmount > 0) {
                    Color tintedColor = new Color(color.r * colorAdjustments.tintColor.r, color.g * colorAdjustments.tintColor.g, color.b * colorAdjustments.tintColor.b);
                    color = Color.Lerp(color, tintedColor, tintAmount);
                }
                // contrast
                float contrast = colorAdjustments.contrast + 1f;
                if (contrast != 1f) {
                    color.r = (color.r - 0.5f) * contrast + 0.5f;
                    color.g = (color.g - 0.5f) * contrast + 0.5f;
                    color.b = (color.b - 0.5f) * contrast + 0.5f;
                }
                // brightness
                float brightness = colorAdjustments.brightness + 1f;
                if (brightness != 1f) {
                    color.r *= brightness;
                    color.g *= brightness;
                    color.b *= brightness;
                }
            }

            return color;
        }


        public Color[] GetNearestColors(Color[] originalColors) {
            return GetNearestColors(originalColors, ColorMatchMode.RGB, 0f, null, false, ColorAdjustments.None);
        }

        static Texture2D lastTexture;
        static Color[] originalColors;

        public Color[] GetNearestColors(Texture tex, ColorMatchMode colorMatchMode, float threshold, ColorEntry[] colorOperations, bool enableColorAdjustments, ColorAdjustments colorAdjustments) {
            if (tex == null || !(tex is Texture2D))
                return null;
            if (lastTexture == null || tex != lastTexture) {
                lastTexture = (Texture2D)tex;
                lastTexture.EnsureTextureIsReadable();
                originalColors = lastTexture.GetPixels();
            }
            return GetNearestColors(originalColors, colorMatchMode, threshold, colorOperations, enableColorAdjustments, colorAdjustments);
        }

        const int MAX_THREADS = 4;
        readonly static FastHashSet<Color>[] matchPool = new FastHashSet<Color>[MAX_THREADS];

        public Color[] GetNearestColors(Color[] originalColors, ColorMatchMode colorMatchMode, float threshold, ColorEntry[] colorOperations, bool enableColorAdjustments, ColorAdjustments colorAdjustments) {
            if (originalColors == null) return null;
            int len = originalColors.Length;
            Color[] newColors = new Color[len];
            Color[] palette = BuildPaletteColors();

            for (int k = 0; k < MAX_THREADS; k++) {
                if (matchPool[k] == null) {
                    matchPool[k] = new FastHashSet<Color>();
                }
                matchPool[k].Clear();
            }

            int threadCount = Mathf.Clamp(Mathf.CeilToInt((float)len / MAX_THREADS), 1, MAX_THREADS);
            Parallel.For(0, threadCount, index => {
                int start = len * index / threadCount;
                int end = (index == threadCount - 1) ? len : len * (index + 1) / threadCount;
                int lastHash = -1;
                Color nearest = Color.white;
                FastHashSet<Color> thisMatch = matchPool[index];
                for (int k = start; k < end; k++) {
                    int r = (int)(originalColors[k].r * 255);
                    int g = (int)(originalColors[k].g * 255);
                    int b = (int)(originalColors[k].b * 255);
                    int colorHash = (r << 16) + (g << 8) + b;

                    if (colorHash != lastHash) {
                        lastHash = colorHash;
                        if (!thisMatch.TryGetValue(colorHash, out nearest)) {
                            nearest = GetNearestColor(palette, originalColors[k], colorMatchMode, threshold, colorOperations, enableColorAdjustments, colorAdjustments);
                            thisMatch.Add(colorHash, nearest);
                        }
                    }
                    nearest.a = originalColors[k].a;
                    newColors[k] = nearest;
                }
            });

            return newColors;
        }

        /// <summary>
        /// Returns a texture with colors that match the current palette
        /// </summary>
        public Texture2D GetNearestTexture(Texture tex, ColorMatchMode colorMatchMode) {
            return GetNearestTexture(tex, colorMatchMode, 0, null, false, ColorAdjustments.None);
        }


        /// <summary>
        /// Returns a texture with colors that match the current palette
        /// </summary>
        public Texture2D GetNearestTexture(Texture tex, ColorMatchMode colorMatchMode, float threshold, ColorEntry[] colorOperations, bool enableColorAdjustments, ColorAdjustments colorAdjustments) {
            Color[] newColors = GetNearestColors(tex, colorMatchMode, threshold, colorOperations, enableColorAdjustments, colorAdjustments);
            if (newColors == null)
                return null;
            Texture2D texture = (Texture2D)tex;
            Texture2D t = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
            t.name = texture.name + Recolor.CS_NAME_SUFFIX;
            t.filterMode = tex.filterMode;
            t.wrapMode = tex.wrapMode;
#if UNITY_EDITOR
            t.hideFlags = HideFlags.None;
            t.alphaIsTransparency = texture.alphaIsTransparency;
#endif
            t.SetPixels(newColors);
            t.Apply();
            return t;
        }

        public void DeleteKeyColor(int index) {
            keyColors[index].visible = false;
            // Pack
            for (int k = index; k < keyColors.Length - 1; k++) {
                keyColors[k] = keyColors[k + 1];
            }
        }


    }

}

using UnityEngine;

namespace ColorStudio {

    public static class ColorConversion {

        public static Color GetColorFromRGBSL(float R, float G, float B, float S, float L) {
            float C = (1 - Mathf.Abs(2f * L - 1)) * S;
            R = (R - 0.5f) * C + L;
            G = (G - 0.5f) * C + L;
            B = (B - 0.5f) * C + L;
            return new Color(R, G, B, 1);

        }

        public static Color GetColor(float H) {
            float R = Mathf.Abs(H * 6 - 3) - 1;
            float G = 2 - Mathf.Abs(H * 6 - 2);
            float B = 2 - Mathf.Abs(H * 6 - 4);
            if (R < 0) {
                R = 0;
            } else if (R > 1f) {
                R = 1f;
            }
            if (G < 0) {
                G = 0;
            } else if (G > 1f) {
                G = 1f;
            }
            if (B < 0) {
                B = 0;
            } else if (B > 1f) {
                B = 1f;
            }
            return new Color(R, G, B, 1);
        }


        public static Color GetColorFromHSL(float H, float S, float L) {
            float R = Mathf.Abs(H * 6 - 3) - 1;
            float G = 2 - Mathf.Abs(H * 6 - 2);
            float B = 2 - Mathf.Abs(H * 6 - 4);
            if (R < 0) {
                R = 0;
            } else if (R > 1f) {
                R = 1f;
            }
            if (G < 0) {
                G = 0;
            } else if (G > 1f) {
                G = 1f;
            }
            if (B < 0) {
                B = 0;
            } else if (B > 1f) {
                B = 1f;
            }
            float C = (1 - Mathf.Abs(2f * L - 1)) * S;
            R = (R - 0.5f) * C + L;
            G = (G - 0.5f) * C + L;
            B = (B - 0.5f) * C + L;
            return new Color(R, G, B, 1);
        }

        public static HSLColor GetHSLFromRGB(float r, float g, float b) {
            float max = Mathf.Max(r, g, b);
            float min = Mathf.Min(r, g, b);
            float h, s, l = (max + min) / 2;

            float d = max - min;
            if (d == 0) return new HSLColor { h = 0, s = 0, l = l };

            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            if (max == r) {
                h = (g - b) / d + (g < b ? 6 : 0);
            } else if (max == g) {
                h = (b - r) / d + 2;
            } else {
                h = (r - g) / d + 4;
            }

            h /= 6;

            return new HSLColor { h = h, s = s, l = l };
        }


        public static float GetHue(float r, float g, float b) {

            float max = Mathf.Max(r, g, b);
            float min = Mathf.Min(r, g, b);
            float h;

            float d = max - min;
            if (d == 0) return 0;

            if (max == r) {
                h = (g - b) / d + (g < b ? 6 : 0);
            } else if (max == g) {
                h = (b - r) / d + 2;
            } else {
                h = (r - g) / d + 4;
            }

            h /= 6;
            if (h < 0) h += 1f;

            return h;
        }


        public static float GetSaturation(float r, float g, float b) {
            float max = Mathf.Max(r, g, b);
            float min = Mathf.Min(r, g, b);
            float s, l = (max + min) / 2;

            float d = max - min;
            if (d == 0) return 0;

            s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
            return s;
        }


        public static float GetLightness(float r, float g, float b) {
            float max = Mathf.Max(r, g, b);
            float min = Mathf.Min(r, g, b);
            return (max + min) / 2;
        }

        public static float GetLuma(this Color color) {
            return 0.299f * color.r + 0.587f * color.g + 0.114f * color.b;
        }

        public static bool GetColorFromHex(string hex, out Color color) {
            return ColorUtility.TryParseHtmlString(hex, out color);
        }

    }




}

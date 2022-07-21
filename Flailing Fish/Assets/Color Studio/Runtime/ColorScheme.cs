using UnityEngine;

namespace ColorStudio {

    public enum ColorScheme {
        Monochromatic,
        Complementary,
        Gradient,
        Analogous,
        SplitComplementary,
        AccentedAnalogous,
        Triadic,
        Tetradic,
        Square,
        Spectrum,
        Custom
    }

    public enum KeyAdjustment {
        Fixed = 0,
        RotatePrimary = 1,
        RotateComplementary = 2,
        RotateComplementaryInverted = 3
    }

    public static class ColorSchemeExtensions {

        public static int minHues(this ColorScheme scheme) {
            if (scheme == ColorScheme.Custom) {
                return 0;
            } else if (scheme == ColorScheme.Monochromatic) {
                return 1;
            } else if (scheme == ColorScheme.Complementary || scheme == ColorScheme.Gradient) {
                return 2;
            } else if (scheme == ColorScheme.SplitComplementary || scheme == ColorScheme.Analogous || scheme == ColorScheme.Triadic) {
                return 3;
            } else if (scheme == ColorScheme.AccentedAnalogous || scheme == ColorScheme.Tetradic || scheme == ColorScheme.Square) {
                return 4;
            } else if (scheme == ColorScheme.Spectrum) {
                return 8;
            }
            return 1;
        }

        public static int recommendedHues(this ColorScheme scheme) {
            if (scheme == ColorScheme.Custom) {
                return 0;
            } else if (scheme == ColorScheme.Monochromatic) {
                return 1;
            } else if (scheme == ColorScheme.Complementary) {
                return 2;
            } else if (scheme == ColorScheme.SplitComplementary || scheme == ColorScheme.Analogous || scheme == ColorScheme.Triadic) {
                return 3;
            } else if (scheme == ColorScheme.AccentedAnalogous || scheme == ColorScheme.Tetradic || scheme == ColorScheme.Square) {
                return 4;
            } else if (scheme == ColorScheme.Spectrum || scheme == ColorScheme.Gradient) {
                return 8;
            }
            return 1;
        }


        public static bool customSplit(this ColorScheme scheme) {
            return scheme == ColorScheme.SplitComplementary || scheme == ColorScheme.Analogous || scheme == ColorScheme.Tetradic || scheme == ColorScheme.AccentedAnalogous;
        }

        public static KeyAdjustment keyAdjustment(this ColorScheme scheme, int keyIndex) {
            switch (scheme) {
                case ColorScheme.Complementary:
                    return keyIndex == 0 ? KeyAdjustment.RotatePrimary : KeyAdjustment.RotatePrimary;
                case ColorScheme.Gradient:
                    return KeyAdjustment.RotateComplementary;
                case ColorScheme.Analogous:
                case ColorScheme.SplitComplementary:
                    if (keyIndex == 0) {
                        return KeyAdjustment.RotatePrimary;
                    } else if (keyIndex == 1) {
                        return KeyAdjustment.RotateComplementary;
                    } else {
                        return KeyAdjustment.RotateComplementaryInverted;
                    }

                case ColorScheme.AccentedAnalogous:
                    if (keyIndex == 0) {
                        return KeyAdjustment.RotatePrimary;
                    } else if (keyIndex == 1) {
                        return KeyAdjustment.RotateComplementary;
                    } else if (keyIndex == 3) {
                        return KeyAdjustment.RotateComplementaryInverted;
                    } else {
                        return KeyAdjustment.RotatePrimary;
                    }
                case ColorScheme.Triadic:
                    return KeyAdjustment.RotatePrimary;
                case ColorScheme.Tetradic:
                    if (keyIndex == 0) {
                        return KeyAdjustment.RotatePrimary;
                    } else if (keyIndex == 1) {
                        return KeyAdjustment.RotateComplementary;
                    } else if (keyIndex == 3) {
                        return KeyAdjustment.RotateComplementary;
                    } else {
                        return KeyAdjustment.RotatePrimary;
                    }
                case ColorScheme.Square:
                    return KeyAdjustment.RotatePrimary;
            }

            return KeyAdjustment.Fixed;
        }

    }




}

using System;
using UnityEngine;

namespace ColorStudio {

    [Serializable]
    public struct ColorAdjustments {
        public bool applyLUT;
        [Tooltip("LUT textures must be of 1024x32 dimensions.\nEnsure the following import settings are set in your LUT textures:\n- Uncheck sRGB Texture (no gamma conversion)\n- No compression\n- Disable mip mapping\n- Aniso set to 0\n- Filtering set to Bilinear\n- Wrapping set to Clamp")]
        public Texture2D LUT;
        [Range(-1, 5f)]
        public float brightness;
        [Range(-1f, 2f)]
        public float contrast;
        public float vibrance;
        [Range(0, 1f)]
        public float tintAmount;
        public Color tintColor;

        public static ColorAdjustments None = new ColorAdjustments();
    }


}

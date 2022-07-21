using System;
using UnityEngine;

namespace ColorStudio {

    public enum ColorOperation {
        Preserve,
        Replace,
    }

    [Serializable]
    public struct ColorEntry {
        public Color color;
        public ColorOperation operation;
        public Color replaceColor;
    }


}

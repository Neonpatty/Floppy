using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace ColorStudio {

    public static class SerializedPropertyExtensions {

        /// <summary>
        /// Returns the index of this property in the array
        /// </summary>
        public static int GetArrayIndex(this SerializedProperty property) {
            string s = property.propertyPath;
            int bracket = s.LastIndexOf("[");
            if (bracket >= 0) {
                string indexStr = s.Substring(bracket + 1, s.Length - bracket - 2);
                int index;
                if (int.TryParse(indexStr, out index)) {
                    return index;
                }
            }
            return 0;
        }
    }
}
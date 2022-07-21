/* Color Studio by Ramiro Oliva (Kronnect)   /
/  Premium assets for Unity on kronnect.com */


using System.IO;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Globalization;

namespace ColorStudio {

    [CustomEditor(typeof(CSPalette))]
    public class CSPaletteEditor : Editor {

        public override bool UseDefaultMargins() {
            return false;
        }

        CSPalette palette;

        private void OnEnable() {
            palette = (CSPalette)target;
            palette.UpdateMaterial();
        }

        public override void OnInspectorGUI() {

            if (palette == null) return;

            if (palette.material == null || palette.material.GetColorArray("_Colors") == null) {
                palette.UpdateMaterial();
            }

            EditorGUILayout.BeginVertical(GUI.skin.box);

            Rect space = EditorGUILayout.BeginVertical();
            float paletteRowSize;
            paletteRowSize = 64;
            GUILayout.Space(paletteRowSize);
            EditorGUILayout.EndVertical();

            palette.material.SetVector("_CursorPos", Vector3.left);
            EditorGUI.DrawPreviewTexture(space, Texture2D.whiteTexture, palette.material);

            EditorGUILayout.EndVertical();

            if (GUILayout.Button("Open in Color Studio")) {
                CSWindow cs = CSWindow.ShowWindow();
                cs.LoadPalette(palette);
            }
        }
    }
}


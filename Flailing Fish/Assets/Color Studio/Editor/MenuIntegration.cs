using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace ColorStudio {
    public class MenuIntegration : MonoBehaviour {

        [MenuItem("Window/Color Studio/Palette Manager", false, 200)]
        static void OpenColorStudio(MenuCommand command) {
            CSWindow.ShowWindow();
        }
        [MenuItem("Window/Color Studio/Pixel Painter", false, 201)]
        static void OpenPixelPainter(MenuCommand command) {
            PPWindow.ShowWindow();
        }

    }
}

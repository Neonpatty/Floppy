/* Pixel Painter by Ramiro Oliva (Kronnect)   /
/  Premium assets for Unity on kronnect.com */

using System.IO;
using UnityEngine;
using UnityEditor;

namespace ColorStudio {


    public partial class PPWindow : EditorWindow {

        Color floodFillRefereceColor;
        bool floodFillChanges;

        void CreateNew(int newWidth, int newHeight) {
            customWidth = width = newWidth;
            customHeight = height = newHeight;
            texture = null;
            sprite = null;
            canvasTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            canvasTexture.filterMode = FilterMode.Point;
            ReadColors();
            Color trans = new Color(0, 0, 0, 0);
            for (int k = 0; k < colors.Length; k++) {
                colors[k] = trans;
            }
            canvasTexture.SetPixels(colors);
            canvasTexture.Apply();
        }

        void SetSize(int newWidth, int newHeight) {
            customWidth = width = newWidth;
            customHeight = height = newHeight;
            Scale(canvasTexture, width, height, FilterMode.Point);
            ReadColors();
        }

        void Scale(Texture2D tex, int width, int height, FilterMode mode = FilterMode.Trilinear) {

            if (tex.width == width && tex.height == height) return;

            RenderTexture currentActiveRT = RenderTexture.active;

            Rect texR = new Rect(0, 0, width, height);

            tex.filterMode = mode;
            tex.Apply(true);

            RenderTexture rtt = RenderTexture.GetTemporary(width, height, 0);

            //Set the RTT in order to render to it
            Graphics.SetRenderTarget(rtt);
            Graphics.Blit(tex, rtt);

            // Update new texture
            tex.Resize(width, height, TextureFormat.ARGB32, false);
            tex.ReadPixels(texR, 0, 0, true);
            tex.Apply(true);

            RenderTexture.active = currentActiveRT;
            RenderTexture.ReleaseTemporary(rtt);
        }


        void PaintPixel() {
            bool changes = PaintPixelOne(currentTexelPos.x, currentTexelPos.y, false);
            switch (_mirrorMode) {
                case MirrorMode.Horizontal:
                    changes = PaintPixelOne(width - 1 - currentTexelPos.x, currentTexelPos.y, changes);
                    break;
                case MirrorMode.Vertical:
                    changes = PaintPixelOne(currentTexelPos.x, height - 1 - currentTexelPos.y, changes);
                    break;
                case MirrorMode.Quad:
                    changes = PaintPixelOne(width - 1 - currentTexelPos.x, currentTexelPos.y, changes);
                    changes = PaintPixelOne(currentTexelPos.x, height - 1 - currentTexelPos.y, changes);
                    changes = PaintPixelOne(width - 1 - currentTexelPos.x, height - 1 - currentTexelPos.y, changes);
                    break;
            }
            if (changes) {
                UpdateCanvasTexture();
                PostPaintPixel();
            }
        }


        bool PaintPixelOne(int cursorPosX, int cursorPosY, bool changes) {
            int brushWidth = brushPixelWidth;
            int offset = -brushWidth / 2;
            for (int py = 0; py < brushWidth; py++) {
                int y = cursorPosY + offset + py;
                if (y < 0 || y >= height) continue;
                for (int px = 0; px < brushWidth; px++) {
                    int x = cursorPosX + offset + px;
                    if (x < 0 || x >= width) continue;
                    if (!BrushShapeTest(x, y, px, py)) continue;
                    int colorIndex = y * width + x;
                    Color newColor = GetTransformedColor(colors[colorIndex]);
                    if (colors[colorIndex] != newColor) {
                        if (!changes) {
                            changes = true;
                            Undo.RegisterCompleteObjectUndo(this, currentBrush.opDescription());
                        }
                        colors[colorIndex] = newColor;
                    }
                }
            }
            return changes;
        }

        bool BrushShapeTest(int x, int y, int px, int py) {
            switch (_brushShape) {
                case BRUSH_SHAPE_CIRCLE:
                    if (_brushWidth < 2) return true;
                    return !(px == 0 && py == 0 || px == _brushWidth && py == 0 || px == 0 && py == _brushWidth || px == _brushWidth && py == _brushWidth);
                case BRUSH_SHAPE_DITHER_1:
                    return (x + y) % 2 == 1;
                case BRUSH_SHAPE_DITHER_2:
                    return (x + y) % 2 == 0;
                case BRUSH_SHAPE_CROSS:
                    return px == _brushWidth / 2 || py == _brushWidth / 2;
                case BRUSH_SHAPE_X:
                    return px == py || px == _brushWidth - py;
            }
            return true;
        }

        Color GetTransformedColor(Color originalColor) {
            switch (currentBrush) {
                case Brush.Darken: {
                        if (originalColor.a == 0) return originalColor;
                        HSLColor hsl = ColorConversion.GetHSLFromRGB(originalColor.r, originalColor.g, originalColor.b);
                        hsl.l -= 0.05f;
                        if (hsl.l < 0.01f) hsl.l = 0.01f;
                        return ColorConversion.GetColorFromHSL(hsl.h, hsl.s, hsl.l);
                    }
                case Brush.Lighten: {
                        if (originalColor.a == 0) return originalColor;
                        HSLColor hsl = ColorConversion.GetHSLFromRGB(originalColor.r, originalColor.g, originalColor.b);
                        hsl.l += 0.05f;
                        if (hsl.l > 0.99f) hsl.l = 0.99f;
                        return ColorConversion.GetColorFromHSL(hsl.h, hsl.s, hsl.l);
                    }
                case Brush.Dry: {
                        if (originalColor.a == 0) return originalColor;
                        HSLColor hsl = ColorConversion.GetHSLFromRGB(originalColor.r, originalColor.g, originalColor.b);
                        hsl.s -= 0.05f;
                        if (hsl.s < 0.01f) hsl.s = 0.01f;
                        return ColorConversion.GetColorFromHSL(hsl.h, hsl.s, hsl.l);
                    }
                case Brush.Vivid: {
                        if (originalColor.a == 0) return originalColor;
                        HSLColor hsl = ColorConversion.GetHSLFromRGB(originalColor.r, originalColor.g, originalColor.b);
                        hsl.s += 0.05f;
                        if (hsl.s > 0.99f) hsl.s = 0.99f;
                        return ColorConversion.GetColorFromHSL(hsl.h, hsl.s, hsl.l);
                    }
                case Brush.Noise: {
                        if (originalColor.a == 0) return originalColor;
                        HSLColor hsl = ColorConversion.GetHSLFromRGB(originalColor.r, originalColor.g, originalColor.b);
                        hsl.l += Random.value * 0.1f - 0.05f;
                        if (hsl.l > 0.99f) hsl.l = 0.99f; else if (hsl.l < 0.01f) hsl.l = 0.01f;
                        return ColorConversion.GetColorFromHSL(hsl.h, hsl.s, hsl.l);
                    }
                case Brush.NoiseTone: {
                        if (originalColor.a == 0) return originalColor;
                        HSLColor hsl = ColorConversion.GetHSLFromRGB(originalColor.r, originalColor.g, originalColor.b);
                        hsl.h += Random.value * 0.1f - 0.05f;
                        if (hsl.h > 0.99f) hsl.l = 0.99f; else if (hsl.h < 0.01f) hsl.h = 0.01f;
                        return ColorConversion.GetColorFromHSL(hsl.h, hsl.s, hsl.l);
                    }
                case Brush.Gradient: {
                        HSLColor hsl = ColorConversion.GetHSLFromRGB(_brushColor.r, _brushColor.g, _brushColor.b);
                        int distance = Mathf.Max(Mathf.Abs(startTexelPos.x - currentTexelPos.x), Mathf.Abs(startTexelPos.y - currentTexelPos.y));
                        if (distance > 0) {
                            hsl.l -= 0.05f * distance;
                            if (hsl.l < 0.01f) hsl.l = 0.01f;
                            return ColorConversion.GetColorFromHSL(hsl.h, hsl.s, hsl.l);

                        }
                        return _brushColor;
                    }


            }
            return _brushColor;
        }


        void ClearAll() {
            Undo.RegisterCompleteObjectUndo(this, "Clear All");
            FillWithColor(new Color(0, 0, 0, 0));
            UpdateCanvasTexture();
        }

        void FillAll() {
            Undo.RegisterCompleteObjectUndo(this, "Fill All");
            FillWithColor(_brushColor);
            UpdateCanvasTexture();
        }

        void FillWithColor(Color color) {
            for (int k = 0; k < colors.Length; k++) {
                colors[k] = color;
            }
            UpdateCanvasTexture();
        }


        void ReplaceColors() {
            Color pixelColor = GetCursorColor();
            bool changes = false;
            for (int k = 0; k < colors.Length; k++) {
                if (!changes) {
                    changes = true;
                    Undo.RegisterCompleteObjectUndo(this, "Replace color");
                }
                if (colors[k] == pixelColor) {
                    colors[k] = _brushColor;
                }
            }
            if (changes) {
                UpdateCanvasTexture();
            }
        }


        void FloodFill() {
            floodFillRefereceColor = GetCursorColor();
            if (_brushColor == floodFillRefereceColor) return;
            floodFillChanges = false;
            FloodFillRecursive(currentTexelPos.x, currentTexelPos.y);
            if (floodFillChanges) {
                UpdateCanvasTexture();
            }
        }

        void FloodFillRecursive(int x, int y) {
            int colorIndex = y * width + x;
            if (colors[colorIndex] == floodFillRefereceColor) {
                if (!floodFillChanges) {
                    floodFillChanges = true;
                    Undo.RegisterCompleteObjectUndo(this, "Flood Fill");
                }
                colors[colorIndex] = _brushColor;
                if (x > 0) FloodFillRecursive(x - 1, y);
                if (x < width - 1) FloodFillRecursive(x + 1, y);
                if (y < height - 1) FloodFillRecursive(x, y + 1);
                if (y > 0) FloodFillRecursive(x, y - 1);
            }
        }

        void FitPalette() {
            Undo.RegisterCompleteObjectUndo(this, "Fit Palette");
            for (int k = 0; k < colors.Length; k++) {
                if (colors[k].a > 0) {
                    colors[k] = CSWindow.palette.GetNearestColor(colors[k], ColorMatchMode.RGB);
                }
            }
            UpdateCanvasTexture();
        }

        void UpdateCanvasTexture() {
            canvasTexture.SetPixels(colors);
            canvasTexture.Apply();
        }

        Color GetCursorColor() {
            int colorIndex = currentTexelPos.y * width + currentTexelPos.x;
            if (colorIndex < 0 || colorIndex >= colors.Length) {
                return _brushColor;
            }
            return colors[colorIndex];
        }

        void FlipHoriz() {
            Color[] newColors = new Color[width * height];
            for (int colorIndex = 0, y = 0; y < height; y++) {
                for (int x = 0; x < width; x++, colorIndex++) {
                    newColors[colorIndex] = colors[y * width + width - 1 - x];
                }
            }
            colors = newColors;
            UpdateCanvasTexture();
        }

        void FlipVert() {
            Color[] newColors = new Color[width * height];
            for (int colorIndex = 0, y = 0; y < height; y++) {
                for (int x = 0; x < width; x++, colorIndex++) {
                    newColors[colorIndex] = colors[(height - 1 - y) * width + x];
                }
            }
            colors = newColors;
            UpdateCanvasTexture();
        }

        void Displace(int dirX, int dirY) {
            Color[] newColors = new Color[width * height];
            Color trans = new Color(0, 0, 0, 0);
            for (int colorIndex = 0, y = 0; y < height; y++) {
                int y0 = y - dirY;
                for (int x = 0; x < width; x++, colorIndex++) {
                    int x0 = x - dirX;
                    if (y0 < 0 || y0 >= height || x0 < 0 || x0 >= width) {
                        newColors[colorIndex] = trans;
                    } else {
                        newColors[colorIndex] = colors[y0 * width + x0];
                    }
                }
            }
            colors = newColors;
            UpdateCanvasTexture();
        }

        void RotateLeft() {
            Color[] newColors = new Color[width * height];
            int aux = width;
            width = height;
            height = aux;
            for (int colorIndex = 0, y = 0; y < height; y++) {
                for (int x = 0; x < width; x++, colorIndex++) {
                    newColors[colorIndex] = colors[(height - x - 1) * height + y];
                }
            }
            colors = newColors;
            UpdateCanvasTexture();
        }

        void RotateRight() {
            Color[] newColors = new Color[width * height];
            int aux = width;
            width = height;
            height = aux;
            for (int colorIndex = 0, y = 0; y < height; y++) {
                for (int x = 0; x < width; x++, colorIndex++) {
                    newColors[colorIndex] = colors[x * height + (width - 1 - y)];
                }
            }
            colors = newColors;
            UpdateCanvasTexture();
        }

    }

}

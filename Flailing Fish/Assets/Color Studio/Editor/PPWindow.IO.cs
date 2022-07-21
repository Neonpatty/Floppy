/* Pixel Painter by Ramiro Oliva (Kronnect)   /
/  Premium assets for Unity on kronnect.com */

using System.IO;
using UnityEngine;
using UnityEditor;

namespace ColorStudio {


    public partial class PPWindow : EditorWindow {

        #region IO functions

        void LoadTexture(Texture2D texture) {
            if (texture == null) return;
            texture.EnsureTextureCanBeEdited();
            width = texture.width;
            height = texture.height;
            canvasTexture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            canvasTexture.filterMode = FilterMode.Point;
            canvasTexture.wrapMode = TextureWrapMode.Clamp;
            Color[] colors = texture.GetPixels();
            // Apply gamma correction to linear texture
            if (texture.IsLinear()) {
                for (int k = 0; k < colors.Length; k++) {
                    colors[k] = colors[k].gamma;
                }
            }
            canvasTexture.SetPixels(colors);
            canvasTexture.Apply();
        }

        void LoadFile() {
            if (sprite != null) {
                Texture2D subTex = GetSpriteTexture();
                LoadTexture(subTex);
            } else if (texture != null) {
                LoadTexture(texture);
            } 
        }

        string GetExportsPath(string subfolder) {
            string path = "Assets/Color Studio/" + subfolder;
            Directory.CreateDirectory(path);
            return path;
        }

        void SaveTexture(TextureImporterType type) {
            if (sprite != null) {
                SaveSpriteTexture();
                return;
            }
            if (texture != null) {
                if (texture.width != canvasTexture.width || texture.height != canvasTexture.width) {
                    texture.Resize(canvasTexture.width, canvasTexture.height);
                }
                // convert to linear
                if (texture.IsLinear()) {
                    Color[] linearColors = new Color[colors.Length];
                    for (int k = 0; k < colors.Length; k++) {
                        linearColors[k] = colors[k].linear;
                    }
                    texture.SetPixels(linearColors);
                } else {
                    texture.SetPixels(colors);
                }
                texture.Apply();
                UpdateTextureContentsOnDisk(texture);
                return;
            }
            string basePath = GetExportsPath("Textures");
            string path = basePath + "/texture.png";
            int counter = 2;
            while (File.Exists(path)) {
                path = basePath + "/texture" + counter + ".png";
                counter++;
                if (counter > 1000) return;
            }
            byte[] contents = canvasTexture.EncodeToPNG();
            File.WriteAllBytes(path, contents);
            AssetDatabase.ImportAsset(path);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(path) as TextureImporter;
            imp.textureType = type;
            imp.filterMode = FilterMode.Point;
            imp.alphaIsTransparency = true;
            imp.isReadable = true;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();

            if (type == TextureImporterType.Default) {
                texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path) as Texture2D;
                EditorGUIUtility.PingObject(texture);
                sprite = null;
            } else {
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path) as Sprite;
                EditorGUIUtility.PingObject(sprite);
                texture = null;
            }

        }


        Texture2D GetSpriteTexture() {
            int w = sprite.texture.width;
            int h = sprite.texture.height;
            int tw = (int)sprite.rect.width;
            int th = (int)sprite.rect.height;
            if (w == tw && h == th) return sprite.texture;

            int x0 = (int)sprite.rect.x;
            int y0 = (int)sprite.rect.y;

            sprite.texture.EnsureTextureCanBeEdited();
            Texture2D tex = new Texture2D(tw, th, TextureFormat.ARGB32, false);
            Color[] spriteColors = sprite.texture.GetPixels();
            Color[] texColors = tex.GetPixels();
            for (int colorIndex = 0, y = 0; y < th; y++) {
                for (int x = 0; x < tw; x++, colorIndex++) {
                    texColors[colorIndex] = spriteColors[(y + y0) * w + x + x0];
                }
            }
            tex.SetPixels(texColors);
            tex.Apply();
            return tex;
        }

        void SaveSpriteTexture() {
            int w = sprite.texture.width;
            int h = sprite.texture.height;
            int tw = (int)sprite.rect.width;
            int th = (int)sprite.rect.height;
            if (w == tw && h == th)
            {
                sprite.texture.SetPixels(colors);
                sprite.texture.Apply();
            }
            else
            {
                int x0 = (int)sprite.rect.x;
                int y0 = (int)sprite.rect.y;

                Color[] spriteColors = sprite.texture.GetPixels();
                for (int colorIndex = 0, y = 0; y < th; y++)
                {
                    for (int x = 0; x < tw; x++, colorIndex++)
                    {
                        spriteColors[(y + y0) * w + x + x0] = colors[colorIndex];
                    }
                }
                sprite.texture.SetPixels(spriteColors);
                sprite.texture.Apply();
            }
            UpdateTextureContentsOnDisk(sprite.texture);
            EditorUtility.SetDirty(sprite);
            AssetDatabase.SaveAssets();
        }

        private void UpdateTextureContentsOnDisk(Texture2D texture) {
            if (texture == null) return;
            string path = AssetDatabase.GetAssetPath(texture);
            if (string.IsNullOrEmpty(path)) return;
            byte[] contents = texture.EncodeToPNG();
            File.WriteAllBytes(path, contents);
            AssetDatabase.ImportAsset(path);
        }

        #endregion

    }


}




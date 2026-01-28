using UnityEngine;
using UnityEditor;
using CollapseGame.Core;

namespace CollapseGame.Editor
{
    public class SpriteGenerator : MonoBehaviour
    {
#if UNITY_EDITOR
        [MenuItem("CollapseGame/Generate Default Sprites")]
        public static void GenerateDefaultSprites()
        {
            string path = "Assets/Sprites/";
            
            if (!AssetDatabase.IsValidFolder(path.TrimEnd('/')))
            {
                AssetDatabase.CreateFolder("Assets", "Sprites");
            }

            Color[] colors = new Color[]
            {
                new Color(0.9f, 0.2f, 0.2f),
                new Color(0.2f, 0.5f, 0.9f),
                new Color(0.2f, 0.8f, 0.3f),
                new Color(0.95f, 0.85f, 0.2f),
                new Color(0.7f, 0.3f, 0.8f),
                new Color(1f, 0.4f, 0.7f)
            };

            string[] names = { "Blue", "Red", "Green", "Yellow", "Purple", "Pink" };

            for (int i = 0; i < colors.Length; i++)
            {
                CreateBlockSprite(path, names[i], colors[i]);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateBlockSprite(string path, string name, Color color)
        {
            int size = 128;
            Texture2D texture = new Texture2D(size, size);
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    int border = 10;
                    bool isInside = x >= border && x < size - border && 
                                   y >= border && y < size - border;
                    
                    if (isInside)
                    {
                        float gradient = 1f - (y / (float)size) * 0.2f;
                        Color pixelColor = color * gradient;
                        pixelColor.a = 1f;
                        texture.SetPixel(x, y, pixelColor);
                    }
                    else
                    {
                        texture.SetPixel(x, y, Color.clear);
                    }
                }
            }

            texture.Apply();
            
            byte[] bytes = texture.EncodeToPNG();
            string filePath = path + "Block_" + name + ".png";
            System.IO.File.WriteAllBytes(filePath, bytes);
        }
#endif
    }
}

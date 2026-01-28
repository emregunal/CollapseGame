using UnityEngine;
using UnityEditor;
using CollapseGame.Core;
using CollapseGame.Managers;
using CollapseGame.Pooling;

namespace CollapseGame.Editor
{
    public class SceneSetupWizard : EditorWindow
    {
        [MenuItem("CollapseGame/Scene Setup Wizard")]
        public static void ShowWindow()
        {
            GetWindow<SceneSetupWizard>("Scene Setup");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Collapse Game Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (GUILayout.Button("Create Game Structure"))
            {
                CreateGameStructure();
            }

            EditorGUILayout.Space();

            if (GUILayout.Button("Create ScriptableObject Assets"))
            {
                CreateScriptableObjects();
            }
        }

        private void CreateGameStructure()
        {
            GameObject gameManager = new GameObject("GameManager");
            gameManager.AddComponent<GameManager>();

            GameObject board = new GameObject("Board");
            board.AddComponent<BoardManager>();

            GameObject pool = new GameObject("BlockPool");
            pool.AddComponent<BlockPool>();

            GameObject ui = new GameObject("UI");
            ui.AddComponent<UIManager>();

            Camera.main.gameObject.AddComponent<CameraController>();
        }

        private void CreateScriptableObjects()
        {
            string configPath = "Assets/ScriptableObjects/";
            
            if (!AssetDatabase.IsValidFolder(configPath.TrimEnd('/')))
            {
                AssetDatabase.CreateFolder("Assets", "ScriptableObjects");
            }

            GameConfig config = ScriptableObject.CreateInstance<GameConfig>();
            AssetDatabase.CreateAsset(config, configPath + "GameConfig.asset");

            string[] colorNames = { "Blue", "Red", "Green", "Yellow", "Purple", "Pink" };
            BlockColor[] colors = { 
                BlockColor.Red, BlockColor.Blue, BlockColor.Green, 
                BlockColor.Yellow, BlockColor.Purple, BlockColor.Pink 
            };
            
            for (int i = 0; i < colorNames.Length; i++)
            {
                BlockData data = ScriptableObject.CreateInstance<BlockData>();
                data.colorName = colorNames[i];
                data.color = colors[i];
                AssetDatabase.CreateAsset(data, configPath + "BlockData_" + colorNames[i] + ".asset");
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}

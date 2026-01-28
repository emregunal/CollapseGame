using UnityEngine;
using UnityEditor;
using CollapseGame.Core;

namespace CollapseGame.Editor
{
    [CustomEditor(typeof(GameConfig))]
    public class GameConfigEditor : UnityEditor.Editor
    {
        private SerializedProperty rows;
        private SerializedProperty columns;
        private SerializedProperty colorCount;
        private SerializedProperty minGroupSize;
        private SerializedProperty thresholdA;
        private SerializedProperty thresholdB;
        private SerializedProperty thresholdC;
        private SerializedProperty fallSpeed;
        private SerializedProperty popDuration;
        private SerializedProperty popDelay;
        private SerializedProperty poolSizeMultiplier;
        private SerializedProperty blockSpacing;

        private void OnEnable()
        {
            rows = serializedObject.FindProperty("rows");
            columns = serializedObject.FindProperty("columns");
            colorCount = serializedObject.FindProperty("colorCount");
            minGroupSize = serializedObject.FindProperty("minGroupSize");
            thresholdA = serializedObject.FindProperty("thresholdA");
            thresholdB = serializedObject.FindProperty("thresholdB");
            thresholdC = serializedObject.FindProperty("thresholdC");
            fallSpeed = serializedObject.FindProperty("fallSpeed");
            popDuration = serializedObject.FindProperty("popDuration");
            popDelay = serializedObject.FindProperty("popDelay");
            poolSizeMultiplier = serializedObject.FindProperty("poolSizeMultiplier");
            blockSpacing = serializedObject.FindProperty("blockSpacing");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.LabelField("Board Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(rows);
            EditorGUILayout.PropertyField(columns);
            EditorGUILayout.PropertyField(colorCount);
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gameplay Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(minGroupSize);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Icon Thresholds", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(thresholdA, new GUIContent("Threshold A"));
            EditorGUILayout.PropertyField(thresholdB, new GUIContent("Threshold B"));
            EditorGUILayout.PropertyField(thresholdC, new GUIContent("Threshold C"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Animation Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(fallSpeed);
            EditorGUILayout.PropertyField(popDuration);
            EditorGUILayout.PropertyField(popDelay);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Performance Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(poolSizeMultiplier);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Visual Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(blockSpacing);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
            int totalBlocks = rows.intValue * columns.intValue;
            int poolSize = Mathf.CeilToInt(totalBlocks * poolSizeMultiplier.floatValue);
            EditorGUILayout.LabelField($"Total Cells: {totalBlocks}");
            EditorGUILayout.LabelField($"Pool Size: {poolSize}");

            serializedObject.ApplyModifiedProperties();
        }
    }
}

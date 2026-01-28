using UnityEngine;

namespace CollapseGame.Core
{
    [CreateAssetMenu(fileName = "GameConfig", menuName = "CollapseGame/GameConfig")]
    public class GameConfig : ScriptableObject
    {
        [Header("Board Settings")]
        [Range(2, 10)]
        public int rows = 8;
        
        [Range(2, 10)]
        public int columns = 8;
        
        [Range(1, 6)]
        public int colorCount = 4;

        [Header("Gameplay Settings")]
        [Min(2)]
        public int minGroupSize = 2;

        [Header("Icon Thresholds")]
        public int thresholdA = 5;
        public int thresholdB = 8;
        public int thresholdC = 10;

        [Header("Animation Settings")]
        public float fallSpeed = 0.15f;
        public float popDuration = 0.2f;
        public float popDelay = 0.02f;

        [Header("Pool Settings")]
        public float poolSizeMultiplier = 1.5f;

        [Header("Visual Settings")]
        public float blockSpacing = 1.1f;

        public int GetIconLevel(int groupSize)
        {
            if (groupSize >= thresholdC) return 3;
            if (groupSize >= thresholdB) return 2;
            if (groupSize >= thresholdA) return 1;
            return 0;
        }

        private void OnValidate()
        {
            rows = Mathf.Clamp(rows, 2, 10);
            columns = Mathf.Clamp(columns, 2, 10);
            colorCount = Mathf.Clamp(colorCount, 1, 6);
            minGroupSize = Mathf.Max(2, minGroupSize);
            
            thresholdA = Mathf.Max(minGroupSize, thresholdA);
            thresholdB = Mathf.Max(thresholdA + 1, thresholdB);
            thresholdC = Mathf.Max(thresholdB + 1, thresholdC);
        }
    }
}

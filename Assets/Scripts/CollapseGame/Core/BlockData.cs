using UnityEngine;

namespace CollapseGame.Core
{
    [System.Serializable]
    public struct BlockSprites
    {
        public Sprite defaultIcon;
        public Sprite iconA;
        public Sprite iconB;
        public Sprite iconC;

        public Sprite GetIconForGroupSize(int groupSize, int thresholdA, int thresholdB, int thresholdC)
        {
            if (groupSize >= thresholdC && iconC != null) return iconC;
            if (groupSize >= thresholdB && iconB != null) return iconB;
            if (groupSize >= thresholdA && iconA != null) return iconA;
            return defaultIcon;
        }
    }

    [CreateAssetMenu(fileName = "NewBlockData", menuName = "CollapseGame/Block Data")]
    public class BlockData : ScriptableObject
    {
        public string colorName;
        public BlockColor color;
        public BlockSprites sprites;

        public Sprite GetIcon(int groupSize, int thresholdA, int thresholdB, int thresholdC)
        {
            return sprites.GetIconForGroupSize(groupSize, thresholdA, thresholdB, thresholdC);
        }

        public Sprite DefaultIcon => sprites.defaultIcon;
    }
}

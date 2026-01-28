using UnityEngine;
using TMPro;
using CollapseGame.Managers;

namespace CollapseGame.UI
{
    public class ScoreManager : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI scoreText;
        
        [Header("Score Settings")]
        [SerializeField] private int pointsPerBlock = 10;
        [SerializeField] private float comboMultiplier = 1.5f;
        
        private int currentScore = 0;
        private int comboCount = 0;
        
        private GridManager gridManager;

        private void Start()
        {
            gridManager = FindFirstObjectByType<GridManager>();
            
            if (gridManager != null)
            {
                gridManager.OnBlocksDestroyed += OnBlocksDestroyed;
            }
            
            UpdateScoreUI();
        }

        private void OnDestroy()
        {
            if (gridManager != null)
            {
                gridManager.OnBlocksDestroyed -= OnBlocksDestroyed;
            }
        }

        private void OnBlocksDestroyed(int blockCount)
        {
            comboCount++;
            float multiplier = 1f + (comboCount - 1) * (comboMultiplier - 1f);
            
            int points = Mathf.RoundToInt(blockCount * pointsPerBlock * multiplier);
            currentScore += points;
            
            UpdateScoreUI();
        }

        public void ResetCombo()
        {
            comboCount = 0;
        }

        public void ResetScore()
        {
            currentScore = 0;
            comboCount = 0;
            UpdateScoreUI();
        }

        private void UpdateScoreUI()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Score: {currentScore}";
            }
        }

        public int CurrentScore => currentScore;
    }
}

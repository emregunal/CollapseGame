using UnityEngine;
using CollapseGame.Core;

namespace CollapseGame.Managers
{
    public class GameManager : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private GameConfig gameConfig;
        
        [Header("References")]
        [SerializeField] private BoardManager boardManager;
        [SerializeField] private UIManager uiManager;

        private int _score;
        private int _moveCount;
        private bool _isGameActive;

        public int Score => _score;
        public int MoveCount => _moveCount;
        public bool IsGameActive => _isGameActive;

        public static GameManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            InitializeGame();
        }

        public void InitializeGame()
        {
            _score = 0;
            _moveCount = 0;
            _isGameActive = true;

            if (boardManager != null)
            {
                boardManager.OnBlocksDestroyed -= HandleBlocksDestroyed;
                boardManager.OnBlocksDestroyed += HandleBlocksDestroyed;
                
                boardManager.OnDeadlockDetected -= HandleDeadlock;
                boardManager.OnDeadlockDetected += HandleDeadlock;
                
                boardManager.OnShuffleComplete -= HandleShuffleComplete;
                boardManager.OnShuffleComplete += HandleShuffleComplete;

                boardManager.ClearBoard();
                boardManager.InitializeBoard();
            }

            UpdateUI();
        }

        public void RestartGame()
        {
            InitializeGame();
        }

        public void RestartGame(int rows, int columns, int colorCount)
        {
            if (gameConfig != null)
            {
                gameConfig.rows = Mathf.Clamp(rows, 2, 10);
                gameConfig.columns = Mathf.Clamp(columns, 2, 10);
                gameConfig.colorCount = Mathf.Clamp(colorCount, 1, 6);
            }
            InitializeGame();
        }

        private void HandleBlocksDestroyed(int count)
        {
            int baseScore = count * 10;
            int bonus = Mathf.Max(0, (count - gameConfig.minGroupSize)) * 5;
            _score += baseScore + bonus;
            _moveCount++;
            UpdateUI();
        }

        private void HandleDeadlock()
        {
            if (uiManager != null)
                uiManager.ShowMessage("No moves! Shuffling...");
        }

        private void HandleShuffleComplete()
        {
            if (uiManager != null)
                uiManager.HideMessage();
        }

        private void UpdateUI()
        {
            if (uiManager != null)
            {
                uiManager.UpdateScore(_score);
                uiManager.UpdateMoveCount(_moveCount);
            }
        }

        private void OnDestroy()
        {
            if (boardManager != null)
            {
                boardManager.OnBlocksDestroyed -= HandleBlocksDestroyed;
                boardManager.OnDeadlockDetected -= HandleDeadlock;
                boardManager.OnShuffleComplete -= HandleShuffleComplete;
            }
            if (Instance == this) Instance = null;
        }
    }
}

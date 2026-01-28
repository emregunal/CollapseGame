using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CollapseGame.Managers
{
    public class UIManager : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI moveCountText;

        [Header("Message Panel")]
        [SerializeField] private GameObject messagePanel;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("Buttons")]
        [SerializeField] private Button restartButton;

        [Header("Settings Panel")]
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Slider rowsSlider;
        [SerializeField] private Slider columnsSlider;
        [SerializeField] private Slider colorsSlider;
        [SerializeField] private TextMeshProUGUI rowsValueText;
        [SerializeField] private TextMeshProUGUI columnsValueText;
        [SerializeField] private TextMeshProUGUI colorsValueText;

        private void Start()
        {
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);

            if (rowsSlider != null)
            {
                rowsSlider.minValue = 2;
                rowsSlider.maxValue = 10;
                rowsSlider.wholeNumbers = true;
                rowsSlider.onValueChanged.AddListener(OnRowsChanged);
            }

            if (columnsSlider != null)
            {
                columnsSlider.minValue = 2;
                columnsSlider.maxValue = 10;
                columnsSlider.wholeNumbers = true;
                columnsSlider.onValueChanged.AddListener(OnColumnsChanged);
            }

            if (colorsSlider != null)
            {
                colorsSlider.minValue = 1;
                colorsSlider.maxValue = 6;
                colorsSlider.wholeNumbers = true;
                colorsSlider.onValueChanged.AddListener(OnColorsChanged);
            }

            HideMessage();
        }

        public void UpdateScore(int score)
        {
            if (scoreText != null)
                scoreText.text = $"Score: {score}";
        }

        public void UpdateMoveCount(int moves)
        {
            if (moveCountText != null)
                moveCountText.text = $"Moves: {moves}";
        }

        public void ShowMessage(string message)
        {
            if (messagePanel != null)
                messagePanel.SetActive(true);
            if (messageText != null)
                messageText.text = message;
        }

        public void HideMessage()
        {
            if (messagePanel != null)
                messagePanel.SetActive(false);
        }

        public void ToggleSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(!settingsPanel.activeSelf);
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                int rows = rowsSlider != null ? (int)rowsSlider.value : 8;
                int cols = columnsSlider != null ? (int)columnsSlider.value : 8;
                int colors = colorsSlider != null ? (int)colorsSlider.value : 4;
                
                GameManager.Instance.RestartGame(rows, cols, colors);
            }
        }

        private void OnRowsChanged(float value)
        {
            if (rowsValueText != null)
                rowsValueText.text = value.ToString("0");
        }

        private void OnColumnsChanged(float value)
        {
            if (columnsValueText != null)
                columnsValueText.text = value.ToString("0");
        }

        private void OnColorsChanged(float value)
        {
            if (colorsValueText != null)
                colorsValueText.text = value.ToString("0");
        }
    }
}

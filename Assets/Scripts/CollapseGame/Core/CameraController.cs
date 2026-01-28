using UnityEngine;

namespace CollapseGame.Core
{
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private GameConfig gameConfig;
        [SerializeField] private float padding = 1.5f;

        private Camera _camera;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
        }

        private void Start()
        {
            AdjustCamera();
        }

        public void AdjustCamera()
        {
            if (gameConfig == null || _camera == null) return;

            float boardWidth = gameConfig.columns * gameConfig.blockSpacing;
            float boardHeight = gameConfig.rows * gameConfig.blockSpacing;

            float screenRatio = (float)Screen.width / Screen.height;
            float targetRatio = boardWidth / boardHeight;

            float orthoSize;
            if (screenRatio >= targetRatio)
            {
                orthoSize = (boardHeight / 2f) + padding;
            }
            else
            {
                float differenceInSize = targetRatio / screenRatio;
                orthoSize = ((boardHeight / 2f) * differenceInSize) + padding;
            }

            _camera.orthographicSize = orthoSize;
        }
    }
}

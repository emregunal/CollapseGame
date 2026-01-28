using UnityEngine;
using System;
using CollapseGame.Pooling;

namespace CollapseGame.Core
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class Block : MonoBehaviour, IPoolable
    {
        [SerializeField] private SpriteRenderer iconRenderer;

        private BlockData _blockData;
        private BlockColor _color;
        private GridPosition _gridPosition;
        private int _currentIconLevel;
        private bool _isActive;
        private int _thresholdA;
        private int _thresholdB;
        private int _thresholdC;

        public BlockColor Color => _color;
        public GridPosition GridPosition => _gridPosition;
        public bool IsActive => _isActive;
        public BlockData Data => _blockData;

        public event Action<Block> OnBlockClicked;

        public void Initialize(BlockData data, GridPosition position, int thresholdA = 5, int thresholdB = 8, int thresholdC = 10)
        {
            _blockData = data;
            _color = data.color;
            _gridPosition = position;
            _currentIconLevel = 0;
            _isActive = true;
            _thresholdA = thresholdA;
            _thresholdB = thresholdB;
            _thresholdC = thresholdC;

            UpdateVisual(1);
        }

        public void SetGridPosition(GridPosition newPosition)
        {
            _gridPosition = newPosition;
        }

        public void UpdateVisual(int groupSize)
        {
            if (iconRenderer == null || _blockData == null) return;

            Sprite newIcon = _blockData.GetIcon(groupSize, _thresholdA, _thresholdB, _thresholdC);
            
            int newLevel = 0;
            if (groupSize >= _thresholdC) newLevel = 3;
            else if (groupSize >= _thresholdB) newLevel = 2;
            else if (groupSize >= _thresholdA) newLevel = 1;

            if (iconRenderer.sprite == null || _currentIconLevel != newLevel)
            {
                _currentIconLevel = newLevel;
                iconRenderer.sprite = newIcon;
            }
        }

        public void SetThresholds(int a, int b, int c)
        {
            _thresholdA = a;
            _thresholdB = b;
            _thresholdC = c;
        }

        public void ResetIcon()
        {
            _currentIconLevel = 0;
            if (iconRenderer != null && _blockData != null)
            {
                iconRenderer.sprite = _blockData.DefaultIcon;
            }
        }

        public void TriggerClick()
        {
            if (_isActive)
            {
                OnBlockClicked?.Invoke(this);
            }
        }

        private void OnMouseDown()
        {
            if (_isActive)
            {
                OnBlockClicked?.Invoke(this);
            }
        }

        public GameObject GameObject => gameObject;

        public void OnSpawn()
        {
            _isActive = true;
            gameObject.SetActive(true);
        }

        public void OnDespawn()
        {
            _isActive = false;
            OnBlockClicked = null;
            ResetIcon();
            gameObject.SetActive(false);
        }

        public void ResetState()
        {
            _currentIconLevel = 0;
            _blockData = null;
            transform.localScale = Vector3.one;
            transform.rotation = Quaternion.identity;
        }
    }
}

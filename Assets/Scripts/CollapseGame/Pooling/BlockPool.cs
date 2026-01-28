using UnityEngine;
using CollapseGame.Core;

namespace CollapseGame.Pooling
{
    public class BlockPool : MonoBehaviour
    {
        [SerializeField] private Block blockPrefab;
        [SerializeField] private Transform poolParent;

        private ObjectPool<Block> _pool;
        private bool _isInitialized;

        public static BlockPool Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void Initialize(int initialCapacity)
        {
            if (_isInitialized) return;

            if (poolParent == null)
            {
                poolParent = new GameObject("BlockPool").transform;
                poolParent.SetParent(transform);
            }

            _pool = new ObjectPool<Block>(blockPrefab, initialCapacity, 0, poolParent);
            _isInitialized = true;
        }

        public Block GetBlock()
        {
            if (!_isInitialized)
            {
                return null;
            }

            return _pool.Get();
        }

        public void ReturnBlock(Block block)
        {
            if (!_isInitialized || block == null) return;
            _pool.Return(block);
        }

        public void ClearPool()
        {
            _pool?.Clear();
            _isInitialized = false;
        }

        public int ActiveCount => _pool?.ActiveCount ?? 0;
        public int PooledCount => _pool?.PooledCount ?? 0;

        private void OnDestroy()
        {
            ClearPool();
            if (Instance == this)
                Instance = null;
        }
    }
}

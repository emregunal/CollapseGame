using System.Collections.Generic;
using UnityEngine;

namespace CollapseGame.Pooling
{
    public class ObjectPool<T> where T : MonoBehaviour, IPoolable
    {
        private readonly Queue<T> _pool;
        private readonly T _prefab;
        private readonly Transform _parent;
        private readonly int _maxSize;

        public int ActiveCount { get; private set; }
        public int PooledCount => _pool.Count;
        public int TotalCount => ActiveCount + PooledCount;

        public ObjectPool(T prefab, int initialSize, int maxSize = 0, Transform parent = null)
        {
            _prefab = prefab;
            _parent = parent;
            _maxSize = maxSize;
            _pool = new Queue<T>(initialSize);
            ActiveCount = 0;

            for (int i = 0; i < initialSize; i++)
            {
                T obj = CreateNewObject();
                obj.OnDespawn();
                _pool.Enqueue(obj);
            }
        }

        public T Get()
        {
            T obj;

            if (_pool.Count > 0)
            {
                obj = _pool.Dequeue();
            }
            else
            {
                obj = CreateNewObject();
            }

            obj.OnSpawn();
            obj.ResetState();
            ActiveCount++;
            return obj;
        }

        public void Return(T obj)
        {
            if (obj == null) return;

            obj.OnDespawn();
            ActiveCount--;

            if (_maxSize == 0 || _pool.Count < _maxSize)
            {
                _pool.Enqueue(obj);
            }
            else
            {
                Object.Destroy(obj.GameObject);
            }
        }

        public void Clear()
        {
            while (_pool.Count > 0)
            {
                T obj = _pool.Dequeue();
                if (obj != null && obj.GameObject != null)
                {
                    Object.Destroy(obj.GameObject);
                }
            }
            ActiveCount = 0;
        }

        private T CreateNewObject()
        {
            T obj = Object.Instantiate(_prefab, _parent);
            obj.GameObject.name = $"{_prefab.name}_Pooled_{TotalCount}";
            return obj;
        }
    }
}

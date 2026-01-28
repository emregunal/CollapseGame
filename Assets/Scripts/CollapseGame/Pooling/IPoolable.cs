using UnityEngine;

namespace CollapseGame.Pooling
{
    public interface IPoolable
    {
        GameObject GameObject { get; }
        void OnSpawn();
        void OnDespawn();
        void ResetState();
    }
}

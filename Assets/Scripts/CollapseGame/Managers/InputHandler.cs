using UnityEngine;
using UnityEngine.InputSystem;
using CollapseGame.Core;

namespace CollapseGame.Managers
{
    public class InputHandler : MonoBehaviour
    {
        [SerializeField] private Camera mainCamera;
        
        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleClick(Mouse.current.position.ReadValue());
            }
            else if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasPressedThisFrame)
            {
                HandleClick(Touchscreen.current.primaryTouch.position.ReadValue());
            }
        }

        private void HandleClick(Vector2 screenPosition)
        {
            if (mainCamera == null) return;

            Vector2 worldPosition = mainCamera.ScreenToWorldPoint(screenPosition);
            RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);
            
            if (hit.collider != null)
            {
                Block block = hit.collider.GetComponent<Block>();
                
                if (block != null && block.IsActive)
                {
                    block.TriggerClick();
                }
            }
        }
    }
}

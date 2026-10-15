using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Assets.Scripts
{
    public class SelectionManager : MonoBehaviour
    {
        public Camera cam;

        void Update()
        {
            // Left mouse button clicked this frame
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();

                Ray ray = cam.ScreenPointToRay(mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Debug.Log("Clicked: " + hit.collider.gameObject.name);

                    GameManager.SelectionMade(hit.collider.gameObject);
                }
            }
        }
    }
}

using GamePieces;
using Scripts;
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

        private void Update()
        {
            // Left mouse button clicked this frame
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePosition = Mouse.current.position.ReadValue();

                Ray ray = cam.ScreenPointToRay(mousePosition);

                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    Debug.Log($"'{hit.collider.gameObject.name}' clicked");

                  if (hit.collider.gameObject != null)
                  {
                    SelectionMade(hit.collider.gameObject);
                  }
                }
            }
        }

        private void SelectionMade(GameObject gameObject)
        {
            var name = gameObject.name;
            Debug.Log($"Selection is '{name}'");

            var piece = gameObject.GetComponent<PieceBase>();
            if (piece != null)
            {
                Debug.Log($"Piece '{piece.name}' selected");
                GameManager.PieceSelected(piece);
            }

            Territory territory = gameObject.GetComponent<Territory>();
            if (territory != null)
            {
                Debug.Log($"Territory '{territory.name}' selected");
                GameManager.TerritorySelected(territory);
            }
        }
    }
}

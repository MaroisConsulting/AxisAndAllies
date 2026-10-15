using UnityEngine;
using UnityEngine.InputSystem;

public class DestinationSetter : MonoBehaviour
{
    public Camera cam;
    public Tank tank;

    public RaycastHit? GetFirstHit()
    {
        // Get mouse position using the new Input System
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Ray ray = cam.ScreenPointToRay(mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit;
        }

        return null;
    }

    void Update()
    {
        if (Mouse.current.leftButton.IsPressed())
        {
            RaycastHit? hit = GetFirstHit();

            if (hit.HasValue)
            {
                Debug.Log("Hit object: " + hit.Value.collider.gameObject.name);
                Debug.Log("Hit position: " + hit.Value.point);

                tank.targetLocation = hit.Value.point;
                tank.commandIssued = true;
            }
        }
    }
}

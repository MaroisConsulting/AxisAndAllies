using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

// Left-drag pans the camera across the map and the scroll wheel zooms it. Lives on the Main Camera. MapEditor,
// GameManagerTestingUI, and PauseMenu disable this component while the mouse is over their panels or while the
// game is paused, so dragging/scrolling on a UI panel doesn't also move the camera.
public class CameraControl : MonoBehaviour
{
    // Height of the horizontal plane the cursor "grabs" while panning (world units). Set it to the map's surface
    // height so the point under the cursor stays under the cursor as you drag.
    [SerializeField, Tooltip("World-space Y height of the horizontal surface grabbed when dragging.")]
    private float panPlaneY = 0f;

    // Multiplies the raw scroll delta. For an orthographic camera it changes orthographicSize; for a perspective
    // camera it is the distance (world units) moved along the camera's forward axis.
    [SerializeField, Min(0f), Tooltip("Zoom speed per unit of mouse-wheel scrolling, scaled by Time.deltaTime.")]
    private float zoomSpeed = 0.02f;

    // Perspective zoom only: the camera never moves below this world Y, so it can't zoom through the map.
    [Tooltip("Minimum world-space Y position allowed when zooming downward.")]
    public float zoomMinY = 0f;

    // Orthographic zoom only: lower bound for orthographicSize so zooming in can't reach zero or go negative.
    [SerializeField, Min(0.01f), Tooltip("Smallest orthographic camera size when zooming in.")]
    private float minOrthographicSize = 0.1f;

    // The Camera on this GameObject (cached in Awake). Used to cast rays from the cursor and to check projection.
    private Camera controlledCamera;
    // The horizontal plane at panPlaneY that cursor rays are intersected with. Rebuilt when each drag starts.
    private Plane dragPlane;
    // The world point on dragPlane that was under the cursor when the drag started; the camera moves to keep it
    // under the cursor.
    private Vector3 dragAnchor;
    // True while a left-button drag is in progress and dragAnchor is valid.
    private bool isDragging;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
    }

    // Converts a screen-pixel mouse position (origin bottom left) into the world point where the cursor ray hits
    // dragPlane. Returns false if there is no camera or the ray misses the plane (e.g. pointing at the sky).
    private bool TryGetDragPoint(Vector2 mousePosition, out Vector3 point)
    {
        point = default;
        if (controlledCamera == null)
            return false;

        Ray ray = controlledCamera.ScreenPointToRay(mousePosition);
        if (!dragPlane.Raycast(ray, out float distance))
            return false;

        point = ray.GetPoint(distance);
        return true;
    }

    // Applies zoom, then panning, once per frame. LateUpdate runs after every Update, so the tools have already
    // enabled/disabled this component for the frame based on where the mouse is.
    private void LateUpdate()
    {
        // New Input System mouse; null when no mouse is connected, in which case any drag ends.
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            isDragging = false;
            return;
        }

        // Let UI controls consume scrolling without also zooming the camera.
        bool scrollingUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
        float zoom = scrollingUI ? 0f : mouse.scroll.ReadValue().y * zoomSpeed * Time.deltaTime;
        // Scrolling up gives a positive zoom, which zooms in.
        if (zoom != 0f)
        {
            // Orthographic: shrink/grow the view size. Perspective: move the camera along its forward axis.
            if (controlledCamera != null && controlledCamera.orthographic)
                controlledCamera.orthographicSize = Mathf.Max(
                    minOrthographicSize, controlledCamera.orthographicSize - zoom);
            else
            {
                Vector3 zoomMovement = transform.forward * zoom;
                // Only moving downward can hit the zoomMinY floor; scale the step so it ends exactly there.
                if (zoomMovement.y < 0f)
                {
                    float availableHeight = Mathf.Max(0f, transform.position.y - zoomMinY);
                    // Stop along the zoom direction so we don't slide at the limit.
                    zoomMovement *= Mathf.Clamp01(availableHeight / -zoomMovement.y);
                }

                transform.position += zoomMovement;
            }
        }

        // Panning: releasing the left button ends the drag.
        if (!mouse.leftButton.isPressed)
        {
            isDragging = false;
            return;
        }

        // Mouse position in screen pixels (origin bottom left).
        Vector2 mousePosition = mouse.position.ReadValue();
        // First frame of a drag (or the button is held but no anchor yet, e.g. the component was just re-enabled):
        // record the grabbed world point and don't move this frame.
        if (!isDragging || mouse.leftButton.wasPressedThisFrame)
        {
            dragPlane = new Plane(Vector3.up, new Vector3(0f, panPlaneY, 0f));
            isDragging = TryGetDragPoint(mousePosition, out dragAnchor);
            return;
        }

        if (TryGetDragPoint(mousePosition, out Vector3 currentPoint))
        {
            // Keep the grabbed world point beneath the cursor at every zoom level.
            Vector3 movement = dragAnchor - currentPoint;
            movement.y = 0f;
            transform.position += movement;
        }
        else
        {
            // Release the anchor if the cursor ray no longer reaches the surface.
            isDragging = false;
        }
    }

    // The tools disable this component while the mouse is over a panel or the game is paused; dropping the drag
    // means a drag doesn't resume from a stale anchor (and jump the camera) when it is re-enabled.
    private void OnDisable() => isDragging = false;

    // Ends any drag when the window loses focus (e.g. alt-tab mid-drag), since the button release may be missed.
    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            isDragging = false;
    }
}

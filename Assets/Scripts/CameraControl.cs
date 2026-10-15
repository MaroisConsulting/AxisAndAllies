using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class CameraControl : MonoBehaviour
{
    [SerializeField, Min(0f), Tooltip("World units moved per pixel of mouse dragging at the starting zoom level.")]
    private float panSpeed = 0.02f;

    [Range(0f, 1f), Tooltip("Minimum fraction of the starting pan speed. Increase this for faster panning when zoomed in.")]
    public float minPanSpeedMultiplier = 0.25f;

    [SerializeField, Min(0f), Tooltip("World units moved per unit of mouse-wheel scrolling.")]
    private float zoomSpeed = 0.02f;

    [Tooltip("Minimum world-space Y position allowed when zooming downward.")]
    public float zoomMinY = 0f;

    [SerializeField, Min(0.01f), Tooltip("Smallest orthographic camera size when zooming in.")]
    private float minOrthographicSize = 0.1f;

    private Camera controlledCamera;
    private InfoChecker infoChecker;
    private Vector2 previousMousePosition;
    private bool isDragging;
    private float startingZoomScale;

    private void Awake()
    {
        controlledCamera = GetComponent<Camera>();
        infoChecker = GetComponent<InfoChecker>();
        startingZoomScale = GetZoomScale();
    }

    private float GetZoomScale()
    {
        if (controlledCamera != null && controlledCamera.orthographic)
            return Mathf.Max(0.01f, controlledCamera.orthographicSize);

        // Keep panning usable even when the camera reaches its minimum height.
        return Mathf.Max(1f, transform.position.y - zoomMinY);
    }

    private void LateUpdate()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null)
        {
            isDragging = false;
            return;
        }

        // Let UI controls consume scrolling without also zooming the camera.
        // The IMGUI import browser is not tracked by the EventSystem.
        bool scrollingUI = (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) ||
            (infoChecker != null && infoChecker.isActiveAndEnabled && infoChecker.IsBrowsing);
        float zoom = scrollingUI ? 0f : mouse.scroll.ReadValue().y * zoomSpeed;
        if (zoom != 0f)
        {
            if (controlledCamera != null && controlledCamera.orthographic)
                controlledCamera.orthographicSize = Mathf.Max(
                    minOrthographicSize, controlledCamera.orthographicSize - zoom);
            else
            {
                Vector3 zoomMovement = transform.forward * zoom;
                if (zoomMovement.y < 0f)
                {
                    float availableHeight = Mathf.Max(0f, transform.position.y - zoomMinY);
                    // Stop along the zoom direction so we don't slide at the limit.
                    zoomMovement *= Mathf.Clamp01(availableHeight / -zoomMovement.y);
                }

                transform.position += zoomMovement;
            }
        }

        if (!mouse.leftButton.isPressed)
        {
            isDragging = false;
            return;
        }

        Vector2 mousePosition = mouse.position.ReadValue();
        if (!isDragging || mouse.leftButton.wasPressedThisFrame)
        {
            previousMousePosition = mousePosition;
            isDragging = true;
            return;
        }

        Vector2 drag = mousePosition - previousMousePosition;
        previousMousePosition = mousePosition;

        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up);
        Vector3 up = Vector3.ProjectOnPlane(transform.up, Vector3.up);

        // Handle even a camera rolled so its right vector points vertically.
        if (right.sqrMagnitude < 0.0001f)
            right = Vector3.Cross(up.normalized, Vector3.up);

        right.Normalize();
        Vector3 forward = Vector3.Cross(right, Vector3.up);

        // Match screen-up for tilted/top-down cameras. At the horizon,
        // use the viewing direction so vertical dragging still moves on XZ.
        float alignment = Vector3.Dot(forward, up);
        if (Mathf.Abs(alignment) < 0.0001f)
            alignment = Vector3.Dot(forward, transform.forward);
        if (alignment < 0f)
            forward = -forward;

        // Move opposite the drag, making the scene follow the cursor like a hand tool.
        // Mouse displacement already represents movement: do not multiply by deltaTime.
        float panSpeedMultiplier = Mathf.Max(
            Mathf.Clamp01(minPanSpeedMultiplier), GetZoomScale() / startingZoomScale);
        float scaledPanSpeed = panSpeed * panSpeedMultiplier;
        transform.position -= (right * drag.x + forward * drag.y) * scaledPanSpeed;
    }

    private void OnDisable() => isDragging = false;

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            isDragging = false;
    }
}

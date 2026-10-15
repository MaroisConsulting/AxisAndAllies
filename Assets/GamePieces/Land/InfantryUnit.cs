using Assets.Enums;
using GamePieces;
using Scripts;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.GraphicsBuffer;

namespace Assets.Models
{
    public class InfantryUnit : LandPieceBase
    {
        public enum AvoidancePreference
        {
            Left,
            Right
        }

        public GameObject DragDropTarget;

        public Vector3 destination;
        [Tooltip("Enable to explicitly target the world origin. Nonzero destinations are automatically treated as set.")]
        public bool hasDestination;
        public float moveSpeed = 3f;
        public float turnSpeed = 180f;
        public float stoppingDistance = 0.5f;
        public GameObject followObject;
        public Animator animator;
        public bool isMoving = false;

        [Header("Obstacle Avoidance")]
        public float sensorDistance = 2f;
        public AvoidancePreference obstacleAvoidancePreference;

        [Header("Selection")]
        public bool allowSelection = true;
        public bool isSelected;
        public Camera mainCamera;
        public Vector3 indicatorSize = new Vector3(0.5f, 0.5f, 0.5f);
        public float indicatorVerticalOffset = 2f;
        public Color indicatorColor = Color.green;
        [Range(0f, 1f)] public float indicatorTransparency = 0.25f;
        [Min(0f)] public float lineThickness = 0.06f;

        private static InfantryUnit selectedUnit;
        private static readonly int RunParameter = Animator.StringToHash("run");
        private GameObject selectionIndicator;
        private Material indicatorMaterial;
        private LineRenderer destinationLine;
        private Material destinationLineMaterial;
        private bool isDraggingDestination;
        private bool hasDragPoint;
        private bool showDestinationLine;
        private Vector3 dragPoint;

        public InfantryUnit()
        {
            PieceType = PieceType.Industry;
        }

        private void Awake()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            CreateSelectionIndicator();
            CreateDestinationLine();
            SetSelected(isSelected && allowSelection);
        }

        private void Start()
        {
            obstacleAvoidancePreference = Random.value < 0.5f
                ? AvoidancePreference.Left
                : AvoidancePreference.Right;
        }

        private void Update()
        {
            HandleSelectionInput();
            UpdateSelectionIndicator();
            MoveToDestination();
            UpdateDestinationLine();
            UpdateAnimator();
        }

        private void HandleSelectionInput()
        {
            if (!allowSelection || mainCamera == null || Mouse.current == null)
            {
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    InfantryUnit clickedUnit = hit.collider.GetComponentInParent<InfantryUnit>();
                    if (clickedUnit == this)
                    {
                        SetSelected(true);
                        isDraggingDestination = true;
                        hasDragPoint = false;
                    }
                }

                return;
            }

            if (!isDraggingDestination)
            {
                return;
            }

            if (Physics.Raycast(ray, out RaycastHit dragHit))
            {
                dragPoint = dragHit.point;
                hasDragPoint = true;

                DragDropTarget = dragHit.collider.gameObject;

            }

            if (Mouse.current.rightButton.wasReleasedThisFrame)
            {
                if (hasDragPoint)
                {
                    SetDestination(dragPoint);
                    showDestinationLine = true;

                    GameManager.DestinationSelected(DragDropTarget);
                }

                followObject = null;
                isDraggingDestination = false;
                SetSelected(false);
            }
        }

        public void SetSelected(bool selected)
        {
            selected = selected && allowSelection;

            if (selected && selectedUnit != this)
            {
                if (selectedUnit != null)
                {
                    selectedUnit.SetSelected(false);
                }

                selectedUnit = this;
            }
            else if (!selected && selectedUnit == this)
            {
                selectedUnit = null;
            }

            isSelected = selected;
            if (!isSelected)
            {
                isDraggingDestination = false;
                hasDragPoint = false;
            }

            if (selectionIndicator != null)
            {
                selectionIndicator.SetActive(isSelected);
            }
        }

        private void CreateSelectionIndicator()
        {
            selectionIndicator = GameObject.CreatePrimitive(PrimitiveType.Cube);
            selectionIndicator.name = "Selection Indicator";
            selectionIndicator.transform.SetParent(transform, false);

            Collider indicatorCollider = selectionIndicator.GetComponent<Collider>();
            if (indicatorCollider != null)
            {
                Destroy(indicatorCollider);
            }

            Renderer indicatorRenderer = selectionIndicator.GetComponent<Renderer>();
            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                indicatorMaterial = new Material(shader);
                indicatorRenderer.material = indicatorMaterial;
            }

            selectionIndicator.SetActive(false);
            UpdateSelectionIndicator();
        }

        private void CreateDestinationLine()
        {
            destinationLine = gameObject.AddComponent<LineRenderer>();
            destinationLine.positionCount = 2;
            destinationLine.useWorldSpace = true;
            destinationLine.startWidth = Mathf.Max(0f, lineThickness);
            destinationLine.endWidth = Mathf.Max(0f, lineThickness);
            destinationLine.numCapVertices = 4;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                destinationLineMaterial = new Material(shader);
                destinationLineMaterial.color = indicatorColor;
                destinationLine.material = destinationLineMaterial;
            }

            destinationLine.enabled = false;
        }

        private void UpdateDestinationLine()
        {
            if (destinationLine == null)
            {
                return;
            }

            destinationLine.startWidth = Mathf.Max(0f, lineThickness);
            destinationLine.endWidth = Mathf.Max(0f, lineThickness);

            if (destinationLineMaterial != null)
            {
                destinationLineMaterial.color = indicatorColor;
            }

            Vector3 lineTarget = isDraggingDestination ? dragPoint : destination;
            bool shouldShow = (isDraggingDestination && hasDragPoint) || showDestinationLine;

            if (showDestinationLine && followObject == null)
            {
                Vector3 horizontalDifference = destination - transform.position;
                horizontalDifference.y = 0f;
                float arrivalDistance = Mathf.Max(0f, stoppingDistance);
                bool reachedDestination = horizontalDifference.magnitude <= arrivalDistance + 0.001f;
                if (reachedDestination || (!isMoving && horizontalDifference.magnitude <= arrivalDistance + 0.01f))
                {
                    showDestinationLine = false;
                    shouldShow = false;
                }
            }

            destinationLine.enabled = shouldShow;
            if (shouldShow)
            {
                destinationLine.SetPosition(0, transform.position);
                destinationLine.SetPosition(1, lineTarget);
            }
        }

        private void UpdateSelectionIndicator()
        {
            if (selectionIndicator == null)
            {
                return;
            }

            if (!allowSelection && isSelected)
            {
                SetSelected(false);
            }

            selectionIndicator.SetActive(isSelected);
            selectionIndicator.transform.localPosition = Vector3.up * indicatorVerticalOffset;
            selectionIndicator.transform.localRotation = Quaternion.identity;
            selectionIndicator.transform.localScale = indicatorSize;

            if (indicatorMaterial != null)
            {
                Color color = indicatorColor;
                color.a = 1f - Mathf.Clamp01(indicatorTransparency);
                indicatorMaterial.color = color;
            }
        }

        private void OnDestroy()
        {
            if (selectedUnit == this)
            {
                selectedUnit = null;
            }

            if (indicatorMaterial != null)
            {
                Destroy(indicatorMaterial);
            }

            if (destinationLineMaterial != null)
            {
                Destroy(destinationLineMaterial);
            }
        }

        private void OnDisable()
        {
            isMoving = false;
            isDraggingDestination = false;
            showDestinationLine = false;
            if (destinationLine != null)
            {
                destinationLine.enabled = false;
            }
            UpdateAnimator();
        }

        private void UpdateAnimator()
        {
            if (animator != null)
            {
                animator.SetBool(RunParameter, isMoving);
            }
        }

        public void SetDestination(Vector3 target)
        {
            destination = target;
            hasDestination = true;
        }

        public void MoveToDestination()
        {
            if (followObject == null && !hasDestination && destination == Vector3.zero)
            {
                isMoving = false;
                return;
            }

            Vector3 positionBeforeMove = transform.position;
            Vector3 target = followObject != null ? followObject.transform.position : destination;
            Vector3 direction = target - transform.position;
            direction.y = 0f;
            float distance = direction.magnitude;
            float stopAt = Mathf.Max(0f, stoppingDistance);

            if (distance <= stopAt)
            {
                isMoving = false;
                return;
            }

            Vector3 angles = transform.eulerAngles;

            float avoidanceTurn = GetObstacleAvoidanceTurn();
            if (!Mathf.Approximately(avoidanceTurn, 0f))
            {
                angles.y += avoidanceTurn * Mathf.Max(0f, turnSpeed) * 2f * Time.deltaTime;
            }
            else
            {
                float targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                angles.y = Mathf.MoveTowardsAngle(
                    angles.y,
                    targetYaw,
                    Mathf.Max(0f, turnSpeed) * Time.deltaTime);
            }

            transform.eulerAngles = angles;

            float step = Mathf.Min(Mathf.Max(0f, moveSpeed) * Time.deltaTime, distance - stopAt);
            transform.Translate(Vector3.forward * step, Space.Self);
            isMoving = (transform.position - positionBeforeMove).sqrMagnitude > Mathf.Epsilon;
        }

        private float GetObstacleAvoidanceTurn()
        {
            float distance = Mathf.Max(0f, sensorDistance);
            if (distance <= 0f)
            {
                return 0f;
            }

            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;
            Vector3 leftDirection = Quaternion.AngleAxis(-35f, Vector3.up) * forward;
            Vector3 rightDirection = Quaternion.AngleAxis(35f, Vector3.up) * forward;

            bool centerBlocked = Physics.Raycast(origin, forward, distance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            bool leftBlocked = Physics.Raycast(origin, leftDirection, distance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);
            bool rightBlocked = Physics.Raycast(origin, rightDirection, distance, Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            Debug.DrawRay(origin, forward * distance, centerBlocked ? Color.red : Color.green);
            Debug.DrawRay(origin, leftDirection * distance, leftBlocked ? Color.red : Color.green);
            Debug.DrawRay(origin, rightDirection * distance, rightBlocked ? Color.red : Color.green);

            if (leftBlocked && !rightBlocked)
            {
                return 1f;
            }

            if (rightBlocked && !leftBlocked)
            {
                return -1f;
            }

            if (centerBlocked || (leftBlocked && rightBlocked))
            {
                return obstacleAvoidancePreference == AvoidancePreference.Left ? -1f : 1f;
            }

            return 0f;
        }
    }
}

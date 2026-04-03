using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Third-person camera that orbits around the player.
/// Mouse moves the camera. Scroll wheel zooms.
/// Automatically adjusts up-axis when gravity changes.
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Orbit Settings")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float verticalMinAngle = -30f;
    [SerializeField] private float verticalMaxAngle = 60f;

    [Header("Distance")]
    [SerializeField] private float distance    = 5f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 10f;
    [SerializeField] private float zoomSpeed   = 3f;

    [Header("Camera Height Offset")]
    [SerializeField] private float heightOffset = 1f; // how high above target pivot to look at

    [Header("Collision")]
    [SerializeField] private float     collisionRadius = 0.3f;
    [SerializeField] private LayerMask collisionLayer;

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothing = 10f;
    [SerializeField] private float rotationSmoothing = 10f;

    private PlayerInputActions  _inputActions;
    private PlayerController    _playerController;
    private float               _yaw;
    private float               _pitch = 20f;
    private Vector3             _currentUp = Vector3.up;

    #region Unity Callbacks

    private void Awake()
    {
        _inputActions     = new PlayerInputActions();
        _playerController = target != null ? target.GetComponent<PlayerController>() : null;

        _yaw = transform.eulerAngles.y;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void OnEnable()  => _inputActions.Player.Enable();
    private void OnDisable() => _inputActions.Player.Disable();

    private void LateUpdate()
    {
        if (target == null) return;

        SyncUpAxis();
        HandleMouseLook();
        HandleZoom();
        PositionCamera();
    }

    #endregion

    #region Camera Logic

    /// <summary>Keep camera up axis in sync with player gravity.</summary>
    private void SyncUpAxis()
    {
        if (_playerController != null)
            _currentUp = -_playerController.GravityDirection;
    }

    private void HandleMouseLook()
    {
        Vector2 delta = _inputActions.Player.Look.ReadValue<Vector2>();
        _yaw         += delta.x * mouseSensitivity;
        _pitch        = Mathf.Clamp(_pitch - delta.y * mouseSensitivity,
                                    verticalMinAngle, verticalMaxAngle);
    }

    private void HandleZoom()
    {
        float scroll = _inputActions.Player.Zoom.ReadValue<float>();
        distance     = Mathf.Clamp(distance - scroll * zoomSpeed * Time.deltaTime,
                                   minDistance, maxDistance);
    }

    private void PositionCamera()
    {
        // Pivot point — slightly above character root
        Vector3 pivot = target.position + _currentUp * heightOffset;

        // Camera orientation
        Quaternion rotation = Quaternion.LookRotation(Vector3.forward, _currentUp)
                              * Quaternion.Euler(_pitch, _yaw, 0f);

        Vector3 desiredPos  = pivot - rotation * Vector3.forward * distance;

        // Collision check — prevent camera clipping into walls
        Vector3 dirToCamera = (desiredPos - pivot).normalized;
        float   actualDist  = distance;

        if (Physics.SphereCast(pivot, collisionRadius, dirToCamera,
            out RaycastHit hit, distance, collisionLayer))
        {
            actualDist = Mathf.Clamp(hit.distance - collisionRadius,
                                     minDistance, distance);
        }

        Vector3 finalPos = pivot - rotation * Vector3.forward * actualDist;

        // Smooth
        transform.position = Vector3.Lerp(transform.position, finalPos,
                                          positionSmoothing * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation,
                                              Quaternion.LookRotation(pivot - transform.position, _currentUp),
                                              rotationSmoothing * Time.deltaTime);
    }

    #endregion

    #region Public API

    /// <summary>Called by GravityManager after gravity switches.</summary>
    public void OnGravityChanged(Vector3 newGravityDir)
    {
        _currentUp = -newGravityDir.normalized;
    }

    #endregion
}
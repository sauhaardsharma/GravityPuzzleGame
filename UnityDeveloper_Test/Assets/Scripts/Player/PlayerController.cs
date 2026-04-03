using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

/// <summary>
/// Handles player movement (WASD), jumping (Space)
/// using Unity's New Input System with Rigidbody + CapsuleCollider.
/// Supports arbitrary gravity direction for gravity manipulation.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float gravityMagnitude = 20f;

    [Header("Ground Detection")]
    [SerializeField] private float groundCheckDistance = 0.15f;
    [SerializeField] private float groundCheckRadius = 0.35f;
    [SerializeField] private LayerMask groundLayer;

    // ── Public State ──────────────────────────────────────────
    public Vector3 GravityDirection { get; private set; } = Vector3.down;
    public bool IsGrounded { get; private set; }
    public float AirTime { get; private set; }
    public Vector3 FlatVelocity { get; private set; }
    public bool IsSwitchingGravity { get; private set; }

    // ── Private ───────────────────────────────────────────────
    private Rigidbody _rb;
    private CapsuleCollider _capsule;
    private PlayerInputActions _inputActions;
    private Vector2 _moveInput;
    private bool _jumpPressed;
    private Quaternion _targetRotation;

    #region Unity Callbacks

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _capsule = GetComponent<CapsuleCollider>();
        _rb.useGravity = false;
        _rb.freezeRotation = true;
        _inputActions = new PlayerInputActions();
        _targetRotation = transform.rotation;
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();
        _inputActions.Player.Move.performed += OnMove;
        _inputActions.Player.Move.canceled += OnMove;
        _inputActions.Player.Jump.performed += OnJump;
    }

    private void OnDisable()
    {
        _inputActions.Player.Move.performed -= OnMove;
        _inputActions.Player.Move.canceled -= OnMove;
        _inputActions.Player.Jump.performed -= OnJump;
        _inputActions.Player.Disable();
    }

    private void Update()
    {
        CheckGrounded();
        HandleMovement();
        HandleJump();
    }

    private void FixedUpdate()
    {
        ApplyGravity();
        ApplyRotation();
    }

    #endregion

    #region Input Callbacks

    private void OnMove(InputAction.CallbackContext ctx)
        => _moveInput = ctx.ReadValue<Vector2>();

    private void OnJump(InputAction.CallbackContext ctx)
        => _jumpPressed = true;

    #endregion

    #region Movement

    private void CheckGrounded()
    {
        float distToBottom = _capsule.center.y
                             - (_capsule.height * 0.5f)
                             + _capsule.radius;
        Vector3 bottomCenter = transform.position
                             + transform.up * distToBottom;

        IsGrounded = Physics.SphereCast(
            bottomCenter,
            _capsule.radius * 0.95f,
            GravityDirection,
            out _,
            groundCheckDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (IsGrounded)
            AirTime = 0f;
        else if (!IsSwitchingGravity)
            AirTime += Time.deltaTime;
    }

    /// <summary>WASD movement relative to camera and current player "up".</summary>
    private void HandleMovement()
    {
        Vector3 playerUp = -GravityDirection;

        if (_moveInput.sqrMagnitude > 0.01f)
        {
            Vector3 camForward = Camera.main != null
                                  ? Camera.main.transform.forward
                                  : transform.forward;

            Vector3 moveForward = Vector3.ProjectOnPlane(camForward, playerUp).normalized;
            Vector3 moveRight = Vector3.Cross(playerUp, moveForward);
            Vector3 move = (moveForward * _moveInput.y
                                + moveRight * _moveInput.x) * moveSpeed;

            FlatVelocity = move;

            // Preserve gravity velocity, replace flat velocity
            Vector3 gravVel = Vector3.Project(_rb.linearVelocity, GravityDirection);
            _rb.linearVelocity = move + gravVel;

            // Only update target rotation when there is movement
            if (move.sqrMagnitude > 0.01f)
                _targetRotation = Quaternion.LookRotation(move.normalized, playerUp);
        }
        else
        {
            FlatVelocity = Vector3.zero;

            // Kill flat velocity, preserve gravity velocity
            Vector3 gravVel = Vector3.Project(_rb.linearVelocity, GravityDirection);
            _rb.linearVelocity = gravVel;
        }
    }

    /// <summary>
    /// Rotation applied in FixedUpdate via MoveRotation
    /// to avoid conflicts with Rigidbody physics.
    /// </summary>
    private void ApplyRotation()
    {
        Quaternion newRot = Quaternion.Slerp(transform.rotation, _targetRotation,
                                              15f * Time.fixedDeltaTime);
        _rb.MoveRotation(newRot);
    }

    private void HandleJump()
    {
        if (_jumpPressed && IsGrounded)
        {
            Vector3 gravVel = Vector3.Project(_rb.linearVelocity, GravityDirection);
            _rb.linearVelocity = _rb.linearVelocity - gravVel
                               + (-GravityDirection * jumpForce);
        }
        _jumpPressed = false;
    }

    /// <summary>Apply custom gravity each physics step.</summary>
    private void ApplyGravity()
    {
        if (!IsGrounded)
        {
            _rb.AddForce(GravityDirection * gravityMagnitude, ForceMode.Acceleration);
        }
        else
        {
            // Kill gravity velocity when grounded to prevent sliding
            Vector3 gravVel = Vector3.Project(_rb.linearVelocity, GravityDirection);
            if (Vector3.Dot(gravVel, GravityDirection) > 0f)
                _rb.linearVelocity -= gravVel;
        }
    }

    #endregion

    #region Public API

    /// <summary>
    /// Called by GravityManager to switch gravity direction.
    /// Uses MoveRotation to avoid Rigidbody conflicts.
    /// </summary>
    public void SetGravityDirection(Vector3 newDirection)
    {
        IsSwitchingGravity = true;

        GravityDirection = newDirection.normalized;
        Vector3 newUp = -GravityDirection;

        // Compute new rotation
        Quaternion rot = Quaternion.FromToRotation(transform.up, newUp)
                            * transform.rotation;

        // Use MoveRotation — never set transform.rotation directly with Rigidbody
        _rb.MoveRotation(rot);
        _targetRotation = rot;

        // Kill all velocity
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        AirTime = 0f;
        FlatVelocity = Vector3.zero;

        // Small nudge toward new surface to ensure grounding
        transform.position += newUp * 0.05f;

        StartCoroutine(ResetSwitchFlag());
    }

    #endregion

    #region Helpers

    private IEnumerator ResetSwitchFlag()
    {
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        IsSwitchingGravity = false;
    }

    #endregion

    #region Debug

    private void OnDrawGizmosSelected()
    {
        if (_capsule == null) _capsule = GetComponent<CapsuleCollider>();
        float distToBottom = _capsule.center.y
                             - (_capsule.height * 0.5f)
                             + _capsule.radius;
        Vector3 bottomCenter = transform.position + transform.up * distToBottom;
        Gizmos.color = IsGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(bottomCenter, _capsule.radius * 0.95f);
    }

    #endregion
}
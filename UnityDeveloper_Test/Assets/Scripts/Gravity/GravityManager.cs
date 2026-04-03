using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles gravity direction selection via arrow keys,
/// shows hologram preview, and applies gravity on Enter.
/// </summary>
public class GravityManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController  playerController;
    [SerializeField] private ThirdPersonCamera thirdPersonCamera;
    [SerializeField] private Transform         holoRoot;

    [Header("Head Alignment")]
    [SerializeField] private Transform playerHeadBone;   // assign head bone of Exo Gray
    [SerializeField] private Transform holoHeadBone;     // assign head bone of Exo Gray_Holo


    [Header("Hologram Settings")]
    [SerializeField] private float holoSmoothSpeed    = 8f;

    [Header("UI")]
    [SerializeField] private GravityUIIndicator uiIndicator;

    private PlayerInputActions _inputActions;
    private Vector3            _pendingGravityDir;
    private Vector3            _selectedGravityDir;
    private bool               _isSelecting;

    #region Unity Callbacks

    private void Awake()
    {
        _inputActions       = new PlayerInputActions();
        _selectedGravityDir = Vector3.down;
        _pendingGravityDir  = Vector3.down;
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();
        _inputActions.Player.GravitySelect.performed  += OnGravitySelect;
        _inputActions.Player.GravitySelect.canceled   += OnGravitySelectCanceled;
        _inputActions.Player.GravityConfirm.performed += OnGravityConfirm;
    }

    private void OnDisable()
    {
        _inputActions.Player.GravitySelect.performed  -= OnGravitySelect;
        _inputActions.Player.GravitySelect.canceled   -= OnGravitySelectCanceled;
        _inputActions.Player.GravityConfirm.performed -= OnGravityConfirm;
        _inputActions.Player.Disable();
    }

    private void Update()
    {
        UpdateHologram();
    }

    #endregion

    #region Input Callbacks

    private void OnGravitySelect(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();
        _isSelecting  = true;

        // Get player's current orientation axes
        Vector3 playerUp      = -playerController.GravityDirection;
        Vector3 playerForward = Vector3.ProjectOnPlane(
                                    playerController.transform.forward, playerUp).normalized;
        Vector3 playerRight   = Vector3.Cross(playerUp, playerForward);

        // Map arrow keys to world directions relative to player orientation
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            _pendingGravityDir = input.x > 0 ? playerRight : -playerRight;
        else
            _pendingGravityDir = input.y > 0 ? playerForward : -playerForward;

        holoRoot.gameObject.SetActive(true);
        uiIndicator?.Show(GetDirectionName(_pendingGravityDir));
    }

    private void OnGravitySelectCanceled(InputAction.CallbackContext ctx)
    {
        // Hologram stays visible until Enter confirms or player moves away
    }

    private void OnGravityConfirm(InputAction.CallbackContext ctx)
    {
        if (!_isSelecting) return;

        // Apply gravity to player and camera
        _selectedGravityDir = _pendingGravityDir;
        playerController.SetGravityDirection(_selectedGravityDir);
        thirdPersonCamera.OnGravityChanged(_selectedGravityDir);

        // Hide hologram and UI
        holoRoot.gameObject.SetActive(false);
        uiIndicator?.Hide();
        _isSelecting = false;
    }

    #endregion

    #region Hologram

    /// <summary>
    /// Positions hologram to show where player will stand
    /// after gravity switches to pending direction.
    /// </summary>
private void UpdateHologram()
{
    if (!_isSelecting || holoRoot == null) return;

    Vector3 newUp = -_pendingGravityDir;

    Vector3 currentForward   = playerController.transform.forward;
    Vector3 projectedForward = Vector3.ProjectOnPlane(currentForward, newUp);

    if (projectedForward.sqrMagnitude < 0.01f)
        projectedForward = Vector3.ProjectOnPlane(playerController.transform.right, newUp);

    projectedForward.Normalize();

    Quaternion targetRot = Quaternion.LookRotation(projectedForward, newUp);
    holoRoot.rotation    = Quaternion.Slerp(holoRoot.rotation, targetRot,
                                             holoSmoothSpeed * Time.deltaTime);

    if (playerHeadBone != null && holoHeadBone != null)
    {
        Vector3 headOffset = playerHeadBone.position - holoHeadBone.position;
        // Push hologram away from player along pendingGravityDir
        holoRoot.position  = holoRoot.position + headOffset
                           + (_pendingGravityDir * 0.3f);
    }
    else
    {
        holoRoot.position = playerController.transform.position;
    }
}

    #endregion

    #region Helpers

    private string GetDirectionName(Vector3 dir)
    {
        Vector3 playerUp      = -playerController.GravityDirection;
        Vector3 playerForward = Vector3.ProjectOnPlane(
                                    playerController.transform.forward, playerUp).normalized;
        Vector3 playerRight   = Vector3.Cross(playerUp, playerForward);

        if (Vector3.Dot(dir, playerUp)      >  0.9f) return "Up (Ceiling)";
        if (Vector3.Dot(dir, playerUp)      < -0.9f) return "Down (Floor)";
        if (Vector3.Dot(dir, playerRight)   >  0.9f) return "Right Wall";
        if (Vector3.Dot(dir, playerRight)   < -0.9f) return "Left Wall";
        if (Vector3.Dot(dir, playerForward) >  0.9f) return "Forward Wall";
        if (Vector3.Dot(dir, playerForward) < -0.9f) return "Back Wall";
        return "Unknown";
    }

    #endregion
}
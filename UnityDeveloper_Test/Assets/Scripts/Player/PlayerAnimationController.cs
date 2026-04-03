using UnityEngine;

/// <summary>
/// Drives the Animator based on PlayerController state.
/// Uses available animations: Idle, Running, Falling Idle.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController playerController;

    [Header("Settings")]
    [SerializeField] private float speedDampTime = 0.1f;
    [SerializeField] private float minFallTime   = 0.5f; // increased to avoid false triggers

    private static readonly int SpeedHash      = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

    private Animator _animator;

    #region Unity Callbacks

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Update()
    {
        UpdateAnimationState();
    }

    #endregion

    #region Animation Logic

    private void UpdateAnimationState()
{
    if (playerController == null) return;

    // During gravity switch — force idle, no falling
    if (playerController.IsSwitchingGravity)
    {
        _animator.SetFloat(SpeedHash, 0f);
        _animator.SetBool(IsGroundedHash, true);
        return;
    }

    float speed = playerController.FlatVelocity.magnitude;
    _animator.SetFloat(SpeedHash, speed, speedDampTime, Time.deltaTime);

    bool isGrounded = playerController.IsGrounded ||
                      playerController.AirTime < minFallTime;
    _animator.SetBool(IsGroundedHash, isGrounded);
}

    #endregion
}
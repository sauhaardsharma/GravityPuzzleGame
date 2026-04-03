using UnityEngine;

/// <summary>
/// Attached to each collectible cube in the scene.
/// Notifies GameManager when collected by player.
/// </summary>
public class CollectibleCube : MonoBehaviour
{
    [Header("Collection Settings")]
    [SerializeField] private float rotateSpeed = 90f;
    [SerializeField] private float bobSpeed = 2f;
    [SerializeField] private float bobHeight = 0.3f;

    private Vector3 _startPosition;

    private void Start()
    {
        _startPosition = transform.position;
    }

    private void Update()
    {
        // Rotate cube
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);

        // Bob up and down
        transform.position = _startPosition
                           + Vector3.up * Mathf.Sin(Time.time * bobSpeed) * bobHeight;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.Instance.OnCubeCollected();
            gameObject.SetActive(false);
        }
    }
}
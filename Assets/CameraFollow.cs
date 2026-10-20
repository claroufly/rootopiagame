using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public float smoothTime = 0.3f;

    [Header("Isometric Settings")]
    public float distance = 1f;
    public float verticalAngle = 5f;   // X rotation - how steep the tilt is
    public float horizontalAngle = 10f; // Y rotation - corner-facing angle

    private Vector3 velocity = Vector3.zero;
    private Vector3 offset;

    void Start()
    {
        // Fixed camera rotation - steep top-down angle
        transform.rotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);

        // Offset is purely direction-based now (no separate height term,
        // since transform.forward already encodes the vertical tilt)
        offset = -transform.forward * distance;
    }

    void Update()
    {
        if (target != null)
        {
            Vector3 targetPosition = target.position + offset;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
        }
    }
}
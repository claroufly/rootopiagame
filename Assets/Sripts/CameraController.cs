using UnityEngine;
using DG.Tweening;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float duration = 1f;

    private Quaternion originalRotation;

    private void Awake()
    {
        originalRotation = transform.rotation;
    }

    public void LookAt(Transform target)
    {
        transform.DOLookAt(target.position, duration);
    }

    public void ReturnToMain()
    {
        transform.DORotateQuaternion(originalRotation, duration);
    }
}
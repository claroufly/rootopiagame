using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 4f;
    public float rotationSmoothing = 0.15f;

    [Header("Gravity")]
    public float gravity = -9.81f;
    public float groundedGravity = -2f;

    private Vector2 move;
    private Camera mainCamera;
    private Animator animator;
    private CharacterController controller;
    private float verticalVelocity;

    private PlayerInteraction playerInteraction;

    void Start()
    {
        mainCamera = Camera.main;
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        playerInteraction = GetComponentInChildren<PlayerInteraction>();

        if (mainCamera == null)
            Debug.LogWarning("No Main Camera found.");

        if (animator == null)
            Debug.LogWarning("No Animator found.");

        if (playerInteraction == null)
            Debug.LogWarning("No PlayerInteraction found.");
    }

    void Update()
    {
        MovePlayer();
        ApplyGravity();
    }

    // Movement Input
    public void OnMove(InputAction.CallbackContext context)
    {
        move = context.ReadValue<Vector2>();
    }

    // Interaction Input
    public void OnInteract(InputAction.CallbackContext context)
    {
        Debug.Log($"[PlayerController] OnInteract fired, phase: {context.phase}");

        if (context.performed)
        {
            playerInteraction.Interact();
        }
    }

    public void OnItemInteract(InputAction.CallbackContext context)
    {
        Debug.Log($"[PlayerController] OnItemInteract fired, phase: {context.phase}");

        if (context.performed)
        {
            playerInteraction.ItemInteract();
        }
    }

    private void MovePlayer()
    {
        float speedPercent = move.magnitude;

        if (animator != null)
            animator.SetFloat("Speed", speedPercent);

        if (move == Vector2.zero || mainCamera == null)
            return;

        Vector3 camForward = mainCamera.transform.forward;
        Vector3 camRight = mainCamera.transform.right;

        camForward.y = 0;
        camRight.y = 0;

        camForward.Normalize();
        camRight.Normalize();

        Vector3 movement = camForward * move.y + camRight * move.x;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(movement),
            rotationSmoothing
        );

        controller.Move(movement * speed * Time.deltaTime);
    }

    private void ApplyGravity()
    {
        if (controller.isGrounded && verticalVelocity < 0)
            verticalVelocity = groundedGravity;
        else
            verticalVelocity += gravity * Time.deltaTime;

        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}
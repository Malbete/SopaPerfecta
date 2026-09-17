using UnityEngine;
using UnityEngine.InputSystem;

public class TomatoController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private InputActionReference movementInput;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private InputActionReference jumpInput;
    [SerializeField] private float raycastDistance = 0.6f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Tamaño")]
    [SerializeField] private float smallScale = 0.6f;
    [SerializeField] private float scaleSpeed = 8f;
    [SerializeField] private InputActionReference smallInput;

    [Header("Calidad")]
    [SerializeField] private float maxQuality = 100f;
    [SerializeField] private float qualityLoss = 5f;

    private Rigidbody rb;
    private Vector3 normalScale;
    private float quality;
    private bool isSmall;
    private bool isGrounded;
    private bool jumpRequested;

    public float Quality => quality;
    public bool IsSmall => isSmall;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        normalScale = transform.localScale;
        quality = maxQuality;
    }

    private void OnEnable()
    {
        movementInput.action.Enable();
        jumpInput.action.Enable();
        smallInput.action.Enable();
    }

    private void OnDisable()
    {
        movementInput.action.Disable();
        jumpInput.action.Disable();
        smallInput.action.Disable();
    }

    private void Update()
    {
        HandleSize();

        if (jumpInput.action.WasPressedThisFrame())
        {
            jumpRequested = true;
        }
    }

    private void FixedUpdate()
    {
        CheckGround();
        HandleMovement();
        HandleJump();
    }

    private void CheckGround()
    {
        // Se calcula la distancia dinámica considerando si el objeto achicó su escala
        float currentRayDistance = raycastDistance * transform.localScale.y;
        isGrounded = Physics.Raycast(transform.position, Vector3.down, currentRayDistance, groundLayer);
    }

    private void HandleMovement()
    {
        Vector2 input = movementInput.action.ReadValue<Vector2>();
        Vector3 movement = new Vector3(input.x, 0f, input.y);

        if (movement.magnitude > 0.01f)
            rb.AddForce(movement.normalized * moveSpeed);
        else
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, 0, deceleration * Time.fixedDeltaTime);
            velocity.z = Mathf.MoveTowards(velocity.z, 0, deceleration * Time.fixedDeltaTime);
            rb.linearVelocity = velocity;
        }

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);

        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(
                horizontalVelocity.x,
                rb.linearVelocity.y,
                horizontalVelocity.z
            );
        }
    }

    private void HandleJump()
    {
        if (jumpRequested && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }

        jumpRequested = false;
    }

    private void HandleSize()
    {
        bool wantsToBeSmall = smallInput.action.IsPressed();

        if (wantsToBeSmall && !isSmall)
        {
            isSmall = true;
            Debug.Log("Se achico");
            quality = Mathf.Clamp(quality - qualityLoss, 0, maxQuality);
        }
        else if (!wantsToBeSmall)
        {
            isSmall = false;
        }

        Vector3 targetScale = normalScale * (isSmall ? smallScale : 1f);

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            scaleSpeed * Time.deltaTime
        );
    }

    private void OnDrawGizmosSelected()
    {
        // Permite visualizar la línea del Raycast en el editor
        Gizmos.color = Color.red;
        float currentRayDistance = raycastDistance * transform.localScale.y;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * currentRayDistance);
    }
}
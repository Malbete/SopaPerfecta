using UnityEngine;
using UnityEngine.InputSystem;

public class Papa : MonoBehaviour
{
    public Papa Instance { get; private set; }

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

    [Header("Dash")]
    [SerializeField] private InputActionReference dashInput;
    [SerializeField] private float dashForce = 15f;
    [SerializeField] private float dashDuration = 0.2f;

    [Header("Calidad")]
    [SerializeField] private float maxQuality = 100f;
    [SerializeField] private float qualityLoss = 0.5f;

    private Rigidbody rb;
    private Vector3 normalScale;
    private Vector3 lastMoveDirection = Vector3.forward;

    private float quality;
    private float dashTimer;

    private bool isSmall;
    private bool isGrounded;
    private bool jumpRequested;
    private bool isDashing;

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
        if (movementInput != null)
            movementInput.action.Enable();

        if (jumpInput != null)
            jumpInput.action.Enable();

        if (dashInput != null)
            dashInput.action.Enable();
    }

    private void OnDisable()
    {
        if (movementInput != null)
            movementInput.action.Disable();

        if (jumpInput != null)
            jumpInput.action.Disable();

        if (dashInput != null)
            dashInput.action.Disable();
    }

    private void Update()
    {
        if (jumpInput != null &&
            jumpInput.action.WasPressedThisFrame())
        {
            jumpRequested = true;
        }

        if (dashInput != null &&
            dashInput.action.WasPressedThisFrame() &&
            !isDashing)
        {
            StartDash();
        }
    }

    private void FixedUpdate()
    {
        CheckGround();

        if (isDashing)
        {
            HandleDash();
        }
        else
        {
            HandleMovement();
        }

        HandleJump();
    }

    private void CheckGround()
    {
        float currentRayDistance =
            raycastDistance * transform.localScale.y;

        isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            currentRayDistance,
            groundLayer
        );
    }

    private void HandleMovement()
    {
        Vector2 input = movementInput.action.ReadValue<Vector2>();

        Vector3 movement = new Vector3(
            input.x,
            0f,
            input.y
        );

        if (movement.magnitude > 0.01f)
        {
            // Guardar la última dirección del input,
            // independientemente de la rotación de la papa.
            lastMoveDirection = movement.normalized;

            rb.AddForce(
                movement.normalized * moveSpeed
            );
        }
        else
        {
            Vector3 velocity = rb.linearVelocity;

            velocity.x = Mathf.MoveTowards(
                velocity.x,
                0f,
                deceleration * Time.fixedDeltaTime
            );

            velocity.z = Mathf.MoveTowards(
                velocity.z,
                0f,
                deceleration * Time.fixedDeltaTime
            );

            rb.linearVelocity = velocity;
        }

        Vector3 horizontalVelocity = new Vector3(
            rb.linearVelocity.x,
            0f,
            rb.linearVelocity.z
        );

        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity =
                horizontalVelocity.normalized * maxSpeed;

            rb.linearVelocity = new Vector3(
                horizontalVelocity.x,
                rb.linearVelocity.y,
                horizontalVelocity.z
            );
        }
    }

    private void StartDash()
    {
        Vector2 input = movementInput.action.ReadValue<Vector2>();

        Vector3 dashDirection = new Vector3(
            input.x,
            0f,
            input.y
        );

        // Si hay una dirección pulsada, usarla.
        // Si no, usar la última dirección de movimiento.
        if (dashDirection.magnitude > 0.01f)
        {
            dashDirection.Normalize();
            lastMoveDirection = dashDirection;
        }
        else
        {
            dashDirection = lastMoveDirection;
        }

        isDashing = true;
        dashTimer = dashDuration;

        // Aplicar el dash sin depender de la rotación física.
        rb.linearVelocity = new Vector3(
            dashDirection.x * dashForce,
            rb.linearVelocity.y,
            dashDirection.z * dashForce
        );
    }

    private void HandleDash()
    {
        dashTimer -= Time.fixedDeltaTime;

        if (dashTimer <= 0f)
        {
            isDashing = false;
        }
    }

    private void HandleJump()
    {
        if (jumpRequested && isGrounded && !isDashing)
        {
            rb.AddForce(
                Vector3.up * jumpForce,
                ForceMode.Impulse
            );
        }

        jumpRequested = false;
    }

    public void LossQuality()
    {
        quality -= qualityLoss;

        quality = Mathf.Clamp(
            quality,
            0f,
            maxQuality
        );

        Debug.Log("Calidad: " + quality);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;

        float currentRayDistance =
            raycastDistance * transform.localScale.y;

        Gizmos.DrawLine(
            transform.position,
            transform.position + Vector3.down * currentRayDistance
        );
    }
}
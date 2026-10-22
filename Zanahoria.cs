using UnityEngine;
using UnityEngine.InputSystem;

public class Zanahoria : MonoBehaviour
{
    public Zanahoria Instance { get; private set; }

    [Header("Movimiento de Auto")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private float turnSpeed = 120f; // Velocidad de giro del volante (grados por segundo)
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

    [Header("Squash & Stretch (Deformación Visual)")]
    [SerializeField] private float stretchFactor = 0.2f;
    [SerializeField] private float scaleSmoothSpeed = 10f;
    private Vector3 originalScale;

    [Header("Calidad")]
    [SerializeField] private float maxQuality = 100f;
    [SerializeField] private float qualityLoss = 0.5f;

    private Rigidbody rb;
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
        quality = maxQuality;
        originalScale = transform.localScale;

        // Congelamos la rotación en X y Z para que la física no la vuelque, 
        // permitiendo que el script controle el giro horizontal (Yaw) como un auto.
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
    }

    private void OnEnable()
    {
        if (movementInput != null) movementInput.action.Enable();
        if (jumpInput != null) jumpInput.action.Enable();
        if (dashInput != null) dashInput.action.Enable();
    }

    private void OnDisable()
    {
        if (movementInput != null) movementInput.action.Disable();
        if (jumpInput != null) jumpInput.action.Disable();
        if (dashInput != null) dashInput.action.Disable();
    }

    private void Update()
    {
        if (jumpInput != null && jumpInput.action.WasPressedThisFrame())
        {
            jumpRequested = true;
        }

        if (dashInput != null && dashInput.action.WasPressedThisFrame() && !isDashing)
        {
            StartDash();
        }

        HandleSquashAndStretch();
    }

    private void FixedUpdate()
    {
        CheckGround();

        if (isDashing)
            HandleDash();
        else
            HandleCarMovement();

        HandleJump();
    }

    private void CheckGround()
    {
        float currentRayDistance = raycastDistance * originalScale.y;

        isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            currentRayDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore
        );
    }

    private void HandleCarMovement()
    {
        if (movementInput == null) return;

        Vector2 input = movementInput.action.ReadValue<Vector2>();
        float moveInput = input.y; // Adelante / Atrás
        float steerInput = input.x; // Izquierda / Derecha (Volante)

        // 1. Manejo del giro tipo auto (solo gira de forma fluida si hay velocidad o input de dirección)
        if (Mathf.Abs(steerInput) > 0.01f)
        {
            // Determina si va hacia adelante o hacia atrás para invertir el giro si retrocede
            float forwardSpeed = Vector3.Dot(rb.linearVelocity, transform.forward);
            float directionMultiplier = forwardSpeed >= -0.1f ? 1f : -1f;

            float rotationAmount = steerInput * turnSpeed * directionMultiplier * Time.fixedDeltaTime;
            transform.Rotate(Vector3.up, rotationAmount);
        }

        // 2. Aceleración en la dirección en la que apunta la zanahoria (eje Forward)
        if (Mathf.Abs(moveInput) > 0.01f)
        {
            rb.AddForce(transform.forward * (moveInput * moveSpeed));
        }
        else
        {
            // Desaceleración gradual cuando no se presiona acelerador
            Vector3 velocity = rb.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, deceleration * Time.fixedDeltaTime);
            velocity.z = Mathf.MoveTowards(velocity.z, 0f, deceleration * Time.fixedDeltaTime);
            rb.linearVelocity = velocity;
        }

        // 3. Limitar la velocidad máxima horizontal
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
    }

    private void HandleSquashAndStretch()
    {
        Vector3 targetScale = originalScale;

        if (!isGrounded)
        {
            targetScale.y = originalScale.y * (1f + stretchFactor);
            targetScale.x = originalScale.x * (1f - stretchFactor * 0.5f);
            targetScale.z = originalScale.z * (1f - stretchFactor * 0.5f);
        }
        else if (isDashing)
        {
            targetScale.y = originalScale.y * (1f - stretchFactor * 0.5f);
            targetScale.x = originalScale.x * (1f + stretchFactor);
            targetScale.z = originalScale.z * (1f + stretchFactor);
        }
        else
        {
            float speedRatio = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z).magnitude / maxSpeed;
            if (speedRatio > 0.1f)
            {
                float wave = Mathf.Sin(Time.time * 15f) * stretchFactor * 0.3f;
                targetScale.y = originalScale.y + wave;
                targetScale.x = originalScale.x - wave * 0.5f;
            }
        }

        transform.localScale = Vector3.Lerp(
            transform.localScale,
            targetScale,
            scaleSmoothSpeed * Time.deltaTime
        );
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;

        rb.linearVelocity = new Vector3(
            transform.forward.x * dashForce,
            rb.linearVelocity.y,
            transform.forward.z * dashForce
        );
    }

    private void HandleDash()
    {
        dashTimer -= Time.fixedDeltaTime;
        if (dashTimer <= 0f)
            isDashing = false;
    }

    private void HandleJump()
    {
        if (jumpRequested && isGrounded && !isDashing)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
        jumpRequested = false;
    }

    public void LossQuality()
    {
        quality = Mathf.Clamp(quality - qualityLoss, 0f, maxQuality);
        Debug.Log("Calidad de la zanahoria: " + quality);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isGrounded ? Color.green : Color.red;
        float distance = raycastDistance * transform.localScale.y;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * distance);
    }
}
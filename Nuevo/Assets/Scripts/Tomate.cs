using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class TomatoController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 20f;
    [SerializeField] private float maxSpeed = 5f;
    [SerializeField] private float deceleration = 15f;
    [SerializeField] private InputActionReference movementInput;
    [SerializeField] private Transform cameraTransform;

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

    private readonly EstadoSuperficieResbalosa surfaceState = new EstadoSuperficieResbalosa();

    public float Quality => quality;
    public bool IsSmall => isSmall;
    public bool IsOnSlipperySurface => surfaceState.IsSlipping;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        normalScale = transform.localScale;
        quality = maxQuality;

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
    }

    private void OnEnable()
    {
        movementInput?.action?.Enable();
        jumpInput?.action?.Enable();
        smallInput?.action?.Enable();
    }

    private void OnDisable()
    {
        surfaceState.Clear();
        jumpRequested = false;
        movementInput?.action?.Disable();
        jumpInput?.action?.Disable();
        smallInput?.action?.Disable();
    }

    private void Update()
    {
        HandleSize();

        if (jumpInput != null && jumpInput.action != null && jumpInput.action.WasPressedThisFrame())
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
        float currentRayDistance = raycastDistance * transform.localScale.y;
        isGrounded = Physics.Raycast(
            transform.position,
            Vector3.down,
            out RaycastHit hit,
            currentRayDistance,
            groundLayer,
            QueryTriggerInteraction.Ignore);

        surfaceState.Update(rb, isGrounded ? hit.collider : null);
    }

    private void HandleMovement()
    {
        Vector2 input = movementInput != null && movementInput.action != null
            ? movementInput.action.ReadValue<Vector2>()
            : Vector2.zero;
        
        Vector3 forward = Vector3.forward;
        Vector3 right = Vector3.right;

        if (cameraTransform != null)
        {
            forward = Vector3.ProjectOnPlane(cameraTransform.forward,Vector3.up);
            right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up);

            if (forward.sqrMagnitude > 0.001f)
                forward.Normalize();
            
            if (right.sqrMagnitude > 0.001f)
                right.Normalize();
        }

        Vector3 movement = forward * input.y + right * input.x;
        movement = Vector3.ClampMagnitude(movement, 1f);

        float acceleration = moveSpeed * surfaceState.AccelerationMultiplier;
        float braking = deceleration * surfaceState.BrakingMultiplier;

        if (movement.magnitude > 0.01f)
        {
            rb.AddForce(movement.normalized * acceleration);
        }
        else
        {
            Vector3 velocity = rb.linearVelocity;
            velocity.x = Mathf.MoveTowards(velocity.x, 0, braking * Time.fixedDeltaTime);
            velocity.z = Mathf.MoveTowards(velocity.z, 0, braking * Time.fixedDeltaTime);
            rb.linearVelocity = velocity;
        }

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude > maxSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * maxSpeed;
            rb.linearVelocity = new Vector3(
                horizontalVelocity.x,
                rb.linearVelocity.y,
                horizontalVelocity.z);
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
        bool wantsToBeSmall = smallInput != null && smallInput.action != null && smallInput.action.IsPressed();

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
            scaleSpeed * Time.deltaTime);
    }

    public void LossQuality()
    {
        quality = Mathf.Clamp(quality - qualityLoss, 0f, maxQuality);
        Debug.Log("Calidad del tomate: " + quality);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = surfaceState.IsSlipping ? Color.cyan : Color.red;
        float currentRayDistance = raycastDistance * transform.localScale.y;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * currentRayDistance);
    }
}

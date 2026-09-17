using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[RequireComponent(typeof(CharacterController))]
public class MovimientoPersonaje : MonoBehaviour
{
    [Header("Movimiento")]
    public Transform camara;
    [Min(0f)] public float velocidadCaminar = 4f;
    [Min(0f)] public float velocidadCorrer = 7f;
    [Min(0f)] public float velocidadGiro = 720f;

    [Header("Salto")]
    [Min(0f)] public float alturaSalto = 1.5f;
    [Tooltip("Aceleracion hacia abajo, en metros por segundo al cuadrado.")]
    [Min(0.1f)] public float gravedad = 20f;

    private CharacterController controlador;
    private float velocidadVertical;

    private void Awake()
    {
        controlador = GetComponent<CharacterController>();
        if (camara == null && Camera.main != null)
            camara = Camera.main.transform;
    }

    private void Update()
    {
        if (Time.deltaTime <= 0f || !controlador.enabled) return;

        LeerTeclado(out Vector2 entrada, out bool correr, out bool saltar);
        entrada = Vector2.ClampMagnitude(entrada, 1f);

        // Direcciones horizontales relativas a la camara.
        Vector3 adelante = Vector3.forward;
        if (camara != null)
        {
            adelante = Vector3.ProjectOnPlane(camara.forward, Vector3.up);
            if (adelante.sqrMagnitude < 0.001f)
                adelante = Vector3.ProjectOnPlane(camara.up, Vector3.up);
            adelante.Normalize();
        }
        Vector3 derecha = Vector3.Cross(Vector3.up, adelante);
        Vector3 direccion = adelante * entrada.y + derecha * entrada.x;

        if (direccion.sqrMagnitude > 0.001f)
        {
            Quaternion giro = Quaternion.LookRotation(direccion, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, giro, velocidadGiro * Time.deltaTime);
        }

        if (controlador.isGrounded && velocidadVertical < 0f)
            velocidadVertical = -2f;

        float aceleracion = Mathf.Max(0.1f, gravedad);
        if (controlador.isGrounded && saltar)
            velocidadVertical = Mathf.Sqrt(2f * aceleracion * Mathf.Max(0f, alturaSalto));

        velocidadVertical -= aceleracion * Time.deltaTime;
        float velocidad = correr ? velocidadCorrer : velocidadCaminar;
        Vector3 movimiento = direccion * velocidad + Vector3.up * velocidadVertical;
        CollisionFlags colisiones = controlador.Move(movimiento * Time.deltaTime);

        // Cancelar el ascenso al golpear un techo.
        if ((colisiones & CollisionFlags.Above) != 0 && velocidadVertical > 0f)
            velocidadVertical = 0f;
        if ((colisiones & CollisionFlags.Below) != 0 && velocidadVertical < 0f)
            velocidadVertical = -2f;
    }

    private static void LeerTeclado(out Vector2 movimiento, out bool correr, out bool saltar)
    {
        movimiento = Vector2.zero;
        correr = false;
        saltar = false;
#if ENABLE_INPUT_SYSTEM
        Keyboard teclado = Keyboard.current;
        if (teclado == null) return;
        if (teclado.wKey.isPressed || teclado.upArrowKey.isPressed) movimiento.y += 1f;
        if (teclado.sKey.isPressed || teclado.downArrowKey.isPressed) movimiento.y -= 1f;
        if (teclado.dKey.isPressed || teclado.rightArrowKey.isPressed) movimiento.x += 1f;
        if (teclado.aKey.isPressed || teclado.leftArrowKey.isPressed) movimiento.x -= 1f;
        correr = teclado.leftShiftKey.isPressed || teclado.rightShiftKey.isPressed;
        saltar = teclado.spaceKey.wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) movimiento.y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) movimiento.y -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) movimiento.x += 1f;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) movimiento.x -= 1f;
        correr = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        saltar = Input.GetKeyDown(KeyCode.Space);
#endif
    }
}

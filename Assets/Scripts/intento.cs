using UnityEngine;
using UnityEngine.InputSystem;

public class CameraTargetController : MonoBehaviour
{
    [Header("Jugador")]
    public Transform jugador;
    public float alturaTarget = 1f;

    [Header("Control Manual")]
    public float sensibilidad = 0.025f;
    public float tiempoParaReactivarAuto = 1f;

    [Header("Movimiento")]
    public float velocidadGiro = 100f;
    public float velocidadSuavizado = 10f;

    [Header("Camara")]
    public Vector3 offsetCamara = new Vector3(0f, 8f, -6f);

    [Header("Deteccion de paredes")]
    public LayerMask capasObstaculos;
    public float radioDeteccion = 0.3f;
    public float intervaloBusqueda = 0.2f;
    public float mejoraMinima = 0.5f;

    private float anguloActual;
    private float anguloObjetivo;

    private float ultimoMovimientoManual;
    private float siguienteBusqueda;

    void Start()
    {
        anguloActual = transform.eulerAngles.y;
        anguloObjetivo = anguloActual;
    }

    void Update()
    {
        ControlManual();
        ControlAutomatico();
    }

    void LateUpdate()
    {
        if (jugador == null)
            return;

        // Seguir al jugador
        transform.position =
            jugador.position + Vector3.up * alturaTarget;

        // Girar suavemente hacia el objetivo
        anguloActual = Mathf.LerpAngle(
            anguloActual,
            anguloObjetivo,
            velocidadSuavizado * Time.deltaTime
        );

        transform.rotation =
            Quaternion.Euler(0f, anguloActual, 0f);
    }

    void ControlManual()
    {
        if (Mouse.current == null)
            return;

        // Mantener clic izquierdo + mover mouse
        if (Mouse.current.leftButton.isPressed)
        {
            float mouseX = Mouse.current.delta.ReadValue().x;

            // Evita movimientos exagerados
            mouseX = Mathf.Clamp(mouseX, -20f, 20f);

            anguloObjetivo += mouseX * sensibilidad;

            // Pausamos temporalmente el automático
            ultimoMovimientoManual = Time.time;
        }
    }

    void ControlAutomatico()
    {
        if (jugador == null)
            return;

        // Si el jugador acaba de mover la cámara,
        // no interferimos.
        if (Time.time - ultimoMovimientoManual < tiempoParaReactivarAuto)
            return;

        if (Time.time < siguienteBusqueda)
            return;

        siguienteBusqueda = Time.time + intervaloBusqueda;

        BuscarMejorAngulo();
    }

    void BuscarMejorAngulo()
    {
        float distanciaActual = ObtenerDistanciaLibre(anguloObjetivo);

        float distanciaCamara = offsetCamara.magnitude;

        // El ángulo actual está libre
        if (distanciaActual >= distanciaCamara - 0.2f)
            return;

        float mejorAngulo = anguloObjetivo;
        float mejorDistancia = distanciaActual;

        float[] angulosPrueba =
        {
            30f,
            -30f,
            45f,
            -45f,
            90f,
            -90f,
            135f,
            -135f,
            180f
        };

        foreach (float diferencia in angulosPrueba)
        {
            float angulo = anguloObjetivo + diferencia;

            float distanciaLibre =
                ObtenerDistanciaLibre(angulo);

            if (distanciaLibre > mejorDistancia + mejoraMinima)
            {
                mejorDistancia = distanciaLibre;
                mejorAngulo = angulo;
            }
        }

        anguloObjetivo = mejorAngulo;
    }

    float ObtenerDistanciaLibre(float angulo)
    {
        Vector3 origen =
            jugador.position + Vector3.up * alturaTarget;

        // Calcula dónde estaría Cinemachine
        Vector3 offsetRotado =
            Quaternion.Euler(0f, angulo, 0f) * offsetCamara;

        Vector3 direccion = offsetRotado.normalized;
        float distancia = offsetRotado.magnitude;

        if (Physics.SphereCast(
            origen,
            radioDeteccion,
            direccion,
            out RaycastHit hit,
            distancia,
            capasObstaculos,
            QueryTriggerInteraction.Ignore))
        {
            return hit.distance;
        }

        return distancia;
    }
}
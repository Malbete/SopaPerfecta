using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CamaraPicado : MonoBehaviour
{
    [Header("Seguimiento")]
    public Transform objetivo;
    [Range(10f, 85f)] public float anguloPicado = 55f;
    [Tooltip("Angulo inicial. Durante el juego la camara busca otros angulos si hay obstaculos.")]
    public float giroHorizontal = 0f;
    [Min(0.1f)] public float distancia = 10f;
    [Tooltip("Altura del punto observado respecto al pivote del personaje.")]
    public float alturaObjetivo = 1f;
    [Min(0f)] public float suavizado = 0.12f;

    [Header("Paredes y giro automatico")]
    public bool girarAutomaticamente = true;
    [Tooltip("Capas con paredes, muebles y otros obstaculos. Se ignoran los colliders del Objetivo y sus hijos.")]
    public LayerMask capasObstaculos = ~0;
    [Min(0.01f)] public float radioColision = 0.2f;
    [Min(0f)] public float margenPared = 0.05f;
    [Min(1f)] public float velocidadGiro = 100f;
    [Min(0.05f)] public float intervaloBusqueda = 0.25f;
    [Tooltip("Espacio extra necesario para cambiar de angulo; evita oscilaciones.")]
    [Min(0.05f)] public float mejoraMinima = 0.5f;

    private readonly RaycastHit[] impactos = new RaycastHit[64];
    private readonly Vector3[] esquinas = new Vector3[4];
    private Camera camara;
    private float giroActual;
    private float giroDestino;
    private float distanciaActual;
    private float velocidadDistancia;
    private float proximaBusqueda;
    private float ultimoGiroConfigurado;
    private bool iniciado;

    private void Awake()
    {
        camara = GetComponent<Camera>();
    }

    private void Start()
    {
        if (objetivo == null)
        {
            Debug.LogWarning("CamaraPicado: arrastra el personaje al campo Objetivo.", this);
            return;
        }
        ActualizarCamara(true);
    }

    private void LateUpdate()
    {
        if (objetivo == null || Time.deltaTime <= 0f) return;
        ActualizarCamara(!iniciado);
    }

    private void ActualizarCamara(bool inmediato)
    {
        Vector3 centro = objetivo.position + Vector3.up * alturaObjetivo;
        float alcance = Mathf.Max(0.1f, distancia);
        float radio = RadioSeguro();

        if (inmediato)
        {
            giroActual = giroDestino = giroHorizontal;
            ultimoGiroConfigurado = giroHorizontal;
            distanciaActual = alcance;
            velocidadDistancia = 0f;
            proximaBusqueda = 0f;
            iniciado = true;
        }

        if (!Mathf.Approximately(ultimoGiroConfigurado, giroHorizontal))
        {
            giroDestino = giroHorizontal;
            ultimoGiroConfigurado = giroHorizontal;
            proximaBusqueda = 0f;
        }

        if (!girarAutomaticamente)
            giroDestino = giroHorizontal;
        else if (inmediato || Time.time >= proximaBusqueda)
        {
            BuscarAngulo(centro, radio, alcance);
            proximaBusqueda = Time.time + Mathf.Max(0.05f, intervaloBusqueda);
        }

        giroActual = inmediato ? giroDestino : Mathf.MoveTowardsAngle(
            giroActual, giroDestino, Mathf.Max(1f, velocidadGiro) * Time.deltaTime);

        // Revisar tambien los angulos intermedios del giro, en cada frame.
        float espacio = DistanciaLibre(centro, giroActual, radio, alcance);
        if (inmediato || espacio < distanciaActual || suavizado <= 0f)
        {
            // Acercamiento inmediato para no suavizar a traves de una pared.
            distanciaActual = espacio;
            velocidadDistancia = 0f;
        }
        else
        {
            distanciaActual = Mathf.SmoothDamp(distanciaActual, espacio,
                ref velocidadDistancia, suavizado);
        }

        Quaternion rotacion = Quaternion.Euler(anguloPicado, giroActual, 0f);
        transform.SetPositionAndRotation(
            centro - rotacion * Vector3.forward * distanciaActual, rotacion);
    }

    private void BuscarAngulo(Vector3 centro, float radio, float alcance)
    {
        float espacioActual = DistanciaLibre(centro, giroDestino, radio, alcance);
        if (espacioActual >= alcance - Mathf.Max(0.05f, margenPared)) return;

        float mejorGiro = giroDestino;
        float mejorPuntaje = espacioActual;

        // 24 direcciones. Favorecer cambios pequenos y conservar el lado elegido
        // mientras siga libre, en lugar de volver continuamente al angulo inicial.
        for (int i = 1; i < 24; i++)
        {
            float candidato = giroDestino + i * 15f;
            float espacio = DistanciaLibre(centro, candidato, radio, alcance);
            if (espacio < espacioActual + Mathf.Max(0.05f, mejoraMinima)) continue;

            float cambio = Mathf.Abs(Mathf.DeltaAngle(giroActual, candidato));
            float puntaje = espacio - alcance * 0.15f * cambio / 180f;
            if (puntaje > mejorPuntaje)
            {
                mejorPuntaje = puntaje;
                mejorGiro = candidato;
            }
        }
        giroDestino = Mathf.Repeat(mejorGiro, 360f);
    }

    private float DistanciaLibre(Vector3 centro, float giro, float radio, float alcance)
    {
        Vector3 direccion = -(Quaternion.Euler(anguloPicado, giro, 0f) * Vector3.forward);
        int cantidad = Physics.SphereCastNonAlloc(centro, radio, direccion, impactos,
            alcance, capasObstaculos, QueryTriggerInteraction.Ignore);
        RaycastHit[] resultados = impactos;

        // Si el buffer se llena, consultar todos para no omitir la pared mas cercana.
        if (cantidad == impactos.Length)
        {
            resultados = Physics.SphereCastAll(centro, radio, direccion,
                alcance, capasObstaculos, QueryTriggerInteraction.Ignore);
            cantidad = resultados.Length;
        }

        float libre = alcance;
        for (int i = 0; i < cantidad; i++)
        {
            Collider obstaculo = resultados[i].collider;
            if (obstaculo == null || obstaculo.transform == objetivo ||
                obstaculo.transform.IsChildOf(objetivo)) continue;
            libre = Mathf.Min(libre, Mathf.Max(0f,
                resultados[i].distance - Mathf.Max(0f, margenPared)));
        }
        return libre;
    }

    private float RadioSeguro()
    {
        // Proteger tambien las esquinas del plano cercano de la camara.
        camara.CalculateFrustumCorners(new Rect(0f, 0f, 1f, 1f),
            camara.nearClipPlane, Camera.MonoOrStereoscopicEye.Mono, esquinas);
        float radio = Mathf.Max(0.01f, radioColision);
        for (int i = 0; i < esquinas.Length; i++)
            radio = Mathf.Max(radio, esquinas[i].magnitude);
        return radio;
    }
}

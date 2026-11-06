using UnityEngine;

// Agregar al mismo GameObject que tiene el CharacterController.
// Compatible con Unity 6. No requiere modificar MovimientoPersonaje.
[RequireComponent(typeof(CharacterController))]
[DisallowMultipleComponent]
public class EmpujarObjetos : MonoBehaviour
{
    [Tooltip("Fuerza de empuje. Los objetos con mayor masa cuestan mas moverlos.")]
    [Min(0f)] public float fuerzaEmpuje = 10f;

    [Tooltip("Velocidad maxima que puede aportar el empuje en su direccion.")]
    [Min(0f)] public float velocidadMaxima = 3f;

    [Tooltip("Solo se empujan objetos de estas capas. Por defecto, todas.")]
    public LayerMask capasEmpujables = ~0;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (!isActiveAndEnabled || Time.deltaTime <= 0f) return;

        Rigidbody cuerpo = hit.collider.attachedRigidbody;
        if (cuerpo == null || cuerpo.isKinematic) return;
        if ((capasEmpujables.value & (1 << hit.collider.gameObject.layer)) == 0) return;

        // Evita empujar el apoyo al caminar encima o aterrizar sobre el.
        if (hit.normal.y > 0.5f || hit.moveDirection.y < -0.3f) return;

        Vector3 direccion = new Vector3(hit.moveDirection.x, 0f, hit.moveDirection.z);
        if (direccion.sqrMagnitude < 0.0001f) return;
        direccion.Normalize();

        float velocidadActual = Vector3.Dot(cuerpo.linearVelocity, direccion);
        float margen = velocidadMaxima - velocidadActual;
        if (margen <= 0f) return;

        // Impulso proporcional al tiempo de este movimiento. Conserva la
        // velocidad vertical y permite que la gravedad siga actuando.
        float impulso = Mathf.Min(fuerzaEmpuje * Time.deltaTime, margen * cuerpo.mass);
        cuerpo.AddForce(direccion * impulso, ForceMode.Impulse);
    }
}

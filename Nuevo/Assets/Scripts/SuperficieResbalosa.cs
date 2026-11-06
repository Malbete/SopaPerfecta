using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public class SuperficieResbalosa : MonoBehaviour
{
    [Header("Respuesta de las verduras sobre esta superficie")]
    [Tooltip("Menor valor: cuesta mas acelerar y corregir la direccion del movimiento.")]
    [Range(0f, 1f)]
    [SerializeField] private float accelerationMultiplier = 0.25f;

    [Tooltip("Menor valor: se conserva mas impulso al soltar las teclas.")]
    [Range(0f, 1f)]
    [SerializeField] private float brakingMultiplier = 0.05f;

    [Tooltip("Menor valor: la zanahoria gira mas despacio sobre esta superficie.")]
    [Range(0f, 1f)]
    [SerializeField] private float steeringMultiplier = 0.35f;

    [Tooltip("Amortiguacion lineal durante el contacto. Se restaura al salir o saltar.")]
    [Min(0f)]
    [SerializeField] private float linearDamping = 0f;

    private readonly Dictionary<Collider, PhysicsMaterial> originalMaterials =
        new Dictionary<Collider, PhysicsMaterial>();
    private PhysicsMaterial slipperyMaterial;

    public float AccelerationMultiplier => accelerationMultiplier;
    public float BrakingMultiplier => brakingMultiplier;
    public float SteeringMultiplier => steeringMultiplier;
    public float LinearDamping => linearDamping;

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (slipperyMaterial == null)
        {
            slipperyMaterial = new PhysicsMaterial("Superficie resbalosa - temporal")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                bounciness = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
        }

        // Afecta los colliders solidos del mismo objeto. Los triggers de Mancha
        // conservan su funcion de detectar contactos y no reciben este material.
        foreach (Collider surfaceCollider in GetComponents<Collider>())
        {
            if (surfaceCollider.isTrigger)
            {
                continue;
            }

            originalMaterials[surfaceCollider] = surfaceCollider.sharedMaterial;
            surfaceCollider.sharedMaterial = slipperyMaterial;
        }

        if (originalMaterials.Count == 0)
        {
            Debug.LogWarning("SuperficieResbalosa necesita un collider con Is Trigger desactivado en el mismo objeto.", this);
        }
    }

    private void OnDisable()
    {
        foreach (KeyValuePair<Collider, PhysicsMaterial> entry in originalMaterials)
        {
            if (entry.Key != null && entry.Key.sharedMaterial == slipperyMaterial)
            {
                entry.Key.sharedMaterial = entry.Value;
            }
        }

        originalMaterials.Clear();
    }

    private void OnDestroy()
    {
        if (slipperyMaterial != null)
        {
            Destroy(slipperyMaterial);
        }
    }
}

// Estado comun de contacto. Es una clase de codigo, no un componente adicional.
// Tomate, papa y zanahoria la usan desde sus controladores existentes.
public sealed class EstadoSuperficieResbalosa
{
    private SuperficieResbalosa currentSurface;
    private Rigidbody currentBody;
    private float previousDamping;

    public bool IsSlipping => currentSurface != null && currentSurface.isActiveAndEnabled;
    public float AccelerationMultiplier => IsSlipping ? currentSurface.AccelerationMultiplier : 1f;
    public float BrakingMultiplier => IsSlipping ? currentSurface.BrakingMultiplier : 1f;
    public float SteeringMultiplier => IsSlipping ? currentSurface.SteeringMultiplier : 1f;

    public void Update(Rigidbody body, Collider groundCollider)
    {
        SuperficieResbalosa surface = groundCollider != null
            ? groundCollider.GetComponentInParent<SuperficieResbalosa>()
            : null;

        if (body == null || surface == null || !surface.isActiveAndEnabled)
        {
            Clear();
            return;
        }

        if (currentBody != body)
        {
            Clear();
            currentBody = body;
            previousDamping = body.linearDamping;
        }

        currentSurface = surface;
        body.linearDamping = surface.LinearDamping;
    }

    public void Clear()
    {
        if (currentBody != null)
        {
            currentBody.linearDamping = previousDamping;
        }

        currentBody = null;
        currentSurface = null;
    }
}

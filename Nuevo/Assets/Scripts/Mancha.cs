using System.Collections.Generic;
using UnityEngine;

public class Mancha : MonoBehaviour
{
    // Varios colliders de una verdura cuentan como un solo contacto con esta mancha.
    private readonly Dictionary<Collider, MonoBehaviour> contacts =
        new Dictionary<Collider, MonoBehaviour>();
    private readonly List<Collider> staleContacts = new List<Collider>();

    private void OnTriggerEnter(Collider other)
    {
        RegisterContact(other);
    }

    private void OnTriggerStay(Collider other)
    {
        // Tambien permite reconocer una verdura que se habilito dentro de la zona.
        // El registro evita repetir la perdida de calidad mientras sigue adentro.
        RegisterContact(other);
    }

    private void OnTriggerExit(Collider other)
    {
        contacts.Remove(other);
    }

    private void OnDisable()
    {
        contacts.Clear();
        staleContacts.Clear();
    }

    private void FixedUpdate()
    {
        RemoveInactiveContacts();
    }

    private void RegisterContact(Collider other)
    {
        if (!isActiveAndEnabled || other == null || !other.enabled || !other.gameObject.activeInHierarchy)
        {
            return;
        }

        RemoveInactiveContacts();
        MonoBehaviour vegetable = FindVegetable(other);
        if (vegetable == null)
        {
            return;
        }

        if (contacts.TryGetValue(other, out MonoBehaviour previous) && previous == vegetable)
        {
            return;
        }

        contacts.Remove(other);
        bool alreadyInside = false;
        foreach (MonoBehaviour owner in contacts.Values)
        {
            if (owner == vegetable)
            {
                alreadyInside = true;
                break;
            }
        }

        contacts[other] = vegetable;
        if (alreadyInside)
        {
            return;
        }

        if (vegetable is TomatoController tomato)
        {
            tomato.LossQuality();
        }
        else if (vegetable is Papa potato)
        {
            potato.LossQuality();
        }
        else if (vegetable is Zanahoria carrot)
        {
            carrot.LossQuality();
        }
    }

    private static MonoBehaviour FindVegetable(Collider other)
    {
        TomatoController tomato = other.GetComponentInParent<TomatoController>();
        if (tomato != null && tomato.isActiveAndEnabled)
        {
            return tomato;
        }

        Papa potato = other.GetComponentInParent<Papa>();
        if (potato != null && potato.isActiveAndEnabled)
        {
            return potato;
        }

        Zanahoria carrot = other.GetComponentInParent<Zanahoria>();
        return carrot != null && carrot.isActiveAndEnabled ? carrot : null;
    }

    private void RemoveInactiveContacts()
    {
        staleContacts.Clear();
        foreach (KeyValuePair<Collider, MonoBehaviour> entry in contacts)
        {
            if (entry.Key == null || !entry.Key.enabled || !entry.Key.gameObject.activeInHierarchy ||
                entry.Value == null || !entry.Value.isActiveAndEnabled)
            {
                staleContacts.Add(entry.Key);
            }
        }

        foreach (Collider collider in staleContacts)
        {
            contacts.Remove(collider);
        }
    }
}

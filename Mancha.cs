using System.Diagnostics;
using UnityEngine;

public class Mancha : MonoBehaviour
{

    private void OnTriggerEnter(Collider collision)
    {
        UnityEngine.Debug.Log("El tomate tocó: " + collision.gameObject.name);

        TomatoController tomato = collision.gameObject.GetComponent<TomatoController>();

        if (tomato != null)
        {
            tomato.LossQuality();
        }
    }

}

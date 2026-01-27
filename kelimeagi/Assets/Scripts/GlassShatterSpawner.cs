using UnityEngine;

/// <summary>
/// Spawns a glass shatter particle effect at a given position.
/// Applies color to particle system.
/// </summary>
public class GlassShatterSpawner : MonoBehaviour
{
    [Tooltip("The glass shatter ParticleSystem prefab to spawn.")]
    public ParticleSystem shatterPrefab;

    /// <summary>
    /// Spawns the shatter effect at the given position with specified color.
    /// </summary>
    public void Spawn(Vector3 position, Color color)
    {
        if (shatterPrefab == null)
        {
            return;
        }

        // Instantiate the prefab
        ParticleSystem ps = Instantiate(shatterPrefab, position, Quaternion.identity);
        if (ps == null) return;

        // Ana particle system rengini ayarla
        var main = ps.main;
        main.startColor = color;
        main.stopAction = ParticleSystemStopAction.Destroy;

        // Child particle sistemleri de varsa renk uygula
        ParticleSystem[] allParticles = ps.GetComponentsInChildren<ParticleSystem>();
        foreach (var childPs in allParticles)
        {
            if (childPs != null)
            {
                var childMain = childPs.main;
                childMain.startColor = color;
            }
        }

        // Renderer sorting (UI uzerinde gorunsun)
        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.sortingOrder = 1500;
            if (SortingLayer.NameToID("UI") != 0)
            {
                psr.sortingLayerName = "UI";
            }
        }

        // Play the effect
        if (!ps.isPlaying)
        {
            ps.Play();
        }
    }
}

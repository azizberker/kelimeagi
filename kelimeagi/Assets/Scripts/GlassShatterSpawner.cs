using UnityEngine;

/// <summary>
/// Spawns a glass shatter particle effect at a given position.
/// All particle settings come from prefab - only sorting is applied from code.
/// Assign the shatterPrefab in the Inspector.
/// </summary>
public class GlassShatterSpawner : MonoBehaviour
{
    [Tooltip("The glass shatter ParticleSystem prefab to spawn.")]
    public ParticleSystem shatterPrefab;

    /// <summary>
    /// Spawns the shatter effect at the given position.
    /// All settings come from prefab - only sorting is modified.
    /// </summary>
    /// <param name="position">World position to spawn the effect.</param>
    /// <param name="color">Not used - prefab color is used instead.</param>
    public void Spawn(Vector3 position, Color color)
    {
        if (shatterPrefab == null)
        {
            Debug.LogWarning("GlassShatterSpawner: shatterPrefab is not assigned!");
            return;
        }

        // Instantiate the prefab
        ParticleSystem ps = Instantiate(shatterPrefab, position, Quaternion.identity);

        // Set stopAction so it auto-destroys
        var main = ps.main;
        main.stopAction = ParticleSystemStopAction.Destroy;

        // Renderer sorting only (to appear above UI)
        ParticleSystemRenderer psr = ps.GetComponent<ParticleSystemRenderer>();
        if (psr != null)
        {
            psr.sortingOrder = 1500;
            if (SortingLayer.NameToID("UI") != 0)
            {
                psr.sortingLayerName = "UI";
            }
        }

        // Play the effect (if not already playing from playOnAwake)
        if (!ps.isPlaying)
        {
            ps.Play();
        }
    }
}

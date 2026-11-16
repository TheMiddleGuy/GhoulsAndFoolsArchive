using UnityEngine;

public class VacuumGhostKillTrigger : MonoBehaviour
{

    [SerializeField] private GameObject specialKeyPrefab;

    public static event System.Action GhostKilled;
    public static event System.Action LastGhostKilled;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ghost"))
        {
            BasicGhostBehavior ghost = other.GetComponentInParent<BasicGhostBehavior>();

            if (ghost != null)
            {
                ghost.ForceReleasePlayer(); // <-- ADD THIS
            }

            if (GhostSpawner.IsLastGhost())
            {
                LastGhostKilled?.Invoke();
                SpawnSpecialKey(other.transform.position);
            }

            Destroy(other.gameObject);
            GhostKilled?.Invoke();
        }
    }

    private void SpawnSpecialKey(Vector3 position)
    {
        if (specialKeyPrefab == null)
        {
            Debug.LogWarning("Special key prefab not assigned!");
            return;
        }

        Vector3 up = Vector3.up;

        GameObject key = Instantiate(specialKeyPrefab, position + up, Quaternion.identity);
        key.GetComponent<ItemScript>().MakeSpecial(true);
    }
}

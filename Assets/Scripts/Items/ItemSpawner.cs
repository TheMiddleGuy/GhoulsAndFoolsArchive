using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private DoorAndKeySpawner _doorAndKeySpawner;
    private List<Transform> _usedLockedRoomsForSpecialItems = new List<Transform>();

    [System.Serializable]
    public class SpawnEntry
    {
        public GameObject itemPrefab;
        public List<Transform> possiblePositionsParents = new List<Transform>();
        public int amount;

        public bool mustSpawnInsideLockedRoom;
        // Offset specifically for the vacuum
        public Vector3 positionOffset = new Vector3(0f, 0f, 0f);
    }

    [SerializeField] private List<SpawnEntry> _itemsToSpawn = new List<SpawnEntry>();

    // Shared memory of used spots
    private HashSet<Transform> _usedPositions = new HashSet<Transform>();

    public void SpawnItems()
    {
        foreach (var entry in _itemsToSpawn)
        {
            // Check if there are positions
            if (entry.possiblePositionsParents == null)
            {
                Debug.LogWarning($"No positions parent for {entry.itemPrefab.name}");
                continue;
            }

            // Gather ONLY unused positions
            List<Transform> availablePositions = new List<Transform>();

            foreach (Transform parent in entry.possiblePositionsParents)
            {
                if (parent == null)
                    continue;

                foreach (Transform child in parent)
                {
                    if (_usedPositions.Contains(child))
                        continue;

                    if (entry.mustSpawnInsideLockedRoom)
                    {
                        if (child.parent == null || child.parent.parent == null)
                            continue;

                        Transform lockedRoom = child.parent.parent;

                        if (!_doorAndKeySpawner.GetLockedRoomSpawnParents().Contains(lockedRoom))
                            continue;

                        if (_usedLockedRoomsForSpecialItems.Contains(lockedRoom))
                            continue;
                    }

                    availablePositions.Add(child);
                }
            }

            // Spawn procedure
            for (int i = 0; i < entry.amount; i++)
            {
                if (availablePositions.Count == 0)
                {
                    Debug.LogWarning($"No free positions left for {entry.itemPrefab.name}");
                    break;
                }

                // Get random point
                int randomIndex = Random.Range(0, availablePositions.Count);
                Transform spawnPoint = availablePositions[randomIndex];

                // Move up a bit
                Vector3 up = new Vector3(0.0f, 0.5f, 0.0f);
                spawnPoint.position += up;

                // Get a random rotation
                float randomRotation = Random.Range(0, 359);
                Quaternion rotation = Quaternion.Euler(0f, randomRotation, 0f);

                // Convert local offset to world offset
                Vector3 rotatedOffset = rotation * entry.positionOffset;

                // Final spawn position
                Vector3 spawnPosition = spawnPoint.position + rotatedOffset;

                // Spawn thing
                Instantiate(entry.itemPrefab, spawnPosition, rotation);
                
                if (entry.mustSpawnInsideLockedRoom)
                {
                    Transform lockedRoom = spawnPoint.parent.parent;
                    _usedLockedRoomsForSpecialItems.Add(lockedRoom);
                }

                // Mark as used globally
                _usedPositions.Add(spawnPoint);

                // Remove locally to avoid reusing within same entry
                availablePositions.RemoveAt(randomIndex);
            }
        }
    }
}
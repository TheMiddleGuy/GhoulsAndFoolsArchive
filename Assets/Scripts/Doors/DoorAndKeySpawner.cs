using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Audio.GeneratorInstance;

public class DoorAndKeySpawner : MonoBehaviour
{
    [SerializeField] private ItemSpawner _itemSpawner;

    [Header("Prefabs")]
    [SerializeField] private GameObject _keyPrefab;
    [SerializeField] private GameObject _lockedDoorPrefab;

    [Header("Lock Setups")]
    [SerializeField] private List<LockSpawnSetup> _lockSetups = new();

    [System.Serializable]
    public class LockSpawnSetup
    {
        [Tooltip("No fucntion, makes things mor readable")]
        public string Name;
        [Tooltip("The ID for the key and the lock(s).")]
        public int LockID;

        [Tooltip("Possible key spawns for this lock")]
        public List<Transform> PossibleKeySpawns = new();

        //0 = no doors (having no possibleDoorOptions also = no doors)
        //There will always spawn 1 key no matter the amount of doors
        [Tooltip("The amount of doors to spawn. Only 1 key no matter the amount of doors.")]
        public int AmountOfDoors = 1;

        public List<DoorSpawnOption> PossibleDoorOptions = new();
    }

    [System.Serializable]
    public class DoorSpawnOption
    {
        [Tooltip("Door that can be replaced by a locked door")]
        public Transform LockSpawn;

        [Tooltip("The parent transform containing all key spawns behind this door")]
        public Transform KeySpawnsBehindDoor;
    }

    private HashSet<Transform> _usedDoorSpawns = new();
    private HashSet<Transform> _usedKeySpawns = new();
    private List<Transform> _blockedOffKeySpawns = new();

    private void Start()
    {
        List<LockSpawnSetup> setupsThatNeedKey = new();

        //First, spawn all doors
        foreach (LockSpawnSetup setup in _lockSetups)
        {
            int doorsToSpawn = Mathf.Max(0, setup.AmountOfDoors);

            for (int i = 0; i < doorsToSpawn; i++)
            {
                DoorSpawnOption chosenDoor = GetRandomUnusedDoor(setup);

                if (chosenDoor == null)
                    break;

                _usedDoorSpawns.Add(chosenDoor.LockSpawn);

                if (chosenDoor.KeySpawnsBehindDoor != null)
                    _blockedOffKeySpawns.Add(chosenDoor.KeySpawnsBehindDoor);

                //Replace the original door with a locked door prefab
                Vector3 doorPosition = chosenDoor.LockSpawn.position;
                Quaternion doorRotation = chosenDoor.LockSpawn.rotation;

                Destroy(chosenDoor.LockSpawn.gameObject);
                GameObject lockedDoor = Instantiate(_lockedDoorPrefab, doorPosition, doorRotation);

                //Give the lock the correct ID
                DoorLock doorLock = lockedDoor.GetComponent<DoorLock>();
                if (doorLock != null)
                    doorLock.SetLockID(setup.LockID);
            }

            setupsThatNeedKey.Add(setup);
        }

        //Second, spawn all keys
        foreach (LockSpawnSetup setup in setupsThatNeedKey)
        {
            Transform keySpawn = GetRandomValidKeySpawn(setup);

            if (keySpawn == null)
                continue;

            _usedKeySpawns.Add(keySpawn);

            //Spawn the key on a valid spawn location (applies a random rotation as well)
            float randomRotation = Random.Range(0, 360);
            Quaternion rotation = Quaternion.Euler(0f, randomRotation, 0f);

            GameObject key = Instantiate(_keyPrefab, keySpawn.position, rotation);

            //Give the key the correct ID
            KeyItemID keyID = key.GetComponent<KeyItemID>();
            if (keyID != null)
                keyID.SetKeyID(setup.LockID);
        }

        _itemSpawner.SpawnItems();
    }

    //Looks between and picks a randomly unused door of the given setup
    private DoorSpawnOption GetRandomUnusedDoor(LockSpawnSetup setup)
    {
        List<DoorSpawnOption> validDoors = new();

        if (setup.PossibleDoorOptions.Count == 0)
        {
            Debug.Log("No doors for " + setup.Name + "--> Spawning only a key.");
            return null;
        }

        foreach (DoorSpawnOption door in setup.PossibleDoorOptions)
        {
            if (door == null || door.LockSpawn == null)
            {
                Debug.Log("Door was invalid.");
                continue;
            }

            if (_usedDoorSpawns.Contains(door.LockSpawn))
                continue;

            validDoors.Add(door);
        }

        if (validDoors.Count == 0)
        {
            Debug.Log("No valid doors found for " + setup.Name);
            return null;
        }

        return validDoors[Random.Range(0, validDoors.Count)];
    }

    //Looks between and picks a randomly unused AND valid key spawn location
    private Transform GetRandomValidKeySpawn(LockSpawnSetup setup)
    {
        List<Transform> validKeySpawns = new();

        foreach (Transform keySpawn in setup.PossibleKeySpawns)
        {
            if (keySpawn == null)
            {
                Debug.Log("Key was somehow invalid.");
                continue;
            }

            if (_usedKeySpawns.Contains(keySpawn))  //Looks for unused key spawns
                continue;

            if (IsBehindAnyLockedDoor(keySpawn))    //Looks for key spawns that are not behind a locked door
                continue;

            validKeySpawns.Add(keySpawn);
        }

        if (validKeySpawns.Count == 0)
        {
            Debug.Log("No valid key spawns found for " + setup.Name);
            return null;
        }

        return validKeySpawns[Random.Range(0, validKeySpawns.Count)];
    }

    //Checks if a key spawn locations is behind a locked door
    private bool IsBehindAnyLockedDoor(Transform keySpawn)
    {
        foreach (Transform blockedParent in _blockedOffKeySpawns)
        {
            if (blockedParent == null)
            {
                Debug.Log("Blocked of room was somehow invalid.");
                continue;
            }

            if (keySpawn.IsChildOf(blockedParent))
                return true;
        }

        return false;
    }

    public List<Transform> GetLockedRoomSpawnParents()
    {
        return _blockedOffKeySpawns;
    }
}
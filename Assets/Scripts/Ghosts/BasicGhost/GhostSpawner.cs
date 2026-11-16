using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class GhostSpawner : MonoBehaviour
{
    // --- Make it singleton ---
    public static GhostSpawner Instance { get; private set; }


    [Header("Prefabs & Counts")]
    public GameObject ghostPrefab;
    public float respawnDelay = 10f;

    [Tooltip("All sections that contain patrol paths.")]
    public List<PatrolSection> PatrolSections = new List<PatrolSection>();

    [System.Serializable]
    public class PatrolSection
    {
        [Tooltip("Has no function, is only for readability.")]
        public string Name;
        [Tooltip("Amount of ghosts that can spawn within this section.")]
        public int GhostCount = 1;

        [Tooltip("The paths of the current section.")]
        public List<PatrolPath> PatrolPaths = new List<PatrolPath>();
    }

    [System.Serializable]
    public class PatrolPath
    {
        [Tooltip("Points of the current path.")]
        public List<Transform> Points = new List<Transform>();
    }

    // runtime
    private List<SpawnedGhost> activeGhosts = new List<SpawnedGhost>();
    private struct PendingRespawn { public float time; public int sectionIndex; public int pathIndex; }
    private List<PendingRespawn> pending = new List<PendingRespawn>();

    private struct SpawnedGhost
    {
        public GameObject ghost;
        public int sectionIndex;
        public int pathIndex;
    }

    [Header("Finale")]
    [SerializeField] private GameObject GhostCounter;

    private static bool _isEndgame = false;
    public static bool IsEndgame
    {
        get { return _isEndgame; }
    }

    private void OnEnable()
    {
        VacuumScript.VacuumFullyUpgraded += StartCounter;
        VacuumGhostKillTrigger.GhostKilled += RefreshCounter;
    }

    private void OnDisable()
    {
        VacuumScript.VacuumFullyUpgraded -= StartCounter;
        VacuumGhostKillTrigger.GhostKilled -= RefreshCounter;
    }

    private void Awake()
    {
        Instance = this;

        _isEndgame = false;
    }

    void Start()
    {
        // 1. Shuffle the patrol paths list before spawning anything
        ShufflePatrolPaths();

        // 2. Initial population
        for (int sectionIndex = 0; sectionIndex < PatrolSections.Count; sectionIndex++)
        {
            PatrolSection section = PatrolSections[sectionIndex];

            for (int i = 0; i < section.GhostCount; i++)
            {
                // We pass 'i' as the index. Because we shuffled, 
                // 'i' will now point to a random path in the list.
                SpawnInSectionOnPath(sectionIndex, i);
            }
        }

    }

    /// <summary>
    /// Randomizes the order of the patrolPaths list using Fisher-Yates shuffle.
    /// </summary>
    private void ShufflePatrolPaths()
    {
        if (PatrolSections == null || PatrolSections.Count == 0) return;

        foreach (PatrolSection section in PatrolSections)
        {
            if (section.PatrolPaths == null || section.PatrolPaths.Count < 2) continue;

            for (int i = 0; i < section.PatrolPaths.Count; i++)
            {
                // Pick a random index from the current position to the end
                int randomIndex = UnityEngine.Random.Range(i, section.PatrolPaths.Count);

                // Swap the elements
                PatrolPath temp = section.PatrolPaths[i];
                section.PatrolPaths[i] = section.PatrolPaths[randomIndex];
                section.PatrolPaths[randomIndex] = temp;
            }
        }
    }

    void Update()
    {
        // Clean up destroyed ghosts and schedule respawns
        for (int i = activeGhosts.Count - 1; i >= 0; i--)
        {
            if (activeGhosts[i].ghost == null)
            {
                // schedule a respawn using a patrol path (choose a random one if available)
                int sectionIndex = activeGhosts[i].sectionIndex;
                int pathIndex = -1;

                if (PatrolSections != null && PatrolSections.Count > sectionIndex)
                {
                    PatrolSection section = PatrolSections[sectionIndex];

                    if (section.PatrolPaths != null && section.PatrolPaths.Count > 0)
                    {
                        pathIndex = GetRandomFreePathIndex(sectionIndex);
                    }
                }

                // schedule new only if not endgame
                if (!_isEndgame) pending.Add(new PendingRespawn { time = Time.time + respawnDelay, sectionIndex = sectionIndex, pathIndex = pathIndex });

                activeGhosts.RemoveAt(i);
                RefreshCounter();
            }
        }

        // Process pending respawns whose time has arrived
        if (!_isEndgame)
        {
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                if (Time.time >= pending[i].time)
                {
                    SpawnInSectionOnPath(pending[i].sectionIndex, pending[i].pathIndex);
                    pending.RemoveAt(i);
                }
            }
        }

        // If for any reason there are fewer than desired (e.g., start without enough), spawn more (unless endgame)
        if (!_isEndgame)
        {
            for (int sectionIndex = 0; sectionIndex < PatrolSections.Count; sectionIndex++)
            {
                PatrolSection section = PatrolSections[sectionIndex];

                while (GetActiveAndPendingCountInSection(sectionIndex) < section.GhostCount)
                {
                    SpawnInSection(sectionIndex);
                }
            }
        }
    }

    private int GetActiveAndPendingCountInSection(int sectionIndex)
    {
        int count = 0;

        foreach (SpawnedGhost ghost in activeGhosts)
        {
            if (ghost.sectionIndex == sectionIndex)
                count++;
        }

        foreach (PendingRespawn respawn in pending)
        {
            if (respawn.sectionIndex == sectionIndex)
                count++;
        }

        return count;
    }

    // Spawn using a patrol path index. If pathIndex is negative or invalid, falls back to random or default spawn.
    private void SpawnInSectionOnPath(int sectionIndex, int pathIndex)
    {
        if (PatrolSections != null && PatrolSections.Count > sectionIndex && sectionIndex >= 0)
        {
            PatrolSection section = PatrolSections[sectionIndex];

            if (section.PatrolPaths != null && section.PatrolPaths.Count > 0 && pathIndex >= 0)
            {
                var path = section.PatrolPaths[pathIndex % section.PatrolPaths.Count];
                if (path != null && path.Points != null && path.Points.Count > 0 && path.Points[0] != null)
                {
                    Vector3 spawnPos = path.Points[0].position;
                    SpawnGhostAtPosition(spawnPos, path, sectionIndex, pathIndex);
                    return;
                }
            }
        }

        // fallback
        SpawnInSection(sectionIndex);
    }

    private void SpawnInSection(int sectionIndex)
    {
        // pick a random patrol path if available
        if (PatrolSections != null && PatrolSections.Count > sectionIndex && sectionIndex >= 0)
        {
            PatrolSection section = PatrolSections[sectionIndex];

            if (section.PatrolPaths != null && section.PatrolPaths.Count > 0)
            {
                int idx = GetRandomFreePathIndex(sectionIndex);
                SpawnInSectionOnPath(sectionIndex, idx);
                return;
            }
        }

        // absolute fallback to spawner transform
        SpawnGhostAtPosition(transform.position, null, sectionIndex, -1);
    }

    private void SpawnRandomSection()
    {
        // pick a random patrol path if available
        if (PatrolSections != null && PatrolSections.Count > 0)
        {
            int idx = UnityEngine.Random.Range(0, PatrolSections.Count);
            SpawnInSection(idx);
            return;
        }

        // absolute fallback to spawner transform
        SpawnGhostAtPosition(transform.position, null, -1, -1);
    }

    private void SpawnGhostAtPosition(Vector3 spawnPos, PatrolPath path, int sectionIndex, int pathIndex)
    {
        if (ghostPrefab == null)
        {
            Debug.LogWarning("GhostSpawner: ghostPrefab is not assigned.");
            return;
        }
        //Vector3 up = new Vector3( 0.0f, 1.5f, 0.0f );
        //spawnPos = spawnPos + up;

        GameObject go = Instantiate(ghostPrefab, spawnPos, Quaternion.identity);
        go.name = "Ghost_" + UnityEngine.Random.Range(1000, 9999);

        var ghostComp = go.GetComponent<BasicGhostBehavior>();
        if (ghostComp != null)
        {
            // assign patrol points (try to find a patrol path that starts at this spawnPos)
            ghostComp.PatrolPoints = new List<Vector3>();

            bool assigned = false;
            if (path != null && path.Points != null && path.Points.Count > 0)
            {
                foreach (var t in path.Points)
                    if (t != null)
                        ghostComp.PatrolPoints.Add(t.position);

                assigned = true;
            }

            if (!assigned)
            {
                // fallback: use any defined patrol path (copy first available) or create a small loop
                if (PatrolSections != null && PatrolSections.Count > 0)
                {
                    var section = PatrolSections[0];

                    if (section != null && section.PatrolPaths != null && section.PatrolPaths.Count > 0)
                    {
                        var p = section.PatrolPaths[0];
                        if (p != null && p.Points != null && p.Points.Count > 0)
                        {
                            foreach (var t in p.Points)
                                if (t != null)
                                    ghostComp.PatrolPoints.Add(t.position);
                        }
                    }
                }

                if (ghostComp.PatrolPoints.Count == 0)
                {
                    ghostComp.PatrolPoints.Add(spawnPos);
                    ghostComp.PatrolPoints.Add(spawnPos + Vector3.forward * 2f);
                    ghostComp.PatrolPoints.Add(spawnPos + Vector3.right * 2f);
                }
            }
        }

        activeGhosts.Add(new SpawnedGhost
        {
            ghost = go,
            sectionIndex = sectionIndex,
            pathIndex = pathIndex
        });
    }

    private void StartCounter()
    {
        _isEndgame = true;
        if (GhostCounter == null)
        {
            Debug.Log("No ghost counter assigned");
            return;
        }
        GhostCounter.SetActive(true);
        RefreshCounter();
    }

    private void RefreshCounter()
    {
        if (!_isEndgame) return;
        if (GhostCounter == null)
        {
            Debug.Log("No ghost counter assigned");
            return;
        }

        GhostCounter.GetComponentInChildren<TextMeshProUGUI>().SetText(activeGhosts.Count.ToString());
    }

    private bool IsPathOccupied(int sectionIndex, int pathIndex)
    {
        foreach (SpawnedGhost ghost in activeGhosts)
        {
            if (ghost.sectionIndex == sectionIndex && ghost.pathIndex == pathIndex)
                return true;
        }

        foreach (PendingRespawn respawn in pending)
        {
            if (respawn.sectionIndex == sectionIndex && respawn.pathIndex == pathIndex)
                return true;
        }

        return false;
    }

    private int GetRandomFreePathIndex(int sectionIndex)
    {
        if (PatrolSections == null || sectionIndex < 0 || sectionIndex >= PatrolSections.Count)
            return -1;

        PatrolSection section = PatrolSections[sectionIndex];

        if (section.PatrolPaths == null || section.PatrolPaths.Count == 0)
            return -1;

        List<int> freePaths = new List<int>();

        for (int i = 0; i < section.PatrolPaths.Count; i++)
        {
            if (!IsPathOccupied(sectionIndex, i))
            {
                freePaths.Add(i);
            }
        }

        if (freePaths.Count > 0)
        {
            return freePaths[UnityEngine.Random.Range(0, freePaths.Count)];
        }

        return UnityEngine.Random.Range(0, section.PatrolPaths.Count);
    }

    public static bool IsLastGhost()
    {
        if (Instance == null)
            return false;

        return (_isEndgame && Instance.activeGhosts.Count == 1);
    }
}
using UnityEngine;
using static UnityEngine.UI.Image;

public class PushSlot : MonoBehaviour
{
    [SerializeField] private DynamicFurniture furniture;
    [SerializeField] private LayerMask obstructionLayers;
    [SerializeField] private float checkDistance = 2f;

    private bool _isAvailable = true; // using this 

    public DynamicFurniture Furniture => furniture;
    public Transform OccupyingPlayer { get; private set; }
    public bool IsOccupied => OccupyingPlayer != null;

    private void Awake()
    {
        if (furniture == null)
            furniture = GetComponentInParent<DynamicFurniture>();
    }

    private void Update()
    {
        if (furniture == null) return;


        Vector3 origin = transform.position + Vector3.up * 0.5f;

        // Direction toward furniture center
        Collider col = furniture.GetComponent<Collider>();
        Vector3 target = col.bounds.center;
        Vector3 dir = target - origin;

        float distance = dir.magnitude;

        if (distance <= 0.01f)
        {
            _isAvailable = true;
            return;
        }

        dir /= distance; // normalize

        // Raycast
        bool blocked = false;

        if (Physics.Raycast(origin, dir, out RaycastHit hit, distance, obstructionLayers, QueryTriggerInteraction.Ignore))
        {
            // If we hit something that's NOT our furniture → blocked
            if (!hit.transform.IsChildOf(furniture.transform))
            {
                blocked = true;
            }
        }
        _isAvailable = !blocked;
    }

    public bool CanAttach(Transform player)
    {
        return !IsOccupied && _isAvailable;
    }

    public bool TryAttach(Transform player)
    {
        if (!CanAttach(player)) return false;
        OccupyingPlayer = player;

        BasicMovement movement = player.GetComponentInParent<BasicMovement>();
        if (movement != null)
        {
            player.GetComponentInParent<Rigidbody>().position = this.transform.position;
            movement.SetPushing(true, this.transform.forward);
        }
        Debug.Log("Player attached to slot " + this.name);
        
        return true;
    }

    public bool TryDetach()
    {
        if (OccupyingPlayer == null) return false;

        BasicMovement movement = OccupyingPlayer.GetComponentInParent<BasicMovement>();
        if (movement != null)
            movement.SetPushing(false, this.transform.forward);

        OccupyingPlayer = null;
        return true;
    }

    public Vector3 GetSignedInput()
    {
        if (OccupyingPlayer == null) return Vector3.zero;

        BasicMovement movement = OccupyingPlayer.GetComponentInParent<BasicMovement>();
        if (movement == null) return Vector3.zero;

        return movement.GetInputDirection();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isAvailable) return;
        if (other == null) return;

        if (!other.CompareTag("Player")) return;

        Debug.Log("Collided with Pickup Slot: " + this.name);
        other.GetComponentInChildren<PlayerPickUp>().ClosestPushSlot = this;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other == null) return;

        if (!other.CompareTag("Player")) return;

        Debug.Log("No longer colliding with Pickup Slot: " + this.name);
        other.GetComponentInChildren<PlayerPickUp>().ClosestPushSlot = null;
    }
}

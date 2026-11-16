using System.Collections.Generic;
using UnityEngine;

public class LightBehavior : MonoBehaviour
{
    [Tooltip("Light triggers make objects more visible within light, but also support the possibility to scare off ghosts.")]
    [SerializeField] private bool _scaresOffGhosts;

    [Header("Detection")]
    [SerializeField] private LayerMask _enemyLayer;
    [SerializeField] private LayerMask _occlusionLayer; // Set this to "Default", "Wall", etc.
    [SerializeField] private Transform _raycastOrigin; // Drag the flashlight tip here

    private List<ItemScript> _itemsInCone = new();

    private void Update()
    {
        HandleItemOcclusion();
    }

    private void HandleItemOcclusion()
    {
        if (_itemsInCone.Count == 0) return;

        // Use the assigned origin or fallback to this transform
        Vector3 origin = _raycastOrigin != null ? _raycastOrigin.position : transform.position;

        foreach (ItemScript item in _itemsInCone)
        {
            if (item == null) continue;

            Vector3 direction = item.transform.position - origin;
            float distance = direction.magnitude;

            // Raycast toward the item. If it hits something on the _occlusionLayer 
            // before reaching the item, the path is blocked.
            if (Physics.Raycast(origin, direction, out RaycastHit hit, distance, _occlusionLayer))
            {
                // We hit a wall or obstacle!
                item.StopSparkle();
            }
            else
            {
                // Path is clear!
                item.Sparkle();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Item"))
        {
            ItemScript item = other.GetComponent<ItemScript>();
            if (item != null && item.GetItemType() != ItemScript.ItemTypes.decor)
            {
                if (!_itemsInCone.Contains(item))
                    _itemsInCone.Add(item);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Item"))
        {
            ItemScript item = other.GetComponent<ItemScript>();
            if (item != null)
            {
                item.StopSparkle(); // Always stop sparkling when leaving the cone
                _itemsInCone.Remove(item);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_scaresOffGhosts) return;

        if (((1 << other.gameObject.layer) & _enemyLayer.value) == 0) return;

        var ghost = other.GetComponentInParent<BasicGhostBehavior>();
        if (ghost != null)
        {
            ghost.StayInLight(transform);
        }
    }

    private void OnDisable()
    {
        foreach (ItemScript item in _itemsInCone)
        {
            if (item != null) item.StopSparkle();
        }
        _itemsInCone.Clear();
    }
}

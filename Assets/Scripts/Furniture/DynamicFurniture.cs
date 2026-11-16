using System.Collections.Generic;
using UnityEngine;

public class DynamicFurniture : MonoBehaviour
{
    [SerializeField] private PushSlot[] slots;
    [SerializeField] private int requiredPlayers = 1;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private LayerMask blockingLayers;

    private Rigidbody _rb;
    private Collider _col;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _col = GetComponent<Collider>();
    }

    private void FixedUpdate()
    {
        int active = 0;
        Vector3 combinedMove = Vector3.zero;

        foreach (PushSlot slot in slots)
        {
            if (slot == null || !slot.IsOccupied) continue;

            active++;

            Vector3 input = slot.GetSignedInput();
            float inputAmount = Vector3.Dot(input, slot.transform.forward);
            combinedMove += slot.transform.forward * inputAmount;
        }

        if (active < requiredPlayers) return;

        Vector3 avg = combinedMove / active;

        if (avg.sqrMagnitude < 0.0001f) return;

        Vector3 move = avg * moveSpeed * Time.fixedDeltaTime;

        //transform.position += move;

        if(CanMove(move))
        {
            _rb.MovePosition(_rb.position + move);

            // Carry attached players manually with the furniture
            foreach (PushSlot slot in slots)
            {
                if (slot == null || !slot.IsOccupied) continue;

                Transform player = slot.OccupyingPlayer;
                var rb = player.GetComponentInParent<Rigidbody>();

                if (rb != null)
                {
                    Vector3 slotpos = slot.transform.position;
                    //rb.MovePosition(new Vector3(slotpos.x, player.transform.position.y, slotpos.z));
                    //rb.MoveRotation(slot.transform.rotation);
                    Vector3 targetPos = new Vector3(slotpos.x, player.transform.position.y, slotpos.z);

                    if (CanMovePlayer(rb, targetPos))
                    {
                        rb.MovePosition(targetPos);
                        rb.MoveRotation(slot.transform.rotation);
                    }
                    else
                    {
                        slot.TryDetach();
                    }
                }
            }
        }
    }

    private bool CanMove(Vector3 move)
    {
        Vector3 center = _col.bounds.center;
        Vector3 halfExtents = _col.bounds.extents;
        Quaternion rotation = transform.rotation;
        float distance = move.magnitude;

        return !Physics.BoxCast(
            center,
            halfExtents,
            move.normalized,
            out _,
            rotation,
            distance,
            blockingLayers,
            QueryTriggerInteraction.Ignore
        );
    }

    private bool CanMovePlayer(Rigidbody playerRb, Vector3 targetPos)
    {
        Collider playerCol = playerRb.GetComponent<Collider>();
        if (playerCol == null) return true;

        Vector3 center = playerCol.bounds.center;
        Vector3 halfExtents = playerCol.bounds.extents * 0.9f;

        Vector3 move = targetPos - playerRb.position;
        float distance = move.magnitude;

        if (distance <= 0.001f) return true;

        return !Physics.BoxCast(
            center,
            halfExtents,
            move.normalized,
            out _,
            playerRb.rotation,
            distance,
            blockingLayers,
            QueryTriggerInteraction.Ignore
        );
    }
}

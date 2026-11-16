using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerPickUp : MonoBehaviour
{
    private InputActionMap _playerActionMap;

    [Header("PickUp UI")]
    [SerializeField] private Canvas _indicator;
    [SerializeField] private Image _indicatorCircle;
    [SerializeField] private Image _indicatorA;
    [SerializeField] private Image _indicatorX;
    [SerializeField] private Vector3 _indicatorOffset = new Vector3(0, 1, 1);
    private Color _playerColor;

    [Header("PickUp")]
    //Sockets for where you hold items
    [SerializeField] private Transform _itemSocket;
    [SerializeField] private Transform _vacuumSocket;
    private readonly List<ItemScript> _itemsInRange = new();    //All ItemScripts of all items in Pick Up range
    //Current held Item
    private GameObject _heldItem;
    private ItemScript _heldItemScript;

    private ReviveGhostBehavior _heldGhost; // Tracks the ghost we are currently holding
    private readonly List<ReviveGhostBehavior> _ghostsInRange = new();

    [SerializeField] private float _singleDropStrenght = 50f;

    [Header("Vacuum")]
    [SerializeField] private Rigidbody _playerRigidBody;
    [SerializeField] private BasicMovement _playerMovement;
    private VacuumScript _heldVacuumScript;
    [SerializeField] private VacuumScript _vacuumInRange;  //The vacuum if it is within range

    [Header("Throw")]
    [SerializeField] private float _throwStrength = 300f;   //The throw strength (Forcemode.Force, so it is mass based)

    [Header("Doors")]
    [SerializeField] private List<DoorLock> _lockedDoors = new();

    [Header("Auto Unlock")]
    [SerializeField] private float _autoUseDistance = 1f;

    private bool _alreadyTriedAutoUpgrade = false;

    [Header("Pushing Furniture")]
    private PushSlot? _closestPushSlot;
    [Header("Line of Sight")]
    [SerializeField] private LayerMask _wallLayerMask;
    public PushSlot? ClosestPushSlot 
    { 
        get => _closestPushSlot; 
        set => _closestPushSlot = value; 
    }

    public bool IsDead = false;

    public static event System.Action<PlayerHealth, ItemScript> ItemPickedUp;

    private void Awake()
    {
        PlayerInput input = GetComponentInParent<PlayerInput>();
        _playerActionMap = input.actions.FindActionMap("Player");

        _playerActionMap.FindAction("PickUp").started += context => TryPickUp();
        _playerActionMap.FindAction("Throw").started += context => Throw();
        _playerActionMap.FindAction("UseItem").started += context => UseItem();
        _playerActionMap.FindAction("UseVacuum").started += context => StartVacuum();
        _playerActionMap.FindAction("UseVacuum").canceled += context => EndVacuum();

        _indicator.transform.SetParent(null);

    }

    //Sets the player color to the player's ui elements
    private void Start()
    {
        _playerColor = GetComponentInParent<PlayerColor>().GetColor();

        _indicatorCircle.color = _playerColor;
    }

    private void OnEnable() => _playerActionMap.Enable();
    private void OnDisable() => _playerActionMap.Disable();

    #region Indicators

    //3 indicator types, PickUp = A, Use = X, None = no indicator
    private enum IndicatorType
    {
        None,
        PickUp,
        Use
    }

    private void Update()
    {
        if (IsDead) return;

        UpdateIndicator();
        UpdateVacuumHudVisibility();
        TryAutoUnlockClosestDoor();
        TryAutoUseUpgradeItem();
    }

    //Checks for the closest Use or PickUp location and places the indicator above it
    private void UpdateIndicator()
    {
        if (_indicator == null) return;

        if (_heldVacuumScript != null)
        {
            ApplyIndicatorVisuals(IndicatorType.None, Vector3.zero);
            return;
        }

        if (_heldGhost != null)
        {
            ApplyIndicatorVisuals(IndicatorType.None, Vector3.zero);
            return;
        }

        IndicatorType closestType = IndicatorType.None;
        Vector3 closestPosition = Vector3.zero;
        float closestDistance = Mathf.Infinity;

        Vector3 playerPosition = transform.position;

        //Checks for all revive ghosts in range
        ReviveGhostBehavior closestGhost = GetClosestGhost();
        if (closestGhost != null)
        {
            float distance = (closestGhost.transform.position - playerPosition).magnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestType = IndicatorType.PickUp;
                closestPosition = closestGhost.transform.position + _indicatorOffset;
            }
        }

        ////Checks for all useable locations in range
        //GameObject useTarget = GetClosestVacuumWhenUpgrading();
        //if (useTarget != null)
        //{
        //    float distance = (useTarget.transform.position - playerPosition).magnitude;
        //    if (distance < closestDistance)
        //    {
        //        closestDistance = distance;
        //        closestType = IndicatorType.Use;
        //        closestPosition = useTarget.transform.position + _indicatorOffset;
        //    }
        //}

        //Checks for all items in range
        ItemScript closestItem = GetClosestItem();
        if (closestItem != null)
        {
            float dist = (closestItem.transform.position - playerPosition).magnitude;
            if (dist < closestDistance)
            {
                closestDistance = dist;
                closestType = IndicatorType.PickUp;
                closestPosition = closestItem.transform.position + _indicatorOffset;
            }
        }

        //Checks for all big object handles in range
        if (ClosestPushSlot != null)
        {
            float distance = (ClosestPushSlot.transform.position - playerPosition).magnitude;
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestType = IndicatorType.PickUp;
                closestPosition = ClosestPushSlot.transform.position + new Vector3(0, 3, 0);
            }
        }

        ////Checks for all locked doors in range
        //DoorLock closestLock = GetClosestDoor();
        //if (closestLock != null)
        //{
        //    float dist = (closestLock.transform.position - playerPosition).magnitude;
        //    if (dist < closestDistance)
        //    {
        //        closestDistance = dist;
        //        closestType = IndicatorType.Use;
        //        closestPosition = closestLock.transform.position + _indicatorOffset;
        //    }
        //}

        ApplyIndicatorVisuals(closestType, closestPosition);
    }

    //Shows the correct indicator on the correct position
    private void ApplyIndicatorVisuals(IndicatorType type, Vector3 position)
    {
        //If indicator type == none, no indicator placement was found and thus the indicator will be invisible
        bool showIndicator = type != IndicatorType.None;

        _indicator.gameObject.SetActive(showIndicator);

        if (!showIndicator) return;

        _indicator.transform.position = position;

        _indicatorA.enabled = type == IndicatorType.PickUp;
        _indicatorX.enabled = type == IndicatorType.Use;
    }

    //Checks for the vacuum when holding an upgrade item
    private GameObject GetClosestVacuumWhenUpgrading()
    {
        if (_heldItemScript == null) return null;
        if (!HoldingUpgradeItem()) return null;

        if (_vacuumInRange != null)
            return _vacuumInRange.gameObject;

        return null;
    }

    private void UpdateVacuumHudVisibility()
    {
        if (_heldVacuumScript != null)
        {
            _heldVacuumScript.SetUpgradeHudVisible();
        }

        if (_vacuumInRange != null && HoldingUpgradeItem())
        {
            _vacuumInRange.SetUpgradeHudVisible();
        }
    }

    #endregion

    #region PickUp

    //Checks what to do when using the Pick Up Button
    private void TryPickUp()
    {
        if (IsDead) return;

        // Furniture first
        if (ClosestPushSlot != null)
        {
            if (ClosestPushSlot.TryDetach()) return;
            else if (ClosestPushSlot.TryAttach(this.transform))
            {
                DropHeldObject();
                return;
            }
        }

        ReviveGhostBehavior closestGhost = GetClosestGhost();

        //Revive ghost gets priority over normal items
        if (closestGhost != null)
        {
            if (_heldGhost == null && _heldItem == null)
            {
                PickUpGhost(closestGhost);
            }
            else
            {
                DropHeldObject(closestGhost.transform.position);
                PickUpGhost(closestGhost);
            }
            return;
        }

        ItemScript closestItem = GetClosestItem();

        if (closestItem != null && _heldItem == null && _heldGhost == null)
        {
            PickUpItem(closestItem);
        }
        else if (closestItem != null && _heldItem != null && _heldVacuumScript == null && _heldGhost == null)
        {
            DropHeldObject(closestItem.transform.position);
            PickUpItem(closestItem);
        }
        else
        {
            if (_heldVacuumScript)
            {
                DropHeldObject(default, transform.forward * - _singleDropStrenght);
            }
            else if (_heldItemScript)
            {
                DropHeldObject(default, transform.forward * _singleDropStrenght);
            }
            else
            {
                DropHeldObject();
            }
        }
    }

    //Picks up the given item
    private void PickUpItem(ItemScript item)
    {
        if (item == null || item.CheckIfCarried()) return;

        PlayerHealth player = GetComponentInParent<PlayerHealth>();
        ItemPickedUp?.Invoke(player, item);

        _heldItemScript = item;
        _heldItem = item.gameObject;

        _heldItemScript.Carry();

        Transform parentSocket = _itemSocket;
        if (_heldItemScript.GetItemType() == ItemScript.ItemTypes.vacuum)
        {
            parentSocket = _vacuumSocket;
            _heldVacuumScript = item.GetComponent<VacuumScript>();
            _heldVacuumScript.TurnNozzleOn(true);
        }
        if (_heldItemScript.GetItemType() == ItemScript.ItemTypes.flashlight)
        {
            this.GetComponentInParent<PlayerHealth>().SetFlashlightState(true);
        }

        _heldItem.transform.position = parentSocket.position;
        _heldItem.transform.rotation = parentSocket.rotation;
        _heldItem.transform.SetParent(parentSocket);

        RemoveItemFromPickUpRange(item);
    }

    private void PickUpGhost(ReviveGhostBehavior ghost)
    {
        if (ghost == null || ghost.IsGrabbed) return;

        _heldGhost = ghost;
        _heldGhost.GrabReviveGhost(_itemSocket);
        ghost.transform.position = _itemSocket.position;
        ghost.transform.SetParent(_itemSocket);

        RemoveGhostFromPickUpRange(ghost);
    }

    public void ForceDropHeldItem()
    {
        DropHeldObject();
    }

    private void DropHeldObject(Vector3 dropPosition = default, Vector3 throwStrength = default)
    {
        if (_heldGhost != null)
        {
            _heldGhost.ReleaseReviveGhost();
            _heldGhost.transform.SetParent(null);

            if (dropPosition != default)
            {
                _heldGhost.transform.position = dropPosition;
            }

            AddGhostToPickUpRange(_heldGhost);
            _heldGhost = null;
            return;
        }

        DropHeldItem(dropPosition, throwStrength);
    }

    //Drops the current held item, dropPosition drops the item at a specified position, throwStrength makes the player throw the item
    private void DropHeldItem(Vector3 dropPosition = default, Vector3 throwStrength = default)
    {
        if (_heldItemScript == null || _heldItem == null) return;

        PlayerHealth player = GetComponentInParent<PlayerHealth>();
        ItemPickedUp?.Invoke(player, null);

        if (_heldItemScript.GetItemType() == ItemScript.ItemTypes.flashlight)
        {
            this.GetComponentInParent<PlayerHealth>().SetFlashlightState(false);
        }

        _heldItemScript.UnCarry(throwStrength);
        _heldItem.transform.SetParent(null);

        if (dropPosition != default)    //Checks if you need to swap out items
        {
            _heldItem.transform.position = dropPosition;
        }

        AddItemToPickUpRange(_heldItemScript);

        _heldItem = null;
        _heldItemScript = null;
        if (_heldVacuumScript != null)
        {
            _heldVacuumScript.TurnVacuumOn(false, _playerRigidBody);
            _heldVacuumScript.TurnNozzleOn(false);
            _heldVacuumScript = null;
        }
    }

    #endregion

    #region Get Closest ...

    //Returns the closest item to the player that is not being held
    private ItemScript GetClosestItem()
    {
        ItemScript closest = null;
        float closestDistance = Mathf.Infinity;
        Vector3 playerPosition = transform.position;

        for (int i = 0; i < _itemsInRange.Count; i++)
        {
            ItemScript item = _itemsInRange[i];

            // 1. Cleanup and Validity Checks
            if (item == null || item.CheckIfCarried())
            {
                _itemsInRange.RemoveAt(i);
                i--;
                continue;
            }

            // 2. Skip if same as held item
            if (_heldItemScript != null && _heldItemScript.GetItemType() == item.GetItemType())
            {
                continue;
            }

            // 3. WALL CHECK (Must be in every loop that selects an item)
            if (!IsPathClear(item.transform.position))
            {
                continue;
            }

            float distance = (item.transform.position - playerPosition).magnitude;

            if (distance < closestDistance)
            {
                closest = item;
                closestDistance = distance;
            }
        }

        // This second block was the culprit. 
        // It is likely intended to find ANY item if a 'specific' one wasn't found,
        // but it still MUST respect wall collision.
        if (closest == null && _itemsInRange.Count > 0)
        {
            for (int i = 0; i < _itemsInRange.Count; i++)
            {
                ItemScript item = _itemsInRange[i];

                if (item == null || item.CheckIfCarried())
                {
                    // Note: Already cleaned up in first loop, but good for safety
                    continue;
                }

                // WALL CHECK (Added here to prevent through-wall pickup fallback)
                if (!IsPathClear(item.transform.position))
                {
                    continue;
                }

                float distance = (item.transform.position - playerPosition).magnitude;

                if (distance < closestDistance)
                {
                    closest = item;
                    closestDistance = distance;
                }
            }
        }

        return closest;
    }

    private DoorLock GetClosestDoor()
    {
        if (!_heldItem) return null;

        if (_heldItemScript.GetItemType() != ItemScript.ItemTypes.key) return null;

        DoorLock closest = null;
        float closestDistance = Mathf.Infinity;
        Vector3 playerPosition = transform.position;

        for (int i = 0; i < _lockedDoors.Count; i++)
        {
            DoorLock door = _lockedDoors[i];

            if (door == null)
            {
                _lockedDoors.RemoveAt(i);
                continue;
            }

            float distance = (door.transform.position - playerPosition).magnitude;

            if (distance < closestDistance)
            {
                closest = door;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private ReviveGhostBehavior GetClosestGhost()
    {
        ReviveGhostBehavior closest = null;
        float closestDistance = Mathf.Infinity;
        Vector3 playerPosition = transform.position;

        for (int i = 0; i < _ghostsInRange.Count; i++)
        {
            ReviveGhostBehavior ghost = _ghostsInRange[i];

            if (ghost == null)
            {
                _ghostsInRange.RemoveAt(i);
                i--;
                continue;
            }

            if (ghost.IsGrabbed)
            {
                _ghostsInRange.RemoveAt(i);
                i--;
                continue;
            }

            float distance = (ghost.transform.position - playerPosition).magnitude;

            if (distance < closestDistance)
            {
                closest = ghost;
                closestDistance = distance;
            }
        }

        return closest;
    }

    #endregion

    #region UseItem

    //private void TryAutoUnlockDoor(DoorLock doorLock)
    //{
    //    if (doorLock == null) return;
    //    if (_heldItem == null || _heldItemScript == null) return;
    //    if (_heldItemScript.GetItemType() != ItemScript.ItemTypes.key) return;

    //    KeyItemID key = _heldItem.GetComponent<KeyItemID>();
    //    if (key == null) return;

    //    doorLock.TryUnlock(key.KeyID, transform);
    //}

    private void TryAutoUnlockClosestDoor()
    {
        if (_heldItem == null || _heldItemScript == null) return;
        if (_heldItemScript.GetItemType() != ItemScript.ItemTypes.key) return;

        DoorLock closestDoor = GetClosestDoor();
        if (closestDoor == null) return;

        float distance = Vector3.Distance(transform.position, closestDoor.transform.position);

        if (distance > _autoUseDistance) return;

        KeyItemID key = _heldItem.GetComponent<KeyItemID>();
        if (key == null) return;

        bool successfullyUsed = closestDoor.TryUnlock(_heldItem.GetComponent<KeyItemID>().KeyID, transform);

        if (successfullyUsed)
        {
            GameObject itemToDestroy = _heldItem;
            DropHeldItem();
            Destroy(itemToDestroy);
        }
    }

    private void TryAutoUseUpgradeItem()
    {
        if (_alreadyTriedAutoUpgrade) return;
        if (_heldItem == null || _heldItemScript == null) return;
        if (!HoldingUpgradeItem()) return;
        if (_vacuumInRange == null) return;

        float distance = Vector3.Distance(transform.position, _vacuumInRange.transform.position);

        if (distance > _autoUseDistance) return;

        _alreadyTriedAutoUpgrade = true;

        bool successfulUpgrade = _vacuumInRange.TryUpgradingWith(_heldItemScript.GetItemType());

        if (successfulUpgrade)
        {
            GameObject itemToDestroy = _heldItem;
            DropHeldItem();
            Destroy(itemToDestroy);
        }
    }

    //Use current held item
    private void UseItem()
    {
        Debug.Log("Using item");
        if (!_heldItem) return;

        //if (_heldItemScript.GetItemType() == ItemScript.ItemTypes.key)
        //{
        //    Debug.Log("Used key");

        //    DoorLock closestLock = GetClosestDoor();

        //    if (closestLock == null)
        //    {
        //        Debug.Log("No locked door in range.");
        //        return;
        //    }

        //    closestLock.TryUnlock(_heldItem.GetComponent<KeyItemID>().KeyID, transform);
        //    //bool successfullyUsed = closestLock.TryUnlock(_heldItem.GetComponent<KeyItemID>().KeyID, transform);

        //    //if (successfullyUsed)
        //    //{
        //    //    GameObject itemToDestroy = _heldItem;
        //    //    DropHeldItem();
        //    //    Destroy(itemToDestroy);
        //    //}
        //}
        if (_heldItemScript.GetItemType() == ItemScript.ItemTypes.flashlight)
        {
            Debug.Log("Used flashlight");
            this.GetComponentInParent<PlayerHealth>().ToggleFlashlight();
        }
        else if (HoldingUpgradeItem())
        {
            Debug.Log("Used " + _heldItemScript.GetItemType());

            if (_vacuumInRange == null) return;

            bool successfulUpgrade = _vacuumInRange.TryUpgradingWith(_heldItemScript.GetItemType());

            if (successfulUpgrade)
            {
                GameObject itemToDestroy = _heldItem;
                DropHeldItem();
                Destroy(itemToDestroy);
            }
        }
    }


    //Using the vacuum
    private void StartVacuum()
    {
        if (_heldItemScript == null) return;
        if (_heldItemScript.GetItemType() != ItemScript.ItemTypes.vacuum) return;

        _heldVacuumScript.TurnVacuumOn(true, _playerRigidBody);
        _playerMovement.StartVacuumSlowness();
    }

    private void EndVacuum()
    {
        if (_heldItemScript == null) return;
        if (_heldItemScript.GetItemType() != ItemScript.ItemTypes.vacuum) return;

        _heldVacuumScript.TurnVacuumOn(false, _playerRigidBody);
        _playerMovement.EndVacuumSlowness();
    }

    #endregion

    //Checks if the player holds an upgrade item
    private bool HoldingUpgradeItem()
    {
        if (_heldItemScript == null) return false;

        ItemScript.ItemTypes type = _heldItemScript.GetItemType();

        return type == ItemScript.ItemTypes.repairParts
            || type == ItemScript.ItemTypes.ductTape
            || type == ItemScript.ItemTypes.cross
            || type == ItemScript.ItemTypes.book;
    }

    #region Throw

    //Throws the current held item
    private void Throw()
    {
        if (_heldGhost != null)
        {
            DropHeldObject();
            return;
        }

        if (_heldItem == null) return;

        if (_heldItemScript.GetItemType() == ItemScript.ItemTypes.vacuum)
        {
            DropHeldItem();
        }
        else
        {
            DropHeldItem(default, (transform.forward + transform.up * 0.5f) * _throwStrength);
        }
    }

    #endregion

    #region ItemsInRange

    //Checks which items enter the player's Pick Up range
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Item"))
        {
            ItemScript itemScript = other.GetComponent<ItemScript>();
            if (itemScript == null) return;

            if (itemScript.GetItemType() == ItemScript.ItemTypes.vacuum)
            {
                _vacuumInRange = other.GetComponent<VacuumScript>();
            }

            if (itemScript.CheckIfCarried()) return;

            AddItemToPickUpRange(itemScript);
        }
        else if (other.CompareTag("Door"))
        {
            Debug.Log("Door in range.");
            DoorLock doorLock = other.GetComponent<DoorLock>();
            if (doorLock == null) return;

            AddDoorToDoorLockRange(doorLock);
            //TryAutoUnlockDoor(doorLock);
        }
        else if (other.CompareTag("ReviveGhost"))
        {
            ReviveGhostBehavior ghost = other.GetComponent<ReviveGhostBehavior>();
            if (ghost == null) return;
            if (ghost.IsGrabbed) return;

            AddGhostToPickUpRange(ghost);
        }

    }

    //Checks which items leave the player's Pick Up range
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Item"))
        {
            ItemScript itemScript = other.GetComponent<ItemScript>();
            if (itemScript == null) return;

            if (itemScript.GetItemType() == ItemScript.ItemTypes.vacuum && other.GetComponent<VacuumScript>() == _vacuumInRange)
            {
                _vacuumInRange = null;
                _alreadyTriedAutoUpgrade = false;
            }

            RemoveItemFromPickUpRange(itemScript);
        }
        else if (other.CompareTag("Door"))
        {
            Debug.Log("Door out range.");
            DoorLock doorLock = other.GetComponent<DoorLock>();
            if (doorLock == null) return;

            RemoveDoorFromDoorLockRange(doorLock);
        }
        else if (other.CompareTag("ReviveGhost"))
        {
            ReviveGhostBehavior ghost = other.GetComponent<ReviveGhostBehavior>();
            if (ghost == null) return;

            RemoveGhostFromPickUpRange(ghost);
        }
    }

    //Adds items to the pick up range list
    private void AddItemToPickUpRange(ItemScript item)
    {
        if (item == null) return;

        if (!_itemsInRange.Contains(item))
            _itemsInRange.Add(item);
    }

    //Removes items from the pick up range list
    private void RemoveItemFromPickUpRange(ItemScript item)
    {
        if (item == null) return;

        _itemsInRange.Remove(item);
    }

    //Adds a locked door to the locked doors list
    private void AddDoorToDoorLockRange(DoorLock doorLock)
    {
        if (doorLock == null) return;

        if (!_lockedDoors.Contains(doorLock))
            _lockedDoors.Add(doorLock);
    }

    //Removes a locked door from the locked doors list
    private void RemoveDoorFromDoorLockRange(DoorLock doorLock)
    {
        if (doorLock == null) return;

        _lockedDoors.Remove(doorLock);
    }

    private void AddGhostToPickUpRange(ReviveGhostBehavior ghost)
    {
        if (ghost == null) return;

        if (!_ghostsInRange.Contains(ghost))
            _ghostsInRange.Add(ghost);
    }

    private void RemoveGhostFromPickUpRange(ReviveGhostBehavior ghost)
    {
        if (ghost == null) return;

        _ghostsInRange.Remove(ghost);
    }
    public bool IsHoldingItem()
    {
        return _heldItem != null;
    }
    #endregion
    private bool IsPathClear(Vector3 itemPosition)
    {
        // 1. Define the start: Center of the item
        // Adding 0.2f ensures we aren't starting inside the floor geometry
        Vector3 origin = itemPosition + Vector3.up * 0.2f;

        // 2. Define the end: Center of the player
        // Typically, transform.position is at the feet, so we add 1.0f for the center
        Vector3 playerCenter = transform.position + Vector3.up * 1.0f;

        // 3. Calculate Vector
        Vector3 direction = playerCenter - origin;
        float distance = direction.magnitude;

        // 4. Perform Raycast
        // We check if anything on the _wallLayerMask exists between origin and player
        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, _wallLayerMask))
        {
            // Path is blocked by something on the Wall layer
            //Debug.DrawLine(origin, hit.point, Color.red);
            return false;
        }

        // Path is clear
        //Debug.DrawLine(origin, playerCenter, Color.green);
        return true;
    }
}
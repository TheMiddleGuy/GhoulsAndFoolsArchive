using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class PlayerIconManager : MonoBehaviour
{
    [System.Serializable]
    public class IconEntry
    {
        public Image ColorIndicator;
        public Image StatusImageHead;
        public Image StatusImageEyes;
        public Image HeldItemImage;
    }

    [Header("Images")]
    // --- Alive / Dead ---
    [SerializeField] private Sprite _aliveImage;
    [SerializeField] private Sprite _deadImage;
    [SerializeField] private Sprite _aliveEyes;
    [SerializeField] private Sprite _deadEyes;
    // --- Items held ---
    [SerializeField] private Sprite _book;
    [SerializeField] private Sprite _cross;
    [SerializeField] private Sprite _vacuum;
    [SerializeField] private Sprite _flashlight;
    [SerializeField] private Sprite _tape;
    [SerializeField] private Sprite _repair;
    [SerializeField] private Sprite _key;

    [Header("Player Icon Display")]
    [SerializeField] private IconEntry[] _playerIcons;

    private void OnEnable()
    {
        PlayerHealth.PlayerStateChanged += UpdatePlayerIcon;
        PlayerSpawner.PlayersSpawned += InitializeAllPlayers;
        PlayerPickUp.ItemPickedUp += UpdateHeldItem;
    }

    private void OnDisable()
    {
        PlayerHealth.PlayerStateChanged -= UpdatePlayerIcon;
        PlayerSpawner.PlayersSpawned -= InitializeAllPlayers;
        PlayerPickUp.ItemPickedUp -= UpdateHeldItem;
    }

    private void Start()
    {
        // --- Hide All ---
        foreach (var icon in _playerIcons)
        {
            icon.ColorIndicator.gameObject.SetActive(false);
            Debug.Log("All disabled!");
        }

        // --- Init ---
        InitializeAllPlayers();
    }

    public void UpdatePlayerIcon(PlayerHealth player)
    {
        int index = player.PlayerIndex;

        if (index < 0 || index >= _playerIcons.Length) return;

        var icon = _playerIcons[index];

        bool isAlive = player.IsAlive;
        icon.StatusImageHead.sprite = (isAlive) 
            ? _aliveImage 
            : _deadImage;
        icon.StatusImageEyes.sprite = (isAlive)
            ? _aliveEyes
            : _deadEyes;
    }

    private void UpdateHeldItem(PlayerHealth player, ItemScript item)
    {
        int index = player.PlayerIndex;

        if (index < 0 || index >= _playerIcons.Length) return;

        var icon = _playerIcons[index];

        if (item == null)
        {
            icon.HeldItemImage.enabled = false;
            return;
        }

        icon.HeldItemImage.enabled = true;

        switch (item.GetItemType())
        {
            case ItemScript.ItemTypes.book:
                icon.HeldItemImage.sprite = _book;
                break;

            case ItemScript.ItemTypes.cross:
                icon.HeldItemImage.sprite = _cross;
                break;

            case ItemScript.ItemTypes.vacuum:
                icon.HeldItemImage.sprite = _vacuum;
                break;

            case ItemScript.ItemTypes.flashlight:
                icon.HeldItemImage.sprite = _flashlight;
                break;

            case ItemScript.ItemTypes.ductTape:
                icon.HeldItemImage.sprite = _tape;
                break;

            case ItemScript.ItemTypes.repairParts:
                icon.HeldItemImage.sprite = _repair;
                break;

            case ItemScript.ItemTypes.key:
                icon.HeldItemImage.sprite = _key;
                break;
        }

        // --- Handle Image Color (namely Key and reset) ---
        if (item.GetItemType() == ItemScript.ItemTypes.key)
        {
            KeyItemID keyData = item.GetComponent<KeyItemID>();

            if (keyData != null)
            {
                icon.HeldItemImage.color =
                    keyData.GetKeyColor();
            }
        }
        else
        {
            icon.HeldItemImage.color = Color.white;
        }
    }

    public void InitializeAllPlayers()
    {
        PlayerHealth[] players = FindObjectsByType<PlayerHealth>(FindObjectsSortMode.None)
            .OrderBy(p => p.PlayerIndex).ToArray();

        foreach (var player in players)
        {
            SetupPlayerIcon(player);
        }

        Debug.Log("All players set up");
    }

    private void SetupPlayerIcon(PlayerHealth player)
    {
        int index = player.PlayerIndex;

        if (index < 0 || index >= _playerIcons.Length) return;

        var icon = _playerIcons[index];

        // --- Disable held item ---
        icon.HeldItemImage.enabled = false;

        // --- Enable ---
        icon.ColorIndicator.gameObject.SetActive(true);
        icon.StatusImageHead.gameObject.SetActive(true);


        // --- Assign Color ---
        Color playerColor = player.GetComponent<PlayerColor>().GetColor();
        icon.ColorIndicator.color = playerColor;
        icon.StatusImageEyes.color = playerColor;

        bool isAlive = player.IsAlive;
        icon.StatusImageHead.sprite = (isAlive)
            ? _aliveImage
            : _deadImage;
        icon.StatusImageEyes.sprite = (isAlive)
            ? _aliveEyes
            : _deadEyes;
    }
}
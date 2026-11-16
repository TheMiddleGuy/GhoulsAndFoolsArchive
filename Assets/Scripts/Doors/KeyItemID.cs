using UnityEngine;

public class KeyItemID : MonoBehaviour
{
    [SerializeField] private int _keyID;
    public int KeyID => _keyID;

    [Header("Style")]
    [SerializeField] private LockColorDatabase _colorDB;
    [SerializeField] private Renderer _renderer;

    private void Start()
    {
        _renderer.material.color = _colorDB.GetColor(_keyID);
    }

    public void SetKeyID(int id)
    {
        _keyID = id;
    }

    public Color GetKeyColor()
    {
        return _colorDB.GetColor(_keyID);
    }
}

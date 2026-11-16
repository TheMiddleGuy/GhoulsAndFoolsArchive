using UnityEngine;

public class PlayerColor : MonoBehaviour
{
    //The color of the current player
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Material _deadMaterial;

    private Material[][] _originalMaterials;

    private Color _playerColor;

    private int _playerId;
    public int PlayerId
    {
        get { return _playerId; } 
        set { _playerId = value; }
    }

    public void SetColor(Color color)
    {
        _playerColor = color;

        Debug.Log("Player " + _playerId + " is getting color " + color);

        foreach (var rend in renderers)
        {
            var mats = rend.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                // Skip body
                if (mats[i].name.Contains("M_PlayerMain"))
                { 
                    continue;
                }

                // Set Glove Color
                if (mats[i].HasProperty("_BaseColor"))
                {
                    Debug.Log("Setting gloves to " + color);
                    mats[i].SetColor("_BaseColor", color);
                }

                // Set Eye color
                if (mats[i].HasProperty("_EmissionColor"))
                {
                    Debug.Log("Setting eyes to " + color);
                    mats[i].SetColor("_EmissionColor", color);
                }
            }

            // Assign modified array back
            rend.materials = mats;
        }

        CacheMaterials();
    }
    public Color GetColor()
    {
        return _playerColor;
    }

    public void SetColorDead()
    {
        // For each body piece 
        foreach (var rend in renderers)
        {
            var mats = rend.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                mats[i] = _deadMaterial;
            }

            rend.materials = mats;
        }
    }

    public void SetColorAlive()
    {
        ResetColors();
    }

    private void CacheMaterials()
    {
        _originalMaterials = new Material[renderers.Length][];

        for (int i = 0; i < renderers.Length; i++)
        {
            _originalMaterials[i] = renderers[i].materials;
        }
    }

    private void ResetColors()
    {
        for (int r = 0; r < renderers.Length; r++)
        {
            renderers[r].materials = _originalMaterials[r];
        }

        SetColor(_playerColor);
    }
}

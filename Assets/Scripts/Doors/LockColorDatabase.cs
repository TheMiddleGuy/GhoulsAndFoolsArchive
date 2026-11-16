using UnityEngine;

[CreateAssetMenu(menuName = "Locks/Lock Color Database")]
public class LockColorDatabase : ScriptableObject
{
    [SerializeField] private Color[] _colors;
    public Color GetColor(int id)
    {
        if (id < 0 || id >= _colors.Length)
            return Color.white;

        return _colors[id];
    }
}
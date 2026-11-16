using UnityEngine;
using UnityEngine.SceneManagement;

public class BootstrapLoaderScript : MonoBehaviour
{
    // --- Created to never be touched again ---
    void Start()
    {
        GameFlowManager.Instance.LoadMainMenu();
    }
}

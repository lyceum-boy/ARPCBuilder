
using UnityEngine;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
using UnityEngine.XR.ARFoundation;
#endif

public class XRSceneLifecycle : MonoBehaviour
{
#if UNITY_EDITOR
    private static XRSceneLifecycle instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name != "ARAssembly")
            return;

        bool deinitialized = LoaderUtility.Deinitialize();
        bool initialized = LoaderUtility.Initialize();

        Debug.Log(
            $"XR Simulation restarted: " +
            $"Deinitialize={deinitialized}, " +
            $"Initialize={initialized}"
        );
    }
#endif
}

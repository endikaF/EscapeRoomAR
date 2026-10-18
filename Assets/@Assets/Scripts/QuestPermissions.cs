using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;
#endif

// Pide en tiempo de ejecución el permiso de escena (necesario para detectar planos en el Quest).
// Ponlo en cualquier objeto de la escena Quest.
public class QuestPermissions : MonoBehaviour
{
    private const string ScenePermission = "com.oculus.permission.USE_SCENE";

    private void Start()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(ScenePermission))
            Permission.RequestUserPermission(ScenePermission);
#endif
    }
}

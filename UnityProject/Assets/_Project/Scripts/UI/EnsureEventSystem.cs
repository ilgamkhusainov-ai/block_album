using UnityEngine;
using UnityEngine.EventSystems;

namespace BlockAlbum.UI
{
    public static class EnsureEventSystem
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureExists()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                EnsureInputModule(existing.gameObject);
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem));
            EnsureInputModule(go);
            Object.DontDestroyOnLoad(go);
        }

        private static void EnsureInputModule(GameObject target)
        {
            var hasModule = target.GetComponent<BaseInputModule>() != null;
            if (hasModule)
            {
                return;
            }

            var inputSystemUiType = System.Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");

            if (inputSystemUiType != null)
            {
                target.AddComponent(inputSystemUiType);
                return;
            }

            target.AddComponent<StandaloneInputModule>();
        }
    }
}

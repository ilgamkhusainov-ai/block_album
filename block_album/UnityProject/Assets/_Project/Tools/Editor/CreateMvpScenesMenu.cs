#if UNITY_EDITOR
using BlockAlbum.Core;
using BlockAlbum.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockAlbum.Tools.Editor
{
    public static class CreateMvpScenesMenu
    {
        private const string BootstrapScenePath = "Assets/_Project/Scenes/Bootstrap.unity";
        private const string GameplayScenePath = "Assets/_Project/Scenes/Gameplay.unity";

        [MenuItem("BlockAlbum/Setup/Create MVP Scenes")]
        public static void CreateMvpScenes()
        {
            CreateBootstrapScene();
            CreateGameplayScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("BlockAlbum", "Scenes created: Bootstrap + Gameplay", "OK");
        }

        private static void CreateBootstrapScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var bootstrap = new GameObject("RuntimeBootstrap");
            bootstrap.AddComponent<RuntimeBootstrap>();

            var portrait = new GameObject("PortraitOnly");
            portrait.AddComponent<PortraitOnly>();

            EditorSceneManager.SaveScene(scene, BootstrapScenePath);
        }

        private static void CreateGameplayScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var portrait = new GameObject("PortraitOnly");
            portrait.AddComponent<PortraitOnly>();

            EnsureEventSystemInScene();

            var canvas = Object.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasObject = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1290f, 2796f); // iPhone 15 Pro Max baseline
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var safeArea = new GameObject("SafeArea");
            safeArea.transform.SetParent(canvas.transform, false);
            var safeRect = safeArea.AddComponent<RectTransform>();
            safeRect.anchorMin = Vector2.zero;
            safeRect.anchorMax = Vector2.one;
            safeRect.offsetMin = Vector2.zero;
            safeRect.offsetMax = Vector2.zero;
            safeArea.AddComponent<SafeAreaFitter>();

            CreateHudPlaceholder(safeArea.transform, "TopBar", new Vector2(0f, 0.88f), new Vector2(1f, 1f));
            CreateHudPlaceholder(safeArea.transform, "BoardArea", new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.85f));
            CreateHudPlaceholder(safeArea.transform, "PieceTray", new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.18f));

            EditorSceneManager.SaveScene(scene, GameplayScenePath);
        }

        private static void CreateHudPlaceholder(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = go.GetComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0.08f);
        }

        private static void EnsureEventSystemInScene()
        {
            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem != null)
            {
                EnsureInputModule(eventSystem.gameObject);
                return;
            }

            var go = new GameObject("EventSystem", typeof(EventSystem));
            EnsureInputModule(go);
        }

        private static void EnsureInputModule(GameObject target)
        {
            if (target.GetComponent<BaseInputModule>() != null)
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
#endif

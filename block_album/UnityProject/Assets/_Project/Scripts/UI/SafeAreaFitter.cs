using UnityEngine;

namespace BlockAlbum.UI
{
    /// <summary>
    /// Fits a RectTransform into the current safe area (notch/dynamic island/home indicator).
    /// Attach to root panel under Canvas.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        [SerializeField] private bool applyOnStart = true;

        private RectTransform _rectTransform;
        private Rect _lastSafeArea;
        private Vector2Int _lastResolution;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void Start()
        {
            if (applyOnStart)
            {
                ApplySafeArea();
            }
        }

        private void Update()
        {
            if (_lastSafeArea != Screen.safeArea ||
                _lastResolution.x != Screen.width ||
                _lastResolution.y != Screen.height)
            {
                ApplySafeArea();
            }
        }

        [ContextMenu("Apply Safe Area")]
        public void ApplySafeArea()
        {
            var safeArea = Screen.safeArea;
            var minAnchor = safeArea.position;
            var maxAnchor = safeArea.position + safeArea.size;

            minAnchor.x /= Screen.width;
            minAnchor.y /= Screen.height;
            maxAnchor.x /= Screen.width;
            maxAnchor.y /= Screen.height;

            _rectTransform.anchorMin = minAnchor;
            _rectTransform.anchorMax = maxAnchor;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;

            _lastSafeArea = safeArea;
            _lastResolution = new Vector2Int(Screen.width, Screen.height);
        }
    }
}

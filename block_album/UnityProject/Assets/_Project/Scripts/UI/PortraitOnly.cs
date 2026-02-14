using UnityEngine;

namespace BlockAlbum.UI
{
    /// <summary>
    /// Keep game in portrait mode at runtime even in Editor play mode.
    /// </summary>
    public sealed class PortraitOnly : MonoBehaviour
    {
        private void Awake()
        {
            Screen.orientation = ScreenOrientation.Portrait;
            Screen.autorotateToPortrait = true;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = false;
            Screen.autorotateToLandscapeRight = false;
        }
    }
}

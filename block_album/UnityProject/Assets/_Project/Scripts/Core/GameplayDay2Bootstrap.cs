using BlockAlbum.Grid;
using BlockAlbum.Pieces;
using BlockAlbum.UI;
using BlockAlbum.Boosters;
using BlockAlbum.Goals;
using UnityEngine;

namespace BlockAlbum.Core
{
    public static class GameplayDay2Bootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SetupGameplayScene()
        {
            var boardArea = GameObject.Find("BoardArea");
            if (boardArea != null && boardArea.GetComponent<BoardView>() == null)
            {
                boardArea.AddComponent<BoardView>();
            }

            var pieceTray = GameObject.Find("PieceTray");
            if (pieceTray != null && pieceTray.GetComponent<PieceTrayView>() == null)
            {
                pieceTray.AddComponent<PieceTrayView>();
            }

            var topBar = GameObject.Find("TopBar");
            if (topBar != null && topBar.GetComponent<TopBarHud>() == null)
            {
                topBar.AddComponent<TopBarHud>();
            }

            var safeArea = GameObject.Find("SafeArea");
            if (safeArea != null && safeArea.GetComponent<BoosterController>() == null)
            {
                safeArea.AddComponent<BoosterController>();
            }

            if (safeArea != null && safeArea.GetComponent<TurnScoreFeedHud>() == null)
            {
                safeArea.AddComponent<TurnScoreFeedHud>();
            }

            if (safeArea != null && safeArea.GetComponent<MatchFlowController>() == null)
            {
                safeArea.AddComponent<MatchFlowController>();
            }

            if (safeArea != null && safeArea.GetComponent<LevelShapePoolIntroHud>() == null)
            {
                safeArea.AddComponent<LevelShapePoolIntroHud>();
            }

            if (safeArea != null && safeArea.GetComponent<AutoHintController>() == null)
            {
                safeArea.AddComponent<AutoHintController>();
            }

            if (topBar != null && topBar.GetComponent<LevelGoalController>() == null)
            {
                topBar.AddComponent<LevelGoalController>();
            }

            if (topBar != null && topBar.GetComponent<LevelProgressionController>() == null)
            {
                topBar.AddComponent<LevelProgressionController>();
            }
        }
    }
}

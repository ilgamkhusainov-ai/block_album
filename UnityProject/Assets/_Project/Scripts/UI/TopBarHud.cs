using System.Collections;
using BlockAlbum.Grid;
using BlockAlbum.Goals;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAlbum.UI
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class TopBarHud : MonoBehaviour
    {
        [SerializeField] private int scoreFontSize = 56;
        [SerializeField] private int detailsFontSize = 32;
        [SerializeField] private Color scoreColor = Color.white;
        [SerializeField] private Color detailsColor = new Color(1f, 1f, 1f, 0.9f);

        private Text _scoreText;
        private Text _detailsText;
        private BoardView _boardView;
        private LevelGoalController _goalController;

        private IEnumerator Start()
        {
            yield return WaitForCanvasReady();
            BuildIfNeeded();
            BindBoard();
            BindGoal();
            RefreshScore(_boardView != null ? _boardView.Score : 0);
            RefreshDetails(BuildIdleDetails());
        }

        private void OnDestroy()
        {
            if (_boardView != null)
            {
                _boardView.TurnResolved -= OnTurnResolved;
                _boardView.RuntimeStateChanged -= OnRuntimeStateChanged;
            }
            if (_goalController != null)
            {
                _goalController.GoalChanged -= OnGoalChanged;
            }
        }

        private void BindBoard()
        {
            _boardView = FindFirstObjectByType<BoardView>();
            if (_boardView == null)
            {
                return;
            }

            _boardView.TurnResolved -= OnTurnResolved;
            _boardView.TurnResolved += OnTurnResolved;
            _boardView.RuntimeStateChanged -= OnRuntimeStateChanged;
            _boardView.RuntimeStateChanged += OnRuntimeStateChanged;
        }

        private void BindGoal()
        {
            _goalController = FindFirstObjectByType<LevelGoalController>();
            if (_goalController == null)
            {
                return;
            }

            _goalController.GoalChanged -= OnGoalChanged;
            _goalController.GoalChanged += OnGoalChanged;
        }

        private void OnTurnResolved(BoardTurnResult result)
        {
            RefreshScore(result.TotalScore);
            if (!result.PlacementSucceeded)
            {
                RefreshDetails(BuildIdleDetails());
                return;
            }

            var details = $"+{result.ScoreGained}";
            if (result.ComboLevel > 0)
            {
                details += $"  Combo x{result.ComboLevel + 1}";
            }
            if (result.LinesCleared > 0 || result.ZonesCleared > 0)
            {
                details += $"  Lines:{result.LinesCleared} Zones:{result.ZonesCleared}";
            }
            if (result.ClearedBlockers > 0)
            {
                details += $"  BlockersCleared:{result.ClearedBlockers}";
            }
            if (result.ClearStreak > 0)
            {
                details += $"  Streak:{result.ClearStreak}";
            }
            details += $"  Power:{result.PowerCharge}/{result.PowerMax}";
            if (result.PowerGained > 0)
            {
                details += $" (+{result.PowerGained})";
            }
            if (_boardView != null)
            {
                details += $"  Blockers:{_boardView.TurnsUntilBlockerMove}";
            }
            if (_goalController != null)
            {
                details += $"  {_goalController.GetStatusLabel()}";
            }
            details += $"  Breakdown:C{result.ScoreFromCells}/B{result.ScoreFromBlockers}/L{result.ScoreFromLines}/Z{result.ScoreFromZones}/K{result.ComboBonusScore}";

            RefreshDetails(details);
        }

        private void OnGoalChanged()
        {
            RefreshDetails(BuildIdleDetails());
        }

        private void OnRuntimeStateChanged()
        {
            if (_boardView != null)
            {
                RefreshScore(_boardView.Score);
            }

            RefreshDetails(BuildIdleDetails());
        }

        private void BuildIfNeeded()
        {
            if (_scoreText != null && _detailsText != null)
            {
                return;
            }

            var scoreNode = EnsureNode("ScoreText", new Vector2(0f, 0f), new Vector2(0.62f, 1f), TextAnchor.MiddleLeft);
            _scoreText = scoreNode.GetComponent<Text>();
            _scoreText.fontSize = scoreFontSize;
            _scoreText.color = scoreColor;

            var detailsNode = EnsureNode("DetailsText", new Vector2(0.38f, 0f), new Vector2(1f, 1f), TextAnchor.MiddleRight);
            _detailsText = detailsNode.GetComponent<Text>();
            _detailsText.fontSize = detailsFontSize;
            _detailsText.color = detailsColor;
        }

        private GameObject EnsureNode(string name, Vector2 anchorMin, Vector2 anchorMax, TextAnchor alignment)
        {
            var existing = transform.Find(name);
            if (existing != null)
            {
                var existingText = existing.GetComponent<Text>();
                if (existingText != null)
                {
                    existingText.font = GetDefaultFont();
                    existingText.alignment = alignment;
                    return existing.gameObject;
                }
            }

            var node = new GameObject(name, typeof(RectTransform), typeof(Text));
            node.transform.SetParent(transform, false);

            var rect = node.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = new Vector2(16f, 0f);
            rect.offsetMax = new Vector2(-16f, 0f);

            var text = node.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            return node;
        }

        private void RefreshScore(int score)
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"SCORE {score}";
            }
        }

        private void RefreshDetails(string value)
        {
            if (_detailsText != null)
            {
                _detailsText.text = value;
            }
        }

        private string BuildIdleDetails()
        {
            if (_boardView == null)
            {
                return "Rule: fill full line or highlighted 3x3 zone";
            }

            var goalText = _goalController != null ? _goalController.GetStatusLabel() : "Goal: n/a";
            return $"{goalText} | Power:{_boardView.PowerCharge}/{_boardView.PowerMax} | Blockers:{_boardView.TurnsUntilBlockerMove}";
        }

        private static Font GetDefaultFont()
        {
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null)
            {
                return font;
            }

            return Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static IEnumerator WaitForCanvasReady()
        {
            var safety = 0;
            while (CanvasUpdateRegistry.IsRebuildingGraphics() || CanvasUpdateRegistry.IsRebuildingLayout())
            {
                if (++safety > 120)
                {
                    break;
                }

                yield return null;
            }

            yield return null;
        }
    }
}

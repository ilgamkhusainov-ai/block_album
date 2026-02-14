using System.Collections;
using BlockAlbum.Boosters;
using BlockAlbum.Goals;
using BlockAlbum.Grid;
using BlockAlbum.Pieces;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAlbum.Core
{
    [DisallowMultipleComponent]
    public sealed class MatchFlowController : MonoBehaviour
    {
        [SerializeField] private bool secondChanceEnabled = true;
        [SerializeField] private int secondChanceExtraTurns = 10;
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.70f);
        [SerializeField] private Color panelColor = new Color(0.12f, 0.14f, 0.20f, 0.96f);
        [SerializeField] private Color restartButtonColor = new Color(0.98f, 0.66f, 0.20f, 1f);
        [SerializeField] private Color secondaryButtonColor = new Color(1f, 1f, 1f, 0.20f);

        private BoardView _boardView;
        private PieceTrayView _pieceTrayView;
        private LevelGoalController _goalController;
        private BoosterController _boosterController;

        private RectTransform _overlayRect;
        private Text _titleText;
        private Text _detailsText;
        private Button _restartButton;
        private Button _secondChanceButton;

        private bool _runEnded;
        private bool _secondChanceUsed;
        private bool _endedWithWin;
        private string _finishReason = string.Empty;
        private bool _suppressEvaluation;

        private IEnumerator Start()
        {
            yield return WaitForCanvasReady();
            BindRuntimeReferences();
            BuildUi();
            HideOverlay();
            EvaluateRunState();
        }

        private void OnDestroy()
        {
            if (_boardView != null)
            {
                _boardView.TurnResolved -= OnTurnResolved;
            }

            if (_pieceTrayView != null)
            {
                _pieceTrayView.TrayChanged -= OnTrayChanged;
            }

            if (_goalController != null)
            {
                _goalController.GoalChanged -= OnGoalChanged;
            }
        }

        private void BindRuntimeReferences()
        {
            if (_boardView == null)
            {
                _boardView = FindFirstObjectByType<BoardView>();
                if (_boardView != null)
                {
                    _boardView.TurnResolved -= OnTurnResolved;
                    _boardView.TurnResolved += OnTurnResolved;
                }
            }

            if (_pieceTrayView == null)
            {
                _pieceTrayView = FindFirstObjectByType<PieceTrayView>();
                if (_pieceTrayView != null)
                {
                    _pieceTrayView.TrayChanged -= OnTrayChanged;
                    _pieceTrayView.TrayChanged += OnTrayChanged;
                }
            }

            if (_goalController == null)
            {
                _goalController = FindFirstObjectByType<LevelGoalController>();
                if (_goalController != null)
                {
                    _goalController.GoalChanged -= OnGoalChanged;
                    _goalController.GoalChanged += OnGoalChanged;
                }
            }

            if (_boosterController == null)
            {
                _boosterController = FindFirstObjectByType<BoosterController>();
            }
        }

        private void OnTurnResolved(BoardTurnResult result)
        {
            if (_suppressEvaluation)
            {
                return;
            }

            EvaluateRunState();
        }

        private void OnTrayChanged()
        {
            if (_suppressEvaluation)
            {
                return;
            }

            EvaluateRunState();
        }

        private void OnGoalChanged()
        {
            if (_suppressEvaluation)
            {
                return;
            }

            EvaluateRunState();
        }

        private void EvaluateRunState()
        {
            if (_runEnded)
            {
                return;
            }

            BindRuntimeReferences();
            if (_boardView == null || _pieceTrayView == null)
            {
                return;
            }

            if (_goalController != null)
            {
                if (_goalController.IsCompleted)
                {
                    EndRun(true, "Goal complete");
                    return;
                }

                if (_goalController.IsFailed)
                {
                    EndRun(false, "Turn limit reached");
                    return;
                }
            }

            if (!_pieceTrayView.HasAnyValidMove())
            {
                EndRun(false, "No valid moves");
            }
        }

        private void EndRun(bool isWin, string reason)
        {
            _runEnded = true;
            _endedWithWin = isWin;
            _finishReason = reason ?? string.Empty;
            ShowOverlay();
        }

        private void ShowOverlay()
        {
            if (_overlayRect == null)
            {
                BuildUi();
            }

            if (_overlayRect == null)
            {
                return;
            }

            _overlayRect.gameObject.SetActive(true);
            var score = _boardView != null ? _boardView.Score : 0;

            if (_titleText != null)
            {
                _titleText.text = _endedWithWin ? "YOU WIN" : "RUN OVER";
            }

            if (_detailsText != null)
            {
                var goalText = _goalController != null ? _goalController.GetStatusLabel() : "Goal: n/a";
                _detailsText.text = $"Score: {score}\n{goalText}\nReason: {_finishReason}";
            }

            if (_secondChanceButton != null)
            {
                var canUseSecondChance = secondChanceEnabled && !_endedWithWin && !_secondChanceUsed;
                _secondChanceButton.gameObject.SetActive(canUseSecondChance);
            }
        }

        private void HideOverlay()
        {
            if (_overlayRect != null)
            {
                _overlayRect.gameObject.SetActive(false);
            }
        }

        private void OnRestartPressed()
        {
            RestartRun();
        }

        private void OnSecondChancePressed()
        {
            if (!_runEnded || _endedWithWin || _secondChanceUsed)
            {
                return;
            }

            _suppressEvaluation = true;
            var continued = false;

            if (_goalController != null)
            {
                continued |= _goalController.GrantExtraTurns(secondChanceExtraTurns);
            }

            if (_pieceTrayView != null)
            {
                continued |= _pieceTrayView.RefillTrayUntilAnyValidMove();
            }

            if (!continued && _boardView != null && _pieceTrayView != null)
            {
                var opened = _boardView.TryForceOpenCellForSecondChance();
                if (opened)
                {
                    continued = _pieceTrayView.RefillTrayUntilAnyValidMove();
                }
            }

            _suppressEvaluation = false;

            if (!continued)
            {
                if (_detailsText != null)
                {
                    _detailsText.text += "\nSecond chance: no valid move available.";
                }
                return;
            }

            _secondChanceUsed = true;
            _runEnded = false;
            _endedWithWin = false;
            _finishReason = string.Empty;
            HideOverlay();
            EvaluateRunState();
        }

        private void RestartRun()
        {
            _suppressEvaluation = true;
            _runEnded = false;
            _endedWithWin = false;
            _finishReason = string.Empty;
            _secondChanceUsed = false;

            if (_boardView != null)
            {
                _boardView.ClearBoard();
            }

            if (_pieceTrayView != null)
            {
                _pieceTrayView.RefillTray();
            }

            if (_goalController != null)
            {
                _goalController.ResetGoalState();
            }

            if (_boosterController != null)
            {
                _boosterController.ResetBoosters();
            }

            _suppressEvaluation = false;
            HideOverlay();
            EvaluateRunState();
        }

        private void BuildUi()
        {
            var overlay = transform.Find("RunEndOverlay") as RectTransform;
            if (overlay == null)
            {
                var overlayGo = new GameObject("RunEndOverlay", typeof(RectTransform), typeof(Image));
                overlay = overlayGo.GetComponent<RectTransform>();
                overlay.SetParent(transform, false);
            }

            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;

            var overlayImage = overlay.GetComponent<Image>();
            overlayImage.color = overlayColor;
            overlayImage.raycastTarget = true;
            _overlayRect = overlay;

            var panel = overlay.Find("Panel") as RectTransform;
            if (panel == null)
            {
                var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
                panel = panelGo.GetComponent<RectTransform>();
                panel.SetParent(overlay, false);
            }

            panel.anchorMin = new Vector2(0.10f, 0.30f);
            panel.anchorMax = new Vector2(0.90f, 0.70f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = panelColor;
            panelImage.raycastTarget = true;

            var panelLayout = panel.GetComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(24, 24, 24, 24);
            panelLayout.spacing = 16f;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlHeight = false;
            panelLayout.childControlWidth = true;
            panelLayout.childForceExpandHeight = false;
            panelLayout.childForceExpandWidth = true;

            _titleText = EnsureText(panel, "TitleText", 64, FontStyle.Bold, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 1f));
            _detailsText = EnsureText(panel, "DetailsText", 30, FontStyle.Normal, TextAnchor.UpperCenter, new Color(1f, 1f, 1f, 0.92f));

            var buttonsRoot = panel.Find("ButtonsRoot") as RectTransform;
            if (buttonsRoot == null)
            {
                var buttonsGo = new GameObject("ButtonsRoot", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                buttonsRoot = buttonsGo.GetComponent<RectTransform>();
                buttonsRoot.SetParent(panel, false);
            }

            var buttonsLayout = buttonsRoot.GetComponent<HorizontalLayoutGroup>();
            buttonsLayout.spacing = 12f;
            buttonsLayout.padding = new RectOffset(0, 0, 8, 0);
            buttonsLayout.childAlignment = TextAnchor.MiddleCenter;
            buttonsLayout.childControlHeight = false;
            buttonsLayout.childControlWidth = false;
            buttonsLayout.childForceExpandHeight = false;
            buttonsLayout.childForceExpandWidth = false;

            _restartButton = EnsureButton(buttonsRoot, "RestartButton", "PLAY AGAIN", restartButtonColor, OnRestartPressed);
            _secondChanceButton = EnsureButton(buttonsRoot, "SecondChanceButton", "SECOND CHANCE", secondaryButtonColor, OnSecondChancePressed);
        }

        private static Text EnsureText(RectTransform parent, string name, int fontSize, FontStyle style, TextAnchor anchor, Color color)
        {
            var node = parent.Find(name) as RectTransform;
            if (node == null)
            {
                var nodeGo = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
                node = nodeGo.GetComponent<RectTransform>();
                node.SetParent(parent, false);
            }

            var text = node.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            var layout = node.GetComponent<LayoutElement>();
            layout.preferredHeight = fontSize <= 32 ? 140f : 88f;
            return text;
        }

        private static Button EnsureButton(RectTransform parent, string name, string label, Color color, UnityEngine.Events.UnityAction action)
        {
            var node = parent.Find(name) as RectTransform;
            if (node == null)
            {
                var nodeGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                node = nodeGo.GetComponent<RectTransform>();
                node.SetParent(parent, false);
            }

            var image = node.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = true;

            var button = node.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);

            var layout = node.GetComponent<LayoutElement>();
            layout.preferredWidth = 280f;
            layout.preferredHeight = 92f;

            var labelNode = node.Find("Label") as RectTransform;
            if (labelNode == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelNode = labelGo.GetComponent<RectTransform>();
                labelNode.SetParent(node, false);
            }

            labelNode.anchorMin = Vector2.zero;
            labelNode.anchorMax = Vector2.one;
            labelNode.offsetMin = Vector2.zero;
            labelNode.offsetMax = Vector2.zero;

            var text = labelNode.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = 30;
            text.fontStyle = FontStyle.Bold;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;
            text.text = label;

            return button;
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

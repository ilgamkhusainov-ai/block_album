using System.Collections;
using BlockAlbum.Grid;
using BlockAlbum.Pieces;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAlbum.Boosters
{
    [DisallowMultipleComponent]
    public sealed class BoosterController : MonoBehaviour
    {
        [SerializeField] private int swapCharges = 1;
        [SerializeField] private int bombHorizontalCharges = 1;
        [SerializeField] private int bombVerticalCharges = 1;
        [SerializeField] private int bombAreaCharges = 1;
        [SerializeField] private int swapPowerCost = 50;
        [SerializeField] private int bombHorizontalPowerCost = 20;
        [SerializeField] private int bombVerticalPowerCost = 20;
        [SerializeField] private int bombAreaPowerCost = 30;

        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.22f);
        [SerializeField] private Color buttonColor = new Color(1f, 1f, 1f, 0.25f);
        [SerializeField] private Color selectedButtonColor = new Color(0.96f, 0.64f, 0.18f, 0.9f);
        [SerializeField] private Color disabledButtonColor = new Color(1f, 1f, 1f, 0.10f);

        private BoardView _boardView;
        private PieceTrayView _pieceTrayView;

        private Button _swapButton;
        private Button _horizontalButton;
        private Button _verticalButton;
        private Button _areaButton;
        private Text _modeText;

        private PendingBomb _pendingBomb = PendingBomb.None;
        private int _initialSwapCharges;
        private int _initialBombHorizontalCharges;
        private int _initialBombVerticalCharges;
        private int _initialBombAreaCharges;

        private enum PendingBomb
        {
            None,
            Horizontal,
            Vertical,
            Area3x3
        }

        private IEnumerator Start()
        {
            yield return WaitForCanvasReady();
            CaptureInitialCharges();
            BindRuntimeReferences();
            BuildUi();
            UpdateUi();
        }

        private void OnDestroy()
        {
            if (_boardView != null)
            {
                _boardView.BoardCellClicked -= OnBoardCellClicked;
                _boardView.RuntimeStateChanged -= OnBoardStateChanged;
            }
        }

        private void BindRuntimeReferences()
        {
            _boardView = FindFirstObjectByType<BoardView>();
            _pieceTrayView = FindFirstObjectByType<PieceTrayView>();

            if (_boardView != null)
            {
                _boardView.BoardCellClicked -= OnBoardCellClicked;
                _boardView.BoardCellClicked += OnBoardCellClicked;
                _boardView.RuntimeStateChanged -= OnBoardStateChanged;
                _boardView.RuntimeStateChanged += OnBoardStateChanged;
            }
        }

        public void ResetBoosters()
        {
            swapCharges = _initialSwapCharges;
            bombHorizontalCharges = _initialBombHorizontalCharges;
            bombVerticalCharges = _initialBombVerticalCharges;
            bombAreaCharges = _initialBombAreaCharges;
            _pendingBomb = PendingBomb.None;
            UpdateUi();
        }

        private void BuildUi()
        {
            var panel = transform.Find("BoosterPanel") as RectTransform;
            if (panel == null)
            {
                var panelGo = new GameObject("BoosterPanel", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup));
                panel = panelGo.GetComponent<RectTransform>();
                panel.SetParent(transform, false);
            }

            panel.anchorMin = new Vector2(0.02f, 0.18f);
            panel.anchorMax = new Vector2(0.98f, 0.24f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = panelColor;
            panelImage.raycastTarget = false;

            var layout = panel.GetComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(8, 8, 6, 6);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;

            _swapButton = EnsureButton(panel, "SwapButton", OnSwapPressed);
            _horizontalButton = EnsureButton(panel, "BombHButton", OnHorizontalPressed);
            _verticalButton = EnsureButton(panel, "BombVButton", OnVerticalPressed);
            _areaButton = EnsureButton(panel, "BombAreaButton", OnAreaPressed);

            var modeGo = transform.Find("BoosterModeText")?.gameObject;
            if (modeGo == null)
            {
                modeGo = new GameObject("BoosterModeText", typeof(RectTransform), typeof(Text));
                modeGo.transform.SetParent(transform, false);
            }

            var modeRect = modeGo.GetComponent<RectTransform>();
            modeRect.anchorMin = new Vector2(0.02f, 0.24f);
            modeRect.anchorMax = new Vector2(0.98f, 0.27f);
            modeRect.offsetMin = Vector2.zero;
            modeRect.offsetMax = Vector2.zero;

            _modeText = modeGo.GetComponent<Text>();
            _modeText.font = GetDefaultFont();
            _modeText.fontSize = 26;
            _modeText.alignment = TextAnchor.MiddleCenter;
            _modeText.color = new Color(1f, 1f, 1f, 0.9f);
            _modeText.raycastTarget = false;
        }

        private Button EnsureButton(RectTransform parent, string name, UnityEngine.Events.UnityAction action)
        {
            var existing = parent.Find(name) as RectTransform;
            if (existing == null)
            {
                var buttonGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
                existing = buttonGo.GetComponent<RectTransform>();
                existing.SetParent(parent, false);
            }

            var image = existing.GetComponent<Image>();
            image.color = buttonColor;

            var button = existing.GetComponent<Button>();
            button.targetGraphic = image;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);

            var layout = existing.GetComponent<LayoutElement>();
            layout.preferredWidth = 150f;
            layout.preferredHeight = 64f;

            var labelTransform = existing.Find("Label");
            Text label;
            if (labelTransform == null)
            {
                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(existing, false);
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                label = labelGo.GetComponent<Text>();
            }
            else
            {
                label = labelTransform.GetComponent<Text>();
            }

            label.font = GetDefaultFont();
            label.fontSize = 30;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;

            return button;
        }

        private void OnSwapPressed()
        {
            if (!CanUseSwap())
            {
                return;
            }

            if (!_boardView.TrySpendPower(swapPowerCost))
            {
                UpdateUi();
                return;
            }

            _pieceTrayView.RefillTray();
            swapCharges--;
            _pendingBomb = PendingBomb.None;
            UpdateUi();
        }

        private void OnHorizontalPressed()
        {
            if (_pendingBomb == PendingBomb.Horizontal)
            {
                _pendingBomb = PendingBomb.None;
                UpdateUi();
                return;
            }

            if (CanArmHorizontalBomb())
            {
                _pendingBomb = PendingBomb.Horizontal;
                UpdateUi();
            }
        }

        private void OnVerticalPressed()
        {
            if (_pendingBomb == PendingBomb.Vertical)
            {
                _pendingBomb = PendingBomb.None;
                UpdateUi();
                return;
            }

            if (CanArmVerticalBomb())
            {
                _pendingBomb = PendingBomb.Vertical;
                UpdateUi();
            }
        }

        private void OnAreaPressed()
        {
            if (_pendingBomb == PendingBomb.Area3x3)
            {
                _pendingBomb = PendingBomb.None;
                UpdateUi();
                return;
            }

            if (CanArmAreaBomb())
            {
                _pendingBomb = PendingBomb.Area3x3;
                UpdateUi();
            }
        }

        private void OnBoardCellClicked(Vector2Int cell)
        {
            if (_boardView == null || _pendingBomb == PendingBomb.None)
            {
                return;
            }

            if (!IsTapCellTriggerable(cell))
            {
                UpdateUi();
                return;
            }

            var applied = false;
            var powerCost = 0;
            switch (_pendingBomb)
            {
                case PendingBomb.Horizontal:
                    if (!WouldHorizontalBombAffect(cell.y))
                    {
                        break;
                    }
                    powerCost = bombHorizontalPowerCost;
                    if (!_boardView.TrySpendPower(powerCost))
                    {
                        break;
                    }
                    applied = _boardView.TryApplyBombHorizontal(cell.y);
                    if (applied) bombHorizontalCharges--;
                    break;
                case PendingBomb.Vertical:
                    if (!WouldVerticalBombAffect(cell.x))
                    {
                        break;
                    }
                    powerCost = bombVerticalPowerCost;
                    if (!_boardView.TrySpendPower(powerCost))
                    {
                        break;
                    }
                    applied = _boardView.TryApplyBombVertical(cell.x);
                    if (applied) bombVerticalCharges--;
                    break;
                case PendingBomb.Area3x3:
                    if (!WouldAreaBombAffect(cell))
                    {
                        break;
                    }
                    powerCost = bombAreaPowerCost;
                    if (!_boardView.TrySpendPower(powerCost))
                    {
                        break;
                    }
                    applied = _boardView.TryApplyBombArea3x3(cell);
                    if (applied) bombAreaCharges--;
                    break;
            }

            if (applied)
            {
                _pendingBomb = PendingBomb.None;
            }

            UpdateUi();
        }

        private void UpdateUi()
        {
            if (_pendingBomb == PendingBomb.Horizontal && !CanArmHorizontalBomb())
            {
                _pendingBomb = PendingBomb.None;
            }
            else if (_pendingBomb == PendingBomb.Vertical && !CanArmVerticalBomb())
            {
                _pendingBomb = PendingBomb.None;
            }
            else if (_pendingBomb == PendingBomb.Area3x3 && !CanArmAreaBomb())
            {
                _pendingBomb = PendingBomb.None;
            }

            UpdateButton(_swapButton, $"SWAP {swapCharges} ({swapPowerCost})", CanUseSwap(), false);
            UpdateButton(_horizontalButton, $"B-H {bombHorizontalCharges} ({bombHorizontalPowerCost})", CanArmHorizontalBomb(), _pendingBomb == PendingBomb.Horizontal);
            UpdateButton(_verticalButton, $"B-V {bombVerticalCharges} ({bombVerticalPowerCost})", CanArmVerticalBomb(), _pendingBomb == PendingBomb.Vertical);
            UpdateButton(_areaButton, $"B-3 {bombAreaCharges} ({bombAreaPowerCost})", CanArmAreaBomb(), _pendingBomb == PendingBomb.Area3x3);

            if (_modeText == null)
            {
                return;
            }

            var powerText = _boardView != null ? $"Power {_boardView.PowerCharge}/{_boardView.PowerMax}" : "Power n/a";
            _modeText.text = _pendingBomb switch
            {
                PendingBomb.Horizontal => $"Bomb H ready: tap board row | {powerText}",
                PendingBomb.Vertical => $"Bomb V ready: tap board column | {powerText}",
                PendingBomb.Area3x3 => $"Bomb 3x3 ready: tap center cell | {powerText}",
                _ => $"Boosters ready | {powerText}",
            };
        }

        private bool CanUseSwap()
        {
            return swapCharges > 0 && _pieceTrayView != null && HasEnoughPower(swapPowerCost);
        }

        private bool CanArmHorizontalBomb()
        {
            return bombHorizontalCharges > 0 && HasEnoughPower(bombHorizontalPowerCost);
        }

        private bool CanArmVerticalBomb()
        {
            return bombVerticalCharges > 0 && HasEnoughPower(bombVerticalPowerCost);
        }

        private bool CanArmAreaBomb()
        {
            return bombAreaCharges > 0 && HasEnoughPower(bombAreaPowerCost);
        }

        private bool HasEnoughPower(int cost)
        {
            if (_boardView == null)
            {
                return false;
            }

            return _boardView.PowerCharge >= Mathf.Max(0, cost);
        }

        private bool WouldHorizontalBombAffect(int row)
        {
            if (_boardView == null || _boardView.Model == null)
            {
                return false;
            }

            var size = _boardView.Model.Size;
            if (row < 0 || row >= size)
            {
                return false;
            }

            for (var x = 0; x < size; x++)
            {
                var cell = new Vector2Int(x, row);
                if (_boardView.Model.IsOccupied(cell) || _boardView.Model.IsBlocked(cell))
                {
                    return true;
                }
            }

            return false;
        }

        private bool WouldVerticalBombAffect(int column)
        {
            if (_boardView == null || _boardView.Model == null)
            {
                return false;
            }

            var size = _boardView.Model.Size;
            if (column < 0 || column >= size)
            {
                return false;
            }

            for (var y = 0; y < size; y++)
            {
                var cell = new Vector2Int(column, y);
                if (_boardView.Model.IsOccupied(cell) || _boardView.Model.IsBlocked(cell))
                {
                    return true;
                }
            }

            return false;
        }

        private bool WouldAreaBombAffect(Vector2Int center)
        {
            if (_boardView == null || _boardView.Model == null)
            {
                return false;
            }

            for (var y = center.y - 1; y <= center.y + 1; y++)
            {
                for (var x = center.x - 1; x <= center.x + 1; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (!_boardView.Model.InBounds(cell))
                    {
                        continue;
                    }

                    if (_boardView.Model.IsOccupied(cell) || _boardView.Model.IsBlocked(cell))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsTapCellTriggerable(Vector2Int cell)
        {
            if (_boardView == null || _boardView.Model == null)
            {
                return false;
            }

            if (!_boardView.Model.InBounds(cell))
            {
                return false;
            }

            return _boardView.Model.IsOccupied(cell) || _boardView.Model.IsBlocked(cell);
        }

        private void OnBoardStateChanged()
        {
            UpdateUi();
        }

        private void UpdateButton(Button button, string label, bool enabled, bool selected)
        {
            if (button == null)
            {
                return;
            }

            button.interactable = enabled;
            var image = button.GetComponent<Image>();
            if (image != null)
            {
                if (!enabled)
                {
                    image.color = disabledButtonColor;
                }
                else
                {
                    image.color = selected ? selectedButtonColor : buttonColor;
                }
            }

            var text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }
        }

        private void CaptureInitialCharges()
        {
            if (_initialSwapCharges != 0 || _initialBombHorizontalCharges != 0 || _initialBombVerticalCharges != 0 || _initialBombAreaCharges != 0)
            {
                return;
            }

            _initialSwapCharges = Mathf.Max(0, swapCharges);
            _initialBombHorizontalCharges = Mathf.Max(0, bombHorizontalCharges);
            _initialBombVerticalCharges = Mathf.Max(0, bombVerticalCharges);
            _initialBombAreaCharges = Mathf.Max(0, bombAreaCharges);
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

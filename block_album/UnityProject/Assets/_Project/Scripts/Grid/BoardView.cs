using System;
using System.Collections;
using System.Collections.Generic;
using BlockAlbum.Clear;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockAlbum.Grid
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class BoardView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private int boardSize = 9;
        [SerializeField] private float boardPadding = 12f;
        [SerializeField] private float cellSpacing = 4f;
        [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.18f);
        [SerializeField] private Color emptyZoneAltColor = new Color(1f, 1f, 1f, 0.11f);
        [SerializeField] private Color filledColor = new Color(1f, 0.86f, 0.20f, 0.95f);
        [SerializeField] private Color ghostValidColor = new Color(0.30f, 1f, 0.40f, 0.58f);
        [SerializeField] private Color ghostInvalidColor = new Color(1f, 0.35f, 0.35f, 0.58f);
        [Header("Day 4: Clear + Score")]
        [SerializeField] private int zoneSize = 3;
        [SerializeField] private int scorePerClearedCell = 1;
        [SerializeField] private int scorePerClearedBlocker = 5;
        [SerializeField] private int scorePerLine = 10;
        [SerializeField] private int scorePerZone = 15;
        [Header("Day 5: Combo + Power")]
        [SerializeField] private int comboScorePerLevel = 15;
        [SerializeField] private int powerMax = 100;
        [SerializeField] private int powerPerComboLevel = 20;
        [SerializeField] private int streakResetOnNoClearTurn = 3;
        [Header("Day 6: Blockers")]
        [SerializeField] private bool blockersEnabled = true;
        [SerializeField] private int blockerCount = 2;
        [SerializeField] private int blockerMoveEveryTurns = 2;
        [SerializeField] private int blockerRespawnDelayTurns = 10;
        [SerializeField] private Color blockerOnEmptyColor = new Color(0.18f, 0.20f, 0.25f, 0.96f);
        [SerializeField] private Color blockerOnFigureColor = new Color(0.90f, 0.22f, 0.40f, 0.98f);
        [Header("Day 7: Blocker Pressure Tuning")]
        [SerializeField] private int maxBlockersOnFigureCells = 1;
        [SerializeField] private int minBlockerChebyshevDistance = 0;
        [SerializeField, Range(0f, 1f)] private float debugRandomFill = 0f;

        private readonly List<Image> _cellImages = new List<Image>(81);

        private BoardModel _model;
        private RectTransform _gridRect;
        private GridLayoutGroup _gridLayout;
        private bool _ghostActive;
        private bool _ghostIsValid;
        private Vector2Int _ghostOrigin;
        private IReadOnlyList<Vector2Int> _ghostOffsets;
        private bool _initialized;
        private int _score;
        private int _playerTurnCount;
        private int _clearStreak;
        private int _turnsSinceLastClear;
        private int _powerCharge;
        private BoardTurnResult _lastTurnResult;
        private readonly List<BlockerState> _blockers = new List<BlockerState>(8);
        private readonly List<Vector2Int> _blockedCellsBuffer = new List<Vector2Int>(16);
        private static readonly Vector2Int[] AdjacentDirections8 =
        {
            new Vector2Int(-1, -1),
            new Vector2Int(0, -1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 0),
            new Vector2Int(1, 0),
            new Vector2Int(-1, 1),
            new Vector2Int(0, 1),
            new Vector2Int(1, 1),
        };

        public BoardModel Model => _model;
        public int Score => _score;
        public int PowerCharge => _powerCharge;
        public int PowerMax => powerMax;
        public int PlayerTurnCount => _playerTurnCount;
        public int TurnsUntilBlockerMove => GetTurnsUntilBlockerMove();
        public BoardTurnResult LastTurnResult => _lastTurnResult;
        public event Action<BoardTurnResult> TurnResolved;
        public event Action<Vector2Int> BoardCellClicked;
        public event Action RuntimeStateChanged;

        private IEnumerator Start()
        {
            yield return WaitForCanvasReady();
            Initialize();
            if (debugRandomFill > 0f)
            {
                FillRandom(debugRandomFill);
            }
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyCellSize();
        }

        [ContextMenu("Board/Rebuild")]
        public void Initialize()
        {
            boardSize = Mathf.Max(3, boardSize);
            _model = new BoardModel(boardSize);
            _score = 0;
            _playerTurnCount = 0;
            _clearStreak = 0;
            _turnsSinceLastClear = 0;
            _powerCharge = 0;
            _lastTurnResult = BoardTurnResult.Empty;
            _initialized = true;

            BuildGridVisual();
            SetupInitialBlockers();
            RefreshVisual();
            TurnResolved?.Invoke(_lastTurnResult);
            RuntimeStateChanged?.Invoke();
        }

        [ContextMenu("Board/Clear")]
        public void ClearBoard()
        {
            if (_model == null)
            {
                return;
            }

            _model.ClearAll();
            _score = 0;
            _playerTurnCount = 0;
            _clearStreak = 0;
            _turnsSinceLastClear = 0;
            _powerCharge = 0;
            _lastTurnResult = BoardTurnResult.Empty;
            SetupInitialBlockers();
            RefreshVisual();
            TurnResolved?.Invoke(_lastTurnResult);
            RuntimeStateChanged?.Invoke();
        }

        public bool TrySpendPower(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (_powerCharge < amount)
            {
                return false;
            }

            _powerCharge -= amount;
            RuntimeStateChanged?.Invoke();
            return true;
        }

        public bool TryForceOpenCellForSecondChance()
        {
            if (_model == null)
            {
                return false;
            }

            var candidates = new List<Vector2Int>(boardSize * boardSize);
            for (var y = 0; y < boardSize; y++)
            {
                for (var x = 0; x < boardSize; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (_model.IsOccupied(cell) || _model.IsBlocked(cell))
                    {
                        candidates.Add(cell);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            var target = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            if (_model.IsBlocked(target))
            {
                MarkDestroyedBlockers(new HashSet<Vector2Int> { target });
                _model.ClearBlocker(target);
            }

            _model.SetOccupied(target, false);
            RefreshVisual();
            RuntimeStateChanged?.Invoke();
            return true;
        }

        public void RefreshVisual()
        {
            if (_model == null || _cellImages.Count == 0)
            {
                return;
            }

            for (var y = 0; y < boardSize; y++)
            {
                for (var x = 0; x < boardSize; x++)
                {
                    var index = ToIndex(x, y);
                    var cell = new Vector2Int(x, y);
                    if (_model.IsBlocked(cell))
                    {
                        _cellImages[index].color = _model.IsBlockedOnOccupied(cell)
                            ? blockerOnFigureColor
                            : blockerOnEmptyColor;
                    }
                    else
                    {
                        _cellImages[index].color = _model.IsOccupied(cell)
                            ? filledColor
                            : GetEmptyCellColor(x, y);
                    }
                }
            }

            if (_ghostActive && _ghostOffsets != null)
            {
                ApplyGhost();
            }
        }

        public bool CanPlace(IReadOnlyList<Vector2Int> offsets, Vector2Int origin)
        {
            return _model != null && offsets != null && _model.CanPlace(offsets, origin);
        }

        public bool TryPlace(IReadOnlyList<Vector2Int> offsets, Vector2Int origin)
        {
            if (_model == null || offsets == null)
            {
                return false;
            }

            var placed = _model.Place(offsets, origin);
            if (!placed)
            {
                return false;
            }

            var clearResolution = ClearResolver.Resolve(_model, zoneSize);
            var clearOutcome = clearResolution.CellsToClear.Count > 0
                ? _model.ClearCellsWithBlockerRules(clearResolution.CellsToClear)
                : default;
            if (clearResolution.CellsToClear.Count > 0)
            {
                MarkDestroyedBlockers(clearResolution.CellsToClear);
            }

            ResolveAndPublishTurn(
                placedCells: offsets.Count,
                linesCleared: clearResolution.LinesCleared,
                zonesCleared: clearResolution.ZonesCleared,
                clearOutcome: clearOutcome,
                consumeTurn: true,
                grantPowerFromCombo: true,
                clearedCellScoreOverride: null);
            return true;
        }

        public bool TryApplyBombHorizontal(int row)
        {
            if (_model == null || row < 0 || row >= boardSize)
            {
                return false;
            }

            var cells = new HashSet<Vector2Int>();
            for (var x = 0; x < boardSize; x++)
            {
                cells.Add(new Vector2Int(x, row));
            }

            return TryApplyDirectClear(cells);
        }

        public bool TryApplyBombVertical(int column)
        {
            if (_model == null || column < 0 || column >= boardSize)
            {
                return false;
            }

            var cells = new HashSet<Vector2Int>();
            for (var y = 0; y < boardSize; y++)
            {
                cells.Add(new Vector2Int(column, y));
            }

            return TryApplyDirectClear(cells);
        }

        public bool TryApplyBombArea3x3(Vector2Int center)
        {
            if (_model == null)
            {
                return false;
            }

            var cells = new HashSet<Vector2Int>();
            for (var y = center.y - 1; y <= center.y + 1; y++)
            {
                for (var x = center.x - 1; x <= center.x + 1; x++)
                {
                    var cell = new Vector2Int(x, y);
                    if (_model.InBounds(cell))
                    {
                        cells.Add(cell);
                    }
                }
            }

            return TryApplyDirectClear(cells);
        }

        public void SetGhost(IReadOnlyList<Vector2Int> offsets, Vector2Int origin, bool canPlace)
        {
            _ghostActive = true;
            _ghostOffsets = offsets;
            _ghostOrigin = origin;
            _ghostIsValid = canPlace;
            RefreshVisual();
        }

        public void ClearGhost()
        {
            if (!_ghostActive)
            {
                return;
            }

            _ghostActive = false;
            _ghostOffsets = null;
            RefreshVisual();
        }

        public bool TryScreenToCell(Vector2 screenPosition, Camera eventCamera, out Vector2Int cell)
        {
            cell = default;
            if (_gridRect == null || _gridLayout == null)
            {
                return false;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_gridRect, screenPosition, eventCamera, out var localPoint))
            {
                return false;
            }

            var rect = _gridRect.rect;
            var px = localPoint.x + rect.width * 0.5f;
            var py = localPoint.y + rect.height * 0.5f;

            var contentWidth = boardSize * _gridLayout.cellSize.x + (boardSize - 1) * _gridLayout.spacing.x;
            var contentHeight = boardSize * _gridLayout.cellSize.y + (boardSize - 1) * _gridLayout.spacing.y;
            var left = (rect.width - contentWidth) * 0.5f;
            var bottom = (rect.height - contentHeight) * 0.5f;
            var right = left + contentWidth;
            var top = bottom + contentHeight;

            if (px < left || px > right || py < bottom || py > top)
            {
                return false;
            }

            var stepX = _gridLayout.cellSize.x + _gridLayout.spacing.x;
            var stepY = _gridLayout.cellSize.y + _gridLayout.spacing.y;

            var x = Mathf.FloorToInt((px - left) / stepX);
            var y = Mathf.FloorToInt((py - bottom) / stepY);
            x = Mathf.Clamp(x, 0, boardSize - 1);
            y = Mathf.Clamp(y, 0, boardSize - 1);

            cell = new Vector2Int(x, y);
            return true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (TryScreenToCell(eventData.position, eventData.pressEventCamera, out var cell))
            {
                BoardCellClicked?.Invoke(cell);
            }
        }

        private bool TryApplyDirectClear(ISet<Vector2Int> cellsToClear)
        {
            if (_model == null || cellsToClear == null || cellsToClear.Count == 0)
            {
                return false;
            }

            var clearOutcome = _model.ClearCellsWithBlockerRules(cellsToClear);
            if (clearOutcome.ClearedOccupiedCells == 0 && clearOutcome.ClearedBlockers == 0)
            {
                return false;
            }

            MarkDestroyedBlockers(cellsToClear);
            ResolveAndPublishTurn(
                placedCells: 0,
                linesCleared: 0,
                zonesCleared: 0,
                clearOutcome: clearOutcome,
                consumeTurn: true,
                grantPowerFromCombo: false,
                clearedCellScoreOverride: 1);
            return true;
        }

        private void ResolveAndPublishTurn(
            int placedCells,
            int linesCleared,
            int zonesCleared,
            BoardModel.ClearWithBlockersResult clearOutcome,
            bool consumeTurn,
            bool grantPowerFromCombo,
            int? clearedCellScoreOverride)
        {
            var clearedCells = clearOutcome.ClearedOccupiedCells;
            var clearedBlockers = clearOutcome.ClearedBlockers;
            var clearGroups = linesCleared + zonesCleared;
            var comboFromMulti = Mathf.Max(0, clearGroups - 1);
            var resetOnNoClearTurn = Mathf.Max(1, streakResetOnNoClearTurn);
            var hadClear = clearedCells > 0 || clearedBlockers > 0;

            if (hadClear)
            {
                var withinWindow = _clearStreak > 0 && _turnsSinceLastClear < resetOnNoClearTurn;
                _clearStreak = withinWindow ? _clearStreak + 1 : 1;
                _turnsSinceLastClear = 0;
            }
            else
            {
                _turnsSinceLastClear++;
                if (_turnsSinceLastClear >= resetOnNoClearTurn)
                {
                    _clearStreak = 0;
                }
            }

            var comboFromStreak = hadClear && _clearStreak >= 2 ? _clearStreak - 1 : 0;
            var comboLevel = comboFromMulti + comboFromStreak;
            var comboBonusScore = comboLevel * comboScorePerLevel;
            var hasLineOrZoneClear = linesCleared > 0 || zonesCleared > 0;
            var cellScore = Mathf.Max(0, clearedCellScoreOverride ?? scorePerClearedCell);
            var scoreFromCells = hasLineOrZoneClear ? 0 : clearedCells * cellScore;
            var scoreFromBlockers = clearedBlockers * scorePerClearedBlocker;
            var scoreFromLines = linesCleared * scorePerLine;
            var scoreFromZones = zonesCleared * scorePerZone;

            var baseScore =
                scoreFromCells +
                scoreFromBlockers +
                scoreFromLines +
                scoreFromZones;
            var scoreGained = baseScore + comboBonusScore;

            var powerGained = grantPowerFromCombo ? comboLevel * powerPerComboLevel : 0;
            _powerCharge = Mathf.Clamp(_powerCharge + powerGained, 0, powerMax);

            _score += scoreGained;
            _lastTurnResult = new BoardTurnResult(
                placementSucceeded: true,
                placedCells: placedCells,
                clearedCells: clearedCells,
                clearedBlockers: clearedBlockers,
                linesCleared: linesCleared,
                zonesCleared: zonesCleared,
                comboLevel: comboLevel,
                comboFromMulti: comboFromMulti,
                comboFromStreak: comboFromStreak,
                clearStreak: _clearStreak,
                scoreGained: scoreGained,
                scoreFromCells: scoreFromCells,
                scoreFromBlockers: scoreFromBlockers,
                scoreFromLines: scoreFromLines,
                scoreFromZones: scoreFromZones,
                comboBonusScore: comboBonusScore,
                totalScore: _score,
                powerGained: powerGained,
                powerCharge: _powerCharge,
                powerMax: powerMax);

            if (consumeTurn)
            {
                _playerTurnCount++;
                TryMoveBlockersForTurn();
                TickRespawnsAndRespawnBlockers();
            }

            RefreshVisual();
            TurnResolved?.Invoke(_lastTurnResult);
            RuntimeStateChanged?.Invoke();
        }

        private void FillRandom(float fillRate)
        {
            _model.ClearAll();
            for (var y = 0; y < boardSize; y++)
            {
                for (var x = 0; x < boardSize; x++)
                {
                    _model.SetOccupied(new Vector2Int(x, y), UnityEngine.Random.value < fillRate);
                }
            }

            SetupInitialBlockers();
            RefreshVisual();
        }

        private void BuildGridVisual()
        {
            if (_gridRect == null)
            {
                var existing = transform.Find("GridRoot");
                if (existing != null)
                {
                    _gridRect = existing as RectTransform;
                }
            }

            if (_gridRect == null)
            {
                var gridRoot = new GameObject("GridRoot", typeof(RectTransform), typeof(AspectRatioFitter), typeof(GridLayoutGroup));
                _gridRect = gridRoot.GetComponent<RectTransform>();
                _gridRect.SetParent(transform, false);
            }

            _gridRect.anchorMin = Vector2.zero;
            _gridRect.anchorMax = Vector2.one;
            _gridRect.offsetMin = new Vector2(boardPadding, boardPadding);
            _gridRect.offsetMax = new Vector2(-boardPadding, -boardPadding);

            var aspect = _gridRect.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 1f;

            _gridLayout = _gridRect.GetComponent<GridLayoutGroup>();
            _gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _gridLayout.constraintCount = boardSize;
            _gridLayout.spacing = new Vector2(cellSpacing, cellSpacing);
            _gridLayout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal;
            _gridLayout.childAlignment = TextAnchor.MiddleCenter;

            for (var i = _gridRect.childCount - 1; i >= 0; i--)
            {
                var child = _gridRect.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            _cellImages.Clear();
            var total = boardSize * boardSize;
            for (var i = 0; i < total; i++)
            {
                var cell = new GameObject($"Cell_{i:00}", typeof(RectTransform), typeof(Image));
                var rect = cell.GetComponent<RectTransform>();
                rect.SetParent(_gridRect, false);
                rect.localScale = Vector3.one;

                var image = cell.GetComponent<Image>();
                image.color = GetEmptyCellColor(i % boardSize, boardSize - 1 - (i / boardSize));
                image.raycastTarget = false;

                _cellImages.Add(image);
            }

            ApplyCellSize();
        }

        private void ApplyCellSize()
        {
            if (_gridRect == null || _gridLayout == null)
            {
                return;
            }
            if (!_initialized)
            {
                return;
            }

            var width = _gridRect.rect.width;
            var height = _gridRect.rect.height;
            var size = Mathf.Min(width, height);
            var totalSpacing = cellSpacing * (boardSize - 1);
            var cellSize = Mathf.Floor((size - totalSpacing) / boardSize);

            _gridLayout.cellSize = new Vector2(cellSize, cellSize);
        }

        private void ApplyGhost()
        {
            var ghostColor = _ghostIsValid ? ghostValidColor : ghostInvalidColor;
            for (var i = 0; i < _ghostOffsets.Count; i++)
            {
                var target = _ghostOrigin + _ghostOffsets[i];
                if (!Model.InBounds(target))
                {
                    continue;
                }

                var index = ToIndex(target.x, target.y);
                _cellImages[index].color = ghostColor;
            }
        }

        private int ToIndex(int x, int y)
        {
            return (boardSize - 1 - y) * boardSize + x;
        }

        private void SetupInitialBlockers()
        {
            if (_model == null)
            {
                return;
            }

            var existing = _model.GetBlockedCells();
            for (var i = 0; i < existing.Count; i++)
            {
                _model.ClearBlocker(existing[i]);
            }
            _blockers.Clear();

            if (!blockersEnabled)
            {
                return;
            }

            var count = Mathf.Clamp(blockerCount, 0, boardSize * boardSize);
            for (var i = 0; i < count; i++)
            {
                var blocker = new BlockerState();
                if (TryGetRandomBlockerCell(out var spawnCell, null))
                {
                    blocker.IsActive = true;
                    blocker.Position = spawnCell;
                    blocker.CooldownTurns = 0;
                    _model.PlaceBlocker(spawnCell);
                }

                _blockers.Add(blocker);
            }
        }

        private void TryMoveBlockersForTurn()
        {
            if (!blockersEnabled || blockerMoveEveryTurns <= 0 || _model == null)
            {
                return;
            }

            if (_playerTurnCount % blockerMoveEveryTurns != 0)
            {
                return;
            }

            for (var i = 0; i < _blockers.Count; i++)
            {
                var blocker = _blockers[i];
                if (!blocker.IsActive)
                {
                    continue;
                }

                var current = blocker.Position;
                _model.ClearBlocker(current);

                if (TryGetAdjacentMoveTarget(current, out var target))
                {
                    blocker.Position = target;
                    _model.PlaceBlocker(target);
                }
                else
                {
                    // No free adjacent cell: blocker stays in place.
                    _model.PlaceBlocker(current);
                }
            }
        }

        private bool TryGetAdjacentMoveTarget(Vector2Int origin, out Vector2Int target)
        {
            target = default;
            if (_model == null)
            {
                return false;
            }

            var occupiedCandidates = new List<Vector2Int>(8);
            var emptyCandidates = new List<Vector2Int>(8);
            for (var i = 0; i < AdjacentDirections8.Length; i++)
            {
                var candidate = origin + AdjacentDirections8[i];
                if (!CanUseBlockerTarget(candidate, null))
                {
                    continue;
                }

                if (_model.IsOccupied(candidate))
                {
                    occupiedCandidates.Add(candidate);
                }
                else
                {
                    emptyCandidates.Add(candidate);
                }
            }

            if (TryPickRandom(occupiedCandidates, out target))
            {
                return true;
            }

            return TryPickRandom(emptyCandidates, out target);
        }

        private void MarkDestroyedBlockers(ISet<Vector2Int> clearedCells)
        {
            if (!blockersEnabled || clearedCells == null)
            {
                return;
            }

            var respawnCooldown = Mathf.Max(0, blockerRespawnDelayTurns) + 1;
            for (var i = 0; i < _blockers.Count; i++)
            {
                var blocker = _blockers[i];
                if (!blocker.IsActive)
                {
                    continue;
                }

                if (!clearedCells.Contains(blocker.Position))
                {
                    continue;
                }

                blocker.IsActive = false;
                blocker.Position = new Vector2Int(-1, -1);
                blocker.CooldownTurns = respawnCooldown;
            }
        }

        private void TickRespawnsAndRespawnBlockers()
        {
            if (!blockersEnabled || _model == null)
            {
                return;
            }

            var reserved = new HashSet<Vector2Int>();
            for (var i = 0; i < _blockers.Count; i++)
            {
                if (_blockers[i].IsActive)
                {
                    reserved.Add(_blockers[i].Position);
                }
            }

            for (var i = 0; i < _blockers.Count; i++)
            {
                var blocker = _blockers[i];
                if (blocker.IsActive)
                {
                    continue;
                }

                if (blocker.CooldownTurns > 0)
                {
                    blocker.CooldownTurns--;
                }

                if (blocker.CooldownTurns > 0)
                {
                    continue;
                }

                if (!TryGetRandomBlockerCell(out var spawnCell, reserved))
                {
                    continue;
                }

                blocker.IsActive = true;
                blocker.Position = spawnCell;
                blocker.CooldownTurns = 0;
                _model.PlaceBlocker(spawnCell);
                reserved.Add(spawnCell);
            }
        }

        private bool TryGetRandomBlockerCell(out Vector2Int cell, ISet<Vector2Int> reserved)
        {
            cell = default;
            if (_model == null)
            {
                return false;
            }

            var maxAttempts = boardSize * boardSize * 2;
            for (var i = 0; i < maxAttempts; i++)
            {
                var x = UnityEngine.Random.Range(0, boardSize);
                var y = UnityEngine.Random.Range(0, boardSize);
                var candidate = new Vector2Int(x, y);
                if (!CanUseBlockerTarget(candidate, reserved))
                {
                    continue;
                }

                cell = candidate;
                return true;
            }

            for (var y = 0; y < boardSize; y++)
            {
                for (var x = 0; x < boardSize; x++)
                {
                    var candidate = new Vector2Int(x, y);
                    if (!CanUseBlockerTarget(candidate, reserved))
                    {
                        continue;
                    }

                    cell = candidate;
                    return true;
                }
            }

            return false;
        }

        private int GetTurnsUntilBlockerMove()
        {
            if (!blockersEnabled || blockerMoveEveryTurns <= 0)
            {
                return 0;
            }

            var mod = _playerTurnCount % blockerMoveEveryTurns;
            return mod == 0 ? blockerMoveEveryTurns : blockerMoveEveryTurns - mod;
        }

        private bool CanUseBlockerTarget(Vector2Int candidate, ISet<Vector2Int> reserved)
        {
            if (_model == null || !_model.InBounds(candidate))
            {
                return false;
            }

            if (_model.IsBlocked(candidate))
            {
                return false;
            }

            if (reserved != null && reserved.Contains(candidate))
            {
                return false;
            }

            var maxOnFigures = Mathf.Max(0, maxBlockersOnFigureCells);
            if (_model.IsOccupied(candidate) && _model.CountBlockedOnOccupied() >= maxOnFigures)
            {
                return false;
            }

            var minDistance = Mathf.Max(0, minBlockerChebyshevDistance);
            if (minDistance > 0)
            {
                var blockedCells = _model.GetBlockedCells(_blockedCellsBuffer);
                for (var i = 0; i < blockedCells.Count; i++)
                {
                    if (ChebyshevDistance(candidate, blockedCells[i]) <= minDistance)
                    {
                        return false;
                    }
                }

                if (reserved != null)
                {
                    foreach (var planned in reserved)
                    {
                        if (ChebyshevDistance(candidate, planned) <= minDistance)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private static int ChebyshevDistance(Vector2Int a, Vector2Int b)
        {
            return Mathf.Max(Mathf.Abs(a.x - b.x), Mathf.Abs(a.y - b.y));
        }

        private static bool TryPickRandom(IReadOnlyList<Vector2Int> list, out Vector2Int value)
        {
            value = default;
            if (list == null || list.Count == 0)
            {
                return false;
            }

            value = list[UnityEngine.Random.Range(0, list.Count)];
            return true;
        }

        private sealed class BlockerState
        {
            public bool IsActive;
            public Vector2Int Position;
            public int CooldownTurns;
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

        private Color GetEmptyCellColor(int x, int y)
        {
            var zoneX = Mathf.FloorToInt(x / Mathf.Max(1f, zoneSize));
            var zoneY = Mathf.FloorToInt(y / Mathf.Max(1f, zoneSize));
            var isAlt = ((zoneX + zoneY) & 1) == 1;
            return isAlt ? emptyZoneAltColor : emptyColor;
        }
    }
}

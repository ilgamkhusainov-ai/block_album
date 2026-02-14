using System.Collections;
using System.Collections.Generic;
using BlockAlbum.Grid;
using BlockAlbum.Pieces;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAlbum.UI
{
    [DisallowMultipleComponent]
    public sealed class AutoHintController : MonoBehaviour
    {
        [SerializeField] private float idleDelaySeconds = 15f;
        [SerializeField] private float hintVisibleSeconds = 2f;
        [SerializeField] private float hintRepeatDelaySeconds = 10f;
        [SerializeField] private float hintBlinkIntervalSeconds = 0.25f;
        [SerializeField] private string introOverlayName = "LevelShapePoolOverlay";
        [SerializeField] private string runEndOverlayName = "RunEndOverlay";

        private readonly List<PieceShapeDefinition> _trayShapes = new List<PieceShapeDefinition>(6);
        private BoardView _boardView;
        private PieceTrayView _pieceTrayView;
        private float _lastActivityTime;
        private float _nextHintAllowedTime;
        private float _hintEndTime;
        private float _nextBlinkTime;
        private bool _hintVisible;
        private bool _hintBlinkVisible;
        private PieceShapeDefinition _activeHintShape;
        private Vector2Int _activeHintOrigin;

        private IEnumerator Start()
        {
            yield return WaitForCanvasReady();
            BindRuntimeReferences();
            MarkActivity(clearHint: true);
        }

        private void Update()
        {
            if (_boardView == null || _pieceTrayView == null)
            {
                BindRuntimeReferences();
                return;
            }

            if (DetectPlayerInput())
            {
                MarkActivity(clearHint: true);
                return;
            }

            if (!CanShowHintNow())
            {
                if (_hintVisible)
                {
                    ClearHint();
                }

                return;
            }

            if (_hintVisible)
            {
                UpdateBlinkingHint();
                return;
            }

            var now = Time.unscaledTime;
            if (now < _nextHintAllowedTime)
            {
                return;
            }

            if (now - _lastActivityTime < Mathf.Max(0.5f, idleDelaySeconds))
            {
                return;
            }

            if (TryShowBestHint())
            {
                StartHintCycle();
            }
            else
            {
                _nextHintAllowedTime = now + 1f;
            }
        }

        private void OnDestroy()
        {
            if (_boardView != null)
            {
                _boardView.TurnResolved -= OnTurnResolved;
                _boardView.BoardCellClicked -= OnBoardCellClicked;
                _boardView.RuntimeStateChanged -= OnRuntimeStateChanged;
            }

            if (_pieceTrayView != null)
            {
                _pieceTrayView.TrayChanged -= OnTrayChanged;
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
                    _boardView.BoardCellClicked -= OnBoardCellClicked;
                    _boardView.BoardCellClicked += OnBoardCellClicked;
                    _boardView.RuntimeStateChanged -= OnRuntimeStateChanged;
                    _boardView.RuntimeStateChanged += OnRuntimeStateChanged;
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
        }

        private void OnTurnResolved(BoardTurnResult result)
        {
            MarkActivity(clearHint: true);
        }

        private void OnBoardCellClicked(Vector2Int cell)
        {
            MarkActivity(clearHint: true);
        }

        private void OnRuntimeStateChanged()
        {
            MarkActivity(clearHint: true);
        }

        private void OnTrayChanged()
        {
            MarkActivity(clearHint: true);
        }

        private void MarkActivity(bool clearHint)
        {
            _lastActivityTime = Time.unscaledTime;
            _nextHintAllowedTime = _lastActivityTime + Mathf.Max(0.5f, idleDelaySeconds);
            if (clearHint)
            {
                ClearHint();
            }
        }

        private void ClearHint()
        {
            if (_boardView != null && _hintVisible)
            {
                _boardView.ClearGhost();
            }

            _hintVisible = false;
            _hintBlinkVisible = false;
            _activeHintShape = null;
        }

        private void StartHintCycle()
        {
            _hintVisible = true;
            _hintBlinkVisible = true;
            _hintEndTime = Time.unscaledTime + Mathf.Max(0.25f, hintVisibleSeconds);
            _nextBlinkTime = Time.unscaledTime + Mathf.Max(0.08f, hintBlinkIntervalSeconds);
            ApplyHintVisibility(true);
        }

        private void UpdateBlinkingHint()
        {
            var now = Time.unscaledTime;
            if (now >= _hintEndTime)
            {
                ClearHint();
                _nextHintAllowedTime = now + Mathf.Max(0.5f, hintRepeatDelaySeconds);
                return;
            }

            if (now >= _nextBlinkTime)
            {
                _hintBlinkVisible = !_hintBlinkVisible;
                ApplyHintVisibility(_hintBlinkVisible);
                _nextBlinkTime = now + Mathf.Max(0.08f, hintBlinkIntervalSeconds);
            }
        }

        private void ApplyHintVisibility(bool visible)
        {
            if (_boardView == null || _activeHintShape == null)
            {
                return;
            }

            if (visible)
            {
                _boardView.SetGhost(_activeHintShape.Cells, _activeHintOrigin, canPlace: true);
            }
            else
            {
                _boardView.ClearGhost();
            }
        }

        private bool TryShowBestHint()
        {
            if (_boardView == null || _pieceTrayView == null || _boardView.Model == null)
            {
                return false;
            }

            if (_pieceTrayView.GetCurrentShapes(_trayShapes) == 0)
            {
                return false;
            }

            var size = _boardView.Model.Size;
            var hasMassCenter = TryGetMassCenter(out var massCenter);
            var hasCandidate = false;
            var best = default(HintCandidate);

            for (var i = 0; i < _trayShapes.Count; i++)
            {
                var shape = _trayShapes[i];
                if (shape == null || shape.Cells == null || shape.Cells.Length == 0)
                {
                    continue;
                }

                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        var origin = new Vector2Int(x, y);
                        if (!_boardView.CanPlace(shape.Cells, origin))
                        {
                            continue;
                        }

                        var candidate = EvaluateCandidate(shape, origin, hasMassCenter, massCenter);
                        if (!hasCandidate || CompareCandidates(candidate, best) > 0)
                        {
                            hasCandidate = true;
                            best = candidate;
                        }
                    }
                }
            }

            if (!hasCandidate || best.Shape == null)
            {
                return false;
            }

            _activeHintShape = best.Shape;
            _activeHintOrigin = best.Origin;
            return true;
        }

        private HintCandidate EvaluateCandidate(PieceShapeDefinition shape, Vector2Int origin, bool hasMassCenter, Vector2 massCenter)
        {
            var model = _boardView.Model;
            var size = model.Size;
            var zone = _boardView.ZoneSize;

            var filled = new bool[size, size];
            var rowBefore = new int[size];
            var colBefore = new int[size];
            var zoneSide = Mathf.Max(1, zone);
            var zoneCountX = Mathf.CeilToInt(size / (float)zoneSide);
            var zoneCountY = Mathf.CeilToInt(size / (float)zoneSide);
            var zoneBefore = new int[zoneCountX * zoneCountY];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var isFilled = model.IsFilledForClear(new Vector2Int(x, y));
                    filled[x, y] = isFilled;
                    if (!isFilled)
                    {
                        continue;
                    }

                    rowBefore[y]++;
                    colBefore[x]++;
                    var zoneIndex = GetZoneIndex(x, y, zoneSide, zoneCountX);
                    zoneBefore[zoneIndex]++;
                }
            }

            var placedCells = new HashSet<Vector2Int>();
            for (var i = 0; i < shape.Cells.Length; i++)
            {
                var cell = origin + shape.Cells[i];
                if (cell.x < 0 || cell.x >= size || cell.y < 0 || cell.y >= size)
                {
                    continue;
                }

                filled[cell.x, cell.y] = true;
                placedCells.Add(cell);
            }

            var clearCells = new HashSet<Vector2Int>();
            var linesCleared = 0;
            for (var y = 0; y < size; y++)
            {
                var full = true;
                for (var x = 0; x < size; x++)
                {
                    if (!filled[x, y])
                    {
                        full = false;
                        break;
                    }
                }

                if (!full)
                {
                    continue;
                }

                linesCleared++;
                for (var x = 0; x < size; x++)
                {
                    clearCells.Add(new Vector2Int(x, y));
                }
            }

            for (var x = 0; x < size; x++)
            {
                var full = true;
                for (var y = 0; y < size; y++)
                {
                    if (!filled[x, y])
                    {
                        full = false;
                        break;
                    }
                }

                if (!full)
                {
                    continue;
                }

                linesCleared++;
                for (var y = 0; y < size; y++)
                {
                    clearCells.Add(new Vector2Int(x, y));
                }
            }

            var zonesCleared = 0;
            for (var startY = 0; startY < size; startY += zone)
            {
                for (var startX = 0; startX < size; startX += zone)
                {
                    var maxY = Mathf.Min(size, startY + zone);
                    var maxX = Mathf.Min(size, startX + zone);
                    var full = true;
                    for (var y = startY; y < maxY && full; y++)
                    {
                        for (var x = startX; x < maxX; x++)
                        {
                            if (!filled[x, y])
                            {
                                full = false;
                                break;
                            }
                        }
                    }

                    if (!full)
                    {
                        continue;
                    }

                    zonesCleared++;
                    for (var y = startY; y < maxY; y++)
                    {
                        for (var x = startX; x < maxX; x++)
                        {
                            clearCells.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }

            var clearGroups = linesCleared + zonesCleared;
            var hadClear = clearCells.Count > 0;
            var placedCleared = 0;
            foreach (var placed in placedCells)
            {
                if (clearCells.Contains(placed))
                {
                    placedCleared++;
                }
            }

            var leftovers = shape.Cells.Length - placedCleared;
            var clearedCells = 0;
            var clearedBlockers = 0;
            foreach (var cell in clearCells)
            {
                if (model.IsBlocked(cell))
                {
                    clearedBlockers++;
                }

                if (!model.IsBlockedOnOccupied(cell))
                {
                    clearedCells++;
                }
            }

            var comboFromMulti = Mathf.Max(0, clearGroups - 1);
            var comboFromStreak = EstimateComboFromStreak(hadClear);
            var comboLevel = comboFromMulti + comboFromStreak;
            var hasLineOrZoneClear = clearGroups > 0;

            var scoreFromCells = hasLineOrZoneClear ? 0 : clearedCells * _boardView.ScorePerClearedCell;
            var scoreFromBlockers = clearedBlockers * _boardView.ScorePerClearedBlocker;
            var scoreFromLines = linesCleared * _boardView.ScorePerLine;
            var scoreFromZones = zonesCleared * _boardView.ScorePerZone;
            var comboBonus = comboLevel * _boardView.ComboScorePerLevel;
            var estimatedScore = scoreFromCells + scoreFromBlockers + scoreFromLines + scoreFromZones + comboBonus;

            var rowAfter = new int[size];
            var colAfter = new int[size];
            var zoneAfter = new int[zoneBefore.Length];
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    if (!filled[x, y])
                    {
                        continue;
                    }

                    rowAfter[y]++;
                    colAfter[x]++;
                    var zoneIndex = GetZoneIndex(x, y, zoneSide, zoneCountX);
                    zoneAfter[zoneIndex]++;
                }
            }

            var proximityBefore = EvaluateProximityScore(rowBefore, size) +
                                  EvaluateProximityScore(colBefore, size) +
                                  EvaluateProximityScore(zoneBefore, zoneSide * zoneSide);
            var proximityAfter = EvaluateProximityScore(rowAfter, size) +
                                 EvaluateProximityScore(colAfter, size) +
                                 EvaluateProximityScore(zoneAfter, zoneSide * zoneSide);
            var proximityGain = proximityAfter - proximityBefore;

            var center = (size - 1) * 0.5f;
            var centerPenalty = 0;
            var massDistancePenalty = 0f;
            for (var i = 0; i < shape.Cells.Length; i++)
            {
                var cell = origin + shape.Cells[i];
                centerPenalty += Mathf.RoundToInt(Mathf.Abs(cell.x - center) + Mathf.Abs(cell.y - center));
                if (hasMassCenter)
                {
                    massDistancePenalty += Mathf.Abs(cell.x - massCenter.x) + Mathf.Abs(cell.y - massCenter.y);
                }
            }

            if (shape.Cells.Length > 0 && hasMassCenter)
            {
                massDistancePenalty /= shape.Cells.Length;
            }

            return new HintCandidate
            {
                Shape = shape,
                Origin = origin,
                LinesCleared = linesCleared,
                ZonesCleared = zonesCleared,
                LeftoverCells = Mathf.Max(0, leftovers),
                EstimatedScore = estimatedScore,
                ComboLevel = comboLevel,
                ProximityGain = proximityGain,
                ProximityAfter = proximityAfter,
                MassDistancePenalty = massDistancePenalty,
                CenterPenalty = centerPenalty,
            };
        }

        private int EstimateComboFromStreak(bool hadClear)
        {
            if (!hadClear)
            {
                return 0;
            }

            var currentStreak = _boardView.CurrentClearStreak;
            var turnsSinceLastClear = _boardView.TurnsSinceLastClear;
            var resetTurn = _boardView.StreakResetOnNoClearTurn;
            var withinWindow = currentStreak > 0 && turnsSinceLastClear < resetTurn;
            var nextStreak = withinWindow ? currentStreak + 1 : 1;
            return nextStreak >= 2 ? nextStreak - 1 : 0;
        }

        private static int CompareCandidates(HintCandidate a, HintCandidate b)
        {
            var aHasLineClear = a.LinesCleared > 0;
            var bHasLineClear = b.LinesCleared > 0;
            if (aHasLineClear != bHasLineClear)
            {
                return aHasLineClear ? 1 : -1;
            }

            if (a.LinesCleared != b.LinesCleared)
            {
                return a.LinesCleared > b.LinesCleared ? 1 : -1;
            }

            // If no line clear is available, prefer moves that bring lines/zones closer to completion.
            if (!aHasLineClear && !bHasLineClear)
            {
                if (a.ProximityGain != b.ProximityGain)
                {
                    return a.ProximityGain > b.ProximityGain ? 1 : -1;
                }

                if (a.ProximityAfter != b.ProximityAfter)
                {
                    return a.ProximityAfter > b.ProximityAfter ? 1 : -1;
                }
            }

            if (a.LeftoverCells != b.LeftoverCells)
            {
                return a.LeftoverCells < b.LeftoverCells ? 1 : -1;
            }

            if (a.EstimatedScore != b.EstimatedScore)
            {
                return a.EstimatedScore > b.EstimatedScore ? 1 : -1;
            }

            if (a.ComboLevel != b.ComboLevel)
            {
                return a.ComboLevel > b.ComboLevel ? 1 : -1;
            }

            if (!Mathf.Approximately(a.MassDistancePenalty, b.MassDistancePenalty))
            {
                return a.MassDistancePenalty < b.MassDistancePenalty ? 1 : -1;
            }

            if (a.ZonesCleared != b.ZonesCleared)
            {
                return a.ZonesCleared > b.ZonesCleared ? 1 : -1;
            }

            if (a.CenterPenalty != b.CenterPenalty)
            {
                return a.CenterPenalty < b.CenterPenalty ? 1 : -1;
            }

            return 0;
        }

        private static int EvaluateProximityScore(IReadOnlyList<int> filledCounts, int fullSize)
        {
            var score = 0;
            for (var i = 0; i < filledCounts.Count; i++)
            {
                score += GetCompletionWeight(filledCounts[i], fullSize);
            }

            return score;
        }

        private static int GetCompletionWeight(int filled, int fullSize)
        {
            var size = Mathf.Max(1, fullSize);
            var clamped = Mathf.Clamp(filled, 0, size);
            var empties = size - clamped;
            if (empties <= 0)
            {
                return 1000;
            }

            if (empties == 1)
            {
                return 300;
            }

            if (empties == 2)
            {
                return 120;
            }

            if (empties == 3)
            {
                return 45;
            }

            return clamped;
        }

        private static int GetZoneIndex(int x, int y, int zoneSide, int zoneCountX)
        {
            var zx = x / Mathf.Max(1, zoneSide);
            var zy = y / Mathf.Max(1, zoneSide);
            return zy * zoneCountX + zx;
        }

        private bool TryGetMassCenter(out Vector2 massCenter)
        {
            massCenter = Vector2.zero;
            if (_boardView == null || _boardView.Model == null)
            {
                return false;
            }

            var model = _boardView.Model;
            var size = model.Size;
            var sumX = 0f;
            var sumY = 0f;
            var count = 0;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    if (!model.IsOccupied(new Vector2Int(x, y)))
                    {
                        continue;
                    }

                    sumX += x;
                    sumY += y;
                    count++;
                }
            }

            if (count <= 0)
            {
                return false;
            }

            massCenter = new Vector2(sumX / count, sumY / count);
            return true;
        }

        private bool CanShowHintNow()
        {
            if (_boardView == null || _pieceTrayView == null || _boardView.Model == null)
            {
                return false;
            }

            // No hints before the first completed turn.
            if (_boardView.PlayerTurnCount <= 0)
            {
                return false;
            }

            if (IsOverlayActive(introOverlayName) || IsOverlayActive(runEndOverlayName))
            {
                return false;
            }

            return _pieceTrayView.HasAnyValidMove();
        }

        private bool IsOverlayActive(string nodeName)
        {
            if (string.IsNullOrEmpty(nodeName))
            {
                return false;
            }

            var node = transform.Find(nodeName);
            return node != null && node.gameObject.activeInHierarchy;
        }

        private static bool DetectPlayerInput()
        {
            if (Input.touchCount > 0)
            {
                return true;
            }

            if (Input.GetMouseButton(0) || Input.GetMouseButtonDown(0))
            {
                return true;
            }

            return Input.anyKeyDown;
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

        private struct HintCandidate
        {
            public PieceShapeDefinition Shape;
            public Vector2Int Origin;
            public int LinesCleared;
            public int ZonesCleared;
            public int LeftoverCells;
            public int EstimatedScore;
            public int ComboLevel;
            public int ProximityGain;
            public int ProximityAfter;
            public float MassDistancePenalty;
            public int CenterPenalty;
        }
    }
}

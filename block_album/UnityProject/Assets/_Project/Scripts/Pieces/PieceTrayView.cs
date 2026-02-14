using System;
using System.Collections;
using System.Collections.Generic;
using BlockAlbum.Grid;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAlbum.Pieces
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class PieceTrayView : MonoBehaviour
    {
        [SerializeField] private int slotCount = 3;
        [SerializeField] private int previewGridSize = 5;
        [SerializeField] private float slotSpacing = 16f;
        [SerializeField] private float slotPadding = 10f;
        [SerializeField] private float miniCellSpacing = 2f;
        [SerializeField] private Color slotBackgroundColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private Color previewEmptyColor = new Color(1f, 1f, 1f, 0f);
        [SerializeField] private Color[] pieceColors =
        {
            new Color(1f, 0.39f, 0.20f, 0.95f),
            new Color(1f, 0.78f, 0.13f, 0.95f),
            new Color(0.32f, 0.86f, 1f, 0.95f),
            new Color(0.63f, 0.92f, 0.36f, 0.95f),
        };

        private readonly List<SlotVisual> _slots = new List<SlotVisual>(3);
        private IReadOnlyList<PieceShapeDefinition> _shapes;
        private BoardView _boardView;
        private bool _initialized;
        public event Action TrayChanged;

        private void Awake()
        {
            _shapes = PieceShapeLibrary.GetDefaults();
            _boardView = FindFirstObjectByType<BoardView>();
        }

        private IEnumerator Start()
        {
            yield return WaitForCanvasReady();
            InitializeIfNeeded();
        }

        [ContextMenu("Tray/Refill")]
        public void RefillTray()
        {
            InitializeIfNeeded();

            if (_slots.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _slots.Count; i++)
            {
                AssignRandomShape(_slots[i].Slot);
            }

            TrayChanged?.Invoke();
        }

        public bool RefillTrayUntilAnyValidMove(int maxAttempts = 80)
        {
            InitializeIfNeeded();
            if (_slots.Count == 0)
            {
                return false;
            }

            if (_boardView == null || _boardView.Model == null)
            {
                RefillTray();
                return true;
            }

            var attempts = Mathf.Max(1, maxAttempts);
            for (var attempt = 0; attempt < attempts; attempt++)
            {
                for (var i = 0; i < _slots.Count; i++)
                {
                    AssignRandomShape(_slots[i].Slot);
                }

                if (HasAnyValidMove())
                {
                    TrayChanged?.Invoke();
                    return true;
                }
            }

            TrayChanged?.Invoke();
            return false;
        }

        public void OnSlotConsumed(PieceTraySlot slot)
        {
            AssignRandomShape(slot);
            TrayChanged?.Invoke();
        }

        public bool HasAnyValidMove()
        {
            InitializeIfNeeded();
            if (_boardView == null || _boardView.Model == null)
            {
                return true;
            }

            var size = _boardView.Model.Size;
            for (var i = 0; i < _slots.Count; i++)
            {
                var shape = _slots[i].Slot.CurrentShape;
                if (shape == null || shape.Cells == null || shape.Cells.Length == 0)
                {
                    continue;
                }

                for (var y = 0; y < size; y++)
                {
                    for (var x = 0; x < size; x++)
                    {
                        if (_boardView.CanPlace(shape.Cells, new Vector2Int(x, y)))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void InitializeIfNeeded()
        {
            if (_initialized)
            {
                return;
            }

            if (_boardView == null)
            {
                _boardView = FindFirstObjectByType<BoardView>();
            }

            BuildSlots();
            RefillTrayInternal();
            _initialized = true;
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

            // One extra frame to avoid race with first render cycle.
            yield return null;
        }

        private void RefillTrayInternal()
        {
            for (var i = 0; i < _slots.Count; i++)
            {
                AssignRandomShape(_slots[i].Slot);
            }

            TrayChanged?.Invoke();
        }

        private void BuildSlots()
        {
            var layout = GetComponent<HorizontalLayoutGroup>();
            if (layout == null)
            {
                layout = gameObject.AddComponent<HorizontalLayoutGroup>();
            }

            layout.spacing = slotSpacing;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.padding = new RectOffset(16, 16, 8, 8);

            for (var i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (Application.isPlaying)
                {
                    Destroy(child.gameObject);
                }
                else
                {
                    DestroyImmediate(child.gameObject);
                }
            }

            _slots.Clear();
            for (var i = 0; i < slotCount; i++)
            {
                _slots.Add(CreateSlotVisual(i));
            }
        }

        private SlotVisual CreateSlotVisual(int index)
        {
            var slot = new GameObject($"PieceSlot_{index + 1}", typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(PieceTraySlot));
            slot.transform.SetParent(transform, false);

            var slotRect = slot.GetComponent<RectTransform>();
            slotRect.localScale = Vector3.one;
            slotRect.anchorMin = new Vector2(0.5f, 0.5f);
            slotRect.anchorMax = new Vector2(0.5f, 0.5f);

            var slotImage = slot.GetComponent<Image>();
            slotImage.color = new Color(slotBackgroundColor.r, slotBackgroundColor.g, slotBackgroundColor.b, 0f);
            slotImage.raycastTarget = true;

            var layout = slot.GetComponent<LayoutElement>();
            layout.minWidth = 180f;
            layout.minHeight = 180f;
            layout.preferredWidth = 220f;
            layout.preferredHeight = 220f;

            var preview = new GameObject("Preview", typeof(RectTransform), typeof(AspectRatioFitter), typeof(GridLayoutGroup));
            preview.transform.SetParent(slot.transform, false);
            var previewRect = preview.GetComponent<RectTransform>();
            previewRect.anchorMin = Vector2.zero;
            previewRect.anchorMax = Vector2.one;
            previewRect.offsetMin = new Vector2(slotPadding, slotPadding);
            previewRect.offsetMax = new Vector2(-slotPadding, -slotPadding);

            var aspect = preview.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 1f;

            var grid = preview.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = previewGridSize;
            grid.spacing = new Vector2(miniCellSpacing, miniCellSpacing);
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            grid.childAlignment = TextAnchor.MiddleCenter;

            var slotVisual = new SlotVisual
            {
                PreviewRect = previewRect,
                PreviewGrid = grid,
                Cells = new List<Image>(previewGridSize * previewGridSize),
                Slot = slot.GetComponent<PieceTraySlot>(),
            };

            var total = previewGridSize * previewGridSize;
            for (var i = 0; i < total; i++)
            {
                var cell = new GameObject($"MiniCell_{i:00}", typeof(RectTransform), typeof(Image));
                cell.transform.SetParent(preview.transform, false);

                var image = cell.GetComponent<Image>();
                image.color = previewEmptyColor;
                image.raycastTarget = false;
                slotVisual.Cells.Add(image);
            }

            ApplyMiniCellSize(slotVisual);
            slotVisual.Slot.Setup(
                this,
                index,
                _boardView,
                slotVisual.Cells,
                previewGridSize,
                miniCellSpacing,
                previewEmptyColor);
            return slotVisual;
        }

        private void AssignRandomShape(PieceTraySlot slot)
        {
            if (slot == null || _shapes == null || _shapes.Count == 0)
            {
                return;
            }

            var shape = _shapes[UnityEngine.Random.Range(0, _shapes.Count)];
            var color = pieceColors[slot.SlotIndex % pieceColors.Length];
            slot.Assign(shape, color);
        }

        private void ApplyMiniCellSize(SlotVisual slot)
        {
            var size = Mathf.Min(slot.PreviewRect.rect.width, slot.PreviewRect.rect.height);
            var spacing = miniCellSpacing * (previewGridSize - 1);
            var cellSize = Mathf.Floor((size - spacing) / previewGridSize);
            slot.PreviewGrid.cellSize = new Vector2(cellSize, cellSize);
        }

        private void OnRectTransformDimensionsChange()
        {
            if (!_initialized)
            {
                return;
            }

            for (var i = 0; i < _slots.Count; i++)
            {
                ApplyMiniCellSize(_slots[i]);
            }
        }

        private sealed class SlotVisual
        {
            public RectTransform PreviewRect;
            public GridLayoutGroup PreviewGrid;
            public List<Image> Cells;
            public PieceTraySlot Slot;
        }
    }
}

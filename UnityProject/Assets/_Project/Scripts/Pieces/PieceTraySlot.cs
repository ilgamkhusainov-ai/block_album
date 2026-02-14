using System.Collections.Generic;
using BlockAlbum.Grid;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BlockAlbum.Pieces
{
    public sealed class PieceTraySlot : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        private PieceTrayView _owner;
        private BoardView _boardView;
        private PieceShapeDefinition _shape;
        private Color _color;
        private int _previewGridSize;
        private float _miniCellSpacing;
        private Color _emptyColor;
        private List<Image> _previewCells;
        private Canvas _rootCanvas;
        private RectTransform _canvasRect;

        private RectTransform _dragVisualRect;
        private GridLayoutGroup _dragGrid;
        private List<Image> _dragCells;
        private Vector2Int _lastOrigin;
        private Vector2Int _pivotOffset;
        private bool _hasDropCandidate;
        private bool _canDrop;

        public int SlotIndex { get; private set; }
        public PieceShapeDefinition CurrentShape => _shape;

        public void Setup(
            PieceTrayView owner,
            int slotIndex,
            BoardView boardView,
            List<Image> previewCells,
            int previewGridSize,
            float miniCellSpacing,
            Color emptyColor)
        {
            _owner = owner;
            SlotIndex = slotIndex;
            _boardView = boardView;
            _previewCells = previewCells;
            _previewGridSize = previewGridSize;
            _miniCellSpacing = miniCellSpacing;
            _emptyColor = emptyColor;

            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            _canvasRect = _rootCanvas != null ? _rootCanvas.GetComponent<RectTransform>() : null;
        }

        public void Assign(PieceShapeDefinition shape, Color color)
        {
            _shape = shape;
            _color = color;
            PiecePreviewRenderer.Render(_previewCells, _previewGridSize, _emptyColor, _shape, _color);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_shape == null)
            {
                return;
            }

            if (_boardView == null)
            {
                _boardView = FindFirstObjectByType<BoardView>();
            }

            if (_boardView == null || _owner == null)
            {
                return;
            }

            CreateDragVisual();
            if (_dragVisualRect == null)
            {
                return;
            }

            HideSlotPreview();
            _pivotOffset = PiecePreviewRenderer.CalculatePlacementPivot(_shape.Cells);
            UpdateDragPosition(eventData);
            UpdateGhost(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_shape == null || _dragVisualRect == null)
            {
                return;
            }

            UpdateDragPosition(eventData);
            UpdateGhost(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_shape == null)
            {
                return;
            }

            var placed = false;
            if (_boardView != null && _hasDropCandidate && _canDrop)
            {
                placed = _boardView.TryPlace(_shape.Cells, _lastOrigin);
                if (placed)
                {
                    _owner.OnSlotConsumed(this);
                }
            }

            if (!placed)
            {
                ShowSlotPreview();
            }

            if (_boardView != null)
            {
                _boardView.ClearGhost();
            }
            DestroyDragVisual();
            _hasDropCandidate = false;
            _canDrop = false;
        }

        private void UpdateGhost(PointerEventData eventData)
        {
            if (_boardView.TryScreenToCell(eventData.position, eventData.pressEventCamera, out var pointerCell))
            {
                _hasDropCandidate = true;
                _lastOrigin = pointerCell - _pivotOffset;
                _canDrop = _boardView.CanPlace(_shape.Cells, _lastOrigin);
                _boardView.SetGhost(_shape.Cells, _lastOrigin, _canDrop);
                return;
            }

            _hasDropCandidate = false;
            _canDrop = false;
            _boardView.ClearGhost();
        }

        private void UpdateDragPosition(PointerEventData eventData)
        {
            if (_canvasRect == null || _dragVisualRect == null)
            {
                return;
            }

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvasRect, eventData.position, eventData.pressEventCamera, out var localPos))
            {
                _dragVisualRect.anchoredPosition = localPos;
            }
        }

        private void CreateDragVisual()
        {
            if (_rootCanvas == null)
            {
                return;
            }

            DestroyDragVisual();

            var dragRoot = new GameObject($"DragVisual_{SlotIndex + 1}", typeof(RectTransform), typeof(CanvasGroup));
            _dragVisualRect = dragRoot.GetComponent<RectTransform>();
            _dragVisualRect.SetParent(_rootCanvas.transform, false);
            _dragVisualRect.SetAsLastSibling();
            _dragVisualRect.sizeDelta = ((RectTransform)transform).rect.size;

            var canvasGroup = dragRoot.GetComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0.9f;

            var preview = new GameObject("Preview", typeof(RectTransform), typeof(AspectRatioFitter), typeof(GridLayoutGroup));
            preview.transform.SetParent(_dragVisualRect, false);
            var previewRect = preview.GetComponent<RectTransform>();
            previewRect.anchorMin = Vector2.zero;
            previewRect.anchorMax = Vector2.one;
            previewRect.offsetMin = Vector2.zero;
            previewRect.offsetMax = Vector2.zero;

            var aspect = preview.GetComponent<AspectRatioFitter>();
            aspect.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            aspect.aspectRatio = 1f;

            _dragGrid = preview.GetComponent<GridLayoutGroup>();
            _dragGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _dragGrid.constraintCount = _previewGridSize;
            _dragGrid.spacing = new Vector2(_miniCellSpacing, _miniCellSpacing);
            _dragGrid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            _dragGrid.startAxis = GridLayoutGroup.Axis.Horizontal;
            _dragGrid.childAlignment = TextAnchor.MiddleCenter;

            _dragCells = new List<Image>(_previewGridSize * _previewGridSize);
            var total = _previewGridSize * _previewGridSize;
            for (var i = 0; i < total; i++)
            {
                var cell = new GameObject($"DragCell_{i:00}", typeof(RectTransform), typeof(Image));
                cell.transform.SetParent(preview.transform, false);
                var image = cell.GetComponent<Image>();
                image.color = _emptyColor;
                image.raycastTarget = false;
                _dragCells.Add(image);
            }

            ApplyDragCellSize(previewRect);
            PiecePreviewRenderer.Render(_dragCells, _previewGridSize, _emptyColor, _shape, _color);
        }

        private void DestroyDragVisual()
        {
            if (_dragVisualRect == null)
            {
                return;
            }

            Destroy(_dragVisualRect.gameObject);
            _dragVisualRect = null;
            _dragGrid = null;
            _dragCells = null;
        }

        private void ApplyDragCellSize(RectTransform previewRect)
        {
            if (_dragGrid == null)
            {
                return;
            }

            var size = Mathf.Min(previewRect.rect.width, previewRect.rect.height);
            var spacing = _miniCellSpacing * (_previewGridSize - 1);
            var cellSize = Mathf.Floor((size - spacing) / _previewGridSize);
            _dragGrid.cellSize = new Vector2(cellSize, cellSize);
        }

        private void HideSlotPreview()
        {
            PiecePreviewRenderer.Render(_previewCells, _previewGridSize, _emptyColor, null, _color);
        }

        private void ShowSlotPreview()
        {
            PiecePreviewRenderer.Render(_previewCells, _previewGridSize, _emptyColor, _shape, _color);
        }
    }
}

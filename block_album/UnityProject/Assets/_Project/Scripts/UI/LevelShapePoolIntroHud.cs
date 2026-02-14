using System.Collections;
using System.Collections.Generic;
using BlockAlbum.Goals;
using BlockAlbum.Pieces;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAlbum.UI
{
    [DisallowMultipleComponent]
    public sealed class LevelShapePoolIntroHud : MonoBehaviour
    {
        [SerializeField] private float showDurationSeconds = 5f;
        [SerializeField] private int previewGridSize = 5;
        [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.72f);
        [SerializeField] private Color panelColor = new Color(0.12f, 0.14f, 0.22f, 0.96f);
        [SerializeField] private Color cardColor = new Color(1f, 1f, 1f, 0.10f);
        [SerializeField] private Color cardHighlightColor = new Color(1f, 0.77f, 0.16f, 0.44f);
        [SerializeField] private Color textPanelColor = new Color(1f, 1f, 1f, 0.12f);
        [SerializeField] private Color textPanelHighlightColor = new Color(1f, 0.77f, 0.16f, 0.32f);
        [SerializeField] private Color miniCellColor = new Color(1f, 0.67f, 0.20f, 0.95f);
        [SerializeField] private Color miniCellHighlightColor = new Color(0.35f, 0.95f, 0.95f, 1f);
        [SerializeField] [Range(0.6f, 1f)] private float miniCellFillRatio = 0.86f;
        [Header("Layout (Fixed iPhone 15 Pro Max)")]
        [SerializeField] private int cardColumns = 2;
        [SerializeField] private float targetScreenWidthPx = 1290f;
        [SerializeField] private float fixedColumnsWidthPx = 1032f; // 80% of 1290
        [SerializeField] private float fixedCardsSpacingPx = 24f;
        [SerializeField] private float fixedFigureCardSizePx = 504f; // (1032 - 24) / 2
        [SerializeField] private float fixedTextPanelHeightPx = 126f; // 25% of 504

        private LevelProgressionController _levelProgressionController;
        private RectTransform _overlayRoot;
        private RectTransform _cardsRoot;
        private GridLayoutGroup _cardsGrid;
        private Text _titleText;
        private Text _detailsText;
        private Coroutine _hideRoutine;
        private int _lastShownLevel = -1;
        private float _lastShownTime = -100f;
        private float _figureCardSize = 220f;
        private float _cardItemHeight = 286f;
        private float _textPanelHeight = 55f;

        private IEnumerator Start()
        {
            yield return WaitForCanvasReady();
            BuildUi();
            BindLevelProgression();
            ShowForCurrentLevel();
        }

        private void OnDestroy()
        {
            if (_levelProgressionController != null)
            {
                _levelProgressionController.LevelChanged -= OnLevelChanged;
            }
        }

        private void BindLevelProgression()
        {
            _levelProgressionController = FindFirstObjectByType<LevelProgressionController>();
            if (_levelProgressionController == null)
            {
                return;
            }

            _levelProgressionController.LevelChanged -= OnLevelChanged;
            _levelProgressionController.LevelChanged += OnLevelChanged;
        }

        private void OnLevelChanged()
        {
            StartCoroutine(ShowNextFrame());
        }

        private IEnumerator ShowNextFrame()
        {
            yield return null;
            var safety = 0;
            while (CanvasUpdateRegistry.IsRebuildingGraphics() || CanvasUpdateRegistry.IsRebuildingLayout())
            {
                if (++safety > 120)
                {
                    break;
                }

                yield return null;
            }

            ShowForCurrentLevel();
        }

        private void ShowForCurrentLevel()
        {
            if (_levelProgressionController == null)
            {
                BindLevelProgression();
            }

            if (_levelProgressionController == null)
            {
                return;
            }

            var level = _levelProgressionController.CurrentLevelNumber;
            if (level == _lastShownLevel && Time.unscaledTime - _lastShownTime < 0.75f)
            {
                return;
            }

            var currentTier = _levelProgressionController.CurrentVarietyTier;
            var previousTier = _levelProgressionController.GetVarietyTierForLevel(level - 1);
            var currentPool = PieceShapeLibrary.GetPoolForVarietyTier(currentTier);
            var previousPool = previousTier > 0
                ? PieceShapeLibrary.GetPoolForVarietyTier(previousTier)
                : new List<PieceShapeDefinition>(0);

            var entries = BuildEntries(currentPool, previousPool);
            if (entries.Count == 0)
            {
                return;
            }

            var highlightedBaseId = SelectHighlightedBase(entries);
            Canvas.ForceUpdateCanvases();
            if (_overlayRoot != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(_overlayRoot);
            }
            Render(level, currentTier, entries, highlightedBaseId);

            _overlayRoot.gameObject.SetActive(true);
            _lastShownLevel = level;
            _lastShownTime = Time.unscaledTime;

            if (_hideRoutine != null)
            {
                StopCoroutine(_hideRoutine);
            }

            _hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private List<ShapeEntry> BuildEntries(
            IReadOnlyList<PieceShapeDefinition> currentPool,
            IReadOnlyList<PieceShapeDefinition> previousPool)
        {
            var previousWeights = new Dictionary<string, int>();
            for (var i = 0; i < previousPool.Count; i++)
            {
                var id = ExtractBaseId(previousPool[i].Id);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (!previousWeights.TryGetValue(id, out var count))
                {
                    count = 0;
                }

                previousWeights[id] = count + 1;
            }

            var currentWeights = new Dictionary<string, int>();
            var representatives = new Dictionary<string, PieceShapeDefinition>();
            for (var i = 0; i < currentPool.Count; i++)
            {
                var shape = currentPool[i];
                var id = ExtractBaseId(shape.Id);
                if (string.IsNullOrEmpty(id))
                {
                    continue;
                }

                if (!currentWeights.TryGetValue(id, out var count))
                {
                    count = 0;
                }

                currentWeights[id] = count + 1;
                if (!representatives.ContainsKey(id))
                {
                    representatives[id] = shape;
                }
            }

            var result = new List<ShapeEntry>(currentWeights.Count);
            foreach (var pair in currentWeights)
            {
                var previousWeight = previousWeights.TryGetValue(pair.Key, out var value) ? value : 0;
                result.Add(new ShapeEntry
                {
                    BaseId = pair.Key,
                    Shape = representatives[pair.Key],
                    Weight = pair.Value,
                    DeltaFromPrevious = pair.Value - previousWeight,
                    IsNew = previousWeight <= 0,
                });
            }

            result.Sort((a, b) =>
            {
                var byWeight = b.Weight.CompareTo(a.Weight);
                if (byWeight != 0)
                {
                    return byWeight;
                }

                return string.CompareOrdinal(a.BaseId, b.BaseId);
            });

            return result;
        }

        private static string SelectHighlightedBase(IReadOnlyList<ShapeEntry> entries)
        {
            ShapeEntry? bestNew = null;
            ShapeEntry? bestDelta = null;

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                if (entry.IsNew)
                {
                    if (!bestNew.HasValue || entry.DeltaFromPrevious > bestNew.Value.DeltaFromPrevious)
                    {
                        bestNew = entry;
                    }
                }
                else if (entry.DeltaFromPrevious > 0)
                {
                    if (!bestDelta.HasValue || entry.DeltaFromPrevious > bestDelta.Value.DeltaFromPrevious)
                    {
                        bestDelta = entry;
                    }
                }
            }

            if (bestNew.HasValue)
            {
                return bestNew.Value.BaseId;
            }

            if (bestDelta.HasValue)
            {
                return bestDelta.Value.BaseId;
            }

            return entries.Count > 0 ? entries[0].BaseId : string.Empty;
        }

        private void Render(int level, int tier, IReadOnlyList<ShapeEntry> entries, string highlightedBaseId)
        {
            if (_overlayRoot == null || _cardsRoot == null)
            {
                return;
            }

            ApplyCardsLayoutSizing();

            var markAllAsNew = level <= 1;

            var highlightedEntry = default(ShapeEntry);
            var hasHighlightedEntry = false;
            if (!markAllAsNew)
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    if (!string.Equals(entries[i].BaseId, highlightedBaseId))
                    {
                        continue;
                    }

                    highlightedEntry = entries[i];
                    hasHighlightedEntry = true;
                    break;
                }
            }

            var highlightedTag = hasHighlightedEntry && highlightedEntry.IsNew ? "NEW" : "BOOST";

            _titleText.text = $"LEVEL {level}  |  SHAPE POOL T{tier}";
            _detailsText.text = markAllAsNew
                ? "NEW: ALL"
                : string.IsNullOrEmpty(highlightedBaseId)
                ? "NEW: -"
                : $"{highlightedTag}: {highlightedBaseId.ToUpperInvariant()}";

            for (var i = _cardsRoot.childCount - 1; i >= 0; i--)
            {
                var child = _cardsRoot.GetChild(i);
                Destroy(child.gameObject);
            }

            for (var i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var isHighlighted = markAllAsNew || string.Equals(entry.BaseId, highlightedBaseId);
                var isNewHighlight = markAllAsNew || (isHighlighted && hasHighlightedEntry && highlightedEntry.IsNew);
                CreateCard(entry, isHighlighted, isNewHighlight);
            }
        }

        private void CreateCard(ShapeEntry entry, bool isHighlighted, bool isNewHighlight)
        {
            var cardGo = new GameObject($"Shape_{entry.BaseId}", typeof(RectTransform), typeof(LayoutElement));
            var cardRect = cardGo.GetComponent<RectTransform>();
            cardRect.SetParent(_cardsRoot, false);

            var cardLayout = cardGo.GetComponent<LayoutElement>();
            cardLayout.preferredWidth = _figureCardSize;
            cardLayout.preferredHeight = _cardItemHeight;
            
            var figureCardGo = new GameObject("FigureCard", typeof(RectTransform), typeof(Image));
            var figureCardRect = figureCardGo.GetComponent<RectTransform>();
            figureCardRect.SetParent(cardRect, false);
            figureCardRect.anchorMin = new Vector2(0f, 0f);
            figureCardRect.anchorMax = new Vector2(1f, 1f);
            figureCardRect.offsetMin = new Vector2(0f, _textPanelHeight);
            figureCardRect.offsetMax = Vector2.zero;

            var figureCardImage = figureCardGo.GetComponent<Image>();
            figureCardImage.color = isHighlighted ? cardHighlightColor : cardColor;
            figureCardImage.raycastTarget = false;

            var previewGo = new GameObject("Preview", typeof(RectTransform));
            var previewRect = previewGo.GetComponent<RectTransform>();
            previewRect.SetParent(figureCardRect, false);
            previewRect.anchorMin = new Vector2(0.5f, 0.5f);
            previewRect.anchorMax = new Vector2(0.5f, 0.5f);
            previewRect.pivot = new Vector2(0.5f, 0.5f);
            previewRect.sizeDelta = new Vector2(_figureCardSize, _figureCardSize);
            PaintShape(previewRect, entry.Shape, isHighlighted);

            var textPanelGo = new GameObject("TextPanel", typeof(RectTransform), typeof(Image));
            var textPanelRect = textPanelGo.GetComponent<RectTransform>();
            textPanelRect.SetParent(cardRect, false);
            textPanelRect.anchorMin = new Vector2(0f, 0f);
            textPanelRect.anchorMax = new Vector2(1f, 0f);
            textPanelRect.pivot = new Vector2(0.5f, 0f);
            textPanelRect.sizeDelta = new Vector2(0f, _textPanelHeight);
            textPanelRect.anchoredPosition = Vector2.zero;

            var textPanelImage = textPanelGo.GetComponent<Image>();
            textPanelImage.color = isHighlighted ? textPanelHighlightColor : textPanelColor;
            textPanelImage.raycastTarget = false;

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.SetParent(textPanelRect, false);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelGo.GetComponent<Text>();
            label.font = GetDefaultFont();
            var maxFontSize = Mathf.Max(1, Mathf.RoundToInt(_textPanelHeight * 0.42f));
            var minFontSize = Mathf.Max(1, Mathf.RoundToInt(_textPanelHeight * 0.20f));
            label.fontSize = maxFontSize;
            label.resizeTextForBestFit = true;
            label.resizeTextMaxSize = maxFontSize;
            label.resizeTextMinSize = Mathf.Min(minFontSize, maxFontSize);
            label.alignment = TextAnchor.MiddleCenter;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.color = Color.white;
            label.raycastTarget = false;
            label.text = isHighlighted
                ? $"{entry.BaseId.ToUpperInvariant()}  {(isNewHighlight ? "NEW" : "BOOST")}"
                : $"{entry.BaseId.ToUpperInvariant()}";
        }

        private void PaintShape(RectTransform previewRect, PieceShapeDefinition shape, bool isHighlighted)
        {
            if (previewRect == null || shape == null || shape.Cells == null || shape.Cells.Length == 0)
            {
                return;
            }

            var minX = int.MaxValue;
            var minY = int.MaxValue;
            var maxX = int.MinValue;
            var maxY = int.MinValue;
            for (var i = 0; i < shape.Cells.Length; i++)
            {
                var c = shape.Cells[i];
                if (c.x < minX) minX = c.x;
                if (c.y < minY) minY = c.y;
                if (c.x > maxX) maxX = c.x;
                if (c.y > maxY) maxY = c.y;
            }

            var widthCells = maxX - minX + 1;
            var heightCells = maxY - minY + 1;
            var maxSide = Mathf.Min(previewRect.rect.width, previewRect.rect.height) * 0.8f;
            var cellSize = Mathf.Floor(maxSide / Mathf.Max(1, previewGridSize));
            if (cellSize < 2f)
            {
                cellSize = 2f;
            }

            var shapeWidth = widthCells * cellSize;
            var shapeHeight = heightCells * cellSize;
            var startX = -shapeWidth * 0.5f + cellSize * 0.5f;
            var startY = -shapeHeight * 0.5f + cellSize * 0.5f;
            var color = isHighlighted ? miniCellHighlightColor : miniCellColor;
            var visualCellSize = Mathf.Max(1f, Mathf.Floor(cellSize * Mathf.Clamp(miniCellFillRatio, 0.6f, 1f)));

            for (var i = 0; i < shape.Cells.Length; i++)
            {
                var c = shape.Cells[i];
                var localX = c.x - minX;
                var localY = c.y - minY;
                var cellGo = new GameObject($"Mini_{i:00}", typeof(RectTransform), typeof(Image));
                var cellRect = cellGo.GetComponent<RectTransform>();
                cellRect.SetParent(previewRect, false);
                cellRect.anchorMin = new Vector2(0.5f, 0.5f);
                cellRect.anchorMax = new Vector2(0.5f, 0.5f);
                cellRect.pivot = new Vector2(0.5f, 0.5f);
                cellRect.sizeDelta = new Vector2(visualCellSize, visualCellSize);
                cellRect.anchoredPosition = new Vector2(startX + localX * cellSize, startY + localY * cellSize);

                var image = cellGo.GetComponent<Image>();
                image.color = color;
                image.raycastTarget = false;
            }
        }

        private void BuildUi()
        {
            var overlay = transform.Find("LevelShapePoolOverlay") as RectTransform;
            if (overlay == null)
            {
                var overlayGo = new GameObject("LevelShapePoolOverlay", typeof(RectTransform), typeof(Image));
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
            _overlayRoot = overlay;

            var panel = overlay.Find("Panel") as RectTransform;
            if (panel == null)
            {
                var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
                panel = panelGo.GetComponent<RectTransform>();
                panel.SetParent(overlay, false);
            }

            panel.anchorMin = new Vector2(0.02f, 0.06f);
            panel.anchorMax = new Vector2(0.98f, 0.94f);
            panel.offsetMin = Vector2.zero;
            panel.offsetMax = Vector2.zero;

            var panelImage = panel.GetComponent<Image>();
            panelImage.color = panelColor;
            panelImage.raycastTarget = true;

            var panelLayout = panel.GetComponent<VerticalLayoutGroup>();
            panelLayout.padding = new RectOffset(12, 12, 12, 12);
            panelLayout.spacing = 12f;
            panelLayout.childAlignment = TextAnchor.UpperCenter;
            panelLayout.childControlHeight = false;
            panelLayout.childControlWidth = true;
            panelLayout.childForceExpandHeight = false;
            panelLayout.childForceExpandWidth = true;

            _titleText = EnsureTextNode(panel, "TitleText", 46, FontStyle.Bold, TextAnchor.MiddleCenter, 72f);
            _detailsText = EnsureTextNode(panel, "DetailsText", 30, FontStyle.Bold, TextAnchor.MiddleCenter, 56f);

            var cards = panel.Find("CardsRoot") as RectTransform;
            if (cards == null)
            {
                var cardsGo = new GameObject("CardsRoot", typeof(RectTransform), typeof(GridLayoutGroup), typeof(LayoutElement));
                cards = cardsGo.GetComponent<RectTransform>();
                cards.SetParent(panel, false);
            }

            var cardsLayout = cards.GetComponent<LayoutElement>();
            cardsLayout.flexibleHeight = 1f;
            cardsLayout.minHeight = 200f;

            var grid = cards.GetComponent<GridLayoutGroup>();
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = Mathf.Max(1, cardColumns);
            grid.spacing = new Vector2(fixedCardsSpacingPx, fixedCardsSpacingPx);
            grid.childAlignment = TextAnchor.UpperCenter;
            grid.startCorner = GridLayoutGroup.Corner.UpperLeft;
            grid.startAxis = GridLayoutGroup.Axis.Horizontal;
            _cardsGrid = grid;
            _cardsRoot = cards;
            ApplyCardsLayoutSizing();

            _overlayRoot.gameObject.SetActive(false);
        }

        private void OnRectTransformDimensionsChange()
        {
            ApplyCardsLayoutSizing();
        }

        private void ApplyCardsLayoutSizing()
        {
            if (_overlayRoot == null || _cardsRoot == null || _cardsGrid == null)
            {
                return;
            }

            _cardsGrid.constraintCount = Mathf.Max(1, cardColumns);
            _cardsGrid.spacing = new Vector2(Mathf.Max(0f, fixedCardsSpacingPx), Mathf.Max(0f, fixedCardsSpacingPx));

            var columns = Mathf.Max(1, cardColumns);
            var figureCardWidth = Mathf.Max(1f, fixedFigureCardSizePx);
            var figureCardHeight = figureCardWidth;

            // Keep the total columns span pinned to iPhone 15 Pro Max target width.
            var requiredColumnsWidth = figureCardWidth * columns + _cardsGrid.spacing.x * (columns - 1);
            var cardsLayout = _cardsRoot.GetComponent<LayoutElement>();
            if (cardsLayout != null)
            {
                var derivedColumnsWidthFromTarget = Mathf.Floor(Mathf.Max(1f, targetScreenWidthPx) * 0.8f);
                var pinnedWidth = Mathf.Max(Mathf.Max(fixedColumnsWidthPx, derivedColumnsWidthFromTarget), requiredColumnsWidth);
                cardsLayout.preferredWidth = pinnedWidth;
                cardsLayout.minWidth = pinnedWidth;
            }

            _figureCardSize = figureCardWidth;
            _textPanelHeight = Mathf.Max(1f, fixedTextPanelHeightPx);
            _cardItemHeight = figureCardHeight + _textPanelHeight;
            _cardsGrid.cellSize = new Vector2(_figureCardSize, _cardItemHeight);
        }

        private static Text EnsureTextNode(
            RectTransform parent,
            string name,
            int fontSize,
            FontStyle style,
            TextAnchor anchor,
            float preferredHeight)
        {
            var node = parent.Find(name) as RectTransform;
            if (node == null)
            {
                var nodeGo = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(LayoutElement));
                node = nodeGo.GetComponent<RectTransform>();
                node.SetParent(parent, false);
            }

            var layout = node.GetComponent<LayoutElement>();
            layout.preferredHeight = preferredHeight;

            var text = node.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.alignment = anchor;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private IEnumerator HideAfterDelay()
        {
            var duration = Mathf.Max(0.1f, showDurationSeconds);
            yield return new WaitForSecondsRealtime(duration);
            if (_overlayRoot != null)
            {
                _overlayRoot.gameObject.SetActive(false);
            }
        }

        private static string ExtractBaseId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return string.Empty;
            }

            var split = id.LastIndexOf('_');
            if (split <= 0 || split >= id.Length - 1)
            {
                return id;
            }

            for (var i = split + 1; i < id.Length; i++)
            {
                if (id[i] < '0' || id[i] > '9')
                {
                    return id;
                }
            }

            return id.Substring(0, split);
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

        private struct ShapeEntry
        {
            public string BaseId;
            public PieceShapeDefinition Shape;
            public int Weight;
            public int DeltaFromPrevious;
            public bool IsNew;
        }
    }
}

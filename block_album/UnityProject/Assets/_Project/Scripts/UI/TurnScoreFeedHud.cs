using System.Collections;
using System.Collections.Generic;
using BlockAlbum.Grid;
using UnityEngine;
using UnityEngine.UI;

namespace BlockAlbum.UI
{
    [DisallowMultipleComponent]
    public sealed class TurnScoreFeedHud : MonoBehaviour
    {
        [SerializeField] private int maxEntries = 4;
        [SerializeField] private float entryLifetime = 2.8f;
        [SerializeField] private float fadeDuration = 0.45f;
        [SerializeField] private int fontSize = 24;
        [SerializeField] private Color textColor = new Color(1f, 0.95f, 0.72f, 1f);
        [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.18f);

        private readonly List<GameObject> _entries = new List<GameObject>(8);
        private BoardView _boardView;
        private RectTransform _feedRoot;

        private IEnumerator Start()
        {
            yield return WaitForCanvasReady();
            BuildUi();
            BindBoard();
        }

        private void OnDestroy()
        {
            if (_boardView != null)
            {
                _boardView.TurnResolved -= OnTurnResolved;
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
        }

        private void BuildUi()
        {
            var existing = transform.Find("TurnScoreFeed") as RectTransform;
            if (existing == null)
            {
                var rootGo = new GameObject("TurnScoreFeed", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
                existing = rootGo.GetComponent<RectTransform>();
                existing.SetParent(transform, false);
            }

            existing.anchorMin = new Vector2(0.03f, 0.66f);
            existing.anchorMax = new Vector2(0.97f, 0.78f);
            existing.offsetMin = Vector2.zero;
            existing.offsetMax = Vector2.zero;

            var image = existing.GetComponent<Image>();
            image.color = panelColor;
            image.raycastTarget = false;

            var layout = existing.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 4f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _feedRoot = existing;
        }

        private void OnTurnResolved(BoardTurnResult result)
        {
            if (!result.PlacementSucceeded || _feedRoot == null)
            {
                return;
            }

            AddEntry(FormatEntry(result));
        }

        private string FormatEntry(BoardTurnResult result)
        {
            var text = $"+{result.ScoreGained}  ";
            text += $"Cells:+{result.ScoreFromCells}  ";
            text += $"Blockers:+{result.ScoreFromBlockers}  ";
            text += $"Lines:+{result.ScoreFromLines}  ";
            text += $"Zones:+{result.ScoreFromZones}";
            if (result.ComboBonusScore > 0)
            {
                text += $"  Combo:+{result.ComboBonusScore}";
            }

            return text;
        }

        private void AddEntry(string value)
        {
            var entryGo = new GameObject("Entry", typeof(RectTransform), typeof(Text), typeof(LayoutElement), typeof(CanvasGroup));
            var rect = entryGo.GetComponent<RectTransform>();
            rect.SetParent(_feedRoot, false);

            var layout = entryGo.GetComponent<LayoutElement>();
            layout.preferredHeight = 34f;

            var text = entryGo.GetComponent<Text>();
            text.font = GetDefaultFont();
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.color = textColor;
            text.raycastTarget = false;
            text.text = value;

            _entries.Add(entryGo);
            while (_entries.Count > Mathf.Max(1, maxEntries))
            {
                var oldest = _entries[0];
                _entries.RemoveAt(0);
                if (oldest != null)
                {
                    Destroy(oldest);
                }
            }

            StartCoroutine(FadeAndRemove(entryGo));
        }

        private IEnumerator FadeAndRemove(GameObject entryGo)
        {
            if (entryGo == null)
            {
                yield break;
            }

            yield return new WaitForSeconds(Mathf.Max(0.1f, entryLifetime));
            if (entryGo == null)
            {
                yield break;
            }

            var cg = entryGo.GetComponent<CanvasGroup>();
            var duration = Mathf.Max(0.05f, fadeDuration);
            var t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                var a = 1f - Mathf.Clamp01(t / duration);
                if (cg != null)
                {
                    cg.alpha = a;
                }

                yield return null;
            }

            _entries.Remove(entryGo);
            if (entryGo != null)
            {
                Destroy(entryGo);
            }
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

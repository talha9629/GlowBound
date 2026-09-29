using System;
using System.Collections.Generic;
using Glowbound.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace Glowbound.Unity.Prototype
{
    public enum PrototypeLanternGesture { DoubleTap, LongPress }

    public static class GlowboundPrototypeBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (UnityEngine.Object.FindFirstObjectByType<GlowboundPrototypeController>() != null) return;
            var go = new GameObject("Glowbound Prototype");
            UnityEngine.Object.DontDestroyOnLoad(go);
            go.AddComponent<GlowboundPrototypeController>();
        }
    }

    public sealed class GlowboundPrototypeController : MonoBehaviour
    {
        private const float DoubleTapWindow = 0.28f;
        private const float LongPressSeconds = 0.42f;
        private PrototypeArtSkin _art;
        private CompiledPuzzle _puzzle;
        private PuzzleState _state;
        private PuzzleEvaluationResult _evaluation;
        private int _levelIndex = 3;
        private PrototypeLanternGesture _gesture = PrototypeLanternGesture.LongPress;
        private int _pressedCell = -1, _pendingTapCell = -1;
        private float _pressStartedAt, _pendingTapAt;
        private bool _longPressTriggered;

        private Canvas _canvas;
        private RectTransform _boardRoot;
        private Text _statusText, _levelText, _gestureText;
        private PrototypeCellView[] _cellViews;
        private Font _font;

        private void Awake()
        {
            _art = new PrototypeArtSkin();
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BuildCanvas();
            LoadLevel(_levelIndex);
        }

        private void Update()
        {
            if (_gesture == PrototypeLanternGesture.DoubleTap && _pendingTapCell >= 0 && Time.unscaledTime - _pendingTapAt > DoubleTapWindow)
            {
                ToggleX(_pendingTapCell);
                _pendingTapCell = -1;
            }

            if (_gesture == PrototypeLanternGesture.LongPress && _pressedCell >= 0 && !_longPressTriggered && Time.unscaledTime - _pressStartedAt >= LongPressSeconds)
            {
                ToggleLantern(_pressedCell);
                _longPressTriggered = true;
            }
        }
        private void BuildCanvas()
        {
            var canvasGo = new GameObject("Glowbound Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            _canvas = canvasGo.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            EnsureEventSystem();
            CreateBackground(canvasGo.transform);
            CreateHeader(canvasGo.transform);
            CreateBoardRoot(canvasGo.transform);
            CreateFooter(canvasGo.transform);
        }

        private void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var eventGo = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventGo.transform.SetParent(transform, false);
        }

        private void CreateBackground(Transform parent)
        {
            var image = CreateImage("Background", parent, null, new Color(0.97f, 0.94f, 0.87f, 1f));
            var rt = image.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        private void CreateHeader(Transform parent)
        {
            var title = CreateText("Title", parent, "GLOWBOUND", 52, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 80f), new Vector2(0f, -70f));

            _levelText = CreateText("Level", parent, string.Empty, 24, FontStyle.Normal, TextAnchor.MiddleCenter);
            SetRect(_levelText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 50f), new Vector2(0f, -125f));

            _statusText = CreateText("Status", parent, string.Empty, 28, FontStyle.Bold, TextAnchor.MiddleCenter);
            SetRect(_statusText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(900f, 60f), new Vector2(0f, -180f));
        }

        private void CreateBoardRoot(Transform parent)
        {
            var go = new GameObject("Board", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            _boardRoot = (RectTransform)go.transform;
            _boardRoot.anchorMin = _boardRoot.anchorMax = new Vector2(0.5f, 0.5f);
            _boardRoot.sizeDelta = new Vector2(960f, 1120f);
            _boardRoot.anchoredPosition = new Vector2(0f, 45f);
        }

        private void CreateFooter(Transform parent)
        {
            CreateButton(parent, "Prev", "< Level", new Vector2(-330f, 160f), () => LoadLevel((_levelIndex - 1 + PrototypePuzzleCatalog.Count) % PrototypePuzzleCatalog.Count));
            CreateButton(parent, "Reset", "Reset", new Vector2(0f, 160f), ResetPuzzle);
            CreateButton(parent, "Next", "Level >", new Vector2(330f, 160f), () => LoadLevel((_levelIndex + 1) % PrototypePuzzleCatalog.Count));

            var gestureButton = CreateButton(parent, "Gesture", string.Empty, new Vector2(0f, 90f), ToggleGesture, new Vector2(820f, 64f));
            _gestureText = gestureButton.GetComponentInChildren<Text>();
        }
        private void BuildBoard()
        {
            for (var i = _boardRoot.childCount - 1; i >= 0; i--)
                Destroy(_boardRoot.GetChild(i).gameObject);

            var count = _puzzle.Definition.CellCount;
            _cellViews = new PrototypeCellView[count];
            var cellSize = Mathf.Min(180f, Mathf.Min(900f / _puzzle.Definition.Width, 1040f / _puzzle.Definition.Height));
            var boardWidth = cellSize * _puzzle.Definition.Width;
            var boardHeight = cellSize * _puzzle.Definition.Height;

            for (var i = 0; i < count; i++)
            {
                _puzzle.Definition.ToCoordinates(i, out var x, out var y);
                var cellGo = new GameObject($"Cell {i}", typeof(RectTransform), typeof(Image), typeof(PrototypeCellView));
                cellGo.transform.SetParent(_boardRoot, false);
                var rt = (RectTransform)cellGo.transform;
                rt.sizeDelta = new Vector2(cellSize, cellSize);
                rt.anchoredPosition = new Vector2(-boardWidth * 0.5f + cellSize * (x + 0.5f), boardHeight * 0.5f - cellSize * (y + 0.5f));

                var baseImage = cellGo.GetComponent<Image>();
                baseImage.preserveAspect = true;
                var beam = CreateImage("Beam", cellGo.transform, null, new Color(1f, 0.78f, 0.18f, 0.20f));
                Stretch(beam.rectTransform, 12f);
                beam.raycastTarget = false;
                var mark = CreateImage("Mark", cellGo.transform, null, Color.white);
                Stretch(mark.rectTransform, cellSize * 0.16f);
                mark.raycastTarget = false;
                var number = CreateText("Number", cellGo.transform, string.Empty, Mathf.RoundToInt(cellSize * 0.30f), FontStyle.Bold, TextAnchor.MiddleCenter);
                Stretch(number.rectTransform, cellSize * 0.18f);
                number.raycastTarget = false;

                var view = cellGo.GetComponent<PrototypeCellView>();
                view.Initialize(this, i, baseImage, beam, mark, number);
                _cellViews[i] = view;
            }
        }
        private void RefreshBoard()
        {
            var litCells = new HashSet<int>();
            for (var i = 0; i < _state.CellCount; i++)
            {
                if (_state[i] != PlayerCellState.Lantern) continue;
                foreach (var ray in LightPropagation.TraceFromLantern(_puzzle, _state, i))
                    foreach (var cell in ray.TraversedPlayableCells) litCells.Add(cell);
            }

            for (var i = 0; i < _cellViews.Length; i++)
            {
                var view = _cellViews[i];
                var def = _puzzle.Definition.Cells[i];
                view.BeamImage.gameObject.SetActive(def.Kind == CellKind.District && litCells.Contains(i));
                view.MarkImage.gameObject.SetActive(false);
                view.NumberText.text = string.Empty;

                if (def.Kind == CellKind.District)
                {
                    view.BaseImage.sprite = _art.Tile(def.DistrictId);
                    if (_state[i] == PlayerCellState.X) { view.MarkImage.sprite = _art.XMark; view.MarkImage.gameObject.SetActive(true); }
                    else if (_state[i] == PlayerCellState.Lantern) { view.MarkImage.sprite = _art.Lantern; view.MarkImage.gameObject.SetActive(true); }
                }
                else if (def.Kind == CellKind.House)
                {
                    var house = FindHouse(i);
                    view.BaseImage.sprite = _art.House(house?.IncomingMask ?? LightDirectionMask.None);
                    view.NumberText.text = def.HouseTarget.ToString();
                    view.NumberText.color = house.HasValue && house.Value.IncomingCount > house.Value.Target ? new Color(0.75f, 0.10f, 0.08f) : new Color(0.28f, 0.18f, 0.12f);
                }
                else view.BaseImage.sprite = _art.Wall(GetWallIncomingMask(i));
            }

            RefreshHeader();
        }
        private void RefreshHeader()
        {
            _levelText.text = $"Prototype - Level {_levelIndex + 1}/{PrototypePuzzleCatalog.Count}";
            if (_evaluation.IsSolved)
            {
                _statusText.text = "SOLVED";
                _statusText.color = new Color(0.14f, 0.48f, 0.25f);
            }
            else if (_evaluation.HasContradiction)
            {
                _statusText.text = "Visible contradiction";
                _statusText.color = new Color(0.70f, 0.18f, 0.16f);
            }
            else
            {
                _statusText.text = "Light every district";
                _statusText.color = new Color(0.22f, 0.25f, 0.28f);
            }

            _gestureText.text = _gesture == PrototypeLanternGesture.LongPress
                ? "Tap = X   |   Hold = Lantern"
                : "Tap = X   |   Double Tap = Lantern";
        }

        public void OnCellPointerDown(int cell)
        {
            if (!IsPlayable(cell)) return;
            _pressedCell = cell;
            _pressStartedAt = Time.unscaledTime;
            _longPressTriggered = false;
        }

        public void OnCellPointerExit(int cell)
        {
            if (_pressedCell == cell) _pressedCell = -1;
        }
        public void OnCellPointerUp(int cell)
        {
            if (_pressedCell != cell) return;
            _pressedCell = -1;

            if (_gesture == PrototypeLanternGesture.LongPress)
            {
                if (!_longPressTriggered) ToggleX(cell);
            }
            else HandleDoubleTap(cell);

            _longPressTriggered = false;
        }

        private void HandleDoubleTap(int cell)
        {
            if (_pendingTapCell == cell && Time.unscaledTime - _pendingTapAt <= DoubleTapWindow)
            {
                _pendingTapCell = -1;
                ToggleLantern(cell);
                return;
            }

            if (_pendingTapCell >= 0) ToggleX(_pendingTapCell);
            _pendingTapCell = cell;
            _pendingTapAt = Time.unscaledTime;
        }

        private void ToggleX(int cell)
        {
            if (!IsPlayable(cell)) return;
            _state[cell] = _state[cell] == PlayerCellState.X ? PlayerCellState.Empty : PlayerCellState.X;
            Reevaluate();
        }

        private void ToggleLantern(int cell)
        {
            if (!IsPlayable(cell)) return;
            _state[cell] = _state[cell] == PlayerCellState.Lantern ? PlayerCellState.Empty : PlayerCellState.Lantern;
            Reevaluate();
        }
        private void Reevaluate()
        {
            _evaluation = PuzzleEvaluator.Evaluate(_puzzle, _state);
            RefreshBoard();
        }

        private void ResetPuzzle()
        {
            _state.Clear();
            CancelPointer();
            Reevaluate();
        }

        private void ToggleGesture()
        {
            _gesture = _gesture == PrototypeLanternGesture.LongPress ? PrototypeLanternGesture.DoubleTap : PrototypeLanternGesture.LongPress;
            CancelPointer();
            RefreshHeader();
        }

        private void LoadLevel(int index)
        {
            _levelIndex = Mathf.Clamp(index, 0, PrototypePuzzleCatalog.Count - 1);
            _puzzle = PuzzleCompiler.Compile(PrototypePuzzleCatalog.Get(_levelIndex));
            _state = new PuzzleState(_puzzle.Definition);
            CancelPointer();
            _evaluation = PuzzleEvaluator.Evaluate(_puzzle, _state);
            BuildBoard();
            RefreshBoard();
        }

        private void CancelPointer()
        {
            _pressedCell = -1;
            _pendingTapCell = -1;
            _longPressTriggered = false;
        }
        private bool IsPlayable(int cell) => cell >= 0 && cell < _puzzle.Definition.CellCount && _puzzle.Definition.Cells[cell].Kind == CellKind.District;

        private HouseEvaluation? FindHouse(int cell)
        {
            foreach (var house in _evaluation.Houses)
                if (house.CellIndex == cell) return house;
            return null;
        }

        private LightDirectionMask GetWallIncomingMask(int wallCell)
        {
            var mask = LightDirectionMask.None;
            for (var i = 0; i < _state.CellCount; i++)
            {
                if (_state[i] != PlayerCellState.Lantern) continue;
                foreach (var ray in LightPropagation.TraceFromLantern(_puzzle, _state, i))
                {
                    if (ray.Termination != LightRayTermination.Wall || ray.TerminalCellIndex != wallCell) continue;
                    mask |= IncomingMask(ray.Direction);
                }
            }
            return mask;
        }

        private static LightDirectionMask IncomingMask(LightDirection rayDirection)
        {
            return rayDirection switch
            {
                LightDirection.Up => LightDirectionMask.Down,
                LightDirection.Right => LightDirectionMask.Left,
                LightDirection.Down => LightDirectionMask.Up,
                LightDirection.Left => LightDirectionMask.Right,
                _ => LightDirectionMask.None
            };
        }
        private Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            image.preserveAspect = true;
            return image;
        }

        private Text CreateText(string name, Transform parent, string value, int size, FontStyle style, TextAnchor anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var text = go.GetComponent<Text>();
            text.font = _font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = anchor;
            text.color = new Color(0.22f, 0.20f, 0.18f);
            return text;
        }

        private Button CreateButton(Transform parent, string name, string label, Vector2 position, UnityEngine.Events.UnityAction action, Vector2? size = null)
        {
            var image = CreateImage(name, parent, null, new Color(1f, 0.96f, 0.88f, 0.98f));
            var button = image.gameObject.AddComponent<Button>();
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.sizeDelta = size ?? new Vector2(250f, 64f);
            rt.anchoredPosition = position;
            button.onClick.AddListener(action);
            var text = CreateText("Label", image.transform, label, 24, FontStyle.Bold, TextAnchor.MiddleCenter);
            Stretch(text.rectTransform, 6f);
            return button;
        }
        private static void SetRect(RectTransform rt, Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 position)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rt, float inset)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(inset, inset);
            rt.offsetMax = new Vector2(-inset, -inset);
        }
    }

    public static class PrototypePuzzleCatalog
    {
        private static readonly string[][] Levels =
        {
            new[]{"A"}, new[]{"1A1"}, new[]{"A2B"}, new[]{"#A#","B3C","###"}, new[]{"EEED1","E2DDD","EBBDD","ADDDC","DD3CC"}
        };

        public static int Count => Levels.Length;

        public static PuzzleDefinition Get(int index)
        {
            if (index < 0 || index >= Levels.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return Parse($"prototype-{index + 1:00}", Levels[index]);
        }
        private static PuzzleDefinition Parse(string id, string[] rows)
        {
            if (rows == null || rows.Length == 0) throw new ArgumentException("At least one row is required.", nameof(rows));
            var width = rows[0].Length;
            var cells = new List<CellDefinition>(width * rows.Length);

            for (var y = 0; y < rows.Length; y++)
            {
                if (rows[y].Length != width) throw new InvalidOperationException("Prototype rows must be rectangular.");
                for (var x = 0; x < width; x++)
                {
                    var token = rows[y][x];
                    if (token >= 'A' && token <= 'Z') cells.Add(CellDefinition.District(token - 'A'));
                    else if (token >= '1' && token <= '3') cells.Add(CellDefinition.House((byte)(token - '0')));
                    else if (token == '#') cells.Add(CellDefinition.Wall());
                    else throw new InvalidOperationException($"Unsupported token '{token}'.");
                }
            }

            return new PuzzleDefinition(id, width, rows.Length, cells.ToArray());
        }
    }
}

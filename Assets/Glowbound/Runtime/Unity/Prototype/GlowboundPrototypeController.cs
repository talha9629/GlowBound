using System;
using System.Collections.Generic;
using Glowbound.Core;
using UnityEngine;

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
        private readonly Color[] _districtColors =
        {
            new Color(0.95f,0.76f,0.73f), new Color(0.72f,0.88f,0.81f),
            new Color(0.76f,0.82f,0.96f), new Color(0.94f,0.87f,0.66f),
            new Color(0.86f,0.76f,0.93f), new Color(0.70f,0.87f,0.91f),
            new Color(0.94f,0.78f,0.87f), new Color(0.82f,0.90f,0.68f)
        };

        private GUIStyle _title, _status, _cell, _house, _small;
        private CompiledPuzzle _puzzle;
        private PuzzleState _state;
        private PuzzleEvaluationResult _evaluation;
        private int _levelIndex = 3;
        private PrototypeLanternGesture _gesture = PrototypeLanternGesture.LongPress;
        private int _pressedCell = -1, _pendingTapCell = -1;
        private float _pressStartedAt, _pendingTapAt;
        private bool _longPressTriggered;
        private Rect _boardRect;
        private float _cellSize;

        private void Awake() => LoadLevel(_levelIndex);

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

        private void OnGUI()
        {
            EnsureStyles(); DrawBackground(); DrawHeader();
            if (_puzzle == null) return;
            CalculateBoardRect(); DrawBoard(); DrawFooter(); HandlePointer(Event.current);
        }

        private void EnsureStyles()
        {
            if (_title != null) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _status = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _cell = new GUIStyle(GUI.skin.box) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            _house = new GUIStyle(_cell) { fontSize = 28 };
            _small = new GUIStyle(GUI.skin.label) { fontSize = 14, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        }

        private void DrawBackground()
        {
            var old = GUI.color; GUI.color = new Color(0.97f,0.94f,0.87f);
            GUI.DrawTexture(new Rect(0,0,Screen.width,Screen.height), Texture2D.whiteTexture); GUI.color = old;
        }

        private void DrawHeader()
        {
            GUI.Label(new Rect(20,14,Screen.width-40,42), "GLOWBOUND", _title);
            GUI.Label(new Rect(20,52,Screen.width-40,28), $"Prototype  -  Level {_levelIndex+1}/{PrototypePuzzleCatalog.Count}", _small);
            var text = _evaluation == null ? "Loading" : _evaluation.IsSolved ? "SOLVED  OK" : _evaluation.HasContradiction ? "Visible contradiction" : "Light every district";
            var old = GUI.contentColor;
            GUI.contentColor = _evaluation != null && _evaluation.IsSolved ? new Color(0.14f,0.48f,0.25f) : _evaluation != null && _evaluation.HasContradiction ? new Color(0.70f,0.18f,0.16f) : new Color(0.22f,0.25f,0.28f);
            GUI.Label(new Rect(20,80,Screen.width-40,32), text, _status); GUI.contentColor = old;
        }

        private void CalculateBoardRect()
        {
            const float top=122f, footer=150f;
            var aw=Mathf.Max(120f,Screen.width-32f); var ah=Mathf.Max(120f,Screen.height-top-footer);
            _cellSize=Mathf.Max(34f,Mathf.Floor(Mathf.Min(aw/_puzzle.Definition.Width,ah/_puzzle.Definition.Height)));
            var w=_cellSize*_puzzle.Definition.Width; var h=_cellSize*_puzzle.Definition.Height;
            _boardRect=new Rect((Screen.width-w)*0.5f,top+Mathf.Max(0f,(ah-h)*0.35f),w,h);
        }

        private void DrawBoard()
        {
            DrawBeams();
            for(var i=0;i<_puzzle.Definition.CellCount;i++)
            {
                var rect=CellRect(i); var c=_puzzle.Definition.Cells[i];
                if(c.Kind==CellKind.District) DrawDistrict(i,rect,c); else if(c.Kind==CellKind.House) DrawHouse(i,rect,c); else DrawWall(rect);
            }
        }

        private void DrawDistrict(int index, Rect rect, CellDefinition c)
        {
            var old=GUI.backgroundColor; GUI.backgroundColor=_districtColors[Math.Abs(c.DistrictId)%_districtColors.Length]; GUI.Box(Shrink(rect,1f),GUIContent.none,_cell); GUI.backgroundColor=old;
            if(_state[index]==PlayerCellState.X)
            {
                var oc=GUI.contentColor; GUI.contentColor=new Color(0.35f,0.38f,0.40f); GUI.Label(rect,"X",_cell); GUI.contentColor=oc;
            }
            else if(_state[index]==PlayerCellState.Lantern)
            {
                var inner=Shrink(rect,_cellSize*0.22f); var oc=GUI.color; GUI.color=new Color(1f,0.68f,0.10f,0.98f); GUI.DrawTexture(inner,Texture2D.whiteTexture); GUI.color=oc; GUI.Label(rect,"L",_cell);
            }
        }

        private void DrawHouse(int index, Rect rect, CellDefinition c)
        {
            var old=GUI.backgroundColor; GUI.backgroundColor=new Color(0.82f,0.67f,0.48f); GUI.Box(Shrink(rect,1f),c.HouseTarget.ToString(),_house); GUI.backgroundColor=old;
            var h=FindHouse(index); if(!h.HasValue) return;
            if(h.Value.IncomingCount>h.Value.Target) Overlay(rect,new Color(0.85f,0.12f,0.10f,0.32f)); else if(h.Value.IsSatisfied) Overlay(rect,new Color(0.20f,0.70f,0.28f,0.22f));
        }

        private void DrawWall(Rect rect)
        {
            var old=GUI.backgroundColor; GUI.backgroundColor=new Color(0.25f,0.42f,0.28f); GUI.Box(Shrink(rect,1f),"#",_cell); GUI.backgroundColor=old;
        }

        private void DrawBeams()
        {
            var old=GUI.color; GUI.color=new Color(1f,0.78f,0.18f,0.22f);
            for(var i=0;i<_state.CellCount;i++)
            {
                if(_state[i]!=PlayerCellState.Lantern) continue;
                var rays=LightPropagation.TraceFromLantern(_puzzle,_state,i);
                foreach(var ray in rays)
                {
                    foreach(var cell in ray.TraversedPlayableCells) GUI.DrawTexture(Shrink(CellRect(cell),4f),Texture2D.whiteTexture);
                    if(ray.IlluminatesHouse && ray.TerminalCellIndex>=0) GUI.DrawTexture(Shrink(CellRect(ray.TerminalCellIndex),8f),Texture2D.whiteTexture);
                }
            }
            GUI.color=old;
        }

        private void DrawFooter()
        {
            var y=Mathf.Min(Screen.height-132f,_boardRect.yMax+14f); var bw=Mathf.Min(150f,(Screen.width-48f)/3f);
            if(GUI.Button(new Rect(16,y,bw,38),"< Level")) LoadLevel((_levelIndex-1+PrototypePuzzleCatalog.Count)%PrototypePuzzleCatalog.Count);
            if(GUI.Button(new Rect((Screen.width-bw)*0.5f,y,bw,38),"Reset")) ResetPuzzle();
            if(GUI.Button(new Rect(Screen.width-bw-16,y,bw,38),"Level >")) LoadLevel((_levelIndex+1)%PrototypePuzzleCatalog.Count);
            y+=46f;
            var label=_gesture==PrototypeLanternGesture.LongPress ? "Input: Tap = X  -  Hold = Lantern" : "Input: Tap = X  -  Double Tap = Lantern";
            if(GUI.Button(new Rect(26,y,Screen.width-52,38),label)) { _gesture=_gesture==PrototypeLanternGesture.LongPress?PrototypeLanternGesture.DoubleTap:PrototypeLanternGesture.LongPress; CancelPointer(); }
            GUI.Label(new Rect(20,y+42f,Screen.width-40,34),"Prototype only - logic and feel first, art later.",_small);
        }

        private void HandlePointer(Event e)
        {
            if(e==null) return;
            if(e.type==EventType.MouseDown && e.button==0)
            {
                var cell=CellAt(e.mousePosition);
                if(cell>=0 && _puzzle.Definition.Cells[cell].Kind==CellKind.District) { _pressedCell=cell; _pressStartedAt=Time.unscaledTime; _longPressTriggered=false; e.Use(); }
            }
            else if(e.type==EventType.MouseUp && e.button==0 && _pressedCell>=0)
            {
                var released=CellAt(e.mousePosition); var cell=_pressedCell; _pressedCell=-1;
                if(released!=cell) { _longPressTriggered=false; return; }
                if(_gesture==PrototypeLanternGesture.LongPress) { if(!_longPressTriggered) ToggleX(cell); } else HandleDoubleTap(cell);
                _longPressTriggered=false; e.Use();
            }
        }

        private void HandleDoubleTap(int cell)
        {
            if(_pendingTapCell==cell && Time.unscaledTime-_pendingTapAt<=DoubleTapWindow) { _pendingTapCell=-1; ToggleLantern(cell); return; }
            if(_pendingTapCell>=0) ToggleX(_pendingTapCell);
            _pendingTapCell=cell; _pendingTapAt=Time.unscaledTime;
        }

        private void ToggleX(int cell)
        {
            if(!IsPlayable(cell)) return;
            _state[cell]=_state[cell]==PlayerCellState.X?PlayerCellState.Empty:PlayerCellState.X; Reevaluate();
        }

        private void ToggleLantern(int cell)
        {
            if(!IsPlayable(cell)) return;
            _state[cell]=_state[cell]==PlayerCellState.Lantern?PlayerCellState.Empty:PlayerCellState.Lantern; Reevaluate();
        }

        private void Reevaluate()=>_evaluation=PuzzleEvaluator.Evaluate(_puzzle,_state);
        private void ResetPuzzle(){_state.Clear();CancelPointer();Reevaluate();}
        private void LoadLevel(int index){_levelIndex=Mathf.Clamp(index,0,PrototypePuzzleCatalog.Count-1);_puzzle=PuzzleCompiler.Compile(PrototypePuzzleCatalog.Get(_levelIndex));_state=new PuzzleState(_puzzle.Definition);CancelPointer();Reevaluate();}
        private void CancelPointer(){_pressedCell=-1;_pendingTapCell=-1;_longPressTriggered=false;}
        private bool IsPlayable(int cell)=>cell>=0&&cell<_puzzle.Definition.CellCount&&_puzzle.Definition.Cells[cell].Kind==CellKind.District;

        private Rect CellRect(int index)
        {
            _puzzle.Definition.ToCoordinates(index,out var x,out var y); return new Rect(_boardRect.x+x*_cellSize,_boardRect.y+y*_cellSize,_cellSize,_cellSize);
        }

        private int CellAt(Vector2 p)
        {
            if(!_boardRect.Contains(p)) return -1; var x=Mathf.FloorToInt((p.x-_boardRect.x)/_cellSize); var y=Mathf.FloorToInt((p.y-_boardRect.y)/_cellSize);
            return _puzzle.Definition.IsInBounds(x,y)?_puzzle.Definition.ToIndex(x,y):-1;
        }

        private HouseEvaluation? FindHouse(int cell)
        {
            if(_evaluation==null) return null; foreach(var h in _evaluation.Houses) if(h.CellIndex==cell) return h; return null;
        }

        private static Rect Shrink(Rect r,float a)=>new Rect(r.x+a,r.y+a,Mathf.Max(0,r.width-a*2),Mathf.Max(0,r.height-a*2));
        private static void Overlay(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(Shrink(r,3f),Texture2D.whiteTexture);GUI.color=old;}
    }

    public static class PrototypePuzzleCatalog
    {
        private static readonly string[][] Levels=
        {
            new[]{"A"}, new[]{"1A1"}, new[]{"A2B"}, new[]{"#A#","B3C","###"}, new[]{"EEED1","E2DDD","EBBDD","ADDDC","DD3CC"}
        };
        public static int Count=>Levels.Length;
        public static PuzzleDefinition Get(int index)
        {
            if(index<0||index>=Levels.Length) throw new ArgumentOutOfRangeException(nameof(index));
            return Parse($"prototype-{index+1:00}",Levels[index]);
        }
        private static PuzzleDefinition Parse(string id,string[] rows)
        {
            if(rows==null||rows.Length==0) throw new ArgumentException("At least one row is required.",nameof(rows));
            var width=rows[0].Length; var cells=new List<CellDefinition>(width*rows.Length);
            for(var y=0;y<rows.Length;y++)
            {
                if(rows[y].Length!=width) throw new InvalidOperationException("Prototype rows must be rectangular.");
                for(var x=0;x<width;x++)
                {
                    var t=rows[y][x];
                    if(t>='A'&&t<='Z') cells.Add(CellDefinition.District(t-'A')); else if(t>='1'&&t<='3') cells.Add(CellDefinition.House((byte)(t-'0'))); else if(t=='#') cells.Add(CellDefinition.Wall()); else throw new InvalidOperationException($"Unsupported token '{t}'.");
                }
            }
            return new PuzzleDefinition(id,width,rows.Length,cells.ToArray());
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using Glowbound.Core;
using Glowbound.Core.Generation;
using Glowbound.Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Glowbound.EditorTools
{
    public sealed class PuzzleGeneratorWindow : EditorWindow
    {
        private enum BoardProfile { Board5x5, Board6x6, Board7x7, Board8x8 }

        private BoardProfile _profile = BoardProfile.Board5x5;
        private int _seed = 1;
        private GenerationResult _result;
        private EnumField _profileField;
        private IntegerField _seedField;
        private Toggle _solutionToggle;
        private Label _status;
        private Label _diagnostics;
        private VisualElement _preview;
        private Button _saveButton;

        [MenuItem("Glowbound/Puzzle Generator")]
        public static void Open() => GetWindow<PuzzleGeneratorWindow>("Glowbound Generator");

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = root.style.paddingRight = 12;
            root.style.paddingTop = root.style.paddingBottom = 10;

            var title = new Label("GLOWBOUND — Puzzle Generator");
            title.style.fontSize = 18;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            root.Add(title);
            root.Add(new Label("Generate unique editor candidates, preview them, then save accepted PuzzleAsset levels."));

            _profileField = new EnumField("Board profile", _profile);
            _profileField.RegisterValueChangedCallback(e => _profile = (BoardProfile)e.newValue);
            root.Add(_profileField);

            _seedField = new IntegerField("Seed") { value = _seed };
            _seedField.RegisterValueChangedCallback(e => _seed = e.newValue);
            root.Add(_seedField);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.marginTop = 6;
            buttons.Add(new Button(Generate) { text = "Generate / Regenerate" });
            buttons.Add(new Button(NextSeed) { text = "Next Seed" });
            _saveButton = new Button(SaveAccepted) { text = "Save Accepted Puzzle" };
            _saveButton.SetEnabled(false);
            buttons.Add(_saveButton);
            root.Add(buttons);

            _solutionToggle = new Toggle("Show planted solution") { value = true };
            _solutionToggle.RegisterValueChangedCallback(_ => RenderPreview());
            root.Add(_solutionToggle);

            _status = new Label("No puzzle generated yet.");
            _status.style.marginTop = 8;
            _status.style.unityFontStyleAndWeight = FontStyle.Bold;
            _diagnostics = new Label();
            root.Add(_status);
            root.Add(_diagnostics);

            var scroll = new ScrollView(ScrollViewMode.VerticalAndHorizontal);
            scroll.style.flexGrow = 1;
            scroll.style.marginTop = 8;
            _preview = new VisualElement();
            _preview.style.alignSelf = Align.Center;
            scroll.Add(_preview);
            root.Add(scroll);
        }

        private void Generate()
        {
            _seed = _seedField.value;
            _result = PuzzleGenerator.Generate(_seed, GetSettings(_profile));
            _saveButton.SetEnabled(_result.Success);

            if (_result.Success)
            {
                _status.text = $"UNIQUE candidate — seed {_seed} — {_result.Definition.Width}x{_result.Definition.Height}";
                _status.style.color = new Color(0.20f, 0.62f, 0.30f);
            }
            else
            {
                _status.text = "Generation failed: " + _result.FailureReason;
                _status.style.color = new Color(0.80f, 0.25f, 0.20f);
            }

            _diagnostics.text = BuildDiagnostics(_result);
            RenderPreview();
        }

        private void NextSeed()
        {
            _seedField.value++;
            Generate();
        }

        private static GenerationSettings GetSettings(BoardProfile profile)
        {
            switch (profile)
            {
                case BoardProfile.Board6x6: return GenerationProfiles.Board6x6();
                case BoardProfile.Board7x7: return GenerationProfiles.Board7x7();
                case BoardProfile.Board8x8: return GenerationProfiles.Board8x8();
                default: return GenerationProfiles.Board5x5();
            }
        }

        private static string BuildDiagnostics(GenerationResult result)
        {
            if (result == null) return string.Empty;
            var d = result.Diagnostics;
            var solve = result.SolveResult;
            return
                $"Attempts: {d.Attempts}   Multiple rejected: {d.RejectedMultipleSolutions}   " +
                $"Invalid/other rejected: {d.RejectedDistricts + d.RejectedSolutions + d.RejectedDefinitions + d.RejectedHouseCount + d.RejectedNoSolution}\n" +
                (solve == null
                    ? $"Last solver nodes: {d.LastSolverNodes}   backtracks: {d.LastSolverBacktracks}"
                    : $"Solutions: {solve.SolutionCount}   nodes: {solve.NodesVisited}   backtracks: {solve.Backtracks}   max depth: {solve.MaxDepth}") +
                "\nDifficulty: UNRATED (human-technique solver not implemented yet).";
        }

        private void RenderPreview()
        {
            if (_preview == null) return;
            _preview.Clear();
            if (_result == null || !_result.Success) return;

            var definition = _result.Definition;
            var solution = new HashSet<int>(_result.KnownSolutionLanternCells);
            var cellSize = Mathf.Clamp(480f / Mathf.Max(definition.Width, definition.Height), 44f, 78f);

            for (var y = 0; y < definition.Height; y++)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                _preview.Add(row);

                for (var x = 0; x < definition.Width; x++)
                {
                    var index = definition.ToIndex(x, y);
                    var cell = definition.Cells[index];
                    row.Add(CreateCell(cell, index, cellSize, solution));
                }
            }
        }

        private VisualElement CreateCell(CellDefinition cell, int index, float size, HashSet<int> solution)
        {
            var box = new VisualElement();
            box.style.width = size;
            box.style.height = size;
            box.style.marginRight = 2;
            box.style.marginBottom = 2;
            box.style.alignItems = Align.Center;
            box.style.justifyContent = Justify.Center;
            box.style.borderTopWidth = box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = box.style.borderRightWidth = 1;
            box.style.borderTopColor = box.style.borderBottomColor = new Color(0.22f, 0.20f, 0.18f, 0.5f);
            box.style.borderLeftColor = box.style.borderRightColor = new Color(0.22f, 0.20f, 0.18f, 0.5f);

            string text;
            if (cell.Kind == CellKind.District)
            {
                box.style.backgroundColor = DistrictColor(cell.DistrictId);
                text = _solutionToggle.value && solution.Contains(index) ? "●" : string.Empty;
            }
            else if (cell.Kind == CellKind.House)
            {
                box.style.backgroundColor = new Color(0.96f, 0.82f, 0.60f);
                text = cell.HouseTarget.ToString();
            }
            else
            {
                box.style.backgroundColor = new Color(0.25f, 0.48f, 0.24f);
                text = "■";
            }

            var label = new Label(text);
            label.style.fontSize = Mathf.RoundToInt(size * 0.42f);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.color = cell.Kind == CellKind.Wall ? Color.white : new Color(0.23f, 0.16f, 0.10f);
            box.Add(label);
            return box;
        }

        private static Color DistrictColor(int id)
        {
            var palette = new[]
            {
                new Color(1.00f, 0.67f, 0.72f), new Color(1.00f, 0.72f, 0.48f),
                new Color(0.98f, 0.87f, 0.48f), new Color(0.64f, 0.88f, 0.58f),
                new Color(0.50f, 0.88f, 0.84f), new Color(0.48f, 0.72f, 0.96f),
                new Color(0.61f, 0.58f, 0.94f), new Color(0.82f, 0.60f, 0.94f),
                new Color(0.94f, 0.56f, 0.78f), new Color(0.72f, 0.78f, 0.52f)
            };
            return palette[Math.Abs(id) % palette.Length];
        }

        private void SaveAccepted()
        {
            if (_result == null || !_result.Success) return;
            const string folder = "Assets/Glowbound/Levels/Generated";
            EnsureFolder(folder);

            var safeId = _result.Definition.Id.Replace(' ', '_');
            var path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{safeId}.asset");
            var asset = CreateInstance<PuzzleAsset>();
            asset.SetGeneratedData(
                _result.Definition, _result.Seed, _profile.ToString(),
                _result.Diagnostics.Attempts,
                _result.SolveResult?.NodesVisited ?? _result.Diagnostics.LastSolverNodes,
                _result.SolveResult?.Backtracks ?? _result.Diagnostics.LastSolverBacktracks,
                _result.KnownSolutionLanternCells);

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
            _status.text = "Saved: " + path;
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}

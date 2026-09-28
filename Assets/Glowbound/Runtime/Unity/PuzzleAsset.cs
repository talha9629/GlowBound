using Glowbound.Core;
using UnityEngine;

namespace Glowbound.Unity
{
    [CreateAssetMenu(
        fileName = "Puzzle",
        menuName = "Glowbound/Puzzle Definition",
        order = 0)]
    public sealed class PuzzleAsset : ScriptableObject
    {
        [SerializeField] private string puzzleId = string.Empty;
        [SerializeField, Min(1)] private int width = 5;
        [SerializeField, Min(1)] private int height = 5;
        [SerializeField] private CellDefinition[] cells = System.Array.Empty<CellDefinition>();

        public string PuzzleId
        {
            get { return puzzleId; }
        }

        public int Width
        {
            get { return width; }
        }

        public int Height
        {
            get { return height; }
        }

        public PuzzleDefinition CreateDefinition()
        {
            var clonedCells = cells == null
                ? System.Array.Empty<CellDefinition>()
                : (CellDefinition[])cells.Clone();

            return new PuzzleDefinition(
                puzzleId,
                width,
                height,
                clonedCells);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (width < 1) width = 1;
            if (height < 1) height = 1;

            var requiredLength = width * height;
            if (cells == null || cells.Length != requiredLength)
            {
                System.Array.Resize(ref cells, requiredLength);
            }
        }
#endif
    }
}

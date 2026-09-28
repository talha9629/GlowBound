using Glowbound.Core;
using UnityEngine;

namespace Glowbound.Unity.Prototype
{
    internal sealed class PrototypeArtSkin
    {
        private readonly Texture2D[] _tiles = new Texture2D[10];
        private readonly Texture2D _lantern;
        private readonly Texture2D _xMark;
        private readonly Texture2D _houseUnlit, _houseLit;
        private readonly Texture2D _houseTop, _houseRight, _houseBottom, _houseLeft;
        private readonly Texture2D _houseHorizontal, _houseVertical, _houseAll;
        private readonly Texture2D _wallUnlit, _wallLit;
        private readonly Texture2D _wallTop, _wallRight, _wallBottom, _wallLeft;
        private readonly Texture2D _wallHorizontal, _wallVertical, _wallAll;

        public PrototypeArtSkin()
        {
            for (var i = 0; i < _tiles.Length; i++)
                _tiles[i] = Load($"Tiles/Tile_{i + 1:00}");

            _lantern = Load("Lantern/Lantern");
            _xMark = Load("Marks/X_Mark");
            _houseUnlit = Load("Houses/House_Unlit");
            _houseLit = Load("Houses/House_Lit");
            _houseTop = Load("Houses/House_Light_Top");
            _houseRight = Load("Houses/House_Light_Right");
            _houseBottom = Load("Houses/House_Light_Bottom");
            _houseLeft = Load("Houses/House_Light_Left");
            _houseHorizontal = Load("Houses/House_Light_Horizontal");
            _houseVertical = Load("Houses/House_Light_Vertical");
            _houseAll = Load("Houses/House_Light_All");

            _wallUnlit = Load("Walls/Wall_Unlit");
            _wallLit = Load("Walls/Wall_Lit");
            _wallTop = Load("Walls/Wall_Light_Top");
            _wallRight = Load("Walls/Wall_Light_Right");
            _wallBottom = Load("Walls/Wall_Light_Bottom");
            _wallLeft = Load("Walls/Wall_Light_Left");
            _wallHorizontal = Load("Walls/Wall_Light_Horizontal");
            _wallVertical = Load("Walls/Wall_Light_Vertical");
            _wallAll = Load("Walls/Wall_Light_All");
        }

        public Texture2D Tile(int districtId) => _tiles[Mathf.Abs(districtId) % _tiles.Length];
        public Texture2D Lantern => _lantern;
        public Texture2D XMark => _xMark;

        public Texture2D House(LightDirectionMask mask)
        {
            if (mask == LightDirectionMask.None) return _houseUnlit;
            if (mask == LightDirectionMask.Up) return _houseTop;
            if (mask == LightDirectionMask.Right) return _houseRight;
            if (mask == LightDirectionMask.Down) return _houseBottom;
            if (mask == LightDirectionMask.Left) return _houseLeft;
            if (mask == (LightDirectionMask.Left | LightDirectionMask.Right)) return _houseHorizontal;
            if (mask == (LightDirectionMask.Up | LightDirectionMask.Down)) return _houseVertical;
            if (mask == (LightDirectionMask.Up | LightDirectionMask.Right | LightDirectionMask.Down | LightDirectionMask.Left)) return _houseAll;
            return _houseLit;
        }

        public Texture2D Wall(LightDirectionMask mask)
        {
            if (mask == LightDirectionMask.None) return _wallUnlit;
            if (mask == LightDirectionMask.Up) return _wallTop;
            if (mask == LightDirectionMask.Right) return _wallRight;
            if (mask == LightDirectionMask.Down) return _wallBottom;
            if (mask == LightDirectionMask.Left) return _wallLeft;
            if (mask == (LightDirectionMask.Left | LightDirectionMask.Right)) return _wallHorizontal;
            if (mask == (LightDirectionMask.Up | LightDirectionMask.Down)) return _wallVertical;
            if (mask == (LightDirectionMask.Up | LightDirectionMask.Right | LightDirectionMask.Down | LightDirectionMask.Left)) return _wallAll;
            return _wallLit;
        }

        private static Texture2D Load(string relativePath)
        {
            return Resources.Load<Texture2D>($"GlowboundPrototype/{relativePath}");
        }
    }
}

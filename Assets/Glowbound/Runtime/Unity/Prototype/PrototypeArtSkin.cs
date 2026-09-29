using Glowbound.Core;
using UnityEngine;

namespace Glowbound.Unity.Prototype
{
    internal sealed class PrototypeArtSkin
    {
        private readonly Sprite[] _tiles = new Sprite[10];
        private readonly Sprite _lantern;
        private readonly Sprite _xMark;
        private readonly Sprite _houseUnlit, _houseLit;
        private readonly Sprite _houseTop, _houseRight, _houseBottom, _houseLeft;
        private readonly Sprite _houseHorizontal, _houseVertical, _houseAll;
        private readonly Sprite _wallUnlit, _wallLit;
        private readonly Sprite _wallTop, _wallRight, _wallBottom, _wallLeft;
        private readonly Sprite _wallHorizontal, _wallVertical, _wallAll;

        public PrototypeArtSkin()
        {
            for (var i = 0; i < _tiles.Length; i++)
                _tiles[i] = Load($"Tiles/Tile_{i + 1:00}");

            _lantern = Load("Lantern/Lantern");
            _xMark = Load("Marks/X_Mark");
            _houseUnlit = Load("Houses/House_Unlit");
            _houseLit = Load("Houses/House_Lit");
            _houseTop = Load("Houses/House_Light_Top");
            _houseRight = Load("Houses/House_Light_Right");            _houseBottom = Load("Houses/House_Light_Bottom");
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

        public Sprite Tile(int districtId) => _tiles[Mathf.Abs(districtId) % _tiles.Length];
        public Sprite Lantern => _lantern;
        public Sprite XMark => _xMark;

        public Sprite House(LightDirectionMask mask)
        {
            if (mask == LightDirectionMask.None) return _houseUnlit;
            if (mask == LightDirectionMask.Up) return _houseTop;
            if (mask == LightDirectionMask.Right) return _houseRight;            if (mask == LightDirectionMask.Down) return _houseBottom;
            if (mask == LightDirectionMask.Left) return _houseLeft;
            if (mask == (LightDirectionMask.Left | LightDirectionMask.Right)) return _houseHorizontal;
            if (mask == (LightDirectionMask.Up | LightDirectionMask.Down)) return _houseVertical;
            if (mask == (LightDirectionMask.Up | LightDirectionMask.Right | LightDirectionMask.Down | LightDirectionMask.Left)) return _houseAll;
            return _houseLit;
        }

        public Sprite Wall(LightDirectionMask mask)
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

        private static Sprite Load(string relativePath)
        {
            var sprite = Resources.Load<Sprite>($"GlowboundPrototype/{relativePath}");
            if (sprite == null) Debug.LogError($"Missing Glowbound sprite: {relativePath}");
            return sprite;
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Glowbound.Unity.Prototype
{
    internal sealed class PrototypeCellView : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public int CellIndex { get; private set; }
        public Image BaseImage { get; private set; }
        public Image BeamImage { get; private set; }
        public Image MarkImage { get; private set; }
        public Text NumberText { get; private set; }
        private GlowboundPrototypeController _controller;

        public void Initialize(GlowboundPrototypeController controller, int index, Image baseImage, Image beamImage, Image markImage, Text numberText)
        {
            _controller = controller;
            CellIndex = index;
            BaseImage = baseImage;
            BeamImage = beamImage;
            MarkImage = markImage;
            NumberText = numberText;
        }

        public void OnPointerDown(PointerEventData eventData) => _controller?.OnCellPointerDown(CellIndex);
        public void OnPointerUp(PointerEventData eventData) => _controller?.OnCellPointerUp(CellIndex);
        public void OnPointerExit(PointerEventData eventData) => _controller?.OnCellPointerExit(CellIndex);
    }
}

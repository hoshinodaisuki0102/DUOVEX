using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace DualSync
{
    /// <summary>Small, editable menu hover/selection visual state. No gameplay timing changes.</summary>
    [DisallowMultipleComponent]
    public sealed class DuovexMenuButtonSkin86 : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        public Sprite idleSprite;
        public Sprite hoverSprite;
        public bool isPrimary;
        public Text title, index, arrow;
        Image graphic;
        bool hovered, selected, pressed;
        void Awake() { graphic=GetComponent<Image>(); Apply(); }
        void OnEnable() { hovered=pressed=selected=false; Apply(); }
        void OnDisable() { hovered=pressed=selected=false; Apply(); }
        void Apply()
        {
            if(graphic==null)graphic=GetComponent<Image>();
            bool active=hovered||selected||pressed;
            if(graphic!=null)graphic.sprite=active && hoverSprite!=null?hoverSprite:idleSprite;
            // Primary button has dark lettering until hovered; other states are light.
            if(title!=null)title.color=isPrimary&&!active
                ?new Color(.08f,.06f,.14f):new Color(.98f,.97f,1f);
            if(index!=null)index.color=isPrimary&&!active
                ?new Color(.40f,.23f,.64f):new Color(.79f,.59f,1f);
            if(arrow!=null)arrow.color=isPrimary&&!active
                ?new Color(.38f,.21f,.57f):Color.white;
        }
        public void OnPointerEnter(PointerEventData eventData) {hovered=true;Apply();}
        public void OnPointerExit(PointerEventData eventData) {hovered=false;pressed=false;Apply();}
        public void OnPointerDown(PointerEventData eventData) {if(eventData.button==PointerEventData.InputButton.Left){pressed=true;Apply();}}
        public void OnPointerUp(PointerEventData eventData) {pressed=false;Apply();}
        public void OnSelect(BaseEventData eventData) {selected=true;Apply();}
        public void OnDeselect(BaseEventData eventData) {selected=false;pressed=false;Apply();}
    }
}

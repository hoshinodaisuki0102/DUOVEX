using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace DualSync
{
    /// <summary>
    /// Unscaled-time motion for keyboard/gamepad and pointer events.
    /// Motion never changes Button.onClick or gameplay timing.
    /// Each object is editable in the saved Unity scene.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DuovexButtonMotion : MonoBehaviour,
        IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler,
        ISelectHandler, IDeselectHandler
    {
        public float hoverOffset = 8f;
        public float hoverScale = 1.025f;
        public float pressScale = .963f;
        public float responseSpeed = 18f;
        RectTransform rect;
        Vector2 originalPosition;
        Vector3 originalScale;
        bool hovered, pressed, selected;
        bool started;
        Image accent;
        float accentAlpha;

        void Awake() { Initialize(); }
        void Initialize()
        {
            if(started) return;
            rect=transform as RectTransform;
            if(rect==null) return;
            originalPosition=rect.anchoredPosition;
            originalScale=rect.localScale;
            Transform node=transform.Find(gameObject.name+"_MotionAccent");
            accent=node!=null?node.GetComponent<Image>():null;
            started=true;
        }
        void OnEnable()
        {
            Initialize();
            hovered=pressed=selected=false;
            if(rect!=null) { rect.anchoredPosition=originalPosition;rect.localScale=originalScale; }
        }
        void OnDisable()
        {
            if(rect!=null && started) {rect.anchoredPosition=originalPosition;rect.localScale=originalScale;}
        }
        void Update()
        {
            if(!started || rect==null) return;
            if(accent==null)
            {
                Transform node=transform.Find(gameObject.name+"_MotionAccent");
                if(node!=null)accent=node.GetComponent<Image>();
            }
            Button b=GetComponent<Button>();
            bool usable=b==null || b.interactable;
            float slide=usable && (hovered||selected)?hoverOffset:0f;
            float scale=usable?(pressed?pressScale:(hovered||selected?hoverScale:1f)):1f;
            float blend=1f-Mathf.Exp(-responseSpeed*Time.unscaledDeltaTime);
            rect.anchoredPosition=Vector2.Lerp(rect.anchoredPosition,originalPosition+Vector2.right*slide,blend);
            rect.localScale=Vector3.Lerp(rect.localScale,originalScale*scale,blend);
            if(accent!=null)
            {
                accentAlpha=Mathf.Lerp(accentAlpha,usable&&(pressed||hovered||selected)?(pressed?1f:.82f):0f,blend);
                Color c=accent.color;c.a=accentAlpha;accent.color=c;
            }
        }
        public void OnPointerEnter(PointerEventData e) { hovered=true; }
        public void OnPointerExit(PointerEventData e) { hovered=false; pressed=false; }
        public void OnPointerDown(PointerEventData e) { if(e.button==PointerEventData.InputButton.Left)pressed=true; }
        public void OnPointerUp(PointerEventData e) { pressed=false; }
        public void OnSelect(BaseEventData e) { selected=true; }
        public void OnDeselect(BaseEventData e) { selected=false; pressed=false; }
    }
}

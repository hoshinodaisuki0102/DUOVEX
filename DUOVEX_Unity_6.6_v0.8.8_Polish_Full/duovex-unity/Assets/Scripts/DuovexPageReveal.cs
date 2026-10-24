using UnityEngine;

namespace DualSync
{
    /// <summary>Short screen-entry fade, independent of DSP and gameplay time.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class DuovexPageReveal : MonoBehaviour
    {
        public float duration=.24f;
        CanvasGroup group;
        float elapsed;
        void Awake() { group=GetComponent<CanvasGroup>(); }
        void OnEnable()
        {
            if(group==null)group=GetComponent<CanvasGroup>();
            elapsed=0f;
            group.alpha=0f;
            group.interactable=false;
            group.blocksRaycasts=false;
        }
        void Update()
        {
            if(group==null)return;
            elapsed+=Time.unscaledDeltaTime;
            float t=Mathf.Clamp01(elapsed/Mathf.Max(.01f,duration));
            // Smoothstep avoids abrupt first/last animation frames.
            group.alpha=t*t*(3f-2f*t);
            if(t>=1f)
            {
                group.interactable=true;
                group.blocksRaycasts=true;
                // Keep enabled to restart OnEnable when the page is reopened.
            }
        }
        void OnDisable()
        {
            if(group==null)return;
            // Called when disabled at fade completion or page becomes hidden.
            group.alpha=1f;group.interactable=true;group.blocksRaycasts=true;
        }
    }
}

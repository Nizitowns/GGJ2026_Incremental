using UnityEngine;
using UnityEngine.EventSystems;

namespace HolenderGames.WizardDemo
{
    /// <summary>
    /// shows tooltip on hover
    /// </summary>
    public class TooltipHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        public GameObject tooltip;
        public void OnPointerEnter(PointerEventData eventData)
        {
            tooltip.gameObject.SetActive(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltip.gameObject.SetActive(false);
        }
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Linq;

public class TouchArea : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public int ButtonId;

    UIBehaviour ui_;
    int pointerId_ = -99;

    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.LogError($"Enter {eventData.pointerId}");
        pointerId_ = eventData.pointerId;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.LogError($"Exit {eventData.pointerId}");
        pointerId_ = -99;
    }

    void Start()
    {
        ui_ = GetComponent<UIBehaviour>();
    }

    public bool IsPushed()
    {
        if (pointerId_ == -99)
        {
            return false;
        }
        var finger = Input.touches.Any(t => t.fingerId == pointerId_ );
        var mouse = Input.GetMouseButton(0) && (pointerId_ == -1);
        return finger | mouse;
    }

}

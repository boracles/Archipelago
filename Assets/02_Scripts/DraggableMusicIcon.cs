using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableMusicIcon : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int clipIndex;
    public MusicDragPlacer placer;
    public Image icon;

    RectTransform ghost;
    Canvas rootCanvas;

    public void OnBeginDrag(PointerEventData e)
    {
        rootCanvas = GetComponentInParent<Canvas>();
        ghost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image))
                .GetComponent<RectTransform>();
        ghost.SetParent(rootCanvas.transform, false);
        ghost.sizeDelta = (transform as RectTransform).rect.size;
        var img = ghost.GetComponent<Image>();
        img.sprite = icon ? icon.sprite : GetComponent<Image>()?.sprite;
        img.raycastTarget = false;
        ghost.position = e.position;
        ghost.GetComponent<CanvasGroup>().alpha = 0.7f;
    }

    public void OnDrag(PointerEventData e)
    {
        if (ghost) ghost.position = e.position;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (ghost) Destroy(ghost.gameObject);
        placer?.TryPlace(e.position, clipIndex);
    }
}

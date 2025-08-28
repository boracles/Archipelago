using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggableMusicIcon : MonoBehaviour,
  IBeginDragHandler, IDragHandler, IEndDragHandler {

  public int clipIndex;                 // 이 아이콘이 배치할 클립 인덱스
  public MusicDragPlacer placer;        // Panel_MusicBox의 MusicDragPlacer
  public Image icon;                    // 아이콘 스프라이트(선택)

  RectTransform _dragGhost;
  Canvas _rootCanvas;

  public void OnBeginDrag(PointerEventData e) {
    if (!placer) return;
    _rootCanvas = GetComponentInParent<Canvas>();

    // UI에 따라다니는 고스트 생성
    _dragGhost = new GameObject("DragGhost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image))
      .GetComponent<RectTransform>();
    _dragGhost.SetParent(_rootCanvas.transform, false);
    _dragGhost.sizeDelta = (transform as RectTransform).rect.size;
    var img = _dragGhost.GetComponent<Image>();
    img.sprite = icon ? icon.sprite : GetComponent<Image>()?.sprite;
    img.raycastTarget = false; // 드래그 동안 UI 막지 않게
    _dragGhost.position = e.position;
    var cg = _dragGhost.GetComponent<CanvasGroup>(); cg.blocksRaycasts = false; cg.alpha = 0.7f;

    placer.BeginPreview();
    placer.UpdatePreview(e.position);
  }

  public void OnDrag(PointerEventData e) {
    if (_dragGhost) _dragGhost.position = e.position;
    placer?.UpdatePreview(e.position);
  }

  public void OnEndDrag(PointerEventData e) {
    if (_dragGhost) Destroy(_dragGhost.gameObject);
    placer?.EndPreview();
    placer?.TryPlace(e.position, clipIndex);
  }
}

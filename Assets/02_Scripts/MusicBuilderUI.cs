using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MusicBuilderUI : MonoBehaviour {
  [Header("Refs")]
  [SerializeField] Toggle buildToggle;        // ← Toggle_BuildMode 드래그
  [SerializeField] GameRunner gameRunner;     // ← GameRunner 드래그
  [SerializeField] Camera cam;                // ← 보통 Main Camera
  [SerializeField] LayerMask placeMask;       // ← 비워두면 Default 전부
  [SerializeField] float yOffset = 0.02f;     // ← 살짝 떠 있게

  [Header("Sound")]
  [SerializeField] int clipIndex = 0;         // 일단 0번 클립 사용

  void Awake() {
    if (!cam) cam = Camera.main;
    if (!gameRunner) gameRunner = FindObjectOfType<GameRunner>(true);
  }

  void Update() {
    if (!buildToggle || !buildToggle.isOn) return;

    // UI 위 터치/클릭은 무시
    if (IsPointerOverUI()) return;

    // 마우스(에디터/PC)
    if (Input.GetMouseButtonDown(0)) TryPlaceAtScreen(Input.mousePosition);

    // 터치(iOS/안드로이드)
    for (int i=0; i<Input.touchCount; i++) {
      var t = Input.GetTouch(i);
      if (t.phase == TouchPhase.Began && !EventSystem.current.IsPointerOverGameObject(t.fingerId))
        TryPlaceAtScreen(t.position);
    }
  }

  bool IsPointerOverUI() {
    if (!EventSystem.current) return false;
    if (Input.touchCount > 0)
      return EventSystem.current.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
    return EventSystem.current.IsPointerOverGameObject();
  }

  void TryPlaceAtScreen(Vector2 screenPos) {
    if (!cam) return;
    var ray = cam.ScreenPointToRay(screenPos);
    int mask = placeMask == 0 ? Physics.DefaultRaycastLayers : placeMask.value;
    if (Physics.Raycast(ray, out var hit, 200f, mask, QueryTriggerInteraction.Ignore)) {
      var pos = hit.point + Vector3.up * yOffset;
      gameRunner?.TrySpawnMusicTile(pos, clipIndex);
    }
  }

  // (옵션) 버튼 추가하면 이 메서드들 연결
  public void Undo()     => gameRunner?.UndoLastMusicTile();
  public void ClearAll() => gameRunner?.ClearAllMusicTiles();
}

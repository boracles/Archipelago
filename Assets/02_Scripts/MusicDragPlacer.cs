using UnityEngine;

public class MusicDragPlacer : MonoBehaviour {
  [Header("Refs")]
  public GameRunner gameRunner;   // GameRunner 드래그
  public Camera cam;              // Main Camera
  public LayerMask placeMask;     // ISLAND | TILE 등

  [Header("Placement")]
  public bool snapToGrid = true;
  public float grid = 1f;
  public float tileHalfHeight = 0.1f; // 타일 BoxCollider Y/2 (예: Y=0.2면 0.1)
  public float yOffset = 0.01f;       // 살짝 띄우기

  [Header("Preview (optional)")]
  public GameObject worldPreviewPrefab; // 반투명 큐브 프리팹(선택)
  GameObject _preview;

  void Awake() { if (!cam) cam = Camera.main; }

  public void BeginPreview() {
    if (worldPreviewPrefab && !_preview) _preview = Instantiate(worldPreviewPrefab);
    if (_preview) _preview.SetActive(true);
  }
  public void UpdatePreview(Vector2 screenPos) {
    if (_preview && TryGetWorld(screenPos, out var pos)) _preview.transform.position = pos;
  }
  public void EndPreview() { if (_preview) _preview.SetActive(false); }

  public bool TryPlace(Vector2 screenPos, int clipIndex) {
    if (!gameRunner) return false;
    if (!TryGetWorld(screenPos, out var pos)) return false;
    return gameRunner.TrySpawnMusicTile(pos, clipIndex);
  }

  bool TryGetWorld(Vector2 screenPos, out Vector3 pos) {
    pos = default;
    if (!cam) return false;
    var ray = cam.ScreenPointToRay(screenPos);
    int mask = placeMask == 0 ? Physics.DefaultRaycastLayers : placeMask.value;
    if (Physics.Raycast(ray, out var hit, 200f, mask, QueryTriggerInteraction.Ignore)) {
      var p = hit.point;
      if (snapToGrid) { p.x = Mathf.Round(p.x / grid) * grid; p.z = Mathf.Round(p.z / grid) * grid; }
      p.y = hit.point.y + tileHalfHeight + yOffset;
      pos = p; return true;
    }
    return false;
  }
}

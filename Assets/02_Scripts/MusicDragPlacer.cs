using UnityEngine;

public class MusicDragPlacer : MonoBehaviour {
  public enum Mode { HorizontalPlane, SurfaceRaycast }
  public Mode mode = Mode.HorizontalPlane;

  [Header("Refs")]
  public GameRunner gameRunner;
  public Camera cam;
  public Transform island;
  public Renderer islandRenderer;

  [Header("Raycast (SurfaceRaycast 모드)")]
  public LayerMask placeMask = ~0;

  [Header("Placement")]
  public bool  snapToGrid = true;
  public float grid = 1f;
  public bool  gridRelativeToIsland = true;
  public float yOffset = 0f; // ← 완전 같은 레벨이면 0 권장

  void Awake() { if (!cam) cam = Camera.main; }

  // 섬 '중심 높이'
  float GroundCenterY() =>
    islandRenderer ? islandRenderer.bounds.center.y :
    island         ? island.position.y : 0f;

  public bool TryPlace(Vector2 screenPos, int clipIndex) {
    if (!cam || !gameRunner) return false;
    return (mode == Mode.HorizontalPlane)
      ? PlaceOnPlane(screenPos, clipIndex)
      : PlaceByRaycast(screenPos, clipIndex);
  }

  // 섬 중심 높이의 수평면과 교차
  bool PlaceOnPlane(Vector2 screenPos, int clipIndex) {
    var ray   = cam.ScreenPointToRay(screenPos);
    float y   = GroundCenterY();
    var plane = new Plane(Vector3.up, new Vector3(0, y, 0));
    if (!plane.Raycast(ray, out var t)) return false;

    var p = SnapXZ(ray.GetPoint(t));
    p.y   = y + yOffset;                       // ★ 중심 높이에 고정
    var ok = gameRunner.TrySpawnMusicTile(p, clipIndex);
    Debug.Log($"[PLACER] planeY={y:F2} pos={p} ok={ok}");
    return ok;
  }

  // 표면을 맞췄더라도 최종 y는 '섬 중심 높이'로 통일
  bool PlaceByRaycast(Vector2 screenPos, int clipIndex) {
    var ray = cam.ScreenPointToRay(screenPos);
    if (!Physics.Raycast(ray, out var hit, 150f, placeMask, QueryTriggerInteraction.Collide))
      return false;

    var p = SnapXZ(hit.point);
    p.y   = GroundCenterY() + yOffset;        // ★ 여기서도 동일 규칙
    var ok = gameRunner.TrySpawnMusicTile(p, clipIndex);
    Debug.Log($"[PLACER] hit={hit.collider.name} pos={p} ok={ok}");
    return ok;
  }

  Vector3 SnapXZ(Vector3 p) {
    if (!snapToGrid) return p;
    float ox = (gridRelativeToIsland && island) ? island.position.x : 0f;
    float oz = (gridRelativeToIsland && island) ? island.position.z : 0f;
    p.x = Mathf.Round((p.x - ox) / grid) * grid + ox;
    p.z = Mathf.Round((p.z - oz) / grid) * grid + oz;
    return p;
  }

  void OnDrawGizmosSelected() {
    if (mode != Mode.HorizontalPlane) return;
    float y = islandRenderer ? islandRenderer.bounds.center.y
              : island ? island.position.y : 0f;
    Gizmos.color = new Color(0, 1, 0, 0.15f);
    Gizmos.DrawCube(new Vector3(0, y, 0), new Vector3(100, 0.001f, 100));
  }

  // 프리뷰 미사용이면 비워둬도 OK
  public void BeginPreview() {}
  public void UpdatePreview(Vector2 _) {}
  public void EndPreview() {}
}

using System;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameRunner : MonoBehaviour, INetworkRunnerCallbacks {
  public NetworkPrefabRef playerPrefab;

  [SerializeField] private Transform island;
  [SerializeField] private Renderer islandRenderer;   // ← 인스펙터에 섬 큐브 Renderer 드래그
  [SerializeField] private float spawnYOffset = 1.05f;
  [SerializeField] private bool autoStart = false;

  private NetworkRunner runner;
  private NetworkObject _localPlayer;
  private Vector3? _savedSpawn = null;
  private Color?   _savedPlayerColor = null;
  private Color?   _savedIslandColor = null;

  public NetworkPrefabRef musicTilePrefab;   // 인스펙터에 등록
  private readonly System.Collections.Generic.List<NetworkObject> _musicTiles =
  new System.Collections.Generic.List<NetworkObject>();

  // 클래스 안 어디든(필드 영역) 추가
static readonly int PROP_BASE_COLOR = Shader.PropertyToID("_BaseColor");
static readonly int PROP_COLOR      = Shader.PropertyToID("_Color");

// GameRunner.cs 안에 있던 함수 교체
static Color GetRendererColor(Renderer r) {
  if (!r) return Color.white;
  // 런타임에는 인스턴스 머티리얼에서 직접 읽기
  var m = Application.isPlaying ? r.material : r.sharedMaterial;
  if (!m) return Color.white;
  if (m.HasProperty(PROP_BASE_COLOR)) return m.GetColor(PROP_BASE_COLOR);
  if (m.HasProperty(PROP_COLOR))      return m.GetColor(PROP_COLOR);
  return m.color;
}

static void SetRendererColor(Renderer r, Color c) {
  if (!r) return;

  // renderer.material 접근 시 Unity가 필요하면 1회만 인스턴싱해 줍니다.
  var m = Application.isPlaying ? r.material : r.sharedMaterial;

  if (m.HasProperty(PROP_BASE_COLOR)) m.SetColor(PROP_BASE_COLOR, c);
  else if (m.HasProperty(PROP_COLOR)) m.SetColor(PROP_COLOR, c);
  else m.color = c;
}

void Awake() {
  if (!island) island = GameObject.FindWithTag("Island")?.transform; // Island 태그 달아두기
  if (!islandRenderer && island) islandRenderer = island.GetComponentInChildren<Renderer>();
}

  async void Start() {
    if (autoStart)
      await BeginWithUser(GetOrCreateUserId());
  }

public async System.Threading.Tasks.Task BeginWithUser(string uid) {
  if (runner && runner.IsRunning) return;

  runner ??= gameObject.AddComponent<NetworkRunner>();
  runner.ProvideInput = true;
  runner.AddCallbacks(this);

  var sceneMgr = GetComponent<NetworkSceneManagerDefault>()
              ?? gameObject.AddComponent<NetworkSceneManagerDefault>();

  string sessionName = MakeIslandSession(uid);

  var result = await runner.StartGame(new StartGameArgs {
    GameMode     = GameMode.Host,
    SessionName  = sessionName,
    Scene        = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
    SceneManager = sceneMgr
  });

  Debug.Log(result.Ok
    ? $"[Fusion] Start OK (Host) session='{sessionName}'"
    : $"[Fusion] Start FAILED: {result.ShutdownReason}");
}

private static string MakeIslandSession(string userId) {
  using var sha1 = SHA1.Create();
  var hex = BitConverter.ToString(sha1.ComputeHash(Encoding.UTF8.GetBytes(userId)))
                        .Replace("-", "").ToLowerInvariant();
  return "island_" + hex.Substring(0, 12);
}

  private string GetOrCreateUserId() {
    const string key = "DEMO_USER_ID";
    if (!PlayerPrefs.HasKey(key))
      PlayerPrefs.SetString(key, Guid.NewGuid().ToString("N"));
    return PlayerPrefs.GetString(key);
  }

private Vector3 GetIslandSpawnPoint() {
  // 섬 Transform
  var t = island ? island : GameObject.FindWithTag("Island")?.transform;
  if (!t) return new Vector3(0, 2f, 0);

  // 섬의 콜라이더/렌더러와 바운즈
  var col  = t.GetComponentInChildren<Collider>();
  var rend = islandRenderer ? islandRenderer : t.GetComponentInChildren<Renderer>();
  var bounds = col ? col.bounds : (rend ? rend.bounds : new Bounds(t.position, t.localScale));

  // 섬 윗면 높이와 중앙 XZ
  float   topY    = bounds.max.y;
  Vector3 centerXZ = new Vector3(bounds.center.x, 0f, bounds.center.z);

  // 저장된 스폰이 있으면 XZ는 섬 AABB 안으로 클램프해서 사용 (Y는 항상 섬 윗면 + 오프셋)
  if (_savedSpawn.HasValue) {
    var s    = _savedSpawn.Value;
    var half = new Vector2(bounds.extents.x, bounds.extents.z);
    var cen  = new Vector2(bounds.center.x, bounds.center.z);

    float x = Mathf.Clamp(s.x, cen.x - half.x, cen.x + half.x);
    float z = Mathf.Clamp(s.z, cen.y - half.y, cen.y + half.y);
    return new Vector3(x, topY + spawnYOffset, z);
  }

  // 저장값 없으면 섬 중앙 위
  return new Vector3(centerXZ.x, topY + spawnYOffset, centerXZ.z);
}

  public void OnPlayerJoined(NetworkRunner r, PlayerRef player) {
    if (!r.IsServer) return;
    var pos = GetIslandSpawnPoint();
    var obj = r.Spawn(playerPrefab, pos, Quaternion.identity, player);
    if (player == r.LocalPlayer)
  _localPlayer = obj;

    StartCoroutine(SnapToGroundNextFrame(obj));

   if (_savedPlayerColor.HasValue && obj) {
  var rend = obj.GetComponentInChildren<Renderer>();
  if (rend) SetRendererColor(rend, _savedPlayerColor.Value);
}
  }

// --- 저장용 현재 상태 조회 ---
  public Vector3 GetCurrentPlayerPosition() =>
    _localPlayer ? _localPlayer.transform.position : GetIslandSpawnPoint();

public Color GetCurrentPlayerColor() {
  if (_localPlayer) {
    var r = _localPlayer.GetComponentInChildren<Renderer>();
    if (r) return GetRendererColor(r);
  }
  return _savedPlayerColor ?? Color.white;
}

public Color GetCurrentIslandColor() {
  return islandRenderer ? GetRendererColor(islandRenderer)
                        : _savedIslandColor ?? Color.white;
}

  public void OnInput(NetworkRunner r, NetworkInput input) {
    input.Set(new NetInput {
      move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
    });
  }

// 저장된 스폰 지정
public void SetSpawnOverride(Vector3 pos) {
  _savedSpawn = pos;
}

// 플레이어/섬 색 적용 (FIX: 'obj' -> '_localPlayer')
public void ApplyAppearance(Color playerColor, Color islandColor) {
  _savedPlayerColor = playerColor;
  _savedIslandColor = islandColor;

  // 섬 색 즉시 반영
  if (islandRenderer)
    SetRendererColor(islandRenderer, islandColor);

  // 플레이어가 이미 스폰되어 있으면 즉시 반영
  if (_localPlayer) {
    var rend = _localPlayer.GetComponentInChildren<Renderer>();
    if (rend) SetRendererColor(rend, playerColor);
  }
}

private System.Collections.IEnumerator SnapToGroundNextFrame(NetworkObject obj) {
  // 물리 한 틱 기다려서 콜라이더/씬 셋업 다 끝난 다음 고정
  yield return new WaitForFixedUpdate();
  if (!obj) yield break;

  var cc = obj.GetComponent<CharacterController>();
  float half = cc ? Mathf.Max(cc.height * 0.5f, cc.radius) : 1f;

  // 섬 윗면 Y
  float topY = 0f;
  if (islandRenderer)          topY = islandRenderer.bounds.max.y;
  else if (island) {
    var col = island.GetComponentInChildren<Collider>();
    topY = col ? col.bounds.max.y : island.position.y;
  }

  // ✅ CC 잠시 비활성 → 정확한 Y로 못박기 → 재활성 + 미량 하강 Move로 접지 확정
  if (cc) cc.enabled = false;

  var p = obj.transform.position;
  obj.transform.position = new Vector3(p.x, topY + half + 0.02f, p.z);

  if (cc) {
    cc.enabled = true;
    cc.Move(Vector3.down * 0.01f);   // 첫 프레임에 Grounded 확정
  }

  // 레이어 충돌 디버그
  int pl = obj.gameObject.layer;
  int il = island ? island.gameObject.layer : 0;
  Debug.Log($"[SPAWN] topY={topY:F2}, half={half:F2}, finalY={topY+half+0.02f:F2}, " +
            $"playerLayer={LayerMask.LayerToName(pl)}, islandLayer={LayerMask.LayerToName(il)}, " +
            $"collide={!Physics.GetIgnoreLayerCollision(pl,il)}");
}

// 길 생성
private void SpawnMusicPath(string uid) {
  if (!runner || !runner.IsServer) return;
  CleanupMusicPath();

  var start = GetIslandSpawnPoint();
  var end   = start + new Vector3(6f, 0f, 6f);  // ← 원하는 방향/길이로 조절

  // 직선 경로로 MusicTile 스폰 (PathUtils는 별도 파일)
  var tiles = PathUtils.SpawnStraightMusicPath(
      runner, musicTilePrefab, start, end, 0.9f, 0.02f);

  _musicTiles.AddRange(tiles);

  // uid 기반 고정 시드로 타일마다 다른 클립 고정
  int seed = MakeDeterministicSeed(uid);
  for (int i = 0; i < _musicTiles.Count; i++) {
    var mt = _musicTiles[i]?.GetComponent<MusicTile>();
    if (mt) mt.SetClipIndexServer(seed + i);
  }
}

// 정리
private void CleanupMusicPath() {
  if (!runner || !runner.IsServer) return;
  foreach (var t in _musicTiles) if (t) runner.Despawn(t);
  _musicTiles.Clear();
}

// uid → 결정적 시드
private static int MakeDeterministicSeed(string uid) {
  using var sha1 = SHA1.Create();
  var h = sha1.ComputeHash(Encoding.UTF8.GetBytes(uid));
  return BitConverter.ToInt32(h, 0);
}

public bool TrySpawnMusicTile(Vector3 pos, int clipIndex) {
  if (runner == null || !runner.IsRunning) {
    Debug.LogWarning("[GameRunner] Runner가 아직 시작되지 않았습니다.");
    return false;
  }

  var nob  = runner.Spawn(musicTilePrefab, pos, Quaternion.identity);
  var tile = nob.GetComponent<MusicTile>();
  if (tile) tile.Init(clipIndex);

  _musicTiles.Add(nob); // 리스트 관리 중이면 추가
  return true;
}

// 마지막 타일 되돌리기(1개 삭제)
public void UndoLastMusicTile() {
  if (!runner || !runner.IsServer || _musicTiles.Count == 0) return;
  var last = _musicTiles[_musicTiles.Count - 1];
  if (last) runner.Despawn(last);
  _musicTiles.RemoveAt(_musicTiles.Count - 1);
}

// 전부 삭제
public void ClearAllMusicTiles() {
  // 내부에서 Despawn까지 해주는 네 기존 메서드가 있다면 그걸 호출해도 OK
  if (!runner || !runner.IsServer) return;
  foreach (var t in _musicTiles) if (t) runner.Despawn(t);
  _musicTiles.Clear();
}


  public void OnPlayerLeft(NetworkRunner r, PlayerRef p) {}
  public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput input) {}
  public void OnShutdown(NetworkRunner r, ShutdownReason s) {
  CleanupMusicPath();
}
  public void OnConnectedToServer(NetworkRunner r) {}
  public void OnDisconnectedFromServer(NetworkRunner r, NetDisconnectReason reason) {}
  public void OnConnectRequest(NetworkRunner r, NetworkRunnerCallbackArgs.ConnectRequest req, byte[] token) {}
  public void OnConnectFailed(NetworkRunner r, NetAddress remote, NetConnectFailedReason reason) {}
  public void OnUserSimulationMessage(NetworkRunner r, SimulationMessagePtr msg) {}
  public void OnSessionListUpdated(NetworkRunner r, List<SessionInfo> list) {}
  public void OnCustomAuthenticationResponse(NetworkRunner r, Dictionary<string, object> data) {}
  public void OnHostMigration(NetworkRunner r, HostMigrationToken token) {}
  public void OnReliableDataReceived(NetworkRunner r, PlayerRef p, ReliableKey k, System.ArraySegment<byte> d) {}
  public void OnReliableDataProgress(NetworkRunner r, PlayerRef p, ReliableKey k, float prog) {}
  public void OnReliableDataLost(NetworkRunner r, PlayerRef p, ReliableKey k) {}
  public void OnSceneLoadStart(NetworkRunner r) {}
  public void OnSceneLoadDone(NetworkRunner r) {}
  public void OnObjectEnterAOI(NetworkRunner r, NetworkObject obj, PlayerRef p) {}
  public void OnObjectExitAOI (NetworkRunner r, NetworkObject obj, PlayerRef p) {}

  void OnDestroy() {
    if (runner != null) runner.RemoveCallbacks(this);
  }
}

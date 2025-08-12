using System;                          // ← 추가
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
  [SerializeField] private float spawnYOffset = 1.05f; // 캡슐 반높이+여유
  private NetworkRunner runner;

  async void Start() {
    runner = gameObject.AddComponent<NetworkRunner>();
    runner.ProvideInput = true;
    runner.AddCallbacks(this);

    var sceneMgr = gameObject.AddComponent<NetworkSceneManagerDefault>();

    string userId = GetOrCreateUserId();            // 로컬 고정 ID (추후 Firebase uid로 교체)
    string sessionName = MakeIslandSession(userId);

var result = await runner.StartGame(new StartGameArgs{
  GameMode    = GameMode.Host,     // 내 섬일 땐 Host로 직행
  SessionName = sessionName,
  Scene       = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
  SceneManager= sceneMgr
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

  private string GetOrCreateUserId() {              // ← 추가
    const string key = "DEMO_USER_ID";
    if (!PlayerPrefs.HasKey(key))
      PlayerPrefs.SetString(key, Guid.NewGuid().ToString("N"));
    return PlayerPrefs.GetString(key);
  }

  private Vector3 GetIslandSpawnPoint() {
  // 1) 섬 찾기 (필드가 비었으면 Tag로)
  Transform t = island ? island :
                GameObject.FindWithTag("Island")?.transform;
  if (!t) return new Vector3(0, 2f, 0);

  // 2) 위에서 아래로 레이캐스트
  var origin = t.position + Vector3.up * 100f;
  if (Physics.Raycast(origin, Vector3.down, out var hit, 500f, ~0, QueryTriggerInteraction.Ignore)) {
    return hit.point + Vector3.up * spawnYOffset;
  }
  // 실패 시 안전값
  return t.position + Vector3.up * 2f;
}

public void OnPlayerJoined(NetworkRunner r, PlayerRef player) {
  if (!r.IsServer) return;
  var pos = GetIslandSpawnPoint();
  var obj = r.Spawn(playerPrefab, pos, Quaternion.identity, player);

  // (옵션) 혹시 살짝 떠 있으면 즉시 지면으로 스냅
  StartCoroutine(SnapToGroundNextFrame(obj));
}

System.Collections.IEnumerator SnapToGroundNextFrame(NetworkObject obj) {
  yield return null; // 한 프레임 대기 후
  if (obj && obj.TryGetComponent(out CharacterController cc)) {
    // 아래로 짧게 쏴서 바로 붙이기
    var p = obj.transform.position;
    if (Physics.Raycast(p + Vector3.up * 0.5f, Vector3.down, out var hit, 2f)) {
      obj.transform.position = hit.point + Vector3.up * 0.01f;
    }
  }
}

  // 나머지 콜백들
  public void OnPlayerLeft(NetworkRunner r, PlayerRef p) {}
  public void OnInput(NetworkRunner r, NetworkInput input) {
    input.Set(new NetInput{ move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")) });
  }
  public void OnInputMissing(NetworkRunner r, PlayerRef p, NetworkInput input) {}
  public void OnShutdown(NetworkRunner r, ShutdownReason s) {}
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
}

// Assets/02_Scripts/AuthTMPPanel.cs
using System.Collections;
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AuthTMPPanel : MonoBehaviour {
  [Header("UI")]
  [SerializeField] TMP_InputField emailField;
  [SerializeField] TMP_InputField passwordField;
  [SerializeField] Button btnLogIn;
  [SerializeField] Button btnSignUp;
  [SerializeField] Button btnSignOut;          // 없어도 됨
  [SerializeField] Toggle showPasswordToggle;  // 없어도 됨
  [SerializeField] TMP_Text statusText;

  [Header("Game")]
  [SerializeField] GameRunner gameRunner;
  [SerializeField] UserDataService dataService;
  [SerializeField] bool autoStartIslandOnSignIn = true;
  [SerializeField] bool resumePreviousSession = false;

  [Header("Auto Save")]
  [SerializeField] bool  enableAutoSave = true;
  [SerializeField] float autosaveInterval = 2f;   // 초
  [SerializeField] float posThreshold = 0.05f;    // 위치 변화 감지 허용오차

  [SerializeField] CanvasGroup panelGroup;

  FirebaseAuth _auth;
  Coroutine _autosaveCo;
  string _currentUid;

  // 마지막으로 저장한 스냅샷
  Vector3 _lastPos;
  Color   _lastPlayerCol, _lastIslandCol;

  async void Start() {
    await FirebaseApp.CheckAndFixDependenciesAsync();
    _auth = FirebaseAuth.DefaultInstance;

    if (btnLogIn)   btnLogIn.onClick.AddListener(() => _ = LogIn());
    if (btnSignUp)  btnSignUp.onClick.AddListener(() => _ = SignUp());
    if (btnSignOut) btnSignOut.onClick.AddListener(SignOut);
    if (showPasswordToggle) showPasswordToggle.onValueChanged.AddListener(OnToggleShowPassword);

    if (resumePreviousSession && _auth.CurrentUser != null)
      await OnSignedIn(_auth.CurrentUser);
    else
      UpdateStatus("Please sign in.");
  }

  void OnToggleShowPassword(bool show) {
    if (!passwordField) return;
    passwordField.contentType = show ? TMP_InputField.ContentType.Standard
                                     : TMP_InputField.ContentType.Password;
    passwordField.ForceLabelUpdate();
  }

  async Task LogIn() {
    SetInteractable(false);
    try {
      var res = await _auth.SignInWithEmailAndPasswordAsync(emailField.text.Trim(), passwordField.text);
      await OnSignedIn(res.User);
    } catch (FirebaseException fe) { ShowAuthError("Log in failed", fe); }
    finally { SetInteractable(true); }
  }

  async Task SignUp() {
    SetInteractable(false);
    try {
      var res = await _auth.CreateUserWithEmailAndPasswordAsync(emailField.text.Trim(), passwordField.text);
      await OnSignedIn(res.User);
    } catch (FirebaseException fe) { ShowAuthError("Sign up failed", fe); }
    finally { SetInteractable(true); }
  }

void SignOut() {
  StopAutosave();
  _auth?.SignOut();
  SetPanelVisible(true);          // ← SetActive(true) 대신
  UpdateStatus("Signed out.");
}

  async Task OnSignedIn(FirebaseUser user) {
  _currentUid = user.UserId;
  UpdateStatus($"Signed in: {user.Email}");

  var gr = gameRunner ? gameRunner : FindObjectOfType<GameRunner>(true);
  if (!dataService || !gr) { UpdateStatus("Missing GameRunner / DataService"); return; }

  var defSpawn     = gr.transform.position + Vector3.up * 1.5f;
  var defPlayerCol = gr.GetCurrentPlayerColor();
  var defIslandCol = gr.GetCurrentIslandColor();

  var data = await dataService.LoadOrCreate(_currentUid, defSpawn, defPlayerCol, defIslandCol);
  Debug.Log($"[USERDATA] LOAD ok: {JsonUtility.ToJson(data)}");

  gr.SetSpawnOverride(new Vector3(data.spawnX, data.spawnY, data.spawnZ));
  gr.ApplyAppearance(data.playerColor.ToColor(), data.islandColor.ToColor());

  if (autoStartIslandOnSignIn)
    await gr.BeginWithUser(_currentUid);

  if (enableAutoSave) StartAutosave(gr);

  SetPanelVisible(false);         // ← SetActive(false) 대신
}

  // ---------- Autosave ----------
  void StartAutosave(GameRunner gr) {
    StopAutosave(); // 중복 방지
    // 초기 스냅샷
    _lastPos       = gr.GetCurrentPlayerPosition();
    _lastPlayerCol = gr.GetCurrentPlayerColor();
    _lastIslandCol = gr.GetCurrentIslandColor();
    _autosaveCo = StartCoroutine(AutoSaveLoop(gr));
  }

  void StopAutosave() {
    if (_autosaveCo != null) { StopCoroutine(_autosaveCo); _autosaveCo = null; }
  }

IEnumerator AutoSaveLoop(GameRunner gr) {
  while (true) {
    yield return new WaitForSeconds(autosaveInterval);
    _ = TrySaveSnapshotAsync(gr);   // ← fire-and-forget (await 안 함)
  }
}

async Task TrySaveSnapshotAsync(GameRunner gr) {
  if (_auth?.CurrentUser == null || dataService == null) return;

  var pos = gr.GetCurrentPlayerPosition();
  var pc  = gr.GetCurrentPlayerColor();
  var ic  = gr.GetCurrentIslandColor();

  bool posChanged = Vector3.Distance(pos, _lastPos) > posThreshold;
  bool colChanged = !Approximately(pc, _lastPlayerCol) || !Approximately(ic, _lastIslandCol);
  if (!posChanged && !colChanged) return;

  var data = new UserData {
    spawnX = pos.x, spawnY = pos.y, spawnZ = pos.z,
    playerColor = UserData.ColorData.From(pc),
    islandColor = UserData.ColorData.From(ic)
  };

  try {
    await dataService.Save(_currentUid, data);
    _lastPos = pos; _lastPlayerCol = pc; _lastIslandCol = ic;
    Debug.Log("[USERDATA] AUTOSAVE ok");
  } catch (System.Exception e) {
    Debug.LogWarning($"[USERDATA] AUTOSAVE failed: {e.Message}");
  }
}

// 종료 직전만 비동기로 저장. OnDisable에서는 저장하지 마세요(로그인 직후 비활성화되므로).
async void OnApplicationQuit() { await FlushSaveNowAsync(); }

async System.Threading.Tasks.Task FlushSaveNowAsync() {
  var gr = gameRunner ? gameRunner : UnityEngine.Object.FindObjectOfType<GameRunner>(true);
  if (_auth?.CurrentUser == null || dataService == null || gr == null) return;

  var data = new UserData {
    spawnX = gr.GetCurrentPlayerPosition().x,
    spawnY = gr.GetCurrentPlayerPosition().y,
    spawnZ = gr.GetCurrentPlayerPosition().z,
    playerColor = UserData.ColorData.From(gr.GetCurrentPlayerColor()),
    islandColor = UserData.ColorData.From(gr.GetCurrentIslandColor())
  };

  try {
    await dataService.Save(_currentUid, data);   // ✅ 절대 .Wait()/.Result 쓰지 않기
    Debug.Log("[USERDATA] SAVE (quit) ok");
  } catch (System.Exception e) {
    Debug.LogWarning($"[USERDATA] SAVE (quit) failed: {e.Message}");
  }
}

  // ---------- utils ----------
  static bool Approximately(Color a, Color b) =>
    Mathf.Approximately(a.r,b.r) && Mathf.Approximately(a.g,b.g) &&
    Mathf.Approximately(a.b,b.b) && Mathf.Approximately(a.a,b.a);

  void SetInteractable(bool on) {
    if (btnLogIn)   btnLogIn.interactable   = on;
    if (btnSignUp)  btnSignUp.interactable  = on;
    if (btnSignOut) btnSignOut.interactable = on;
    if (emailField) emailField.interactable = on;
    if (passwordField) passwordField.interactable = on;
  }

// 패널 표시/숨김 유틸
void SetPanelVisible(bool show) {
  if (!panelGroup) { gameObject.SetActive(show); return; }
  panelGroup.alpha = show ? 1f : 0f;
  panelGroup.blocksRaycasts = show;
  panelGroup.interactable = show;
}

  void UpdateStatus(string msg) { if (statusText) statusText.text = msg; }

  void ShowAuthError(string prefix, FirebaseException fe) {
    var code = (AuthError)fe.ErrorCode;
    string msg = code switch {
      AuthError.EmailAlreadyInUse => "이미 사용 중인 이메일입니다.",
      AuthError.InvalidEmail      => "이메일 형식이 올바르지 않습니다.",
      AuthError.WeakPassword      => "비밀번호는 최소 6자 이상이어야 합니다.",
      AuthError.WrongPassword     => "비밀번호가 올바르지 않습니다.",
      AuthError.UserNotFound      => "해당 계정을 찾을 수 없습니다.",
      _                           => fe.Message
    };
    UpdateStatus($"{prefix}: {msg}");
    Debug.LogWarning($"{prefix}: {code} / {fe.Message}");
  }
}

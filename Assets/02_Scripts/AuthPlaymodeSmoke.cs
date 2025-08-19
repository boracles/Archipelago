// Assets/Scripts/Auth/AuthPlaymodeSmoke.cs
using System.Threading.Tasks;
using Firebase;
using Firebase.Auth;
using UnityEngine;

public class AuthPlaymodeSmoke : MonoBehaviour {
  [SerializeField] string email = "bebegenerale@gmail.com";
  [SerializeField] string password = "SOBORO!";

  async void Awake() {
    var dep = await FirebaseApp.CheckAndFixDependenciesAsync();
    if (dep != DependencyStatus.Available) {
      Debug.LogError($"[Auth] Firebase deps: {dep}");
      return;
    }

    var auth = FirebaseAuth.DefaultInstance;

    try {
      // v12+ : AuthResult 반환
      var result = await auth.SignInWithEmailAndPasswordAsync(email, password);
      var user = result.User ?? auth.CurrentUser;   // 안전하게 한 번 더
      Debug.Log($"[Auth] SIGN-IN OK uid={user.UserId}, email={user.Email}");

      // 바로 게임 시작하려면:
      FindObjectOfType<GameRunner>(true)?.BeginWithUser(user.UserId);
    }
    catch (FirebaseException fe) {
      Debug.LogError($"[Auth] SignIn error code={fe.ErrorCode} msg={fe.Message}");
    }
    catch (System.Exception e) {
      Debug.LogError($"[Auth] SignIn error: {e}");
    }
  }
}

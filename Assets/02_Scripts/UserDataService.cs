using System.Threading.Tasks;
using Firebase.Database;
using UnityEngine;

[System.Serializable]
public class UserData {
  public float spawnX, spawnY, spawnZ;
  public ColorData playerColor = new ColorData(1,1,1,1);
  public ColorData islandColor = new ColorData(0.7f,0.7f,0.7f,1);

  [System.Serializable]
  public struct ColorData {
    public float r,g,b,a;
    public ColorData(float r,float g,float b,float a){ this.r=r; this.g=g; this.b=b; this.a=a; }
    public static ColorData From(Color c) => new ColorData(c.r,c.g,c.b,c.a);
    public Color ToColor() => new Color(r,g,b,a);
  }
}

public class UserDataService : MonoBehaviour {
  [SerializeField] string databaseUrl; // 인스펙터에 이미 넣어둔 주소

  DatabaseReference _root;

  void Awake() {
    var db = string.IsNullOrEmpty(databaseUrl)
      ? FirebaseDatabase.DefaultInstance
      : FirebaseDatabase.GetInstance(databaseUrl);
    _root = db.RootReference;
  }

  public async Task<UserData> LoadOrCreate(string uid, Vector3 defaultSpawn, Color defPlayer, Color defIsland) {
    var snap = await _root.Child("users").Child(uid).GetValueAsync();
    if (snap.Exists && !string.IsNullOrEmpty(snap.GetRawJsonValue()))
      return JsonUtility.FromJson<UserData>(snap.GetRawJsonValue());

    var data = new UserData{
      spawnX = defaultSpawn.x, spawnY = defaultSpawn.y, spawnZ = defaultSpawn.z,
      playerColor = UserData.ColorData.From(defPlayer),
      islandColor = UserData.ColorData.From(defIsland)
    };
    await Save(uid, data);
    return data;
  }

  public Task Save(string uid, UserData data) {
    var json = JsonUtility.ToJson(data);
    return _root.Child("users").Child(uid).SetRawJsonValueAsync(json);
  }
}

// Assets/02_Scripts/Music/PathUtils.cs
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public static class PathUtils {
  public static List<NetworkObject> SpawnStraightMusicPath(
      NetworkRunner runner, NetworkPrefabRef musicTilePrefab,
      Vector3 from, Vector3 to, float step = 0.9f, float yOffset = 0.02f) {

    var list = new List<NetworkObject>();
    if (!musicTilePrefab.IsValid || runner == null) return list;

    Vector3 v = to - from; v.y = 0f;
    int count = Mathf.Max(1, Mathf.CeilToInt(v.magnitude / step));

    for (int i = 1; i <= count; i++) {
      float t = (float)i / (count + 1);
      Vector3 pos = Vector3.Lerp(from, to, t);

      if (Physics.Raycast(pos + Vector3.up * 5f, Vector3.down, out var hit, 12f,
          Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) {
        pos = hit.point + Vector3.up * yOffset;
      }

      var no = runner.Spawn(musicTilePrefab, pos, Quaternion.identity);
      list.Add(no);
    }
    return list;
  }
}

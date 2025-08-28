// CanvasGroupToggle.cs
using UnityEngine;

public class CanvasGroupToggle : MonoBehaviour {
  [SerializeField] CanvasGroup group;
  [SerializeField] bool startOn = false;

  void Awake() { Set(startOn); }

  public void Set(bool on) {
    if (!group) group = GetComponent<CanvasGroup>();
    if (!group) return;
    group.alpha = on ? 1f : 0f;
    group.interactable = on;
    group.blocksRaycasts = on;
  }
}

// Assets/02_Scripts/Music/MusicTile.cs
using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(AudioSource))]
[RequireComponent(typeof(Rigidbody))]
public class MusicTile : NetworkBehaviour {
  [Header("Clips")]
  [SerializeField] AudioClip[] clips;

  [Header("Audio")]
  [SerializeField, Range(0f,1f)] float targetVolume = 0.5f;
  [SerializeField] float fadeInSeconds = 0.08f;
  [SerializeField] float fadeOutSeconds = 0.15f;
  [SerializeField] float maxDistance = 18f;

  AudioSource _audio;

  [Networked] int  ClipIndex  { get; set; }
  [Networked] bool Pressed    { get; set; }

  void Reset() {
    var col = GetComponent<BoxCollider>();
    col.isTrigger = true; col.size = new Vector3(1f, 0.2f, 1f);

    var rb = GetComponent<Rigidbody>();
    rb.isKinematic = true; rb.useGravity = false;
  }

  public override void Spawned() {
    _audio = GetComponent<AudioSource>();
    _audio.playOnAwake = false;
    _audio.loop = true;
    _audio.spatialBlend = 1f;
    _audio.rolloffMode = AudioRolloffMode.Logarithmic;
    _audio.maxDistance = maxDistance;
    _audio.volume = 0f;

    ApplyClipLocal(ClipIndex);
  }

  // ----- Trigger (서버에서만 판정) -----
  void OnTriggerEnter(Collider other) {
    if (!Object.HasStateAuthority) return;
    if (!IsPlayer(other)) return;
    RPC_SetPressed(true);
  }
  void OnTriggerExit(Collider other) {
    if (!Object.HasStateAuthority) return;
    if (!IsPlayer(other)) return;
    RPC_SetPressed(false);
  }
  bool IsPlayer(Collider c) => c.GetComponentInParent<CharacterController>() != null;

  // ----- RPC 동기화 -----
  [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
  void RPC_SetPressed(bool on) {
    Pressed = on;
    if (Pressed && _audio && _audio.clip && !_audio.isPlaying) _audio.Play();
  }

  [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
  void RPC_SetClip(int idx) {
    ClipIndex = idx;
    ApplyClipLocal(idx);
  }

  void ApplyClipLocal(int idx) {
    if (clips == null || clips.Length == 0 || _audio == null) return;
    _audio.clip = clips[Mathf.Abs(idx) % clips.Length];
  }

  // ----- 볼륨 페이드 -----
  void Update() {
    if (!_audio) return;
    float target = Pressed ? targetVolume : 0f;
    float rate = (Pressed ? (targetVolume / Mathf.Max(0.01f, fadeInSeconds))
                          : (targetVolume / Mathf.Max(0.01f, fadeOutSeconds)));
    _audio.volume = Mathf.MoveTowards(_audio.volume, target, rate * Time.deltaTime);
    if (!Pressed && _audio.isPlaying && _audio.volume <= 0.01f) _audio.Stop();
  }

  // ----- 서버 전용 설정 API -----
  public void SetClipIndexServer(int idx) {
    if (Object.HasStateAuthority) RPC_SetClip(idx);
  }
}

using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource), typeof(Collider))]
public class MusicTile : MonoBehaviour
{
    [Header("Clips")]
    public AudioClip[] clips;     // 프리팹에 클립들 등록
    public int clipIndex;         // 스폰 시 지정됨

    [Header("Audio")]
    [Range(0f,1f)] public float targetVolume = 0.5f;
    public float fadeIn = 0.08f;
    public float fadeOut = 0.15f;
    public float maxDistance = 18f;

    AudioSource _src;
    bool _armed = true;           // 들어올 때만 1회 재생, 나가면 재장전
    Coroutine _fadeCo;

    void Awake()
    {
        _src = GetComponent<AudioSource>();
        _src.playOnAwake = false;
        _src.loop = false;
        _src.spatialBlend = 1f;                 // 3D 사운드
        _src.rolloffMode = AudioRolloffMode.Logarithmic;
        _src.maxDistance = maxDistance;
        _src.volume = 0f;
        var trig = GetComponent<Collider>();
        trig.isTrigger = true;                  // 안전장치
    }

    public void Init(int index)
    {
        clipIndex = Mathf.Clamp(index, 0, Mathf.Max(0, clips.Length - 1));
        _src.clip = clips.Length > 0 ? clips[clipIndex] : null;
    }

// MusicTile.cs에 추가
public void SetClipIndexServer(int seed)
{
    int count = clips != null ? clips.Length : 0;
    int idx = count > 0 ? Mathf.Abs(seed) % count : 0;
    Init(idx); // 내부에서 _src.clip 세팅
}

    void OnTriggerEnter(Collider other)
    {
        if (!_armed) return;
        if (!IsPlayer(other)) return;
        PlayOnce();
        _armed = false; // 들어올 때 1번만
    }

    void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        _armed = true;  // 다시 들어오면 또 1번 재생
        StartFade(0f, fadeOut);
    }

    bool IsPlayer(Collider c)
    {
        // 프로젝트에 맞춰 태그/컴포넌트 아무거나!
        return c.CompareTag("Player") || c.GetComponent<PlayerController>() != null;
    }

    void PlayOnce()
    {
        if (_src.clip == null) return;
        if (_src.isPlaying) _src.Stop();
        _src.time = 0f;
        _src.Play();
        StartFade(targetVolume, fadeIn);
    }

    void StartFade(float to, float dur)
    {
        if (_fadeCo != null) StopCoroutine(_fadeCo);
        _fadeCo = StartCoroutine(FadeTo(to, dur));
    }

    IEnumerator FadeTo(float to, float dur)
    {
        float from = _src.volume;
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            _src.volume = Mathf.Lerp(from, to, dur > 0f ? t / dur : 1f);
            yield return null;
        }
        _src.volume = to;
    }
}

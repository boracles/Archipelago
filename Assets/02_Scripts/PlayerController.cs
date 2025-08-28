using Fusion;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkCharacterController))]
public class PlayerController : NetworkBehaviour {
    [SerializeField] private NetworkCharacterController ncc;
    [SerializeField] private float moveSpeed = 6f;

    public override void Spawned() {
        if (!ncc) ncc = GetComponent<NetworkCharacterController>();
        if (Object.HasInputAuthority) {
            var cam = Camera.main;
            if (cam) {
                cam.transform.SetParent(transform);
                cam.transform.localPosition = new Vector3(0, 1.6f, -4f);
                cam.transform.localRotation = Quaternion.Euler(10f, 0f, 0f);
            }
        }
    }

public override void FixedUpdateNetwork() {
    Vector3 move = Vector3.zero;

    if (GetInput<NetInput>(out var input)) {
        // 수평 이동 입력
        move = new Vector3(input.move.x, 0f, input.move.y).normalized * moveSpeed * Runner.DeltaTime;
    }

    // ✅ 입력이 없어도 항상 Move를 호출해야 접지/중력/충돌이 정상 동작
    ncc.Move(move);
}

}

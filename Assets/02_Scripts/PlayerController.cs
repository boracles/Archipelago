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
        if (GetInput<NetInput>(out var input)) {
            Vector3 dir = new Vector3(input.move.x, 0f, input.move.y);
            ncc.Move(dir.normalized * moveSpeed * Runner.DeltaTime);
        }
    }
}

using UnityEngine;
using UnityEngine.Events;

public class Checkpoint : MonoBehaviour
{
    [SerializeField] RespawnSystem _respawn;
    [SerializeField] Transform _spawnPoint;
    [SerializeField] private UnityEvent _checkpointActivated;
    public Vector3 SpawnPosition => _spawnPoint.position;

    void OnTriggerEnter(Collider other)
    {
        if (_respawn == null) return;
        var rb = other.attachedRigidbody; // 子 Capsule → 親rb
        if (rb == null) return;
        if (!rb.CompareTag("Player")) return; // 判定は rb側
        _respawn.SetCheckpoint(this);

        _checkpointActivated?.Invoke();
    }



}


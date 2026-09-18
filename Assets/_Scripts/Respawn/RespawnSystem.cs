using UnityEngine;
using UnityEngine.Events;

public class RespawnSystem : MonoBehaviour
{
    [SerializeField] private Rigidbody _playerRb;
    [SerializeField] private Checkpoint _current;
    [SerializeField] private UnityEvent _onRespawned;
    [SerializeField] private float _cooldown = 0.25f;
    private float _nextAllowed;
    public void SetCheckpoint(Checkpoint cp)
    {
        if (cp != null) _current = cp;
    }

    public void Respawn()
    {
        if (_current == null || _playerRb == null) return;
        if (Time.time < _nextAllowed) return;
        _nextAllowed = Time.time + _cooldown;
        _playerRb.linearVelocity = Vector3.zero;
        _playerRb.angularVelocity = Vector3.zero;
        _playerRb.position = _current.SpawnPosition;
        _onRespawned?.Invoke();
    }
}

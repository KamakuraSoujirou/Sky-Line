using UnityEngine;

public class FallTrigger : MonoBehaviour
{
    [SerializeField] RespawnSystem _respawn;
    [SerializeField] Rigidbody _playerRb;
    [SerializeField] float _killY = -10f;
    void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody == _playerRb)
            _respawn.Respawn();
    }
    private void Update()
    {
        if (_playerRb.position.y < _killY)
            _respawn.Respawn();
    }
}


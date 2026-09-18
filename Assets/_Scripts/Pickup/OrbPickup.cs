using UnityEngine;
using UnityEngine.Events;

public class OrbPickup : MonoBehaviour
{
    [SerializeField] private float _gaugeAmount = 0.1f;
    [SerializeField] private SpeedGaugeController _speedGaugeController;
    [SerializeField] private UnityEvent _onPickup;
    bool _isPickedUp = false;
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"OrbPickup: OnTriggerEnter with {other.name}");
        if (_isPickedUp) return;
        if (!other.CompareTag("Player")) return;
        {
            _speedGaugeController.AddGauge(_gaugeAmount);
            _onPickup?.Invoke();
            _isPickedUp = true;
            gameObject.SetActive(false);
        }
    }
}

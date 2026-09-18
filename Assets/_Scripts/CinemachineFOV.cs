using UnityEngine;
using Unity.Cinemachine;

public class CinemachineFOV : MonoBehaviour
{
    [SerializeField] private CinemachineCamera _vcam;
    [SerializeField] private SpeedGaugeController _gauge;
    [SerializeField] private float _lerpSpeed = 5f;
    [SerializeField] private float[] _fovByLevel = { 60, 68, 78, 90 };
    private float _targetFov;

    void OnEnable() => _gauge.LevelChanged += OnLevelChanged;
    void OnDisable() => _gauge.LevelChanged -= OnLevelChanged;
    void Start() => _targetFov = FovFor(_gauge.CurrentLevel); // 購読前のレベルを取りこぼさない
    void OnLevelChanged(int oldLv, int newLv)
    {
        _targetFov = FovFor(newLv);
    }
    void LateUpdate()
    {
        var lens = _vcam.Lens;
        lens.FieldOfView = Mathf.Lerp(lens.FieldOfView, _targetFov, Time.deltaTime * _lerpSpeed);
        _vcam.Lens = lens; // struct なので代入し直す
    }

    private float FovFor(int level)
    {
        int i = Mathf.Clamp(level - 1, 0, _fovByLevel.Length - 1);
        return _fovByLevel[i];
    }
}

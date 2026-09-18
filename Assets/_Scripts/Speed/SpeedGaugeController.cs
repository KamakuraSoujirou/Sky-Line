using System;
using UnityEngine;
using UnityEngine.Events;


public class SpeedGaugeController : MonoBehaviour
{
    [SerializeField] private float[] _speedLevelDefinitions = new float[] { 0f, 0.25f, 0.5f, 0.75f };
    [SerializeField] private float[] _moveSpeedMultiplier = new float[] { 1f, 1.5f, 2f, 2.5f };

    [SerializeField] UnityEvent<int> _onLevelChanged;
    [SerializeField] private float _normalizedGauge = 0f;
    [SerializeField] private int _currentLevel = 1;
    // 読み取り専用。代入は Add/Remove/Reset 経由だけ
    public float NormalizedGauge => _normalizedGauge;
    public int CurrentLevel => _currentLevel;

    public float CurrentMoveSpeedMultiplier
    {
        get
        {
            if (_moveSpeedMultiplier == null || _moveSpeedMultiplier.Length == 0)
                return 1f;
            int i = Mathf.Clamp(_currentLevel - 1, 0, _moveSpeedMultiplier.Length - 1);
            return _moveSpeedMultiplier[i];
        }
    }
    public event Action<int, int> LevelChanged;

    //デバッグ用のコンテキストメニュー
    [ContextMenu("Debug/Add 0.1")]
    void DebugAdd() => AddGauge(0.1f);
    [ContextMenu("Debug/Reduce 0.1")]
    void DebugReduce() => RemoveGauge(0.1f);
    [ContextMenu("Debug/Fill")]
    void DebugFill() => AddGauge(1f);
    [ContextMenu("Debug/Reset")]
    void DebugReset() => ResetGauge();

    /// <summary>
    /// スピードゲージを増加させる
    /// </summary>
    /// <param name="amount"></param>
    public void AddGauge(float amount)
    {
        // Implementation for adding gauge
        ApplyGauge(amount);
    }

    /// <summary>
    /// スピードゲージを減少させる
    /// </summary>
    /// <param name="amount"></param>
    public void RemoveGauge(float amount)
    {
        // Implementation for removing gauge
        ApplyGauge(-amount);
    }


    /// <summary>
    /// スピードレベルリセット
    /// </summary>
    public void ResetGauge()
    {
        _normalizedGauge = 0f;
        RecalculateLevel();

    }

    /// <summary>
    /// ゲージ値からレベルを算出する
    /// </summary>
    /// <param name="gaugeValue"></param>
    /// <returns></returns>
    private int ResolveLevel(float gaugeValue)
    {

        if (_speedLevelDefinitions == null || _speedLevelDefinitions.Length == 0)
        return 1;

        //閾値を満たす一番高いレベルを返す
        for (int i = _speedLevelDefinitions.Length - 1; i >= 0; i--)
        {
            if (gaugeValue >= _speedLevelDefinitions[i])
            {
                return i + 1;// index 3 → Lv.4
            }
        }
        return 1;
    }

    /// <summary>
    /// ゲージの増減を適用し、レベルの変化をチェックする
    /// </summary>
    /// <param name="amount"></param>
    private void ApplyGauge(float amount)
    {
        _normalizedGauge = Mathf.Clamp01(_normalizedGauge + amount);
        RecalculateLevel();
    }

    /// <summary>
    /// スピードレベルを再計算する
    /// </summary>
    private void RecalculateLevel()
    {
        int oldLevel = _currentLevel;
        _currentLevel = ResolveLevel(_normalizedGauge);
        if (oldLevel != _currentLevel)
        {
            Debug.Log($"スピードレベルを {oldLevel} から {_currentLevel}に変更しました");
            _onLevelChanged?.Invoke(_currentLevel);
            LevelChanged?.Invoke(oldLevel, _currentLevel);
        }
    }
}

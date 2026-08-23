using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCam : MonoBehaviour
{
    [Header("アサイン")]
    [SerializeField] private Transform _orientation;

    [Header("感度設定")]
    [SerializeField] private float _mouseSensX;
    [SerializeField] private float _mouseSensY;
    [SerializeField] private float _gamePadSensX;
    [SerializeField] private float _gamePadSensY;

    private float _xRotation;
    private float _yRotation;

    private InputAction _lookAction;

    void Start()
    {
        _lookAction = InputSystem.actions.FindAction("Look");
    }

    void Update()
    {
        Vector2 lookValue = _lookAction.ReadValue<Vector2>();
        // 1. デバイスがゲームパッド（コントローラー）かどうかを判定
        bool isGamepad = _lookAction.activeControl?.device is Gamepad;
        // 2. デバイスに応じて適用する感度・時間係数を決定
        float currentSensX = isGamepad ? _gamePadSensX : _mouseSensX;
        float currentSensY = isGamepad ? _gamePadSensY : _mouseSensY;
        float timeFactor = isGamepad ? Time.deltaTime : 1f;
        // 3. 回転量を計算
        float inputX = lookValue.x * currentSensX * timeFactor;
        float inputY = lookValue.y * currentSensY * timeFactor;

        _yRotation += inputX;
        _xRotation -= inputY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        // カメラの回転を更新
        transform.rotation = Quaternion.Euler(_xRotation, _yRotation, 0);
        _orientation.rotation = Quaternion.Euler(0, _yRotation, 0);



    }
}

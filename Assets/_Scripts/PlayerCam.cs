using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCam : MonoBehaviour
{
    [Header("アサイン")]
    [SerializeField] private Transform _orientation;

    [Header("感度設定")]
    [SerializeField] private float _sensX;
    [SerializeField] private float _sensY;

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

        // ゲームパッドの場合はTime.deltaTimeを使用し、マウスの場合は1を使用
        float timeFactor = (_lookAction.activeControl?.device is Gamepad) ? Time.deltaTime : 1f;

        float inputX = lookValue.x * timeFactor * _sensX;
        float inputY = lookValue.y * timeFactor * _sensY;

        _yRotation += inputX;
        _xRotation -= inputY;
        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);

        // カメラの回転を更新
        transform.rotation = Quaternion.Euler(_xRotation, _yRotation, 0);
        _orientation.rotation = Quaternion.Euler(0, _yRotation, 0);



    }
}

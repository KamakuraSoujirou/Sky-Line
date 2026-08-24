using UnityEngine;
using UnityEngine.InputSystem;

public class Sliding : MonoBehaviour
{
    [Header("アサイン")]
    [SerializeField] private Transform _playerObj;
    [Header("スライディング")]
    [SerializeField] private float _maxSlideTime;
    [SerializeField] private float _slideForce;
    [SerializeField] private float _slideTimer;

    [SerializeField] private float _slideYScale;
    private float _startYScale;

    private InputAction _slideAction;

    private Rigidbody _rb;
    private PlayerMovement _pm;
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _pm = GetComponent<PlayerMovement>();
        _slideAction = InputSystem.actions.FindAction("Slide");

        _startYScale = _playerObj.localScale.y;
    }

    void Update()
    {
        MyInput();
    }
    void FixedUpdate()
    {
        if (_pm.IsSliding)
        {
            SlidingMovement();
        }
    }

    private void MyInput()
    {
        if (_slideAction.triggered && !_pm.IsSliding)
        {
            StartSlide();
        }
        if(_slideAction.phase == InputActionPhase.Canceled && _pm.IsSliding)
        {
            StopSlide();
        }
    }
    private void StartSlide()
    {
        _pm.IsSliding = true;
        _playerObj.localScale = new Vector3(_playerObj.localScale.x, _slideYScale, _playerObj.localScale.z);
        _rb.AddForce(Vector3.down * 5f, ForceMode.Impulse);

        _slideTimer = _maxSlideTime;
    }
    private void SlidingMovement()
    {
        Vector3 inputDirection = _pm.Orientation.forward * _pm.VerticalInput + _pm.Orientation.right * _pm.HorizontalInput;


        //通常スライド時
        if (!_pm.OnSlope() || _rb.linearVelocity.y > -0.1f)
        {
            _rb.AddForce(inputDirection.normalized * _slideForce, ForceMode.Force);
            _slideTimer -= Time.deltaTime;
        }
        //スロープ上でのスライド時
        else
        {
            _rb.AddForce(_pm.GetSlopeMoveDirection(inputDirection) * _slideForce, ForceMode.Force);
        }

        if(_slideTimer <= 0)
        {
            StopSlide();
        }
    }
    private void StopSlide()
    {
        _pm.IsSliding = false;
        _playerObj.localScale = new Vector3(_playerObj.localScale.x, _startYScale, _playerObj.localScale.z);

    }
}

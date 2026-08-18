using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("アサイン")]
    [SerializeField] private Transform _orientation;


    [Header("移動設定")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _walkSpeed = 4f;
    [SerializeField] private float _sprintSpeed = 6f;

    [SerializeField] private float _groundDrag = 6f;

    [Header("ジャンプ設定")]
    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _jumpCooldown = 0.25f;
    [SerializeField] private float _airMultiplier = 0.4f;
    private bool _readyToJump = true;

    [Header("しゃがみ設定")]
    [SerializeField] private float _crouchSpeed = 2f;
    [SerializeField] private float _crouchYScale = 0.5f;
    private Vector3 _startScale;


    [Header("設置判定")]
    [SerializeField] private float playerHeight = 2f;
    [SerializeField] private LayerMask groundLayer;
    private bool _grounded;

    [Header("スロープ判定")]
    [SerializeField] private float _maxSlopeAngle = 35f;
    [SerializeField] private RaycastHit _slopeHit;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _crouchAction;

    private Rigidbody _rb;

    private float _horizontalInput;
    private float _verticalInput;

    private Vector3 _moveDirection;

    private MovementState _movementState;
    private enum MovementState
    {
        walking,
        sprinting,
        crouching,
        air
    }


    void Start()
    {
        _moveAction = InputSystem.actions.FindAction("Move");
        _jumpAction = InputSystem.actions.FindAction("Jump");
        _sprintAction = InputSystem.actions.FindAction("Sprint");
        _crouchAction = InputSystem.actions.FindAction("Crouch");

        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;

        _startScale = transform.localScale;

    }

    void Update()
    {
        //設置判定
        _grounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, groundLayer);
        MyInput();

        // Dragを設定
        if (_grounded)
        {
            _rb.linearDamping = _groundDrag;
        }
        else
        {
            _rb.linearDamping = 0f;
        }
    }

    private void FixedUpdate()
    {
        MovePlayer();
        SpeedControl();
        StateHandler();
    }

    private void MyInput()
    {
        // InputActionからの入力を取得
        _horizontalInput = _moveAction.ReadValue<Vector2>().x;
        _verticalInput = _moveAction.ReadValue<Vector2>().y;

        if(_readyToJump && _jumpAction != null && _jumpAction.triggered && _grounded)
        {
            _readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), _jumpCooldown);
        }
        if(_crouchAction != null && _crouchAction.triggered)
        {
            if (transform.localScale.y == _startScale.y)
            {
                transform.localScale = new Vector3(_startScale.x, _crouchYScale, _startScale.z);
                _rb.AddForce(Vector3.down * 10f, ForceMode.Impulse); // しゃがむときに少し下に押す力を加える
                _moveSpeed = _crouchSpeed;
            }
            else
            {
                transform.localScale = _startScale;
                _moveSpeed = _walkSpeed;
            }
        }
    }

    private void StateHandler()
    {
        // 状態を判定
        if(_crouchAction != null && _crouchAction.triggered)
        {
            _movementState = MovementState.crouching;
            _moveSpeed = _crouchSpeed;
        }
        else

            if (_grounded && _sprintAction != null && _sprintAction.ReadValue<float>() > 0.1f)
        {
            _movementState = MovementState.sprinting;
            _moveSpeed = _sprintSpeed;
        }
        else if (_grounded)
        {
            _movementState = MovementState.walking;
            _moveSpeed = _walkSpeed;
        }
        else
        {
            _movementState = MovementState.air;
        }
    }

    private void MovePlayer()
    {
        // 入力に基づいて移動方向を計算
        _moveDirection = _orientation.forward * _verticalInput + _orientation.right * _horizontalInput;

        // スロープ上にいる場合の移動方向を調整
        if (OnSlope())
        {
            _rb.AddForce(GetSlopeMoveDirection() * _moveSpeed * 10f, ForceMode.Force);
            if (_rb.linearVelocity.y > 0)
                _rb.AddForce(Vector3.down * 80f, ForceMode.Force);
        }
        if (_grounded)
        {
            _rb.AddForce(_moveDirection.normalized * _moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            _rb.AddForce(_moveDirection.normalized * _moveSpeed * 10f * _airMultiplier, ForceMode.Force);
        }
    }
    private void SpeedControl()
    {
        Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

        if(flatVel.magnitude > _moveSpeed)
        {
            Vector3 limitedVel = flatVel.normalized * _moveSpeed;
            _rb.linearVelocity = new Vector3(limitedVel.x, _rb.linearVelocity.y, limitedVel.z);
        }
    }

    private void Jump()
    {
        // ジャンプ処理
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        _rb.AddForce(transform.up * _jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        // ジャンプのリセット処理
        _readyToJump = true;
    }
    private bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out _slopeHit, playerHeight * 0.5f + 0.3f))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
            return angle < _maxSlopeAngle && angle != 0;
        }
        return false;
    }
    private Vector3 GetSlopeMoveDirection()
    {
        return Vector3.ProjectOnPlane(_moveDirection, _slopeHit.normal).normalized;
    }
}


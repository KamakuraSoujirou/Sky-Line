using System.Collections;
using UnityEditor.ShaderKeywordFilter;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("アサイン")]
    public Transform Orientation;

    [Header("移動設定")]
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _walkSpeed = 4f;
    [SerializeField] private float _sprintSpeed = 6f;
    [SerializeField] private float _slideSpeed = 8f;
    [SerializeField] private float _wallrunningSpeed;
    [SerializeField] private float _speedIncreaseMultiplier = 1.5f;
    [SerializeField] private float _slopeIncreaseMultiplier = 2.5f;
    [SerializeField] private float _groundDrag = 6f;

    private bool _sprintToggle = false;
    private float _desiredMoveSpeed;
    private float _lastDisiredMoveSpeed;

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
    [SerializeField] private LayerMask _groundLayer;
    public bool IsGrounded;

    [Header("スロープ判定")]
    [SerializeField] private float _maxSlopeAngle = 35f;
    [SerializeField] private RaycastHit _slopeHit;
    [SerializeField] private bool _OnSlope;
    private bool _exitingSlope = false;

    private InputAction _moveAction;
    private InputAction _jumpAction;
    private InputAction _sprintAction;
    private InputAction _crouchAction;

    private Rigidbody _rb;

    public float HorizontalInput{ get; private set; }
    public float VerticalInput { get; private set; }
    public bool IsSliding;
    public bool IsWallrunning;

    private Vector3 _moveDirection;

    [SerializeField] private MovementState _movementState;
    private enum MovementState
    {
        walking,
        sprinting,
        wallruning,
        crouching,
        sliding,
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
        IsGrounded = Physics.Raycast(transform.position, Vector3.down, playerHeight * 0.5f + 0.3f, _groundLayer);
        MyInput();

        // Dragを設定
        if (IsGrounded)
        {
            _rb.linearDamping = _groundDrag;
        }
        else
        {
            _rb.linearDamping = 0f;
        }
        _OnSlope = OnSlope();
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
        HorizontalInput = _moveAction.ReadValue<Vector2>().x;
        VerticalInput = _moveAction.ReadValue<Vector2>().y;

        // Sprintのトグル処理
        if (_sprintAction != null && _sprintAction.triggered)
        {
            _sprintToggle = !_sprintToggle;
        }
        // ジャンプ処理
        if (_readyToJump && _jumpAction != null && _jumpAction.triggered && IsGrounded)
        {
            _readyToJump = false;
            Jump();
            Invoke(nameof(ResetJump), _jumpCooldown);
        }
        if (_crouchAction != null && _crouchAction.triggered)
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

    private bool IsCrouching => transform.localScale.y < _startScale.y;

    private void StateHandler()
    {
        //モード　- 壁走り
        if(IsWallrunning)
        {
            _movementState = MovementState.wallruning;
            _desiredMoveSpeed = _wallrunningSpeed;
        }
        // モード - スライド
        if (IsSliding)
        {
            _movementState = MovementState.sliding;

            if (OnSlope() && VerticalInput > 0)
                _desiredMoveSpeed = _slideSpeed;
            else
                _desiredMoveSpeed = _sprintSpeed;
        }
        // モード - しゃがみ
        else if (IsCrouching)
        {
            _movementState = MovementState.crouching;
            _desiredMoveSpeed = _crouchSpeed;
        }
        // モード - スプリント
        else if (IsGrounded && _sprintToggle)
        {
            _movementState = MovementState.sprinting;
            _desiredMoveSpeed = _sprintSpeed;
        }
        // モード - 歩行
        else if (IsGrounded)
        {
            _movementState = MovementState.walking;
            _desiredMoveSpeed = _walkSpeed;
        }
        // モード - 空中
        else
        {
            _movementState = MovementState.air;
        }
        // 速度の変化がある場合、補間処理を開始
        if (Mathf.Abs(_desiredMoveSpeed - _lastDisiredMoveSpeed) > 6f && _moveSpeed != 0)
        {
            StopAllCoroutines();
            StartCoroutine(SmoothlyLerpMoveSpeed());
        }
        else
        {
            _moveSpeed = _desiredMoveSpeed;
        }

        _lastDisiredMoveSpeed = _desiredMoveSpeed;
    }

    private IEnumerator SmoothlyLerpMoveSpeed()
    {
        // 移動速度を滑らかに補間する処理
        float time = 0;
        float difference = Mathf.Abs(_desiredMoveSpeed - _moveSpeed);
        float startValue = _moveSpeed;
        while (time < difference)
        {
            _moveSpeed = Mathf.Lerp(startValue, _desiredMoveSpeed, time / difference);
            if (OnSlope())
            {
                float slopeAngle = Vector3.Angle(Vector3.up, _slopeHit.normal);
                float slopeAngleIncrease = 1 + (slopeAngle / 90f);

                time += Time.deltaTime * _speedIncreaseMultiplier * _slopeIncreaseMultiplier * slopeAngleIncrease;
            }
            else
                time += Time.deltaTime * _speedIncreaseMultiplier * _slopeIncreaseMultiplier;
            yield return null;
        }
        _moveSpeed = _desiredMoveSpeed;
    }

    private void MovePlayer()
    {
        // 入力に基づいて移動方向を計算
        _moveDirection = Orientation.forward * VerticalInput + Orientation.right * HorizontalInput;

        // スロープ上にいる場合の移動方向を調整
        if (OnSlope() && !_exitingSlope)
        {
            _rb.AddForce(GetSlopeMoveDirection(_moveDirection) * _moveSpeed * 10f, ForceMode.Force);
            if (_rb.linearVelocity.y > 0)
                _rb.AddForce(Vector3.down * 20f, ForceMode.Force);
        }
        else if (IsGrounded)
        {
            _rb.AddForce(_moveDirection.normalized * _moveSpeed * 10f, ForceMode.Force);
        }
        else
        {
            _rb.AddForce(_moveDirection.normalized * _moveSpeed * 10f * _airMultiplier, ForceMode.Force);
        }

        _rb.useGravity = !OnSlope();
    }
    private void SpeedControl()
    {
        // スロープ上での速度制御
        if (OnSlope() && !_exitingSlope)
        {
            if (_rb.linearVelocity.magnitude > _moveSpeed)
            {
                _rb.linearVelocity = _rb.linearVelocity.normalized * _moveSpeed;
            }
        }
        // 地上での速度制御
        else
        {
            Vector3 flatVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);

            // 速度が最大速度を超えた場合、速度を制限
            if (flatVel.magnitude > _moveSpeed)
            {
                Vector3 limitedVel = flatVel.normalized * _moveSpeed;
                _rb.linearVelocity = new Vector3(limitedVel.x, _rb.linearVelocity.y, limitedVel.z);
            }
        }
    }

    private void Jump()
    {
        _exitingSlope = true;
        // ジャンプ処理
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        _rb.AddForce(transform.up * _jumpForce, ForceMode.Impulse);
    }

    private void ResetJump()
    {
        // ジャンプのリセット処理
        _readyToJump = true;

        _exitingSlope = false;
    }
    public bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out _slopeHit, playerHeight * 0.5f + 0.3f, _groundLayer))
        {
            float angle = Vector3.Angle(Vector3.up, _slopeHit.normal);
            return angle < _maxSlopeAngle && angle != 0;
        }
        return false;
    }
    public Vector3 GetSlopeMoveDirection(Vector3 moveDirection)
    {
        return Vector3.ProjectOnPlane(moveDirection, _slopeHit.normal).normalized;
    }
}


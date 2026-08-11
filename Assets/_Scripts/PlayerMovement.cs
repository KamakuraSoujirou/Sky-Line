using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("アサイン")]
    [SerializeField] private Transform _orientation;


    [Header("プレイヤー設定")]
    [SerializeField] private float _moveSpeed = 5f;

    [SerializeField] private float _groundDrag = 6f;

    [SerializeField] private float _jumpForce = 5f;
    [SerializeField] private float _jumpCooldown = 0.25f;
    [SerializeField] private float _airMultiplier = 0.4f;
    private bool _readyToJump = true;


    [Header("設置判定")]
    public float playerHeight = 2f;
    public LayerMask groundLayer;
    private bool _grounded;


    private InputAction _moveAction;
    private InputAction _jumpAction;

    private Rigidbody _rb;

    private float _horizontalInput;
    private float _verticalInput;

    private Vector3 _moveDirection;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _moveAction = InputSystem.actions.FindAction("Move");
        _jumpAction = InputSystem.actions.FindAction("Jump");

        _rb = GetComponent<Rigidbody>();
        _rb.freezeRotation = true;

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
    }

    private void MovePlayer()
    {
        // 入力に基づいて移動方向を計算
        _moveDirection = _orientation.forward * _verticalInput + _orientation.right * _horizontalInput;

        if(_grounded)
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
}


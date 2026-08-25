using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class WallRunning : MonoBehaviour
{
    [Header("ウォールラン設定")]
    [SerializeField] private LayerMask _wallLayer;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _wallRunForce;
    [SerializeField] private float _wallJumpUpForce;
    [SerializeField] private float _wallJumpSideForce;
    [SerializeField] private float _wallClimebSpeed;
    [SerializeField] private float _maxWallRunTime;
    private float _wallRunTimer;
    [Header("壁検知")]
    [SerializeField] private float _wallCheckDistance;
    [SerializeField] private float _minJumpHeight;
    private RaycastHit _leftWallhit;
    private RaycastHit _rightWallhit;
    private bool _wallLeft;
    private bool _wallRight;

    private Rigidbody _rb;
    private PlayerMovement _pm;
    private InputAction _jumpAction;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _pm = GetComponent<PlayerMovement>();
        _jumpAction = InputSystem.actions.FindAction("Jump");
    }

    // Update is called once per frame
    void Update()
    {
        CheckForWall();
        StateMachine();
    }
    private void FixedUpdate()
    {
        if (_pm.IsWallrunning)
        {
            WallRunningMovement();
        }
    }
    private void CheckForWall()
    {
        _wallRight = Physics.Raycast(transform.position, _pm.Orientation.right, out _rightWallhit, _wallCheckDistance, _wallLayer);
        _wallLeft = Physics.Raycast(transform.position, -_pm.Orientation.right, out _leftWallhit, _wallCheckDistance, _wallLayer);
    }
    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, _minJumpHeight, _groundLayer);
    }
    private void StateMachine()
    {
        //State - 壁走り
        if ((_wallLeft || _wallRight) && _pm.VerticalInput > 0 && AboveGround())
        {
            if (!_pm.IsWallrunning)
            {
                StartWallRun();
            }
            //壁ジャンプ
            if (_jumpAction != null && _jumpAction.triggered)
            {
                WallJump();
            }
        }
        //State - 通常時
        else
        {
            if (_pm.IsWallrunning)
            {
                StopWallRun();
            }
        }
    }
    private void StartWallRun()
    {
        _pm.IsWallrunning = true;
    }
    private void WallRunningMovement()
    {
        _rb.useGravity = false;
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
        Vector3 wallNormal = _wallRight ? _rightWallhit.normal : _leftWallhit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

        //壁が反対向きの場合
        if((_pm.Orientation.forward - wallForward).magnitude > (_pm.Orientation.forward - -wallForward).magnitude)
        {
            wallForward = -wallForward;
        }
        _rb.AddForce(wallForward * _wallRunForce, ForceMode.Force);
        //壁に押し付ける
        if (!(_wallLeft && _pm.HorizontalInput > 0) && !(_wallRight && _pm.HorizontalInput < 0))
        {
            _rb.AddForce(-wallNormal * 50f, ForceMode.Force);
        }
    }
    private void StopWallRun()
    {
        _pm.IsWallrunning = false;
    }
    private void WallJump()
    {
        Vector3 wallNormal = _wallRight ? _rightWallhit.normal : _leftWallhit.normal;
        Vector3 forceToApply = transform.up * _wallJumpUpForce + wallNormal * _wallJumpSideForce;

        //力をリセットして、ジャンプする
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x,0f,_rb.linearVelocity.z);
        _rb.AddForce(forceToApply, ForceMode.Impulse);

        Debug.Log("壁ジャンプ");
    }
}

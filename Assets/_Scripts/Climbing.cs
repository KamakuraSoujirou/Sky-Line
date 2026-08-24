using Unity.VisualScripting;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputSystem;

public class Climbing : MonoBehaviour
{
    [Header("参照")]

    //[SerializeField] private LayerMask[] _whatIsLayers;
    [SerializeField] private Transform _cameraTrans;

    [Header("登る設定")]
    [SerializeField] private float _climbSpeed;
    [SerializeField] private float _maxClimebTime;
    [SerializeField] private float _climbTimer;
    [SerializeField] private bool _climbing;

    [Header("壁検知")]
    [SerializeField]private float _detactionLength;
    [SerializeField] private float _sphereCastRadius;
    [SerializeField] private float _maxWallLookAngle;
    private float _wallLookAngle;
    [SerializeField]private RaycastHit _frontWallHit;
    private bool _wallFront;
    private Rigidbody _rb;
    private PlayerMovement _pm;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        _pm = GetComponent<PlayerMovement>();
    }
    private void Update()
    {
        WallCheck();
        StateMachine();

        if (_climbing) ClimbingMovement();
    }
    private void StateMachine()
    {
        //State Climbing
        if (_wallFront && _pm.VerticalInput > 0 && _wallLookAngle < _maxWallLookAngle)
        {
            if (!_climbing && _climbTimer > 0) StartClimbing();
            if (_climbTimer > 0) _climbTimer -= Time.deltaTime;
            if (_climbTimer < 0) StopClimbing();

        }
        //State None
        else
        {
            if(_climbing) StopClimbing() ;
        }
    }

    private void WallCheck()
    {
        _wallFront = Physics.SphereCast(transform.position, _sphereCastRadius, _pm.Orientation.forward, out _frontWallHit, _detactionLength);
        _wallLookAngle = Vector3.Angle(_pm.Orientation.forward, -_frontWallHit.normal);

        if (_pm.IsGrounded)
        {
            _climbTimer = _maxClimebTime;
        }
    }
    private void StartClimbing()
    {
     _climbing = true;
    }
    private void ClimbingMovement()
    {
        _rb.linearVelocity = new Vector3(_rb.linearVelocity.x, _climbSpeed, _rb.linearVelocity.z);
    }
    private void StopClimbing()
    {
        _climbing = false;
    }
}

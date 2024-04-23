using System;
using UnityEngine;

public class HeroEntity : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private Rigidbody2D _rigidbody;

    [Header("Horizontal Movements")]
    [SerializeField] private HeroHorizontalMovementSettings _movementsSettings;
    private float _horizontalSpeed = 0f;
    private float _moveDirX = 0f;

    [Header("Vertical Movements")]
    private float _verticalSpeed = 0f;

    [Header("Fall")]
    [SerializeField] private HeroFallSettings _fallSettings;

    [Header("Ground")]
    [SerializeField] private GroundDetector _groundDetector;
    public bool IsTouchingGround { get; private set; }

    [Header("Jump")]
    [SerializeField] private HeroJumpSettings _jumpSettings;

    [SerializeField] private HeroFallSettings _jumpFallSettings;
    enum JumpState{
        NotJumping,
        JumpImplusion,
        Falling
    }
    private JumpState _jumpState = JumpState.NotJumping;
    private float _jumpTimer = 0f;

    [Header("Dash")]
    [SerializeField] private DashSettings _dashSettings;
    private bool _isDashing;

    [Header("Orientation")]
    [SerializeField] private Transform _orientVisualRoot;
    private float _orientX = 1f;

    [Header("Debug")]
    [SerializeField] private bool _guiDebug = false;

    public void SetMoveDirX(float dirX)
    {
        _moveDirX = dirX;
    }

    #region Dash
    public void SetDash(bool dash)
    {
        _isDashing = dash;
    }

    private void _ApplyDashSpeed()
    {
        _horizontalSpeed += _dashSettings.DashSpeed * _dashSettings.DashDuration;
        if (_horizontalSpeed > _dashSettings.DashSpeed)
        {
            _horizontalSpeed = _dashSettings.DashSpeed;
        }
    }
    #endregion

    #region Update
    private void FixedUpdate()
    {
        _ApplyGroundDetection();

        if (_AreOrientAndMovementOpposite())
        {
            _TurnBack();
        }
        else { 
            _UpdateHorizontalSpeed();
            _ChangeOrientFromHorizontalMovement();
        }

        if (_isDashing)
        {
            _ApplyDashSpeed();
        }

        if (IsJumping)
        {
            _UpdateJump();
        }
        else
        {
            if (!IsTouchingGround)
            {
                _ApplyFallGravity(_fallSettings);
            }
            else
            {
                _ResetVerticalSpeed();
            }
        }
        _ApplyHorizontalSpeed();
        _ApplyVerticalSpeed();

    }

    private void Update()
    {
        _UpdateOrientVisual();
    }

    #endregion

    #region Horizontal Movements
    private void _ApplyHorizontalSpeed()
    {
        Vector2 velocity = _rigidbody.velocity;
        velocity.x = _horizontalSpeed * _orientX;
        _rigidbody.velocity = velocity;
    }


    private void _UpdateHorizontalSpeed()
    {
        if (_moveDirX != 0f)
        {
            _Accelerate();
        }
        else
        {
            _Decelerate();
        }
    }

    private void _Accelerate()
    {
        _horizontalSpeed += _movementsSettings.acceleration * Time.fixedDeltaTime;
        if( _horizontalSpeed > _movementsSettings.speedMax)
        {
            _horizontalSpeed = _movementsSettings.speedMax;
        }
    }

    private void _Decelerate()
    {
        _horizontalSpeed -= _movementsSettings.deceleration * Time.fixedDeltaTime;
        if(_horizontalSpeed < 0f)
        {
            _horizontalSpeed = 0f;
        }
    }

    private void _TurnBack()
    {
        _horizontalSpeed -= _movementsSettings.turnBackFrictions * Time.fixedDeltaTime;
        if( _horizontalSpeed < 0f)
        {
            _horizontalSpeed = 0f;
            _ChangeOrientFromHorizontalMovement();
        }
    }

    #endregion

    #region Vertical Movements

    private void _ApplyFallGravity(HeroFallSettings settings)
    {
        _verticalSpeed -= settings.fallGravity * Time.fixedDeltaTime;
        if(_verticalSpeed < -settings.fallSpeedMax)
        {
            _verticalSpeed = -settings.fallSpeedMax;
        }
    }

    private void _ApplyVerticalSpeed()
    {
        Vector2 velocity = _rigidbody.velocity;
        velocity.y = _verticalSpeed;
        _rigidbody.velocity = velocity;
    }

    private void _ApplyGroundDetection()
    {
        IsTouchingGround = _groundDetector.DetectGroundNeayBy();
    }

    public void _ResetVerticalSpeed()
    {
        _verticalSpeed = 0f;
    }

    #endregion
    #region Jump

    public void JumpStart()
    {
        _jumpState = JumpState.JumpImplusion;
        _jumpTimer = 0f;
    }

    public bool IsJumping => _jumpState != JumpState.NotJumping;

    private void _UpdateJumpStateImpulsion()
    {
        _jumpTimer += Time.fixedDeltaTime;
        if (_jumpTimer < _jumpSettings.jumpMaxDuration)
        {
            _verticalSpeed = _jumpSettings.jumpSpeed;
        }
        else
        {
            _jumpState = JumpState.Falling;
        }
    }

    public void StopJumpImpulsion()
    {
        _jumpState = JumpState.Falling;
    }

    public bool IsJumpImpulsion => _jumpState == JumpState.JumpImplusion;
    public bool IsJumpMinDurationReached => _jumpTimer >= _jumpSettings.jumpMinDuration;

    private void _UpdateJumpStateFalling()
    {
        if (!IsTouchingGround)
        {
            _ApplyFallGravity(_jumpFallSettings);
        }
        else
        {
            _ResetVerticalSpeed();
            _jumpState = JumpState.NotJumping;
        }
    }

    private void _UpdateJump()
    {
        switch (_jumpState)
        {
            case JumpState.JumpImplusion:
                _UpdateJumpStateImpulsion();
                break;
            case JumpState.Falling:
                _UpdateJumpStateFalling();
                break;
        }
    }
    
    
    #endregion
    
    #region Sprite

    private void _ChangeOrientFromHorizontalMovement()
    {
        if (_moveDirX == 0f) return;
        _orientX = Mathf.Sign(_moveDirX);
    }

    private void _UpdateOrientVisual()
    {
        Vector3 newScale = _orientVisualRoot.localScale;
        newScale.x = _orientX;
        _orientVisualRoot.localScale = newScale;
    }

    private bool _AreOrientAndMovementOpposite()
    {
        return _moveDirX * _orientX <0f;
    }

    #endregion

    private void OnGUI()
    {
        if (!_guiDebug) return;

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(gameObject.name);
        GUILayout.Label($"MoveDirX = {_moveDirX}");
        GUILayout.Label($"OrientX = {_orientX}");
        if(IsTouchingGround)
        {
            GUILayout.Label("OnGround");
        }
        else
        {
            GUILayout.Label("InAir");
        }
        GUILayout.Label($"Horizontal Speed = {_horizontalSpeed}");
        GUILayout.Label($"Vertical Speed = {_verticalSpeed}");
        GUILayout.Label($"isDashing = {_isDashing}");
        
        GUILayout.EndVertical();
    }
}
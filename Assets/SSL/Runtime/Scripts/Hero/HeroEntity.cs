using System;
using UnityEngine;
using UnityEngine.Serialization;

public class HeroEntity : MonoBehaviour
{
    [Header("Physics")]
    [SerializeField] private Rigidbody2D _rigidbody;

    [Header("Horizontal Movements")]
    [FormerlySerializedAs("_movementSettings")]
    [SerializeField] private HeroHorizontalMovementSettings _groundHorizontalMovementSettings;
    [SerializeField] private HeroHorizontalMovementSettings _airHorizontalMovementSettings;
    
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
    [FormerlySerializedAs("_dashsettings")]
    [SerializeField] private DashSettings _groundDashSettings;
    [SerializeField] private DashSettings _airDashSettings;

    private bool _isDashing = false;
    private float _dashTimer = 0f;

    private float _UpdateGravityTimer = 0.6f;
    private float _UpdateGravityCooldown = 0f;
    
    [Header("Orientation")]
    [SerializeField] private Transform _orientVisualRoot;
    private float _orientX = 1f;

    [Header("Debug")]
    [SerializeField] private bool _guiDebug = false;

    

    #region Update
    private void FixedUpdate()
    {
        _ApplyGroundDetection();
        HeroHorizontalMovementSettings horizontalMovementSettings = _getCurrentHorizontalMovementSettings();
        DashSettings dashSettings = _getCurrentDashSettings();
        if (_AreOrientAndMovementOpposite())
        {
            _TurnBack(horizontalMovementSettings);
        }
        else { 
            _UpdateHorizontalSpeed(horizontalMovementSettings);
            _ChangeOrientFromHorizontalMovement();
        }

        if (_isDashing)
        {
            _UpdateDashImpulsion(dashSettings);
        }
        
        else
        {
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
    
    public void SetMoveDirX(float dirX)
    {
        _moveDirX = dirX;
    }
    private void _ApplyHorizontalSpeed()
    {
        Vector2 velocity = _rigidbody.velocity;
        velocity.x = _horizontalSpeed * _orientX;
        _rigidbody.velocity = velocity;
    }


    private void _UpdateHorizontalSpeed(HeroHorizontalMovementSettings _settings)
    {
        if (_moveDirX != 0f)
        {
            _Accelerate(_settings);
        }
        else
        {
            _Decelerate(_settings);
        }
    }

    private void _Accelerate(HeroHorizontalMovementSettings settings)
    {
        _horizontalSpeed += settings.acceleration * Time.fixedDeltaTime;
        if( _horizontalSpeed > settings.speedMax)
        {
            _horizontalSpeed = settings.speedMax;
        }
    }

    private void _Decelerate(HeroHorizontalMovementSettings settings)
    {
        _horizontalSpeed -= settings.deceleration * Time.fixedDeltaTime;
        if(_horizontalSpeed < 0f)
        {
            _horizontalSpeed = 0f;
        }
    }

    private void _TurnBack(HeroHorizontalMovementSettings settings)
    {
        _horizontalSpeed -= settings.turnBackFrictions * Time.fixedDeltaTime;
        if( _horizontalSpeed < 0f)
        {
            _horizontalSpeed = 0f;
            _ChangeOrientFromHorizontalMovement();
        }
    }


    private HeroHorizontalMovementSettings _getCurrentHorizontalMovementSettings()
    {
        if (IsTouchingGround)
        {
            return _groundHorizontalMovementSettings;
        }
        else
        {
            return _airHorizontalMovementSettings;
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
    
    #region Dash

    public void StartDash()
    {
        _isDashing = true;
        _dashTimer = 0f;
    }

    private void _UpdateDashImpulsion(DashSettings settings)
    {
        _dashTimer += Time.fixedDeltaTime;
        if (_dashTimer < settings.DashDuration)
        {
            _horizontalSpeed = settings.DashSpeed;
            _UpdateJump();
            _UpdateGravity();
        }
        else
        {
            _horizontalSpeed = _getCurrentHorizontalMovementSettings().speedMax;
            _isDashing = false;
        }
    }

    private void _UpdateGravity()
    {
        if (!IsTouchingGround)
        {
            _UpdateGravityCooldown = 0f;
            _rigidbody.gravityScale = 0f;
            if (_UpdateGravityCooldown >= _UpdateGravityTimer)
            {
                _rigidbody.gravityScale = 1f;
            }
            _UpdateGravityCooldown += Time.fixedDeltaTime;
        }
    }

    private DashSettings _getCurrentDashSettings()
    {
        if (IsTouchingGround)
        {
            return _groundDashSettings;
        }
        else
        {
            return _airDashSettings;
        }
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
        GUILayout.Label($"IsDashing = {_isDashing}");
        //GUILayout.Label($"isDashing = {_isDashing}");
        
        GUILayout.EndVertical();
    }
}
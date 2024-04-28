using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Serialization;

public class HeroEntity : MonoBehaviour
{
    [Header("Physics")] [SerializeField] private Rigidbody2D _rigidbody;

    [Header("Horizontal Movements")] [FormerlySerializedAs("_movementSettings")] [SerializeField]
    private HeroHorizontalMovementSettings _groundHorizontalMovementSettings;

    [SerializeField] private HeroHorizontalMovementSettings _airHorizontalMovementSettings;

    private float _horizontalSpeed = 0f;
    private float _moveDirX = 0f;

    [Header("Vertical Movements")] private float _verticalSpeed = 0f;

    [Header("Fall")] [SerializeField] private HeroFallSettings _fallSettings;

    [Header("Ground")] [SerializeField] private GroundDetector _groundDetector;
    public bool IsTouchingGround { get; private set; }

    [Header("Wall")] [SerializeField] private WallDetector _wallDetector;
    public bool IsTouchingWall { get; private set; }

    [SerializeField] private HeroWallInteractSettings _wallInteractSettings;
    public bool isWallSliding { get; private set; }
    public bool wallJumpAvailable { get; private set; }

    [Header("Jump")] [SerializeField] private HeroJumpSettings _jumpSettings;
    [SerializeField] private HeroJumpSettings _wallJumpSettings;
    private HeroJumpSettings _currentJumpSettings;
    [SerializeField] private HeroFallSettings _jumpFallSettings;
    [SerializeField] private HeroHorizontalMovementSettings _jumpHorizontalMovementSettings;

    public enum JumpState
    {
        NotJumping,
        JumpImplusion,
        Falling
    }

    public static JumpState _jumpState = JumpState.NotJumping;
    
    private float _jumpTimer = 0f;

    [SerializeField] private HeroJumpSettings[] _listHeroMultiJump;
    public bool wasJumping { get; private set; }
    public int indexJump = 1;
    private bool canJump;

    [Header("Dash")] [FormerlySerializedAs("_dashsettings")]
    [SerializeField] private DashSettings _groundDashSettings;

    [SerializeField] private DashSettings _airDashSettings;

    [SerializeField] private HeroFallSettings _dashFallSettings;
    private bool _isDashing = false;
    private float _dashTimer = 0f;

    [Header("Orientation")] [SerializeField]
    private Transform _orientVisualRoot;

    public static float _orientX = 1f;

    [Header("Debug")] [SerializeField] private bool _guiDebug = false;

    [Header("Camera")] private CameraFollowable _cameraFollowable;

    private void Awake()
    {
        _cameraFollowable = GetComponent<CameraFollowable>();
        _cameraFollowable.FollowPositionX = _rigidbody.position.x;
        _cameraFollowable.FollowPositionY = _rigidbody.position.y;
    }

    #region Camera

    private void _UpdateCameraFollowPosition()
    {
        _cameraFollowable.FollowPositionX = _rigidbody.position.x;
        if (IsTouchingGround && !IsJumping)
        {
            _cameraFollowable.FollowPositionY = _rigidbody.position.y;
        }
    }

    #endregion

    #region Update

    private void FixedUpdate()
    {
        _ApplyGroundDetection();
        _ApplyWallDetection();
        _UpdateCameraFollowPosition();
        HeroHorizontalMovementSettings horizontalMovementSettings = _getCurrentHorizontalMovementSettings();
        DashSettings dashSettings = _getCurrentDashSettings();
        if (_AreOrientAndMovementOpposite())
        {
            _TurnBack(horizontalMovementSettings);
        }
        else
        {
            _UpdateHorizontalSpeed(horizontalMovementSettings);
            _ChangeOrientFromHorizontalMovement();
        }

        if (IsTouchingWall)
        {
            _ResetHorizontalSpeed();
            if (_IsWallSliding())
            {
                _WallSlide(_wallInteractSettings);
            }
        }

        if (_isDashing)
        {
            _UpdateDashImpulsion(dashSettings);
        }
        else
        {
            if (IsJumping && canJump)
            {
                if (_IsWallSliding())
                {
                    _WallJumping();
                }
                else if (WasJumping())
                {
                    _MultiJump();
                }
                else
                {
                    _UpdateJump(_currentJumpSettings);
                }
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
        if (_horizontalSpeed > settings.speedMax)
        {
            _horizontalSpeed = settings.speedMax;
        }
    }

    private void _Decelerate(HeroHorizontalMovementSettings settings)
    {
        _horizontalSpeed -= settings.deceleration * Time.fixedDeltaTime;
        if (_horizontalSpeed < 0f)
        {
            _horizontalSpeed = 0f;
        }
    }

    private void _TurnBack(HeroHorizontalMovementSettings settings)
    {
        _horizontalSpeed -= settings.turnBackFrictions * Time.fixedDeltaTime;
        if (_horizontalSpeed < 0f)
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
        else if (!IsTouchingGround && _jumpState == JumpState.NotJumping)
        {
            return _airHorizontalMovementSettings;
        }
        else if (!IsTouchingGround && _jumpState == JumpState.Falling)
        {
            return _jumpHorizontalMovementSettings;
        }

        return _groundHorizontalMovementSettings;
    }

    private void _ResetHorizontalSpeed()
    {
        _horizontalSpeed = 0f;
    }

    #endregion

    #region Vertical Movements

    private void _ApplyFallGravity(HeroFallSettings settings)
    {
        _verticalSpeed -= settings.fallGravity * Time.fixedDeltaTime;
        if (_verticalSpeed < -settings.fallSpeedMax)
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
        IsTouchingGround = _groundDetector.DetectGroundNearBy();
        if (IsTouchingGround)
        {
            indexJump = 1;
            canJump = true;
        }
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
            _UpdateJump(_currentJumpSettings);
            _ApplyFallGravity(_dashFallSettings);
        }
        else
        {
            _horizontalSpeed = _getCurrentHorizontalMovementSettings().speedMax;
            _isDashing = false;
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

    private void _UpdateJumpStateImpulsion(HeroJumpSettings settings)
    {
        _jumpTimer += Time.fixedDeltaTime;
        if (_jumpTimer < settings.jumpMaxDuration)
        {
            _verticalSpeed = settings.jumpSpeed;
            if (_currentJumpSettings == _wallJumpSettings && wallJumpAvailable)
            {
                _orientX *= -1;
                _horizontalSpeed += settings.jumpSpeed * _orientX;
            }
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
        if (!IsTouchingGround && !IsTouchingWall)
        {
            _ApplyFallGravity(_jumpFallSettings);
        }
        else
        {
            _ResetVerticalSpeed();
            _jumpState = JumpState.NotJumping;
        }
    }

    private void _UpdateJump(HeroJumpSettings settings)
    {
        switch (_jumpState)
        {
            case JumpState.JumpImplusion:
                _UpdateJumpStateImpulsion(settings);
                break;
            case JumpState.Falling:
                _UpdateJumpStateFalling();
                break;
        }
    }

    private void _MultiJump()
    {
        if (indexJump >= _listHeroMultiJump.Length)
        {
            canJump = false;
        }
        else
        {
            _UpdateJump(_listHeroMultiJump[indexJump]);
        }
    }

    private bool WasJumping()
    {
        if (_jumpState == JumpState.Falling)
        {
            wasJumping = true;
        }
        else
        {
            wasJumping = false;
        }

        return wasJumping;
    }

    #endregion

    #region WallSlide / Wall Jump

    private void _ApplyWallDetection()
    {
        if (_orientX == 1f)
            IsTouchingWall = _wallDetector.DetectWallNearByRight();
        if (_orientX == -1)
            IsTouchingWall = _wallDetector.DetectWallNearByLeft();
    }

    private bool _IsWallSliding()
    {
        if (!IsTouchingGround && IsTouchingWall)
        {
            isWallSliding = true;
            wallJumpAvailable = true;
            _currentJumpSettings = _wallJumpSettings;
        }
        else
        {
            isWallSliding = false;
            _currentJumpSettings = _jumpSettings;
        }

        return isWallSliding;
    }

    private void _WallSlide(HeroWallInteractSettings settings)
    {
        _verticalSpeed -= settings.wallSlideSpeed * Time.fixedDeltaTime;
        if (_verticalSpeed < -settings.wallSlideSpeed)
        {
            _verticalSpeed = -settings.wallSlideSpeed;
        }
    }

    private void _WallJumping()
    {
        _currentJumpSettings = _wallJumpSettings;
        _UpdateJump(_wallJumpSettings);
        wallJumpAvailable = false;
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
        return _moveDirX * _orientX < 0f;
    }

    #endregion

    private void OnGUI()
    {
        if (!_guiDebug) return;

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(gameObject.name);
        GUILayout.Label($"MoveDirX = {_moveDirX}");
        GUILayout.Label($"OrientX = {_orientX}");
        if (IsTouchingGround)
        {
            GUILayout.Label("OnGround");
        }
        else
        {
            GUILayout.Label("InAir");
        }

        GUILayout.Label($"IsTouchingWall = {IsTouchingWall}");
        GUILayout.Label($"Horizontal Speed = {_horizontalSpeed}");
        GUILayout.Label($"Vertical Speed = {_verticalSpeed}");
        GUILayout.Label($"IsDashing = {_isDashing}");
        GUILayout.Label($"WasJumping = {WasJumping()}");
        GUILayout.Label($"WasJumping = {_jumpState}");
        GUILayout.Label($"WasJumping = {indexJump}");
        

        GUILayout.EndVertical();
    }
}
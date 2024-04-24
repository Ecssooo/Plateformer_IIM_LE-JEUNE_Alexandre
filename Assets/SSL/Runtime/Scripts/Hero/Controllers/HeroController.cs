using System.Runtime.InteropServices.WindowsRuntime;
using UnityEngine;


public class HeroController : MonoBehaviour
{
    [Header("Entity")]
    [SerializeField] private HeroEntity _entity;
    private bool _entityWasTouchingGround = false;
    
    [Header("Debug")]
    [SerializeField] private bool _guiDebug = false;

    [Header("Jump Buffer")] 
    [SerializeField] private float _jumpBufferDuration = 0.2f;
    private float _jumpBufferTimer = 0f;

    [Header("Coyote Time")] 
    [SerializeField] private float _coyoteTimeDuration = 0.2f;
    private float _coyoteTimeCooldown = 0f;

    private void OnGUI()
    {
        if (!_guiDebug) return;

        GUILayout.BeginVertical(GUI.skin.box);
        GUILayout.Label(gameObject.name);
        GUILayout.Label($"Jump Buffer Timer = {_jumpBufferTimer}");
        GUILayout.Label($"Jump Buffer Timer = {_coyoteTimeCooldown}");
        GUILayout.EndVertical();
    }

    private void Start()
    {
        _CancelJumpBuffer();
    }
    private void Update()
    {
        _UpdateJumpBuffer(); 
        _entity.SetMoveDirX(GetInputMoveX());
        if (GetInputDash())
        {
            _entity.StartDash();
        }
        else
        {
            if (_EntityHasExitGround())
            {
                _ResetCoyoteTime();
            }
            else
            {
                _UpdateCoyoteTime();
            }
            
            if (_GetInputDownJump())
            {
                if ((_entity.IsTouchingGround ||_IsCoyoteActive()) && !_entity.IsJumping)
                {
                    _entity.JumpStart();
                }
                else
                {
                    _ResetJumpBuffer();
                }
            }

            if (IsJumpBufferActive())
            {
                if ((_entity.IsTouchingGround ||_IsCoyoteActive()) && !_entity.IsJumping)
                {
                    _entity.JumpStart();
                }
            }

            if (_entity.IsJumpImpulsion)
            {
                if (!_GetInputJump() && _entity.IsJumpMinDurationReached)
                {
                    _entity.StopJumpImpulsion();
                }
            }
        }

        _entityWasTouchingGround = _entity.IsTouchingGround;

    }

    #region Horizontal Controller
    private float GetInputMoveX()
    {
        float inputMoveX = 0f;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Q))
        {
            inputMoveX = -1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            inputMoveX = 1f;
        }
        return inputMoveX;
    }
    
    private bool GetInputDash()
    {
        return Input.GetKeyDown(KeyCode.E);
    }
    #endregion
    
    #region Jump Controller
    
    #region Jump Input
    
    private bool _GetInputDownJump()
    {
        return Input.GetKeyDown(KeyCode.Space);
    }

    private bool _GetInputJump()
    {
        return Input.GetKey(KeyCode.Space);
    }
    
    
    #endregion
    
    #region Jump Buffer
    private void _ResetJumpBuffer()
    {
        _jumpBufferTimer = 0f;
    }

    private bool IsJumpBufferActive()
    {
        return _jumpBufferTimer < _jumpBufferDuration;
    }

    private void _UpdateJumpBuffer()
    {
        if (!IsJumpBufferActive()) return;
        _jumpBufferTimer += Time.deltaTime;
    }

    private void _CancelJumpBuffer()
    {
        _jumpBufferTimer = _jumpBufferDuration;
    }
    
    #endregion
    #region coyote Time

    private void _UpdateCoyoteTime()
    {
        if (!_IsCoyoteActive()) return;
        _coyoteTimeCooldown -= Time.deltaTime;
    }

    private bool _IsCoyoteActive()
    {
        return _coyoteTimeCooldown > 0f;
    }

    private void _ResetCoyoteTime()
    {
        _coyoteTimeCooldown = _coyoteTimeDuration;
    }

    private bool _EntityHasExitGround()
    {
        return _entityWasTouchingGround && !_entity.IsTouchingGround;
    }
    
    
    #endregion
    #endregion
}

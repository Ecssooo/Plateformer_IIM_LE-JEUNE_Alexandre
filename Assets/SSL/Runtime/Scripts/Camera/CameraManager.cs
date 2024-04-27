using System;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    
    public static CameraManager Instance { get; private set; }
    
    [Header("Camera")]
    [SerializeField] private Camera _camera;
    
    [Header("Player Offset")]
    [SerializeField] private Transform _playerTransform;

    [SerializeField] private CameraProfileTransition _offsetTransition;

    [Header("Profile System")]
    [SerializeField] private CameraProfile _defaultCameraProfile;
    private CameraProfile _currentCameraProfile;
    //Transition
    private float _profileTransitionTimer = 0f;
    private float _profileTransitionDuration = 0f;
    private Vector3 _profileTransitionStartPosition;
    private float _profileTransitionStartSize;
    //Follow
    private Vector3 _profileLastFollowDestination;
    //Damping
    private Vector3 _dampedPosition;
    //Auto Scroll
    private Vector3 _profileAutoScrollDestination; 
    
    #region Update
    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        _InitToDefaultProfile();
    }

    private void Update()
    {
        Vector3 nextPosition = _FindCameraNextPosition(_offsetTransition);
        nextPosition = _ClampPositionIntoBounds(nextPosition);
        nextPosition = _ApplyDamping(nextPosition);
        
        
        if (_IsPlayingProfileTransition())
        {
            _profileTransitionTimer += Time.deltaTime;
            Vector3 transitionPosition = _CalculateProfileTransitionCameraPosition(nextPosition);
            _SetCameraPosition(transitionPosition);
            float transitionSize = _CalculateProfileTransitionCameraSize(_currentCameraProfile.CameraSize);
            _SetCameraSize(transitionSize);
        }
        else
        {
            _SetCameraPosition(nextPosition);
            _SetCameraSize(_currentCameraProfile.CameraSize);
        }
    }
    #endregion
    #region Init / Set 
    private void _InitToDefaultProfile()
    {
        _currentCameraProfile = _defaultCameraProfile;
        _SetCameraPosition(_currentCameraProfile.position);
        _SetCameraSize(_currentCameraProfile.CameraSize);
        _SetCameraDampedPosition(_ClampPositionIntoBounds(_FindCameraNextPosition(_offsetTransition)));
    }
    
    private void _SetCameraPosition(Vector3 position)
    {
        Vector3 newCameraPosition = _camera.transform.position;
        newCameraPosition.x = position.x;
        newCameraPosition.y = position.y;
        _camera.transform.position = newCameraPosition;
    }

    private void _SetCameraSize(float size)
    {
        _camera.orthographicSize = size;
    }

    private void _SetCameraDampedPosition(Vector3 position)
    {
        _dampedPosition.x = position.x;
        _dampedPosition.y = position.y;
    }
    #endregion
    #region Collider
    public void EnterProfile(CameraProfile cameraProfile,CameraProfileTransition transition = null)
    {
        _currentCameraProfile = cameraProfile;
        if(transition != null)
            _PlayProfileTransition(transition);
        _SetCameraDampedPosition(_FindCameraNextPosition(_offsetTransition));
    }

    public void ExitProfile(CameraProfile cameraProfile, CameraProfileTransition transition = null)
    {
        if (_currentCameraProfile != cameraProfile) return;
        _currentCameraProfile = _defaultCameraProfile;
        if(transition != null)
            _PlayProfileTransition(transition);
        _SetCameraDampedPosition(_FindCameraNextPosition(_offsetTransition));
    }

    #endregion
    #region Transition
    private void _PlayProfileTransition(CameraProfileTransition transition)
    {
        _profileTransitionStartPosition = _camera.transform.position;
        _profileTransitionStartSize = _camera.orthographicSize;
        _profileTransitionTimer = 0f;
        _profileTransitionDuration = transition.duration;
    }

    private bool _IsPlayingProfileTransition()
    {
        return _profileTransitionTimer < _profileTransitionDuration;
    }

    private float _CalculateProfileTransitionCameraSize(float endSize)
    {
        float percent = _profileTransitionTimer / _profileTransitionDuration;
        float startSize = _profileTransitionStartSize;
        return Mathf.Lerp(startSize, endSize, percent);
    }
    private Vector3 _CalculateProfileTransitionCameraPosition(Vector3 endPosition)
    {
        float percent = _profileTransitionTimer / _profileTransitionDuration;
        Vector3 origin = _profileTransitionStartPosition;
        return Vector3.Lerp(origin, endPosition, percent);
    }
    #endregion
    
    private Vector3 _FindCameraNextPosition(CameraProfileTransition transition = null)
    {
        if (_currentCameraProfile.ProfileType == CameraProfileType.FollowTarget)
        {
            if (_currentCameraProfile.TargetToFollow != null)
            {
                CameraFollowable targetToFollow = _currentCameraProfile.TargetToFollow;
                if (HeroEntity._orientX == 1f)
                {
                    _profileLastFollowDestination.x = targetToFollow.FollowPositionX + _currentCameraProfile.FollowOffsetX;
                    _profileLastFollowDestination.y = targetToFollow.FollowPositionY;
                    if(transition != null)
                        _PlayProfileTransition(transition);
                }
                else
                {
                    _profileLastFollowDestination.x = targetToFollow.FollowPositionX - _currentCameraProfile.FollowOffsetX;
                    _profileLastFollowDestination.y = targetToFollow.FollowPositionY;
                    if(transition != null)
                        _PlayProfileTransition(transition);
                }
                return _profileLastFollowDestination;
            }
        }
        //AutoScroll
        else if (_currentCameraProfile.ProfileType == CameraProfileType.AutoScroll)
        {
            _profileAutoScrollDestination.x = _camera.transform.position.x + _currentCameraProfile.AutoScrollHorizontalSpeed * Time.fixedDeltaTime;
            _profileAutoScrollDestination.y = _camera.transform.position.y + _currentCameraProfile.AutoScrollVerticalSpeed * Time.fixedDeltaTime;
            return _profileAutoScrollDestination;
        }
        return _currentCameraProfile.position;
    }

    #region Damping
    private Vector3 _ApplyDamping(Vector3 position)
    {
        if (_currentCameraProfile.UseDampingHorizontally)
        {
            _dampedPosition.x = Mathf.Lerp(_dampedPosition.x,
                position.x,
                _currentCameraProfile.HorizontalDampingFactor * Time.deltaTime
                );
        }
        else
        {
            _dampedPosition.x = position.x;
        }
        if (_currentCameraProfile.UseDampingVertically)
        {
            _dampedPosition.y = Mathf.Lerp(_dampedPosition.y,
                position.y,
                _currentCameraProfile.VerticalDampingFactor * Time.deltaTime
            );
        }
        else
        {
            _dampedPosition.y = position.y;
        }

        return _dampedPosition;
    }
    #endregion
    #region Bounds
    private Vector3 _ClampPositionIntoBounds(Vector3 position)
    {
        if (!_currentCameraProfile.HasBounds) return position;
        
        Rect boundsRect = _currentCameraProfile.BoundsRect;
        Vector3 worldBottomLeft = _camera.ScreenToWorldPoint(new Vector3(0f, 0f));
        Vector3 worldTopRight = _camera.ScreenToWorldPoint(new Vector3(_camera.pixelWidth, _camera.pixelHeight));
        Vector2 worldScreenSize = new Vector2(worldTopRight.x - worldBottomLeft.x, worldTopRight.y - worldBottomLeft.y);
        Vector2 worldHalfScreenSize = worldScreenSize / 2f;

        if (position.x > boundsRect.xMax - worldHalfScreenSize.x)
        {
            position.x = boundsRect.xMax - worldHalfScreenSize.x;
        }

        if (position.x < boundsRect.xMin + worldHalfScreenSize.x)
        {
            position.x = boundsRect.xMin + worldHalfScreenSize.x;
        }

        if (position.y > boundsRect.yMax - worldHalfScreenSize.y)
        {
            position.y = boundsRect.yMax - worldHalfScreenSize.y;
        }

        if (position.y < boundsRect.yMin + worldHalfScreenSize.y)
        {
            position.y = boundsRect.yMin + worldHalfScreenSize.y;
        }
        return position;
    }
    #endregion
    
    
    
    
    /* Lerp example sans Mathf.lerp
    private float _CalculateProfileTransitionCameraSize(float endsize)
    {
        float percent = _profileTransitionTimer /              _profileTransitionDuration;
        percent = Mathf.Clamp01(percent);

        float startSize = _profileTransitionStartSize;
        return startSize + (endsize - startSize) * percent;
    }
*/
}
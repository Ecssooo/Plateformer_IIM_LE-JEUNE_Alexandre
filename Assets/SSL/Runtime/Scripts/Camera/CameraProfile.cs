using System;
using UnityEngine;

public enum CameraProfileType
{
    Static = 0,
    FollowTarget
}

public class CameraProfile : MonoBehaviour
{
    private Camera _camera;
    public float CameraSize => _camera.orthographicSize;
    public Vector3 position => _camera.transform.position;



    [Header("Type")] 
    [SerializeField] private CameraProfileType _profileType = CameraProfileType.Static;

    [Header("Follow")] 
    [SerializeField] private CameraFollowable _targetToFollow = null;

    [Header("Damping")] 
    [SerializeField] private bool _useDampingHorizontally;
    [SerializeField] private float _horizontalDampingFactor = 5f;
    [SerializeField] private bool _useDampingVertically;
    [SerializeField] private float _verticalDampingFactor = 5f;

    public bool UseDampingHorizontally => _useDampingHorizontally;
    public float HorizontalDampingFactor => _horizontalDampingFactor;
    public bool UseDampingVertically => _useDampingVertically;
    public float VerticalDampingFactor => _verticalDampingFactor;
    
    public CameraProfileType ProfileType => _profileType;
    public CameraFollowable TargetToFollow => _targetToFollow;
    
    
    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera != null)
            _camera.enabled = false;
    }
}
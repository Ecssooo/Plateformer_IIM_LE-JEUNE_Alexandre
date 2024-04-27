using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class GroundDetector : MonoBehaviour
{
    [Header("Detection")]

    [SerializeField] private Transform[] _detectionPoints;
    [SerializeField] private float _detectionLenght = 0.1f;
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private LayerMask _groundWallLayerMask;

    public bool DetectGroundNearBy()
    {
        foreach(Transform detectionPoint in _detectionPoints)
        {
            RaycastHit2D hitResult = Physics2D.Raycast(
                detectionPoint.position,
                Vector2.down,
                _detectionLenght,
                _groundLayerMask);
            RaycastHit2D hitResult2 = Physics2D.Raycast(
                detectionPoint.position,
                Vector2.down,
                _detectionLenght,
                _groundWallLayerMask);
            if(hitResult.collider != null || hitResult2.collider != null)
            {
                return true;
            }
        }
        return false;
    }
}

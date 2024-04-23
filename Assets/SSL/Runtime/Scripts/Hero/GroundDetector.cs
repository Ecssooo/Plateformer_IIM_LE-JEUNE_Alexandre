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

    public bool DetectGroundNeayBy()
    {
        foreach(Transform detectionPoint in _detectionPoints)
        {
            RaycastHit2D hitResult = Physics2D.Raycast(
                detectionPoint.position,
                Vector2.down,
                _detectionLenght,
                _groundLayerMask);
            if(hitResult.collider != null)
            {
                return true;
            }
        }
        return false;
    }
}

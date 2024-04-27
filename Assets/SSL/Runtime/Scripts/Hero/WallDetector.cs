using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class WallDetector : MonoBehaviour
{
    [Header("Detection")]

    [SerializeField] private Transform[] _detectionPointsRight;
    [SerializeField] private Transform[] _detectionPointsLeft;
    [SerializeField] private float _detectionLenght = 0.1f;
    [SerializeField] private LayerMask _wallLayerMask;
    [SerializeField] private LayerMask _groundWallLayerMask;

    public bool DetectWallNearByRight()
    {
        foreach(Transform detectionPointRight in _detectionPointsRight)
        {
            RaycastHit2D hitResult = Physics2D.Raycast(
                detectionPointRight.position,
                Vector2.right,
                _detectionLenght,
                _wallLayerMask);
            RaycastHit2D hitResult2 = Physics2D.Raycast(
                detectionPointRight.position,
                Vector2.right,
                _detectionLenght,
                _groundWallLayerMask);
            if(hitResult.collider != null || hitResult2.collider != null)
            {
                return true;
            }
        }
        return false;
    }

    public bool DetectWallNearByLeft()
    {
        foreach (Transform detectionPointLeft in _detectionPointsLeft)
        {
            RaycastHit2D hitResult = Physics2D.Raycast(
                detectionPointLeft.position,
                Vector2.left,
                _detectionLenght,
                _wallLayerMask);
            RaycastHit2D hitResult2 = Physics2D.Raycast(
                detectionPointLeft.position,
                Vector2.left,
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
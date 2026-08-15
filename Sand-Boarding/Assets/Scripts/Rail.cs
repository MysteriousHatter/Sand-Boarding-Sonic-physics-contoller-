using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

public class Rail : MonoBehaviour
{

    private EdgeCollider2D edgeCollider;
    private Vector2[] railPoints;

    private void Awake()
    {
        // Get the EdgeCollider2D component attached to this GameObject
        edgeCollider = GetComponent<EdgeCollider2D>();

        if (edgeCollider != null)
        {
            // Copy all points from the EdgeCollider2D
            railPoints = edgeCollider.points;
        }
        else
        {
            Debug.LogError("EdgeCollider2D not found on the GameObject.");
        }
    }

    // Method to get all points on the rail
    public Vector2[] GetRailPoints()
    {
        return railPoints;
    }
}


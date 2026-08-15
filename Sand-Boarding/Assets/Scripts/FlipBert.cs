using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlipBert : MonoBehaviour
{

   private CircleCollider2D myCircleCollider;
    private PlayerController myPlayerController;
    private void Start()
    {
        myCircleCollider = GetComponent<CircleCollider2D>();
        myPlayerController = GetComponentInParent<PlayerController>();
    }


    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Check if the top collider touches the ground or any obstacle
        if (collision.gameObject.CompareTag("Ground"))
        {
            myPlayerController.isUpsideDown = true;
            Debug.Log("Player is upside down! " + collision.collider.name);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        // Reset the flag if the collider leaves the ground
        if (collision.gameObject.CompareTag("Ground"))
        {
            myPlayerController.isUpsideDown = false;
        }
    }
}

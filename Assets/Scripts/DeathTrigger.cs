using UnityEngine;

public class DeathTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        PlayerMovement player = collision.GetComponentInParent<PlayerMovement>();
        if (player != null)
        {
            player.Die();
        }
    }
}


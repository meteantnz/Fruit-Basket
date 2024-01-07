using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BasketKontroller : MonoBehaviour
{
    
    public float speed;
    [SerializeField]
    Rigidbody2D rb;
    void Start()
    {

    }

    void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        Movement(horizontal);
    }

    void Movement(float horizontal)
    {
        rb.velocity = new Vector2(horizontal * speed, rb.velocity.y);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Duvar"))
        {
            Debug.Log("Temas etti");
        }
    }
    
}

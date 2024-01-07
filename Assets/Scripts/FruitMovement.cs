using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitMovement : MonoBehaviour
{
    public float movementSpeed;
    GameUI gameUI;

    private void Start()
    {
        gameUI=FindObjectOfType<GameUI>();
    }
    void Update()
    {
        MeyveHareketEt();
        
    }

    void MeyveHareketEt()
    {
        // Meyveyi a�a�� do�ru hareket ettir
        transform.Translate(Vector3.down * movementSpeed * Time.deltaTime);

        // E�er meyve a�a��da belirli bir y�ksekli�in alt�na d��erse, meyveyi yok et
        if (transform.position.y < -6f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Basket"))
        {
            Destroy(gameObject);
            gameUI.ScoreCounter(5);
        }
    }
}

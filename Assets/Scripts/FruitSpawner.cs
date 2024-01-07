using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    [SerializeField]
    GameObject fruitPrefab;
    public float minX = -2.39f; // Minimum X koordinat�
    public float maxX = 2.39f;  // Maximum X koordinat�
    public float ySpawnHeight = 5f; // Y spawn y�ksekli�i

    public int meyveAdeti = 10; // Spawn edilecek meyve say�s�
    public float spawnInterval = 1.0f; // Spawn aral��� (her meyve aras�ndaki zaman)

    private int meyveIndex = 0; // Spawn edilen meyve say�s�n� takip etmek i�in

    void Start()
    {
        InvokeRepeating("SpawnMeyve", 0f, spawnInterval);
    }

    void SpawnMeyve()
    {
        if (meyveIndex < meyveAdeti)
        {
            // Belirtilen x ekseni aral���nda rasgele koordinat se�me
            float randomX = Random.Range(minX, maxX);

            // Y spawn y�ksekli�inde meyve spawn et
            Vector3 spawnPosition = new Vector3(randomX, ySpawnHeight, 0f);
            Instantiate(fruitPrefab, spawnPosition, Quaternion.identity);

            meyveIndex++;
        }
    }
}

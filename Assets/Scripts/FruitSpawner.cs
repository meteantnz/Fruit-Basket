using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FruitSpawner : MonoBehaviour
{
    [SerializeField]
    GameObject fruitPrefab;
    public float minX = -2.39f; // Minimum X koordinatý
    public float maxX = 2.39f;  // Maximum X koordinatý
    public float ySpawnHeight = 5f; // Y spawn yüksekliði

    public int meyveAdeti = 10; // Spawn edilecek meyve sayýsý
    public float spawnInterval = 1.0f; // Spawn aralýðý (her meyve arasýndaki zaman)

    private int meyveIndex = 0; // Spawn edilen meyve sayýsýný takip etmek için

    void Start()
    {
        InvokeRepeating("SpawnMeyve", 0f, spawnInterval);
    }

    void SpawnMeyve()
    {
        if (meyveIndex < meyveAdeti)
        {
            // Belirtilen x ekseni aralýðýnda rasgele koordinat seçme
            float randomX = Random.Range(minX, maxX);

            // Y spawn yüksekliðinde meyve spawn et
            Vector3 spawnPosition = new Vector3(randomX, ySpawnHeight, 0f);
            Instantiate(fruitPrefab, spawnPosition, Quaternion.identity);

            meyveIndex++;
        }
    }
}

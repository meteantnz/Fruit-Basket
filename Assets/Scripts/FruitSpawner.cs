//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class FruitSpawner : MonoBehaviour
//{
//    [SerializeField]
//    GameObject fruitPrefab;
//    public float minX = -2.39f; // Minimum X koordinatý
//    public float maxX = 2.39f;  // Maximum X koordinatý
//    public float ySpawnHeight = 5f; // Y spawn yüksekliði

//    public int meyveAdeti = 10; // Spawn edilecek meyve sayýsý
//    public float spawnInterval = 1.0f; // Spawn aralýðý (her meyve arasýndaki zaman)

//    private int meyveIndex = 0; // Spawn edilen meyve sayýsýný takip etmek için

//    void Start()
//    {
//        InvokeRepeating("SpawnMeyve", 0f, spawnInterval);
//    }

//    void SpawnMeyve()
//    {
//        if (meyveIndex < meyveAdeti)
//        {
//            // Belirtilen x ekseni aralýðýnda rasgele koordinat seçme
//            float randomX = Random.Range(minX, maxX);

//            // Y spawn yüksekliðinde meyve spawn et
//            Vector3 spawnPosition = new Vector3(randomX, ySpawnHeight, 0f);
//            Instantiate(fruitPrefab, spawnPosition, Quaternion.identity);

//            meyveIndex++;
//        }
//    }
//}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Meyveleri temsil eden sýnýf
[System.Serializable]
public class Fruit
{
    public GameObject prefab;   // Meyve prefabý
    public float spawnProbability;   // Meyve spawn olasýlýðý
    public int score;   // Meyvenin puan deðeri
}

public class FruitSpawner : MonoBehaviour
{
    [SerializeField]
    List<Fruit> fruitList;   // Meyvelerin listesi

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

            // Rasgele meyve seçimi
            Fruit selectedFruit = ChooseRandomFruit();

            // Y spawn yüksekliðinde meyve spawn et
            Vector3 spawnPosition = new Vector3(randomX, ySpawnHeight, 0f);
            Instantiate(selectedFruit.prefab, spawnPosition, Quaternion.identity);

            meyveIndex++;
        }
    }

    Fruit ChooseRandomFruit()
    {
        float totalProbability = 0f;

        // Toplam olasýlýðý hesapla
        foreach (var fruit in fruitList)
        {
            totalProbability += fruit.spawnProbability;
        }

        // Rastgele bir sayý seç ve meyve tipini belirle
        float randomValue = Random.Range(0f, totalProbability);
        float cumulativeProbability = 0f;

        foreach (var fruit in fruitList)
        {
            cumulativeProbability += fruit.spawnProbability;
            if (randomValue <= cumulativeProbability)
            {
                return fruit;
            }
        }

        // Buraya gelindiði durumda bir hata oluþtu demektir, ilk meyveyi döndür
        return fruitList[0];
    }
}


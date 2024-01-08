//using System.Collections;
//using System.Collections.Generic;
//using UnityEngine;

//public class FruitSpawner : MonoBehaviour
//{
//    [SerializeField]
//    GameObject fruitPrefab;
//    public float minX = -2.39f; // Minimum X koordinat�
//    public float maxX = 2.39f;  // Maximum X koordinat�
//    public float ySpawnHeight = 5f; // Y spawn y�ksekli�i

//    public int meyveAdeti = 10; // Spawn edilecek meyve say�s�
//    public float spawnInterval = 1.0f; // Spawn aral��� (her meyve aras�ndaki zaman)

//    private int meyveIndex = 0; // Spawn edilen meyve say�s�n� takip etmek i�in

//    void Start()
//    {
//        InvokeRepeating("SpawnMeyve", 0f, spawnInterval);
//    }

//    void SpawnMeyve()
//    {
//        if (meyveIndex < meyveAdeti)
//        {
//            // Belirtilen x ekseni aral���nda rasgele koordinat se�me
//            float randomX = Random.Range(minX, maxX);

//            // Y spawn y�ksekli�inde meyve spawn et
//            Vector3 spawnPosition = new Vector3(randomX, ySpawnHeight, 0f);
//            Instantiate(fruitPrefab, spawnPosition, Quaternion.identity);

//            meyveIndex++;
//        }
//    }
//}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Meyveleri temsil eden s�n�f
[System.Serializable]
public class Fruit
{
    public GameObject prefab;   // Meyve prefab�
    public float spawnProbability;   // Meyve spawn olas�l���
    public int score;   // Meyvenin puan de�eri
}

public class FruitSpawner : MonoBehaviour
{
    [SerializeField]
    List<Fruit> fruitList;   // Meyvelerin listesi

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

            // Rasgele meyve se�imi
            Fruit selectedFruit = ChooseRandomFruit();

            // Y spawn y�ksekli�inde meyve spawn et
            Vector3 spawnPosition = new Vector3(randomX, ySpawnHeight, 0f);
            Instantiate(selectedFruit.prefab, spawnPosition, Quaternion.identity);

            meyveIndex++;
        }
    }

    Fruit ChooseRandomFruit()
    {
        float totalProbability = 0f;

        // Toplam olas�l��� hesapla
        foreach (var fruit in fruitList)
        {
            totalProbability += fruit.spawnProbability;
        }

        // Rastgele bir say� se� ve meyve tipini belirle
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

        // Buraya gelindi�i durumda bir hata olu�tu demektir, ilk meyveyi d�nd�r
        return fruitList[0];
    }
}


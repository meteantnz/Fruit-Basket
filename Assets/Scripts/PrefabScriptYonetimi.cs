using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PrefabScriptYonetimi : EditorWindow
{
    private MonoScript eklenenScript;
    private Vector2 kaydirmaPozisyonu;
    private List<GameObject> yuklenenPrefablar = new List<GameObject>();

    [MenuItem("Window/Prefab Script Yönetimi")]
    static void Init()
    {
        PrefabScriptYonetimi pencere = (PrefabScriptYonetimi)EditorWindow.GetWindow(typeof(PrefabScriptYonetimi));
        pencere.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Prefab Script Yönetimi", EditorStyles.boldLabel);

        eklenenScript = (MonoScript)EditorGUILayout.ObjectField("Eklenecek/Çýkarýlacak Script", eklenenScript, typeof(MonoScript), false);

        GUILayout.Label("Yüklenen Prefablar", EditorStyles.boldLabel);

        kaydirmaPozisyonu = EditorGUILayout.BeginScrollView(kaydirmaPozisyonu);

        for (int i = yuklenenPrefablar.Count - 1; i >= 0; i--)
        {
            GameObject prefab = yuklenenPrefablar[i];

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
            if (GUILayout.Button("Kaldýr", GUILayout.Width(70)))
            {
                PrefabiKaldir(prefab);
            }
            EditorGUILayout.EndHorizontal();
        }


        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Seçilen Prefablara Script Ekle"))
        {
            SecilenPrefablaraScriptEkle();
        }

        if (GUILayout.Button("Seçilen Prefablardan Script Çýkar"))
        {
            SecilenPrefablardanScriptCikar();
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        GUILayout.Label("Prefablarý buraya sürükleyin:", EditorStyles.boldLabel);
        Rect suruklemeAlani = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        EditorGUI.DropShadowLabel(suruklemeAlani, "Prefablarý buraya sürükleyin");

        Event mevcutEvent = Event.current;
        if (mevcutEvent.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
        }
        else if (mevcutEvent.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();

            foreach (Object suruklenenObj in DragAndDrop.objectReferences)
            {
                GameObject suruklenenPrefab = suruklenenObj as GameObject;
                if (suruklenenPrefab != null && PrefabUtility.IsPartOfPrefabAsset(suruklenenPrefab))
                {
                    if (!yuklenenPrefablar.Contains(suruklenenPrefab))
                    {
                        yuklenenPrefablar.Add(suruklenenPrefab);
                    }
                }
            }
        }
    }

    void SecilenPrefablaraScriptEkle()
    {
        if (eklenenScript == null)
        {
            EditorUtility.DisplayDialog("Hata", "Script seçilmelidir.", "OK");
            return;
        }

        foreach (GameObject prefab in yuklenenPrefablar)
        {
            PrefabaScriptEkle(prefab, eklenenScript);
        }

        EditorUtility.DisplayDialog("Baþarý", "Script seçilen prefablara eklendi.", "OK");
    }

    void SecilenPrefablardanScriptCikar()
    {
        if (eklenenScript == null)
        {
            EditorUtility.DisplayDialog("Hata", "Script seçilmelidir.", "OK");
            return;
        }

        foreach (GameObject prefab in yuklenenPrefablar)
        {
            PrefabdanScriptCikar(prefab, eklenenScript);
        }

        EditorUtility.DisplayDialog("Baþarý", "Script seçilen prefablardan çýkarýldý.", "OK");
    }

    void PrefabaScriptEkle(GameObject prefabObje, MonoScript script)
    {
        if (prefabObje != null && script != null)
        {
            MonoBehaviour varolanScript = prefabObje.GetComponent(script.GetClass()) as MonoBehaviour;

            if (varolanScript == null)
            {
                prefabObje.AddComponent(script.GetClass());
            }
            else
            {
                Debug.LogWarning($"Prefab {prefabObje.name} zaten script {script.name} içeriyor.");
            }
        }
    }

    void PrefabdanScriptCikar(GameObject prefabObje, MonoScript script)
    {
        if (prefabObje != null && script != null)
        {
            MonoBehaviour varolanScript = prefabObje.GetComponent(script.GetClass()) as MonoBehaviour;

            if (varolanScript != null)
            {
                DestroyImmediate(varolanScript, true);
            }
            else
            {
                Debug.LogWarning($"Prefab {prefabObje.name} script {script.name} içermiyor.");
            }
        }
    }

    void PrefabiKaldir(GameObject prefab)
    {
        if (yuklenenPrefablar.Contains(prefab))
        {
            yuklenenPrefablar.Remove(prefab);
        }
    }
}



using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PrefabScriptYonetimi : EditorWindow
{
    private MonoScript eklenenScript;
    private Vector2 kaydirmaPozisyonu;
    private List<GameObject> yuklenenPrefablar = new List<GameObject>();

    [MenuItem("Window/Prefab Script Y�netimi")]
    static void Init()
    {
        PrefabScriptYonetimi pencere = (PrefabScriptYonetimi)EditorWindow.GetWindow(typeof(PrefabScriptYonetimi));
        pencere.Show();
    }

    void OnGUI()
    {
        GUILayout.Label("Prefab Script Y�netimi", EditorStyles.boldLabel);

        eklenenScript = (MonoScript)EditorGUILayout.ObjectField("Eklenecek/��kar�lacak Script", eklenenScript, typeof(MonoScript), false);

        GUILayout.Label("Y�klenen Prefablar", EditorStyles.boldLabel);

        kaydirmaPozisyonu = EditorGUILayout.BeginScrollView(kaydirmaPozisyonu);

        for (int i = yuklenenPrefablar.Count - 1; i >= 0; i--)
        {
            GameObject prefab = yuklenenPrefablar[i];

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
            if (GUILayout.Button("Kald�r", GUILayout.Width(70)))
            {
                PrefabiKaldir(prefab);
            }
            EditorGUILayout.EndHorizontal();
        }


        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        GUILayout.BeginHorizontal();

        if (GUILayout.Button("Se�ilen Prefablara Script Ekle"))
        {
            SecilenPrefablaraScriptEkle();
        }

        if (GUILayout.Button("Se�ilen Prefablardan Script ��kar"))
        {
            SecilenPrefablardanScriptCikar();
        }

        GUILayout.EndHorizontal();

        EditorGUILayout.Space();

        GUILayout.Label("Prefablar� buraya s�r�kleyin:", EditorStyles.boldLabel);
        Rect suruklemeAlani = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        EditorGUI.DropShadowLabel(suruklemeAlani, "Prefablar� buraya s�r�kleyin");

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
            EditorUtility.DisplayDialog("Hata", "Script se�ilmelidir.", "OK");
            return;
        }

        foreach (GameObject prefab in yuklenenPrefablar)
        {
            PrefabaScriptEkle(prefab, eklenenScript);
        }

        EditorUtility.DisplayDialog("Ba�ar�", "Script se�ilen prefablara eklendi.", "OK");
    }

    void SecilenPrefablardanScriptCikar()
    {
        if (eklenenScript == null)
        {
            EditorUtility.DisplayDialog("Hata", "Script se�ilmelidir.", "OK");
            return;
        }

        foreach (GameObject prefab in yuklenenPrefablar)
        {
            PrefabdanScriptCikar(prefab, eklenenScript);
        }

        EditorUtility.DisplayDialog("Ba�ar�", "Script se�ilen prefablardan ��kar�ld�.", "OK");
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
                Debug.LogWarning($"Prefab {prefabObje.name} zaten script {script.name} i�eriyor.");
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
                Debug.LogWarning($"Prefab {prefabObje.name} script {script.name} i�ermiyor.");
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



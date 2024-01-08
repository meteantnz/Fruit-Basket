using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class PrefabInspectorWindow : EditorWindow
{
    private List<GameObject> selectedPrefabs = new List<GameObject>();
    private List<Component> selectedComponents = new List<Component>(); // Se�ili GameObject'lerin t�m bile�enleri
    private float sharedValue = 0.0f;
    private Vector2 scrollPosition = Vector2.zero;

    [MenuItem("Window/Prefab Inspector")]
    public static void ShowWindow()
    {
        GetWindow<PrefabInspectorWindow>("Prefab Inspector");
    }

    private void OnGUI()
    {
        Event e = Event.current;

        GUILayout.Label("Prefab'lar� Se�in ve Pencereye B�rak�n", EditorStyles.boldLabel);

        // S�r�kle ve b�rak kontrol�
        if (e.type == EventType.DragUpdated || e.type == EventType.DragPerform)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

            if (e.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();

                foreach (Object draggedObject in DragAndDrop.objectReferences)
                {
                    if (draggedObject is GameObject)
                    {
                        GameObject draggedPrefab = draggedObject as GameObject;

                        if (!selectedPrefabs.Contains(draggedPrefab))
                        {
                            selectedPrefabs.Add(draggedPrefab);
                            // Se�ilen prefablar�n t�m bile�enlerini al
                            Component[] components = draggedPrefab.GetComponents<Component>();
                            selectedComponents.AddRange(components);
                        }
                    }
                }

                Event.current.Use();
            }
        }

        GUILayout.Space(20); // Prefab'lar ile ilk ��e aras�na bir bo�luk ekleyin

        GUILayout.Label("Script Bile�eni Se�in", EditorStyles.boldLabel);

        // Scroll view ba�lat�l�yor
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (GameObject selectedPrefab in selectedPrefabs)
        {
            if (selectedPrefab == null)
                continue;

            GUILayout.BeginVertical();

            // Prefab'�n ismini mavi kutu i�inde g�ster
            EditorGUILayout.ObjectField(selectedPrefab, typeof(GameObject), true);

            // Prefab isminin yan�nda "Kald�r" butonu
            if (GUILayout.Button("Kald�r", GUILayout.Width(80), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                RemoveSelectedPrefab(selectedPrefab);
                // �terasyon s�ras�nda listeden bir ��e kald�r�ld���nda, indeksi g�ncelleyelim.
                break;
            }

            GUILayout.Space(10);

            foreach (Component component in selectedComponents)
            {
                SerializedObject serializedObject = new SerializedObject(component);
                SerializedProperty iterator = serializedObject.GetIterator();

                while (iterator.NextVisible(true))
                {
                    EditorGUILayout.PropertyField(iterator, true);
                }

                serializedObject.ApplyModifiedProperties();

                GUILayout.Space(10);
            }

            GUILayout.Space(20); // Her prefab aras�na bir bo�luk ekleyin

            GUILayout.EndVertical();

            GUILayout.Space(10);
        }

        // Scroll view sonland�r�l�yor
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        GUILayout.Label("Payla��lan De�er", EditorStyles.boldLabel);
        sharedValue = EditorGUILayout.FloatField(sharedValue);

        GUILayout.Space(10);

        if (GUILayout.Button("Uygula"))
        {
            ApplyChangesToSelectedPrefabs();
        }

        GUILayout.Space(10);
    }

    private void ApplyChangesToSelectedPrefabs()
    {
        foreach (Component component in selectedComponents)
        {
            SerializedObject serializedObject = new SerializedObject(component);
            SerializedProperty iterator = serializedObject.GetIterator();

            while (iterator.NextVisible(true))
            {
                if (iterator.propertyType != SerializedPropertyType.ObjectReference)
                {
                    iterator.floatValue = sharedValue;
                }
            }

            serializedObject.ApplyModifiedProperties();
        }

        foreach (GameObject selectedPrefab in selectedPrefabs)
        {
            if (selectedPrefab == null)
                continue;

            EditorUtility.SetDirty(selectedPrefab);
            PrefabUtility.RecordPrefabInstancePropertyModifications(selectedPrefab);
        }
    }

    private void RemoveSelectedPrefab(GameObject prefabToRemove)
    {
        selectedPrefabs.Remove(prefabToRemove);
        // Se�ilen prefablar�n t�m bile�enlerini listeden ��kar
        Component[] components = prefabToRemove.GetComponents<Component>();
        foreach (Component component in components)
        {
            selectedComponents.Remove(component);
        }

        // Prefab Asset'i i�indeki GameObject'u silmek i�in
        GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(prefabToRemove);
        DestroyImmediate(prefabRoot, true);
    }
}

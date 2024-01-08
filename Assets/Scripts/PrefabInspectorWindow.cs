using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class PrefabInspectorWindow : EditorWindow
{
    private List<GameObject> selectedPrefabs = new List<GameObject>();
    private List<Component> selectedComponents = new List<Component>(); // Seçili GameObject'lerin tüm bileþenleri
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

        GUILayout.Label("Prefab'larý Seçin ve Pencereye Býrakýn", EditorStyles.boldLabel);

        // Sürükle ve býrak kontrolü
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
                            // Seçilen prefablarýn tüm bileþenlerini al
                            Component[] components = draggedPrefab.GetComponents<Component>();
                            selectedComponents.AddRange(components);
                        }
                    }
                }

                Event.current.Use();
            }
        }

        GUILayout.Space(20); // Prefab'lar ile ilk öðe arasýna bir boþluk ekleyin

        GUILayout.Label("Script Bileþeni Seçin", EditorStyles.boldLabel);

        // Scroll view baþlatýlýyor
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (GameObject selectedPrefab in selectedPrefabs)
        {
            if (selectedPrefab == null)
                continue;

            GUILayout.BeginVertical();

            // Prefab'ýn ismini mavi kutu içinde göster
            EditorGUILayout.ObjectField(selectedPrefab, typeof(GameObject), true);

            // Prefab isminin yanýnda "Kaldýr" butonu
            if (GUILayout.Button("Kaldýr", GUILayout.Width(80), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                RemoveSelectedPrefab(selectedPrefab);
                // Ýterasyon sýrasýnda listeden bir öðe kaldýrýldýðýnda, indeksi güncelleyelim.
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

            GUILayout.Space(20); // Her prefab arasýna bir boþluk ekleyin

            GUILayout.EndVertical();

            GUILayout.Space(10);
        }

        // Scroll view sonlandýrýlýyor
        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        GUILayout.Label("Paylaþýlan Deðer", EditorStyles.boldLabel);
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
        // Seçilen prefablarýn tüm bileþenlerini listeden çýkar
        Component[] components = prefabToRemove.GetComponents<Component>();
        foreach (Component component in components)
        {
            selectedComponents.Remove(component);
        }

        // Prefab Asset'i içindeki GameObject'u silmek için
        GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(prefabToRemove);
        DestroyImmediate(prefabRoot, true);
    }
}

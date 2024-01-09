using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class PrefabInspectorWindow : EditorWindow
{
    private List<GameObject> selectedPrefabs = new List<GameObject>();
    private Component[] selectedComponents;
    private float sharedValue = 0.0f;
    private Vector2 scrollPosition = Vector2.zero;
    private int selectedColliderTypeIndex = 0;
    private bool isAddingCollider = true;

    private string[] colliderTypes = { "None", "Box Collider", "Sphere Collider", "Capsule Collider", "Mesh Collider", "Box Collider 2D", "Circle Collider 2D", "Edge Collider 2D", "Polygon Collider 2D" };

    [MenuItem("Window/Prefab Inspector")]
    public static void ShowWindow()
    {
        GetWindow<PrefabInspectorWindow>("Prefab Inspector");
    }

    private void OnGUI()
    {
        Event e = Event.current;

        GUILayout.Label("Prefab'larý Seçin ve Pencereye Býrakýn", EditorStyles.boldLabel);

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
                        }
                    }
                }

                Event.current.Use();
            }
        }

        GUILayout.Space(20);

        GUILayout.Label("Script Bileþeni Seçin", EditorStyles.boldLabel);

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        foreach (GameObject selectedPrefab in selectedPrefabs)
        {
            if (selectedPrefab == null)
                continue;

            GUILayout.BeginHorizontal();

            EditorGUILayout.ObjectField(selectedPrefab, typeof(GameObject), true);

            if (GUILayout.Button("Kaldýr", GUILayout.Width(80), GUILayout.Height(EditorGUIUtility.singleLineHeight)))
            {
                RemoveSelectedPrefab(selectedPrefab);
                break;
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            selectedComponents = selectedPrefab.GetComponents<Component>();

            foreach (Component component in selectedComponents)
            {
                if (component is MonoBehaviour)
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
            }

            GUILayout.Space(20);
        }

        EditorGUILayout.EndScrollView();

        GUILayout.Space(10);

        GUILayout.Label("Paylaþýlan Deðer", EditorStyles.boldLabel);
        sharedValue = EditorGUILayout.FloatField(sharedValue);

        GUILayout.Space(10);

        GUILayout.Label("Collider Ayarlarý", EditorStyles.boldLabel);

        GUILayout.BeginHorizontal();

        GUILayout.Label("Collider Türünü Seçin", GUILayout.Width(150));
        selectedColliderTypeIndex = EditorGUILayout.Popup(selectedColliderTypeIndex, colliderTypes);

        GUILayout.Space(10);

        if (GUILayout.Button(isAddingCollider ? "Collider Ekle" : "Collider Kaldýr"))
        {
            AddRemoveColliderToSelectedPrefabs();
        }

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (GUILayout.Button("Uygula"))
        {
            ApplyChangesToSelectedPrefabs();
        }

        GUILayout.Space(10);
    }

    private void AddRemoveColliderToSelectedPrefabs()
    {
        foreach (GameObject selectedPrefab in selectedPrefabs)
        {
            if (selectedPrefab == null)
                continue;

            string colliderType = colliderTypes[selectedColliderTypeIndex];

            if (colliderType != "None")
            {
                if (isAddingCollider)
                {
                    AddCollider(selectedPrefab, colliderType);
                }
                else
                {
                    RemoveCollider(selectedPrefab, colliderType);
                }
            }
        }
    }

    private void AddCollider(GameObject targetObject, string colliderType)
    {
        if (isAddingCollider)
        {
            switch (colliderType)
            {
                case "Box Collider":
                    targetObject.AddComponent<BoxCollider>();
                    break;
                case "Sphere Collider":
                    targetObject.AddComponent<SphereCollider>();
                    break;
                case "Capsule Collider":
                    targetObject.AddComponent<CapsuleCollider>();
                    break;
                case "Mesh Collider":
                    targetObject.AddComponent<MeshCollider>();
                    break;
                case "Box Collider 2D":
                    targetObject.AddComponent<BoxCollider2D>();
                    break;
                case "Circle Collider 2D":
                    targetObject.AddComponent<CircleCollider2D>();
                    break;
                case "Edge Collider 2D":
                    targetObject.AddComponent<EdgeCollider2D>();
                    break;
                case "Polygon Collider 2D":
                    targetObject.AddComponent<PolygonCollider2D>();
                    break;
            }
        }
    }

    private void RemoveCollider(GameObject targetObject, string colliderType)
    {
        Component colliderComponent = isAddingCollider ? (Component)targetObject.GetComponent<Collider>() : (Component)targetObject.GetComponent<Collider2D>();

        if (colliderComponent != null && colliderComponent.GetType().Name == colliderType.Replace(" 2D", ""))
        {
            DestroyImmediate(colliderComponent);
        }
    }

    private void ApplyChangesToSelectedPrefabs()
    {
        foreach (GameObject selectedPrefab in selectedPrefabs)
        {
            if (selectedPrefab == null)
                continue;

            Component[] components = selectedPrefab.GetComponents<Component>();

            foreach (Component component in components)
            {
                if (component is MonoBehaviour)
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

                    EditorUtility.SetDirty(selectedPrefab);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(selectedPrefab);
                }
            }
        }
    }

    private void RemoveSelectedPrefab(GameObject prefabToRemove)
    {
        selectedPrefabs.Remove(prefabToRemove);

        GameObject prefabRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(prefabToRemove);
        DestroyImmediate(prefabRoot, true);
    }
}
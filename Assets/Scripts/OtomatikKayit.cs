using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

public class CustomEditorWindow : EditorWindow
{
    private GameObject draggedGameObject;
    private List<MonoBehaviour> scriptComponents = new List<MonoBehaviour>();
    private bool[] showPropertyArray; // Her �zelli�in g�r�n�rl���n� saklamak i�in dizi
    private bool myToggleVariable = true; // �rnek bir Toggle de�i�keni

    [MenuItem("Window/�zel Edit�r Penceresi")]
    public static void ShowWindow()
    {
        GetWindow<CustomEditorWindow>("�zel Edit�r Penceresi");
    }

    private void OnGUI()
    {
        Event currentEvent = Event.current;

        GUILayout.Label("Objeyi Buraya S�r�kleyin", EditorStyles.boldLabel);

        Rect dropArea = new Rect(0, 0, position.width, position.height);

        switch (currentEvent.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (currentEvent.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    foreach (Object draggedObj in DragAndDrop.objectReferences)
                    {
                        draggedGameObject = draggedObj as GameObject;
                        ScriptleriTara();
                        Repaint();
                    }
                }

                Event.current.Use();
                break;
        }

        if (draggedGameObject != null)
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.ObjectField(draggedGameObject, typeof(GameObject), false);

            if (GUILayout.Button("Kald�r"))
            {
                draggedGameObject = null;
                scriptComponents.Clear();
                showPropertyArray = null;
            }

            GUILayout.EndHorizontal();

            // �rnek Toggle d��mesi
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("�rnek Toggle De�i�keni");
            myToggleVariable = EditorGUILayout.ToggleLeft("", myToggleVariable);
            GUILayout.EndHorizontal();

            if (showPropertyArray == null || showPropertyArray.Length != scriptComponents.Count)
            {
                showPropertyArray = new bool[scriptComponents.Count];
                for (int i = 0; i < showPropertyArray.Length; i++)
                {
                    showPropertyArray[i] = true;
                }
            }

            for (int i = 0; i < scriptComponents.Count; i++)
            {
                ScriptComponentInceleyici(scriptComponents[i], showPropertyArray[i]);
            }
        }
    }

    private void ScriptleriTara()
    {
        if (draggedGameObject != null)
        {
            scriptComponents.Clear();
            MonoBehaviour[] scripts = draggedGameObject.GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour script in scripts)
            {
                scriptComponents.Add(script);
            }
        }
    }

    private void ScriptComponentInceleyici(MonoBehaviour scriptComponent, bool showAllProperties)
    {
        if (scriptComponent == null)
            return;

        System.Type scriptType = scriptComponent.GetType();

        // T�m alanlar� al, bu alanlar private ve public i�erecek �ekilde
        System.Reflection.FieldInfo[] fields = scriptType.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        foreach (var fieldInfo in fields)
        {
            if (showAllProperties || GetPropertyVisibility(scriptComponent, fieldInfo.Name))
            {
                GUILayout.BeginHorizontal();

                bool showProperty = GetPropertyVisibility(scriptComponent, fieldInfo.Name);
                bool newVisibility = EditorGUILayout.ToggleLeft(fieldInfo.Name, showProperty);

                if (newVisibility != showProperty)
                {
                    SetPropertyVisibility(scriptComponent, fieldInfo.Name, newVisibility);
                }

                GUILayout.EndHorizontal();

                // E�er alan g�sterilmeliyse, onu g�ster
                if (showAllProperties || newVisibility)
                {
                    object fieldValue = fieldInfo.GetValue(scriptComponent);

                    EditorGUI.BeginChangeCheck();

                    // Alan�n t�r�n� kontrol et ve uygun alan t�r�n� kullan
                    if (fieldInfo.FieldType == typeof(int))
                    {
                        fieldValue = EditorGUILayout.IntField(fieldInfo.Name, (int)fieldValue);
                    }
                    else if (fieldInfo.FieldType == typeof(float))
                    {
                        fieldValue = EditorGUILayout.FloatField(fieldInfo.Name, (float)fieldValue);
                    }
                    else if (fieldInfo.FieldType == typeof(string))
                    {
                        fieldValue = EditorGUILayout.TextField(fieldInfo.Name, (string)fieldValue);
                    }
                    // Di�er t�rleri ihtiyaca g�re ekleyin...

                    if (EditorGUI.EndChangeCheck())
                    {
                        fieldInfo.SetValue(scriptComponent, fieldValue);
                    }
                }
            }
        }
    }

    private bool GetPropertyVisibility(MonoBehaviour scriptComponent, string propertyName)
    {
        string key = GetVisibilityKey(scriptComponent, propertyName);
        return EditorPrefs.GetBool(key, true);
    }

    private void SetPropertyVisibility(MonoBehaviour scriptComponent, string propertyName, bool visibility)
    {
        string key = GetVisibilityKey(scriptComponent, propertyName);
        EditorPrefs.SetBool(key, visibility);
    }

    private string GetVisibilityKey(MonoBehaviour scriptComponent, string propertyName)
    {
        // PlayerPrefs i�in benzersiz bir anahtar olu�tur
        return $"{scriptComponent.GetType().FullName}_{scriptComponent.GetInstanceID()}_{propertyName}";
    }
}

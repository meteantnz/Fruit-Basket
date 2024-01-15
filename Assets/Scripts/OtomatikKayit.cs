using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class JsonManager
{
    private static string jsonFilePath = "Assets/Resources/savedData.json";

    public static void SaveToJson(Dictionary<string, Dictionary<string, object>> data)
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(jsonFilePath, json);

        Debug.Log("JSON dosyasý þuraya kaydedildi: " + jsonFilePath);
    }

    public static Dictionary<string, Dictionary<string, object>> LoadFromJson()
    {
        if (File.Exists(jsonFilePath))
        {
            string json = File.ReadAllText(jsonFilePath);
            var loadedData = JsonUtility.FromJson<Dictionary<string, Dictionary<string, object>>>(json);

            Debug.Log("JSON dosyasý þuradan yüklendi: " + jsonFilePath);
            return loadedData;
        }
        else
        {
            Debug.Log("Belirtilen yerde JSON dosyasý bulunamadý: " + jsonFilePath);
            return new Dictionary<string, Dictionary<string, object>>();
        }
    }
}


public class CustomEditorWindow : EditorWindow
{
    private GameObject draggedGameObject;
    private List<MonoBehaviour> scriptComponents = new List<MonoBehaviour>();
    private bool[] showPropertyArray;
    private bool[] toggleValues;
    private Dictionary<string, Dictionary<string, object>> jsonValues;

    [MenuItem("Window/Özel Editör Penceresi")]
    public static void ShowWindow()
    {
        GetWindow<CustomEditorWindow>("Özel Editör Penceresi");
    }

    private void OnEnable()
    {
        jsonValues = JsonManager.LoadFromJson();
        Debug.Log("CustomEditorWindow etkinleþtirildi");
    }

    private void OnDisable()
    {
        JsonManager.SaveToJson(jsonValues);
        Debug.Log("CustomEditorWindow devre dýþý býrakýldý");
    }

    private void OnGUI()
    {
        Event currentEvent = Event.current;

        GUILayout.Label("Objeyi Buraya Sürükleyin", EditorStyles.boldLabel);

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
                    Debug.Log("GameObject sürüklendi ve iþlendi");
                }

                Event.current.Use();
                break;
        }

        if (draggedGameObject != null)
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.ObjectField(draggedGameObject, typeof(GameObject), false);

            if (GUILayout.Button("Kaldýr"))
            {
                draggedGameObject = null;
                scriptComponents.Clear();
                showPropertyArray = null;
                toggleValues = null;
                jsonValues.Clear();
            }

            GUILayout.EndHorizontal();

            if (toggleValues == null || toggleValues.Length != scriptComponents.Count)
            {
                toggleValues = new bool[scriptComponents.Count];
                for (int j = 0; j < toggleValues.Length; j++)
                {
                    toggleValues[j] = false;
                }
            }

            for (int i = 0; i < scriptComponents.Count; i++)
            {
                GUILayout.BeginHorizontal();

                toggleValues[i] = EditorGUILayout.Toggle(toggleValues[i], GUILayout.Width(20));

                GUILayout.Label(scriptComponents[i].GetType().Name);

                GUILayout.EndHorizontal();

                if (showPropertyArray == null || showPropertyArray.Length != scriptComponents.Count)
                {
                    showPropertyArray = new bool[scriptComponents.Count];
                    for (int j = 0; j < showPropertyArray.Length; j++)
                    {
                        showPropertyArray[j] = true;
                    }
                }

                if (showPropertyArray[i] || toggleValues[i])
                {
                    System.Reflection.FieldInfo[] fields = scriptComponents[i].GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    foreach (var fieldInfo in fields)
                    {
                        GUILayout.BeginHorizontal();

                        bool showProperty = GetPropertyVisibility(scriptComponents[i], fieldInfo.Name);
                        bool newVisibility = EditorGUILayout.ToggleLeft(fieldInfo.Name, showProperty, GUILayout.Width(EditorGUIUtility.labelWidth - 15));

                        if (newVisibility != showProperty)
                        {
                            SetPropertyVisibility(scriptComponents[i], fieldInfo.Name, newVisibility);
                        }

                        GUILayout.EndHorizontal();

                        if (showPropertyArray == null || showPropertyArray.Length != scriptComponents.Count)
                        {
                            showPropertyArray = new bool[scriptComponents.Count];
                            for (int j = 0; j < showPropertyArray.Length; j++)
                            {
                                showPropertyArray[j] = true;
                            }
                        }

                        if (showPropertyArray[i] || newVisibility)
                        {
                            object fieldValue = fieldInfo.GetValue(scriptComponents[i]);

                            EditorGUI.BeginChangeCheck();

                            if (fieldInfo.FieldType == typeof(int))
                            {
                                fieldValue = EditorGUILayout.IntField("", (int)fieldValue);
                            }
                            else if (fieldInfo.FieldType == typeof(float))
                            {
                                fieldValue = EditorGUILayout.FloatField("", (float)fieldValue);
                            }
                            else if (fieldInfo.FieldType == typeof(string))
                            {
                                fieldValue = EditorGUILayout.TextField("", (string)fieldValue);
                            }

                            if (EditorGUI.EndChangeCheck())
                            {
                                fieldInfo.SetValue(scriptComponents[i], fieldValue);
                            }

                            // JSON deðerini güncelle
                            UpdateJsonValue(scriptComponents[i].GetType().Name, fieldInfo.Name, fieldValue, toggleValues[i]);

                        }
                    }
                }
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

    private void SetPropertyVisibility(MonoBehaviour scriptComponent, string propertyName, bool visibility)
    {
        string key = GetVisibilityKey(scriptComponent, propertyName);
        EditorPrefs.SetBool(key, visibility);
    }

    private bool GetPropertyVisibility(MonoBehaviour scriptComponent, string propertyName)
    {
        string key = GetVisibilityKey(scriptComponent, propertyName);
        return EditorPrefs.GetBool(key, true);
    }

    private string GetVisibilityKey(MonoBehaviour scriptComponent, string propertyName)
    {
        return $"{scriptComponent.GetType().FullName}_{scriptComponent.GetInstanceID()}_{propertyName}";
    }

    private void UpdateJsonValue(string componentName, string propertyName, object value, bool shouldSave)
    {
        if (shouldSave && toggleValues != null && toggleValues.Length > 0)
        {
            int componentIndex = scriptComponents.FindIndex(component => component.GetType().Name == componentName);

            if (componentIndex >= 0 && componentIndex < toggleValues.Length && toggleValues[componentIndex])
            {
                if (!jsonValues.ContainsKey(componentName))
                {
                    jsonValues[componentName] = new Dictionary<string, object>();
                }

                jsonValues[componentName][propertyName] = value;

                Debug.Log($"JSON deðeri güncellendi: {componentName}.{propertyName} = {value}");
            }
        }
    }

}

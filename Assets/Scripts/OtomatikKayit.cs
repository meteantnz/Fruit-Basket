using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;

public class JsonManager
{
    private static string jsonFilePath = "Assets/Resources/saveData/savedData.json";

    public static void SaveToJson(Dictionary<string, Dictionary<string, object>> data)
    {
        string directoryPath = Path.GetDirectoryName(jsonFilePath);

        // Dosya yolundaki klas�rleri kontrol et ve yoksa olu�tur
        if (!Directory.Exists(directoryPath))
        {
            try
            {
                Directory.CreateDirectory(directoryPath);
            }
            catch (Exception e)
            {
                Debug.LogError("Dosya yolu olu�turulamad�: " + e.Message);
                return;
            }
        }

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(jsonFilePath, json);

        Debug.Log("JSON dosyas� �uraya kaydedildi: " + jsonFilePath);
    }

    public static Dictionary<string, Dictionary<string, object>> LoadFromJson()
    {
        if (Resources.Load<TextAsset>("saveData/savedData") != null)
        {
            TextAsset textAsset = Resources.Load<TextAsset>("saveData/savedData");
            string json = textAsset.text;

            var loadedData = JsonUtility.FromJson<Dictionary<string, Dictionary<string, object>>>(json);

            Debug.Log("JSON dosyas� �uradan y�klendi: " + jsonFilePath);
            return loadedData;
        }
        else
        {
            Debug.Log("Belirtilen yerde JSON dosyas� bulunamad�: " + jsonFilePath);
            return new Dictionary<string, Dictionary<string, object>>();
        }
    }
}

public class CustomEditorWindow : EditorWindow
{
    private GameObject draggedGameObject;
    private List<MonoBehaviour> scriptComponents = new List<MonoBehaviour>();
    private Dictionary<string, bool> toggleValues = new Dictionary<string, bool>(); // Her toggle i�in ayr� bir kimlik ve durum
    private Dictionary<string, Dictionary<string, object>> jsonValues;

    [MenuItem("Window/�zel Edit�r Penceresi")]
    public static void ShowWindow()
    {
        GetWindow<CustomEditorWindow>("�zel Edit�r Penceresi");
    }

    private void OnEnable()
    {
        jsonValues = JsonManager.LoadFromJson();
        Debug.Log("CustomEditorWindow etkinle�tirildi");
    }

    private void OnDisable()
    {
        JsonManager.SaveToJson(jsonValues);
        Debug.Log("CustomEditorWindow devre d��� b�rak�ld�");
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

                    foreach (UnityEngine.Object draggedObj in DragAndDrop.objectReferences)
                    {
                        draggedGameObject = draggedObj as GameObject;
                        ScriptleriTara();
                        Repaint();
                    }
                    Debug.Log("GameObject s�r�klendi ve i�lendi");
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
                toggleValues.Clear();
                jsonValues.Clear();
            }

            GUILayout.EndHorizontal();

            for (int i = 0; i < scriptComponents.Count; i++)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Label(scriptComponents[i].GetType().Name);

                GUILayout.EndHorizontal();

                System.Reflection.FieldInfo[] fields = scriptComponents[i].GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                foreach (var fieldInfo in fields)
                {
                    GUILayout.BeginHorizontal();

                    bool showProperty = GetPropertyVisibility(scriptComponents[i], fieldInfo.Name);
                    bool newVisibility = EditorGUILayout.ToggleLeft(fieldInfo.Name, showProperty, GUILayout.Width(120));

                    if (newVisibility != showProperty)
                    {
                        SetPropertyVisibility(scriptComponents[i], fieldInfo.Name, newVisibility);
                    }

                    // Yeni kod, alan de�erini do�rudan g�r�nt�lemek ve d�zenlemek i�in
                    object value = fieldInfo.GetValue(scriptComponents[i]);
                    Type fieldType = fieldInfo.FieldType;

                    // Alan etiketini g�r�nt�le
                    GUILayout.Label(":", GUILayout.Width(5));

                    // Alan de�erini g�r�nt�le ve d�zenle
                    if (fieldType == typeof(int))
                    {
                        int newValue = EditorGUILayout.IntField((int)value, GUILayout.Width(60));
                        fieldInfo.SetValue(scriptComponents[i], newValue);
                    }
                    else if (fieldType == typeof(float))
                    {
                        float newValue = EditorGUILayout.FloatField((float)value, GUILayout.Width(60));
                        fieldInfo.SetValue(scriptComponents[i], newValue);
                    }
                    else if (fieldType == typeof(string))
                    {
                        string newValue = EditorGUILayout.TextField((string)value, GUILayout.Width(80));
                        fieldInfo.SetValue(scriptComponents[i], newValue);
                    }
                    // Di�er alan t�rleri i�in gerekirse daha fazla durum ekle

                    GUILayout.EndHorizontal();

                    if (newVisibility)
                    {
                        // Toggle'�n kimli�ini olu�tur
                        string toggleKey = $"{scriptComponents[i].GetType().Name}_{fieldInfo.Name}";

                        // Kimlikle birlikte JSON de�erini g�ncelle
                        UpdateJsonValue(scriptComponents[i].GetType().Name, fieldInfo.Name, fieldInfo.GetValue(scriptComponents[i]), toggleValues.ContainsKey(toggleKey) && toggleValues[toggleKey]);

                        // Toggle durumunu g�ncelle
                        toggleValues[toggleKey] = newVisibility;
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

            foreach (var script in scripts)
            {
                scriptComponents.Add(script);

                // Toggle'� varsay�lan olarak false olarak ayarla
                string toggleKey = $"{script.GetType().Name}_";
                toggleValues[toggleKey] = false;
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

    private void UpdateJsonValue(string componentName, string propertyName, object value, bool toggleState)
    {
        if (toggleState)
        {
            if (!jsonValues.ContainsKey(componentName))
            {
                jsonValues[componentName] = new Dictionary<string, object>();
            }

            string key = $"{componentName}_{propertyName}";
            jsonValues[componentName][key] = value;

            Debug.Log($"JSON de�eri g�ncellendi: {componentName}.{propertyName} = {value}");
        }
    }
}

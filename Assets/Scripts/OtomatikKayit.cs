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

        // Dosya yolundaki klasörleri kontrol et ve yoksa oluþtur
        if (!Directory.Exists(directoryPath))
        {
            try
            {
                Directory.CreateDirectory(directoryPath);
            }
            catch (Exception e)
            {
                Debug.LogError("Dosya yolu oluþturulamadý: " + e.Message);
                return;
            }
        }

        string json = JsonUtility.ToJson(data);
        File.WriteAllText(jsonFilePath, json);

        Debug.Log("JSON dosyasý þuraya kaydedildi: " + jsonFilePath);
    }

    public static Dictionary<string, Dictionary<string, object>> LoadFromJson()
    {
        if (Resources.Load<TextAsset>("saveData/savedData") != null)
        {
            TextAsset textAsset = Resources.Load<TextAsset>("saveData/savedData");
            string json = textAsset.text;

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
    private Dictionary<string, bool> toggleValues = new Dictionary<string, bool>(); // Her toggle için ayrý bir kimlik ve durum
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

                    foreach (UnityEngine.Object draggedObj in DragAndDrop.objectReferences)
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

                    // Yeni kod, alan deðerini doðrudan görüntülemek ve düzenlemek için
                    object value = fieldInfo.GetValue(scriptComponents[i]);
                    Type fieldType = fieldInfo.FieldType;

                    // Alan etiketini görüntüle
                    GUILayout.Label(":", GUILayout.Width(5));

                    // Alan deðerini görüntüle ve düzenle
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
                    // Diðer alan türleri için gerekirse daha fazla durum ekle

                    GUILayout.EndHorizontal();

                    if (newVisibility)
                    {
                        // Toggle'ýn kimliðini oluþtur
                        string toggleKey = $"{scriptComponents[i].GetType().Name}_{fieldInfo.Name}";

                        // Kimlikle birlikte JSON deðerini güncelle
                        UpdateJsonValue(scriptComponents[i].GetType().Name, fieldInfo.Name, fieldInfo.GetValue(scriptComponents[i]), toggleValues.ContainsKey(toggleKey) && toggleValues[toggleKey]);

                        // Toggle durumunu güncelle
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

                // Toggle'ý varsayýlan olarak false olarak ayarla
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

            Debug.Log($"JSON deðeri güncellendi: {componentName}.{propertyName} = {value}");
        }
    }
}

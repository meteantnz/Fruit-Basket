using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Xml;
using System.Linq;

public class CombinedManagerWindow : EditorWindow
{
    private string jsonFilePath = "Assets/Resources/saveData/savedData.json";
    private List<KeyValuePair<string, Dictionary<string, object>>> jsonValues = new List<KeyValuePair<string, Dictionary<string, object>>>();
    private GameObject draggedGameObject;
    private List<MonoBehaviour> scriptComponents = new List<MonoBehaviour>();
    private Dictionary<string, bool> toggleValues = new Dictionary<string, bool>();

    [MenuItem("Window/�zel Edit�r Penceresi")]
    public static void ShowWindow()
    {
        GetWindow<CombinedManagerWindow>("�zel Edit�r Penceresi");
    }

    private void OnEnable()
    {
        jsonValues = new List<KeyValuePair<string, Dictionary<string, object>>>();
        LoadJsonValues();
        Debug.Log("CombinedManagerWindow etkinle�tirildi");
    }

    private void OnDisable()
    {
        SaveToJson();
        Debug.Log("CombinedManagerWindow devre d��� b�rak�ld�");
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
            // componentName'a ait �nceki ��eyi bul
            var existingEntry = jsonValues.Find(entry => entry.Key == componentName);

            if (existingEntry.Equals(default(KeyValuePair<string, Dictionary<string, object>>)))
            {
                // componentName'a ait �nceki ��e yoksa, yeni bir ��e olu�tur
                var newEntry = new JsonEntry
                {
                    Key = componentName,
                    Values = new Dictionary<string, object>()
                };
                newEntry.Values[$"{componentName}_{propertyName}"] = value;

                // Yeni ��eyi jsonValues listesine ekle
                jsonValues.Add(new KeyValuePair<string, Dictionary<string, object>>(componentName, newEntry.Values));
            }
            else
            {
                // componentName'a ait �nceki ��e varsa, de�eri g�ncelle
                existingEntry.Value[$"{componentName}_{propertyName}"] = value;
            }

            // Burada _jsonValues listesini jsonValues listesine e�itleyin
            SerializableData serializableData = new SerializableData();
            serializableData._jsonValues = jsonValues.SelectMany(entry => entry.Value.Select(kv => new JsonData
            {
                key = kv.Key,
                value = kv.Value
            })).ToList();

            Debug.Log("Debug - serializableData.jsonValues i�eri�i: " + JsonUtility.ToJson(serializableData._jsonValues, true));
            Debug.Log($"UpdateJsonValue - componentName: {componentName}, propertyName: {propertyName}, value: {value}, toggleState: {toggleState}");
            Debug.Log($"JSON de�eri g�ncellendi: {componentName}.{propertyName} = {value}");
        }
    }





    // JSON verilerini serile�tirmek ve deserializasyon yapmak i�in kullan�lacak s�n�f
    [System.Serializable]
    public class JsonData
    {
        public string key;
        public object value;
    }

    [System.Serializable]
    public class JsonEntry
    {
        public string Key;
        public Dictionary<string, object> Values;
    }

    [System.Serializable]
    public class SerializableData
    {
        public List<JsonData> _jsonValues = new List<JsonData>();
    }


    private void SaveToJson()
    {
        // JsonData s�n�f�n� kullanarak _jsonValues'e ekle
        SerializableData serializableData = new SerializableData();
        foreach (var entry in jsonValues)
        {
            foreach (var item in entry.Value)
            {
                JsonData jsonData = new JsonData();
                jsonData.key = item.Key;
                jsonData.value = item.Value;
                serializableData._jsonValues.Add(jsonData);
            }
        }

        // JSON verilerini dosyaya yazma
        string json = JsonUtility.ToJson(serializableData, true);
        File.WriteAllText(jsonFilePath, json);

        Debug.Log($"JSON ��eri�i (SaveToJson): {json}");
    }

    private void LoadJsonValues()
    {
        if (Resources.Load<TextAsset>("saveData/savedData") != null)
        {
            TextAsset textAsset = Resources.Load<TextAsset>("saveData/savedData");

            string json = textAsset.text;

            // JSON verilerini dosyadan okuma ve deserializasyon i�lemi
            SerializableData loadedData = JsonUtility.FromJson<SerializableData>(json);

            if (loadedData != null)
            {
                // jsonValues listesini temizle
                jsonValues.Clear();

                foreach (var jsonData in loadedData._jsonValues)
                {
                    // jsonValues listesine elemanlar� ekleyin
                    var keyParts = jsonData.key.Split('_');
                    if (keyParts.Length == 2)
                    {
                        var componentName = keyParts[0];
                        var propertyName = keyParts[1];

                        // componentName'a ait �nceki ��eyi bul
                        var existingEntry = jsonValues.Find(entry => entry.Key == componentName);

                        if (existingEntry.Equals(default(KeyValuePair<string, Dictionary<string, object>>)))
                        {
                            // componentName'a ait �nceki ��e yoksa, yeni bir ��e olu�tur
                            var newEntry = new KeyValuePair<string, Dictionary<string, object>>(componentName, new Dictionary<string, object>());
                            newEntry.Value[$"{componentName}_{propertyName}"] = jsonData.value;

                            // Yeni ��eyi jsonValues listesine ekle
                            jsonValues.Add(newEntry);
                        }
                        else
                        {
                            // componentName'a ait �nceki ��e varsa, de�eri g�ncelle
                            existingEntry.Value[$"{componentName}_{propertyName}"] = jsonData.value;
                        }
                    }
                }

                Debug.Log("JSON dosyas� �uradan y�klendi: " + jsonFilePath);
            }
            else
            {
                Debug.LogError("JSON dosyas� y�klenemedi.");
            }
        }
        else
        {
            Debug.Log("Belirtilen yerde JSON dosyas� bulunamad�: " + jsonFilePath);
        }
    }



}

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Xml;
using System.Linq;
using static CombinedManagerWindow;

public class CombinedManagerWindow : EditorWindow
{
    private string jsonFilePath = "Assets/Resources/saveData/savedData.json";
    private List<KeyValuePair<string, Dictionary<string, object>>> jsonValues = new List<KeyValuePair<string, Dictionary<string, object>>>();
    private GameObject draggedGameObject;
    private List<MonoBehaviour> scriptComponents = new List<MonoBehaviour>();
    private Dictionary<string, bool> toggleValues = new Dictionary<string, bool>();

    [MenuItem("Window/Özel Editör Penceresi")]
    public static void ShowWindow()
    {
        GetWindow<CombinedManagerWindow>("Özel Editör Penceresi");
    }

    private void OnEnable()
    {
        jsonValues = new List<KeyValuePair<string, Dictionary<string, object>>>();
        LoadJsonValues();
        Debug.Log("CombinedManagerWindow etkinleþtirildi");
    }

    private void OnDisable()
    {
        SaveToJson();
        Debug.Log("CombinedManagerWindow devre dýþý býrakýldý");
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
    private SerializableData _serializableData = new SerializableData();
    private void UpdateJsonValue(string componentName, string propertyName, object value, bool toggleState)
    {
        // componentName ve propertyName'e ait önceki öðeyi bul
        var existingEntry = jsonValues.Find(entry => entry.Key == componentName);

        if (existingEntry.Equals(default(KeyValuePair<string, Dictionary<string, object>>)))
        {
            // componentName'a ait önceki öðe yoksa, yeni bir öðe oluþtur
            existingEntry = new KeyValuePair<string, Dictionary<string, object>>(
                componentName,
                new Dictionary<string, object>()
            );
            jsonValues.Add(existingEntry);
        }

        // componentName'a ait önceki öðe varsa, deðeri güncelle
        existingEntry.Value[propertyName] = value; // propertyName ve deðeri JSON'a ekleyin

        // _jsonValues listesini oluþturun
        List<JsonData> jsonDataList = jsonValues.SelectMany(entry =>
        {
            return entry.Value.Select(kv => new JsonData
            {
                key = $"{entry.Key}_{kv.Key}", // componentName'ý ve propertyName'ý birleþtir
                componentName = entry.Key,       // componentName'ý ayrý bir alanda sakla
                propertyName = kv.Key,           // propertyName'ý ayrý bir alanda sakla
                value = kv.Value
            });
        }).ToList();

        // jsonValues listesini güncelleyin
        jsonValues = jsonDataList.GroupBy(jd => jd.componentName)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(item => item.propertyName, item => item.value)
            )
            .Select(entry => new KeyValuePair<string, Dictionary<string, object>>(entry.Key, entry.Value))
            .ToList();

        // _serializableData._jsonValues'i güncelle
        _serializableData._jsonValues = jsonDataList;

        // Debug çýktýsý ekle
        foreach (var jsonData in jsonDataList)
        {
            Debug.Log($"Component: {jsonData.componentName}, Property: {jsonData.propertyName}, Value: {jsonData.value}");
        }

        Debug.Log("Debug - _jsonValues içeriði: " + JsonUtility.ToJson(_serializableData._jsonValues, true));
        Debug.Log("Debug - jsonValues: " + JsonUtility.ToJson(jsonValues, true));
        Debug.Log($"UpdateJsonValue - componentName: {componentName}, propertyName: {propertyName}, value: {value}, toggleState: {toggleState}");
        Debug.Log($"JSON deðeri güncellendi: {componentName}.{propertyName} = {value}");
    }





    // JSON verilerini serileþtirmek ve deserializasyon yapmak için kullanýlacak sýnýf
    [System.Serializable]
    public class JsonData
    {
        public string key;
        public string componentName;
        public string propertyName;
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
        try
        {
            // _jsonValues listesini güncelleyin
            _serializableData._jsonValues = jsonValues.SelectMany(entry =>
            {
                return entry.Value.Select(kv => new JsonData
                {
                    key = $"{entry.Key}_{kv.Key}",
                    componentName = entry.Key,
                    propertyName = kv.Key,
                    value = kv.Value
                });
            }).ToList();

            // JSON dosyasýný oluþtur ve kaydet
            string json = JsonUtility.ToJson(_serializableData, true);
            File.WriteAllText(jsonFilePath, json);

            // Deðerleri daha ayrýntýlý göstermek için JsonData nesnelerini yazdýr
            foreach (var jsonData in _serializableData._jsonValues)
            {
                Debug.Log($"Key: {jsonData.key}, Component: {jsonData.componentName}, Property: {jsonData.propertyName}, Value: {jsonData.value}");
            }

            Debug.Log($"JSON Ýçeriði (SaveToJson): {json}");
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveToJson Hatasý: {e.Message}");
        }
    }








    private void LoadJsonValues()
    {
        if (Resources.Load<TextAsset>("saveData/savedData") != null)
        {
            TextAsset textAsset = Resources.Load<TextAsset>("saveData/savedData");

            string json = textAsset.text;

            // JSON verilerini dosyadan okuma ve deserializasyon iþlemi
            SerializableData loadedData = JsonUtility.FromJson<SerializableData>(json);

            if (loadedData != null && loadedData._jsonValues != null)
            {
                // _jsonValues listesini güncelle
                _serializableData._jsonValues = loadedData._jsonValues.ToList();

                Debug.Log("JSON dosyasý þuradan yüklendi: " + jsonFilePath);
            }
            else
            {
                Debug.LogError("JSON dosyasý yüklenemedi veya _jsonValues boþ.");
            }
        }
        else
        {
            Debug.Log("Belirtilen yerde JSON dosyasý bulunamadý: " + jsonFilePath);
        }
    }





}

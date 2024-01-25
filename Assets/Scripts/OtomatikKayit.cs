using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Globalization;
using System.Linq;
using System.Collections;

public class CombinedManagerWindow : EditorWindow
{
    private string jsonFilePath = "Assets/Resources/saveData/savedData.json";
    private List<KeyValuePair<string, Dictionary<string, object>>> jsonValues = new List<KeyValuePair<string, Dictionary<string, object>>>();
    private GameObject draggedGameObject;
    private List<MonoBehaviour> scriptComponents = new List<MonoBehaviour>();
    private Dictionary<string, bool> toggleValues = new Dictionary<string, bool>();
    private SerializableData _serializableData = new SerializableData();
    private Dictionary<MonoBehaviour, Dictionary<string, object>> previousComponentValues = new Dictionary<MonoBehaviour, Dictionary<string, object>>();
    public static event Action<string, string, object> OnValueChanged;


    [MenuItem("Window/Özel Editör Penceresi")]
    public static void ShowWindow()
    {
        GetWindow<CombinedManagerWindow>("Özel Editör Penceresi");
    }

    private void OnEnable()
    {
        jsonValues = new List<KeyValuePair<string, Dictionary<string, object>>>();
        LoadJsonValues();
        OnValueChanged += HandleValueChanged;
        Debug.Log("CombinedManagerWindow etkinleþtirildi");
    }

    private void OnDisable()
    {
        OnValueChanged -= HandleValueChanged;
        if (EditorApplication.isPlaying)
        {
            SaveToJson();
            Debug.Log("CombinedManagerWindow devre dýþý býrakýldý");
        }

    }
    private void Awake()
    {
        LoadJsonValues();
        Debug.Log("CombinedManagerWindow awake metodu çaðrýldý");
    }

    private void OnDestroy()
    {
        SaveToJson();
        OnValueChanged -= HandleValueChanged;
        Debug.Log("CombinedManagerWindow destroy metodu çaðrýldý");
    }

    private void Update()
    {
        // Deðer deðiþtiðinde Repaint fonksiyonunu çaðýrmak için kontrol
        bool changesDetected = CheckForChanges();

        if (changesDetected)
        {
            // Deðer deðiþtiðinde Repaint fonksiyonunu çaðýr
            Repaint();
        }

        // Geri kalan Update fonksiyonu içeriði...
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

                    object value = fieldInfo.GetValue(scriptComponents[i]);
                    Type fieldType = fieldInfo.FieldType;

                    GUILayout.Label(":", GUILayout.Width(5));

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
                        string newValue = EditorGUILayout.TextField((string)value, GUILayout.Width(60));
                        fieldInfo.SetValue(scriptComponents[i], newValue);
                    }

                    GUILayout.EndHorizontal();

                    if (newVisibility)
                    {
                        string toggleKey = $"{scriptComponents[i].GetType().Name}_{fieldInfo.Name}";
                        UpdateJsonValue(scriptComponents[i].GetType().Name, fieldInfo.Name, fieldInfo.GetValue(scriptComponents[i]), toggleValues.ContainsKey(toggleKey) && toggleValues[toggleKey]);
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

                // Debug mesajlarý ekle
                Debug.Log($"Oyun Öncesi - ScriptleriTara Metodu - Script Component: {script.GetType().Name}");

                // Script component'in public alan deðerlerini yazdýr
                System.Reflection.FieldInfo[] fields = script.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                foreach (var fieldInfo in fields)
                {
                    // Özel durum: Eðer alanýn türü List ise, listenin elemanlarýný yazdýr
                    if (fieldInfo.FieldType.IsGenericType && fieldInfo.FieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        IList list = fieldInfo.GetValue(script) as IList;

                        if (list != null)
                        {
                            for (int i = 0; i < list.Count; i++)
                            {
                                //Debug.Log($"- {fieldInfo.Name}[{i}]: {list[i] ?? "null"}");
                            }
                        }
                    }
                    else
                    {
                        // Diðer türler için normal deðeri yazdýr
                        object value = fieldInfo.GetValue(script);
                        //Debug.Log($"- {fieldInfo.Name}: {value ?? "null"}");
                    }
                }
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

    // JSON verilerini serileþtirmek ve deserializasyon yapmak için kullanýlacak sýnýf
    [System.Serializable]
    public class JsonData
    {
        public string key;
        public string componentName;
        public string propertyName;
        public string originalType; // Yeni eklenen alan: orijinal veri tipini saklar
        public string value;
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
    public void UpdateJsonValue(string componentName, string propertyName, object value, bool toggleState)
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
        existingEntry.Value[propertyName] = value;

        // _jsonValues listesini oluþturun
        List<JsonData> jsonDataList = jsonValues.SelectMany(entry =>
        {
            return entry.Value.Select(kv => new JsonData
            {
                key = $"{entry.Key}_{kv.Key}",
                componentName = entry.Key,
                propertyName = kv.Key,
                originalType = GetOriginalTypeString(kv.Value), // Orijinal türü string olarak sakla
                value = ConvertToString(kv.Value) // Deðerleri stringe dönüþtür
            });
        }).ToList();

        // jsonValues listesini güncelleyin
        jsonValues = jsonDataList.GroupBy(jd => jd.componentName)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(item => item.propertyName, item => ConvertFromString(item.originalType, item.value)) // Deðerleri geri çevir
            )
            .Select(entry => new KeyValuePair<string, Dictionary<string, object>>(entry.Key, entry.Value))
            .ToList();

        // _serializableData._jsonValues'i güncelle
        _serializableData._jsonValues = jsonDataList;

        // Olayý tetikle
        OnValueChanged?.Invoke(componentName, propertyName, value);

        // Debug çýktýsý ekle
        foreach (var jsonData in jsonDataList)
        {
            Debug.Log($"Key: {jsonData.key}, Component: {jsonData.componentName}, Property: {jsonData.propertyName}, OriginalType: {jsonData.originalType}, Value: {jsonData.value}");
        }

        // SaveToJson fonksiyonunu çaðýr
        SaveToJson();
    }



    private void SaveToJson()
    {
        if (EditorApplication.isPlaying)
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
                        originalType = GetOriginalTypeString(kv.Value), // Orijinal türü string olarak sakla
                        value = ConvertToString(kv.Value) // Deðerleri stringe dönüþtür
                    });
                }).ToList();

                // JSON dosyasýný oluþtur ve kaydet
                string json = JsonUtility.ToJson(_serializableData, true);
                File.WriteAllText(jsonFilePath, json);

                // Deðerleri daha ayrýntýlý göstermek için JsonData nesnelerini yazdýr
                foreach (var jsonData in _serializableData._jsonValues)
                {
                    jsonData.value = ConvertFromString(jsonData.originalType, jsonData.value) as string; // Deðerleri orijinal türlerine çevir

                    //Debug.Log($"Key: {jsonData.key}, Component: {jsonData.componentName}, Property: {jsonData.propertyName}, OriginalType: {jsonData.originalType}, Value: {jsonData.value}");
                }

                Debug.Log($"JSON Ýçeriði (SaveToJson): {json}");
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveToJson Hatasý: {e.Message}");
            }
        }
    }



    private void LoadJsonValues()
    {
        try
        {
            if (Resources.Load<TextAsset>("saveData/savedData") != null)
            {
                TextAsset textAsset = Resources.Load<TextAsset>("saveData/savedData");

                string json = textAsset.text;

                // JSON verilerini dosyadan okuma ve deserializasyon iþlemi
                SerializableData loadedData = JsonUtility.FromJson<SerializableData>(json);

                if (loadedData != null && loadedData._jsonValues != null)
                {
                    // Debug çýktýsý: JSON dosyasýndan yüklenen SerializableData içeriði
                    //Debug.Log($"Yüklenen SerializableData: {JsonUtility.ToJson(loadedData, true)}");

                    // _jsonValues listesini güncelle
                    _serializableData._jsonValues = loadedData._jsonValues.ToList();

                    // Debug çýktýsý: _jsonValues listesi
                    //Debug.Log($"_jsonValues Ýçeriði: {JsonUtility.ToJson(_serializableData._jsonValues, true)}");

                    // Deðerleri ilgili deðiþkenlere atama iþlemini burada yapabilirsiniz
                    // ...

                    // Örneðin:
                    foreach (var jsonData in _serializableData._jsonValues)
                    {
                        MonoBehaviour script = FindScriptComponent(jsonData.componentName);
                        if (script != null)
                        {
                            System.Reflection.FieldInfo fieldInfo = script.GetType().GetField(jsonData.propertyName);
                            if (fieldInfo != null)
                            {
                                object loadedValue = ConvertFromString(jsonData.originalType, jsonData.value);
                                fieldInfo.SetValue(script, loadedValue);

                                // Deðer deðiþikliðini tetikle
                                
                            }
                        }
                    }

                    // Debug çýktýsý: Yüklenen JSON deðerleri
                    foreach (var jsonData in _serializableData._jsonValues)
                    {
                        //Debug.Log($"Anahtar: {jsonData.key}, Bileþen: {jsonData.componentName}, Özellik: {jsonData.propertyName}, Orijinal Tür: {jsonData.originalType}, Deðer: {jsonData.value}");
                    }

                    //Debug.Log("JSON dosyasý þuradan yüklendi: " + jsonFilePath);
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
        catch (Exception e)
        {
            Debug.LogError($"LoadJsonValues Hatasý: {e.Message}");
        }
    }


    private string GetOriginalTypeString(object value)
    {
        if (value == null)
        {
            return "null";
        }
        else
        {
            return value.GetType().FullName; // Orijinal türün tam adýný kullanabilirsiniz
        }
    }

    private string ConvertToString(object value)
    {
        if (value == null)
        {
            return "null";
        }
        else if (value is int || value is float || value is bool)
        {
            // Sayýsal deðerleri kültür bilgisini kullanarak nokta ile ayýr
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
        else
        {
            // Diðer türler için özel durumlar ekleme
            return value.ToString();
        }
    }

    // Deðerleri string'e dönüþtüren yardýmcý metod
    private object ConvertFromString(string originalType, string valueString)
    {
        Debug.Log($"String'den deðer dönüþtürülüyor: {valueString}");

        object result = null;

        try
        {
            if (originalType == typeof(int).FullName)
            {
                int intValue;
                if (int.TryParse(valueString, out intValue))
                {
                    result = intValue;
                }
            }
            else if (originalType == typeof(float).FullName)
            {
                float floatValue;
                // Virgülle ayrýlmýþ sayýlarý noktaya dönüþtür
                if (float.TryParse(valueString.Replace('.', ','), out floatValue))
                {
                    result = floatValue;
                }
            }
            else if (originalType == typeof(string).FullName)
            {
                result = valueString;
            }
            // Diðer türler için gerekirse daha fazla durum ekle

            // Bilinmeyen tür için varsayýlan olarak string döndür
            if (result == null)
            {
                result = valueString;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ConvertFromString Hatasý: {e.Message}");
        }

        Debug.Log($"Dönüþtürülen deðer: {result}");

        return result;
    }


    private bool CheckForChanges()
    {
        bool changesDetected = false;

        if (draggedGameObject != null)
        {
            scriptComponents.Clear();
            MonoBehaviour[] scripts = draggedGameObject.GetComponents<MonoBehaviour>();

            foreach (var script in scripts)
            {
                scriptComponents.Add(script);

                // Önceki durumu kontrol etmek için bir sözlük oluþtur
                if (!previousComponentValues.ContainsKey(script))
                {
                    previousComponentValues[script] = new Dictionary<string, object>();
                }

                // Farklýlýk kontrolü yap
                changesDetected |= CheckForChanges(script);
            }
        }

        return changesDetected;
    }
    private bool CheckForChanges(MonoBehaviour script)
    {
        Dictionary<string, object> previousValues = previousComponentValues[script];
        bool changesDetected = false;

        System.Reflection.FieldInfo[] fields = script.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

        foreach (var fieldInfo in fields)
        {
            object currentValue = fieldInfo.GetValue(script);
            string fieldName = fieldInfo.Name;

            // Önceki deðer var mý kontrol et
            if (previousValues.ContainsKey(fieldName))
            {
                object previousValue = previousValues[fieldName];

                // Farklýlýk var mý kontrol et
                if (!UnityEngine.Object.Equals(currentValue, previousValue))
                {
                    changesDetected = true;

                    // Önceki deðeri güncelle
                    previousValues[fieldName] = currentValue;

                    break;  // Farklýlýk bulunduðu için döngüyü sonlandýr
                }
            }
            else
            {
                // Önceki deðeri güncelle
                previousValues[fieldName] = currentValue;
            }
        }
        foreach (var kvp in previousValues)
        {
            //Debug.Log($"{script.GetType().Name}.{kvp.Key} - Previous Value: {kvp.Value}");
        }

        return changesDetected;
    }

    private MonoBehaviour FindScriptComponent(string componentName)
    {
        if (draggedGameObject != null)
        {
            MonoBehaviour[] scripts = draggedGameObject.GetComponents<MonoBehaviour>();

            foreach (var script in scripts)
            {
                if (script.GetType().Name == componentName)
                {
                    Debug.Log($"Script component found for {componentName}: {script.GetType().Name}");
                    return script;
                }
            }
        }

        Debug.LogWarning($"Script component not found for {componentName}");
        return null;
    }

    private void HandleValueChanged(string componentName, string propertyName, object newValue)
    {
        // Deðer deðiþikliðini burada iþleyin
        MonoBehaviour script = FindScriptComponent(componentName);
        if (script != null)
        {
            System.Reflection.FieldInfo fieldInfo = script.GetType().GetField(propertyName);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(script, newValue);
            }
        }
    }
}

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


    [MenuItem("Window/�zel Edit�r Penceresi")]
    public static void ShowWindow()
    {
        GetWindow<CombinedManagerWindow>("�zel Edit�r Penceresi");
    }

    private void OnEnable()
    {
        jsonValues = new List<KeyValuePair<string, Dictionary<string, object>>>();
        LoadJsonValues();
        OnValueChanged += HandleValueChanged;
        Debug.Log("CombinedManagerWindow etkinle�tirildi");
    }

    private void OnDisable()
    {
        OnValueChanged -= HandleValueChanged;
        if (EditorApplication.isPlaying)
        {
            SaveToJson();
            Debug.Log("CombinedManagerWindow devre d��� b�rak�ld�");
        }

    }
    private void Awake()
    {
        LoadJsonValues();
        Debug.Log("CombinedManagerWindow awake metodu �a�r�ld�");
    }

    private void OnDestroy()
    {
        SaveToJson();
        OnValueChanged -= HandleValueChanged;
        Debug.Log("CombinedManagerWindow destroy metodu �a�r�ld�");
    }

    private void Update()
    {
        // De�er de�i�ti�inde Repaint fonksiyonunu �a��rmak i�in kontrol
        bool changesDetected = CheckForChanges();

        if (changesDetected)
        {
            // De�er de�i�ti�inde Repaint fonksiyonunu �a��r
            Repaint();
        }

        // Geri kalan Update fonksiyonu i�eri�i...
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

                // Toggle'� varsay�lan olarak false olarak ayarla
                string toggleKey = $"{script.GetType().Name}_";
                toggleValues[toggleKey] = false;

                // Debug mesajlar� ekle
                Debug.Log($"Oyun �ncesi - ScriptleriTara Metodu - Script Component: {script.GetType().Name}");

                // Script component'in public alan de�erlerini yazd�r
                System.Reflection.FieldInfo[] fields = script.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

                foreach (var fieldInfo in fields)
                {
                    // �zel durum: E�er alan�n t�r� List ise, listenin elemanlar�n� yazd�r
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
                        // Di�er t�rler i�in normal de�eri yazd�r
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

    // JSON verilerini serile�tirmek ve deserializasyon yapmak i�in kullan�lacak s�n�f
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
        // componentName ve propertyName'e ait �nceki ��eyi bul
        var existingEntry = jsonValues.Find(entry => entry.Key == componentName);

        if (existingEntry.Equals(default(KeyValuePair<string, Dictionary<string, object>>)))
        {
            // componentName'a ait �nceki ��e yoksa, yeni bir ��e olu�tur
            existingEntry = new KeyValuePair<string, Dictionary<string, object>>(
                componentName,
                new Dictionary<string, object>()
            );
            jsonValues.Add(existingEntry);
        }

        // componentName'a ait �nceki ��e varsa, de�eri g�ncelle
        existingEntry.Value[propertyName] = value;

        // _jsonValues listesini olu�turun
        List<JsonData> jsonDataList = jsonValues.SelectMany(entry =>
        {
            return entry.Value.Select(kv => new JsonData
            {
                key = $"{entry.Key}_{kv.Key}",
                componentName = entry.Key,
                propertyName = kv.Key,
                originalType = GetOriginalTypeString(kv.Value), // Orijinal t�r� string olarak sakla
                value = ConvertToString(kv.Value) // De�erleri stringe d�n��t�r
            });
        }).ToList();

        // jsonValues listesini g�ncelleyin
        jsonValues = jsonDataList.GroupBy(jd => jd.componentName)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(item => item.propertyName, item => ConvertFromString(item.originalType, item.value)) // De�erleri geri �evir
            )
            .Select(entry => new KeyValuePair<string, Dictionary<string, object>>(entry.Key, entry.Value))
            .ToList();

        // _serializableData._jsonValues'i g�ncelle
        _serializableData._jsonValues = jsonDataList;

        // Olay� tetikle
        OnValueChanged?.Invoke(componentName, propertyName, value);

        // Debug ��kt�s� ekle
        foreach (var jsonData in jsonDataList)
        {
            Debug.Log($"Key: {jsonData.key}, Component: {jsonData.componentName}, Property: {jsonData.propertyName}, OriginalType: {jsonData.originalType}, Value: {jsonData.value}");
        }

        // SaveToJson fonksiyonunu �a��r
        SaveToJson();
    }



    private void SaveToJson()
    {
        if (EditorApplication.isPlaying)
        {
            try
            {
                // _jsonValues listesini g�ncelleyin
                _serializableData._jsonValues = jsonValues.SelectMany(entry =>
                {
                    return entry.Value.Select(kv => new JsonData
                    {
                        key = $"{entry.Key}_{kv.Key}",
                        componentName = entry.Key,
                        propertyName = kv.Key,
                        originalType = GetOriginalTypeString(kv.Value), // Orijinal t�r� string olarak sakla
                        value = ConvertToString(kv.Value) // De�erleri stringe d�n��t�r
                    });
                }).ToList();

                // JSON dosyas�n� olu�tur ve kaydet
                string json = JsonUtility.ToJson(_serializableData, true);
                File.WriteAllText(jsonFilePath, json);

                // De�erleri daha ayr�nt�l� g�stermek i�in JsonData nesnelerini yazd�r
                foreach (var jsonData in _serializableData._jsonValues)
                {
                    jsonData.value = ConvertFromString(jsonData.originalType, jsonData.value) as string; // De�erleri orijinal t�rlerine �evir

                    //Debug.Log($"Key: {jsonData.key}, Component: {jsonData.componentName}, Property: {jsonData.propertyName}, OriginalType: {jsonData.originalType}, Value: {jsonData.value}");
                }

                Debug.Log($"JSON ��eri�i (SaveToJson): {json}");
            }
            catch (Exception e)
            {
                Debug.LogError($"SaveToJson Hatas�: {e.Message}");
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

                // JSON verilerini dosyadan okuma ve deserializasyon i�lemi
                SerializableData loadedData = JsonUtility.FromJson<SerializableData>(json);

                if (loadedData != null && loadedData._jsonValues != null)
                {
                    // Debug ��kt�s�: JSON dosyas�ndan y�klenen SerializableData i�eri�i
                    //Debug.Log($"Y�klenen SerializableData: {JsonUtility.ToJson(loadedData, true)}");

                    // _jsonValues listesini g�ncelle
                    _serializableData._jsonValues = loadedData._jsonValues.ToList();

                    // Debug ��kt�s�: _jsonValues listesi
                    //Debug.Log($"_jsonValues ��eri�i: {JsonUtility.ToJson(_serializableData._jsonValues, true)}");

                    // De�erleri ilgili de�i�kenlere atama i�lemini burada yapabilirsiniz
                    // ...

                    // �rne�in:
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

                                // De�er de�i�ikli�ini tetikle
                                
                            }
                        }
                    }

                    // Debug ��kt�s�: Y�klenen JSON de�erleri
                    foreach (var jsonData in _serializableData._jsonValues)
                    {
                        //Debug.Log($"Anahtar: {jsonData.key}, Bile�en: {jsonData.componentName}, �zellik: {jsonData.propertyName}, Orijinal T�r: {jsonData.originalType}, De�er: {jsonData.value}");
                    }

                    //Debug.Log("JSON dosyas� �uradan y�klendi: " + jsonFilePath);
                }
                else
                {
                    Debug.LogError("JSON dosyas� y�klenemedi veya _jsonValues bo�.");
                }
            }
            else
            {
                Debug.Log("Belirtilen yerde JSON dosyas� bulunamad�: " + jsonFilePath);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadJsonValues Hatas�: {e.Message}");
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
            return value.GetType().FullName; // Orijinal t�r�n tam ad�n� kullanabilirsiniz
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
            // Say�sal de�erleri k�lt�r bilgisini kullanarak nokta ile ay�r
            return Convert.ToString(value, CultureInfo.InvariantCulture);
        }
        else
        {
            // Di�er t�rler i�in �zel durumlar ekleme
            return value.ToString();
        }
    }

    // De�erleri string'e d�n��t�ren yard�mc� metod
    private object ConvertFromString(string originalType, string valueString)
    {
        Debug.Log($"String'den de�er d�n��t�r�l�yor: {valueString}");

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
                // Virg�lle ayr�lm�� say�lar� noktaya d�n��t�r
                if (float.TryParse(valueString.Replace('.', ','), out floatValue))
                {
                    result = floatValue;
                }
            }
            else if (originalType == typeof(string).FullName)
            {
                result = valueString;
            }
            // Di�er t�rler i�in gerekirse daha fazla durum ekle

            // Bilinmeyen t�r i�in varsay�lan olarak string d�nd�r
            if (result == null)
            {
                result = valueString;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ConvertFromString Hatas�: {e.Message}");
        }

        Debug.Log($"D�n��t�r�len de�er: {result}");

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

                // �nceki durumu kontrol etmek i�in bir s�zl�k olu�tur
                if (!previousComponentValues.ContainsKey(script))
                {
                    previousComponentValues[script] = new Dictionary<string, object>();
                }

                // Farkl�l�k kontrol� yap
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

            // �nceki de�er var m� kontrol et
            if (previousValues.ContainsKey(fieldName))
            {
                object previousValue = previousValues[fieldName];

                // Farkl�l�k var m� kontrol et
                if (!UnityEngine.Object.Equals(currentValue, previousValue))
                {
                    changesDetected = true;

                    // �nceki de�eri g�ncelle
                    previousValues[fieldName] = currentValue;

                    break;  // Farkl�l�k bulundu�u i�in d�ng�y� sonland�r
                }
            }
            else
            {
                // �nceki de�eri g�ncelle
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
        // De�er de�i�ikli�ini burada i�leyin
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

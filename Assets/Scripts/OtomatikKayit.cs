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
    private List<MonoBehaviour> scriptComponents = new List<MonoBehaviour>();
    //public GameObject draggedGameObject;
    private Dictionary<string, bool> toggleValues = new Dictionary<string, bool>();
    private SerializableData _serializableData = new SerializableData();
    private Dictionary<MonoBehaviour, Dictionary<string, object>> previousComponentValues = new Dictionary<MonoBehaviour, Dictionary<string, object>>();
    public static event Action<string, string, object> OnValueChanged;
    private Dictionary<string, bool> scriptFoldouts = new Dictionary<string, bool>();
    private string currentGameObjectKey = "";
    private List<GameObject> draggedGameObjectsList = new List<GameObject>();
    private bool foldout = true;
    private List<ObjectData> objectDataList = new List<ObjectData>();
    private static bool isDirty = false;


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

        // scriptFoldouts sözlüðünü EditorPrefs'ten yükle
        foreach (var scriptComponent in scriptComponents)
        {
            string key = GetFoldoutKey(scriptComponent);
            scriptFoldouts[scriptComponent.GetType().Name] = EditorPrefs.GetBool(key, true);
        }
        EditorApplication.quitting += OnApplicationQuitting;
        Debug.Log("CombinedManagerWindow etkinleþtirildi");
    }

    private void OnDisable()
    {
        OnValueChanged -= HandleValueChanged;

        // scriptFoldouts sözlüðünü EditorPrefs'e kaydet
        foreach (var scriptComponent in scriptComponents)
        {
            string key = GetFoldoutKey(scriptComponent);
            EditorPrefs.SetBool(key, scriptFoldouts[scriptComponent.GetType().Name]);
        }
        EditorApplication.quitting -= OnApplicationQuitting;
        Debug.Log("CombinedManagerWindow devre dýþý býrakýldý");
    }

    private void OnApplicationQuitting()
    {
        // Oyun kapanýrken veya uygulama kapanýrken çaðrýlacak kod


        SaveAllObjectsToJson();
        Debug.Log("Çalýþtýýý");

    }
    private void Awake()
    {
        LoadJsonValues();
        Debug.Log("CombinedManagerWindow awake metodu çaðrýldý");

    }

    private void OnDestroy()
    {
        //SaveToJson();
        SaveAllObjectsToJson();
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
                        GameObject draggedGameObject = draggedObj as GameObject;

                        if (draggedGameObject != null && !draggedGameObjectsList.Contains(draggedGameObject))
                        {
                            draggedGameObjectsList.Add(draggedGameObject);
                            ScriptleriTara();
                            Repaint();
                            Debug.Log("GameObject sürüklendi ve iþlendi: " + draggedGameObject.name);
                        }
                    }
                }

                Event.current.Use();
                break;
        }

        // Listeye eklenen tüm GameObject'leri göster
        foreach (var gameObject in draggedGameObjectsList)
        {
            GUILayout.BeginHorizontal();

            EditorGUILayout.ObjectField(gameObject, typeof(GameObject), false);

            if (GUILayout.Button("Kaldýr"))
            {
                draggedGameObjectsList.Remove(gameObject);
                // Clear associated data
                scriptComponents.Clear();
                toggleValues.Clear();
                jsonValues.Clear();
                // Optionally update list or perform other actions
            }

            GUILayout.EndHorizontal();
        }

        GUILayout.Space(10f);

        //foldout = EditorGUILayout.Foldout(foldout, "Sürüklenen Game Object'ler", true);

        //if (foldout)
        //{
        //    EditorGUI.indentLevel++;

        //    // Sürüklenen Game Object'leri liste içinde göster
        //    for (int i = draggedGameObjectsList.Count - 1; i >= 0; i--)
        //    {
        //        EditorGUILayout.BeginHorizontal();

        //        EditorGUILayout.ObjectField(draggedGameObjectsList[i], typeof(GameObject), false);

        //        if (GUILayout.Button("Kaldýr"))
        //        {
        //            // GameObject'i listeden çýkar
        //            draggedGameObjectsList.RemoveAt(i);
        //            // Clear associated data
        //            scriptComponents.Clear();
        //            toggleValues.Clear();
        //            jsonValues.Clear();
        //        }

        //        EditorGUILayout.EndHorizontal();
        //    }

        //    EditorGUI.indentLevel--;
        //}

        // Her bir GameObject için scriptleri ve deðerleri göster
        foreach (var draggedGameObject in draggedGameObjectsList)
        {
            if (draggedGameObject != null)
            {
                // GameObject'e ait scriptleri ve deðerleri kontrol et
                MonoBehaviour[] scripts = draggedGameObject.GetComponents<MonoBehaviour>();

                foreach (var script in scripts)
                {
                    GUILayout.BeginHorizontal();

                    scriptFoldouts.TryGetValue(script.GetType().Name, out bool isFoldout);
                    bool newFoldout = EditorGUILayout.Foldout(isFoldout, " " + script.GetType().Name, true);
                    scriptFoldouts[script.GetType().Name] = newFoldout;

                    GUILayout.EndHorizontal();

                    if (newFoldout)
                    {
                        System.Reflection.FieldInfo[] fields = script.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                        foreach (var fieldInfo in fields)
                        {
                            GUILayout.BeginHorizontal();

                            bool showProperty = GetPropertyVisibility(script, fieldInfo.Name);
                            bool newVisibility = EditorGUILayout.ToggleLeft(fieldInfo.Name, showProperty, GUILayout.Width(120));

                            if (newVisibility != showProperty)
                            {
                                SetPropertyVisibility(script, fieldInfo.Name, newVisibility);
                            }

                            object value = fieldInfo.GetValue(script);
                            Type fieldType = fieldInfo.FieldType;

                            GUILayout.Label(":", GUILayout.Width(5));

                            if (fieldType == typeof(int))
                            {
                                int newValue = EditorGUILayout.IntField((int)value, GUILayout.Width(60));
                                fieldInfo.SetValue(script, newValue);
                            }
                            else if (fieldType == typeof(float))
                            {
                                float newValue = EditorGUILayout.FloatField((float)value, GUILayout.Width(60));
                                fieldInfo.SetValue(script, newValue);
                            }
                            else if (fieldType == typeof(string))
                            {
                                string newValue = EditorGUILayout.TextField((string)value, GUILayout.Width(60));
                                fieldInfo.SetValue(script, newValue);
                            }

                            GUILayout.EndHorizontal();

                            if (newVisibility)
                            {
                                string toggleKey = $"{script.GetType().Name}_{fieldInfo.Name}";
                                UpdateJsonValue(script.GetType().Name, fieldInfo.Name, fieldInfo.GetValue(script), toggleValues.ContainsKey(toggleKey) && toggleValues[toggleKey]);
                                toggleValues[toggleKey] = newVisibility;
                            }
                        }
                    }
                }
            }
        }
    }


    private void OnSelectionChange()
    {
        draggedGameObjectsList.Clear();

        foreach (var selectedObject in Selection.objects)
        {
            if (selectedObject is GameObject)
            {
                draggedGameObjectsList.Add(selectedObject as GameObject);
            }
        }

        Repaint();
    }

    private string GetFoldoutKey(MonoBehaviour scriptComponent)
    {
        // gameObject kimliðini de kullanarak bir anahtar oluþtur
        return $"{scriptComponent.GetType().FullName}_{scriptComponent.GetInstanceID()}_{currentGameObjectKey}_Foldout";
    }

    private void ScriptleriTara()
    {
        Debug.Log("Scriptleri taranýyor...");

        scriptComponents.Clear();
        toggleValues.Clear();
        objectDataList.Clear();

        foreach (var gameObject in draggedGameObjectsList)
        {
            if (gameObject != null)
            {
                MonoBehaviour[] scripts = gameObject.GetComponents<MonoBehaviour>();

                foreach (var script in scripts)
                {
                    scriptComponents.Add(script);

                    string toggleKey = $"{script.GetType().Name}_";
                    toggleValues[toggleKey] = false;

                    Debug.Log($"Oyun Öncesi - ScriptleriTara Metodu - Script Component: {script.GetType().Name}");

                    ComponentData componentData = new ComponentData();
                    componentData.ComponentName = script.GetType().Name;

                    System.Reflection.FieldInfo[] fields = script.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    foreach (var fieldInfo in fields)
                    {
                        bool showProperty = GetPropertyVisibility(script, fieldInfo.Name);

                        object value = fieldInfo.GetValue(script);
                        Type fieldType = fieldInfo.FieldType;

                        if (showProperty)
                        {
                            string toggleKeyForField = $"{script.GetType().Name}_{fieldInfo.Name}";
                            toggleValues[toggleKeyForField] = false;

                            // Eklenen toggle'larý göster
                            Debug.Log($"Toggle: {toggleKeyForField}, Value: {toggleValues[toggleKeyForField]}");
                        }

                        componentData.FieldValues[fieldInfo.Name] = new FieldData { Value = value, ToggleKey = $"{script.GetType().Name}_{fieldInfo.Name}" };
                    }

                    ObjectData objectData = new ObjectData();
                    objectData.ObjectName = gameObject.name;
                    objectData.ToggleValues[toggleKey] = toggleValues[toggleKey];
                    objectData.ComponentDataList.Add(componentData);

                    objectDataList.Add(objectData);
                }
            }
        }

        Debug.Log($"objectDataList Ýçeriði: {JsonUtility.ToJson(objectDataList, true)}");
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
    [Serializable]
    public class ObjectData
    {
        public string ObjectName;
        public bool Toggle;
        public Dictionary<string, bool> ToggleValues = new Dictionary<string, bool>();
        public List<ComponentData> ComponentDataList = new List<ComponentData>();
    }

    [Serializable]
    public class ComponentData
    {
        public string ComponentName;
        public Dictionary<string, object> FieldValues = new Dictionary<string, object>();
    }

    [Serializable]
    public class FieldData
    {
        public object Value;
        public string ToggleKey;
        public bool Toggle;
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
        public bool toggleState;
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
    private bool _hasChanges = false;

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

        // Deðeri güncelle
        var oldValue = existingEntry.Value.ContainsKey(propertyName) ? existingEntry.Value[propertyName] : null;
        if (!Equals(oldValue, value))
        {
            existingEntry.Value[propertyName] = value;
            _hasChanges = true; // Deðiþiklik tespit edildi
        }

        // _jsonValues listesini oluþturun
        List<JsonData> jsonDataList = jsonValues.SelectMany(entry =>
        {
            return entry.Value.Select(kv => new JsonData
            {
                key = $"{entry.Key}_{kv.Key}",
                componentName = entry.Key,
                propertyName = kv.Key,
                originalType = GetOriginalTypeString(kv.Value),
                value = ConvertToString(kv.Value)
            });
        }).ToList();

        // jsonValues listesini güncelleyin
        jsonValues = jsonDataList.GroupBy(jd => jd.componentName)
            .ToDictionary(
                group => group.Key,
                group => group.ToDictionary(item => item.propertyName, item => ConvertFromString(item.originalType, item.value))
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
            //Debug.Log($"Key: {jsonData.key}, Component: {jsonData.componentName}, Property: {jsonData.propertyName}, OriginalType: {jsonData.originalType}, Value: {jsonData.value}");
        }

        // Deðiþiklik varsa kaydet
        if (_hasChanges)
        {
            SaveAllObjectsToJson();
            _hasChanges = false; // Kaydetme sonrasý deðiþiklikleri sýfýrla
        }
    }

    private bool GetToggleState(string componentName, string propertyName)
    {
        string toggleKey = $"{componentName}_{propertyName}";
        return toggleValues.ContainsKey(toggleKey) && toggleValues[toggleKey];
    }

    private void SaveAllObjectsToJson()
    {
        try
        {
            List<JsonEntry> allObjectsData = new List<JsonEntry>();

            foreach (var draggedObject in draggedGameObjectsList)
            {
                JsonEntry entry = new JsonEntry();
                entry.Key = draggedObject.name;
                entry.Values = new Dictionary<string, object>();

                // Sürüklenen objenin üzerindeki bileþenlerin deðerlerini ekleyin
                MonoBehaviour[] scripts = draggedObject.GetComponents<MonoBehaviour>();

                foreach (var script in scripts)
                {
                    string componentName = script.GetType().Name;
                    entry.Values[componentName] = new Dictionary<string, object>();

                    System.Reflection.FieldInfo[] fields = script.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    foreach (var fieldInfo in fields)
                    {
                        // Yalnýzca toggle durumu true olanlarý kaydet
                        string toggleKey = $"{componentName}_{fieldInfo.Name}";
                        if (toggleValues.ContainsKey(toggleKey) && toggleValues[toggleKey])
                        {
                            object value = fieldInfo.GetValue(script);
                            string fieldName = fieldInfo.Name;

                            // Burada entry'nin tipini JsonEntry olarak belirtiyoruz
                            ((Dictionary<string, object>)entry.Values[componentName])[fieldName] = value;
                        }
                    }
                }

                allObjectsData.Add(entry);
            }

            string saveDataPath = "Assets/Resources/saveData/savedData.json";
            _serializableData._jsonValues = allObjectsData
                .SelectMany(entry =>
                    entry.Values.SelectMany(component =>
                    {
                        // Burada component.Value'ýn tipini belirtiyoruz
                        Dictionary<string, object> componentValues = (Dictionary<string, object>)component.Value;

                        return componentValues.Select(kv => new JsonData
                        {
                            key = $"{entry.Key}_{component.Key}_{kv.Key}",
                            componentName = component.Key,
                            propertyName = kv.Key,
                            originalType = GetOriginalTypeString(kv.Value), // Orijinal türü string olarak sakla
                            value = ConvertToString(kv.Value)
                        });
                    })
                )
                .ToList();

            string json = JsonUtility.ToJson(_serializableData, true);
            File.WriteAllText(saveDataPath, json);

            Debug.Log($"Tüm alanlar JSON olarak kaydedildi: {saveDataPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveAllObjectsToJson Hatasý: {e.Message}");
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
        //Debug.Log($"String'den deðer dönüþtürülüyor: {valueString}");

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

        //Debug.Log($"Dönüþtürülen deðer: {result}");

        return result;
    }


    private bool CheckForChanges()
    {
        bool changesDetected = false;

        foreach (var draggedObject in draggedGameObjectsList)
        {
            MonoBehaviour[] scripts = draggedObject.GetComponents<MonoBehaviour>();

            foreach (var script in scripts)
            {
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
                if (!Equals(currentValue, previousValue))
                {
                    changesDetected = true;

                    // Önceki deðeri güncelle
                    previousValues[fieldName] = currentValue;
                }
            }
            else
            {
                // Önceki deðeri güncelle
                previousValues[fieldName] = currentValue;
            }
        }

        return changesDetected;
    }

    private MonoBehaviour FindScriptComponent(string componentName)
    {
        foreach (var gameObject in draggedGameObjectsList)
        {
            if (gameObject != null)
            {
                MonoBehaviour[] scripts = gameObject.GetComponents<MonoBehaviour>();

                foreach (var script in scripts)
                {
                    if (script.GetType().Name == componentName)
                    {
                        // Debug.Log($"Script component found for {componentName}: {script.GetType().Name}");
                        return script;
                    }
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

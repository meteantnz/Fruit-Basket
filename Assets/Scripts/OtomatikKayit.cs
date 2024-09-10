using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using System.Globalization;
using System.Linq;
using System.Collections;
using Unity.VisualScripting;

public class CombinedManagerWindow : EditorWindow
{
    //private string jsonFilePath = "Assets/Resources/saveData/savedData.json";
    private List<KeyValuePair<string, Dictionary<string, object>>> jsonValues = new List<KeyValuePair<string, Dictionary<string, object>>>();
    private List<MonoBehaviour> scriptComponents = new List<MonoBehaviour>();
    //public GameObject draggedGameObject;
    private Dictionary<string, bool> toggleValues = new Dictionary<string, bool>();
    private SerializableData _serializableData = new SerializableData();
    //private Dictionary<MonoBehaviour, Dictionary<string, object>> previousComponentValues = new Dictionary<MonoBehaviour, Dictionary<string, object>>();
    public static event Action<string, string, object> OnValueChanged;
    private Dictionary<string, bool> scriptFoldouts = new Dictionary<string, bool>();
    private string currentGameObjectKey = "";
    private List<GameObject> draggedGameObjectsList = new List<GameObject>();
    //private bool foldout = true;
    private List<ObjectData> objectDataList = new List<ObjectData>();
    private static bool isDirty = false;
    private bool hasLoadedJsonData = false;


    [MenuItem("Window/Otomatik Kayýt Sistemi")]
    public static void ShowWindow()
    {
        GetWindow<CombinedManagerWindow>("Otomatik Kayýt Sistemi");
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
        //EditorApplication.quitting += OnApplicationQuitting;
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

        Debug.Log("CombinedManagerWindow devre dýþý býrakýldý");
    }
    //private void OnApplicationQuitting()
    //{
    //    // Oyun kapanýrken veya uygulama kapanýrken çaðrýlacak kod


    //    SaveAllObjectsToJson();
    //    Debug.Log("Çalýþtýýý");

    //}
    private void Awake()
    {
        SaveAllObjectsToJson();
        LoadJsonValues();
        Debug.Log("CombinedManagerWindow awake metodu çaðrýldý");

    }

    private void OnDestroy()
    {
        SaveAllObjectsToJson();
        OnValueChanged -= HandleValueChanged;
        Debug.Log("CombinedManagerWindow destroy metodu çaðrýldý");
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (!hasLoadedJsonData)
            {
                LoadJsonValues();
                hasLoadedJsonData = true;
                Debug.Log("Oyun durduruldu, JSON verileri yüklendi.");
            }
        }
        else
        {
            if (hasLoadedJsonData)
            {
                hasLoadedJsonData = false;
                Debug.Log("Oyun baþlatýldý, JSON verileri yeniden yüklendi.");
                LoadJsonValues();
            }

            // Deðiþiklikleri kontrol et ve kaydet
            
        }
        CheckForChanges();
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
        #region
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
        #endregion
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

    //OnSelectionChance týklanýlan objenin verilerini panele otomatik tanýmlar.
    //private void OnSelectionChange()
    //{
    //    draggedGameObjectsList.Clear();

    //    foreach (var selectedObject in Selection.objects)
    //    {
    //        if (selectedObject is GameObject)
    //        {
    //            draggedGameObjectsList.Add(selectedObject as GameObject);
    //        }
    //    }

    //    Repaint();
    //}

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
    //private bool _hasChanges = false;

    public void UpdateJsonValue(string componentName, string propertyName, object value, bool toggleState)
    {
        // Mevcut JSON verilerini güncelle
        var jsonData = _serializableData._jsonValues
            .FirstOrDefault(jd => jd.componentName == componentName && jd.propertyName == propertyName);

        if (jsonData != null)
        {
            jsonData.value = ConvertToString(value);
            jsonData.originalType = GetOriginalTypeString(value);
            jsonData.toggleState = toggleState;
        }
        else
        {
            _serializableData._jsonValues.Add(new JsonData
            {
                key = $"{componentName}_{propertyName}",
                componentName = componentName,
                propertyName = propertyName,
                originalType = GetOriginalTypeString(value),
                value = ConvertToString(value),
                toggleState = toggleState

            });
        }
        //Debug.Log($"Updating JSON value for {componentName}.{propertyName}. New Value: {ConvertToString(value)}");

        OnValueChanged?.Invoke(componentName, propertyName, value);
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
            // JSON verilerini güncellenmiþ haliyle kaydet
            string path = Application.persistentDataPath + "/saveData.json";
            string json = JsonUtility.ToJson(_serializableData, true);
            File.WriteAllText(path, json);
            Debug.Log($"Tüm alanlar JSON olarak kaydedildi: {path}");
            Debug.Log($"json:{json}");
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
            string path = Application.persistentDataPath + "/saveData.json";
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                _serializableData = JsonUtility.FromJson<SerializableData>(json);

                // Yüklenen verilerin geçersiz veya boþ olup olmadýðýný kontrol edin
                if (_serializableData == null || _serializableData._jsonValues == null)
                {
                    Debug.LogWarning("Yüklenen veri geçersiz veya boþ.");
                    _serializableData = new SerializableData(); // Boþ bir veri oluþtur
                    _serializableData._jsonValues = new List<JsonData>();
                }
                else
                {
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
                                Debug.Log($"Deðer yüklendi: {jsonData.componentName}.{jsonData.propertyName} = {loadedValue}");
                            }
                        }
                    }
                    Debug.Log("JSON verileri baþarýyla yüklendi.");
                }
            }
            else
            {
                Debug.LogWarning("JSON dosyasý bulunamadý.");
                _serializableData = new SerializableData(); // Boþ bir veri oluþtur
                _serializableData._jsonValues = new List<JsonData>();
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"LoadJsonValues Hatasý: {e.Message}");
            _serializableData = new SerializableData(); // Hata durumunda da boþ veri oluþtur
            _serializableData._jsonValues = new List<JsonData>();
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
            Type type = value.GetType();
            if (type == typeof(int)) return "System.Int32";
            if (type == typeof(float)) return "System.Single";
            if (type == typeof(bool)) return "System.Boolean";
            if (type == typeof(string)) return "System.String";
            // Diðer türler için gerekirse daha fazla durum ekleyebilirsiniz
            return type.FullName;
        }
    }

    private string ConvertToString(object value)
    {
        if (value == null)
        {
            return "{}"; // Boþ deðerler için uygun bir temsil
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

    private object ConvertFromString(string originalType, string valueString)
    {
        if (string.IsNullOrEmpty(valueString) || valueString == "null")
        {
            return null;
        }

        try
        {
            switch (originalType)
            {
                case "System.Int32":
                    if (int.TryParse(valueString, out int intValue))
                    {
                        return intValue;
                    }
                    break;

                case "System.Single":
                    if (float.TryParse(valueString, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float floatValue))
                    {
                        return floatValue;
                    }
                    break;

                case "System.Boolean":
                    if (bool.TryParse(valueString, out bool boolValue))
                    {
                        return boolValue;
                    }
                    break;

                case "System.String":
                    return valueString;

                default:
                    Debug.LogError($"Bilinmeyen tür: {originalType}. Deðer: {valueString}");
                    break;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"ConvertFromString Hatasý: {e.Message}. Tür: {originalType}, Deðer: {valueString}");
        }

        // Hata durumunda varsayýlan olarak null döndür
        return null;
    }


    private void CheckForChanges()
    {
        if(!Application.isPlaying)
        {
            Repaint();
        }
        foreach (var jsonData in _serializableData._jsonValues)
        {
            MonoBehaviour script = FindScriptComponent(jsonData.componentName);
            if (script != null)
            {
                System.Reflection.FieldInfo fieldInfo = script.GetType().GetField(jsonData.propertyName);
                if (fieldInfo != null)
                {
                    object currentValue = fieldInfo.GetValue(script);
                    object previousValue = ConvertFromString(jsonData.originalType, jsonData.value);
                    if (!currentValue.Equals(previousValue))
                    {
                        // Deðiþiklik olduðunu belirten bayrak
                        isDirty = true;
                        Debug.Log($"Deðiþiklik algýlandý: {jsonData.componentName}.{jsonData.propertyName} önceki deðer = {previousValue}, yeni deðer = {currentValue}");
                        //Debug.Log("Repaint edildidspþgjdsfpogjdsfopgdsfpogjdsfgdjfopgdpsofjdfopgj");
                        Debug.Log(isDirty);
                        jsonData.value = ConvertToString(currentValue); // Yeni deðeri JSON'da güncelle
                        SaveAllObjectsToJson();
                        Repaint();
                    }
                }
            }
        }
    }

    //private void CheckForChanges(MonoBehaviour script)
    //{
    //    foreach (var jsonData in _serializableData._jsonValues)
    //    {
    //        if (jsonData.componentName == script.GetType().Name)
    //        {
    //            System.Reflection.FieldInfo fieldInfo = script.GetType().GetField(jsonData.propertyName);
    //            if (fieldInfo != null)
    //            {
    //                object currentValue = fieldInfo.GetValue(script);
    //                object previousValue = ConvertFromString(jsonData.originalType, jsonData.value);

    //                if (!currentValue.Equals(previousValue))
    //                {
    //                    //Debug.Log($"Deðiþiklik algýlandý: {jsonData.componentName}.{jsonData.propertyName} önceki deðer = {previousValue}, yeni deðer = {currentValue}");

    //                    // Deðiþiklik algýlandýktan sonra gerekli iþlemleri yapýn
    //                    jsonData.value = ConvertToString(currentValue); // Yeni deðeri JSON'da güncelle
    //                }
    //            }
    //        }
    //    }
    //}

    private MonoBehaviour FindScriptComponent(string componentName)
    {
        foreach (var gameObject in FindObjectsOfType<GameObject>())
        {
            if (gameObject != null)
            {
                MonoBehaviour[] scripts = gameObject.GetComponents<MonoBehaviour>();

                foreach (var script in scripts)
                {
                    if (script.GetType().Name == componentName)
                    {
                        return script;
                    }
                }
            }
        }

        Debug.LogWarning($"Script component bulunamadý: {componentName}");
        return null;
    }

    [Serializable]
    private class JsonWrapper
    {
        public object Value;
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

    private void LogSerializableData()
    {
        if (_serializableData != null && _serializableData._jsonValues != null)
        {
            Debug.Log("Logging _serializableData contents:");

            foreach (var jsonData in _serializableData._jsonValues)
            {
                Debug.Log($"Key: {jsonData.key}");
                Debug.Log($"Component Name: {jsonData.componentName}");
                Debug.Log($"Property Name: {jsonData.propertyName}");
                Debug.Log($"Original Type: {jsonData.originalType}");
                Debug.Log($"Value: {jsonData.value}");
                Debug.Log($"Toggle State: {jsonData.toggleState}");
                Debug.Log("--------");
            }
        }
        else
        {
            Debug.LogWarning("_serializableData or _jsonValues is null.");
        }
    }
}

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
    private Dictionary<string, bool> scriptFoldouts = new Dictionary<string, bool>();
    private string currentGameObjectKey = "";
    private List<GameObject> draggedGameObjectsList = new List<GameObject>();
    private bool foldout = true;
    private List<ObjectData> objectDataList = new List<ObjectData>();
    private bool updateJsonValueFlag = false;
    private Dictionary<string, Dictionary<string, object>> previousValuesDict = new Dictionary<string, Dictionary<string, object>>();


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

        // scriptFoldouts s�zl���n� EditorPrefs'ten y�kle
        foreach (var scriptComponent in scriptComponents)
        {
            string key = GetFoldoutKey(scriptComponent);
            scriptFoldouts[scriptComponent.GetType().Name] = EditorPrefs.GetBool(key, true);
        }

        Debug.Log("CombinedManagerWindow etkinle�tirildi");
    }

    private void OnDisable()
    {
        OnValueChanged -= HandleValueChanged;

        // scriptFoldouts s�zl���n� EditorPrefs'e kaydet
        foreach (var scriptComponent in scriptComponents)
        {
            string key = GetFoldoutKey(scriptComponent);
            EditorPrefs.SetBool(key, scriptFoldouts[scriptComponent.GetType().Name]);
        }

        Debug.Log("CombinedManagerWindow devre d��� b�rak�ld�");
    }
    private void Awake()
    {
        LoadJsonValues();
        Debug.Log("CombinedManagerWindow awake metodu �a�r�ld�");

    }

    private void OnDestroy()
    {
        //SaveToJson();
        SaveAllObjectsToJson();
        OnValueChanged -= HandleValueChanged;
        Debug.Log("CombinedManagerWindow destroy metodu �a�r�ld�");
    }

    private void Update()
    {
        // De�er de�i�ti�inde Repaint fonksiyonunu �a��rmak i�in kontrol
        bool changesDetectedList = CheckForAnyComponentChanges();
        bool changesDetected = CheckForChanges();

        if (changesDetectedList || changesDetected)
        {
            updateJsonValueFlag = true;
            SaveAllObjectsToJson();
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
                        updateJsonValueFlag = true;
                    }
                    Debug.Log("GameObject s�r�klendi ve i�lendi");
                }

                Event.current.Use();
                break;
        }


        if (draggedGameObject != null)
        {
            //SaveToJson();
            SaveAllObjectsToJson();
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

            EditorGUILayout.Space(5f);

            // Objeyi otomatik olarak listeye ekle
            if (draggedGameObject != null && !draggedGameObjectsList.Contains(draggedGameObject))
            {
                draggedGameObjectsList.Add(draggedGameObject);
                Repaint();
            }

            for (int i = 0; i < scriptComponents.Count; i++)
            {
                GUILayout.BeginHorizontal();

                scriptFoldouts.TryGetValue(scriptComponents[i].GetType().Name, out bool isFoldout);
                bool newFoldout = EditorGUILayout.Foldout(isFoldout, " " + scriptComponents[i].GetType().Name, true);
                scriptFoldouts[scriptComponents[i].GetType().Name] = newFoldout;

                GUILayout.EndHorizontal();

                if (newFoldout)
                {
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
                            toggleValues[toggleKey] = newVisibility;

                            if (updateJsonValueFlag)
                            {
                                UpdateJsonValue(scriptComponents[i].GetType().Name, fieldInfo.Name, fieldInfo.GetValue(scriptComponents[i]), toggleValues.ContainsKey(toggleKey) && toggleValues[toggleKey]);
                                updateJsonValueFlag = false;
                                Debug.Log("dspfgjdpsgjdsfg");
                            }
                        }
                    }
                }
            }

            GUILayout.Space(10f);

            foldout = EditorGUILayout.Foldout(foldout, "S�r�klenen Game Object'ler", true);

            if (foldout)
            {
                EditorGUI.indentLevel++;

                // S�r�klenen Game Object'leri liste i�inde g�ster
                for (int i = draggedGameObjectsList.Count - 1; i >= 0; i--)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUILayout.ObjectField(draggedGameObjectsList[i], typeof(GameObject), false);

                    if (GUILayout.Button("Kald�r"))
                    {
                        if (draggedGameObjectsList[i] != draggedGameObject)
                        {
                            // E�er referanslar ayn� de�ilse, yani s�r�klenen obje ile listedeki obje farkl�ysa
                            draggedGameObjectsList.RemoveAt(i);
                        }
                        // E�er referanslar ayn�ysa, yani s�r�klenen obje ile listedeki obje ayn�ysa, bo� i�lem yap
                    }

                    EditorGUILayout.EndHorizontal();
                }

                // T�m d�ng� bittikten sonra Repaint() �a�r�s� yap

                EditorGUI.indentLevel--;

                // draggedGameObject'u otomatik olarak listeye ekle
                if (draggedGameObject != null && !draggedGameObjectsList.Contains(draggedGameObject))
                {
                    draggedGameObjectsList.Add(draggedGameObject);
                    Repaint();
                }

            }
        }
        if (draggedGameObjectsList.Count > 0)
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("S�r�klenen Game Object'ler");

            foreach (var obj in draggedGameObjectsList)
            {
                GUILayout.BeginVertical(EditorStyles.helpBox);

                EditorGUILayout.LabelField($"Game Object: {obj.name}");

                foreach (var component in obj.GetComponents<MonoBehaviour>())
                {
                    GUILayout.BeginVertical(EditorStyles.helpBox);

                    EditorGUILayout.LabelField($"Component: {component.GetType().Name}");

                    System.Reflection.FieldInfo[] fields = component.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    foreach (var fieldInfo in fields)
                    {
                        GUILayout.BeginHorizontal();

                        bool showProperty = GetPropertyVisibility(component, fieldInfo.Name);
                        bool newVisibility = EditorGUILayout.ToggleLeft(fieldInfo.Name, showProperty, GUILayout.Width(120));

                        if (newVisibility != showProperty)
                        {
                            SetPropertyVisibility(component, fieldInfo.Name, newVisibility);
                        }

                        object value = fieldInfo.GetValue(component);
                        Type fieldType = fieldInfo.FieldType;

                        GUILayout.Label(":", GUILayout.Width(5));

                        if (fieldType == typeof(int))
                        {
                            int newValue = EditorGUILayout.IntField((int)value, GUILayout.Width(60));
                            fieldInfo.SetValue(component, newValue);
                        }
                        else if (fieldType == typeof(float))
                        {
                            float newValue = EditorGUILayout.FloatField((float)value, GUILayout.Width(60));
                            fieldInfo.SetValue(component, newValue);
                        }
                        else if (fieldType == typeof(string))
                        {
                            string newValue = EditorGUILayout.TextField((string)value, GUILayout.Width(60));
                            fieldInfo.SetValue(component, newValue);
                        }

                        GUILayout.EndHorizontal();

                        if (newVisibility)
                        {
                            string toggleKey = $"{component.GetType().Name}_{fieldInfo.Name}";
                            
                            toggleValues[toggleKey] = newVisibility;

                            if (updateJsonValueFlag)
                            {
                                UpdateJsonValue(component.GetType().Name, fieldInfo.Name, fieldInfo.GetValue(component), toggleValues.ContainsKey(toggleKey) && toggleValues[toggleKey]);
                                updateJsonValueFlag = false;
                            }

                        }
                    }

                    GUILayout.EndVertical();
                }

                GUILayout.EndVertical();
            }

            GUILayout.EndVertical();
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
        // gameObject kimli�ini de kullanarak bir anahtar olu�tur
        return $"{scriptComponent.GetType().FullName}_{scriptComponent.GetInstanceID()}_{currentGameObjectKey}_Foldout";
    }

    private void ScriptleriTara()
    {
        if (draggedGameObject != null)
        {
            Debug.Log("Scriptleri taran�yor...");

            scriptComponents.Clear();
            toggleValues.Clear();
            objectDataList.Clear();

            MonoBehaviour[] scripts = draggedGameObject.GetComponents<MonoBehaviour>();

            foreach (var script in scripts)
            {
                scriptComponents.Add(script);

                string toggleKey = $"{script.GetType().Name}_";
                toggleValues[toggleKey] = false;

                Debug.Log($"Oyun �ncesi - ScriptleriTara Metodu - Script Component: {script.GetType().Name}");

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

                        // Eklenen toggle'lar� g�ster
                        Debug.Log($"Toggle: {toggleKeyForField}, Value: {toggleValues[toggleKeyForField]}");
                    }

                    componentData.FieldValues[fieldInfo.Name] = new FieldData { Value = value, ToggleKey = $"{script.GetType().Name}_{fieldInfo.Name}" };
                }

                ObjectData objectData = new ObjectData();
                objectData.ObjectName = draggedGameObject.name;
                objectData.ToggleValues[toggleKey] = toggleValues[toggleKey];
                objectData.ComponentDataList.Add(componentData);

                objectDataList.Add(objectData);
            }
        }

        Debug.Log($"objectDataList ��eri�i: {JsonUtility.ToJson(objectDataList, true)}");
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
                originalType = GetOriginalTypeString(kv.Value),
                value = ConvertToString(kv.Value),
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
        //SaveToJson();
        SaveAllObjectsToJson();
    }

    private bool GetToggleState(string componentName, string propertyName)
    {
        string toggleKey = $"{componentName}_{propertyName}";
        return toggleValues.ContainsKey(toggleKey) && toggleValues[toggleKey];
    }



    private void SaveToJson()
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

                // S�r�klenen objenin �zerindeki bile�enlerin de�erlerini ekleyin
                MonoBehaviour[] scripts = draggedObject.GetComponents<MonoBehaviour>();

                foreach (var script in scripts)
                {
                    string componentName = script.GetType().Name;
                    entry.Values[componentName] = new Dictionary<string, object>();

                    System.Reflection.FieldInfo[] fields = script.GetType().GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                    foreach (var fieldInfo in fields)
                    {
                        // Yaln�zca toggle durumu true olanlar� kaydet
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
                        // Burada component.Value'�n tipini belirtiyoruz
                        Dictionary<string, object> componentValues = (Dictionary<string, object>)component.Value;

                        return componentValues.Select(kv => new JsonData
                        {
                            key = $"{entry.Key}_{component.Key}_{kv.Key}",
                            componentName = component.Key,
                            propertyName = kv.Key,
                            originalType = GetOriginalTypeString(kv.Value), // Orijinal t�r� string olarak sakla
                            value = ConvertToString(kv.Value)
                        });
                    })
                )
                .ToList();

            string json = JsonUtility.ToJson(_serializableData, true);
            File.WriteAllText(saveDataPath, json);

            Debug.Log($"T�m alanlar JSON olarak kaydedildi: {saveDataPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"SaveAllObjectsToJson Hatas�: {e.Message}");
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
        //Debug.Log($"String'den de�er d�n��t�r�l�yor: {valueString}");

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

        //Debug.Log($"D�n��t�r�len de�er: {result}");

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

        // Yaln�zca alt de�erlerde de�i�iklik oldu�unda Repaint fonksiyonunu �a��r
        if (changesDetected)
        {
            Repaint();
        }

        return changesDetected;
    }

    private bool CheckForChanges(MonoBehaviour script)
    {
        bool changesDetected = false;

        // previousComponentValues i�inde script anahtar�n�n olup olmad���n� kontrol et
        if (!previousComponentValues.ContainsKey(script))
        {
            // E�er anahtar yoksa, yeni bir Dictionary olu�tur
            previousComponentValues[script] = new Dictionary<string, object>();
        }

        Dictionary<string, object> previousValues = previousComponentValues[script];

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

        return changesDetected;
    }

    private bool CheckForAnyComponentChanges()
{
    foreach (var draggedObject in draggedGameObjectsList)
    {
        MonoBehaviour[] scripts = draggedObject.GetComponents<MonoBehaviour>();

        foreach (var script in scripts)
        {
            // De�i�iklik kontrol�
            if (CheckForChanges(script))
            {
                return true; // Herhangi bir de�i�iklik bulundu�unda true d�nd�r
            }
        }
    }

    return false; // Hi�bir de�i�iklik bulunamad���nda false d�nd�r
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
                    //Debug.Log($"Script component found for {componentName}: {script.GetType().Name}");
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

        // draggedGameObjectsList i�indeki her objenin bile�enlerini g�ncelle
        foreach (var draggedObject in draggedGameObjectsList)
        {
            MonoBehaviour[] scripts = draggedObject.GetComponents<MonoBehaviour>();

            foreach (MonoBehaviour innerScript in scripts)
            {
                if (innerScript.GetType().Name == componentName)
                {
                    System.Reflection.FieldInfo fieldInfo = innerScript.GetType().GetField(propertyName);
                    if (fieldInfo != null)
                    {
                        fieldInfo.SetValue(innerScript, newValue);
                        
                    }
                }
            }
        }
    }
}

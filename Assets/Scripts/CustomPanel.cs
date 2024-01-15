using UnityEngine;
using UnityEditor;
using System.IO;

[System.Serializable]
public class SaveData
{
    public float value1;
    public int value2;
    // Ek özellikler ekleyebilirsiniz.

    public string ToJson()
    {
        return JsonUtility.ToJson(this);
    }

    public static SaveData FromJson(string json)
    {
        return JsonUtility.FromJson<SaveData>(json);
    }
}

public interface ILoadable
{
    void LoadData(SaveData data);
}

public class EditorPanel : EditorWindow
{
    private SaveData saveData;
    private GameObject selectedObject;

    [MenuItem("Window/Editor Panel")]
    static void OpenWindow()
    {
        EditorPanel window = (EditorPanel)EditorWindow.GetWindow(typeof(EditorPanel));
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Editor Panel", EditorStyles.boldLabel);

        selectedObject = EditorGUILayout.ObjectField("Select Object", selectedObject, typeof(GameObject), true) as GameObject;

        if (GUILayout.Button("Load Object to Panel"))
        {
            LoadObjectToPanel(selectedObject);
        }

        if (saveData != null)
        {
            // Panelde gösterilecek diðer özellikleri ekleyebilirsiniz.
            saveData.value1 = EditorGUILayout.FloatField("Value 1", saveData.value1);
            saveData.value2 = EditorGUILayout.IntField("Value 2", saveData.value2);

            if (GUILayout.Button("Save to JSON"))
            {
                SaveToJson();
            }
        }
    }

    private void LoadObjectToPanel(GameObject obj)
    {
        if (obj != null)
        {
            ILoadable loadable = obj.GetComponent<ILoadable>();
            if (loadable != null)
            {
                saveData = new SaveData();
                loadable.LoadData(saveData);
            }
            else
            {
                Debug.LogError("Selected object does not implement ILoadable interface.");
            }
        }
        else
        {
            Debug.LogError("No object selected.");
        }
    }

    private void SaveToJson()
    {
        string path = EditorUtility.SaveFilePanel("Save JSON", "", "data.json", "json");
        if (path.Length != 0)
        {
            File.WriteAllText(path, saveData.ToJson());
            Debug.Log("Data saved to " + path);
        }
    }
}

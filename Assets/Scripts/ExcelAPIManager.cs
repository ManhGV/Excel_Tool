using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

public class ExcelAPIManager : MonoBehaviour
{
    public static ExcelAPIManager Instance { get; private set; }

    private string apiUrl = "";
    private bool isLoading = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Khoi tao voi URL deployment
    public void Initialize(string deploymentUrl)
    {
        apiUrl = deploymentUrl;
        Debug.Log($"Excel API initialized with URL: {apiUrl}");
    }

    // Load data tu Google Sheets
    public void LoadData(System.Action<OrderData[]> onSuccess, System.Action<string> onError)
    {
        if (string.IsNullOrEmpty(apiUrl))
        {
            onError?.Invoke("API URL not initialized");
            return;
        }

        StartCoroutine(GetDataCoroutine(onSuccess, onError));
    }

    private IEnumerator GetDataCoroutine(System.Action<OrderData[]> onSuccess, System.Action<string> onError)
    {
        isLoading = true;
        
        using (UnityWebRequest www = UnityWebRequest.Get(apiUrl))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string json = www.downloadHandler.text;
                    Debug.Log($"Received data: {json.Substring(0, Mathf.Min(2000, json.Length))}...");

                    // Parse JSON array
                    var orders = ParseOrdersFromJson(json);
                    onSuccess?.Invoke(orders);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing data: {e.Message}");
                    onError?.Invoke($"Error parsing data: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Error loading data: {www.error}");
                onError?.Invoke($"Error loading data: {www.error}");
            }
        }

        isLoading = false;
    }

    // Parse JSON response to OrderData array
    private OrderData[] ParseOrdersFromJson(string json)
    {
        List<OrderData> orders = new List<OrderData>();
        
        try
        {
            json = json.Trim();
            
            // Skip the outer [ ]
            if (json.StartsWith("[") && json.EndsWith("]"))
            {
                json = json.Substring(1, json.Length - 2);
            }
            
            // Find each object { ... }
            int depth = 0;
            string currentObject = "";
            bool inString = false;
            char prevChar = ' ';
            
            for (int i = 0; i < json.Length; i++)
            {
                char c = json[i];
                
                // Track string boundaries (ignore escaped quotes)
                if (c == '"' && prevChar != '\\')
                    inString = !inString;
                
                if (!inString)
                {
                    if (c == '{') depth++;
                    if (c == '}') depth--;
                }
                
                if (depth > 0 || c == '{')
                    currentObject += c;
                
                // When object is complete
                if (depth == 0 && currentObject.Length > 0 && !inString)
                {
                    if (c == '}' || i == json.Length - 1)
                    {
                        if (c == '}')
                            currentObject += c;
                        
                        try
                        {
                            var dict = ParseJsonObject(currentObject.Trim());
                            if (dict != null && dict.Count > 0)
                            {
                                // Debug: log dictionary keys
                                if (orders.Count == 0)
                                {
                                    string keys = "";
                                    foreach (var k in dict.Keys)
                                    {
                                        keys += k + ", ";
                                    }
                                    Debug.Log($"Dict keys: {keys}");
                                }
                                
                                var order = OrderData.FromDictionary(dict);
                                orders.Add(order);
                                Debug.Log($"Parsed order: {order.TenNguoiNhan}");
                            }
                        }
                        catch (System.Exception e)
                        {
                            Debug.LogWarning($"Error parsing object: {currentObject}. Error: {e.Message}");
                        }
                        currentObject = "";
                    }
                }
                
                prevChar = c;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing JSON: {e.Message}");
        }
        
        Debug.Log($"Total orders parsed: {orders.Count}");
        return orders.ToArray();
    }
    
    private Dictionary<string, object> ParseJsonObject(string jsonStr)
    {
        var dict = new Dictionary<string, object>();
        jsonStr = jsonStr.Trim();
        
        if (!jsonStr.StartsWith("{") || !jsonStr.EndsWith("}"))
            return dict;
        
        // Remove { }
        jsonStr = jsonStr.Substring(1, jsonStr.Length - 2);
        
        // Split by commas (but not inside strings or nested objects)
        List<string> pairs = new List<string>();
        string current = "";
        bool inString = false;
        int depth = 0;
        char prevChar = ' ';
        
        for (int i = 0; i < jsonStr.Length; i++)
        {
            char c = jsonStr[i];
            
            if (c == '"' && prevChar != '\\')
                inString = !inString;
            
            if (!inString)
            {
                if (c == '{' || c == '[') depth++;
                if (c == '}' || c == ']') depth--;
                if (c == ',' && depth == 0)
                {
                    pairs.Add(current.Trim());
                    current = "";
                    prevChar = c;
                    continue;
                }
            }
            
            current += c;
            prevChar = c;
        }
        
        if (current.Length > 0)
            pairs.Add(current.Trim());
        
        // Parse each key-value pair
        foreach (var pair in pairs)
        {
            int colonIdx = pair.IndexOf(':');
            if (colonIdx < 0) continue;
            
            string key = pair.Substring(0, colonIdx).Trim().Trim('"');
            string value = pair.Substring(colonIdx + 1).Trim();
            
            // Remove quotes from string values
            if (value.StartsWith("\"") && value.EndsWith("\""))
                value = value.Substring(1, value.Length - 2).Replace("\\\"", "\"");
            
            dict[key] = value;
        }
        
        return dict;
    }

    // Push data len Google Sheets
    public void PushData(OrderData[] orders, System.Action<bool> onComplete, System.Action<string> onError)
    {
        if (string.IsNullOrEmpty(apiUrl))
        {
            onError?.Invoke("API URL not initialized");
            return;
        }

        StartCoroutine(PostDataCoroutine(orders, onComplete, onError));
    }

    private IEnumerator PostDataCoroutine(OrderData[] orders, System.Action<bool> onComplete, System.Action<string> onError)
    {
        isLoading = true;

        // Tao JSON
        List<Dictionary<string, object>> dataList = new List<Dictionary<string, object>>();
        foreach (var order in orders)
        {
            dataList.Add(order.ToDictionary());
        }

        string json = JsonUtility.ToJson(new { data = dataList });
        Debug.Log($"Pushing data: {json.Substring(0, Mathf.Min(200, json.Length))}...");

        using (UnityWebRequest www = new UnityWebRequest(apiUrl, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Data pushed successfully!");
                onComplete?.Invoke(true);
            }
            else
            {
                Debug.LogError($"Error pushing data: {www.error}");
                onError?.Invoke($"Error pushing data: {www.error}");
                onComplete?.Invoke(false);
            }
        }

        isLoading = false;
    }

    public bool IsLoading => isLoading;
}

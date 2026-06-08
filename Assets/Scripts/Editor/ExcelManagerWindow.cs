using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class ExcelManagerWindow : EditorWindow
{
    private ExcelConfig excelConfig;
    private Vector2 gridScrollPosition;
    private OrderData[] currentData;
    private bool isLoading = false;
    private string statusMessage = "Ready";
    
    // UI State
    private List<List<string>> gridData = new List<List<string>>();
    private List<string> columnNames = new List<string>();
    private float cellWidth = 120f;
    private float cellHeight = 18f;

    [MenuItem("Tools/Excel Manager")]
    public static void ShowWindow()
    {
        GetWindow<ExcelManagerWindow>("Excel Manager");
    }

    private void OnGUI()
    {
        GUILayout.Label("Excel Manager Tool", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Config section
        GUILayout.Label("Configuration", EditorStyles.boldLabel);
        excelConfig = (ExcelConfig)EditorGUILayout.ObjectField("Excel Config", excelConfig, typeof(ExcelConfig), false);
        
        if (excelConfig == null)
        {
            EditorGUILayout.HelpBox("Please assign ExcelConfig asset", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space();

        // Button section
        GUILayout.Label("Actions", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        
        GUI.enabled = !isLoading;
        if (GUILayout.Button("Load Data", GUILayout.Height(30)))
        {
            LoadData();
        }

        if (GUILayout.Button("Reload", GUILayout.Height(30)))
        {
            gridData.Clear();
            columnNames.Clear();
            statusMessage = "Cleared";
            LoadData();
        }

        if (GUILayout.Button("Add Row", GUILayout.Height(30)))
        {
            AddNewRow();
        }

        if (GUILayout.Button("Push Data", GUILayout.Height(30)))
        {
            PushData();
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Status section
        EditorGUILayout.HelpBox(statusMessage, MessageType.Info);

        if (isLoading)
        {
            EditorGUILayout.LabelField("Loading...", EditorStyles.boldLabel);
        }

        EditorGUILayout.Space();

        // Grid section
        if (gridData.Count > 0)
        {
            GUILayout.Label($"Data ({gridData.Count - 1} rows, {columnNames.Count} columns)", EditorStyles.boldLabel);
            DrawGrid();
        }
    }

    private void DrawGrid()
    {
        EditorGUILayout.LabelField("Scroll to see more →", EditorStyles.miniLabel);
        
        gridScrollPosition = EditorGUILayout.BeginScrollView(gridScrollPosition, GUILayout.Height(300));
        
        // Draw each row
        for (int row = 0; row < gridData.Count; row++)
        {
            EditorGUILayout.BeginHorizontal();

            for (int col = 0; col < columnNames.Count; col++)
            {
                string value = col < gridData[row].Count ? gridData[row][col] : "";
                
                if (row == 0)
                {
                    // Header row
                    EditorGUILayout.LabelField(value, EditorStyles.boldLabel, GUILayout.Width(cellWidth));
                }
                else
                {
                    // Data row - editable
                    string newValue = EditorGUILayout.TextField(value, GUILayout.Width(cellWidth));
                    if (newValue != value)
                    {
                        gridData[row][col] = newValue;
                    }
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void LoadData()
    {
        isLoading = true;
        statusMessage = "Loading...";
        
        // Ensure ExcelAPIManager exists
        var apiManager = FindObjectOfType<ExcelAPIManager>();
        if (apiManager == null)
        {
            var go = new GameObject("ExcelAPIManager");
            apiManager = go.AddComponent<ExcelAPIManager>();
        }

        apiManager.Initialize(excelConfig.GetDeploymentUrl());
        apiManager.LoadData(
            (orders) =>
            {
                currentData = orders;
                PopulateGridFromOrders(orders);
                statusMessage = $"Loaded {orders.Length} orders successfully";
                isLoading = false;
                Repaint();
            },
            (error) =>
            {
                statusMessage = $"Error: {error}";
                isLoading = false;
                Repaint();
            }
        );
    }

    private void PopulateGridFromOrders(OrderData[] orders)
    {
        gridData.Clear();
        columnNames.Clear();

        // Get column names
        columnNames = new List<string>(OrderData.ColumnMapping.Keys);

        // Header row
        var headerRow = new List<string>();
        foreach (var colName in columnNames)
        {
            headerRow.Add(OrderData.GetDisplayName(colName));
        }
        gridData.Add(headerRow);

        // Data rows
        foreach (var order in orders)
        {
            var dataRow = new List<string>();
            foreach (var colName in columnNames)
            {
                var field = typeof(OrderData).GetField(colName);
                var value = field?.GetValue(order)?.ToString() ?? "";
                dataRow.Add(value);
            }
            gridData.Add(dataRow);
        }
    }

    private void AddNewRow()
    {
        if (gridData.Count < 1)
        {
            statusMessage = "Load data first";
            return;
        }

        var newRow = new List<string>();
        for (int i = 0; i < columnNames.Count; i++)
        {
            newRow.Add("");
        }
        gridData.Add(newRow);
        statusMessage = $"Added new row. Total: {gridData.Count - 1} rows";
    }

    private void PushData()
    {
        if (gridData.Count < 2)
        {
            statusMessage = "No data to push";
            return;
        }

        // Convert grid back to OrderData array
        var orders = new List<OrderData>();

        for (int row = 1; row < gridData.Count; row++)
        {
            var order = new OrderData();
            var dataRow = gridData[row];

            for (int col = 0; col < columnNames.Count && col < dataRow.Count; col++)
            {
                var colName = columnNames[col];
                var value = dataRow[col];
                var field = typeof(OrderData).GetField(colName);
                if (field != null)
                {
                    field.SetValue(order, value);
                }
            }

            // Only add rows with at least MaDonHang filled
            if (!string.IsNullOrEmpty(order.MaDonHang))
            {
                orders.Add(order);
            }
        }

        if (orders.Count == 0)
        {
            statusMessage = "No valid orders to push";
            return;
        }

        isLoading = true;
        statusMessage = "Pushing...";

        var apiManager = FindObjectOfType<ExcelAPIManager>();
        if (apiManager == null)
        {
            var go = new GameObject("ExcelAPIManager");
            apiManager = go.AddComponent<ExcelAPIManager>();
        }

        apiManager.Initialize(excelConfig.GetDeploymentUrl());
        apiManager.PushData(
            orders.ToArray(),
            (success) =>
            {
                if (success)
                {
                    statusMessage = $"Pushed {orders.Count} orders successfully!";
                    LoadData(); // Reload after push
                }
                else
                {
                    statusMessage = "Failed to push data";
                }
                isLoading = false;
                Repaint();
            },
            (error) =>
            {
                statusMessage = $"Error: {error}";
                isLoading = false;
                Repaint();
            }
        );
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }
}

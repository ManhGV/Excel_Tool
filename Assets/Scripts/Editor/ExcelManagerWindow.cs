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
    private List<string> visibleColumnNames = new List<string>();
    private float cellWidth = 120f;
    private float cellHeight = 18f;
    
    // Bulk fill state
    private int bulkFillColumnIndex = -1;
    private string bulkFillValue = "";
    private bool showBulkFillPanel = false;

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
            visibleColumnNames.Clear();
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

        if (GUILayout.Button("Format Address", GUILayout.Height(30)))
        {
            FormatAllAddresses();
        }

        if (GUILayout.Button("Bulk Fill", GUILayout.Height(30)))
        {
            showBulkFillPanel = !showBulkFillPanel;
            bulkFillColumnIndex = -1;
            bulkFillValue = "";
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
        
        // Bulk Fill Panel
        if (showBulkFillPanel)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Bulk Fill Column", EditorStyles.boldLabel);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Column selector
            if (visibleColumnNames.Count > 0)
            {
                string[] columnDisplayNames = new string[visibleColumnNames.Count];
                for (int i = 0; i < visibleColumnNames.Count; i++)
                {
                    columnDisplayNames[i] = OrderData.GetDisplayName(visibleColumnNames[i]);
                }
                
                bulkFillColumnIndex = EditorGUILayout.Popup("Select Column", bulkFillColumnIndex, columnDisplayNames);
                
                // Value input
                EditorGUILayout.LabelField("Value");
                bulkFillValue = EditorGUILayout.TextArea(bulkFillValue, GUILayout.Height(60));
                
                // Apply button
                EditorGUILayout.Space();
                if (GUILayout.Button("Apply to All Rows", GUILayout.Height(30)))
                {
                    if (bulkFillColumnIndex >= 0 && !string.IsNullOrEmpty(bulkFillValue))
                    {
                        ApplyBulkFill();
                        showBulkFillPanel = false;
                    }
                    else
                    {
                        statusMessage = "Please select a column and enter a value";
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Load data first", MessageType.Warning);
            }
            
            EditorGUILayout.EndVertical();
        }

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
            GUILayout.Label($"Data ({gridData.Count - 1} rows, {visibleColumnNames.Count} columns)", EditorStyles.boldLabel);
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

            for (int col = 0; col < visibleColumnNames.Count; col++)
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
        // Clear old data first
        gridData.Clear();
        visibleColumnNames.Clear();
        
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
        visibleColumnNames.Clear();

        // Header row
        var headerRow = new List<string>();
        foreach (var kvp in OrderData.ColumnMapping)
        {
            string fieldName = kvp.Key;
            string displayName = kvp.Value;
            
            // Kiểm tra visibility từ ExcelConfig
            if (excelConfig.columnVisibility.IsVisible(fieldName))
            {
                visibleColumnNames.Add(fieldName);
                headerRow.Add(displayName);
            }
        }
        gridData.Add(headerRow);

        // Data rows
        foreach (var order in orders)
        {
            var dataRow = new List<string>();
            foreach (var fieldName in visibleColumnNames)
            {
                var field = typeof(OrderData).GetField(fieldName);
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
        for (int i = 0; i < visibleColumnNames.Count; i++)
        {
            newRow.Add("");
        }
        gridData.Add(newRow);
        statusMessage = $"Added new row. Total: {gridData.Count - 1} rows";
    }

    private void PushData()
    {
        // Auto-load if not loaded yet
        if (visibleColumnNames.Count == 0 || gridData.Count < 1)
        {
            statusMessage = "Loading data first...";
            LoadData();
            return;
        }
        
        if (gridData.Count < 2)
        {
            statusMessage = "No data to push";
            return;
        }

        // Convert grid back to OrderData array
        var orders = new List<OrderData>();

        Debug.Log($"Grid data count: {gridData.Count}, Visible columns: {visibleColumnNames.Count}");
        
        // Debug: print column names
        string colNames = "Columns: ";
        foreach (var col in visibleColumnNames)
        {
            colNames += col + ", ";
        }
        Debug.Log(colNames);

        for (int row = 1; row < gridData.Count; row++)
        {
            var order = new OrderData();
            var dataRow = gridData[row];
            
            // Debug: print row data
            string rowData = $"Row {row}: ";
            foreach (var cell in dataRow)
            {
                rowData += $"[{cell}] ";
            }
            Debug.Log(rowData);

            for (int col = 0; col < visibleColumnNames.Count && col < dataRow.Count; col++)
            {
                var fieldName = visibleColumnNames[col];
                var value = dataRow[col];
                var field = typeof(OrderData).GetField(fieldName);
                if (field != null)
                {
                    field.SetValue(order, value);
                }
            }

            // Add row if it has ANY data filled (not completely empty)
            Debug.Log($"Row {row} - MaDonHang: '{order.MaDonHang}'");
            bool hasAnyData = false;
            foreach (var col in visibleColumnNames)
            {
                var field = typeof(OrderData).GetField(col);
                if (field != null)
                {
                    var value = field.GetValue(order)?.ToString();
                    if (!string.IsNullOrEmpty(value))
                    {
                        hasAnyData = true;
                        break;
                    }
                }
            }
            
            if (hasAnyData)
            {
                orders.Add(order);
            }
        }

        if (orders.Count == 0)
        {
            statusMessage = "No valid orders to push (no MaDonHang found)";
            Debug.LogError("PushData error: No valid orders. Check MaDonHang field!");
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

    private void FormatAllAddresses()
    {
        if (gridData.Count < 2)
        {
            statusMessage = "Load data first";
            return;
        }

        // Find the index of DiaChiChiTiet column
        int diaChiChiTietIndex = visibleColumnNames.IndexOf("DiaChiChiTiet");

        if (diaChiChiTietIndex < 0)
        {
            statusMessage = "Column 'Địa chỉ chi tiết' not found in visible columns";
            return;
        }

        int formattedCount = 0;
        // Process all data rows (skip header at index 0)
        for (int i = 1; i < gridData.Count; i++)
        {
            var row = gridData[i];
            if (diaChiChiTietIndex < row.Count)
            {
                string originalAddress = row[diaChiChiTietIndex];
                if (!string.IsNullOrWhiteSpace(originalAddress))
                {
                    string formattedAddress = AddressFormatter.FormatAddress(originalAddress);
                    gridData[i][diaChiChiTietIndex] = formattedAddress;
                    formattedCount++;
                }
            }
        }

        statusMessage = $"Formatted {formattedCount} addresses successfully!";
        Repaint();
    }

    private void ApplyBulkFill()
    {
        if (gridData.Count < 2)
        {
            statusMessage = "Load data first";
            return;
        }

        if (bulkFillColumnIndex < 0 || bulkFillColumnIndex >= visibleColumnNames.Count)
        {
            statusMessage = "Invalid column selected";
            return;
        }

        int fillCount = 0;
        // Apply value to all data rows (skip header at index 0)
        for (int i = 1; i < gridData.Count; i++)
        {
            var row = gridData[i];
            if (bulkFillColumnIndex < row.Count)
            {
                gridData[i][bulkFillColumnIndex] = bulkFillValue;
                fillCount++;
            }
        }

        statusMessage = $"Applied '{bulkFillValue}' to {fillCount} rows in column '{OrderData.GetDisplayName(visibleColumnNames[bulkFillColumnIndex])}'";
        Repaint();
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }
}

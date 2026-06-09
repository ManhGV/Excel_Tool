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
    
    // Group by recipient state
    private bool showGroupByRecipient = false;
    private Vector2 groupScrollPosition;
    private Dictionary<string, List<int>> recipientGroups = new Dictionary<string, List<int>>();
    private Dictionary<string, bool> expandedGroups = new Dictionary<string, bool>();

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

        if (GUILayout.Button("Sort Phone", GUILayout.Height(30)))
        {
            SortRowsWithoutPhoneToBottom();
        }

        if (recipientGroups.Count > 0 && GUILayout.Button("Auto Fill Phone & Address", GUILayout.Height(30)))
        {
            AutoFillPhoneAndAddress();
        }

        if (GUILayout.Button("Group by Recipient", GUILayout.Height(30)))
        {
            GroupByRecipient();
            showGroupByRecipient = !showGroupByRecipient;
        }

        if (recipientGroups.Count > 0 && GUILayout.Button("Auto Generate Order Code", GUILayout.Height(30)))
        {
            AutoGenerateOrderCodes();
        }

        if (GUILayout.Button("Sort by Order Code", GUILayout.Height(30)))
        {
            SortByOrderCode();
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

        // Group by recipient section
        DrawGroupByRecipient();
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

    private void DrawGroupByRecipient()
    {
        if (!showGroupByRecipient || recipientGroups.Count == 0)
            return;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Grouped by Recipient Name", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox($"Total groups: {recipientGroups.Count}", MessageType.Info);

        groupScrollPosition = EditorGUILayout.BeginScrollView(groupScrollPosition, GUILayout.Height(200));

        var sortedKeys = new List<string>(recipientGroups.Keys);
        sortedKeys.Sort();

        foreach (var recipientName in sortedKeys)
        {
            var rowIndices = recipientGroups[recipientName];
            bool isExpanded = expandedGroups.ContainsKey(recipientName) && expandedGroups[recipientName];

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button(isExpanded ? "▼" : "▶", GUILayout.Width(20)))
            {
                if (!expandedGroups.ContainsKey(recipientName))
                    expandedGroups[recipientName] = false;
                expandedGroups[recipientName] = !expandedGroups[recipientName];
            }

            EditorGUILayout.LabelField($"{recipientName} ({rowIndices.Count})", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            if (isExpanded)
            {
                EditorGUI.indentLevel++;
                foreach (var rowIndex in rowIndices)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField($"Row {rowIndex}", GUILayout.Width(60));
                    
                    // Display some key info
                    if (rowIndex < gridData.Count)
                    {
                        var row = gridData[rowIndex];
                        int maDonHangIdx = visibleColumnNames.IndexOf("MaDonHang");
                        int soDienThoaiIdx = visibleColumnNames.IndexOf("SoDienThoai");
                        
                        string maDonHang = maDonHangIdx >= 0 && maDonHangIdx < row.Count ? row[maDonHangIdx] : "";
                        string soDienThoai = soDienThoaiIdx >= 0 && soDienThoaiIdx < row.Count ? row[soDienThoaiIdx] : "";
                        
                        EditorGUILayout.LabelField($"ID: {maDonHang} | Phone: {soDienThoai}", GUILayout.ExpandWidth(true));
                    }
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUI.indentLevel--;
            }
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

    private void SortRowsWithoutPhoneToBottom()
    {
        if (gridData.Count < 2)
        {
            statusMessage = "Load data first";
            return;
        }

        int phoneColumnIndex = visibleColumnNames.IndexOf("SoDienThoai");
        int addressColumnIndex = visibleColumnNames.IndexOf("DiaChiChiTiet");
        
        if (phoneColumnIndex < 0)
        {
            statusMessage = "Column 'Số điện thoại' not found in visible columns";
            return;
        }
        
        if (addressColumnIndex < 0)
        {
            statusMessage = "Column 'Địa chỉ chi tiết' not found in visible columns";
            return;
        }

        // Category 1: Has both phone and address
        var rowsWithBoth = new List<List<string>>();
        // Category 2: Has address but no phone
        var rowsWithAddressOnly = new List<List<string>>();
        // Category 3: No address (regardless of phone)
        var rowsWithoutAddress = new List<List<string>>();

        for (int i = 1; i < gridData.Count; i++)
        {
            var row = gridData[i];
            string phone = phoneColumnIndex < row.Count ? row[phoneColumnIndex] : "";
            string address = addressColumnIndex < row.Count ? row[addressColumnIndex] : "";

            bool hasPhone = !string.IsNullOrWhiteSpace(phone);
            bool hasAddress = !string.IsNullOrWhiteSpace(address);

            if (!hasAddress)
            {
                // No address -> bottom
                rowsWithoutAddress.Add(row);
            }
            else if (hasAddress && !hasPhone)
            {
                // Has address but no phone -> middle
                rowsWithAddressOnly.Add(row);
            }
            else if (hasAddress && hasPhone)
            {
                // Has both -> top
                rowsWithBoth.Add(row);
            }
        }

        var headerRow = gridData[0];
        gridData.Clear();
        gridData.Add(headerRow);
        gridData.AddRange(rowsWithBoth);
        gridData.AddRange(rowsWithAddressOnly);
        gridData.AddRange(rowsWithoutAddress);

        statusMessage = $"Sorted: {rowsWithBoth.Count} with both | {rowsWithAddressOnly.Count} address only | {rowsWithoutAddress.Count} no address";
        Repaint();
    }

    private void GroupByRecipient()
    {
        if (gridData.Count < 2)
        {
            statusMessage = "Load data first";
            return;
        }

        int recipientColumnIndex = visibleColumnNames.IndexOf("TenNguoiNhan");
        if (recipientColumnIndex < 0)
        {
            statusMessage = "Column 'Tên người nhận' not found in visible columns";
            return;
        }

        recipientGroups.Clear();
        expandedGroups.Clear();

        for (int i = 1; i < gridData.Count; i++)
        {
            var row = gridData[i];
            string recipientName = recipientColumnIndex < row.Count ? row[recipientColumnIndex] : "";

            if (string.IsNullOrWhiteSpace(recipientName))
                recipientName = "[Empty]"; // Group empty names together

            if (!recipientGroups.ContainsKey(recipientName))
                recipientGroups[recipientName] = new List<int>();

            recipientGroups[recipientName].Add(i);
        }

        statusMessage = $"Grouped {gridData.Count - 1} rows into {recipientGroups.Count} recipient groups";
        Repaint();
    }

    private void AutoGenerateOrderCodes()
    {
        if (recipientGroups.Count == 0)
        {
            statusMessage = "Group by Recipient first";
            return;
        }

        int maDonHangColumnIndex = visibleColumnNames.IndexOf("MaDonHang");
        if (maDonHangColumnIndex < 0)
        {
            statusMessage = "Column 'Mã đơn hàng' not found in visible columns";
            return;
        }

        // Create a sorted list of recipient names and assign index+1 as order code
        var sortedRecipients = new List<string>(recipientGroups.Keys);
        sortedRecipients.Sort();

        int codeCount = 0;
        for (int groupIndex = 0; groupIndex < sortedRecipients.Count; groupIndex++)
        {
            string recipientName = sortedRecipients[groupIndex];
            string orderCode = (groupIndex + 1).ToString();
            var rowIndices = recipientGroups[recipientName];

            foreach (var rowIndex in rowIndices)
            {
                if (rowIndex < gridData.Count && maDonHangColumnIndex < gridData[rowIndex].Count)
                {
                    gridData[rowIndex][maDonHangColumnIndex] = orderCode;
                    codeCount++;
                }
            }
        }

        statusMessage = $"Auto-generated {codeCount} order codes based on {sortedRecipients.Count} recipient groups";
        Repaint();
    }

    private void SortByOrderCode()
    {
        if (gridData.Count < 2)
        {
            statusMessage = "Load data first";
            return;
        }

        int maDonHangColumnIndex = visibleColumnNames.IndexOf("MaDonHang");
        if (maDonHangColumnIndex < 0)
        {
            statusMessage = "Column 'Mã đơn hàng' not found in visible columns";
            return;
        }

        // Get header row
        var headerRow = gridData[0];
        
        // Get data rows (skip header)
        var dataRows = new List<List<string>>();
        for (int i = 1; i < gridData.Count; i++)
        {
            dataRows.Add(gridData[i]);
        }

        // Sort data rows by order code (MaDonHang)
        dataRows.Sort((a, b) =>
        {
            string codeA = maDonHangColumnIndex < a.Count ? a[maDonHangColumnIndex] : "";
            string codeB = maDonHangColumnIndex < b.Count ? b[maDonHangColumnIndex] : "";

            // Try to parse as numbers for numeric sorting
            if (int.TryParse(codeA, out int numA) && int.TryParse(codeB, out int numB))
            {
                return numA.CompareTo(numB);
            }

            // Fall back to string comparison
            return codeA.CompareTo(codeB);
        });

        // Rebuild grid
        gridData.Clear();
        gridData.Add(headerRow);
        gridData.AddRange(dataRows);

        statusMessage = $"Sorted {dataRows.Count} rows by order code";
        Repaint();
    }

    private void AutoFillPhoneAndAddress()
    {
        if (recipientGroups.Count == 0)
        {
            statusMessage = "Group by Recipient first";
            return;
        }

        int phoneColumnIndex = visibleColumnNames.IndexOf("SoDienThoai");
        int addressColumnIndex = visibleColumnNames.IndexOf("DiaChiChiTiet");

        if (phoneColumnIndex < 0 || addressColumnIndex < 0)
        {
            statusMessage = "Columns 'Số điện thoại' or 'Địa chỉ chi tiết' not found";
            return;
        }

        int filledCount = 0;
        var conflicts = new List<string>();

        foreach (var groupEntry in recipientGroups)
        {
            string recipientName = groupEntry.Key;
            var rowIndices = groupEntry.Value;

            // Find non-empty phone and address in this group
            string groupPhone = "";
            string groupAddress = "";
            var phoneSources = new HashSet<string>();
            var addressSources = new HashSet<string>();

            foreach (var rowIndex in rowIndices)
            {
                if (rowIndex < gridData.Count)
                {
                    var row = gridData[rowIndex];
                    string phone = phoneColumnIndex < row.Count ? row[phoneColumnIndex] : "";
                    string address = addressColumnIndex < row.Count ? row[addressColumnIndex] : "";

                    if (!string.IsNullOrWhiteSpace(phone))
                    {
                        if (string.IsNullOrWhiteSpace(groupPhone))
                            groupPhone = phone;
                        phoneSources.Add(phone);
                    }

                    if (!string.IsNullOrWhiteSpace(address))
                    {
                        if (string.IsNullOrWhiteSpace(groupAddress))
                            groupAddress = address;
                        addressSources.Add(address);
                    }
                }
            }

            // Check for conflicts
            if (phoneSources.Count > 1)
            {
                conflicts.Add($"[{recipientName}] Multiple phone numbers: {string.Join(", ", phoneSources)}");
            }

            if (addressSources.Count > 1)
            {
                conflicts.Add($"[{recipientName}] Multiple addresses: {string.Join(", ", addressSources)}");
            }

            // Fill missing values in group
            if (!string.IsNullOrWhiteSpace(groupPhone) || !string.IsNullOrWhiteSpace(groupAddress))
            {
                foreach (var rowIndex in rowIndices)
                {
                    if (rowIndex < gridData.Count)
                    {
                        var row = gridData[rowIndex];
                        string currentPhone = phoneColumnIndex < row.Count ? row[phoneColumnIndex] : "";
                        string currentAddress = addressColumnIndex < row.Count ? row[addressColumnIndex] : "";

                        // Fill phone if empty
                        if (string.IsNullOrWhiteSpace(currentPhone) && !string.IsNullOrWhiteSpace(groupPhone))
                        {
                            gridData[rowIndex][phoneColumnIndex] = groupPhone;
                            filledCount++;
                        }

                        // Fill address if empty
                        if (string.IsNullOrWhiteSpace(currentAddress) && !string.IsNullOrWhiteSpace(groupAddress))
                        {
                            gridData[rowIndex][addressColumnIndex] = groupAddress;
                            filledCount++;
                        }
                    }
                }
            }
        }

        if (conflicts.Count > 0)
        {
            statusMessage = $"Filled {filledCount} fields. *** WARNING *** {conflicts.Count} conflicts found - check console";
            Debug.LogWarning($"Auto-fill conflicts detected:\n{string.Join("\n", conflicts)}");
        }
        else
        {
            statusMessage = $"Auto-filled {filledCount} phone and address fields in {recipientGroups.Count} groups";
        }

        Repaint();
    }

    private void OnInspectorUpdate()
    {
        Repaint();
    }
}

using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class ExcelUIManager : MonoBehaviour
{
    [SerializeField] private ExcelConfig excelConfig;
    [SerializeField] private Transform gridContent; // Parent cho cells
    [SerializeField] private GameObject cellPrefab; // Prefab cho 1 cell
    [SerializeField] private Button loadButton;
    [SerializeField] private Button pushButton;
    [SerializeField] private Button addRowButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private VerticalLayoutGroup gridLayout;

    private OrderData[] currentData;
    private List<List<InputField>> gridInputs = new List<List<InputField>>();
    private int columnCount = 0;

    private void Start()
    {
        if (excelConfig == null)
        {
            SetStatus("ERROR: ExcelConfig not assigned!");
            return;
        }

        loadButton.onClick.AddListener(LoadDataFromSheet);
        pushButton.onClick.AddListener(PushDataToSheet);
        addRowButton.onClick.AddListener(AddNewRow);

        // Initialize API
        if (ExcelAPIManager.Instance != null)
        {
            ExcelAPIManager.Instance.Initialize(excelConfig.GetDeploymentUrl());
            SetStatus("Ready. Click Load Data to start.");
        }
    }

    public void LoadDataFromSheet()
    {
        SetStatus("Loading data...");
        loadButton.interactable = false;

        ExcelAPIManager.Instance.LoadData(
            (orders) =>
            {
                currentData = orders;
                DisplayData(orders);
                SetStatus($"Loaded {orders.Length} orders");
                loadButton.interactable = true;
            },
            (error) =>
            {
                SetStatus($"Error: {error}");
                loadButton.interactable = true;
            }
        );
    }

    private void DisplayData(OrderData[] orders)
    {
        if (orders == null)
        {
            SetStatus("No orders data");
            return;
        }

        // Clear old grid
        if (gridContent != null)
        {
            foreach (Transform child in gridContent)
            {
                Destroy(child.gameObject);
            }
        }
        gridInputs.Clear();

        if (orders.Length == 0)
        {
            SetStatus("No data to display");
            return;
        }

        // Get column names from mapping
        var columnNames = new List<string>(OrderData.ColumnMapping.Keys);
        columnCount = columnNames.Count;

        try
        {
            // Create header row
            CreateHeaderRow(columnNames);

            // Create data rows
            for (int i = 0; i < orders.Length; i++)
            {
                CreateDataRow(orders[i], columnNames);
            }
            SetStatus($"Loaded {orders.Length} orders");
        }
        catch (System.Exception e)
        {
            SetStatus($"Error displaying data: {e.Message}");
            Debug.LogError($"DisplayData error: {e.StackTrace}");
        }
    }

    private void CreateHeaderRow(List<string> columnNames)
    {
        var headerRow = new List<InputField>();

        foreach (var colName in columnNames)
        {
            var displayName = OrderData.GetDisplayName(colName);
            CreateCell(displayName, true, headerRow);
        }

        gridInputs.Add(headerRow);
    }

    private void CreateDataRow(OrderData order, List<string> columnNames)
    {
        var dataRow = new List<InputField>();

        foreach (var colName in columnNames)
        {
            var property = typeof(OrderData).GetProperty(colName);
            var value = property?.GetValue(order)?.ToString() ?? "";

            var inputField = CreateCell(value, false, dataRow);
        }

        gridInputs.Add(dataRow);
    }

    private InputField CreateCell(string value, bool isHeader, List<InputField> row)
    {
        if (cellPrefab == null)
        {
            Debug.LogError("cellPrefab is not assigned in Inspector!");
            return null;
        }

        var cellObj = Instantiate(cellPrefab, gridContent);
        var inputField = cellObj.GetComponent<InputField>();

        if (inputField == null)
        {
            Debug.LogError("cellPrefab does not have InputField component!");
            Destroy(cellObj);
            return null;
        }

        if (isHeader)
        {
            inputField.text = value;
            inputField.interactable = false;
            var img = cellObj.GetComponent<Image>();
            if (img != null)
                img.color = new Color(0.8f, 0.8f, 0.8f); // Header color
        }
        else
        {
            inputField.text = value;
            inputField.interactable = true;
        }

        if (inputField != null)
            row.Add(inputField);
        
        return inputField;
    }

    private void AddNewRow()
    {
        if (gridInputs.Count < 2)
        {
            SetStatus("Load data first");
            return;
        }

        var newRow = new List<InputField>();
        var columnNames = new List<string>(OrderData.ColumnMapping.Keys);

        foreach (var colName in columnNames)
        {
            CreateCell("", false, newRow);
        }

        gridInputs.Add(newRow);
        SetStatus($"Added new row. Total rows: {gridInputs.Count - 1}");
    }

    private void PushDataToSheet()
    {
        if (gridInputs.Count < 2)
        {
            SetStatus("No data to push");
            return;
        }

        // Convert grid data back to OrderData array
        var orders = new List<OrderData>();
        var columnNames = new List<string>(OrderData.ColumnMapping.Keys);

        for (int i = 1; i < gridInputs.Count; i++) // Skip header row
        {
            var order = new OrderData();
            var row = gridInputs[i];

            for (int j = 0; j < columnNames.Count && j < row.Count; j++)
            {
                var colName = columnNames[j];
                var value = row[j].text;
                var property = typeof(OrderData).GetProperty(colName);
                if (property != null)
                {
                    property.SetValue(order, value);
                }
            }

            // Only add rows that have at least MaDonHang filled
            if (!string.IsNullOrEmpty(order.MaDonHang))
            {
                orders.Add(order);
            }
        }

        if (orders.Count == 0)
        {
            SetStatus("No valid orders to push");
            return;
        }

        SetStatus("Pushing data...");
        pushButton.interactable = false;

        ExcelAPIManager.Instance.PushData(
            orders.ToArray(),
            (success) =>
            {
                if (success)
                {
                    SetStatus($"Pushed {orders.Count} orders successfully!");
                }
                else
                {
                    SetStatus("Failed to push data");
                }
                pushButton.interactable = true;
            },
            (error) =>
            {
                SetStatus($"Error pushing data: {error}");
                pushButton.interactable = true;
            }
        );
    }

    private void SetStatus(string message)
    {
        statusText.text = message;
        Debug.Log($"[ExcelUI] {message}");
    }
}

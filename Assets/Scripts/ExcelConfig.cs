using UnityEngine;

[CreateAssetMenu(fileName = "ExcelConfig", menuName = "Excel/Config")]
public class ExcelConfig : ScriptableObject
{
    [SerializeField]
    [TextArea(2, 4)]
    public string deploymentId = "AKfycbxITObh8XDgNJ_i_Pucqu9JcNs1_gWD7rcFAD2ZjGfQqCFo5sgbwDF99XfkzV5nAAUKFw";

    [SerializeField]
    [TextArea(2, 4)]
    public string deploymentUrl = "https://script.google.com/macros/s/AKfycbxITObh8XDgNJ_i_Pucqu9JcNs1_gWD7rcFAD2ZjGfQqCFo5sgbwDF99XfkzV5nAAUKFw/exec";

    [SerializeField]
    public string spreadsheetId = "1MdS28yqxIcqjSvjijYN-4UoLv23fMkQd";

    [SerializeField]
    [Tooltip("Tích/bỏ tích để hiển thị/ẩn từng cột trong tool")]
    public OrderDataVisibility columnVisibility = new OrderDataVisibility();

    public string GetDeploymentUrl()
    {
        return deploymentUrl;
    }

    public string GetDeploymentId()
    {
        return deploymentId;
    }

    public string GetSpreadsheetId()
    {
        return spreadsheetId;
    }
}

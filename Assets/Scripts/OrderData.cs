using System.Collections.Generic;

[System.Serializable]
public class OrderData
{
    public string MaDonHang;
    public string TenNguoiNhan;
    public string SoDienThoai;
    public string TinhThanhPho;
    public string QuanHuyen;
    public string XaPhuong;
    public string DiaChiChiTiet;
    public string LuuYVeDiaChi;
    public string MaBuuChinh;
    public string TenSanPham;
    public string SoLuong;
    public string GiaTien;
    public string TongCanNang;
    public string ChieuDai;
    public string ChieuRong;
    public string ChieuCao;
    public string MaKhachHang;
    public string GiaTriDonHang;
    public string GiaoHangMotPhan;
    public string ChoPhepThuHang;
    public string ChoXemHangKhongChoThu;
    public string ThuPhiTuChoiNhanHang;
    public string PhiTuChoiNhanHangCanThu;
    public string ThuCOD;
    public string SoTienCOD;
    public string BuuGuiGiaTriCao;
    public string HinhThucThanhToan;
    public string LuuYGiaoHang;
    public string NhacNhoDienDungSoTienCOD;
    public string DonChiHoanThanhNeuODuoiHien;

    // Mapping dictionary from code name to display name (Vietnamese with diacritics)
    public static Dictionary<string, string> ColumnMapping = new Dictionary<string, string>
    {
        { "MaDonHang", "*Mã đơn hàng" },
        { "TenNguoiNhan", "*Tên người nhận" },
        { "SoDienThoai", "*Số điện thoại" },
        { "TinhThanhPho", "*Tỉnh/Thành Phố" },
        { "QuanHuyen", "*Quận/Huyện" },
        { "XaPhuong", "*Xã/Phường" },
        { "DiaChiChiTiet", "*Địa chỉ chi tiết" },
        { "LuuYVeDiaChi", "Lưu ý về địa chỉ" },
        { "MaBuuChinh", "Mã bưu chính" },
        { "TenSanPham", "*Tên sản phẩm" },
        { "SoLuong", "Số lượng (Thông tin bắt buộc khi chọn Giao hàng một phần & Thu COD)" },
        { "GiaTien", "Giá tiền (Thông tin bắt buộc khi chọn Giao hàng một phần & Thu COD)" },
        { "TongCanNang", "*Tổng cân nặng bưu gửi (KG)" },
        { "ChieuDai", "Chiều dài (CM)" },
        { "ChieuRong", "Chiều rộng (CM)" },
        { "ChieuCao", "Chiều cao (CM)" },
        { "MaKhachHang", "Mã khách hàng" },
        { "GiaTriDonHang", "*Giá trị đơn hàng" },
        { "GiaoHangMotPhan", "*Giao hàng một phần (Y/N)" },
        { "ChoPhepThuHang", "*Cho phép thử hàng (Y/N)" },
        { "ChoXemHangKhongChoThu", "*Cho xem hàng, không cho thử (Y/N)" },
        { "ThuPhiTuChoiNhanHang", "Thu phí từ chối nhận hàng (Y/N)" },
        { "PhiTuChoiNhanHangCanThu", "Phí từ chối nhận hàng cần thu" },
        { "ThuCOD", "*Thu COD (Y/N)" },
        { "SoTienCOD", "Số tiền COD" },
        { "BuuGuiGiaTriCao", "bưu gửi giá trị cao (Y/N)" },
        { "HinhThucThanhToan", "*Hình thức thanh Toán" },
        { "LuuYGiaoHang", "Lưu ý giao hàng" },
        { "NhacNhoDienDungSoTienCOD", "Nhắc nhở điền đúng số tiền COD" },
        { "DonChiHoanThanhNeuODuoiHien", "Đơn chỉ hoàn thành nếu ở dưới hiện \"Đủ điều kiện\"" }
    };

    // Get display name from code name
    public static string GetDisplayName(string codeName)
    {
        return ColumnMapping.ContainsKey(codeName) ? ColumnMapping[codeName] : codeName;
    }

    // Get code name from display name (reverse mapping)
    public static string GetCodeName(string displayName)
    {
        foreach (var kvp in ColumnMapping)
        {
            if (kvp.Value == displayName)
                return kvp.Key;
        }
        return displayName;
    }

    // Convert from API response (with Vietnamese headers) to OrderData
    public static OrderData FromDictionary(Dictionary<string, object> dict)
    {
        var order = new OrderData();
        
        // Try to match by display name (Vietnamese with diacritics) from API
        foreach (var kvp in ColumnMapping)
        {
            string codeName = kvp.Key;
            string displayName = kvp.Value;
            
            object value = null;
            
            // Try exact match first
            if (dict.ContainsKey(displayName))
            {
                value = dict[displayName];
            }
            else
            {
                // Try to find by fuzzy match (normalize both sides)
                string normalizedDisplay = NormalizeString(displayName);
                foreach (var dictKey in dict.Keys)
                {
                    string normalizedDictKey = NormalizeString(dictKey.ToString());
                    if (normalizedDictKey == normalizedDisplay)
                    {
                        value = dict[dictKey];
                        break;
                    }
                }
            }
            
            // Set property if value found
            if (value != null)
            {
                var field = typeof(OrderData).GetField(codeName);
                if (field != null)
                {
                    field.SetValue(order, value.ToString());
                }
            }
        }
        
        return order;
    }
    
    private static string NormalizeString(string str)
    {
        // Remove accents and special characters for comparison
        string normalized = str.ToLower();
        // Remove common punctuation
        normalized = normalized.Replace("*", "").Replace(" ", "").Replace("(", "").Replace(")", "")
            .Replace("&", "").Replace("/", "").Replace(",", "").Replace(".", "");
        return normalized;
    }

    // Convert OrderData to dictionary with Vietnamese headers (for API push)
    public Dictionary<string, object> ToDictionary()
    {
        var dict = new Dictionary<string, object>();
        
        foreach (var kvp in ColumnMapping)
        {
            string codeName = kvp.Key;
            string displayName = kvp.Value;
            
            var field = typeof(OrderData).GetField(codeName);
            if (field != null)
            {
                object value = field.GetValue(this) ?? "";
                dict[displayName] = value;
            }
        }
        
        return dict;
    }
}

[System.Serializable]
public class OrderDataWrapper
{
    public OrderData[] data;
}

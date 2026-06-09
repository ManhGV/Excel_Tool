using UnityEngine;

/// <summary>
/// Lưu trạng thái hiển thị/ẩn của từng cột trong OrderData
/// Mỗi field bool tương ứng với một cột trong OrderData
/// </summary>
[System.Serializable]
public class OrderDataVisibility
{
    [Tooltip("*Mã đơn hàng")]
    public bool MaDonHang = true;
    
    [Tooltip("*Tên người nhận")]
    public bool TenNguoiNhan = true;
    
    [Tooltip("*Số điện thoại")]
    public bool SoDienThoai = true;
    
    [Tooltip("*Tỉnh/Thành Phố")]
    public bool TinhThanhPho = true;
    
    [Tooltip("*Quận/Huyện")]
    public bool QuanHuyen = true;
    
    [Tooltip("*Xã/Phường")]
    public bool XaPhuong = true;
    
    [Tooltip("*Địa chỉ chi tiết")]
    public bool DiaChiChiTiet = true;
    
    [Tooltip("Lưu ý về địa chỉ")]
    public bool LuuYVeDiaChi = false;
    
    [Tooltip("Mã bưu chính")]
    public bool MaBuuChinh = false;
    
    [Tooltip("*Tên sản phẩm")]
    public bool TenSanPham = true;
    
    [Tooltip("Số lượng (Thông tin bắt buộc khi chọn Giao hàng một phần & Thu COD)")]
    public bool SoLuong = true;
    
    [Tooltip("Giá tiền (Thông tin bắt buộc khi chọn Giao hàng một phần & Thu COD)")]
    public bool GiaTien = true;
    
    [Tooltip("*Tổng cân nặng bưu gửi (KG)")]
    public bool TongCanNang = true;
    
    [Tooltip("Chiều dài (CM)")]
    public bool ChieuDai = false;
    
    [Tooltip("Chiều rộng (CM)")]
    public bool ChieuRong = false;
    
    [Tooltip("Chiều cao (CM)")]
    public bool ChieuCao = false;
    
    [Tooltip("Mã khách hàng")]
    public bool MaKhachHang = false;
    
    [Tooltip("*Giá trị đơn hàng")]
    public bool GiaTriDonHang = true;
    
    [Tooltip("*Giao hàng một phần (Y/N)")]
    public bool GiaoHangMotPhan = true;
    
    [Tooltip("*Cho phép thử hàng (Y/N)")]
    public bool ChoPhepThuHang = true;
    
    [Tooltip("*Cho xem hàng, không cho thử (Y/N)")]
    public bool ChoXemHangKhongChoThu = true;
    
    [Tooltip("Thu phí từ chối nhận hàng (Y/N)")]
    public bool ThuPhiTuChoiNhanHang = false;
    
    [Tooltip("Phí từ chối nhận hàng cần thu")]
    public bool PhiTuChoiNhanHangCanThu = false;
    
    [Tooltip("*Thu COD (Y/N)")]
    public bool ThuCOD = true;
    
    [Tooltip("Số tiền COD")]
    public bool SoTienCOD = true;
    
    [Tooltip("bưu gửi giá trị cao (Y/N)")]
    public bool BuuGuiGiaTriCao = false;
    
    [Tooltip("*Hình thức thanh Toán")]
    public bool HinhThucThanhToan = true;
    
    [Tooltip("Lưu ý giao hàng")]
    public bool LuuYGiaoHang = false;
    
    [Tooltip("Nhắc nhở điền đúng số tiền COD")]
    public bool NhacNhoDienDungSoTienCOD = false;
    
    [Tooltip("Đơn chỉ hoàn thành nếu ở dưới hiện \"Đủ điều kiện\"")]
    public bool DonChiHoanThanhNeuODuoiHien = false;

    /// <summary>
    /// Lấy giá trị visibility theo field name
    /// </summary>
    public bool IsVisible(string fieldName)
    {
        var field = typeof(OrderDataVisibility).GetField(fieldName);
        if (field != null)
        {
            return (bool)field.GetValue(this);
        }
        return true; // Default: hiển thị nếu không tìm thấy
    }

    /// <summary>
    /// Set visibility theo field name
    /// </summary>
    public void SetVisible(string fieldName, bool visible)
    {
        var field = typeof(OrderDataVisibility).GetField(fieldName);
        if (field != null)
        {
            field.SetValue(this, visible);
        }
    }

    /// <summary>
    /// Hiển thị tất cả cột
    /// </summary>
    public void ShowAll()
    {
        var fields = typeof(OrderDataVisibility).GetFields();
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(bool))
            {
                field.SetValue(this, true);
            }
        }
    }

    /// <summary>
    /// Ẩn tất cả cột
    /// </summary>
    public void HideAll()
    {
        var fields = typeof(OrderDataVisibility).GetFields();
        foreach (var field in fields)
        {
            if (field.FieldType == typeof(bool))
            {
                field.SetValue(this, false);
            }
        }
    }
}

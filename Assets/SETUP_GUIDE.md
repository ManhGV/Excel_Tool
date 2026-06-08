# Excel Sheet Manager - Setup Guide

## Bước 1: Import TextMesh Pro
Nếu chưa có TextMesh Pro, Unity sẽ hỏi khi bạn tạo scene. Click "Import TMP Essentials".

## Bước 2: Tạo Cell Prefab

1. Tạo **Canvas** mới nếu chưa có:
   - Right-click Hierarchy → UI → Canvas

2. Tạo **Cell Prefab**:
   - Tạo **Panel** mới trong Canvas → đặt tên "CellPrefab"
   - Thêm **InputField - TextMeshPro** vào trong Panel:
     - Right-click CellPrefab → UI → Input Field - TextMeshPro
   - **Settings cho CellPrefab:**
     - Width: 200, Height: 40
     - Layout: LayoutElement
     - Color: white
     - Border: gray (optional)
   - **Drag CellPrefab vào Assets/Prefabs** folder (tạo folder nếu chưa có)
   - **Xóa CellPrefab khỏi Canvas** (vì nó sẽ được spawn qua code)

## Bước 3: Tạo Main Scene

1. **Tạo UI Layout:**
   - Canvas (Main)
     - Header Panel (chứa buttons)
       - Load Button
       - Push Button
       - Add Row Button
     - Grid Scroll View
       - Content (Grid Layout Group - ngang)
     - Status Text

2. **Load Button:**
   - Thêm Button - TextMeshPro
   - Text: "Load Data"
   - Đặt tên: "LoadButton"

3. **Push Button:**
   - Thêm Button - TextMeshPro
   - Text: "Push to Sheet"
   - Đặt tên: "PushButton"

4. **Add Row Button:**
   - Thêm Button - TextMeshPro
   - Text: "Add Row"
   - Đặt tên: "AddRowButton"

5. **Grid Scroll View:**
   - Right-click Canvas → UI → Scroll View - TextMeshPro
   - Đặt tên: "GridScrollView"
   - **Content:**
     - Thêm **GridLayoutGroup**:
       - Cell Size: (200, 40)
       - Spacing: (2, 2)
       - Constraint: Preferred Size

6. **Status Text:**
   - Thêm Text - TextMeshPro
   - Đặt tên: "StatusText"

## Bước 4: Attach Scripts

1. **Tạo GameObject quản lý API:**
   - Tạo empty GameObject "ExcelManager"
   - Thêm script **ExcelAPIManager** vào

2. **Attach UI Manager:**
   - Select Canvas hoặc main panel
   - Thêm script **ExcelUIManager**
   - **Gán references trong Inspector:**
     - Deployment URL: [paste URL từ Google Apps Script]
     - Grid Content: Drag Content (từ GridScrollView)
     - Cell Prefab: Drag CellPrefab từ Assets/Prefabs
     - Load Button: Drag LoadButton
     - Push Button: Drag PushButton
     - Add Row Button: Drag AddRowButton
     - Status Text: Drag StatusText
     - Grid Layout: Grid Content → GridLayoutGroup

## Bước 5: Paste Deployment URL

1. Copy URL từ Google Apps Script:
   ```
   https://script.google.com/macros/s/AKfycbxITObh8XDgNJ_i_Pucqu9JcNs1_gWD7rcFAD2ZjGfQqCFo5sgbwDF99XfkzV5nAAUKFw/exec
   ```

2. Paste vào **ExcelUIManager → Deployment URL** trong Inspector

## Bước 6: Test

1. Play scene
2. Click **Load Data** → sẽ load từ Google Sheets
3. Chỉnh sửa dữ liệu trong các cells
4. Click **Add Row** để thêm hàng mới
5. Click **Push to Sheet** để đẩy lên Google Sheets

## Troubleshooting

**Lỗi "API URL not initialized":**
- Kiểm tra Deployment URL có paste đúng không trong Inspector

**Lỗi "Error loading data":**
- Mở Console → xem lỗi chi tiết
- Kiểm tra Google Apps Script có được deploy không

**Dữ liệu không hiển thị:**
- Kiểm tra Grid Content có được gán đúng không
- Kiểm tra Cell Prefab có InputField không

**Data không push lên:**
- Kiểm tra có ít nhất một dòng có "Mã đơn hàng" không
- Xem Console log để debug

## Features

- ✅ Load dữ liệu từ Google Sheets
- ✅ Chỉnh sửa dữ liệu trực tiếp trong Unity UI
- ✅ Thêm hàng mới
- ✅ Push dữ liệu lên Google Sheets
- ✅ Tự động mapping tiếng Việt không dấu ↔ tiếng Việt có dấu
- ✅ Status message hiển thị tiến trình

## Để sử dụng với Google Sheet khác

1. Copy code Google Apps Script sang sheet mới
2. Deploy lại
3. Copy Deployment URL mới
4. Paste vào ExcelUIManager → Deployment URL
5. Click Play!

Không cần thay đổi code Unity!

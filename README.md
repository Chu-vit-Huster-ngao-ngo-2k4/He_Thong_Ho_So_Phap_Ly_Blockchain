# Hệ thống Quản lý Hồ sơ Pháp lý Số hóa

## Giới thiệu

Đây là hệ thống giúp số hóa quy trình quản lý hồ sơ pháp lý, cho phép người dùng tạo, lưu trữ, quản lý và xác minh các tài liệu pháp lý một cách an toàn, hiện đại và minh bạch.

## Tính năng chính
- Đăng ký, đăng nhập người dùng
- Tạo, xem, cập nhật, xóa hồ sơ pháp lý
- Upload nhiều file đính kèm (PDF, Word)
- Lưu trữ file trên server, lưu metadata và đường dẫn file trong database
- Sinh mã băm SHA-256 cho mỗi file khi upload
- Xác minh hồ sơ: kiểm tra tính toàn vẹn file bằng mã băm SHA-256
- Giao diện quản lý hồ sơ dạng bảng, lọc theo trạng thái
- Hiển thị tên người dùng, phân quyền truy cập

## Công nghệ sử dụng
- ASP.NET Core MVC
- Entity Framework Core (Pomelo MySQL)
- MySQL
- Bootstrap 5, Font Awesome
- BCrypt (băm mật khẩu)
- SHA-256 (băm file)

## Cài đặt & Chạy thử
1. **Clone dự án** về máy
2. Cài đặt .NET SDK, MySQL
3. Cấu hình chuỗi kết nối MySQL trong `appsettings.json`
4. Chạy lệnh migrate để tạo database:
   ```bash
   dotnet ef database update
   ```
5. Chạy ứng dụng:
   ```bash
   dotnet run
   ```
6. Truy cập http://localhost:xxxx trên trình duyệt

## Ý nghĩa ứng dụng
- Số hóa quy trình quản lý hồ sơ pháp lý, tiết kiệm thời gian, chi phí
- Đảm bảo tính toàn vẹn, chống giả mạo tài liệu nhờ mã băm SHA-256
- Tăng tính bảo mật, minh bạch, dễ dàng truy xuất và xác minh hồ sơ
- Hỗ trợ chuyển đổi số cho tổ chức, doanh nghiệp, cá nhân

## Hướng dẫn xác minh hồ sơ
1. Truy cập menu **"Xác minh hồ sơ"** trên thanh điều hướng
2. Upload file cần xác minh (PDF, Word...)
3. Hệ thống sẽ tự động tính mã băm SHA-256 và so sánh với các file đã lưu
4. Nếu trùng, hệ thống thông báo xác minh thành công và trả về thông tin hồ sơ chứa file đó

---

**Mọi thắc mắc, góp ý xin liên hệ nhóm phát triển!** 
# ChatOps Guardian · Bàn giao frontend WPF

Solution tiếp nối ChatOps.zip, SDK .NET 10 cho Windows, WPF/MVVM. Mở ChatOpsSolution.slnx hoặc ChatOps.csproj trong Visual Studio hỗ trợ .NET 10 với workload .NET desktop development. Có thể dùng `dotnet build ChatOps.csproj -c Release` trên Windows rồi `dotnet run --project ChatOps.csproj`.

## Kết nối
Đặt biến môi trường CHATOPS_API_BASE_URL thành URL đầy đủ của API, **bao gồm tiền tố /api/v1/**. Trong PowerShell, thiết lập biến trong cùng terminal trước khi chạy ứng dụng. Nếu chạy từ IDE, mở IDE từ terminal đó hoặc khởi động lại IDE sau khi đặt biến người dùng. Client chấp nhận HTTPS và HTTP trên loopback cho phát triển. Khi thiếu cấu hình, ứng dụng mở khung điều hướng và hiển thị hướng dẫn kết nối. Chỉ nạp dữ liệu vận hành sau đăng nhập. Gói không có tài khoản, token, dịch vụ cục bộ hay dữ liệu seed.

## Điểm tích hợp
- Model/Contracts.cs là schema có kiểu. JSON dùng camelCase. Trả danh sách rỗng bằng [], số đo chưa có bằng null, thời điểm bằng ISO 8601 có offset. Trả nhãn severity/workflowState tiếng Việt như bộ lọc trong IncidentsViewModel. Nếu server dùng enum khác, ánh xạ tại adapter. Mỗi lần triển khai cần thống nhất bản dịch và mã trạng thái.
- Services/GuardianApi.cs chịu trách nhiệm HTTP, bearer token trong bộ nhớ, thời hạn chờ và xử lý lỗi. 401 của phiên hoạt động đóng cửa sổ con và xóa dữ liệu phiên. Đăng xuất cục bộ xóa token khỏi client. Thu hồi token phía server cần endpoint do backend thống nhất.
- ViewModel/PageViewModel.cs chứa các truy vấn đọc của 5 mục sidebar. ViewModel/Details.cs quản lý các form, việc tải chi tiết và quyết định tại chỗ. MainWindowViewModel điều phối điều hướng. Views chứa màn hình, Controls chứa đồ thị và nhãn trạng thái.
- Nút ✓/× trong danh sách đưa người dùng tới sự cố để đối chiếu đề xuất. Hai nút ghi quyết định nằm ngay trong khối Đề xuất xử lý của cửa sổ chi tiết. Cửa sổ cha tải lại khi đóng. Các nút phía client là hỗ trợ giao diện; backend kiểm tra quyền, TTL, trạng thái, người đề xuất và dịch vụ thuộc allowlist tại thời điểm ghi.

## Hợp đồng HTTP đề xuất
Đường dẫn tính từ CHATOPS_API_BASE_URL. Phần mở rộng so với tài liệu review gồm bearer token, phân trang, quyền CRUD, bộ lọc KPI, sự kiện Replay và các chuỗi dữ liệu cho biểu đồ. Backend có thể triển khai hợp đồng này hoặc chỉnh adapter trước tích hợp.

| Phương thức / đường dẫn | Request | Response thành công |
|---|---|---|
| POST sessions | username, password | Session gồm accessToken, displayName, roles, allowedActions |
| GET overview | — | Overview. openCount tính sự cố riêng biệt. pendingIncidentCount là tập con, pendingRequestCount đếm yêu cầu. queue đã xếp severity rồi expiresAt. trend là chuỗi số sự cố theo thời gian. |
| GET devices | q, page (từ 1), online=true tùy chọn | Page&lt;Device&gt; gồm items, hasMore |
| GET devices/{id} | — | DeviceDetail gồm device, lastSeenAt, diskPct, appHealth, ba trend và incidents của riêng máy |
| POST devices / PATCH devices/{id} | pcName, ip | 2xx sau khi lưu |
| DELETE devices/{id} | — | 2xx sau khi xóa |
| GET incidents | q, severity, workflowState, openOnly, page | Page&lt;Incident&gt;. Giá trị lọc trống nghĩa là tất cả |
| GET incidents/{id} | — | IncidentDetail gồm incident, events, evidenceRefs, diagnosis, proposedAction, taskStatus, probeResult |
| GET requests/{id} | — | ActionRequest. status=pending để cho phép xem thao tác. allowedActions và blockedReason mô tả quyền hiện tại |
| POST requests/{id}/decision | decision=approve hoặc reject, header Idempotency-Key | 2xx. UI đọc lại incident và request, kể cả sau lỗi ghi. Backend xử lý ghi nguyên tử, lưu idempotency key và từ chối quyết định thứ hai. |
| GET incidents/{id}/replay hoặc GET replay | pc, from, to. Route thứ hai dùng khi chỉ nhập tên PC. | ReplayResult. Khoảng thời gian gồm from và loại trừ to, quy đổi từ ngày địa phương. Sắp xếp theo occurredAt. |
| GET reports/weekly | weekStart=yyyy-MM-dd (thứ Hai) | Weekly theo vai trò. Không có báo cáo trả 404. DiskTrend dùng cùng đơn vị xuyên chuỗi; ghi đơn vị trong diskForecast. |

Quyền cấp phiên dùng createDevice. Quyền trên bản ghi thiết bị dùng update, delete. Quyền trên yêu cầu và dòng sự cố dùng approve, reject. Khi server trả 403, 409, 410, client hiển thị thông báo; sau lỗi quyết định, client đọc lại server trước khi cho thao tác lại. Khi không đọc được trạng thái mới, dùng Tải lại. Client không gửi lệnh xuống endpoint. Kết quả task và kết quả probe là hai trường độc lập.

Evidence.redactedPreview chỉ chứa dữ liệu đã lọc. TimelineEvent.metrics và snapshotAt có thể null. screenshotBase64 là ảnh PNG/JPEG đã che dữ liệu, tùy chọn, tối đa 8 triệu ký tự mã hóa. Màn hình Replay chọn mốc, xem bằng chứng/ảnh và phát lần lượt theo thời gian. Backend chịu trách nhiệm giới hạn nội dung trả về theo quyền.

## Kiểm tra tích hợp trên Windows
1. Khởi động khi chưa cấu hình URL, kiểm tra thông báo và đóng/mở cửa sổ đăng nhập.
2. Đăng nhập sai rồi đúng; kiểm tra dữ liệu theo quyền, đổi phiên và xử lý 401.
3. Từ KPI mở danh sách có bộ lọc; tìm, chuyển trang, kiểm tra kết quả rỗng, dữ liệu thiếu và máy offline.
4. Thêm/sửa/xóa theo quyền. Kiểm tra xác nhận tên thiết bị và refresh sau khi lưu.
5. Mở một sự cố chờ duyệt. Thử đúng quyền, sai quyền, hết hạn, xung đột và ngắt mạng sau khi gửi. So sánh trạng thái trên server và dòng danh sách. Sau duyệt chờ probe trước khi ghi nhận phục hồi.
6. Mở Replay từ sự cố, đổi mốc, kiểm tra khi thiếu snapshot hoặc evidence hết hạn. Đổi tuần báo cáo và kiểm tra scope theo phiên.

using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using VnPay.Models;

namespace VnPay.Services.VnPay
{
    public class VnPayService
    {
        private readonly IConfiguration config;
        private readonly IHttpContextAccessor httpContextAccessor;

        public VnPayService(IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            this.config = config;
            this.httpContextAccessor = httpContextAccessor;
        }

        public string getPaymentUrl(Order order)
        {
            //Vnpay config
            string version = config["VnPay:Version"];
            string command = config["Vnpay:Command"];
            string tmnCode = config["VnPay:TmnCode"];
            string amount = (order.Amount*100).ToString();
            string bankCode = config["VnPay:BankCode"];

            string createDate = order.CreatedDate != null ? 
                order.CreatedDate.Value.ToString("yyyyMMddHHmmss") : 
                DateTime.Now.ToString("yyyyMMddHHmmss");

            string currCode = config["VnPay:CurrCode"];
            string locale = config["VnPay:Locale"];
            string orderInfo = order.OrderInfo.ToString();
            string orderType = order.OrderType.ToString();
            string url = config["VnPay:Url"];
            string returnUrl = config["VnPay:ReturnUrl"];
            string hashSecret = config["VnPay:HashSecret"];
            string ipAddress = getIpAddress();

            PayLib pay = new PayLib();

            pay.AddRequestData("vnp_Version", version); //Phiên bản api mà merchant kết nối. Phiên bản hiện tại là 2.1.0
            pay.AddRequestData("vnp_Command", command); //Mã API sử dụng, mã cho giao dịch thanh toán là 'pay'
            pay.AddRequestData("vnp_TmnCode", tmnCode); //Mã website của merchant trên hệ thống của VNPAY (khi đăng ký tài khoản sẽ có trong mail VNPAY gửi về)
            pay.AddRequestData("vnp_Amount", amount); //số tiền cần thanh toán, công thức: số tiền * 100 - ví dụ 10.000 (mười nghìn đồng) --> 1000000
            pay.AddRequestData("vnp_BankCode", bankCode); //Mã Ngân hàng thanh toán (tham khảo: https://sandbox.vnpayment.vn/apis/danh-sach-ngan-hang/), có thể để trống, người dùng có thể chọn trên cổng thanh toán VNPAY
            pay.AddRequestData("vnp_CreateDate", createDate); //ngày thanh toán theo định dạng yyyyMMddHHmmss
            pay.AddRequestData("vnp_CurrCode", currCode); //Đơn vị tiền tệ sử dụng thanh toán. Hiện tại chỉ hỗ trợ VND
            pay.AddRequestData("vnp_IpAddr", ipAddress); //Địa chỉ IP của khách hàng thực hiện giao dịch
            pay.AddRequestData("vnp_Locale", locale); //Ngôn ngữ giao diện hiển thị - Tiếng Việt (vn), Tiếng Anh (en)
            pay.AddRequestData("vnp_OrderInfo", orderInfo); //Thông tin mô tả nội dung thanh toán
            pay.AddRequestData("vnp_OrderType", orderType); //topup: Nạp tiền điện thoại - billpayment: Thanh toán hóa đơn - fashion: Thời trang - other: Thanh toán trực tuyến
            pay.AddRequestData("vnp_ReturnUrl", returnUrl); //URL thông báo kết quả giao dịch khi Khách hàng kết thúc thanh toán
            pay.AddRequestData("vnp_TxnRef", DateTime.Now.Ticks.ToString()); //mã hóa đơn

            string paymentUrl = pay.CreateRequestUrl(url, hashSecret);

            return paymentUrl;
        }

        public static string HmacSHA512(string key, string inputData)
        {
            var hash = new StringBuilder();
            byte[] keyBytes = Encoding.UTF8.GetBytes(key);
            byte[] inputBytes = Encoding.UTF8.GetBytes(inputData);
            using (var hmac = new HMACSHA512(keyBytes))
            {
                byte[] hashValue = hmac.ComputeHash(inputBytes);
                foreach (var theByte in hashValue)
                {
                    hash.Append(theByte.ToString("x2"));
                }
            }

            return hash.ToString();
        }

        public string getIpAddress()
        {
            var httpContext = httpContextAccessor.HttpContext;
            string ipAddress = httpContext.Connection.RemoteIpAddress.ToString();
            if (httpContext.Request.Headers.ContainsKey("X-Forwarded-For"))
            {
                ipAddress = httpContext.Request.Headers["X-Forwarded-For"];
            }
            return ipAddress;
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using VnPay.Data;
using VnPay.Models;
using VnPay.Services.VnPay;

namespace VnPay.Controllers
{
    public class OrdersController : Controller
    {
        private readonly VnPayContext _context;
        private readonly IConfiguration config;
        private readonly IHttpContextAccessor httpContextAccessor;



        public OrdersController(VnPayContext context, IConfiguration config, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            this.config = config;
            this.httpContextAccessor=httpContextAccessor;
        }

        // GET: Orders
        public async Task<IActionResult> Index()
        {
              return _context.Order != null ? 
                          View(await _context.Order.ToListAsync()) :
                          Problem("Entity set 'VnPayContext.Order'  is null.");
        }

        // GET: Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Order == null)
            {
                return NotFound();
            }

            var order = await _context.Order
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // GET: Orders/Create
        public IActionResult Create()
        {
            return View();
        }



        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,OrderInfo,OrderType,Amount,CreatedDate,UpdatedDate")] Order order)
        {
            if (ModelState.IsValid)
            {
                _context.Add(order);

                var Payment = new VnPayService(config,httpContextAccessor);

                string paymentVnPayUrl =  Payment.getPaymentUrl(order);
                
                return Redirect(paymentVnPayUrl);


                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(order);
        }
        [HttpGet]
        public string CheckOut()
        {
            string Message = null;
            var Request = HttpContext.Request;


            string hashSecret = config["VnPay:HashSecret"]; //Chuỗi bí mật

            var vnpayData = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());
            PayLib pay = new PayLib();

            if (vnpayData.Count()<1)
            {
                return "Đơn hàng chưa được khởi tạo";
            };

            //lấy toàn bộ dữ liệu được trả về
            foreach (KeyValuePair<string, string> pair in vnpayData)
            {
                string key = pair.Key;
                string value = pair.Value;

                if (!string.IsNullOrEmpty(key) && key.StartsWith("vnp_"))
                {
                    pay.AddResponseData(key, value);
                }
            }

            long orderId = Convert.ToInt64(pay.GetResponseData("vnp_TxnRef")); //mã hóa đơn
            long vnpayTranId = Convert.ToInt64(pay.GetResponseData("vnp_TransactionNo")); //mã giao dịch tại hệ thống VNPAY
            string vnp_ResponseCode = pay.GetResponseData("vnp_ResponseCode"); //response code: 00 - thành công, khác 00 - xem thêm https://sandbox.vnpayment.vn/apis/docs/bang-ma-loi/
            string vnp_SecureHash = Request.Query["vnp_SecureHash"]; //hash của dữ liệu trả về

            bool checkSignature = pay.ValidateSignature(vnp_SecureHash, hashSecret); //check chữ ký đúng hay không?

            if (checkSignature)
            {
                if (vnp_ResponseCode == "00")
                {
                    //Thanh toán thành công
                    Message = "Thanh toán thành công hóa đơn " + orderId + " | Mã giao dịch: " + vnpayTranId;
                }
                else
                {
                    //Thanh toán không thành công. Mã lỗi: vnp_ResponseCode
                    Message = "Có lỗi xảy ra trong quá trình xử lý hóa đơn " + orderId + " | Mã giao dịch: " + vnpayTranId + " | Mã lỗi: " + vnp_ResponseCode;
                }
            }
            else
            {
                Message = "Có lỗi xảy ra trong quá trình xử lý";
            }

            return Message;
        }


        private bool OrderExists(int id)
        {
          return (_context.Order?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}

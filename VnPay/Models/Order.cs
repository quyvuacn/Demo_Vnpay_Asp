namespace VnPay.Models
{
    public class Order : BaseEntity
    {
        public int Id { get; set; }
        public string OrderInfo { get; set; }
        public string OrderType { get; set; } = "other";
        public double Amount { get; set; }
    }
}

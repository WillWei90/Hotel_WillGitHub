namespace HotelBookingSystem.Models
{
    public class OrderDetailsViewModel
    {
        public int OrderNo { get; set; }
        public required string RoomName { get; set; }
        public decimal Price { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsPay { get; set; }
        public bool Cancel { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
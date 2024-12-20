namespace HotelBookingSystem.Models
{
    public class ReserveRoomViewModel
    {
        public int RoomNo { get; set; }
        public required string RoomName { get; set; }
        public decimal Price { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}

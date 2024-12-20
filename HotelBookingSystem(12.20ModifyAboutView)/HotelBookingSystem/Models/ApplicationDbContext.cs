//using Microsoft.EntityFrameworkCore;

//namespace HotelBookingSystem.Models
//{
//    public class HotelDbContext : DbContext
//    {
//        public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options)
//        {
//        }

//        public required DbSet<Order> Orders { get; set; }
//        public required DbSet<Room> Rooms { get; set; }

//        protected override void OnModelCreating(ModelBuilder modelBuilder)
//        {
//            base.OnModelCreating(modelBuilder);

//            // ©ú½T«ü©wªí¦WºÙ
//            modelBuilder.Entity<Order>().ToTable("Order");

//            // °t¸m Order Ãþ¹ïÀ³ªº¸ê®Æªí©M¥DÁä
//            modelBuilder.Entity<Order>().ToTable("Order").HasKey(o => o.OrderNo);

//            // °t¸m Room Ãþ¹ïÀ³ªº¸ê®Æªí©M¥DÁä
//            modelBuilder.Entity<Room>().ToTable("Room").HasKey(r => r.RoomNo);
//        }
//    }
//}

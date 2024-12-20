using Microsoft.EntityFrameworkCore;

namespace HotelBookingSystem.Models
{
    public class HotelDbContext : DbContext
    {
        public HotelDbContext(DbContextOptions<HotelDbContext> options) : base(options)
        {
        }

        public required DbSet<User> Users { get; set; }
        public required DbSet<Order> Orders { get; set; }
        public required DbSet<Room> Rooms { get; set; }
        public required DbSet<Member> Members { get; set; }

        //will新增
        public DbSet<MemberAccount> MemberAccounts { get; set; }
        public DbSet<QA> QAs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ©ú½T«ü©wªí¦WºÙ
            modelBuilder.Entity<Order>().ToTable("Order");

            // °t¸m Order Ãþ¹ïÀ³ªº¸ê®Æªí©M¥DÁä
            modelBuilder.Entity<Order>().ToTable("Order").HasKey(o => o.OrderNo);

            // °t¸m Room Ãþ¹ïÀ³ªº¸ê®Æªí©M¥DÁä
            modelBuilder.Entity<Room>().ToTable("Room").HasKey(r => r.RoomNo);
            // will新增, 使用Member資料表, MemberNo為主鍵
            modelBuilder.Entity<MemberAccount>().ToTable("Member").HasKey(m => m.MemberNo); //指定資料表名稱
                                                                                            // 添加 MemberAccount 和 Member 之間的關係設定
                                                                                            // 明確指定資料表欄位對應
            modelBuilder.Entity<MemberAccount>()
                .Property(m => m.UserName).HasColumnName("UserName");
            modelBuilder.Entity<MemberAccount>()
                .Property(m => m.Password).HasColumnName("Password");
            modelBuilder.Entity<MemberAccount>()
                .Property(m => m.Phone).HasColumnName("Phone");

            // 如果確實需要一對一關係
            modelBuilder.Entity<MemberAccount>()
                .HasOne<Member>() // 與 Member 建立一對一關係
                .WithOne() // 另一側也是一對一
                .HasForeignKey<MemberAccount>(m => m.MemberNo); // 使用 MemberNo 作為外鍵
        }
    }
}

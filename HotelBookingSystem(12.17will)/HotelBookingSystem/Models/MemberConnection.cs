using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;

namespace HotelBookingSystem.Models
{
    public class MemberConnection
    {
        //連線字串變為屬性
        //private readonly string connStr = "Data Source=114.32.69.228,61433\\SQLEXPRESS;Database=Project;User ID=user;Password=1qaz@WSX3edc;Trusted_Connection=True";
        private readonly string connStr = "Data Source=114.32.69.228,61433\\SQLEXPRESS;Database=Project;User ID=lazzydog;Password=lazzydog;encrypt=false;";

        //資料庫讀取
        public List<MemberAccount> getAccounts()
        {
            List<MemberAccount> accounts = new List<MemberAccount>();

            //連線字串丟給connection
            SqlConnection sqlConnection = new SqlConnection(connStr);

            //產生sql語法
            SqlCommand sqlCommand = new SqlCommand("SELECT * FROM Member");

            //連線
            sqlCommand.Connection = sqlConnection;
            sqlConnection.Open();

            //執行sql語法做讀取
            SqlDataReader reader = sqlCommand.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    MemberAccount myAccount = new MemberAccount
                    {
                        MemberNo = reader.GetInt32(reader.GetOrdinal("MemberNo")),
                        UserName = reader.GetString(reader.GetOrdinal("UserName")),
                        Password = reader.GetString(reader.GetOrdinal("Password")),
                        Phone = reader.GetString(reader.GetOrdinal("Phone")),
                        Birthday = reader.GetDateTime(reader.GetOrdinal("Birthday")),
                        JoinDate = reader.GetDateTime(reader.GetOrdinal("JoinDate"))
                    };
                    accounts.Add(myAccount);
                }
            }
            else
            {
                Console.WriteLine("資料庫為空！");
            }
            sqlConnection.Close();
            return accounts;//資料送回給讀資料庫的MemberController裡的SignIn()
        }

        //資料庫寫入
        public void newAccount(MemberAccount user)
        {
            //string myMemberNo = user.MemberNo;
            string myUserName = user.UserName;
            string myPassword = user.Password;
            string myPhone = user.Phone;
            string myBirthday = user.Birthday.ToString();

            SqlConnection sqlconnection = new SqlConnection(connStr);
            SqlCommand sqlcommand = new SqlCommand(@"INSERT INTO Member(UserName,Password,Birthday,Phone) VALUES(@myUserName,@myPassword, @myBirthday,@myPhone)");
            sqlcommand.Connection = sqlconnection;

            //加入帳號密碼電話
            //sqlcommand.Parameters.Add(new SqlParameter("@myMemberNo", user.MemberNo));
            sqlcommand.Parameters.Add(new SqlParameter("@myUserName", user.UserName));
            sqlcommand.Parameters.Add(new SqlParameter("@myPassword", user.Password));
            sqlcommand.Parameters.Add(new SqlParameter("@myBirthday", user.Birthday));
            sqlcommand.Parameters.Add(new SqlParameter("@myPhone", user.Phone));


            //執行語法
            sqlconnection.Open();
            sqlcommand.ExecuteNonQuery();
            sqlconnection.Close();
        }

        public bool IsUserNameExists(string myUserName)
        {
            // 使用 using 語句確保資源正確釋放
            using (SqlConnection sqlConnection = new SqlConnection(connStr))
            {
                // 在 SqlCommand 中指定連接
                using (SqlCommand sqlCommand = new SqlCommand("SELECT COUNT(*) FROM Member WHERE UserName = @myUserName", sqlConnection))
                {
                    // 注意：參數名稱要對應 SQL 語句中的參數名稱
                    sqlCommand.Parameters.AddWithValue("@myUserName", myUserName);

                    // 開啟連接
                    sqlConnection.Open();

                    // 執行查詢並取得符合條件的數量
                    int count = (int)sqlCommand.ExecuteScalar();

                    // 如果count > 0，表示帳號已存在
                    return count > 0;
                }
            }
        }
    }
}
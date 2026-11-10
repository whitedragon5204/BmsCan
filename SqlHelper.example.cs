using SqlSugar;
using System;

namespace Bms88122
{
    public class SqlHelper
    {
        static SqlSugarClient db;

        public static SqlSugarClient DbSugarContext
        {
            get
            {
                if (db == null)
                {
                    db = new SqlSugarClient(new ConnectionConfig()
                    {
                        DbType = DbType.MySql,
                        ConnectionString = "Data Source=你的数据库地址;port=3306;User ID=账号;Password=密码;Database=pack_record;Charset=utf8;",
                        IsAutoCloseConnection = true,
                    },
                    client =>
                    {
                        client.Aop.OnLogExecuting = (sql, pars) =>
                        {
                            Console.WriteLine(sql);
                        };
                    });
                }
                return db;
            }
        }
    }
}

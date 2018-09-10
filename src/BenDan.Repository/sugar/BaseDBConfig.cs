using System;
using System.Collections.Generic;
using System.Text;

namespace BenDan.Repository.sugar
{
    public class BaseDBConfig
    {
        /// <summary>
        /// 获取 SQL Server 连接字符串
        /// </summary>
        public static string ConnectionString { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace YYYQ.Framework.Binlog
{
    /// <summary>
    /// Binlog选项类
    /// </summary>
    public class BinlogOptions
    {
        /// <summary>
        /// 订阅的表列表，以库名.表名的形式
        /// </summary>
        public List<string> SubTableList { get; set; }
        /// <summary>
        /// KAFKA服务器IP
        /// </summary>
        public string ServerIP { get; set; }
        /// <summary>
        /// KAFKA服务器端口
        /// </summary>
        public int ServerPort { get; set; }
        /// <summary>
        /// 同一应用的GroupId应该一致的，如果负载均衡时被同一消费则不会再消费了..
        /// </summary>
        public string GroupId { get; set; }
    }
}

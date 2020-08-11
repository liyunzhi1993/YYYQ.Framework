using System;
using System.Collections.Generic;
using System.Text;

namespace YYYQ.Framework.Binlog
{
    public class BinlogReceivedMessageModel
    {
        /// <summary>
        /// 库名
        /// </summary>
        public string DataBaseName { get; set; }
        /// <summary>
        /// 表名
        /// </summary>
        public string TableName { get; set; }
        /// <summary>
        /// 行数据
        /// </summary>
        public string RowJson { get; set; }
        /// <summary>
        /// 变更类型
        /// </summary>
        public string Type { get; set; }
    }
}

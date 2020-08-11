using System;
using System.Collections.Generic;
using System.Text;

namespace YYYQ.Framework.Cache
{
    public class BinlogRowInfoModel
    {
        /// <summary>
        /// 字段名
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 字段值
        /// </summary>
        public string value { get; set; }
        /// <summary>
        /// 是否Null值
        /// </summary>
        public bool isNull { get; set; }
        /// <summary>
        /// 字段类型
        /// </summary>
        public string mysqlType { get; set; }
    }
}

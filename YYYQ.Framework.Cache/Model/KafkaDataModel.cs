using System;
using System.Collections.Generic;
using System.Text;

namespace YYYQ.Framework.Cache
{
    public class KafkaDataModel
    {
        /// <summary>
        /// 数据
        /// </summary>
        public object[] data { get; set; }
        /// <summary>
        /// 数据库名
        /// </summary>
        public string database { get; set; }
        /// <summary>
        /// 表名
        /// </summary>
        public string table { get; set; }
        /// <summary>
        /// 变更类型
        /// </summary>
        public string type { get; set; }
    }
}

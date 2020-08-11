using System;
using System.Collections.Generic;
using System.Text;

namespace YYYQ.Framework.Cache
{
    public class CacheTableAttribute:Attribute
    {
        /// <summary>
        /// 表名
        /// </summary>
        public string Name { get; set; }
    }
}

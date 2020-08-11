using System;
using System.Collections.Generic;
using System.Text;

namespace YYYQ.Framework.Cache
{
    public class CacheDbOptions
    {
        /// <summary>
        /// 反射的程序集名称，通过反射可以获取到你需要订阅的数据库表来生成对应的RedisValue
        /// </summary>
        public string AssemblyName { get; set; }
        /// <summary>
        /// 分区ID，需要唯一！
        /// </summary>
        public string GroupId { get; set; }
    }
}

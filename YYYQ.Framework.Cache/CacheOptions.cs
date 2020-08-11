using System;
using System.Collections.Generic;
using System.Text;

namespace YYYQ.Framework.Cache
{
    /// <summary>
    /// 缓存选项类
    /// </summary>
    public class BinlogOptions
    {
        /// <summary>
        /// 单机模式
        /// </summary>
        public string RedisServer { get; set; }
        public string RedisDBId { get; set; }
        /// <summary>
        /// 哨兵模式
        /// </summary>
        public string RedisEndPoints { get; set; }
        public string RedisPwd { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace YYYQ.Framework.Cache
{
    public class CacheDataBaseAttribute:Attribute
    {
        /// <summary>
        /// 数据库名称【需要在apollog中配置】，该组件依赖apollog配置，由于递易系统中，各个环境数据库名称不同导致！
        /// </summary>
        public string ConfigName { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace YYYQ.Framework.Cache
{
    public class ProccessCacheKeyInfoModel
    {
        //数据库名称
        public string DbName { get; set; }
        //表名
        public string TableName { get; set; }
        //组合Key名称
        public List<string> CacheKeyList { get; set; }
    }
}

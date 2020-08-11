using System;
using System.Collections.Generic;
using System.Text;

namespace YYYQ.Framework.Cache
{
    public enum CacheModeEnum
    {
        /// <summary>
        /// 低一致性模式【数据库值几乎不更新，以缓存值为主，如地域信息，字典表等】
        /// </summary>
        Low = 0,
        /// <summary>
        /// 高一致性模式【缓存值和数据库保持一致，保持一致的方式由框架统一处理】
        /// </summary>
        High = 1,
        /// <summary>
        /// 缓存唯一模式【只获取缓存，不从数据库获取，需要其他手段来更新缓存值】
        /// </summary>
        OnlyCache = 2
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace YYYQ.Framework.Config
{
    public interface IConfig
    {
        /// <summary>
        /// 获取配置
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        string Get(string key);
    }
}

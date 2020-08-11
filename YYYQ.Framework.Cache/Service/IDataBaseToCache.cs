using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace YYYQ.Framework.Cache
{
    public interface IDataBaseToCache
    {
        /// <summary>
        /// 监听binlog
        /// </summary>
        void Start(CancellationToken cancellationToken);
    }
}

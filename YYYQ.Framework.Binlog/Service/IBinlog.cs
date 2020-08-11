using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YYYQ.Framework.Binlog
{
    public interface IBinlog
    {
        /// <summary>
        /// 开启监听Binlog
        /// </summary>
        void Start(CancellationToken cancellationToken);
        /// <summary>
        /// 监听事件
        /// </summary>
        event EventHandler<BinlogReceivedEventArgs> BinlogReceived;
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace YYYQ.Framework.Binlog
{
    public class BinlogReceivedEventArgs: EventArgs
    {
        public BinlogReceivedEventArgs(BinlogReceivedMessageModel binlogReceivedMessageModel)
        {
            BinlogReceivedMessageModel = binlogReceivedMessageModel ?? throw new ArgumentNullException(nameof(binlogReceivedMessageModel));
        }

        public BinlogReceivedMessageModel BinlogReceivedMessageModel { get; }
        //public BinlogReceivedEventArgs(string receivedMessage)
        //{
        //    ReceivedMessageJson = receivedMessage ?? throw new ArgumentNullException(nameof(receivedMessage));
        //}

        //public string ReceivedMessageJson { get; }
    }
}

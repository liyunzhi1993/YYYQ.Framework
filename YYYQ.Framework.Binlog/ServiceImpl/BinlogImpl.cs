using Confluent.Kafka;
using YYYQ.Framework.Config;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YYYQ.Framework.Binlog
{
    public class BinlogImpl : IBinlog
    {
        private readonly BinlogOptions _binlogOptions;
        public event EventHandler<BinlogReceivedEventArgs> BinlogReceived;
        public BinlogImpl(BinlogOptions binlogOptions)
        {
            _binlogOptions = binlogOptions;
        }

        public void Start(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_binlogOptions.GroupId))
            {
                throw new Exception("GroupId不能为空！");
            }
            var config = new ConsumerConfig
            {
                BootstrapServers = _binlogOptions.ServerIP + ":" + _binlogOptions.ServerPort,
                GroupId = "Consumer" + _binlogOptions.GroupId,
                AutoOffsetReset = AutoOffsetReset.Latest,
            };

            using (var consumer = new ConsumerBuilder<Ignore, string>(config)
                .SetErrorHandler((_, e) => Console.WriteLine($"Kafka连接错误：Error： {e.Reason}"))
                .Build())
                {
                    consumer.Subscribe(_binlogOptions.SubTableList);
                    try
                    {
                        while (true)
                        {
                            try
                            {
                                var consumeResult = consumer.Consume(cancellationToken);
                                KafkaDataModel kafkaDataModel = JsonConvert.DeserializeObject<KafkaDataModel>(consumeResult.Message.Value);

                                if (kafkaDataModel.data==null)
                                {
                                    continue;
                                }
                                BinlogReceivedMessageModel binlogReceivedMessageModel = new BinlogReceivedMessageModel();
                                binlogReceivedMessageModel.DataBaseName = kafkaDataModel.database;
                                binlogReceivedMessageModel.TableName = kafkaDataModel.table;
                                binlogReceivedMessageModel.RowJson = kafkaDataModel.data[0].ToString();
                                binlogReceivedMessageModel.Type = kafkaDataModel.type;
                                BinlogReceived?.Invoke(this, new BinlogReceivedEventArgs(binlogReceivedMessageModel));
                            }
                            catch (ConsumeException e)
                            {
                                Console.WriteLine($"Kafka消费Error：{e.Error.Reason}");
                            }
                        }
                    }
                    catch (OperationCanceledException e)
                    {
                        //出现错误就取消消费，否则可能会造成消息丢失
                        Console.WriteLine("Kafka消费Error：" + e.StackTrace);
                        consumer.Close();
                    }
            }
        }
    }
}

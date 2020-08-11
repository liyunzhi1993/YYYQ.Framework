# C#基于阿里canal监听mysql binlog kafka模式

## 前言
> 没接触过java spring cloud之前对aop的概念很薄弱，基于c#的通常都是微软封装好的filter类，进而实现就好。很早之前就想做个缓存框架，之前的redis缓存都是通过判断有没有缓存进而插入和更新，大量的重复代码，缓存利用率不高。真正自己设计的时候，发现在高一致模式下同步是个问题，高一致即数据库表与缓存保持一致。幸好，发现了阿里开源的canal正好满足需求。正所谓技术大多数服务于业务场景，本文就是C#基于阿里canal监听mysql binlog kafka模式实现binlog的组件化。

## 符合的业务场景有
> - 监听某张表进行监听，进行推送，如发现订单，推送到各个系统【**微服务体系下，如果有订单中心就不存在这个事**】
> - redis缓存的高一致性模式，即数据库表与缓存保持一致


## 进行监听
> 在startup ConfigureServices中配置
```csharp
services.AddBinlog(options =>
{
    options.SubTableList = new List<string>() {
        "库名.表名"
        //"zcy-test"//监听的表名  库名由于多环境不一致需要配置..
    };
    //options.CanalDestination = _config.Get("CanalDestination");//问DBA 要 Canal名称
    //options.CanalHost = _config.Get("CanalHost");//问DBA
    //options.CanalPort = int.Parse(_config.Get("CanalPort"));//问DBA
    options.ServerIP = "VM_50_11_centos";//KAFKA服务器IP
    options.ServerPort = 9092;//KAFKA服务器端口
    options.GroupId = "YYYQTest";//同一应用的GroupId应该一致的，如果负载均衡时被同一消费则不会再消费了..
});
serviceProvider = services.BuildServiceProvider();

await Task.Factory.StartNew(() =>
{
    var binlogService = serviceProvider.GetService<IBinlog>();
    binlogService.BinlogReceived += ReceivedMessageHandler;//监听事件
    binlogService.Start(new CancellationTokenSource().Token);//开启binlog监听任务
}, TaskCreationOptions.LongRunning);//使用长时间运行Task
```

## 监听处理
```csharp
 public void ReceivedMessageHandler(object sender,BinlogReceivedEventArgs e)
{
    //打印接收到的表行数据变更json，来处理相关业务，建议推送到自己业务向mq里处理削峰
    Console.WriteLine(e.BinlogReceivedMessageModel.RowJson);
}
```
## kafka消费Code
```csharp
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
        AutoOffsetReset = AutoOffsetReset.Latest,//这个模式一定要注意
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
                        BinlogReceived?.Invoke(this, new BinlogReceivedEventArgs(binlogReceivedMessageModel));//进行通知
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
```
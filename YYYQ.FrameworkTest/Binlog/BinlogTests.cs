using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Microsoft.Extensions.Configuration;
using YYYQ.Framework.Config;
using YYYQ.Framework.Binlog;
using System.Threading.Tasks;
using System.Threading;

namespace YYYQ.FrameworkTests.Binlog
{
    [TestClass()]
    public class BinlogTests
    {
        [TestMethod()]
        public async Task AddBinlogTestAsync()
        {
            IServiceCollection services = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json", true, true);
            services.AddConfig(options => {
                options.ConfigurationBuilder = configBuilder;
                options.Namespace = "YYYQ";
            });
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            IConfig _config = serviceProvider.GetService<IConfig>();
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

            await Task.Factory.StartNew(() =>
            {
                var binlogService = serviceProvider.GetService<IBinlog>();
                binlogService.BinlogReceived += ReceivedMessageHandler;//监听事件
                binlogService.Start(new CancellationTokenSource().Token);//开启binlog监听任务
            }, TaskCreationOptions.LongRunning);//使用长时间运行Task

            var cancellationTokenSource = new CancellationTokenSource();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => cancellationTokenSource.Cancel();
            Console.CancelKeyPress += (s, e) => cancellationTokenSource.Cancel();
            await Task.Delay(-1, cancellationTokenSource.Token).ContinueWith(t => { });
        }
        public void ReceivedMessageHandler(object sender, BinlogReceivedEventArgs e)
        {
            Console.WriteLine(e.BinlogReceivedMessageModel.RowJson);
        }
    }
}

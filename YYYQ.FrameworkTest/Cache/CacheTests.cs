using Microsoft.VisualStudio.TestTools.UnitTesting;
using YYYQ.Framework.Cache;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using System.IO;
using Microsoft.Extensions.Configuration;
using YYYQ.Framework.Config;
using AspectCore.Extensions.DependencyInjection;
using AspectCore.DynamicProxy;
using AspectCore.Configuration;
using System.Threading.Tasks;
using System.Threading;

namespace YYYQ.FrameworkTests.Binlog
{
    [TestClass()]
    public class CacheTests
    {
        [TestMethod()]
        public async Task AddCacheTestAsync()
        {
            IServiceCollection services = new ServiceCollection();
            var configBuilder = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddJsonFile("appsettings.json", true, true);
            services.AddConfig(options => {
                options.ConfigurationBuilder = configBuilder;
                options.Namespace = "Dev001.ThirdService";
            });
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            IConfig _config = serviceProvider.GetService<IConfig>();
            services.AddCache(options =>
            {
                options.RedisEndPoints = _config.Get("RedisEndPoints");
                options.RedisServer = _config.Get("RedisServer");
                options.RedisPwd = _config.Get("RedisPwd");
                options.RedisDBId = _config.Get("RedisDBId");
            }, cacheDbOptions => {
                cacheDbOptions.AssemblyName = "SendOrder.Service";
                cacheDbOptions.GroupId = "SendOrderDbToCache";
            });
            services.AddTransient<CacheAttributeTestCacheIN, CacheAttributeTestCache>();
            var i=services.BuildServiceContextProvider();
            var s=i.GetService<CacheAttributeTestCacheIN>();
            s.CacheAttributeTest("123123");
            serviceProvider = services.BuildServiceProvider();
            ICache _cache = serviceProvider.GetService<ICache>();
            IDataBaseToCache _dataBaseToCache = serviceProvider.GetService<IDataBaseToCache>();

            Task.Factory.StartNew(() =>
            {
                _dataBaseToCache.Start(new CancellationTokenSource().Token);
            }, TaskCreationOptions.LongRunning);//使用长时间运行Task

            var testValue = _cache.Get<string>("1123123");
            _cache.InsertAsync("ssss", "sss", 111);

            var cancellationTokenSource = new CancellationTokenSource();
            AppDomain.CurrentDomain.ProcessExit += (s, e) => cancellationTokenSource.Cancel();
            Console.CancelKeyPress += (s, e) => cancellationTokenSource.Cancel();
            await Task.Delay(-1, cancellationTokenSource.Token).ContinueWith(t => { });
        }
    }

    public interface CacheAttributeTestCacheIN
    {
        string CacheAttributeTest(string Ossss);
    }

    public class CacheAttributeTestCache: CacheAttributeTestCacheIN
    {
        [Cache(CacheMode = CacheModeEnum.Low, ReturnType = typeof(string), IsTask = false, Expire = 60 * 60 * 24)]
        public virtual string CacheAttributeTest(string Ossss)
        {
            return "sss";
        }
    }
}
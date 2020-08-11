using System;
using YYYQ.Framework.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace YYYQ.Framework.Binlog {
    /// <summary>
    /// Binlog扩展
    /// </summary>
    public static class Extensions {
        /// <summary>
        /// 注册Binlog缓存操作
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configAction">配置操作</param>
        public static void AddBinlog(this IServiceCollection services, Action<BinlogOptions> configAction) {
            var options = new BinlogOptions();
            configAction(options);
            IServiceProvider serviceProvider = services.BuildServiceProvider();
            IConfig _config = serviceProvider.GetService<IConfig>();
            services.AddSingleton<IBinlog>(cache=>new BinlogImpl(options)); 
        }
    }
}
using System;
using YYYQ.Framework.Config;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using AspectCore.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;

namespace YYYQ.Framework.Cache {
    /// <summary>
    /// Cache缓存扩展
    /// </summary>
    public static class Extensions {
        /// <summary>
        /// 注册Cache缓存操作
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configAction">配置操作</param>
        public static void AddCache(this IServiceCollection services, Action<BinlogOptions> configAction,Action<CacheDbOptions> configCacheDbAction=null) {
            var options = new BinlogOptions();
            configAction(options);
            services.AddSingleton<ICache>(cache=>new RedisCacheImpl(options));
            if (configCacheDbAction!=null)
            {
                IServiceProvider serviceProvider = services.BuildServiceProvider();
                var cacheDbOptions = new CacheDbOptions();
                configCacheDbAction(cacheDbOptions);
                IConfig _config = serviceProvider.GetService<IConfig>();
                ICache _cache = serviceProvider.GetService<ICache>();
                services.TryAddSingleton<IDataBaseToCache>(cd => new DataBaseToCacheImpl(_config, _cache, cacheDbOptions));
            }
        }
    }
}
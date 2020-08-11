using System;
using System.IO;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Com.Ctrip.Framework.Apollo;

namespace YYYQ.Framework.Config {
    /// <summary>
    /// Config扩展
    /// </summary>
    public static class Extensions {
        /// <summary>
        /// 注册Config操作
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="configAction">配置操作</param>
        public static void AddConfig(this IServiceCollection services, Action<ConfigOptions> configAction) {
            var options = new ConfigOptions();
            configAction(options);  
            if (options.Configuration!=null)
            {
                services.TryAddSingleton<IConfig>(config => new ApolloConfigImpl(options.Configuration));
            }
            else
            {
                IConfigurationRoot configuration = null;
                if (!string.IsNullOrEmpty(options.Namespace))
                {
                    configuration = options.ConfigurationBuilder.AddApollo(options.ConfigurationBuilder.Build().GetSection("apollo")).AddDefault().AddNamespace(options.Namespace).Build();
                }
                else
                {
                    configuration = options.ConfigurationBuilder.AddApollo(options.ConfigurationBuilder.Build().GetSection("apollo")).AddDefault().Build();
                }
                services.TryAddSingleton<IConfig>(config => new ApolloConfigImpl(configuration));
            }
        }
    }
}
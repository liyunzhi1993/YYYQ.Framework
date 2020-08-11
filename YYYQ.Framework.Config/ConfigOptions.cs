using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace YYYQ.Framework.Config
{
    public class ConfigOptions
    {
        /// <summary>
        /// 配置节点【.Net Core3.0以上项目支持】
        /// </summary>
        public IConfigurationBuilder ConfigurationBuilder {get;set;}
        /// <summary>
        /// 额外服务【apollo专用】【.Net Core3.0以上项目支持】
        /// </summary>
        public string Namespace { get; set; }
        /// <summary>
        /// 配置节点【.Net Core2.0以上项目支持】
        /// </summary>
        public IConfiguration Configuration { get; set; }
    }
}

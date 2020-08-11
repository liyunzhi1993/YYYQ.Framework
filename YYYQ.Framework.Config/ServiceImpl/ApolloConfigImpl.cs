using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;

namespace YYYQ.Framework.Config
{
    public class ApolloConfigImpl : IConfig
    {
        private readonly IConfiguration configuration;
        public ApolloConfigImpl(IConfiguration _configuration)
        {
            configuration = _configuration;
        }
        public string Get(string key)
        {
            var result = configuration.GetValue(key, "");
            return result;
        }
    }
}

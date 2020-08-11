using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace YYYQ.Framework.Cache
{
    /// <summary>
    /// Redis缓存
    /// </summary>
    public class RedisCacheImpl : ICache
    {
        private readonly IDatabase _database;
        private readonly ConnectionMultiplexer _connectionMultiplexer;
        private readonly JsonSerializerSettings jsonConfig;
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="cacheOptions"></param>
        public RedisCacheImpl(BinlogOptions cacheOptions)
        {
            jsonConfig = new JsonSerializerSettings() { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, NullValueHandling = NullValueHandling.Ignore };

            string endPoints = cacheOptions.RedisEndPoints;//支持多endpoints
            ConfigurationOptions option = new ConfigurationOptions();
            option.AllowAdmin = true;
            if (!string.IsNullOrEmpty(endPoints))
            {
                foreach (string endPoint in endPoints.Split(';'))
                {
                    option.EndPoints.Add(endPoint);
                }
                option.Password = cacheOptions.RedisPwd;//支持多endpoints
                option.ConnectTimeout = 15000;
                option.SyncTimeout = 5000;
                option.AsyncTimeout = 5000;
                option.AbortOnConnectFail = false;
                option.AllowAdmin = true;
                option.KeepAlive = 180;
                _connectionMultiplexer = ConnectionMultiplexer.Connect(option);
            }
            else
            {
                _connectionMultiplexer = ConnectionMultiplexer.Connect(cacheOptions.RedisServer);
            }
            _database=_connectionMultiplexer.GetDatabase(int.Parse(cacheOptions.RedisDBId));
        }


        private bool ChangeMaster(IDatabase database)
        {
            var mex = database.Multiplexer;
            var endpoints = mex.GetEndPoints();
            if (endpoints != null || endpoints.Length < 2)
            {
                return false;
            }
            //多个endpoint 才切换主备服务器
            List<EndPoint> connectedPoints = new List<EndPoint>();
            List<EndPoint> disconnetedPoints = new List<EndPoint>();
            foreach (var item in endpoints)
            {
                //判断哪些服务器可以连接
                var server = mex.GetServer(item);
                if (server.IsConnected)
                {
                    connectedPoints.Add(item);
                }
                else
                {
                    disconnetedPoints.Add(item);
                }
            }
            var connectedPoint = connectedPoints[0];
            if (connectedPoint == null)
            {
                throw new Exception("没有可用的redis服务器");
            }

            //使用哨兵自动切换主从 等待切换
            Thread.Sleep(20 * 1000);
            return true;
        }

        /// <summary>
        /// 判断cacheKey是否存在
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public bool Exists(string cacheKey)
        {
            return _database.KeyExists(cacheKey);
        }

        /// <summary>
        /// 获取缓存【泛型】
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public T Get<T>(string cacheKey)
        {
            var cacheValue = _database.StringGet(cacheKey);
            var value = default(T);
            if (!cacheValue.IsNullOrEmpty)
            {
                try
                {
                    value = JsonConvert.DeserializeObject<T>(cacheValue, jsonConfig);
                }
                catch (Exception ex)
                {
                    if (ChangeMaster(_database)) value = JsonConvert.DeserializeObject<T>(cacheValue, jsonConfig);
                    else throw ex;
                }
            }
            return value;
        }

        /// <summary>
        /// 获取缓存【泛型】【异步】
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public async Task<T> GetAsync<T>(string cacheKey)
        {
            var cacheValue =await _database.StringGetAsync(cacheKey);
            var value = default(T);
            if (!cacheValue.IsNullOrEmpty)
            {
                try
                {
                    value = JsonConvert.DeserializeObject<T>(cacheValue, jsonConfig);
                }
                catch (Exception ex)
                {
                    if (ChangeMaster(_database)) value = JsonConvert.DeserializeObject<T>(cacheValue, jsonConfig);
                    else throw ex;
                }
            }
            return value;
        }

        /// <summary>
        /// 将指定键的对象添加到缓存中，并指定过期时间【秒单位】【异步】
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        /// <param name="cacheValue">缓存值</param>
        /// <param name="expiration">过期时间</param>
        public Task InsertAsync<T>(string cacheKey, T cacheValue, int expiration=-1)
        {
            var jsonData = JsonConvert.SerializeObject(cacheValue);
            if (expiration != -1)
            {
                return Task.FromResult(
               _database.StringSetAsync(cacheKey, jsonData, TimeSpan.FromSeconds(expiration)));
            }
            else
            {
                return Task.FromResult(
               _database.StringSetAsync(cacheKey, jsonData));
            }
        }

        /// <summary>
        /// 移除缓存
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        public Task RemoveAsync(string cacheKey)
        {
            return Task.FromResult(
               _database.KeyDeleteAsync(cacheKey));
        }
    }
}

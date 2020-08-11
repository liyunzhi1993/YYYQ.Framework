using CanalSharp.Client.Impl;
using Com.Alibaba.Otter.Canal.Protocol;
using Confluent.Kafka;
using YYYQ.Framework.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;

namespace YYYQ.Framework.Cache   
{
    public class DataBaseToCacheImpl : IDataBaseToCache
    {
        private readonly IConfig _config;
        private readonly ICache _cache;
        private readonly CacheDbOptions _cacheDbOptions;
        private List<ProccessCacheKeyInfoModel> proccessCacheKeyList = new List<ProccessCacheKeyInfoModel>();
        public DataBaseToCacheImpl(IConfig config, ICache cache, CacheDbOptions cacheDbOptions)
        {
            _config = config;
            _cache = cache;
            _cacheDbOptions = cacheDbOptions;
        }
        public void Start(CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(_cacheDbOptions.GroupId))
                {
                    throw new Exception("GroupId不能为空！");
                }
                var config = new ConsumerConfig
                {
                    BootstrapServers = _config.Get("KafkaServerIP") + ":" + _config.Get("KafkaServerPort"),
                    GroupId = "Consumer" + _cacheDbOptions.GroupId,
                    AutoOffsetReset = AutoOffsetReset.Latest,
                };

                //批量注册订阅表 1、如果有对应DataBase注解则配置的名称为注解的Name值.表名 如果没有则以默认值来
                Assembly assemblyDao = Assembly.Load(_cacheDbOptions.AssemblyName);
                List<System.Type> daoAttributeTypeList = assemblyDao.GetTypes().ToList();
                var dataBaseName = string.Empty;
                List<string> subscribeTableList = new List<string>();
                foreach (var type in daoAttributeTypeList)
                {
                    Attribute attributeDb = type.GetCustomAttribute(typeof(CacheDataBaseAttribute));
                    if (attributeDb != null)
                    {
                        dataBaseName = _config.Get(attributeDb.GetType().GetProperty("ConfigName").GetValue(attributeDb).ToString());
                    }
                    Attribute attributeTable = type.GetCustomAttribute(typeof(CacheTableAttribute));
                    if (attributeTable != null)
                    {
                        subscribeTableList.Add($"{dataBaseName}.{attributeTable.GetType().GetProperty("Name").GetValue(attributeTable).ToString()}");
                    }
                }
                if (subscribeTableList.Count == 0)
                {
                    throw new Exception("没有可订阅的表！");
                }

                //批量获取Dao层的所有包含Cache注解的
                List<System.Type> daoTypeList = assemblyDao.GetTypes().Where(s => !s.IsInterface).ToList();
                //定义基本参数类型，只处理基本参数类型
                List<System.Type> baseTypeList = new List<System.Type> {
                    typeof(string),typeof(int),typeof(double),
                    typeof(long),typeof(float),typeof(decimal),
                    typeof(bool),typeof(char)
                };

                foreach (var type in daoTypeList)
                {
                    Attribute attributeDb = type.GetCustomAttribute(typeof(CacheDataBaseAttribute));
                    Attribute attributeTable = type.GetCustomAttribute(typeof(CacheTableAttribute));
                    if (attributeDb == null)
                    {
                        continue;
                    }
                    if (attributeTable == null)
                    {
                        continue;
                    }
                    var dbName = _config.Get(attributeDb.GetType().GetProperty("ConfigName").GetValue(attributeDb).ToString().ToUpper());
                    var dbTable = attributeTable.GetType().GetProperty("Name").GetValue(attributeTable).ToString().ToUpper();

                    //获取所有方法
                    var methods = type.GetMethods();
                    foreach (var method in methods)
                    {
                        Attribute cacheAttribute = method.GetCustomAttribute(typeof(CacheAttribute));
                        if (cacheAttribute != null)
                        {
                            //只处理高一致性模式
                            if (!cacheAttribute.GetType().GetProperty("CacheMode").GetValue(cacheAttribute).ToString().Equals("High"))
                            {
                                break;
                            }
                            else
                            {
                                //获取参数列表
                                var parameters = method.GetParameters();
                                ProccessCacheKeyInfoModel proccessCacheKeyInfo = new ProccessCacheKeyInfoModel();
                                List<string> cacheKeyList = new List<string>();
                                for (int i = 0; i < parameters.Length; i++)
                                {
                                    if (baseTypeList.Contains(parameters[i].ParameterType))
                                    {
                                        cacheKeyList.Add(parameters[i].Name.ToUpper());
                                    }
                                    else
                                    {
                                        //有非基础参数的类型的 直接不处理
                                        break;
                                    }
                                }
                                if (cacheKeyList.Count > 0)
                                {
                                    proccessCacheKeyInfo.DbName = dbName.ToUpper();
                                    proccessCacheKeyInfo.TableName = dbTable.ToUpper();
                                    proccessCacheKeyInfo.CacheKeyList = cacheKeyList;
                                    proccessCacheKeyList.Add(proccessCacheKeyInfo);
                                }
                            }
                        }
                    }
                }


                using (var consumer = new ConsumerBuilder<Ignore, string>(config)
               .SetErrorHandler((_, e) => Console.WriteLine($"Kafka连接错误：Error： {e.Reason}"))
               .Build())
                {
                    consumer.Subscribe(subscribeTableList);
                    try
                    {
                        while (true)
                        {
                            try
                            {
                                var consumeResult = consumer.Consume(cancellationToken);
                                KafkaDataModel kafkaDataModel = JsonConvert.DeserializeObject<KafkaDataModel>(consumeResult.Message.Value);

                                if (kafkaDataModel.data == null)
                                {
                                    continue;
                                }
                                BinlogReceivedMessageModel binlogReceivedMessageModel = new BinlogReceivedMessageModel();
                                binlogReceivedMessageModel.DataBaseName = kafkaDataModel.database;
                                binlogReceivedMessageModel.TableName = kafkaDataModel.table;
                                binlogReceivedMessageModel.Rows = kafkaDataModel.data[0];

                                //当前需要处理的缓存Key列表
                                List<ProccessCacheKeyInfoModel> currentProccessKeyList = proccessCacheKeyList.Where(x => x.DbName.Equals(binlogReceivedMessageModel.DataBaseName.ToUpper()) && x.TableName.Equals(binlogReceivedMessageModel.TableName.ToUpper())).ToList();
                                List<KeyValuePair<string,string>> rowList = new List<KeyValuePair<string, string>>();
                                rowList = JsonConvert.DeserializeObject<Dictionary<string, string>>(binlogReceivedMessageModel.Rows.ToString()).ToList();
                                foreach (var proccessCacheKey in currentProccessKeyList)
                                {
                                    var cacheKeyName = $"{proccessCacheKey.DbName}:{proccessCacheKey.TableName}:" +
                                        $"{string.Join(":", proccessCacheKey.CacheKeyList.ToArray())}:";

                                    var cacheKeyValueList = new List<string>(); 
                                    //参数是否完整
                                    var isParamAll = true;
                                    foreach (var cacheKeyP in proccessCacheKey.CacheKeyList)
                                    {
                                        //根据字段名获取字段值
                                        var cacheKeyPValue=rowList.Where(x=>x.Key.ToUpper().Equals(cacheKeyP)).FirstOrDefault();
                                        if (string.IsNullOrEmpty(cacheKeyPValue.Key))
                                        {
                                            isParamAll = false;
                                            break;
                                        }
                                        else
                                        {
                                            cacheKeyValueList.Add(cacheKeyPValue.Value);
                                        }
                                    }
                                    if (!isParamAll)
                                    {
                                        break;
                                    }
                                    if (cacheKeyValueList.Count > 0 && cacheKeyValueList.Count == proccessCacheKey.CacheKeyList.Count)
                                    {
                                        cacheKeyName += string.Join(":", cacheKeyValueList.ToArray());

                                        //判断删除逻辑IsDelete=1
                                        var isDelete=rowList.Where(x=>x.Key.Equals("IsDelete")).FirstOrDefault();
                                        var isDeleted=rowList.Where(x=>x.Key.Equals("IsDeleted")).FirstOrDefault();
                                        if ((!string.IsNullOrEmpty(isDelete.Key) && isDelete.Value.Equals("1"))|| (!string.IsNullOrEmpty(isDeleted.Key) && isDeleted.Value.Equals("1")))
                                        {
                                            _cache.RemoveAsync(cacheKeyName);
                                        }
                                        else
                                        {
                                            _cache.InsertAsync(cacheKeyName, binlogReceivedMessageModel.Rows.ToString().Replace("\n", "").Replace("\r", "").Replace(" ", ""), 60 * 60 * 24);
                                        }
                                    }
                                      
                                }
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
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace + ex.Message);
                throw new Exception($"binlog回调异常#{ex.ToString()}#{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 处理批量数据
        /// </summary>
        /// <param name="entrys">一个entry表示一个数据库变更</param>
        private void ProcessBatchMessage(List<Entry> entrys)
        {
            foreach (var entry in entrys)
            {
                if (entry.EntryType == EntryType.Transactionbegin || entry.EntryType == EntryType.Transactionend)
                {
                    continue;
                }
                try
                {
                    //获取行变更
                    RowChange rowChange = RowChange.Parser.ParseFrom(entry.StoreValue);
                    if (rowChange != null)
                    {
                        
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.StackTrace + ex.Message);
                    throw new Exception($"binlog回调异常#{ex.ToString()}#{ex.StackTrace}");
                }
            }
        }
    }
}

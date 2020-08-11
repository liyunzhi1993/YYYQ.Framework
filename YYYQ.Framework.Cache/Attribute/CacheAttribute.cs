using AspectCore.DynamicProxy;
using YYYQ.Framework.Config;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace YYYQ.Framework.Cache
{
    [AttributeUsage(AttributeTargets.Method)]
    public class CacheAttribute: AbstractInterceptorAttribute
    {
        private ICache _cache;
        private IConfig _config;
        /// <summary>
        /// 缓存模式
        /// </summary>
        public CacheModeEnum CacheMode { get; set; }

        /// <summary>
        /// 返回类型是否是Task模式
        /// </summary>
        public bool IsTask { get; set; } = true;

        /// <summary>
        /// 过期时间（秒）【低一致模式需要此字段】
        /// </summary>
        public int Expire { get; set; }
        public Type ReturnType { get; set; }

        public async override Task Invoke(AspectContext context, AspectDelegate next)
        {
            try
            {
                _cache = (ICache)context.ServiceProvider.GetService(typeof(ICache));
                _config = (IConfig)context.ServiceProvider.GetService(typeof(IConfig));
                //获取参数列表
                var parameters = context.ImplementationMethod.GetParameters();
                ProccessCacheKeyInfoModel proccessCacheKeyInfoModel = new ProccessCacheKeyInfoModel();
                List<string> cacheKeyList = new List<string>();
#if DEBUG
                //返回类型必须要有
                if (ReturnType == null)
                {
                    throw new Exception("开发注意！返回类型必须要有！请参考文档！");
                }
                //定义基本参数类型，只处理基本参数类型
                List<Type> baseTypeList = new List<Type> {
                    typeof(string),typeof(int),typeof(double),
                    typeof(long),typeof(float),typeof(decimal),
                    typeof(bool),typeof(char)
                };
#endif
                for (int i = 0; i < parameters.Length; i++)
                {
#if DEBUG
                    if (!baseTypeList.Contains(parameters[i].ParameterType) && CacheMode == CacheModeEnum.High)
                    {
                        throw new Exception("开发注意！高一致性模式下，只支持基础类型的参数，请参考文档！");
                    }
#endif
                    cacheKeyList.Add(parameters[i].Name.ToUpper());
                }
                if (cacheKeyList.Count > 0)
                {
                    var classType = context.Implementation.GetType();
                    var cacheKeyName = (classType.FullName.Replace(".", ":")+":" + context.ImplementationMethod.Name) + ":";
                    switch (CacheMode)
                    {
                        case CacheModeEnum.Low:
                            break;
                        case CacheModeEnum.High:
                            Attribute attributeDb = classType.GetCustomAttribute(typeof(CacheDataBaseAttribute));
                            Attribute attributeTable = classType.GetCustomAttribute(typeof(CacheTableAttribute));
#if DEBUG
                            if (attributeDb == null || attributeTable == null)
                            {
                                throw new Exception("开发注意！请在Dao的Impl实现类里打上DataBase和Table的标签，请参考文档！");
                            }
#endif
                            proccessCacheKeyInfoModel.DbName = _config.Get(attributeDb.GetType().GetProperty("ConfigName").GetValue(attributeDb).ToString()).ToUpper();
                            proccessCacheKeyInfoModel.TableName = attributeTable.GetType().GetProperty("Name").GetValue(attributeTable).ToString().ToUpper();
                            proccessCacheKeyInfoModel.CacheKeyList = cacheKeyList;

                            //组装缓存Key
                            cacheKeyName = $"{proccessCacheKeyInfoModel.DbName}:{proccessCacheKeyInfoModel.TableName}:" +
                                               $"{string.Join(":", proccessCacheKeyInfoModel.CacheKeyList.ToArray())}:";
                            break;
                        case CacheModeEnum.OnlyCache:
                            break;
                        default:
                            throw new Exception("请设置缓存模式");
                    }

                   
                    var cacheKeyValueList = new List<string>();
                    var contextCacheKeyList = context.Parameters;
                    foreach (var cacheKeyP in contextCacheKeyList)
                    {
                        if (cacheKeyP == null)
                        {
                            break;
                        }
                        else
                        {
                            cacheKeyValueList.Add(cacheKeyP.ToString().ToUpper());
                        }
                    }
                    cacheKeyName += string.Join(":", cacheKeyValueList.ToArray());
                    var cacheValue = _cache.Get<string>(cacheKeyName);
                    if (cacheValue == null)
                    {
                        await context.Invoke(next);
                        if (context.ReturnValue != null)
                        {
                            object returnValue = null;
                            if (IsTask)
                            {
                                returnValue = context.ReturnValue.GetType().GetProperty("Result").GetValue(context.ReturnValue);
                            }
                            else
                            {
                                returnValue = context.ReturnValue;
                            }
                            switch (CacheMode)
                            {
                                case CacheModeEnum.Low:
#if DEBUG
                                    if (Expire == 0)
                                    {
                                        throw new Exception("开发注意！低一致性模式下，请设置缓存的过期时间，请参考文档！");
                                    }
#endif
                                    await _cache.InsertAsync(cacheKeyName, JsonConvert.SerializeObject(returnValue), Expire);
                                    break;
                                case CacheModeEnum.High:
                                    //高一致性保持24小时
                                    await _cache.InsertAsync(cacheKeyName, JsonConvert.SerializeObject(returnValue),60*60*24);
                                    break;
                                case CacheModeEnum.OnlyCache:
                                    await _cache.InsertAsync(cacheKeyName, JsonConvert.SerializeObject(returnValue));
                                    break;
                                default:
                                    throw new Exception("请设置缓存模式");
                            }
                        }
                    }
                    else
                    {
                        dynamic returnValue = JsonConvert.DeserializeObject(cacheValue.ToString(), ReturnType);
                        if (IsTask)
                        {
                            context.ReturnValue = Task.FromResult(returnValue);
                        }
                        else
                        {
                            context.ReturnValue = returnValue;
                        }
                    }
                }
            }
            catch (Exception)
            {
                if (context.ReturnValue == null)
                {
                    await context.Invoke(next);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace YYYQ.Framework.Cache
{
    /// <summary>
    /// 缓存
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// 获得指定键的缓存值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        T Get<T>(string cacheKey);
        /// <summary>
        /// 获得指定键的缓存值【异步】
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        Task<T> GetAsync<T>(string cacheKey);
        /// <summary>
        /// 从缓存中移除指定键的缓存值【异步】
        /// </summary>
        /// <param name="cacheKey">缓存键</param>
        Task RemoveAsync(string cacheKey);
        /// <summary>
        /// 将指定键的对象添加到缓存中，并指定过期时间【秒单位】【异步】
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="cacheKey"></param>
        /// <param name="cacheValue"></param>
        /// <param name="expiration"></param>
        /// <returns></returns>
        Task InsertAsync<T>(string cacheKey, T cacheValue, int expiration=-1);
        /// <summary>
        /// 判断cacheKey是否存在
        /// </summary>
        /// <param name="cacheKey"></param>
        /// <returns></returns>
        bool Exists(string cacheKey);
    }   
}

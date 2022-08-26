/**
 * 资源加载器接口
 */

using UnityEngine;
using System.Collections.Generic;

namespace GameUnityFramework.Resource
{
    internal class BaseResourceLoader
    {
        /// <summary>
        /// 异步请求队列
        /// </summary>
        protected Queue<AsyncLoadRequest> _asyncLoadRequestQueue = new Queue<AsyncLoadRequest>();
        /// <summary>
        /// 缓存资源
        /// </summary>
        protected Dictionary<string, Object> _resourceCache = new Dictionary<string, Object>();

        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public virtual T SyncLoad<T>(string path) where T : Object { return null; }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        public virtual void AsyncLoad(string path, System.Action<Object> callback) { }

        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="path"></param>
        public virtual void Unload(string path) { }

        /// <summary>
        /// 判断资源路径是否存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public virtual bool Exists(string path) { return false; }

        /// <summary>
        /// Update
        /// </summary>
        public virtual void Update() { }

        /// <summary>
        /// 清理缓存资源
        /// </summary>
        /// <param name="path">为空表示清理所有缓存资源</param>
        /// <returns></returns>
        protected void ClearCacheResource(string path = "")
        {
            if (string.IsNullOrEmpty(path))
            {
                _resourceCache.Clear();
                return;
            }
            if (_resourceCache.ContainsKey(path))
            {
                _resourceCache.Remove(path);
            }
        }

        /// <summary>
        /// 缓存资源
        /// </summary>
        protected void CacheResource(string path, Object obj)
        {
            if (obj == null) return;

            if (_resourceCache.ContainsKey(path))
            {
                _resourceCache[path] = obj;
            }
            else
            {
                _resourceCache.Add(path, obj);
            }
        }
    }
}
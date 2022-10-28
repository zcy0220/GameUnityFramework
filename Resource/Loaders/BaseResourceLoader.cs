/**
 * 资源加载器接口
 */

using UnityEngine;
using System.Collections.Generic;
using GameBaseFramework.Base;

namespace GameUnityFramework.Resource
{
    internal class BaseResourceLoader
    {
        /// <summary>
        /// 异步请求队列
        /// </summary>
        protected List<AsyncLoadRequest> _asyncLoadRequestList = new List<AsyncLoadRequest>();
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
        public virtual void AsyncLoad(string path, System.Action<Object> callback)
        {
            if (!Exists(path))
            {
                Debuger.LogError($"not find resource path: {path}");
                return;
            }

            if (_resourceCache.TryGetValue(path, out var obj))
            {
                if (obj != null)
                {
                    callback.Invoke(obj);
                    return;
                }
            }

            for (var i = 0; i < _asyncLoadRequestList.Count; i++)
            {
                var request = _asyncLoadRequestList[i];
                if (request.Path.Equals(path, System.StringComparison.OrdinalIgnoreCase))
                {
                    request.Count++;
                    request.Callback += callback;
                    return;
                }
            }

            var asyncRequest = new AsyncLoadRequest();
            asyncRequest.Path = path;
            asyncRequest.Callback = callback;
            asyncRequest.Callback += (obj) => { CacheResource(path, obj); };
            _asyncLoadRequestList.Add(asyncRequest);
        }

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
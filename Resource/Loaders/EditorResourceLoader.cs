/**
 * 编辑模式下资源加载器
 */

using System.IO;
using UnityEngine;
using GameUnityFramework.Log;

namespace GameUnityFramework.Resource
{
    internal class EditorResourceLoader : BaseResourceLoader
    {
        /// <summary>
        /// 编辑器模式下用AssetDatabase.LoadAssetAtPath同步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public override T SyncLoad<T>(string path)
        {
            if (!Exists(path))
            {
                Debuger.LogError($"not find resource path: {path}");
                return null;
            }

            if (_resourceCache.TryGetValue(path, out var obj))
            {
                if (obj != null)
                {
                    return obj as T;
                }
            }

#if UNITY_EDITOR
            obj = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            CacheResource(path, obj);
            return obj as T;
#else
            return null;
#endif
        }

        /// <summary>
        /// AssetDatabase.LoadAssetAtPath没有异步加载
        /// 采用队列隔帧加载模拟
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        public override void AsyncLoad(string path, System.Action<Object> callback)
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
            
            foreach (var request in _asyncLoadRequestQueue)
            {
                if (request.Path.Equals(path, System.StringComparison.OrdinalIgnoreCase))
                {
                    request.Callback += callback;
                    return;
                }
            }

            _asyncLoadRequestQueue.Enqueue(new AsyncLoadRequest() { Path = path, Callback = callback });
        }

        /// <summary>
        /// Update检测异步加载请求
        /// </summary>
        public override void Update()
        {
#if UNITY_EDITOR
            if (_asyncLoadRequestQueue.Count > 0)
            {
                Debug.LogError("_asyncLoadRequestQueue");
                var request = _asyncLoadRequestQueue.Dequeue();
                var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(request.Path);
                CacheResource(request.Path, obj);
                request.Callback(obj);
                return;
            }
#endif
        }

        /// <summary>
        /// 编辑器模式下没有卸载指定路径资源的方法
        /// </summary>
        /// <param name="path"></param>
        public override void Unload(string path)
        {
            if (_resourceCache.TryGetValue(path, out var obj))
            {
                if (obj != null)
                {
                    Resources.UnloadAsset(obj);
                }
                _resourceCache.Remove(path);
            }
        }

        /// <summary>
        /// 判断资源路径是否存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public override bool Exists(string path)
        {
            var prefix = "Assets";
            return File.Exists(Path.Combine(Application.dataPath, path.Substring(prefix.Length + 1)));
        }
    }
}

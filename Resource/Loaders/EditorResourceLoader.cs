/**
 * 编辑模式下资源加载器
 */

using System.IO;
using UnityEngine;
using GameBaseFramework.Base;

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
        /// Update检测异步加载请求
        /// </summary>
        public override void Update()
        {
#if UNITY_EDITOR
            if (_asyncLoadRequestList.Count > 0)
            {
                var request = _asyncLoadRequestList[0];
                _asyncLoadRequestList.RemoveAt(0);
                var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(request.Path);
                request.Callback?.Invoke(obj);
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

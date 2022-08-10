/**
 * 编辑模式下资源加载器
 */

using UnityEngine;
using GameUnityFramework.Log;

namespace GameUnityFramework.Resource
{
    public class EditorResourceLoader : IResourceLoader
    {
        /// <summary>
        /// 编辑器模式下用AssetDatabase.LoadAssetAtPath同步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public T SyncLoad<T>(string path) where T : Object
        {
            if (!Exists(path))
            {
                Debuger.LogError($"not find resource path: {path}");
                return null;
            }

#if UNITY_EDITOR
            var obj = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            if (obj == null)
            {
                Debuger.LogError($"syncload resource failed: {path}");
                return null;
            }
            return obj;
#endif
        }

        /// <summary>
        /// AssetDatabase.LoadAssetAtPath没有异步加载
        /// 采用队列隔帧加载模拟
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        public void AsyncLoad(string path, System.Action<Object> callback)
        {
            if (!Exists(path))
            {
                Debuger.LogError($"not find resource path: {path}");
                return;
            }
        }

        public void Unload(string path)
        {
        }

        public bool Exists(string path)
        {
            return false;
        }
    }
}

/**
 * AssetBundle资源加载器
 */

using UnityEngine;
using GameUnityFramework.Log;
using System.IO;

namespace GameUnityFramework.Resource
{
    internal class AssetBundleUnit
    {
        public int RefCount;
        public AssetBundle AssetBundle;
    }

    internal class AssetBundleLoader : BaseResourceLoader
    {
        /// <summary>
        /// AssetBundle同步加载
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

            //obj = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
            //CacheResource(path, obj);
            return obj as T;
        }

        /// <summary>
        /// 判断资源路径是否存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public override bool Exists(string path)
        {
            //return base.Exists(path);
            return false;
        }
    }
}

/**
 * 资源加载器接口
 */

using UnityEngine;

namespace GameUnityFramework.Resource
{
    public interface IResourceLoader
    {
        /// <summary>
        /// 同步加载资源
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public T SyncLoad<T>(string path) where T : Object;

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        public void AsyncLoad(string path, System.Action<Object> callback);

        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="path"></param>
        public void Unload(string path);

        /// <summary>
        /// 判断资源路径是否存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public bool Exists(string path);
    }
}
/**
 * 异步加载请求
 */

using System;

namespace GameUnityFramework.Resource
{
    internal class AsyncLoadRequest
    {
        /// <summary>
        /// 资源路径
        /// </summary>
        public string Path;
        /// <summary>
        /// 加载完整回调
        /// </summary>
        public Action<UnityEngine.Object> Callback;
    }
}

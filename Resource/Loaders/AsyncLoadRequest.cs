/**
 * 异步加载请求
 */

using System;

namespace GameUnityFramework.Resource
{
    internal enum ERequestState
    {
        None,
        Ready,
        Loading,
        Done
    }

    internal class AsyncLoadRequest
    {
        /// <summary>
        /// 资源路径
        /// </summary>
        public string Path;
        /// <summary>
        /// 请求数量
        /// </summary>
        public int Count = 1;
        /// <summary>
        /// 加载完整回调
        /// </summary>
        public Action<UnityEngine.Object> Callback;
        /// <summary>
        /// 状态
        /// </summary>
        public ERequestState State = ERequestState.None;
    }
}

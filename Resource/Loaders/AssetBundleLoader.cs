/**
 * AssetBundle资源加载器
 */

using UnityEngine;
using GameUnityFramework.Log;
using System.IO;

namespace GameUnityFramework.Resource
{
    internal class AssetBundleLoader : BaseResourceLoader
    {
        /// <summary>
        /// 构造绑定MonoBehaviour
        /// </summary>
        /// <param name="mono"></param>
        public AssetBundleLoader(MonoBehaviour mono) : base(mono) { }
    }
}

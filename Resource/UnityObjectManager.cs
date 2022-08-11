/**
 * UnityObjectManager
 */

using UnityEngine;
using GameBaseFramework.Patterns;
using GameUnityFramework.Log;
using GameUnityFramework.Patterns;

namespace GameUnityFramework.Resource
{
    public class UnityObjectManager : Singleton<UnityObjectManager>
    {
        /// <summary>
        /// 资源加载器
        /// </summary>
        private BaseResourceLoader _resourceLoader;
        
        /// <summary>
        /// 初始化资源加载器
        /// </summary>
        public void Init(MonoBehaviour mono)
        {
#if UNITY_EDITOR
            _resourceLoader = new EditorResourceLoader(mono);
#else
            _resourceLoader = new AssetBundleLoader(mono);
#endif
        }

        /// <summary>
        /// Update
        /// </summary>
        public void Update()
        {
            if (_resourceLoader != null)
            {
                _resourceLoader.Update();
            }
        }

        #region 同步加载实例化对象资源
        public GameObject SyncGameObjectInstantiate(string path)
        {
            var orginal = _resourceLoader.SyncLoad<GameObject>(path);
            if (orginal != null)
            {
                var obj = GameObject.Instantiate(orginal);
                return obj;
            }
            return null;
        }
        #endregion
    }
}

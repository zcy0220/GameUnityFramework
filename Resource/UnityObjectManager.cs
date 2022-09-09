﻿/**
 * UnityObjectManager
 */

using UnityEngine;
using GameUnityFramework.Log;

namespace GameUnityFramework.Resource
{
    public class UnityObjectManager
    {
        /// <summary>
        /// 资源加载器
        /// </summary>
        private BaseResourceLoader _resourceLoader;
        
        /// <summary>
        /// 初始化资源加载器
        /// </summary>
        public UnityObjectManager()
        {
#if UNITY_EDITOR
            _resourceLoader = new EditorResourceLoader();
#else
            _resourceLoader = new AssetBundleLoader();
#endif
        }

        #region 同步加载实例化对象资源
        public GameObject SyncGameObjectInstantiate(string path)
        {
            path = ResourcePathHelper.GetFullResourcePath(path);
            var orginal = _resourceLoader.SyncLoad<GameObject>(path);
            if (orginal != null)
            {
                var obj = GameObject.Instantiate(orginal);
                return obj;
            }
            else
            {
                Debuger.LogError($"syncload resource failed: {path}");
                return null;
            }
        }
        #endregion

        #region 异步加载实例化对象资源
        public void AsyncGameObjectInstantiate(string path, System.Action<GameObject> callback)
        {
            path = ResourcePathHelper.GetFullResourcePath(path);
            _resourceLoader.AsyncLoad(path, (orginal) =>
            {
                if (orginal != null)
                {
                    var obj = GameObject.Instantiate(orginal) as GameObject;
                    callback(obj);
                }
                else
                {
                    Debuger.LogError($"asyncload resource failed: {path}");
                }
            });
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
        #endregion
    }
}

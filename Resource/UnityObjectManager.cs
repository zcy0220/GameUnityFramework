/**
 * UnityObjectManager
 */

using UnityEngine;
using GameUnityFramework.Log;
using System.Collections.Generic;
using UnityEngine.UI;

namespace GameUnityFramework.Resource
{
    public class UnityObjectManager
    {
        /// <summary>
        /// 定时扫描检测空资源
        /// </summary>
        private const float CheckResourceRefTime = 30.0f;
        /// <summary>
        /// 资源加载器
        /// </summary>
        private BaseResourceLoader _resourceLoader;
        /// <summary>
        /// 资源引用列表映射
        /// </summary>
        private Dictionary<string, HashSet<Object>> _resourceRefDict = new Dictionary<string, HashSet<Object>>();
        /// <summary>
        /// 需要清除null的列表
        /// </summary>
        private List<Object> _needClearNullRefList = new List<Object>();
        /// <summary>
        /// 剩余时间
        /// </summary>
        private float _leftTime = CheckResourceRefTime;
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

        /// <summary>
        /// Update
        /// </summary>
        public void Update()
        {
            if (_resourceLoader != null)
            {
                _resourceLoader.Update();
            }

            CheckResourceRefDict();
        }

        /// <summary>
        /// 检测_resourceRefDict引用列表中为空的资源
        /// </summary>
        private void CheckResourceRefDict()
        {
            _leftTime -= Time.deltaTime;
            if (_leftTime <= 0)
            {
                Debug.Log("unity object manager ==> check resource ref");
                _leftTime = CheckResourceRefTime;
                foreach(var item in _resourceRefDict)
                {
                    foreach(var obj in item.Value)
                    {
                        if (obj == null)
                        {
                            _needClearNullRefList.Add(obj);
                        }
                    }
                    for(var i = 0; i < _needClearNullRefList.Count; i++)
                    {
                        item.Value.Remove(_needClearNullRefList[i]);
                    }
                    _needClearNullRefList.Clear();
                }
            }
        }

        /// <summary>
        /// 增加引用
        /// </summary>
        /// <param name="path"></param>
        /// <param name="obj"></param>
        private void AddResourceRef(string path, Object obj)
        {
            if (!_resourceRefDict.TryGetValue(path, out var resourceSet))
            {
                resourceSet = new HashSet<Object>();
            }
            if (!resourceSet.Contains(obj))
            {
                resourceSet.Add(obj);
            }
        }

        /// <summary>
        /// 异步加载资源
        /// 不记录引用，要自己管理资源卸载
        /// </summary>
        /// <param name="path"></param>
        /// <param name="callback"></param>
        public void AsyncLoad(string path, System.Action<Object> callback)
        {
            path = ResourcePathHelper.GetFullResourcePath(path);
            _resourceLoader.AsyncLoad(path, (orginal) =>
            {
                callback?.Invoke(orginal);
            });
        }

        #region Srpite相关
        public void SyncLoadSrpite(string path, Image image)
        {
            path = ResourcePathHelper.GetFullResourcePath(path);
            var sprite = _resourceLoader.SyncLoad<Sprite>(path);
            if (sprite != null)
            {
                AddResourceRef(path, image);
                image.sprite = sprite;
            }
        }
        #endregion

        #region 同步加载实例化对象资源
        public GameObject SyncGameObjectInstantiate(string path)
        {
            path = ResourcePathHelper.GetFullResourcePath(path);
            var orginal = _resourceLoader.SyncLoad<GameObject>(path);
            if (orginal != null)
            {
                var obj = GameObject.Instantiate(orginal);
                AddResourceRef(path, obj);
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
                    AddResourceRef(path, obj);
                    callback(obj);
                }
                else
                {
                    Debuger.LogError($"asyncload resource failed: {path}");
                }
            });
        }
        #endregion
    }
}

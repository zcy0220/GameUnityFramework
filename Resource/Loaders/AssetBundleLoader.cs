/**
 * AssetBundle资源加载器
 */

using UnityEngine;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using GameUnityFramework.Log;

namespace GameUnityFramework.Resource
{
    [System.Serializable]
    public class PackConfig
    {
        /// <summary>
        /// AssetPath -> AssetBundleName
        /// </summary>
        public Dictionary<string, string> DependencyDataDict = new Dictionary<string, string>();
        /// <summary>
        /// 把文件夹名和文件名映射成它对应的位置索引
        /// 减少存储量
        /// </summary>
        public List<string> AllDirectoryAndFileNames = new List<string>();
    }

    internal class AssetBundleUnit
    {
        /// <summary>
        /// 引用次数
        /// </summary>
        public int RefCount;
        /// <summary>
        /// AssetBundle
        /// </summary>
        public AssetBundle AssetBundle;
    }

    /// <summary>
    /// 资源加载的协程
    /// </summary>
    internal class LoaderCoroutine : MonoBehaviour { }

    internal class AssetBundleLoader : BaseResourceLoader
    {
        /// <summary>
        /// AssetBundleManifest
        /// </summary>
        private AssetBundleManifest _assetBundleManifest;
        /// <summary>
        /// 资源和AssetBundle映射
        /// 资源路径转为全小写存放
        /// 因为Windows对路径大小写不敏感，其他平台又是严格大小写的
        /// </summary>
        private Dictionary<string, string> _packConfigDict = new Dictionary<string, string>();
        /// <summary>
        /// AssetBunldeName -> AssetBundleUnit
        /// </summary>
        private Dictionary<string, AssetBundleUnit> _assetBundleUnitDict = new Dictionary<string, AssetBundleUnit>();
        /// <summary>
        /// 强制保留永不卸载的AssetBundle(一般就是跟shader相关的AssetBundle)
        /// </summary>
        private HashSet<string> _noUnloadAssetBundleHashSet = new HashSet<string>();
        /// <summary>
        /// 需要删除的异步请求列表
        /// </summary>
        private List<int> _removeAsyncLoadRequestList = new List<int>();
        /// <summary>
        /// 资源加载协程
        /// </summary>
        private LoaderCoroutine _loaderCoroutine;
        /// <summary>
        /// 异步请求AssetBundle队列
        /// </summary>
        protected List<AsyncLoadRequest> _asyncLoadAssetBundleRequestList = new List<AsyncLoadRequest>();

        /// <summary>
        /// 
        /// </summary>
        public AssetBundleLoader()
        {
            var manifestBundleName = ResourcePathHelper.AssetBundlesFolder;
            var manifestBundlePath = ResourcePathHelper.GetLocalAssetBundlePath(manifestBundleName);
            var manifestAssetBundle = AssetBundle.LoadFromFile(manifestBundlePath);
            _assetBundleManifest = manifestAssetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
            var packConfigPath = ResourcePathHelper.PackConfigPath;
            var packConfigAssetBundleName = ResourcePathHelper.GetAssetBundleName(packConfigPath);
            var packConfigAssetBundlePath = ResourcePathHelper.GetLocalAssetBundlePath(packConfigAssetBundleName);
            var packConfigAssetBundle = AssetBundle.LoadFromFile(packConfigAssetBundlePath);
            var configTextAsset = packConfigAssetBundle.LoadAsset<TextAsset>(packConfigPath);
            var packConfig = JsonConvert.DeserializeObject<PackConfig>(configTextAsset.text);
            foreach(var item in packConfig.DependencyDataDict)
            {
                var realFilePath = "";
                var indexs = item.Key.Split("/");
                for (int i = 0; i < indexs.Length; i++)
                {
                    var index = int.Parse(indexs[i]);
                    var name = packConfig.AllDirectoryAndFileNames[index];
                    realFilePath += index == 0 ? name : "/" + name;
                }
                _packConfigDict.Add(realFilePath.ToLower(), item.Value);
                if (realFilePath.EndsWith(".shader"))
                {
                    _noUnloadAssetBundleHashSet.Add(item.Key);
                }
            }
            _loaderCoroutine = new GameObject("AssetLoaderCoroutine").AddComponent<LoaderCoroutine>();
            GameObject.DontDestroyOnLoad(_loaderCoroutine.gameObject);
        }

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

            var assetBundleName = _packConfigDict[path.ToLower()];
            var dependencies = _assetBundleManifest.GetAllDependencies(assetBundleName);
            foreach (var depend in dependencies)
            {
                SyncLoadAssetBundle(depend);
            }

            var assetBundle = SyncLoadAssetBundle(assetBundleName);
            obj = assetBundle.LoadAsset<T>(path);
            CacheResource(path, obj);
            return obj as T;
        }

        /// <summary>
        /// 判断资源路径是否存在
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public override bool Exists(string path)
        {
            return _packConfigDict.ContainsKey(path.ToLower());
        }

        /// <summary>
        /// 根据AB包名同步加载AssetBundle
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <returns></returns>
        private AssetBundle SyncLoadAssetBundle(string assetBundleName)
        {
            if (_assetBundleUnitDict.TryGetValue(assetBundleName, out var assetBundleUnit))
            {
                assetBundleUnit.RefCount++;
            }
            else
            {
                var path = ResourcePathHelper.GetLocalAssetBundlePath(assetBundleName);
                var assetBundle = AssetBundle.LoadFromFile(path);
                if (assetBundle == null)
                {
                    Debuger.LogError($"load assetbundle error: {assetBundleName}");
                    return null;
                }
                assetBundleUnit = new AssetBundleUnit();
                assetBundleUnit.AssetBundle = assetBundle;
                assetBundleUnit.RefCount = 1;
                _assetBundleUnitDict.Add(assetBundleName, assetBundleUnit);
            }
            return assetBundleUnit.AssetBundle;
        }

        /// <summary>
        /// 卸载AssetBundle
        /// </summary>
        /// <param name="assetBundleName">AB包名</param>
        private void UnloadAssetBundle(string assetBundleName)
        {
            if (_assetBundleUnitDict.ContainsKey(assetBundleName))
            {
                var unit = _assetBundleUnitDict[assetBundleName];
                unit.RefCount--;
                Debuger.Log($"unload assetbundle:{assetBundleName}, refcount:{unit.RefCount}");
                if (!_noUnloadAssetBundleHashSet.Contains(assetBundleName) && unit.RefCount <= 0)
                {
                    Debuger.Log($"real unload assetbundle:{assetBundleName}", "cyan");
                    unit.AssetBundle?.Unload(true);
                    _assetBundleUnitDict.Remove(assetBundleName);
                }
            }
        }

        /// <summary>
        /// 卸载资源
        /// </summary>
        /// <param name="path"></param>
        public override void Unload(string path)
        {
            if (_resourceCache.ContainsKey(path))
            {
                _resourceCache.Remove(path);
            }

            if (Exists(path))
            {
                var assetBundleName = _packConfigDict[path];
                var dependencies = _assetBundleManifest.GetAllDependencies(assetBundleName);
                UnloadAssetBundle(assetBundleName);
                foreach (var depend in dependencies)
                {
                    UnloadAssetBundle(depend);
                }
            }
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private IEnumerator AsyncLoadAsset(AsyncLoadRequest request)
        {
            request.State = ERequestState.Loading;
            var assetBundleName = _packConfigDict[request.Path.ToLower()];
            if (_assetBundleUnitDict.TryGetValue(assetBundleName, out var assetBundleUnit))
            {
                if (assetBundleUnit.AssetBundle.isStreamedSceneAssetBundle)
                {
                    request.State = ERequestState.Done;
                    request.Callback?.Invoke(null);
                }
                else
                {
                    var abRequest = assetBundleUnit.AssetBundle.LoadAssetAsync(request.Path);
                    yield return abRequest;
                    request.State = ERequestState.Done;
                    request.Callback?.Invoke(abRequest.asset);
                }
            }
        }

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private IEnumerator AsyncLoadAssetBundle(AsyncLoadRequest request)
        {
            request.State = ERequestState.Loading;
            var assetBundleName = request.Path;
            var path = ResourcePathHelper.GetLocalAssetBundlePath(assetBundleName);
            var abCreateRequest = AssetBundle.LoadFromFileAsync(path);
            yield return abCreateRequest;
            var assetBundle = abCreateRequest.assetBundle;
            if (assetBundle != null)
            {
                var assetBundleUnit = new AssetBundleUnit();
                assetBundleUnit.AssetBundle = assetBundle;
                assetBundleUnit.RefCount = request.Count;
                _assetBundleUnitDict.Add(assetBundleName, assetBundleUnit);
            }
            request.State = ERequestState.Done;
        }

        /// <summary>
        /// 检查对应资源路径对应的所有依赖包是否都加载完了
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public void CheckAllDependenciesLoaded(AsyncLoadRequest request)
        {
            var isAllLoaded = true;
            var path = request.Path;
            var assetBundleName = _packConfigDict[path.ToLower()];
            if (!_assetBundleUnitDict.ContainsKey(assetBundleName))
            {
                AsyncLoadAssetBundle(assetBundleName);
                isAllLoaded = false;
            }
            var dependencies = _assetBundleManifest.GetAllDependencies(assetBundleName);
            foreach (var depend in dependencies)
            {
                if (!_assetBundleUnitDict.ContainsKey(depend))
                {
                    AsyncLoadAssetBundle(depend);
                    isAllLoaded = false;
                }
            }
            if (isAllLoaded)
            {
                request.State = ERequestState.Ready;
            }
        }

        /// <summary>
        /// 添加异步加载AssetBundle请求到队列
        /// </summary>
        /// <param name="assetBundleName"></param>
        private void AsyncLoadAssetBundle(string assetBundleName)
        {
            for(var i = 0; i < _asyncLoadAssetBundleRequestList.Count; i++)
            {
                var request = _asyncLoadAssetBundleRequestList[i];
                if (request.Path == assetBundleName)
                {
                    request.Count++;
                    return;
                }
            }
            var assetBundleRequest = new AsyncLoadRequest();
            assetBundleRequest.State = ERequestState.Ready;
            assetBundleRequest.Path = assetBundleName;
            _asyncLoadAssetBundleRequestList.Add(assetBundleRequest);
        }

        /// <summary>
        /// Update检测异步加载请求
        /// </summary>
        public override void Update()
        {
            UpdateAsyncLoadRequest();
            UpdateAsyncLoadAssetBundleRequest();
        }

        /// <summary>
        /// 处理更新异步资源请求
        /// </summary>
        private void UpdateAsyncLoadRequest()
        {
            for (int i = 0; i < _asyncLoadRequestList.Count; i++)
            {
                var request = _asyncLoadRequestList[i];
                if (request.State == ERequestState.None)
                {
                    CheckAllDependenciesLoaded(request);
                }
                if (request.State == ERequestState.Ready)
                {
                    _loaderCoroutine.StartCoroutine(AsyncLoadAsset(request));
                }
                if (request.State == ERequestState.Done)
                {
                    _removeAsyncLoadRequestList.Add(i);
                }
            }

            if (_removeAsyncLoadRequestList.Count > 0)
            {
                for (int i = _removeAsyncLoadRequestList.Count - 1; i >= 0; i--)
                {
                    var index = _removeAsyncLoadRequestList[i];
                    _asyncLoadRequestList.RemoveAt(index);
                }
                _removeAsyncLoadRequestList.Clear();
            }
        }

        /// <summary>
        /// 处理更新异步AssetBundle请求
        /// </summary>
        private void UpdateAsyncLoadAssetBundleRequest()
        {
            for (int i = 0; i < _asyncLoadAssetBundleRequestList.Count; i++)
            {
                var request = _asyncLoadAssetBundleRequestList[i];
                if (request.State == ERequestState.Ready)
                {
                    _loaderCoroutine.StartCoroutine(AsyncLoadAssetBundle(request));
                }
                if (request.State == ERequestState.Done)
                {
                    _removeAsyncLoadRequestList.Add(i);
                }
            }
            if (_removeAsyncLoadRequestList.Count > 0)
            {
                for (int i = _removeAsyncLoadRequestList.Count - 1; i >= 0; i--)
                {
                    var index = _removeAsyncLoadRequestList[i];
                    _removeAsyncLoadRequestList.RemoveAt(index);
                }
                _removeAsyncLoadRequestList.Clear();
            }
        }
    }
}

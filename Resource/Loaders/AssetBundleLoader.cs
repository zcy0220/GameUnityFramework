/**
 * AssetBundle资源加载器
 */

using UnityEngine;
using System.IO;
using System.Collections.Generic;
using GameUnityFramework.Log;
using Newtonsoft.Json;

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
            }
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
    }
}

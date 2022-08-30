/**
 * 热更新
 */

using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System;

namespace GameUnityFramework.Resource
{
    /// <summary>
    /// 版本控制配置信息
    /// </summary>
    public class VersionConfig
    {
        /// <summary>
        /// 版本号
        /// </summary>
        public string Version;
    }

    /// <summary>
    /// 热更新资源结果
    /// </summary>
    public enum EHotfixResourceResult
    {
        Hotfix,
        NewVersion,
        InitLocalVersionError,
        InitServerVersionError,
    }

    public class HotfixResourceManager
    {
        /// <summary>
        /// VersionConfig的文件路径(相对于Application.streamingAssetsPath)
        /// </summary>
        private const string VersionConfigFile = "version.json";
        /// <summary>
        /// 记录已下载AB的文件路径(相对于Application.persistentDataPath)
        /// </summary>
        private const string CompleteDownloadFile = "completedownload.txt";
        /// <summary>
        /// AssetBundle的导出路径(相对于Application.streamingAssetsPath)
        /// </summary>
        private const string AssetBundlesFolder = "assetbundles";
        //=============================================================
        /// <summary>
        /// 本地版本配置信息
        /// </summary>
        private VersionConfig _localVersionConfig;
        /// <summary>
        /// 服务器版本配置信息
        /// </summary>
        private VersionConfig _serverVersionConfig;
        /// <summary>
        /// 热更新结果
        /// </summary>
        private EHotfixResourceResult _result;
        /// <summary>
        /// 需要总的下载资源数 = 服务器上与本地所有的AB差异数
        /// </summary>
        private int _totalDownloadCount = 0;
        /// <summary>
        /// 需要热更新下载的AssetBundle队列 = _totalDownloadCount - _completeDownloadList
        /// </summary>
        private Queue<string> _needDownloadQueue = new Queue<string>();
        /// <summary>
        /// 已经下载的列表
        /// </summary>
        private HashSet<string> _completeDownloadList;
        /// <summary>
        /// 已下载的资源写入Stream
        /// </summary>
        private StreamWriter _completeDownloadStreamWriter;
        //=============================================================
        /// <summary>
        /// 热更新服务器地址
        /// </summary>
        public string ServerAddress;
        /// <summary>
        /// 回调
        /// </summary>
        public System.Action<EHotfixResourceResult> FinishedCallback;
        //=============================================================

        /// <summary>
        /// 开始检测热更新
        /// </summary>
        public IEnumerator Start()
        {
            yield return InitLocalVersionConfig();
            yield return InitServerVersionConfig();
            yield return CompareVersionConfig();
            FinishedCallback?.Invoke(_result);
        }

        /// <summary>
        /// 初始化本地版本配置信息
        /// </summary>
        private IEnumerator InitLocalVersionConfig()
        {
            var localVersionConfigPath = ResourcePathHelper.GetLocalFilePath(VersionConfigFile);
            var uwr = new UnityWebRequest(localVersionConfigPath);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                _localVersionConfig = JsonUtility.FromJson<VersionConfig>(uwr.downloadHandler.text);
            }
            else
            {
                _result = EHotfixResourceResult.InitLocalVersionError;
            }

            uwr.downloadHandler.Dispose();
            uwr.Dispose();
        }

        /// <summary>
        /// 初始化服务器版本配置信息
        /// </summary>
        private IEnumerator InitServerVersionConfig()
        {
            var versionConfigPath = ServerAddress + VersionConfigFile;
            var uwr = new UnityWebRequest(versionConfigPath);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                _serverVersionConfig = JsonUtility.FromJson<VersionConfig>(uwr.downloadHandler.text);
            }
            else
            {
                _result = EHotfixResourceResult.InitServerVersionError;
            }

            uwr.downloadHandler.Dispose();
            uwr.Dispose();
        }

        /// <summary>
        /// 对比版本，检测资源
        /// </summary>
        /// <returns></returns>
        private IEnumerator CompareVersionConfig()
        {
            if (_localVersionConfig != null && _serverVersionConfig != null)
            {
                var localVersionConfig = _localVersionConfig.Version.Split('.');
                var serverVersionConfig = _serverVersionConfig.Version.Split('.');
                var localVersion0 = int.Parse(localVersionConfig[0]);
                var localVersion1 = int.Parse(localVersionConfig[1]);
                var serverVersion0 = int.Parse(serverVersionConfig[0]);
                var serverVersion1 = int.Parse(serverVersionConfig[1]);
                // 大版本更新
                if (serverVersion0 > localVersion0)
                {
                    _result = EHotfixResourceResult.NewVersion;
                    yield return null;
                }
                // 热更新
                if (serverVersion0 == localVersion0 && serverVersion1 > localVersion1)
                {
                    _result = EHotfixResourceResult.Hotfix;
                    yield return CompareResources();
                }
            }
        }
        
        /// <summary>
        /// 对比资源列表
        /// </summary>
        /// <returns></returns>
        private IEnumerator CompareResources()
        {
            var manifestAssetBundlePath = Path.Combine(AssetBundlesFolder, AssetBundlesFolder);
            /**
             * 本地的AssetBundleManifest
             */
            var localManifestAssetBundlePath = ResourcePathHelper.GetLocalFilePath(manifestAssetBundlePath);
            var localUWR = UnityWebRequestAssetBundle.GetAssetBundle(localManifestAssetBundlePath);
            yield return localUWR.SendWebRequest();
            var localAllAssetBundlesDict = new Dictionary<string, Hash128>();
            if (localUWR.result == UnityWebRequest.Result.Success)
            {
                var localManifestAssetBundle = DownloadHandlerAssetBundle.GetContent(localUWR);
                var localManifest = localManifestAssetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                var localAllAssetBundles = new List<string>(localManifest.GetAllAssetBundles());
                foreach (var abName in localAllAssetBundles)
                {
                    localAllAssetBundlesDict.Add(abName, localManifest.GetAssetBundleHash(abName));
                }
                localManifestAssetBundle.Unload(true);
            }
            localUWR.Dispose();

            /**
             * 服务器的AssetBundleManifest
             */
            var serverManifestAssetBundlePath = ServerAddress + manifestAssetBundlePath;
            var serverUWR = UnityWebRequestAssetBundle.GetAssetBundle(serverManifestAssetBundlePath);
            yield return serverUWR.SendWebRequest();
            if (serverUWR.result == UnityWebRequest.Result.Success)
            {
                var serverManifestAssetBundle = DownloadHandlerAssetBundle.GetContent(serverUWR);
                var serverManifest = serverManifestAssetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                var serverAllAssetBundles = new List<string>(serverManifest.GetAllAssetBundles());
                foreach (var assetBundleName in serverAllAssetBundles)
                {
                    if (localAllAssetBundlesDict.ContainsKey(assetBundleName))
                    {
                        var serverAssetBundleHash = serverManifest.GetAssetBundleHash(assetBundleName);
                        if (localAllAssetBundlesDict[assetBundleName] != serverAssetBundleHash)
                        {
                            AddNeedDownLoadResource(assetBundleName);
                        }
                    }
                    else
                    {
                        AddNeedDownLoadResource(assetBundleName);
                    }
                }
                AddNeedDownLoadResource(AssetBundlesFolder);
                serverManifestAssetBundle.Unload(true);
            }
            serverUWR.Dispose();
        }

        /// <summary>
        /// 添加需要下载的资源
        /// 对比下当前已下载的文件中是否已经下载
        /// </summary>
        private void AddNeedDownLoadResource(string assetBundleName)
        {
            _totalDownloadCount++;
            if (_completeDownloadList == null)
            {
                var completeDownloadFilePath = ResourcePathHelper.GetPresistentDataFilePath(CompleteDownloadFile);
                if (File.Exists(completeDownloadFilePath))
                {
                    try
                    {
                        var fileStream = new StreamReader(completeDownloadFilePath);
                        var str = fileStream.ReadToEnd();
                        if (!string.IsNullOrEmpty(str))
                        {
                            var list = str.Split(',');
                            _completeDownloadList = new HashSet<string>(list);
                        }
                        fileStream.Close();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.Message);
                    }
                }
            }
            if (_completeDownloadList == null || !_completeDownloadList.Contains(assetBundleName))
            {
                _needDownloadQueue.Enqueue(assetBundleName);
            }
        }

        /// <summary>
        /// 下载资源
        /// </summary>
        /// <returns></returns>
        private IEnumerator DownLoadResources()
        {
            while (_needDownloadQueue.Count > 0)
            {
                var assetBundleName = _needDownloadQueue.Dequeue();
                var url = ServerAddress + AssetBundlesFolder + "/" + assetBundleName;
                var uwr = new UnityWebRequest(url);
                uwr.downloadHandler = new DownloadHandlerBuffer();
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    ReplaceLocalResource(assetBundleName, uwr.downloadHandler.data);
                }
                uwr.downloadHandler.Dispose();
                uwr.Dispose();
            }
        }

        /// <summary>
        /// 替换本地的资源
        /// </summary>
        private void ReplaceLocalResource(string assetBundleName, byte[] data)
        {
            if (data == null) return;
            try
            {
                var assetBundlePath = AssetBundlesFolder + "/" + assetBundleName;
                var path = ResourcePathHelper.GetPresistentDataFilePath(assetBundlePath);
                //FileEx.WriteAllBytes(path, data);
                //_needDownloadQueue.Dequeue();
                //if (_completeDownloadStreamWriter == null)
                //{
                //    var completeDownloadFilePath = ResourcePath.GetPresistentDataFilePath(COMPLETEDOWNLOADFILE);
                //    _completeDownloadStreamWriter = new StreamWriter(completeDownloadFilePath);
                //}
                //_completeDownloadStreamWriter.Write(abName + ",");
                //_completeDownloadStreamWriter.Flush();
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }
}

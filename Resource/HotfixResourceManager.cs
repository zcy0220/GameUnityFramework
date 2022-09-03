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
        public string Version;
    }

    /// <summary>
    /// 热更新资源结果
    /// </summary>
    public enum EHotfixResourceStatus
    {
        None,
        NewVersion,
        StartHotfix,
        HotfixSuccess,
        InitLocalVersionError,
        InitServerVersionError,
        DownloadError
    }

    public class DownloadAssetBundleItem
    {
        public string AssetBundleName;
        public UnityWebRequest AssetBundleRequest = new UnityWebRequest();
    }

    public class HotfixResourceManager : MonoBehaviour
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
        private static HotfixResourceManager _self;
        /// <summary>
        /// 本地版本配置信息
        /// </summary>
        private VersionConfig _localVersionConfig;
        /// <summary>
        /// 服务器版本配置信息
        /// </summary>
        private VersionConfig _serverVersionConfig;
        /// <summary>
        /// 当前状态
        /// </summary>
        private EHotfixResourceStatus _status = EHotfixResourceStatus.None;
        /// <summary>
        /// 需要总的下载资源数 = 服务器上与本地所有的AB差异数
        /// </summary>
        private int _totalDownloadCount = 0;
        /// <summary>
        /// 完成下载的数量
        /// </summary>
        private int _completeDownloadCount = 0;
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
        /// <summary>
        /// 对象池
        /// </summary>
        private Queue<DownloadAssetBundleItem> _downloadAssetBundleItemPool = new Queue<DownloadAssetBundleItem>();
        /// <summary>
        /// 正在下载的UnityWebRequest
        /// </summary>
        private List<DownloadAssetBundleItem> _downloadAssetBundleList = new List<DownloadAssetBundleItem>();
        //=============================================================
        /// <summary>
        /// 热更新服务器地址
        /// </summary>
        public string ServerAddress;
        /// <summary>
        /// 状态回调
        /// </summary>
        public System.Action<EHotfixResourceStatus> OnStatusCallback;
        //=============================================================

        /// <summary>
        /// 初始化
        /// </summary>
        public static void Init(string serverAddress, System.Action<EHotfixResourceStatus> onStatusCallback)
        {
            _self = new GameObject("HotfixResourceManager").AddComponent<HotfixResourceManager>();
            _self.ServerAddress = serverAddress;
            _self.OnStatusCallback = onStatusCallback;
            _self.StartCoroutine(_self.Startup());
        }

        /// <summary>
        /// 开始检测热更新
        /// </summary>
        private IEnumerator Startup()
        {
            yield return InitLocalVersionConfig();
            yield return InitServerVersionConfig();
            yield return CompareVersionConfig();
        }

        /// <summary>
        /// 初始化本地版本配置信息
        /// </summary>
        private IEnumerator InitLocalVersionConfig()
        {
            var localVersionConfigPath = ResourcePathHelper.GetLocalFilePath(VersionConfigFile);
            var uwr = new UnityWebRequest();
            uwr.uri = new Uri(localVersionConfigPath);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();
            var result = uwr.result;
            if (result == UnityWebRequest.Result.Success)
            {
                _localVersionConfig = JsonUtility.FromJson<VersionConfig>(uwr.downloadHandler.text);
            }
            else
            {
                GameObject.Destroy(_self.gameObject);
                _status = EHotfixResourceStatus.InitLocalVersionError;
                OnStatusCallback?.Invoke(_status);
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
            var uwr = new UnityWebRequest();
            uwr.uri = new Uri(versionConfigPath);
            uwr.downloadHandler = new DownloadHandlerBuffer();
            yield return uwr.SendWebRequest();
            var result = uwr.result;
            if (result == UnityWebRequest.Result.Success)
            {
                _serverVersionConfig = JsonUtility.FromJson<VersionConfig>(uwr.downloadHandler.text);
            }
            else
            {
                GameObject.Destroy(_self.gameObject);
                _status = EHotfixResourceStatus.InitServerVersionError;
                OnStatusCallback?.Invoke(_status);
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
                    GameObject.Destroy(_self.gameObject);
                    _status = EHotfixResourceStatus.NewVersion;
                    OnStatusCallback?.Invoke(_status);
                    yield return null;
                }
                // 热更新
                if (serverVersion0 == localVersion0 && serverVersion1 > localVersion1)
                {
                    yield return CompareResources();
                    _status = EHotfixResourceStatus.StartHotfix;
                    OnStatusCallback?.Invoke(_status);
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
                if (_totalDownloadCount > 0) AddNeedDownLoadResource(AssetBundlesFolder);
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
        /// 更新配置文件
        /// </summary>
        private void UpdateVersionConfig()
        {
            var path = ResourcePathHelper.GetPresistentDataFilePath(VersionConfigFile);
            var text = JsonUtility.ToJson(_serverVersionConfig);
            WriteAllText(path, text);
        }

        /// <summary>
        /// 替换本地的资源
        /// </summary>
        private void ReplaceLocalResource(string assetBundleName, byte[] data)
        {
            try
            {
                var assetBundlePath = AssetBundlesFolder + "/" + assetBundleName;
                var path = ResourcePathHelper.GetPresistentDataFilePath(assetBundlePath);
                WriteAllBytes(path, data);
                if (_completeDownloadStreamWriter == null)
                {
                    var completeDownloadFilePath = ResourcePathHelper.GetPresistentDataFilePath(CompleteDownloadFile);
                    _completeDownloadStreamWriter = new StreamWriter(completeDownloadFilePath);
                }
                _completeDownloadStreamWriter.Write(assetBundleName + ",");
                _completeDownloadStreamWriter.Flush();

                _completeDownloadCount++;
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
        }

        /// <summary>
        /// 检测文件并创建对应文件夹
        /// </summary>
        public static bool CheckFileAndCreateDirWhenNeeded(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            var fileInfo = new FileInfo(filePath);
            var dirInfo = fileInfo.Directory;
            if (!dirInfo.Exists) Directory.CreateDirectory(dirInfo.FullName);
            return true;
        }

        /// <summary>
        /// WriteAllText.
        /// </summary>
        private bool WriteAllText(string outFile, string outText)
        {
            try
            {
                if (!CheckFileAndCreateDirWhenNeeded(outFile)) return false;
                if (File.Exists(outFile)) File.SetAttributes(outFile, FileAttributes.Normal);
                File.WriteAllText(outFile, outText);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"WriteAllText failed! path = {outFile} with err = {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// WriteAllBytes
        /// </summary>
        private bool WriteAllBytes(string outFile, byte[] outBytes)
        {
            try
            {
                if (!CheckFileAndCreateDirWhenNeeded(outFile)) return false;
                if (File.Exists(outFile)) File.SetAttributes(outFile, FileAttributes.Normal);
                File.WriteAllBytes(outFile, outBytes);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"WriteAllBytes failed! path = {outFile} with err = {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检测下载队列
        /// </summary>
        private void Update()
        {
            while(_needDownloadQueue.Count > 0 && _downloadAssetBundleList.Count < 10)
            {
                var assetBundleName = _needDownloadQueue.Dequeue();
                var downloadAssetBundleItem = _downloadAssetBundleItemPool.Count == 0 ? new DownloadAssetBundleItem() : _downloadAssetBundleItemPool.Dequeue();
                downloadAssetBundleItem.AssetBundleName = assetBundleName;
                var url = ServerAddress + AssetBundlesFolder + "/" + assetBundleName;
                downloadAssetBundleItem.AssetBundleRequest.uri = new Uri(url);
                downloadAssetBundleItem.AssetBundleRequest.downloadHandler = new DownloadHandlerBuffer();
                downloadAssetBundleItem.AssetBundleRequest.SendWebRequest();
                _downloadAssetBundleList.Add(downloadAssetBundleItem);
            }
            for (int i = _downloadAssetBundleList.Count - 1; i >= 0; i--)
            {
                var downloadAssetBundleItem = _downloadAssetBundleList[i];
                var downloadHandler = downloadAssetBundleItem.AssetBundleRequest.downloadHandler;
                if (downloadHandler.isDone)
                {
                    Debug.LogError(downloadAssetBundleItem.AssetBundleName);
                    ReplaceLocalResource(downloadAssetBundleItem.AssetBundleName, downloadAssetBundleItem.AssetBundleRequest.downloadHandler.data);
                    _downloadAssetBundleList.RemoveAt(i);
                }
                else if (!string.IsNullOrWhiteSpace(downloadHandler.error))
                {
                    //todo:下载错误，记录AssetBundle再多下载一次，如果还错误，报DownloadError
                }
            }

            if (_status == EHotfixResourceStatus.StartHotfix)
            {
                if (_completeDownloadCount == _totalDownloadCount)
                {
                    UpdateVersionConfig();
                    GameObject.Destroy(_self.gameObject);
                    _status = EHotfixResourceStatus.HotfixSuccess;
                    OnStatusCallback?.Invoke(_status);
                }
            }
        }
    }
}

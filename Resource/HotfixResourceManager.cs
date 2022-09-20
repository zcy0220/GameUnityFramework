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
    /// AssetBundle相关信息
    /// </summary>
    [System.Serializable]
    public class AssetBundleInfo
    {
        /// <summary>
        /// AssetBundle包名
        /// </summary>
        public string Name;
        /// <summary>
        /// AssetBundle大小(B)
        /// </summary>
        public long Size;
    }

    /// <summary>
    /// AssetBundle版本信息
    /// </summary>
    public class VersionConfig
    {
        /// <summary>
        /// 版本(时间)
        /// </summary>
        public string Version = "0_0";
        /// <summary>
        /// AssetBundleInfoList
        /// </summary>
        public List<AssetBundleInfo> AssetBundleInfoList = new List<AssetBundleInfo>();
    }

    /// <summary>
    /// 热更新资源结果
    /// </summary>
    public enum EHotfixResourceStatus
    {
        None,
        NewVersion,
        StartHotfix,
        EnterGame,
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
        private const string VersionConfigFile = "VersionConfig.json";
        /// <summary>
        /// 记录已下载AB的文件路径(相对于Application.persistentDataPath)
        /// </summary>
        private const string CompleteDownloadFile = "CompleteDownload.txt";
        /// <summary>
        /// AssetBundle的导出路径(相对于Application.streamingAssetsPath)
        /// </summary>
        private string AssetBundlesFolder = ResourcePathHelper.AssetBundlesFolder;
        //=============================================================
        /// <summary>
        /// 热更新服务器地址
        /// </summary>
        private string _serverAddress;
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
        /// 需要总的下载资源大小
        /// </summary>
        private long _totalDownloadSize = 0;
        private long _completeDownloadSize = 0;
        /// <summary>
        /// 需要热更新下载的AssetBundle队列 = _totalDownloadCount - _completeDownloadList
        /// </summary>
        private Queue<string> _needDownloadQueue = new Queue<string>();
        /// <summary>
        /// 正在下载队列数量
        /// </summary>
        private int _loadingAssetBundleCount = 0;
        /// <summary>
        /// 已经下载的列表
        /// </summary>
        private HashSet<string> _completeDownloadList;
        /// <summary>
        /// 已下载的资源写入Stream
        /// </summary>
        private StreamWriter _completeDownloadStreamWriter;
        /// <summary>
        /// AssetBundle信息映射
        /// </summary>
        private Dictionary<string, AssetBundleInfo> _assetBundleInfoDict = new Dictionary<string, AssetBundleInfo>();
        //=============================================================
        /// <summary>
        /// 状态回调
        /// </summary>
        public System.Action<EHotfixResourceStatus> OnStatusCallback { get; set; }
        //=============================================================
        /*
         * 表现层
         */
        public UnityEngine.UI.Text TextProgress;
        public UnityEngine.UI.Slider SliderProgress;
        public UnityEngine.UI.Text TextTips;

        /// <summary>
        /// 设置进度
        /// </summary>
        /// <param name="progress"></param>
        private void SetProgress(long completeDownloadSize, long totalDownloadSize)
        {
            var progress = 1.0f * completeDownloadSize / totalDownloadSize;
            if (TextProgress != null)
            {
                TextProgress.text = $"{Mathf.FloorToInt(progress * 100)}%";
            }
            if (SliderProgress != null)
            {
                SliderProgress.value = progress;
            }
            if (completeDownloadSize == totalDownloadSize)
            {
                TextTips.text = "更新完成";
            }
            else
            {
                var complete = (1.0f * completeDownloadSize / 1024 / 1024).ToString("F2");
                var total = (1.0f * totalDownloadSize / 1024 / 1024).ToString("F2");
                TextTips.text = $"下载资源({complete}M/{total}M)";
            }
        }
        //=============================================================
        /// <summary>
        /// 开始检测热更新
        /// </summary>
        public IEnumerator Startup(string serverAddress)
        {
            _serverAddress = serverAddress;
            yield return InitLocalVersionConfig();
            yield return InitServerVersionConfig();
            yield return CompareVersionConfig();
        }

        /// <summary>
        /// 获取服务器上的资源路径
        /// </summary>
        /// <returns></returns>
        private string GetServerAssetURL(string path)
        {
            return _serverAddress + "/" + path;
        }

        /// <summary>
        /// 初始化本地版本配置信息
        /// </summary>
        private IEnumerator InitLocalVersionConfig()
        {
            var versionConfigPath = ResourcePathHelper.GetLocalFilePath(VersionConfigFile);
            using (var uwr = new UnityWebRequest(versionConfigPath))
            {
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.timeout = 5;
                uwr.disposeDownloadHandlerOnDispose = true;
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    _localVersionConfig = JsonUtility.FromJson<VersionConfig>(uwr.downloadHandler.text);
                }
                else
                {
                    _localVersionConfig = new VersionConfig();
                }
            }
        }

        /// <summary>
        /// 初始化服务器版本配置信息
        /// </summary>
        private IEnumerator InitServerVersionConfig()
        {
            var versionConfigPath = GetServerAssetURL(VersionConfigFile);
            using (var uwr = new UnityWebRequest(versionConfigPath))
            {
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.timeout = 5;
                uwr.disposeDownloadHandlerOnDispose = true;
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    _serverVersionConfig = JsonUtility.FromJson<VersionConfig>(uwr.downloadHandler.text);
                    for(int i = 0; i < _serverVersionConfig.AssetBundleInfoList.Count; i++)
                    {
                        var info = _serverVersionConfig.AssetBundleInfoList[i];
                        _assetBundleInfoDict.Add(info.Name, info);
                        _totalDownloadSize += info.Size;
                    }
                }
                else
                {
                    DestroySelf();
                    _status = EHotfixResourceStatus.InitServerVersionError;
                    OnStatusCallback?.Invoke(_status);
                }
            }
        }

        /// <summary>
        /// 对比版本，检测资源
        /// </summary>
        /// <returns></returns>
        private IEnumerator CompareVersionConfig()
        {
            if (_localVersionConfig != null &&_serverVersionConfig != null)
            {
                var localVersionConfig = _localVersionConfig.Version.Split('_');
                var serverVersionConfig = _serverVersionConfig.Version.Split('_');
                var localVersion0 = int.Parse(localVersionConfig[0]);
                var localVersion1 = int.Parse(localVersionConfig[1]);
                var serverVersion0 = int.Parse(serverVersionConfig[0]);
                var serverVersion1 = int.Parse(serverVersionConfig[1]);
                //// 大版本更新
                //if (serverVersion0 > localVersion0)
                //{
                //    DestroySelf();
                //    _status = EHotfixResourceStatus.NewVersion;
                //    OnStatusCallback?.Invoke(_status);
                //    yield return null;
                //}
                // 热更新
                if (serverVersion0 > localVersion0 && serverVersion1 > localVersion1)
                {
                    yield return CompareResources();
                    _status = EHotfixResourceStatus.StartHotfix;
                    OnStatusCallback?.Invoke(_status);
                }
                else
                {
                    DestroySelf();
                    _status = EHotfixResourceStatus.EnterGame;
                    OnStatusCallback?.Invoke(_status);
                }
            }
            yield return null;
        }
        
        /// <summary>
        /// 对比资源列表
        /// </summary>
        /// <returns></returns>
        private IEnumerator CompareResources()
        {
            var localAllAssetBundlesDict = new Dictionary<string, Hash128>();
            var manifestAssetBundlePath = ResourcePathHelper.PathCombine(AssetBundlesFolder, AssetBundlesFolder);
            /**
             * 本地的AssetBundleManifest
             */
            try
            {
                var localManifestAssetBundlePath = ResourcePathHelper.GetLocalFilePath(manifestAssetBundlePath);
                var localManifestAssetBundle = AssetBundle.LoadFromFile(localManifestAssetBundlePath);
                if (localManifestAssetBundle != null)
                {
                    var localManifest = localManifestAssetBundle.LoadAsset<AssetBundleManifest>("AssetBundleManifest");
                    var localAllAssetBundles = new List<string>(localManifest.GetAllAssetBundles());
                    foreach (var abName in localAllAssetBundles)
                    {
                        localAllAssetBundlesDict.Add(abName, localManifest.GetAssetBundleHash(abName));
                    }
                    localManifestAssetBundle.Unload(true);
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning(e.Message);
            }

            /**
             * 服务器的AssetBundleManifest
             */
            var serverManifestAssetBundlePath = GetServerAssetURL(manifestAssetBundlePath);
            using (var uwr = UnityWebRequestAssetBundle.GetAssetBundle(serverManifestAssetBundlePath))
            {
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    var serverManifestAssetBundle = DownloadHandlerAssetBundle.GetContent(uwr);
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
                    serverManifestAssetBundle.Unload(true);
                    if (_completeDownloadList != null)
                    {
                        foreach(var assetBundleName in _completeDownloadList)
                        {
                            if (_assetBundleInfoDict.ContainsKey(assetBundleName))
                            {
                                var assetBundleInfo = _assetBundleInfoDict[assetBundleName];
                                _completeDownloadSize += assetBundleInfo.Size;
                            }
                        }
                        SetProgress(_completeDownloadSize, _totalDownloadSize);
                    }
                }
            }
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
                    using (var fileStream = new StreamReader(completeDownloadFilePath))
                    {
                        var str = fileStream.ReadToEnd();
                        if (!string.IsNullOrEmpty(str))
                        {
                            _completeDownloadList = new HashSet<string>();
                            var list = str.Split(',');
                            for (int i = 0; i < list.Length; i++)
                            {
                                if (!string.IsNullOrEmpty(list[i]))
                                {
                                    _completeDownloadList.Add(list[i]);
                                }
                            }
                        }
                    }
                }
            }

            if (_completeDownloadList == null) _completeDownloadList = new HashSet<string>();
            
            if (!_completeDownloadList.Contains(assetBundleName))
            {
                _needDownloadQueue.Enqueue(assetBundleName);
            }
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
                    _completeDownloadStreamWriter = new StreamWriter(completeDownloadFilePath, true);
                }
                _completeDownloadStreamWriter.Write(assetBundleName + ",");
                _completeDownloadStreamWriter.Flush();

                if (!_completeDownloadList.Contains(assetBundleName))
                {
                    _completeDownloadList.Add(assetBundleName);
                }

                if (_assetBundleInfoDict.TryGetValue(assetBundleName, out var assetBundleInfo))
                {
                    _completeDownloadSize += assetBundleInfo.Size;
                }

                SetProgress(_completeDownloadSize, _totalDownloadCount);
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
            while (_needDownloadQueue.Count > 0 && _loadingAssetBundleCount < 10)
            {
                _loadingAssetBundleCount++;
                var assetBundleName = _needDownloadQueue.Dequeue();
                StartCoroutine(DownloadAssetBundle(assetBundleName));
            }

            if (_status == EHotfixResourceStatus.StartHotfix)
            {
                if (_completeDownloadList == null || _completeDownloadList.Count == _totalDownloadCount)
                {
                    StartCoroutine(DownloadFinished());
                }
            }
        }

        /// <summary>
        /// 下载AssetBundle
        /// </summary>
        /// <param name="assetBundleName"></param>
        /// <returns></returns>
        private IEnumerator DownloadAssetBundle(string assetBundleName)
        {
            var url = GetServerAssetURL(ResourcePathHelper.PathCombine(AssetBundlesFolder, assetBundleName));
            using (var uwr = new UnityWebRequest(url))
            {
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.timeout = 30;
                uwr.disposeDownloadHandlerOnDispose = true;
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    ReplaceLocalResource(assetBundleName, uwr.downloadHandler.data);
                }
                else
                {
                    //超时或错误重新加入到下载列表中
                    _needDownloadQueue.Enqueue(assetBundleName);
                    Debug.LogError($"assetbundle name:{assetBundleName} {uwr.error}");
                }
                UnityWebRequest.ClearCookieCache();
                _loadingAssetBundleCount--;
            }
        }

        /// <summary>
        /// 资源更新完成
        /// </summary>
        /// <returns></returns>
        private IEnumerator DownloadFinished()
        {
            if (_completeDownloadStreamWriter != null) _completeDownloadStreamWriter.Close();
            File.Delete(ResourcePathHelper.GetPresistentDataFilePath(CompleteDownloadFile));

            var assetBundlePath = ResourcePathHelper.PathCombine(AssetBundlesFolder, AssetBundlesFolder);
            var url = GetServerAssetURL(assetBundlePath);
            using (var uwr = new UnityWebRequest(url))
            {
                uwr.downloadHandler = new DownloadHandlerBuffer();
                uwr.timeout = 30;
                uwr.disposeDownloadHandlerOnDispose = true;
                yield return uwr.SendWebRequest();
                if (uwr.result == UnityWebRequest.Result.Success)
                {
                    var localAssetBundlePath = ResourcePathHelper.GetPresistentDataFilePath(assetBundlePath);
                    WriteAllBytes(localAssetBundlePath, uwr.downloadHandler.data);
                    yield return null;
                    
                    var versionConfigPath = ResourcePathHelper.GetPresistentDataFilePath(VersionConfigFile);
                    var text = JsonUtility.ToJson(_serverVersionConfig);
                    WriteAllText(versionConfigPath, text);

                    SetProgress(_totalDownloadCount, _totalDownloadCount);
                    yield return null;
                    DestroySelf();
                    _status = EHotfixResourceStatus.EnterGame;
                    OnStatusCallback?.Invoke(_status);
                }
            }
        }

        /// <summary>
        /// 销毁
        /// </summary>
        private void DestroySelf()
        {
            GameObject.Destroy(gameObject);
        }
    }
}

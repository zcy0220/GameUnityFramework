/**
 * 热更新
 */

using UnityEngine;
using UnityEngine.Networking;
using GameUnityFramework.Log;
using System.Collections;

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
        Success,
        NewVersion,
        InitLocalVersionError,
        InitServerVersionError
    }

    public class HotfixResourceManager
    {
        /// <summary>
        /// VersionConfig的文件路径(相对于Application.streamingAssetsPath)
        /// </summary>
        private const string VersionConfigFile = "VersionConfig.json";
        /// <summary>
        /// 记录已下载AB的文件路径(相对于Application.persistentDataPath)
        /// </summary>
        private const string CompleteDownloadFile = "CompleteDownload.txt";
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
            yield return CompareVersionConfigAndResources();
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
                Debuger.LogError(uwr.result.ToString());
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
                Debuger.LogError(uwr.result.ToString());
            }

            uwr.downloadHandler.Dispose();
            uwr.Dispose();
        }

        /// <summary>
        /// 对比版本，检测资源
        /// </summary>
        /// <returns></returns>
        private IEnumerator CompareVersionConfigAndResources()
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
                    _result = EHotfixResourceResult.Success;
                }
            }
        }
    }
}

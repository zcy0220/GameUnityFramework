/**
 * 资源路径工具
 */

using System.IO;
using UnityEngine;
using System.Text;

namespace GameUnityFramework.Resource
{
    public class ResourcePathHelper
    {
        /// <summary>
        /// 资源完整路径的前缀
        /// </summary>
        public static string ResourcePathPrefix;
        /// <summary>
        /// AssetBundle存放根目录(相对于Application.streamingAssetsPath)
        /// </summary>
        public static string AssetBundlesFolder;
        /// <summary>
        /// Asset - AssetBundle映射配置路径
        /// </summary>
        public static string PackConfigPath;
        /// <summary>
        /// 当覆盖安装包时，StreamingAssets要比PresistentData新
        /// </summary>
        public static bool IsStreamingAssetsVersionNew = false;

        /// <summary>
        /// 路径连接
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <returns></returns>
        public static string PathCombine(string path1, string path2)
        {
            return path1 + "/" + path2;
        }

        /// <summary>
        /// 获得资源的完整路径
        /// </summary>
        public static string GetFullResourcePath(string path)
        {
            return PathCombine(ResourcePathPrefix, path);
        }

        /// <summary>
        /// 获得本地资源文件路径
        /// 热更新资源会写入到PresistentData目录下
        /// 本地资源先查询PresistentData目录，没有则返回StreamingAssets目录下路径
        /// </summary>
        public static string GetLocalFilePath(string filePath)
        {
            if (!IsStreamingAssetsVersionNew)
            {
                var path = GetPresistentDataFilePath(filePath);
                if (File.Exists(path))
                {
                    return path;
                }
            }
            return GetStreamingAssetsFilePath(filePath);
        }

        /// <summary>
        /// 获得StreamingAssets下的资源文件路径
        /// </summary>
        public static string GetStreamingAssetsFilePath(string filePath)
        {
            return PathCombine(Application.streamingAssetsPath, filePath);
        }

        /// <summary>
        /// 获得PresistentData下的资源文件路径
        /// </summary>
        public static string GetPresistentDataFilePath(string filePath)
        {
            return PathCombine(Application.persistentDataPath, filePath);
        }

        /// <summary>
        /// 根据资源路径映射对应的AssetBundle包名
        /// </summary>
        /// <returns></returns>
        public static string GetAssetBundleName(string path)
        {
            return path.Replace("/", "_").Replace(".", "_").ToLower();
        }

        /// <summary>
        /// 获取本地AssetBundle路径
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static string GetLocalAssetBundlePath(string assetBundleName)
        {
            var assetBundlesPath = PathCombine(AssetBundlesFolder, assetBundleName);
            return GetLocalFilePath(assetBundlesPath);
        }
    }
}

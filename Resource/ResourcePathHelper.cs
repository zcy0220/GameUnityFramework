/**
 * 资源路径工具
 */

using System.IO;
using UnityEngine;

namespace GameUnityFramework.Resource
{
    public class ResourcePathHelper
    {
        /// <summary>
        /// 资源完整路径的前缀
        /// </summary>
        public static string ResourcePathPrefix;

        /// <summary>
        /// 获得资源的完整路径
        /// </summary>
        public static string GetFullResourcePath(string path)
        {
            return Path.Combine(ResourcePathPrefix, path);
        }

        /// <summary>
        /// 获得本地资源文件路径
        /// 热更新资源会写入到PresistentData目录下
        /// 本地资源先查询PresistentData目录，没有则返回StreamingAssets目录下路径
        /// </summary>
        public static string GetLocalFilePath(string filePath)
        {
            return CheckPresistentDataFileExsits(filePath) ? GetPresistentDataFilePath(filePath) : GetStreamingAssetsFilePath(filePath);
        }

        /// <summary>
        /// 获得StreamingAssets下的资源文件路径
        /// </summary>
        public static string GetStreamingAssetsFilePath(string filePath)
        {
            return Path.Combine(Application.streamingAssetsPath, filePath);
        }

        /// <summary>
        /// 获得PresistentData下的资源文件路径
        /// </summary>
        public static string GetPresistentDataFilePath(string filePath)
        {
            return Path.Combine(Application.persistentDataPath, filePath);
        }

        /// <summary>
        /// 检测PresistentData下的资源文件路径
        /// </summary>
        public static bool CheckPresistentDataFileExsits(string filePath)
        {
            var path = GetPresistentDataFilePath(filePath);
            return File.Exists(path);
        }
    }
}

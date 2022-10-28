/**
 * 文件处理拓展工具
 */

using System.IO;
using GameBaseFramework.Base;

namespace GameUnityFramework.Utils
{
    public class FileUtil
    {
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
        /// Writes all text.
        /// </summary>
        public static bool WriteAllText(string outFile, string outText)
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
                Debuger.LogError($"WriteAllText failed! path = {outFile} with err = {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Writes all bytes.
        /// </summary>
        public static bool WriteAllBytes(string outFile, byte[] outBytes)
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
                Debuger.LogError($"WriteAllBytes failed! path = {outFile} with err = {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Read Text
        /// </summary>
        public static string ReadAllText(string inFile)
        {
            try
            {
                if (string.IsNullOrEmpty(inFile)) return null;
                if (!File.Exists(inFile)) return null;
                File.SetAttributes(inFile, FileAttributes.Normal);
                return File.ReadAllText(inFile);
            }
            catch (System.Exception e)
            {
                Debuger.LogError("ReadAllText failed! path = {0} with err = {1}", inFile, e.Message);
                return null;
            }
        }

        /// <summary>
        /// Read Bytes
        /// </summary>
        public static byte[] ReadAllBytes(string inFile)
        {
            try
            {
                if (string.IsNullOrEmpty(inFile)) return null;
                if (!File.Exists(inFile)) return null;
                File.SetAttributes(inFile, FileAttributes.Normal);
                return File.ReadAllBytes(inFile);
            }
            catch (System.Exception e)
            {
                Debuger.LogError("ReadAllBytes failed! path = {0} with err = {1}", inFile, e.Message);
                return null;
            }
        }

    }
}

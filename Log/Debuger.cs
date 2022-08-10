/**
 * 日志
 */

namespace GameUnityFramework.Log
{
    public class Debuger
    {
        /// <summary>
        /// Log
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void Log(string msg, params object[] args)
        {
            UnityEngine.Debug.LogFormat(msg, args);
        }

        /// <summary>
        /// Log Warning
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void LogWarning(string msg, params object[] args)
        {
            UnityEngine.Debug.LogWarningFormat(msg, args);
        }

        /// <summary>
        /// Log Error
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogError(string format, params object[] args)
        {
            UnityEngine.Debug.LogErrorFormat(format, args);
        }
    }
}

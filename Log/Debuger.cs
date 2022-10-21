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
        /// <param name="message"></param>
        public static void Log(string message)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log(message);
#endif
        }

        /// <summary>
        /// Log(color)
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="color"></param>
        public static void Log(string message, string color)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Log($"<color={color}>{message}</color>");
#endif
        }

        /// <summary>
        /// Log
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void Log(string message, params object[] args)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogFormat(message, args);
#endif
        }

        /// <summary>
        /// Log Warning
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void LogWarning(string message, params object[] args)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogWarningFormat(message, args);
#endif
        }

        /// <summary>
        /// Log Error
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogError(string format, params object[] args)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.LogErrorFormat(format, args);
#endif
        }
    }
}

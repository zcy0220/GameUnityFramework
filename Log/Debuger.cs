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
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UnityEngine.Debug.LogFormat(msg, args);
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="color"></param>
        public static void Log(string msg, string color)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UnityEngine.Debug.Log($"<color={color}>{msg}</color>");
#endif
        }

        /// <summary>
        /// Log Warning
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="args"></param>
        public static void LogWarning(string msg, params object[] args)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UnityEngine.Debug.LogWarningFormat(msg, args);
#endif
        }

        /// <summary>
        /// Log Error
        /// </summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public static void LogError(string format, params object[] args)
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            UnityEngine.Debug.LogErrorFormat(format, args);
#endif
        }
    }
}

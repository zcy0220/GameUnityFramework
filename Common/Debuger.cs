/**
 * 日志
 */

using UnityEngine;

namespace GameFramework
{
    public class Debuger
    {
        public static void LogError(string format, params object[] args)
        {
            Debug.LogErrorFormat(format, args);
        }
    }
}

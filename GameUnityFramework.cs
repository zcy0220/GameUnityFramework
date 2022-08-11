/**
 * GameUnityFramework Init
 */

namespace GameUnityFramework
{
    using Utils;
    using Resource;

    public class GameUnityFrameworkConfig
    {
        /// <summary>
        /// 资源文件路径前缀
        /// </summary>
        public static string ResourcePathPrefix;

        /// <summary>
        /// 框架初始化
        /// </summary>
        public static void Init()
        {
            UnityObjectManager.Instance.Init(MonoBehaviourUtils.Instance);
            MonoBehaviourUtils.Instance.AddUpdateListener(UnityObjectManager.Instance.Update);
        }
    }
}

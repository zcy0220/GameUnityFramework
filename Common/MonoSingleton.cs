/**
 * Mono单例
 */

using UnityEngine;

namespace GameFramework
{
    public class MonoSingleton<T> : MonoBehaviour where T : MonoSingleton<T>
    {
        /// <summary>
        /// 唯一引用
        /// </summary>
        protected static T _instance;

        /// <summary>
        /// 公用访问
        /// </summary>
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    var type = typeof(T);
                    _instance = GameObject.FindObjectOfType(type) as T;
                    if (_instance == null)
                    {
                        _instance = new GameObject(type.Name, type).GetComponent<T>();
                        GameObject.DontDestroyOnLoad(_instance.gameObject);
                    }
                }
                return _instance;
            }
        }
    }
}
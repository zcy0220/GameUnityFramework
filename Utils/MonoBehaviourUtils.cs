/**
 * Mono Updater 管理
 */

using System;
using GameUnityFramework.Log;
using GameUnityFramework.Patterns;

namespace GameUnityFramework.Utils
{
    /// <summary>
    /// MonoUpdater委托事件
    /// </summary>
    public delegate void MonoUpdateEvent();

    public class MonoBehaviourUtils : MonoSingleton<MonoBehaviourUtils>
    {
        /// <summary>
        /// 渲染Update
        /// </summary>
        private event MonoUpdateEvent UpdateEvent;
        /// <summary>
        /// 物理固定时间Update
        /// </summary>
        private event MonoUpdateEvent FixedUpdateEvent;

        /// <summary>
        /// AddUpdateListener
        /// </summary>
        /// <param name="listener"></param>
        public void AddUpdateListener(MonoUpdateEvent listener)
        {
            UpdateEvent += listener;
        }

        /// <summary>
        /// RemoveUpdateListener
        /// </summary>
        /// <param name="listener"></param>
        public void RemoveUpdateListener(MonoUpdateEvent listener)
        {
            UpdateEvent -= listener;
        }

        /// <summary>
        /// AddFixedUpdateListener
        /// </summary>
        /// <param name="listener"></param>
        public void AddFixedUpdateListener(MonoUpdateEvent listener)
        {
            FixedUpdateEvent += listener;
        }

        /// <summary>
        /// RemoveFixedUpdateListener
        /// </summary>
        /// <param name="listener"></param>
        public void RemoveFixedUpdateListener(MonoUpdateEvent listener)
        {
            FixedUpdateEvent -= listener;
        }

        /// <summary>
        /// Update
        /// </summary>
        private void Update()
        {
            if (UpdateEvent != null)
            {
                try
                {
                    UpdateEvent();
                }
                catch (Exception e)
                {
                    Debuger.LogError("MonoUpdateEvent", "Update() Error:{0}\n{1}", e.Message, e.StackTrace);
                }
            }
        }

        /// <summary>
        /// FixedUpdate
        /// </summary>
        private void FixedUpdate()
        {
            if (FixedUpdateEvent != null)
            {
                try
                {
                    FixedUpdateEvent();
                }
                catch (Exception e)
                {
                    Debuger.LogError("MonoUpdateEvent", "FixedUpdate() Error:{0}\n{1}", e.Message, e.StackTrace);
                }
            }
        }
    }
}



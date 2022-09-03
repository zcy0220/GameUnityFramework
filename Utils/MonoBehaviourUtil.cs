/**
 * Mono Updater 管理
 */

using System;
using GameUnityFramework.Log;
using GameBaseFramework.Patterns;

namespace GameUnityFramework.Utils
{
    /// <summary>
    /// MonoUpdater委托事件
    /// </summary>
    public delegate void MonoUpdateEvent(float deltaTime);
    /// <summary>
    /// FixedUpdate委托事件
    /// 单位毫秒
    /// </summary>
    public delegate void MonoFixedUpdateEvent(int deltaTime);

    public class MonoBehaviourUtil : Singleton<MonoBehaviourUtil>
    {
        /// <summary>
        /// 渲染Update
        /// </summary>
        private event MonoUpdateEvent UpdateEvent;
        /// <summary>
        /// 物理固定时间Update
        /// </summary>
        private event MonoFixedUpdateEvent FixedUpdateEvent;

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
        public void AddFixedUpdateListener(MonoFixedUpdateEvent listener)
        {
            FixedUpdateEvent += listener;
        }

        /// <summary>
        /// RemoveFixedUpdateListener
        /// </summary>
        /// <param name="listener"></param>
        public void RemoveFixedUpdateListener(MonoFixedUpdateEvent listener)
        {
            FixedUpdateEvent -= listener;
        }

        /// <summary>
        /// Update
        /// </summary>
        public void Update(float deltaTime)
        {
            if (UpdateEvent != null)
            {
                try
                {
                    UpdateEvent(deltaTime);
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
        public void FixedUpdate(int deltaTime)
        {
            if (FixedUpdateEvent != null)
            {
                try
                {
                    FixedUpdateEvent(deltaTime);
                }
                catch (Exception e)
                {
                    Debuger.LogError("MonoUpdateEvent", "FixedUpdate() Error:{0}\n{1}", e.Message, e.StackTrace);
                }
            }
        }
    }
}



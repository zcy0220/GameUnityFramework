/**
 * Mono Updater 管理
 */

using System;

namespace GameFramework.Utils
{
    /// <summary>
    /// MonoUpdater委托事件
    /// </summary>
    public delegate void MonoUpdaterEvent();

    public class MonoUpdaterManager : MonoSingleton<MonoUpdaterManager>
    {
        /// <summary>
        /// 渲染Update
        /// </summary>
        private event MonoUpdaterEvent UpdateEvent;
        /// <summary>
        /// 物理固定时间Update
        /// </summary>
        private event MonoUpdaterEvent FixedUpdateEvent;

        public void AddUpdateListener(MonoUpdaterEvent listener)
        {
            UpdateEvent += listener;
        }

        public void RemoveUpdateListener(MonoUpdaterEvent listener)
        {
            UpdateEvent -= listener;
        }

        public void AddFixedUpdateListener(MonoUpdaterEvent listener)
        {
            FixedUpdateEvent += listener;
        }

        public void RemoveFixedUpdateListener(MonoUpdaterEvent listener)
        {
            FixedUpdateEvent -= listener;
        }

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
                    Debuger.LogError("MonoUpdaterManager", "Update() Error:{0}\n{1}", e.Message, e.StackTrace);
                }
            }
        }

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
                    Debuger.LogError("MonoUpdaterManager", "FixedUpdate() Error:{0}\n{1}", e.Message, e.StackTrace);
                }
            }
        }
    }
}



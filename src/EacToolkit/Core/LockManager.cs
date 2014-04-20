#region Using Directives

using System.Collections.Generic;
using EndecaControl.EacToolkit.Services;

#endregion

namespace EndecaControl.EacToolkit.Core
{
    internal class LockManager
    {
        public const string BASELINE_DATA_READY_FLAG = "baseline_data_ready";
        public const string PARTIAL_DATA_READY_FLAG = "partial_data_ready";
        public const string UPDATE_FLAG = "update";


        private readonly string appId;

        public LockManager(string appId)
        {
            this.appId = appId;
        }

        public bool AcquireLock(string lockName)
        {
            return EacGateway.Instance.SetFlag(appId, lockName);
        }

        public void ReleaseLock(string lockName)
        {
            EacGateway.Instance.RemoveFlag(appId, lockName);
        }

        public void ReleaseAllLocks()
        {
            EacGateway.Instance.RemoveAllFlags(appId);
        }

        public List<string> GetAllLocks()
        {
            return new List<string>(EacGateway.Instance.GetAllFlags(appId));
        }

        public bool IsLockSet(string lockName)
        {
            return GetAllLocks().Contains(lockName);
        }
    }
}
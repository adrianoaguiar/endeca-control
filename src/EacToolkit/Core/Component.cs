#region Using Directives

using System;
using System.Collections.Generic;
using System.Threading;
using Endeca.Control.EacToolkit;
using EndecaControl.EacToolkit.Services;

#endregion

namespace EndecaControl.EacToolkit.Core
{
    public abstract class Component : EacElement
    {
        private const string NUM_LOG_BACKUPS_PROPNAME = "numLogBackups";
        private const int numLogBackups = 1;
        private readonly string componentId;
        private readonly string hostId;
        private Dictionary<string, string> customProps = new Dictionary<string, string>();
        private string failureMessage = string.Empty;

        private int waitInterval = 10000; // 10 sec.

        protected Component(string compId, string appId, HostType host) : base(appId, host.hostname, host.port)
        {
            componentId = compId;
            hostId = host.hostID;
        }

        /// <summary>
        /// Get or set a component's waiting interval in milliseconds for waiting operations.
        /// Deafult is 10 seconds
        /// </summary>
        public int WaitInterval
        {
            get { return waitInterval; }
            set { waitInterval = value; }
        }

        /// <summary>
        /// Gets ComponentId
        /// </summary>
        public string ComponentId
        {
            get { return componentId; }
        }

        /// <summary>
        /// Working file directory of a component.
        /// </summary>
        public string WorkingDir { get; set; }

        /// <summary>
        /// Log files directory for the component
        /// </summary>
        public string LogDir { get; set; }


        /// <summary>
        /// Returns true if a component's status is Active.
        /// </summary>
        public bool IsActive
        {
            get
            {
                var statusType = GetStatus();
                return statusType.state == StateType.Starting || statusType.state == StateType.Running;
            }
        }

        public bool IsFailed
        {
            get
            {
                var statusType = GetStatus();
                failureMessage = statusType.failureMessage;
                return statusType.state == StateType.Failed;
            }
        }

        /// <summary>
        /// Temporary files folder
        /// </summary>
        public string TempDir { get; set; }

        public string InputDirectory { get; set; }

        public string FailureMessage
        {
            get { return failureMessage; }
        }

        public string HostId
        {
            get { return hostId; }
        }

        public string DataPrefix { get; set; }

        public int NumLogBackups
        {
            get {
                return customProps.ContainsKey(NUM_LOG_BACKUPS_PROPNAME) ? 
                    (Convert.ToInt32(CustomProps[NUM_LOG_BACKUPS_PROPNAME])) : numLogBackups;
            }
        }

        public Dictionary<string, string> CustomProps
        {
            get { return customProps; }
            set { customProps = value; }
        }

        public virtual void CleanDirs()
        {
        }

        /// <summary>
        /// Start EAC component.
        /// </summary>
        protected void StartComponent(bool waitComplete)
        {
            Logger.Debug(String.Format("StartComponent: {0}", ComponentId));
            EacGateway.Instance.StartComponent(AppId, ComponentId);
            if (waitComplete)
            {
                WaitComplete();
            }
        }


        /// <summary>
        /// Sends a Stop command to EAC component and waits until it stops.
        /// </summary>
        protected void StopComponent(bool waitComplete)
        {
            Logger.Debug(String.Format("StopComponent: {0}", ComponentId));
            EacGateway.Instance.StopComponent(AppId, ComponentId);
            if (waitComplete)
            {
                WaitComplete();
            }
        }

        /// <summary>
        /// Returns Componet Status
        /// </summary>
        /// <returns></returns>
        public StatusType GetStatus()
        {
            var statusType = EacGateway.Instance.GetComponentStatus(AppId, ComponentId);
            Logger.Debug(String.Format("GetStatus: {0}:{1} {2}", ComponentId, statusType.state,
                                       statusType.failureMessage));
            return statusType;
        }

        /// <summary>
        /// Pauses a thread until a component's status is Running.
        /// </summary>
        protected virtual void WaitComplete()
        {
            while (IsActive)
            {
                Thread.Sleep(waitInterval);
            }
        }

        /// <summary>
        /// Cleans specified deirectory
        /// </summary>
        /// <param name="dir"></param>
        public bool CleanDir(string dir)
        {
            Logger.Debug(String.Format("CleanDir: {0} - {1}", ComponentId, dir));
            var cmd = String.Format(@"del /s /q {0}\. > nul", dir);
            var token = ShellCmd(cmd);
            return WaitUtilityComplete(token);
        }

        public virtual bool ArchiveLog(bool waitComplete)
        {
            Logger.Debug(String.Format("ArchiveLog: {0}", ComponentId));
            var token = BackupFiles(LogDir, NumLogBackups);
            if (waitComplete)
            {
                return WaitUtilityComplete(token);
            }
            return true;
        }


        /// <summary>
        /// Executes a shell command on a given host.
        /// </summary>
        /// <param name="cmd">Command text.</param>
        /// <returns>A string token to check an operation status by.</returns>
        protected string ShellCmd(string cmd)
        {
            Logger.Debug(String.Format("ShellCmd: {0} - {1}", ComponentId, cmd));
            return EacGateway.Instance.ShellCmd(AppId, HostId, cmd);
        }

        protected string CopyFiles(string fromHostId, string fromPath, string toPath)
        {
            Logger.Debug(String.Format("CopyFiles: {0}-{1}:{2}->{3}", ComponentId, fromHostId, fromPath, toPath));
            if (!fromPath.EndsWith("\\*"))
            {
                fromPath += "\\*";
            }
            return EacGateway.Instance.StartCopyFiles(AppId, fromHostId, fromPath, HostId, toPath, true);
        }

        protected string BackupFiles(string dir, int numBackups)
        {
            Logger.Debug(String.Format("BackupFiles: {0}-{1}", ComponentId, dir));
            return EacGateway.Instance.StartBackupFiles(AppId, HostId, dir, numBackups, BackupMethodType.Move);
        }

        protected bool WaitUtilityComplete(string token)
        {
            Logger.Debug(String.Format("WaitUtilityComplete: {0}-{1}", ComponentId, token));
            var status = EacGateway.Instance.GetUtilityStatus(AppId, token);
            while (status.state == StateType.Running)
            {
                Thread.Sleep(waitInterval);
                status = EacGateway.Instance.GetUtilityStatus(AppId, token);
            }
            failureMessage = status.failureMessage;
            Logger.Debug(String.Format("WaitUtilityComplete finished: {0}-{1} state: {2}", ComponentId, token,
                                       status.state));
            return status.state != StateType.Failed;
        }

        public override string ToString()
        {
            return String.Format("{0} {1}:{2}", ComponentId, EacHostName, EacPort);
        }
    }
}
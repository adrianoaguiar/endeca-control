#region Using Directives

using System;
using System.Net;
using Endeca.Control.EacToolkit;
using EndecaControl.EacToolkit.Core;
using EndecaControl.EacToolkit.Services;

#endregion

namespace EndecaControl.EacToolkit.Components
{
    public sealed class DgraphComponent : ServerComponent
    {
        private const int WebReqTimeout = 600000; // 10 min
        internal bool IndexApplied;

        internal bool UpdateApplied;
        private string _updateDir;

        //private const string DeleteBackupIndexFolderTemplate = @"del /s /q {0}.bak\. > nul && rd /s /q {0}.bak > nul";
        private const string BackUpExistingIndexTemplate = @"move {0} {0}.bak > nul";
       // private const string DeleteExistingBackupTemplate = @"rd /s /q {0}.bak > nul";
        private const string MoveExistingIndexToBackupTemplate = @"move {0} {1} > nul";

        internal DgraphComponent(string compId, string appId, HostType host) : base(compId, appId, host)
        {
        }


        /// <summary>
        ///     Partial update log files folder
        /// </summary>
        public string UpdateLogDir { get; set; }

        /// <summary>
        ///     Partial update folder
        /// </summary>
        public string UpdateDir
        {
            get { return _updateDir; }
            set { _updateDir = value; }
        }

        /// <summary>
        ///     Index distribution local temporary folder
        /// </summary>
        public string IndexDistributionDir { set; get; }

        /// <summary>
        ///     Stops MDEX engine, copy files to the input folder and starts the engine
        /// </summary>
        /// <returns>true if successful</returns>
        public bool ApplyIndex()
        {
            if (IsActive)
            {
                StopComponent(true);
            }
            Logger.Debug(String.Format("{0}:{1} - Deleting backup index folder.", hostName, port));
            CleanDir(string.Format(InputDirectory.Substring(0, InputDirectory.LastIndexOf(@"\", StringComparison.InvariantCulture)), ".bak"));

            Logger.Debug(String.Format("{0}:{1} - Backing up existing index.", hostName, port));
            BackupFiles(CustomProps["localIndexDir"], 1);
            // BackUpExistingIndex();

            Logger.Debug(String.Format("{0}:{1} - Move index from the local index distribution folder.", hostName, port));
            MoveIndexToInputFolder();

            var token = CopyFiles(HostId, IndexDistributionDir, InputDirectory);

            Logger.Debug(String.Format("{0}:{1} - Re-creating the local index distribution folder.", hostName, port));
            ReCreateLocalIndexDistributionFolder();

            Logger.Debug(String.Format("{0}:{1} - Attempting to start Dgraph.", hostName, port));

            if (WaitUtilityComplete(token))
            {
                ArchiveLog(true);
                Start(true);
            }
            else
            {
                Logger.Error(FailureMessage);
            }
            IndexApplied = IsActive;
            return IndexApplied;
        }

        private void ReCreateLocalIndexDistributionFolder()
        {
            const string reCreateLocalIndexDistributionFolderTemplate = @"mkdir {0}";
            var cmd = String.Format(reCreateLocalIndexDistributionFolderTemplate, IndexDistributionDir);
            var token = EacGateway.Instance.ShellCmd(AppId, HostId, cmd);
            if (WaitUtilityComplete(token))
            {
                Logger.Debug(String.Format("Re-created local index distribution folder on host {0}. Token: {1}.",
                                           HostId, token));
            }
            else
            {
                throw new ControlScriptException(
                    String.Format("Re-creating local index distribution failed on host {0}.", HostId));
            }
        }

        private void MoveIndexToInputFolder()
        {
            var cmd = String.Format(MoveExistingIndexToBackupTemplate, IndexDistributionDir,
                                    InputDirectory.Substring(0, InputDirectory.LastIndexOf(@"\")));
            var token = EacGateway.Instance.ShellCmd(AppId, HostId, cmd);
            if (WaitUtilityComplete(token))
            {
                Logger.Debug(String.Format("Deleted backup index folder on host {0}. Token: {1}.", HostId, token));
            }
            else
            {
                throw new ControlScriptException(
                    String.Format("Local index distribution folder cleaning failed on host {0}.", HostId));
            }
        }

        private void BackUpExistingIndex()
        {
            var inputDir = InputDirectory.Substring(0, InputDirectory.LastIndexOf(@"\", StringComparison.InvariantCulture));
            var cmd = String.Format(BackUpExistingIndexTemplate, inputDir);
            var token = EacGateway.Instance.ShellCmd(AppId, HostId, cmd);
            if (WaitUtilityComplete(token))
            {
                Logger.Debug(String.Format("Backed up existing index on host {0}. Token: {1} & {2}.", HostId, token,
                                           token));
            }
            else
            {
                throw new ControlScriptException(
                    String.Format("Existing index backup failed on host {0}.", HostId));
            }
        }

        /// <summary>
        ///     Cleans partial updates folder
        /// </summary>
        public void CleanUpdateDir()
        {
            CleanDir(_updateDir);
        }

        /// <summary>
        ///     Applies partial update
        /// </summary>
        public bool ApplyUpdate()
        {
            //TODO: Async????????
            Logger.Debug(String.Format("{0}:{1} - Applying updates.", hostName, port));
            var req = String.Format("http://{0}:{1}/admin?op=update", HostName, Port);
            var updateReq = (HttpWebRequest) WebRequest.Create(req);
            updateReq.Credentials = CredentialCache.DefaultCredentials;
            updateReq.Timeout = WebReqTimeout;
            try
            {
                var response = (HttpWebResponse) updateReq.GetResponse();
                UpdateApplied = response.StatusCode == HttpStatusCode.OK;
            }
            catch (WebException e)
            {
                Logger.Error("Update request", e);
                UpdateApplied = false;
            }
            return UpdateApplied;
        }

        /// <summary>
        ///     Lightweight healthcheck for monitoring whether the Dgraph is alive and accepting queries
        /// </summary>
        /// <returns> true if responds</returns>
        public bool IsAlive()
        {
            Logger.Debug(String.Format("Pinging Dgraph on {0}:{1}", hostName, port));
            var req = String.Format("http://{0}:{1}/admin?op=ping", HostName, Port);
            var updateReq = (HttpWebRequest) WebRequest.Create(req);
            updateReq.Credentials = CredentialCache.DefaultCredentials;
            updateReq.Timeout = WebReqTimeout;
            try
            {
                var response = (HttpWebResponse) updateReq.GetResponse();
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (WebException e)
            {
                Logger.Error(String.Format("Failed to ping {0}:{1}", hostName, Port), e);
                return false;
            }
        }

        public override void CleanDirs()
        {
            CleanDir(IndexDistributionDir);
            CleanDir(UpdateDir);
        }
    }
}
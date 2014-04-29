#region Using Directives

using System;
using System.IO;
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

        internal DgraphComponent(string compId, string appId, HostType host) : base(compId, appId, host)
        {
        }


        /// <summary>
        /// Partial update log files folder
        /// </summary>
        public string UpdateLogDir { get; set; }

        /// <summary>
        /// Partial update folder
        /// </summary>
        public string UpdateDir
        {
            get { return _updateDir; }
            set { _updateDir = value; }
        }

        /// <summary>
        /// Index distribution local temporary folder
        /// </summary>
        public string IndexDistributionDir { set; get; }

        /// <summary>
        /// Stops MDEX engine, copy files to the input folder and starts the engine
        /// </summary>
        /// <returns>true if successful</returns>
        public bool ApplyIndex()
        {
            if (IsActive)
            {
                Logger.Info(String.Format("{0} - Stopping dgraph ...", ComponentId));
                StopComponent(true);
            }

            var inputFolder = InputDirectory.Substring(0, InputDirectory.LastIndexOf(@"\"));

            BackUpExistingIndex(inputFolder);
            CopyFilesAndApply(inputFolder);
            IndexApplied = IsActive;
            return IndexApplied;
        }

        /// <summary>
        /// Cleans partial updates folder
        /// </summary>
        public void CleanUpdateDir()
        {
            CleanDir(_updateDir);
        }

        /// <summary>
        /// Applies partial update
        /// </summary>
        public bool ApplyUpdate()
        {
            //TODO: Async????????
            Logger.Debug(String.Format("{0} - Applying updates ...", ComponentId));
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
        /// Lightweight healthcheck for monitoring whether the Dgraph is alive and accepting queries
        /// </summary>
        /// <returns> true if responds</returns>
        public bool IsAlive()
        {
            Logger.Debug(String.Format("Pinging {0} on {1}:{2} ...", ComponentId, hostName, port));
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
                Logger.Error(String.Format("Failed to ping {0} at {1}:{2}!", ComponentId, hostName, Port), e);
                return false;
            }
        }

        public override void CleanDirs()
        {
            CleanDir(IndexDistributionDir);
            CleanDir(UpdateDir);
        }

        #region Private Helper Methods

        private void CopyFilesAndApply(string inputFolder)
        {
            var token = CopyFiles(HostId, IndexDistributionDir, inputFolder);

            if (WaitUtilityComplete(token))
            {
                Logger.Info(String.Format("{0} - Archiving logs ...", ComponentId));
                ArchiveLog(true);

                Logger.Debug(String.Format("{0} - Attempting to start Dgraph ...", ComponentId));
                Start(true);
            }
            else
            {
                Logger.Error(FailureMessage);
            }
        }

        private void BackUpExistingIndex(string inputFolder)
        {
            Logger.Info(String.Format("{0} - Backing up existing index ...", ComponentId));
            var token = BackupFiles(inputFolder, 1);

            if (WaitUtilityComplete(token))
            {
                Logger.Debug(String.Format("{0} - Existing index backed up!", ComponentId));
            }
            else
            {
                Logger.Error(String.Format("{0} - Backing up existing index failed!", ComponentId));
            }
        }

        #endregion

    }
}
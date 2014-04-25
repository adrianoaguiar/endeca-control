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

        private const string BackUpExistingIndexTemplate = @"move {0} {0}.bak > nul";
        private const string MoveFoldersTemplate = @"move {0} {1} > nul";

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

            var inputFolder = InputDirectory.Substring(0, InputDirectory.LastIndexOf(@"\"));

            Logger.Debug(String.Format("{0}:{1} - Backing up existing index.", hostName, port));
            BackupFiles(inputFolder, 1);

            var token = CopyFiles(HostId, IndexDistributionDir, inputFolder);

            if (WaitUtilityComplete(token))
            {
                Logger.Debug(String.Format("{0}:{1} - Archiving logs.", hostName, port));
                ArchiveLog(true);

                Logger.Debug(String.Format("{0}:{1} - Attempting to start Dgraph.", hostName, port));
                Start(true);
            }
            else
            {
                Logger.Error(FailureMessage);
            }
            IndexApplied = IsActive;
            return IndexApplied;
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
#region Using Directives

using System.Collections.Generic;
using System.Net;
using Endeca.Control.EacToolkit;

#endregion

namespace EndecaControl.EacToolkit.Services
{
    public class EacGateway
    {
        private const int retryCnt = 3;
        private const int timeout = 60000;
        private static EacGateway instance;
        private readonly ComponentControlPortSOAPBinding componentSvc = new ComponentControlPortSOAPBinding();
        private readonly ProvisioningPortSOAPBinding provisionSvc = new ProvisioningPortSOAPBinding();
        private readonly ScriptControlPortSOAPBinding scriptSvc = new ScriptControlPortSOAPBinding();
        private readonly SynchronizationPortSOAPBinding syncSvc = new SynchronizationPortSOAPBinding();
        private readonly UtilityPortSOAPBinding utilSvc = new UtilityPortSOAPBinding();

        private EacGateway(string hostName, int port)
        {
            componentSvc.Url = string.Format("http://{0}:{1}/eac/ComponentControlService", hostName, port);
            componentSvc.Timeout = timeout;
            provisionSvc.Url = string.Format("http://{0}:{1}/eac/ProvisioningService", hostName, port);
            provisionSvc.Timeout = timeout;
            scriptSvc.Url = string.Format("http://{0}:{1}/eac/ScriptControlService", hostName, port);
            scriptSvc.Timeout = timeout;
            syncSvc.Url = string.Format("http://{0}:{1}/eac/SynchronizationService", hostName, port);
            syncSvc.Timeout = timeout;
            utilSvc.Url = string.Format("http://{0}:{1}/eac/UtilityService", hostName, port);
            utilSvc.Timeout = timeout;
        }

        public static EacGateway Instance
        {
            get { return instance; }
        }

        public int Timeout
        {
            get { return timeout; }
        }

        #region Provisioning Service Methods

        public List<ApplicationType> GetApplications()
        {
            var apps = new List<ApplicationType>();
            var iDs = ListApplicationIDs();
            foreach (var id in iDs)
            {
                var app = GetApplication(id);
                if (app != null)
                {
                    apps.Add(app);
                }
            }
            return apps;
        }

        public string[] ListApplicationIDs()
        {
            var input = new listApplicationIDsInput();
            return provisionSvc.listApplicationIDs(input);
        }

        public ApplicationType GetApplication(string appId)
        {
            return (ApplicationType) ExecRetry(delegate { return provisionSvc.getApplication(appId); });
        }

        public ProvisioningWarningType[] RemoveApplication(string appId, bool forceRemove)
        {
            var param = new RemoveApplicationType();
            param.applicationID = appId;
            param.forceRemove = forceRemove;
            param.forceRemoveSpecified = true;
            return provisionSvc.removeApplication(param);
        }

        public ProvisioningWarningType[] AddApplication(ApplicationType app)
        {
            return provisionSvc.defineApplication(app);
        }

        #endregion

        #region Component Service Methods

        public StatusType GetComponentStatus(string appId, string compId)
        {
            var component = new FullyQualifiedComponentIDType();
            component.applicationID = appId;
            component.componentID = compId;
            var status = (StatusType) ExecRetry(delegate { return componentSvc.getComponentStatus(component); });
            return status;
        }

        public void StartComponent(string appId, string compId)
        {
            var component = new FullyQualifiedComponentIDType();
            component.applicationID = appId;
            component.componentID = compId;
            //componentSvc.startComponent(component);
            ExecRetry(delegate
                {
                    componentSvc.startComponent(component);
                    return null;
                });
        }

        public void StopComponent(string appId, string compId)
        {
            var component = new FullyQualifiedComponentIDType();
            component.applicationID = appId;
            component.componentID = compId;
            //componentSvc.stopComponent(component);
            ExecRetry(delegate
                {
                    componentSvc.stopComponent(component);
                    return null;
                });
        }

        #endregion

        #region Utility Service Methods

        public string StartCopyFiles(string appId, string fromHostId, string fromPath, string toHostId, string toPath,
                                     bool recursive)
        {
            var param = new RunFileCopyType();
            param.applicationID = appId;
            param.fromHostID = fromHostId;
            param.sourcePath = fromPath;
            param.toHostID = toHostId;
            param.destinationPath = toPath;
            param.recursive = recursive;
            param.recursiveSpecified = true;
            return utilSvc.startFileCopy(param);
        }

        public string StartBackupFiles(string appId, string hostId, string dir, int numBackups, BackupMethodType method)
        {
            var param = new RunBackupType();
            param.applicationID = appId;
            param.hostID = hostId;
            param.numBackups = numBackups;
            param.numBackupsSpecified = true;
            param.backupMethod = method;
            param.backupMethodSpecified = true;
            param.dirName = dir;
            return utilSvc.startBackup(param);
        }

        public BatchStatusType GetUtilityStatus(string appId, string token)
        {
            var param = new FullyQualifiedUtilityTokenType();
            param.applicationID = appId;
            param.token = token;
            var status =
                (BatchStatusType) ExecRetry(delegate { return utilSvc.getStatus(param); });
            return status;
        }

        /// <summary>
        ///     Executes a shell command on a given host.
        /// </summary>
        /// <param name="appId">Endeca Application ID which an operation will belong to when started.</param>
        /// <param name="hostId">Host's ID which to execute a shell command on.</param>
        /// <param name="cmd">A shell command text.</param>
        /// <returns>A string token used to determine an operation status.</returns>
        public string ShellCmd(string appId, string hostId, string cmd)
        {
            var param = new RunShellType();
            param.applicationID = appId;
            param.hostID = hostId;
            param.cmd = cmd;
            return utilSvc.startShell(param);
        }

        #endregion

        #region Sync Service Methods

        /// <summary>
        ///     Creates a new flag, identified by flagID, that is associated with the named application
        /// </summary>
        /// <param name="appId">identifies the application to use</param>
        /// <param name="flag">unique string identifier for this flag</param>
        /// <returns>Trues if successfull, false if flag is already set</returns>
        public bool SetFlag(string appId, string flag)
        {
            var flagIDType = new FullyQualifiedFlagIDType();
            flagIDType.applicationID = appId;
            flagIDType.flagID = flag;
            return syncSvc.setFlag(flagIDType);
        }

        /// <summary>
        ///     Removes the named flag
        /// </summary>
        /// <param name="appId">identifies the application to use</param>
        /// <param name="flag">unique string identifier for this flag</param>
        public void RemoveFlag(string appId, string flag)
        {
            var flagIDType = new FullyQualifiedFlagIDType();
            flagIDType.applicationID = appId;
            flagIDType.flagID = flag;
            syncSvc.removeFlag(flagIDType);
        }

        /// <summary>
        ///     Removes all flags in an application
        /// </summary>
        /// <param name="appId">identifies the application to use</param>
        public void RemoveAllFlags(string appId)
        {
            syncSvc.removeAllFlags(appId);
        }

        /// <summary>
        ///     Returns the collection of flags in an application
        /// </summary>
        /// <param name="appId">identifies the application to use</param>
        /// <returns>string array of flags</returns>
        public string[] GetAllFlags(string appId)
        {
            return syncSvc.listFlags(appId);
        }

        #endregion

        #region Script Service Methods

        /// <summary>
        /// Starts the named script.
        /// </summary>
        /// <param name="appId">appId identifies the application to use.</param>
        /// <param name="scriptId">scriptID identifies the script to use.</param>
        public void StartScript(string appId, string scriptId)
        {
            var scriptType = new FullyQualifiedScriptIDType {applicationID = appId, scriptID = scriptId};
            scriptSvc.startScript(scriptType);
        }

        /// <summary>
        /// Stops the named script.
        /// </summary>
        /// <param name="appId">appId identifies the application to use.</param>
        /// <param name="scriptId">scriptID identifies the script to use.</param>
        public void StopScript(string appId, string scriptId)
        {
            var scriptType = new FullyQualifiedScriptIDType { applicationID = appId, scriptID = scriptId };
            scriptSvc.stopScript(scriptType);
        }

        /// <summary>
        /// Returns the status of a script.
        /// </summary>
        /// <param name="appId">appId identifies the application to use.</param>
        /// <param name="scriptId">scriptID identifies the script to use.</param>
        /// <returns>ScriptStatus object (a sub-class of the StatusType class). 
        /// This status may be Running, NotRunning, or Failed. 
        /// (Failure results from a failure error code or internal EAC errors).</returns>
        public StatusType GetScriptStatus(string appId, string scriptId)
        {
            var scriptType = new FullyQualifiedScriptIDType { applicationID = appId, scriptID = scriptId };
            return scriptSvc.getScriptStatus(scriptType);
        }

        #endregion

        #region Helper methods

        internal static object ExecRetry(ExecRetryDelegate cmd)
        {
            for (var i = 0; i < retryCnt; i++)
            {
                try
                {
                    return cmd();
                }
                catch (WebException e)
                {
                    Logger.Debug(e.ToString());
                    if (e.Status != WebExceptionStatus.Timeout || i >= retryCnt)
                    {
                        throw;
                    }
                }
            }
            return null;
        }

        internal delegate object ExecRetryDelegate();

        #endregion

        public static void CreateEacGateway(string hostName, int port)
        {
            if (instance == null)
            {
                lock (typeof (EacGateway))
                {
                    instance = new EacGateway(hostName, port);
                }
            }
        }
    }
}
#region Using Directives

using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Gets the applications that are defined.
        /// </summary>
        /// <returns>A list of applications available on the server.</returns>
        public List<ApplicationType> GetApplications()
        {
            var iDs = ListApplicationIDs();
            return iDs.Select(id => GetApplication(id)).Where(app => app != null).ToList();
        }

        /// <summary>
        /// Lists the applications that are defined.
        /// </summary>
        /// <returns>String array containing application IDs.</returns>
        public string[] ListApplicationIDs()
        {
            var input = new listApplicationIDsInput();
            return provisionSvc.listApplicationIDs(input);
        }

        /// <summary>
        /// Gets an application, which is composed of hosts, components, 
        /// and scripts and identified by an application ID.
        /// </summary>
        /// <param name="appId">appId identifies the application to use.</param>
        /// <returns>The application identified by the application ID.</returns>
        public ApplicationType GetApplication(string appId)
        {
            return (ApplicationType) ExecRetry(() => provisionSvc.getApplication(appId));
        }

        /// <summary>
        /// Removes the named application.
        /// </summary>
        /// <param name="appId">appId identifies the application to remove.</param>
        /// <param name="forceRemove">Forces the application removal if true.</param>
        /// <returns></returns>
        public ProvisioningWarningType[] RemoveApplication(string appId, bool forceRemove)
        {
            var param = new RemoveApplicationType
                {
                    applicationID = appId,
                    forceRemove = forceRemove,
                    forceRemoveSpecified = true
                };
            return provisionSvc.removeApplication(param);
        }

        /// <summary>
        /// Defines an application.
        /// ApplicationType parameters:
        ///     • applicationID identifies the application to use.
        ///     • hosts is a collection of HostType objects, representing the hosts to define.
        ///     • components is a collection of ComponentType objects (such as ForgeComponentType,
        ///       DgraphComponentType, and so on) representing the components to define.
        ///     • scripts is a collection of ScriptType objects.
        /// </summary>
        /// <param name="app">Application object to add.</param>
        /// <returns></returns>
        public ProvisioningWarningType[] AddApplication(ApplicationType app)
        {
            return provisionSvc.defineApplication(app);
        }

        /// <summary>
        /// Adds a script to an application.
        /// </summary>
        /// <param name="appId">appId identifies the application to use.</param>
        /// <param name="script">script is a ScriptType object specifying the script to be updated.</param>
        /// <returns>A ProvisioningWarningListType object, containing minor 
        /// warnings about non-fatal provisioning problems.</returns>
        public ProvisioningWarningType[] AddScript(string appId, ScriptType script)
        {
            var addScriptInput = new AddScriptType() { applicationID = appId, script = script };
            return provisionSvc.addScript(addScriptInput);
        }

        /// <summary>
        /// Removes a script from an application.
        /// </summary>
        /// <param name="appId">appId identifies the application to use.</param>
        /// <param name="scriptId">script is a ScriptType object specifying the script to be updated.</param>
        /// <param name="forceRemove">forceRemove indicates that the Application Controller will attempt to 
        /// force the conditions under which the remove can take place.</param>
        /// <returns>A ProvisioningWarningListType object, containing minor 
        /// warnings about non-fatal provisioning problems.</returns>
        public ProvisioningWarningType[] RemoveScript(string appId, string scriptId, bool forceRemove = false)
        {
            var removeScriptInput = new RemoveScriptType { applicationID = appId, scriptID = scriptId, forceRemove = forceRemove };
            return provisionSvc.removeScript(removeScriptInput);
        }

        /// <summary>
        /// Updates a running script.
        /// </summary>
        /// <param name="appId">appId identifies the application to use.</param>
        /// <param name="script">script is a ScriptType object specifying the script to be updated.</param>
        /// <param name="forceUpdate">forceUpdate is a Boolean that indicates whether the Application Controller 
        /// should force a running script to stop before attempting the update.</param>
        /// <returns>A ProvisioningWarningListType object, containing minor 
        /// warnings about non-fatal provisioning problems.</returns>
        public ProvisioningWarningType[] UpdateScript(string appId, ScriptType script, bool forceUpdate = false)
        {
            var updateScriptInput = new UpdateScriptType() { applicationID = appId, script = script, forceUpdate = forceUpdate };
            return provisionSvc.updateScript(updateScriptInput);
        }

        #endregion

        #region Component Service Methods

        public StatusType GetComponentStatus(string appId, string compId)
        {
            var component = new FullyQualifiedComponentIDType {applicationID = appId, componentID = compId};
            var status = (StatusType) ExecRetry(() => componentSvc.getComponentStatus(component));
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
            var component = new FullyQualifiedComponentIDType {applicationID = appId, componentID = compId};
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
            var param = new RunFileCopyType
                {
                    applicationID = appId,
                    fromHostID = fromHostId,
                    sourcePath = fromPath,
                    toHostID = toHostId,
                    destinationPath = toPath,
                    recursive = recursive,
                    recursiveSpecified = true
                };
            return utilSvc.startFileCopy(param);
        }

        public string StartBackupFiles(string appId, string hostId, string dir, int numBackups, BackupMethodType method)
        {
            var param = new RunBackupType
                {
                    applicationID = appId,
                    hostID = hostId,
                    numBackups = numBackups,
                    numBackupsSpecified = true,
                    backupMethod = method,
                    backupMethodSpecified = true,
                    dirName = dir
                };
            return utilSvc.startBackup(param);
        }

        public BatchStatusType GetUtilityStatus(string appId, string token)
        {
            var param = new FullyQualifiedUtilityTokenType {applicationID = appId, token = token};
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
            var param = new RunShellType {applicationID = appId, hostID = hostId, cmd = cmd};
            return utilSvc.startShell(param);
        }

        #endregion

        #region Sync Service Methods

        /// <summary>
        /// Creates a new flag, identified by flagID, that is associated with the named application.
        /// </summary>
        /// <param name="appId">identifies the application to use.</param>
        /// <param name="flag">unique string identifier for this flag.</param>
        /// <returns>Trues if successful, false if flag is already set.</returns>
        public bool SetFlag(string appId, string flag)
        {
            var flagIdType = new FullyQualifiedFlagIDType {applicationID = appId, flagID = flag};
            return syncSvc.setFlag(flagIdType);
        }

        /// <summary>
        /// Removes the named flag.
        /// </summary>
        /// <param name="appId">identifies the application to use.</param>
        /// <param name="flag">unique string identifier for this flag.</param>
        public void RemoveFlag(string appId, string flag)
        {
            var flagIdType = new FullyQualifiedFlagIDType {applicationID = appId, flagID = flag};
            syncSvc.removeFlag(flagIdType);
        }

        /// <summary>
        /// Removes all flags in an application.
        /// </summary>
        /// <param name="appId">identifies the application to use.</param>
        public void RemoveAllFlags(string appId)
        {
            syncSvc.removeAllFlags(appId);
        }

        /// <summary>
        /// Returns the collection of flags in an application.
        /// </summary>
        /// <param name="appId">identifies the application to use.</param>
        /// <returns>string array of flags.</returns>
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
            if (instance != null) return;
            lock (typeof (EacGateway))
            {
                instance = new EacGateway(hostName, port);
            }
        }
    }
}
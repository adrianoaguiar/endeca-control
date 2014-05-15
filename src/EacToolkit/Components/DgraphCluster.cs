#region Using Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Endeca.Control.EacToolkit;
using EndecaControl.EacToolkit.Services;

#endregion

namespace EndecaControl.EacToolkit.Components
{
    public sealed class DgraphCluster
    {
        private const int PollInterval = 10000;
        private const string CleanDirCmdTemplate = @"del /s /q {0}\. > nul";
        private readonly EndecaApplication _app;
        //One Dgraph per host collection. Needed for index distribution and local archives
        private readonly List<DgraphComponent> _hostDgraphs = new List<DgraphComponent>();

        public DgraphCluster(EndecaApplication app)
        {
            _app = app;
            foreach (var host in app.Hosts)
            {
                var component = app.Dgraphs.FindOne(host.HostId);
                if (component != null)
                {
                    _hostDgraphs.Add(component);
                }
            }
        }

        #region Baseline Update

        /// <summary>
        /// Distributes the index to all hosts (local and remote) in parallel
        /// </summary>
        public void DistributeIndex()
        {
            var source = String.Format(@"{0}\*", _app.Dgidx.OutputDirectory.Substring(0,_app.Dgidx.OutputDirectory.LastIndexOf(@"\")));
            var copyOps = new List<HostOperation>();
            foreach (var dgraph in _hostDgraphs)
            {
                var token = EacGateway.Instance.StartCopyFiles(
                    _app.AppId, _app.Dgidx.HostId, source, dgraph.HostId, dgraph.IndexDistributionDir, true);
                copyOps.Add(new HostOperation(dgraph.HostId, token, null));
                Logger.Debug(String.Format("Started copy operation on host {0}. Token: {1}.", dgraph.HostId, token));
            }
            WaitComplete(copyOps);
        }

        /// <summary>
        /// </summary>
        /// <param name="pauseBetweenDgraphs">seconds between drgraph updates</param>
        public void ApplyIndex(int pauseBetweenDgraphs)
        {
            foreach (var dgraph in _app.Dgraphs)
            {
                if (dgraph.IndexApplied) continue;

                Logger.Debug(String.Format("Applying index on {0}:{1} dgraph instance.", dgraph.HostName,
                                           dgraph.Port));
                if (dgraph.ApplyIndex())
                {
                    Logger.Info(String.Format("{0}:{1} - Dgraph started.", dgraph.HostName, dgraph.Port));
                    if (pauseBetweenDgraphs > 0)
                    {
                        Logger.Info(String.Format("Pausing for {0} seconds", pauseBetweenDgraphs));
                        Thread.Sleep(pauseBetweenDgraphs*1000);
                    }
                }
                else
                {
                    Logger.Error(String.Format("{0}:{1} - Dgraph FAILED to start!!!", dgraph.HostName, dgraph.Port));
                }
            }
        }

        /// <summary>
        ///     Applies index to the specified dgraphs
        /// </summary>
        /// <param name="hostIDs">Host Ids index to be applied</param>
        /// <returns>False if at least one of Dgraphs fails to update</returns>
        public bool ApplyIndex(string[] hostIDs)
        {
            var allOk = true;
            foreach (var hostId in hostIDs)
            {
                foreach (var dgraph in _app.Dgraphs.FindAll(hostId))
                {
                    if (dgraph.IndexApplied) continue;

                    Logger.Debug(String.Format("Applying index on {0}:{1} dgraph instance.", dgraph.HostName,
                                               dgraph.Port));
                    if (dgraph.ApplyIndex())
                    {
                        Logger.Info(String.Format("{0}:{1} - Dgraph started.", dgraph.HostName, dgraph.Port));
                    }
                    else
                    {
                        Logger.Error(String.Format("{0}:{1} - Dgraph FAILED to start!!!", dgraph.HostName,
                                                   dgraph.Port));
                        allOk = false;
                    }
                }
            }
            return allOk;
        }

        #endregion

        #region Partial Update

        /// <summary>
        ///     Distributes the update to all hosts.
        /// </summary>
        /// <param name="partialForge">Partial forge instance.</param>
        public void DistributeUpdate(ForgeComponent partialForge)
        {
            //stamp partial update to ensure all partial updates are kept in the dgraph update folder until baseline update run
            var stampedName = partialForge.StampPartialUpdate();
            var updateFileName = Path.Combine(partialForge.OutputDirectory, stampedName);
            //get forge host
            var forgeHost = _app.Hosts[partialForge.HostId];

            var copyOps = new List<HostOperation>();
            foreach (var dgraph in _hostDgraphs)
            {
                var dgraph1 = dgraph;
                //find dgraph host
                var dgraphHost = _app.Hosts[dgraph1.HostId];

                // Update was already created on the Partial Forge hosts,
                // so we'll copy update only to the remote hosts
                if (forgeHost.EacHostName == dgraphHost.EacHostName) continue;

                var token = EacGateway.Instance.StartCopyFiles(_app.AppId, forgeHost.HostId, updateFileName,
                                                               dgraph.HostId,
                                                               dgraph.UpdateDir, false);
                copyOps.Add(new HostOperation(dgraph.HostId, token, null));
                Logger.Debug(String.Format("Started copy operation on host {0}. Token: {1}.", dgraph.HostId, token));
            }
            //Wait until all operations finish
            WaitComplete(copyOps);
        }

        /// <summary>
        ///     Applies the update to the entire dgraph cluster.
        /// </summary>
        public void ApplyUpdate()
        {
            foreach (var dgraph in _app.Dgraphs)
            {
                if (dgraph.UpdateApplied) continue;

                if (!dgraph.IsAlive())
                {
                    Logger.Error(
                        string.Format(
                            "Dgraph instance on {0}:{1} - is not reachable. The server might be down or offline.",
                            dgraph.HostName, dgraph.Port));
                }
                else if (dgraph.ApplyUpdate())
                {
                    Logger.Info(String.Format("{0}:{1} - updated successfully.", dgraph.HostName, dgraph.Port));
                }
                else
                {
                    throw new ControlScriptException(String.Format(
                        "Dgraph instance on {0}:{1} - failed to update.", dgraph.HostName, dgraph.Port));
                }
            }
        }

        #endregion

        #region Folder archive/cleanup methods

        public void CleanLocalIndexDistributionDir()
        {
            var cleanOps = new List<HostOperation>();
            foreach (var dgraph in _hostDgraphs)
            {
                try
                {
                    var cmd = String.Format(CleanDirCmdTemplate, dgraph.IndexDistributionDir);
                    var token = EacGateway.Instance.ShellCmd(_app.AppId, dgraph.HostId, cmd);
                    cleanOps.Add(new HostOperation(dgraph.HostId, token, null));
                    Logger.Debug(
                        String.Format(
                            "Started local index distribution folder cleaning on host {0}. Token: {1}. Command: {2}",
                            dgraph.HostId, token, cmd));
                }
                catch (Exception e)
                {
                    throw new ControlScriptException(
                        String.Format("Local index distribution folder cleaning failed on host {0}.\n\n{1}.",
                                      dgraph.HostId, e));
                }
            }
            WaitComplete(cleanOps);
        }

        public void CleanLocalUpdatesDir()
        {
            var cleanOps = new List<HostOperation>();
            foreach (var dgraph in _hostDgraphs)
            {
                try
                {
                    var cmd = String.Format(CleanDirCmdTemplate,
                                            dgraph.InputDirectory.Substring(0,
                                                                            dgraph.InputDirectory.LastIndexOf(@"\",
                                                                                                              StringComparison
                                                                                                                  .InvariantCulture)));
                    var token = EacGateway.Instance.ShellCmd(_app.AppId, dgraph.HostId, cmd);
                    cleanOps.Add(new HostOperation(dgraph.HostId, token, null));
                    Logger.Debug(
                        String.Format("Started local updates foder cleaning on host {0}. Token: {1}. Command: {2}",
                                      dgraph.HostId, token, cmd));
                }
                catch (Exception e)
                {
                    throw new ControlScriptException(
                        String.Format("Local updates folder cleaning failed on host {0}.\n\n{1}", dgraph.HostId, e));
                }
            }
            WaitComplete(cleanOps);
        }

        #endregion

        #region Helper methods

        private void WaitComplete(IEnumerable<HostOperation> operations)
        {
            var done = false;
            while (!done)
            {
                done = true;
                foreach (var hostOperation in operations)
                {
                    // check previuos status to avoid the case where EAC has discarted the token which will result in exception
                    if (hostOperation.Status == null ||
                        (hostOperation.State == StateType.Running || hostOperation.State == StateType.Starting))
                    {
                        hostOperation.Status = EacGateway.Instance.GetUtilityStatus(_app.AppId, hostOperation.Token);
                    }
                    Logger.Debug(hostOperation.ToString());
                    if (hostOperation.State == StateType.Running)
                    {
                        done = false;
                    }
                    else if (hostOperation.State == StateType.Failed)
                    {
                        Logger.Error(String.Format("Host {0}: {1}", hostOperation.Host, hostOperation.FailureMessage));
                    }
                }
                if (!done)
                {
                    Thread.Sleep(PollInterval);
                }
            }
        }

        #endregion
    }

    internal class HostOperation
    {
        private string _host;
        private BatchStatusType _status;
        private string _token;

        public HostOperation(string host, string token, BatchStatusType status)
        {
            _host = host;
            _token = token;
            _status = status;
        }

        public HostOperation(string host, string token)
        {
            _host = host;
            _token = token;
            _status = null;
        }

        public string Host
        {
            get { return _host; }
            set { _host = value; }
        }

        public string Token
        {
            get { return _token; }
            set { _token = value; }
        }

        public BatchStatusType Status
        {
            get { return _status; }
            set { _status = value; }
        }

        /// <remarks />
        public StateType State
        {
            get { return _status.state; }
        }

        /// <remarks />
        public string FailureMessage
        {
            get { return _status.failureMessage; }
        }

        public override string ToString()
        {
            return string.Format("{0}:{1} - {2}", _host, _token, State);
        }
    }
}
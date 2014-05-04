#region Using Directives

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using EndecaControl.EacToolkit.Components;
using EndecaControl.EacToolkit.Core;
using EndecaControl.EacToolkit.Services;

#endregion

namespace Endeca.Control.EacToolkit
{
    public class EndecaApplication : EacElement
    {
        private readonly ComponentCollection<DgraphComponent> dgraphs = new ComponentCollection<DgraphComponent>();
        private readonly ComponentCollection<ForgeComponent> forges = new ComponentCollection<ForgeComponent>();
        private readonly HostCollection hosts = new HostCollection();
        private readonly LockManager lockManager;
        private ApplicationType appConfig;
        private DgidxComponent dgidx;
        private LogServerComponent logServer;

        public EndecaApplication(string appId, string eacHost, int eacPort) : base(appId, eacHost, eacPort)
        {
            EacGateway.CreateEacGateway(eacHost, eacPort);
            lockManager = new LockManager(appId);
        }

        public ApplicationType AppConfig
        {
            get { return appConfig; }
        }

        public ComponentCollection<ForgeComponent> Forges
        {
            get { return forges; }
        }

        public DgidxComponent Dgidx
        {
            get { return dgidx; }
        }

        public ComponentCollection<DgraphComponent> Dgraphs
        {
            get { return dgraphs; }
        }

        public LogServerComponent LogServer
        {
            get { return logServer; }
        }

        public HostCollection Hosts
        {
            get { return hosts; }
        }


        /// <summary>
        ///     Loads application from the host
        /// </summary>
        public void LoadApplication()
        {
            if (!IsDefined())
            {
                throw new EndecaApplicationException(String.Format("Application {0} is not defined!", AppId));
            }
            appConfig = EacGateway.Instance.GetApplication(AppId);
            LoadComponents();
        }

        /// <summary>
        ///     Loads application from configuration file
        /// </summary>
        /// <remarks>
        ///     Configuration file produced by the eaccmd is not compatible
        /// </remarks>
        /// <param name="fileName">Config file name</param>
        public void LoadApplicationFromXml(string fileName)
        {
            var ser = new XmlSerializer(typeof (ApplicationType));
            using (var fs = new FileStream(fileName, FileMode.Open))
            {
                appConfig = ser.Deserialize(fs) as ApplicationType;
            }

            if (appConfig == null || string.IsNullOrEmpty(appConfig.applicationID) ||
                appConfig.hosts == null || appConfig.hosts.Length == 0 ||
                appConfig.components == null || appConfig.components.Length < 3)
            {
                throw new EndecaApplicationException("Invalid application configuration");
            }
            LoadComponents();
        }

        /// <summary>
        ///     true if the application is defined on the host
        /// </summary>
        /// <returns></returns>
        public bool IsDefined()
        {
            var apps = EacGateway.Instance.ListApplicationIDs();
            return Array.Find(apps, delegate(string apId) { return apId == AppId; }) != null;
        }

        /// <summary>
        ///     Serializes instance of Endeca ApplicationType to file
        /// </summary>
        /// <param name="fileName">Name of the file.</param>
        public void SaveAppConfig(string fileName)
        {
            var ser = new XmlSerializer(typeof (ApplicationType));
            using (var fs = new FileStream(fileName, FileMode.Open))
            {
                ser.Serialize(fs, appConfig);
            }
        }

        /// <summary>
        ///     Acquires an update lock
        /// </summary>
        /// <returns>False if lock is already set</returns>
        public bool AcquireUpdateLock()
        {
            return lockManager.AcquireLock(LockManager.UpdateFlag);
        }

        /// <summary>
        ///     Releases update lock
        /// </summary>
        public void ReleaseUpdateLock()
        {
            lockManager.ReleaseLock(LockManager.UpdateFlag);
        }

        /// <summary>
        ///     Releases all locks
        /// </summary>
        public void ReleaseAllLocks()
        {
            lockManager.ReleaseAllLocks();
        }

        private void LoadComponents()
        {
            foreach (var host in appConfig.hosts)
            {
                Hosts.Add(ComponentFactory.CreateHostComponent(appConfig, host));
            }
            foreach (var comp in appConfig.components)
            {
                if (comp is ForgeComponentType)
                {
                    Forges.Add(ComponentFactory.CreateForgeComponent(appConfig, comp as ForgeComponentType));
                }
                else if (comp is DgidxComponentType)
                {
                    dgidx = ComponentFactory.CreateDgidxComponent(appConfig, comp as DgidxComponentType);
                }
                else if (comp is DgraphComponentType)
                {
                    Dgraphs.Add(ComponentFactory.CreateDgraphComponent(appConfig, comp as DgraphComponentType));
                }
                else if (comp is LogServerComponentType)
                {
                    logServer = ComponentFactory.CreateLogServerComponent(appConfig, comp as LogServerComponentType);
                }
            }
        }


        /// <summary>
        ///     Defines endeca application loaded from config file on the host
        /// </summary>
        /// <returns>Provisioning warnings from EAC</returns>
        /// <exception cref="EndecaApplicationException">
        ///     If application is not defined
        /// </exception>
        public ProvisioningWarningType[] Define()
        {
            var warnings = new List<ProvisioningWarningType>();
            if (IsDefined())
            {
                throw new EndecaApplicationException(String.Format("Appliaction {0} is defined. Remove it first!", AppId));
            }

            warnings.AddRange(EacGateway.Instance.AddApplication(appConfig));
            return warnings.ToArray();
        }

        /// <summary>
        ///     Removed
        /// </summary>
        /// <returns></returns>
        public ProvisioningWarningType[] Remove()
        {
            var warnings = new List<ProvisioningWarningType>();
            if (IsDefined())
            {
                warnings.AddRange(EacGateway.Instance.RemoveApplication(AppId, true));
            }
            return warnings.ToArray();
        }

        public string GetConfiguration()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("'{0}' application configuration\r\n", AppId);
            sb.AppendFormat("Endeca Application Controler {0}:{1}\r\n", EacHostName, EacPort);
            sb.Append("Hosts:\r\n");
            foreach (var host in hosts)
            {
                sb.AppendFormat("  {0} on {1}\r\n", host.HostId, host.EacHostName);
            }
            sb.AppendFormat("Components:\r\n");
            sb.AppendFormat("Forges:\r\n");
            if (forges != null)
            {
                foreach (var forge in forges)
                {
                    sb.AppendFormat("   {0} on {1}\r\n", forge.ComponentId, forge.HostId);
                }
            }
            sb.AppendFormat("Dgidx:\r\n");
            if (dgidx != null)
            {
                sb.AppendFormat("   {0} on {1}\r\n", Dgidx.ComponentId, Dgidx.HostId);
            }
            sb.AppendFormat("Dgraphs:\r\n");
            if (dgraphs != null)
            {
                foreach (var dgraph in dgraphs)
                {
                    sb.AppendFormat("   {0} on {1}:{2}\r\n", dgraph.ComponentId, dgraph.HostName, dgraph.Port);
                }
            }
            return sb.ToString();
        }
    }
}
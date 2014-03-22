using System.Collections.ObjectModel;
using System.Threading;

namespace Indigo.Endeca.EacToolkit
{
    

    public abstract class EndecaServiceComponent<CType> : EndecaComponent<CType> where CType : ComponentType
    {
        public EndecaServiceComponent(CType component, EndecaApplication app) : base(component, app)
        {
        }

        /// <summary>
        /// Returns true if a component's status is Starting. Only makes sense for a service type components.
        /// </summary>
        public bool IsStarting
        {
            get { return Status.state == StateType.Starting; }
        }

        /// <summary>
        /// Pauses a thread until a component's status is Starting.
        /// </summary>
        public void WaitStarting()
        {
            while (IsStarting)
            {
                Thread.Sleep(waitInterval);
            }
        }
    }

    public class Forge : EndecaComponent<ForgeComponentType>
    {
        public Forge(ForgeComponentType component, EndecaApplication app) : base(component, app)
        {
        }
    }

    public class Dgidx : EndecaComponent<DgidxComponentType>
    {
        public Dgidx(DgidxComponentType component, EndecaApplication app) : base(component, app)
        {
        }

        public string OutputDir
        {
            get { return ResolveRelativePath(RemovePrefixNameFromDir(component.outputPrefix)); }
        }
    }

    public class Dgraph : EndecaServiceComponent<DgraphComponentType>
    {
        public Dgraph(DgraphComponentType component, EndecaApplication app) : base(component, app)
        {
        }

        public string InputDir
        {
            get { return ResolveRelativePath(RemovePrefixNameFromDir(component.inputPrefix)); }
        }

        /// <summary>
        /// Starts copying files from a given location to the Dgraph's working folder and waits until the operation is completed.
        /// </summary>
        /// <param name="fromPath">Source location</param>
        /// <returns>Operation BatchStatusType value.</returns>
        /// <exception cref="EndecaApplicationException">Throws an EndecaApplicationException if the operation has failed.</exception>
        public BatchStatusType FetchIndexFiles(string fromPath)
        {
            if (!fromPath.EndsWith("\\*"))
            {
                fromPath += "\\*";
            }
            string token = app.EacGateway.StartCopyFiles(app.AppId, Host, fromPath, Host, InputDir, false);
            return WaitBatchOperationComplete(token);
        }

        /// <summary>
        /// Move the current index's files to a backup folder. Cleans up the Dgraph working directory. Requires to stop Dgraph before
        /// invoking this method.
        /// </summary>
        /// <param name="numBackups"></param>
        public BatchStatusType Backup(int numBackups)
        {
            string token = app.EacGateway.StartBackupFiles(app.AppId, Host, InputDir, numBackups, BackupMethodType.Move);
            return WaitBatchOperationComplete(token);
        }

        protected BatchStatusType WaitBatchOperationComplete(string token)
        {
            while (true)
            {
                BatchStatusType status = app.EacGateway.GetUtilityStatus(app.AppId, token);
                if (status.state == StateType.NotRunning)
                {
                    return status;
                }
                if (status.state == StateType.Failed)
                {
                    throw new EndecaApplicationException(Id, status);
                }
                Thread.Sleep(WaitInterval);
            }
        }
    }

    public class DgraphCollection : KeyedCollection<string, Dgraph>
    {
        protected override string GetKeyForItem(Dgraph item)
        {
            return item.Id;
        }
    }
}
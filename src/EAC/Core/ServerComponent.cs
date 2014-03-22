#region Using Directives

using System.Threading;

#endregion

namespace Endeca.Control.EacToolkit
{
    public abstract class ServerComponent : Component
    {
        protected string hostName;
        protected int port;

        protected ServerComponent(string compId, string appId, HostType host) : base(compId, appId, host)
        {
        }

        /// <summary>
        ///     Server Component Port
        /// </summary>
        public int Port
        {
            get { return port; }
            set { port = value; }
        }

        /// <summary>
        ///     Server Component Host
        /// </summary>
        public string HostName
        {
            get { return hostName; }
            set { hostName = value; }
        }

        /// <summary>
        ///     Returns true if a component's status is Starting.
        /// </summary>
        public bool IsStarting
        {
            get
            {
                var statusType = GetStatus();
                return statusType.state == StateType.Starting;
            }
        }

        public void Start(bool waitComplete)
        {
            StartComponent(false);
            if (waitComplete)
            {
                while (IsStarting)
                {
                    Thread.Sleep(WaitInterval);
                }
            }
        }
    }
}
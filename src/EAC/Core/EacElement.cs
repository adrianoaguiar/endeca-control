#region Using Directives

using System;

#endregion

namespace Endeca.Control.EacToolkit
{
    /// <summary>
    ///     Root class from which all EAC classes inherit. Stores an application
    ///     name and the host and port of the EAC central server.
    /// </summary>
    public abstract class EacElement
    {
        private readonly String appId;

        private readonly String eacHostName;


        private readonly int eacPort;

        protected EacElement(string appId, string eacHostName, int eacPort)
        {
            this.appId = appId;
            this.eacHostName = eacHostName;
            this.eacPort = eacPort;
        }

        /// <summary>
        ///     Name of the application
        /// </summary>
        public String AppId
        {
            get { return appId; }
        }

        /// <summary>
        ///     Hostname of the EAC central server
        /// </summary>
        public String EacHostName
        {
            get { return eacHostName; }
        }

        /// <summary>
        ///     Port on which the EAC central server is listening
        /// </summary>
        protected int EacPort
        {
            get { return eacPort; }
        }

        public override string ToString()
        {
            return String.Format("{0} {1}:{2}", AppId, EacHostName, EacPort);
        }
    }
}
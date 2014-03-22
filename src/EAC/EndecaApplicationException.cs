#region Using Directives

using System;

#endregion

namespace Endeca.Control.EacToolkit
{
    [Serializable]
    public class EndecaApplicationException : Exception
    {
        private string appId;
        private string component;

        public EndecaApplicationException()
        {
        }

        public EndecaApplicationException(string message) : base(message)
        {
        }

        public EndecaApplicationException(string appId, string message)
            : this(message)
        {
            this.appId = appId;
        }

        public EndecaApplicationException(string appId, string component, string message)
            : this(appId, message)
        {
            this.component = component;
        }
    }
}
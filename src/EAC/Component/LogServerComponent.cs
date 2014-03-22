#region Using Directives

using System;
using System.Net;

#endregion

namespace Endeca.Control.EacToolkit
{
    public class LogServerComponent : ServerComponent
    {
        internal LogServerComponent(string compId, string appId, HostType host) : base(compId, appId, host)
        {
        }

        public bool RollLog()
        {
            Logger.Debug(String.Format("{0}:{1} - Rolling log server log.", hostName, port));
            var req = String.Format("http://{0}:{1}/roll", HostName, Port);
            var updateReq = (HttpWebRequest) WebRequest.Create(req);
            updateReq.Credentials = CredentialCache.DefaultCredentials;
            updateReq.Timeout = 200000;
            try
            {
                var response = (HttpWebResponse) updateReq.GetResponse();
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (WebException e)
            {
                Logger.Error("Roll Log request", e);
            }
            return false;
        }
    }
}
namespace EndecaControl.EacToolkit.Core
{
    public class Host : EacElement
    {
        internal Host(string appId, string eacHostName, string hostId, int eacPort) : base(appId, eacHostName, eacPort)
        {
            HostId = hostId;
        }

        public string HostId { get; set; }
    }
}
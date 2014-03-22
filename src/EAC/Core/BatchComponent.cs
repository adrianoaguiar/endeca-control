namespace Endeca.Control.EacToolkit
{
    public abstract class BatchComponent : Component
    {
        protected BatchComponent(string compId, string appId, HostType host)
            : base(compId, appId, host)
        {
        }

        public string OutputDirectory { get; set; }

        public virtual void Run()
        {
            StartComponent(true);
        }

        public virtual void Stop()
        {
            StopComponent(true);
        }

        public override void CleanDirs()
        {
            CleanDir(OutputDirectory);
        }
    }
}
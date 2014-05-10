using System;
using EndecaControl.EacToolkit.Core;

namespace EndecaControl.EacToolkit.Components
{
    public sealed class DgidxComponent : BatchComponent
    {
        internal DgidxComponent(string compId, string appId, HostType host)
            : base(compId, appId, host)
        {
        }

        public bool ArchiveIndex()
        {
            int numIndexBackups = Int32.TryParse(CustomProps["numIndexBackups"], out numIndexBackups) && numIndexBackups > 0 ?
                                numIndexBackups : 1;

            var token = BackupFiles(OutputDirectory.Substring(0, OutputDirectory.LastIndexOf(@"\")), numIndexBackups);
            return WaitUtilityComplete(token);
        }

        public bool RollbackIndex()
        {
            var token = RollbackFiles(OutputDirectory.Substring(0, OutputDirectory.LastIndexOf(@"\")));
            return WaitUtilityComplete(token);
        }
    }
}
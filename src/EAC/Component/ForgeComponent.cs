#region Using Directives

using System;
using System.IO;

#endregion

namespace Endeca.Control.EacToolkit
{
    public sealed class ForgeComponent : BatchComponent
    {
        private const string UPDATE_FILENAME_TEMPLATE = "{0}-sgmt0.records.xml";

        internal ForgeComponent(string compId, string appId, HostType host)
            : base(compId, appId, host)
        {
        }


        public string StampPartialUpdate()
        {
            var fileName = String.Format(UPDATE_FILENAME_TEMPLATE, DataPrefix);
            var newName = String.Format("{0}_{1}", DateTime.Now.ToString("yyyyMMddHHmmss"), fileName);
            Logger.Debug(String.Format("Stamping update file {0} to {1}.", fileName, newName));
            var token = ShellCmd(String.Format("ren {0} {1}", Path.Combine(OutputDirectory, fileName), newName));
            WaitUtilityComplete(token);
            return newName;
        }
    }
}
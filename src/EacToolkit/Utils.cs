#region Using Directives

using System;
using System.Diagnostics;
using System.IO;

#endregion

namespace Endeca.Control.EacToolkit
{
    public static class Utils
    {
        public static void ClearFolder(string folder)
        {
            foreach (var file in Directory.GetFiles(Environment.ExpandEnvironmentVariables(folder)))
            {
                File.Delete(file);
            }
        }

        public static void CopyFiles(string fromFolder, string toFolder)
        {
            foreach (var file in Directory.GetFiles(Environment.ExpandEnvironmentVariables(fromFolder)))
            {
                var dest = Path.Combine(Environment.ExpandEnvironmentVariables(toFolder), Path.GetFileName(file));
                File.Copy(file, dest);
            }
        }

        public static void Exec(string app, string args)
        {
            Debug.Assert(app != null);

            var process = new Process();
            process.StartInfo.FileName = Environment.ExpandEnvironmentVariables(app);
            if (args != null)
            {
                process.StartInfo.Arguments = Environment.ExpandEnvironmentVariables(args);
            }
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.Start();
            process.WaitForExit();
        }
    }
}
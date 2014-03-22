#region Using Directives

using System;
using System.Threading;
using Endeca.Control.EacToolkit;

#endregion

namespace CSS_TestHarness
{
    internal class ControlScript
    {
        private const int port = 8888;
        private const string appName = "appname";


        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: css_testharness <server> <interval_minutes>");
                return 1;
            }
            var server = args[0];
            if (server.Equals(".") || server.Equals("local") || server.Equals("localhost"))
                server = Environment.MachineName;

            var app = new EndecaApplication(appName, server, port);
            Logger.Info(String.Format("Loading {0} application on {1}:{2} ...", appName, server, port));

            var sleepTime = 0;
            if (!Int32.TryParse(args[1], out sleepTime))
            {
                sleepTime = 10;
            }

            try
            {
                app.LoadApplication();
                Logger.Info(app.GetConfiguration());
                Logger.Info("Staring tests. Press any key to terminate ...");

                StartTest(app, sleepTime);
                return 0;
            }
            catch (Exception e)
            {
                var msg = string.Format("Script failed:\r\n{0}", e);
                Logger.Fatal(msg);
                return 1;
            }
        }


        private static void StartTest(EndecaApplication app, int sleepInterval)
        {
            var cluster = new DgraphCluster(app);
            while (Console.KeyAvailable == false)
            {
                Logger.Info("Starting test ...");
                Logger.Info("Distributing index to remote hosts...");
                cluster.DistributeIndex();

                foreach (var dgraph in app.Dgraphs)
                {
                    Logger.Info(String.Format("Applying index on {0}:{1} dgraph instance.", dgraph.HostName, dgraph.Port));
                    if (dgraph.ApplyIndex())
                    {
                        Logger.Info(String.Format("{0}:{1} - Dgraph started.", dgraph.HostName, dgraph.Port));
                    }
                    else
                    {
                        Logger.Info(String.Format("{0}:{1} - Dgraph FAILED to start!!!", dgraph.HostName, dgraph.Port));
                    }
                }
                Logger.Info("Test Complete!. Sleeping for " + sleepInterval.ToString());
                Thread.Sleep(sleepInterval*60*1000);
            }
        }
    }
}
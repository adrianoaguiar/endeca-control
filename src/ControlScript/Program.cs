#region Using Directives

using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using Endeca.Control.EacToolkit;
using EndecaControl.EacToolkit.Components;
using log4net.Repository.Hierarchy;
using Logger = Endeca.Control.EacToolkit.Logger;

#endregion

namespace EndecaControl.ControlScript
{
    internal class ControlScript
    {
        #region Private Properties

        private static readonly string HelpTitle = ConfigurationManager.AppSettings["help_title"];
        private static readonly string HelpBody = ConfigurationManager.AppSettings["help_body"];

        private static readonly string BaselineForge = ConfigurationManager.AppSettings["baseline_forge"];
        private static readonly string PartialForge = ConfigurationManager.AppSettings["partial_forge"];
        private static readonly int Port = Int32.Parse(ConfigurationManager.AppSettings["port"]);
        private static readonly string AppName = ConfigurationManager.AppSettings["app_name"];
        private static readonly string IndexTestHostId = ConfigurationManager.AppSettings["index_test_host_id"];

        private static readonly int PauseBetweenUpdates =
            Int32.Parse(ConfigurationManager.AppSettings["pauseBetweenDgraphUpdates"]); 
        
        #endregion

        private static int Main(string[] args)
        {
            if (args.Length < 2)
            {
                DisplayHelp();
                return 1;
            }
            var server = args[0];
            if (server.Equals(".") || server.Equals("local") || server.Equals("localhost"))
                server = Environment.MachineName;

            var app = new EndecaApplication(AppName, server, Port);
            Logger.Info(String.Format("Loading {0} application on {1}:{2} ...", AppName, server, Port));
            Console.CancelKeyPress += delegate
                {
                    Logger.Warn("Script has been cancelled. Current operation is in progress and will complete!");
                    app.ReleaseAllLocks();
                };

            if (args[1] == "/r")
            {
                app.ReleaseAllLocks();
                Console.WriteLine("Locks released!");
                return 0;
            }
            if (!app.AcquireUpdateLock())
            {
                Logger.Error("Cannot acquire lock. Update probably in progress!");
                return 1;
            }

            try
            {
                app.LoadApplication();

                Logger.Notify("Index update started.\r\n\r\nData feed contents:\r\n\r\n" +
                              GetDirectoryListing(args, app));

                Logger.Info(app.GetConfiguration());

                // start log server if defined and down
                StartLogServer(app);

                switch (args[1])
                {
                    case "/baseline_update":
                    case "/b":
                        BaselineUpdate(app, true);
                        break;
                    case "/partial_update":
                    case "/p":
                        PartialUpdate(app);
                        break;
                    case "/u":
                    case "/update_without_applying":
                        BaselineUpdate(app, false);
                        break;
                    case "/a":
                    case "/apply_index":
                        ApplyIndex(app);
                        break;
                    default:
                        Console.WriteLine(
                            "/b[aseline_update], /p[artial_update], /u[pdate_without_applying] or /a[pply_index expected");
                        return 1;
                }
                Logger.Info("Script finished");
                Logger.Notify("Index successfuly updated.\r\n\r\nSession Log:\r\n" + Logger.GetLog());
                return 0;
            }
            catch (Exception e)
            {
                var msg = string.Format("Script failed:\r\n{0}", e);
                Logger.Fatal(msg);
                Logger.NotifyOnError(msg);
                return 1;
            }
            finally
            {
                app.ReleaseAllLocks();
            }
        }

        private static void DisplayHelp()
        {
            Console.WriteLine(HelpTitle);
            Console.WriteLine(HelpBody);
        }

        private static string GetDirectoryListing(string[] args, EndecaApplication app)
        {
            var updateType = ((args[1] == "/p") || (args[1] == "/partial_update")) ? PartialForge : BaselineForge;
            var path = app.Forges[updateType].InputDirectory.Replace('/', '\\');
            var listing = string.Empty;

            if (Directory.Exists(path))
            {
                var dir = new DirectoryInfo(path);
                foreach (var f in dir.GetFiles("*.txt"))
                {
                    listing += string.Format("[{0}] - {1:0,0} bytes - {2:d/MMM/yyyy - HH:mm:ss}\r\r\n", f.Name, f.Length,
                                             f.LastWriteTime);
                }
            }

            return string.IsNullOrEmpty(listing)
                       ? string.Format("No input files found in {0}.", path)
                       : string.Format("Path: {0}\r\n\r\n{1}", path, listing);
        }

        private static void StartLogServer(EndecaApplication app)
        {
            if (app.LogServer != null && !app.LogServer.IsActive)
            {
                app.LogServer.Start(false);
            }
        }

        private static void PartialUpdate(EndecaApplication app)
        {
            /// Partial updates scenario
            /// 1. Distribute update
            /// 2. Apply update
            /// 2.1. Apply update
            /// 2.2. Copy update file to cumulative updates folder
            /// 2.3. Clean updates foder

            Logger.Info("Partial update in progress ...");
            var forge = app.Forges[PartialForge];
            Debug.Assert(forge != null);
            forge.ArchiveLog(false);

            Logger.Info("Forging...");
            forge.Run();
            if (forge.IsFailed)
            {
                throw new ControlScriptException(forge.FailureMessage);
            }


            var cluster = new DgraphCluster(app);

            Logger.Info("Distributing update to remote hosts...");
            cluster.DistributeUpdate(forge);

            Logger.Info("Applying updates ...");
            cluster.ApplyUpdate();

            Logger.Info("Update complete!");
        }

        private static void BaselineUpdate(EndecaApplication app, bool applyNewIndex)
        {
            Logger.Info("Baseline update in progress ...");

            RunForge(app.Forges[BaselineForge]);

            RunDgidx(app.Dgidx);

            var cluster = new DgraphCluster(app);

            /// Baseline update scenario
            /// 1. Clean local temp folder 
            /// 2. Clean local updates folder
            /// 3. Distrubute index
            /// 4. Apply index
            /// 5. Archive updates folder

            CleanFoldersAndDistributeIndex(cluster);

            TestIndex(cluster);

            if (applyNewIndex)
            {
                ApplyIndex(app);
            }
            /*Logger.Info("Sending post-forge dimensions to Web Studio ...");
			Utils.Exec(@"%ENDECA_ROOT%\perl\5.8.3\bin\perl.exe",
					   @"%ENDECA_ROOT%\bin\emgr_update.pl --host localhost:8888 --app_name appname --prefix appname --action set_post_forge_dims --post_forge_file %APP_ROOT%\data\forge_output\appname.dimensions.xml");
            */
            Logger.Info("Baseline update complete!");
        }

        private static void RollLogServerLog(EndecaApplication app)
        {
            Logger.Info("Rolling log server log ...");
            if (app.LogServer != null && app.LogServer.IsActive)
            {
                app.LogServer.RollLog();
            }
        }

        private static void ApplyIndex(EndecaApplication app)
        {
            var cluster = new DgraphCluster(app);

            Logger.Info("Applying index ...");
            cluster.ApplyIndex(PauseBetweenUpdates);

            RollLogServerLog(app);
        }

        private static void TestIndex(DgraphCluster cluster)
        {
            Logger.Info("Testing index ...");
            var indexOk = cluster.ApplyIndex(new[] {IndexTestHostId});
            if (!indexOk)
            {
                throw new ControlScriptException(
                    String.Format("Index test failed on {0}. At least one Dgraph instance failed to start.",
                                  IndexTestHostId));
            }
        }

        private static void CleanFoldersAndDistributeIndex(DgraphCluster cluster)
        {
            Logger.Info("Cleaning local distribution folder on remote hosts ...");
            cluster.CleanLocalIndexDistributionDir();

            Logger.Info("Cleaning local update folder on remote hosts ...");
            cluster.CleanLocalUpdatesDir();

            Logger.Info("Distributing index to remote hosts...");
            cluster.DistributeIndex();
        }

        private static void RunDgidx(DgidxComponent dgidx)
        {
            //backup old index (this will automatically clear dgidx output folder)
            Logger.Info("Archiving Previous Index ...");
            dgidx.ArchiveIndex();

            Logger.Info("Indexing...");
            dgidx.ArchiveLog(true);
            dgidx.Run();

            if (dgidx.IsFailed)
            {
                throw new ControlScriptException(dgidx.FailureMessage);
            }
        }

        private static void RunForge(ForgeComponent forge)
        {
            Logger.Info("Forging...");
            forge.ArchiveLog(true);
            forge.CleanDirs();
            forge.Run();

            if (forge.IsFailed)
            {
                throw new ControlScriptException(forge.FailureMessage);
            }
        }
    }
}
#region Using Directives

using System;
using System.IO;
using EndecaControl.EacToolkit.Core;

#endregion

namespace EndecaControl.EacToolkit.Components
{
    public static class ComponentFactory
    {
        public static Host CreateHostComponent(ApplicationType appType, HostType hostType)
        {
            return new Host(appType.applicationID, hostType.hostname, hostType.hostID, hostType.port);
        }

        public static DgidxComponent CreateDgidxComponent(ApplicationType appType, DgidxComponentType dgidx)
        {
            var ht = GetHost(appType, dgidx.hostID);
            var component = new DgidxComponent(dgidx.componentID, appType.applicationID, ht)
                {
                    WorkingDir = dgidx.workingDir,
                    OutputDirectory = ResolveRelativePath(dgidx.workingDir,
                                                          RemovePrefixNameFromDir(dgidx.outputPrefix)),
                    InputDirectory = ResolveRelativePath(dgidx.workingDir, RemovePrefixNameFromDir(dgidx.inputPrefix)),
                    LogDir = ResolveRelativePath(dgidx.workingDir, Path.GetDirectoryName(dgidx.logFile)),
                    DataPrefix = GetDataPrefixFromDir(dgidx.outputPrefix)
                };
            LoadCustomProperties(component, dgidx);
            return component;
        }

        public static ForgeComponent CreateForgeComponent(ApplicationType appType, ForgeComponentType forge)
        {
            var ht = GetHost(appType, forge.hostID);
            var component = new ForgeComponent(forge.componentID, appType.applicationID, ht)
                {
                    OutputDirectory = ResolveRelativePath(forge.workingDir, forge.outputDir),
                    InputDirectory = ResolveRelativePath(forge.workingDir, forge.inputDir),
                    LogDir = ResolveRelativePath(forge.workingDir, Path.GetDirectoryName(forge.logFile)),
                    WorkingDir = forge.workingDir,
                    DataPrefix = forge.outputPrefixName
                };
            LoadCustomProperties(component, forge);
            return component;
        }

        public static DgraphComponent CreateDgraphComponent(ApplicationType appType, DgraphComponentType dgraph)
        {
            var ht = GetHost(appType, dgraph.hostID);
            var component = new DgraphComponent(dgraph.componentID, appType.applicationID, ht)
                {
                    WorkingDir = dgraph.workingDir,
                    LogDir = ResolveRelativePath(dgraph.workingDir, Path.GetDirectoryName(dgraph.logFile)),
                    InputDirectory = ResolveRelativePath(dgraph.workingDir,
                                                         RemovePrefixNameFromDir(dgraph.inputPrefix)),
                    UpdateDir = ResolveRelativePath(dgraph.workingDir, dgraph.updateDir),
                    UpdateLogDir = ResolveRelativePath(dgraph.workingDir, Path.GetDirectoryName(dgraph.updateLogFile))
                };
            LoadCustomProperties(component, dgraph);
            component.IndexDistributionDir = ResolveRelativePath(dgraph.workingDir,
                                                                 component.CustomProps["localIndexDir"]);
            component.DataPrefix = GetDataPrefixFromDir(dgraph.inputPrefix);
            component.Port = dgraph.port;
            component.HostName = ht.hostname;

            return component;
        }

        public static LogServerComponent CreateLogServerComponent(ApplicationType appType,
                                                                  LogServerComponentType logServer)
        {
            var ht = GetHost(appType, logServer.hostID);
            var component = new LogServerComponent(logServer.componentID, appType.applicationID, ht)
                {
                    WorkingDir = logServer.workingDir,
                    Port = logServer.port,
                    HostName = ht.hostname
                };
            return component;
        }

        private static HostType GetHost(ApplicationType app, string hostId)
        {
            return Array.Find(app.hosts, delegate(HostType ht) { return ht.hostID == hostId; });
        }

        private static void LoadCustomProperties(Component component, ComponentType endecaComponent)
        {
            foreach (var property in endecaComponent.properties)
            {
                component.CustomProps.Add(property.name, property.value);
            }
        }

        private static string ResolveRelativePath(string root, string relativePath)
        {
            if (relativePath.StartsWith("."))
            {
                relativePath = relativePath.TrimStart('.');
            }
            else if (relativePath.StartsWith("\\"))
            {
                relativePath = relativePath.TrimStart('\\');
            }
            return (root + relativePath).Replace("/", "\\");
        }

        private static string RemovePrefixNameFromDir(string dir)
        {
            var parts = dir.Split('\\');
            if (parts.Length > 2)
            {
                return string.Join("\\", parts, 0, parts.Length - 1);
            }
            return parts.Length == 2 ? parts[0] : dir;
        }

        private static string GetDataPrefixFromDir(string dir)
        {
            var parts = dir.Split('\\');
            var index = parts.Length - 1;
            return parts[index];
        }
    }
}
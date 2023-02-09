using EnvDTE;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.InteropServices;
using TCatSysManagerLib;

namespace Twinlib
{
    internal class AutomationInterface
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        static DTE CreateDte(out IOleMessageFilter oldFilter, bool hidden = true)
        {
            logger.Info("Creating DTE");

            IOleMessageFilter filter = new MessageFilter();
            CoRegisterMessageFilter(filter, out oldFilter);
            Type t = System.Type.GetTypeFromProgID("TcXaeShell.DTE.15.0");

            DTE dte = (DTE)Activator.CreateInstance(t);
            dte.SuppressUI = hidden;
            dte.MainWindow.Visible = !hidden;
            var settings = dte.GetObject("TcAutomationSettings");
            settings.SilentMode = hidden;

            return dte;
        }

        static public Repository.Platform LatestLibraries(List<string> distributorFilter=null)
        {
            IOleMessageFilter oldFilter;
            var dte = CreateDte(out oldFilter);
            Solution solution = dte.Solution;

            logger.Info("Creating Dummy Solution");
            string outputPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(outputPath);
            try
            {
                solution.Create($"{outputPath}", $"DummyProject");
                solution.SaveAs($@"{outputPath}\DummyProject.sln");
                dynamic prj = solution.AddFromTemplate(@"C:\TwinCAT\3.1\Components\Base\PrjTemplate\TwinCAT Project.tsproj", $@"{outputPath}\DummyProject", $"DummyProject");

                var remoteManager = dte.GetObject("TcRemoteManager");
                string[] versions = remoteManager.Versions;
                var platform = new Repository.Platform { Name = versions.OrderByDescending(x => Version.Parse(x)).First() };
                var libraries = new List<Repository.Library>();

                ITcSysManager systemManager = prj.Object;
                ITcSmTreeItem plc = systemManager.LookupTreeItem("TIPC");
                ITcProjectRoot plcproject = plc.CreateChild($"DummyProject", 0, "", @"Empty PLC Template") as ITcProjectRoot;
                var libManager = plcproject.NestedProject.LookupChild("References") as ITcPlcLibraryManager;
                foreach (ITcPlcLibrary r in libManager.ScanLibraries())
                {
                    libraries.Add(new Repository.Library
                    {
                        Name = r.Name,
                        LibraryVersion = Version.Parse(r.Version),
                        Distributor = r.Distributor
                    });
                }

                dte.Quit();
                CoRegisterMessageFilter(oldFilter, out _);

                if (distributorFilter != null)
                    platform.Libraries = libraries.Where(x => distributorFilter.Contains(x.Distributor));
                else
                    platform.Libraries = libraries;

                logger.Info($"Found/filtered {platform.Name} libraries for platform {platform.Libraries.Count()}");
                return platform;
            }
            finally
            {
                Directory.Delete(outputPath, true);
            }
        }


        [DllImport("Ole32.dll")]
        private static extern int CoRegisterMessageFilter(IOleMessageFilter newFilter, out IOleMessageFilter oldFilter);
    }
}

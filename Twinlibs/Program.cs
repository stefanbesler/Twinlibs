using System;
using System.Runtime.InteropServices;
using TCatSysManagerLib;
using EnvDTE;
using CommandLine;
using System.IO;
using System.Text.Json;
using NLog;
using System.Collections.Generic;
using System.Linq;

namespace Twinlib
{
    internal class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        [Verb("update")]
        public class UpdateOptions
        {
            [Option('d', "distributor-filter", Required = false, HelpText = "Filter libraries, defaults to 'System', 'Beckhoff Automation GmbH' ")]
            public IEnumerable<string> DistributorFilter { get; set; }

            [Option('m', "manifest", Default = "Twinlibs.manifest", Required = false, HelpText = "Path the manifest file that should be updated")]
            public string Manifest { get; set; }
        }

        [Verb("render")]
        class RenderOptions
        {
            [Option('o', "output", Default = "Twinlibs.md", Required = false, HelpText = "Path to a markdown file that should be rendered to")]
            public string Output { get; set; }

            [Option('m', "manifest", Default = "Twinlibs.manifest", Required = false, HelpText = "Path the manifest file that should be rendered")]
            public string Manifest { get; set; }
        }

        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<UpdateOptions, RenderOptions>(args)
                .MapResult(
                (UpdateOptions opts) =>
                {
                    Repository manifest = File.Exists(opts.Manifest) ?
                        JsonSerializer.Deserialize<Repository>(File.ReadAllText(opts.Manifest), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) : new Repository();

                    if (!opts.DistributorFilter.Any())
                        opts.DistributorFilter = new List<string> { "System", "Beckhoff Automation GmbH" };
                    logger.Info("Updating manifest file");

                    var platform = AutomationInterface.LatestLibraries(opts.DistributorFilter);
                    manifest.Platforms[platform.Name] = platform.Libraries;

                    string json = JsonSerializer.Serialize(platform, new JsonSerializerOptions { WriteIndented = true });
                    File.WriteAllText(opts.Manifest, json);
                    return 0;
                },
                (RenderOptions opts) =>
                {
                    Repository manifest = File.Exists(opts.Manifest) ?
                        JsonSerializer.Deserialize<Repository>(File.ReadAllText(opts.Manifest), new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) : new Repository();

                    var manifestTemplate = new Twinlibs.ManifestTemplate();
                    File.WriteAllText(opts.Output, manifestTemplate.TransformText());
                    return 0;
                }, errs => 1); ;
        }
    }
}

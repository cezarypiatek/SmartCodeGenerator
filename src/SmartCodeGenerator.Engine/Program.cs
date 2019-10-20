using System;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

namespace SmartCodeGenerator.Engine
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Console.WriteLine(((Exception) eventArgs.ExceptionObject).ToString());
            };

            var instances = MSBuildLocator.QueryVisualStudioInstances(new VisualStudioInstanceQueryOptions()
            {
                DiscoveryTypes = DiscoveryType.DotNetSdk | DiscoveryType.DeveloperConsole | DiscoveryType.VisualStudioSetup
            });
            var selectedMsBuildInstance = instances?.FirstOrDefault();
            if (selectedMsBuildInstance == null)
            {
                Console.WriteLine("Cannot find VisualStudio instance");
                return 1;
            }

            Console.WriteLine($"Selected msbuild {selectedMsBuildInstance.Name} {selectedMsBuildInstance.Version}");

            var options = ReadOptions(args);
            if (options == null)
            {
                return -1;
            }

            if (string.IsNullOrWhiteSpace(options.GeneratorPaths))
            {
                Console.WriteLine("No plugins provided. Generation skipped");
                return 0;
            }

            Console.WriteLine("Run with the following options:");
            Console.WriteLine($"{nameof(options.ProjectPath)} -> {options.ProjectPath}");
            Console.WriteLine($"{nameof(options.OutputPath)} -> {options.OutputPath}");
            Console.WriteLine($"{nameof(options.GeneratorPaths)} -> {options.GeneratorPaths}");

            var progressReporter = new ProgressReporter();
            
            MSBuildLocator.RegisterInstance(selectedMsBuildInstance);
            using (var workspace = MSBuildWorkspace.Create())
            {
                workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);
                var project = await workspace.OpenProjectAsync(options.ProjectPath, progressReporter);
                var generator = new CompilationGenerator(options.GeneratorPaths.Split(";"), options.OutputPath, progressReporter);
                await generator.Process(project);
            }
            return 0;
        }

        private static ToolOptions? ReadOptions(string[] args)
        {
            
            ToolOptions? result = null;
            Parser.Default.ParseArguments<ToolOptions>(args).WithParsed((options) => { result = options; }).WithNotParsed(
                errors =>
                {
                    //TODO: Display help
                });
            return result;
        }
    }
}

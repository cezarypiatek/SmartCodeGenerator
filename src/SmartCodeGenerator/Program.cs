using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;

namespace SmartCodeGenerator
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {

            AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
            {
                Console.WriteLine(((Exception) eventArgs.ExceptionObject).ToString());
            };

            var instance = MSBuildLocator.QueryVisualStudioInstances(new VisualStudioInstanceQueryOptions()
            {
                DiscoveryTypes = DiscoveryType.DotNetSdk | DiscoveryType.DeveloperConsole | DiscoveryType.VisualStudioSetup
            });
            if (instance == null)
            {
                Console.WriteLine("Cannot find VisualStudio instance");
                return 1;
            }

            var options = ReadOptions(args);
            if (options ==null)
            {
                return -1;
            }

            var progressReporter = new ProgressReporter();
            MSBuildLocator.RegisterInstance(instance.FirstOrDefault());
            using (var workspace = MSBuildWorkspace.Create())
            {
                workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);
                var project = await workspace.OpenProjectAsync(options.ProjectPath, progressReporter);
                var generator = new CompilationGenerator(new []{options.GeneratorPath}, options.OutputPath, progressReporter);
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

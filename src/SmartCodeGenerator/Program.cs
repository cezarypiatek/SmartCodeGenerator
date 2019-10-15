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


            MSBuildLocator.RegisterInstance(instance.FirstOrDefault());
            using (var workspace = MSBuildWorkspace.Create())
            {
                workspace.WorkspaceFailed += (o, e) => Console.WriteLine(e.Diagnostic.Message);
                var project = await workspace.OpenProjectAsync(options.ProjectPath, new ConsoleProgressReporter());

                var projectDirectory = Path.GetDirectoryName(options.ProjectPath)?? string.Empty;
                var generator = new CompilationGenerator(new []{options.GeneratorPath}, options.OutputPath, projectDirectory);
                await generator.Generate(project, new Progress<Diagnostic>(diagnostic =>
                {
                    
                }));
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

        private class ConsoleProgressReporter : IProgress<ProjectLoadProgress>
        {
            public void Report(ProjectLoadProgress loadProgress)
            {
                var projectDisplay = Path.GetFileName(loadProgress.FilePath);
                if (loadProgress.TargetFramework != null)
                {
                    projectDisplay += $" ({loadProgress.TargetFramework})";
                }

                Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
            }
        }
    }
}

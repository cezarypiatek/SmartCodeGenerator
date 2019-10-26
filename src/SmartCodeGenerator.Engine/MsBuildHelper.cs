using System.Collections.Generic;
using System.Linq;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis.MSBuild;

namespace SmartCodeGenerator.Engine
{
    static class MsBuildHelper
    {
        public static MSBuildWorkspace CreateMsBuildWorkspace(ProgressReporter progressReporter)
        {
            var properties = new Dictionary<string, string>()
            {
                ["SmartGeneratorProcessing"] = "true"
            };
            var msBuildWorkspace = MSBuildWorkspace.Create(properties);
            msBuildWorkspace.WorkspaceFailed += (o, e) => progressReporter.ReportInfo(e.Diagnostic.Message);
            return msBuildWorkspace;
        }

        public static VisualStudioInstance? GetMsBuildInstance(ProgressReporter progressReporter)
        {
            var instances = MSBuildLocator.QueryVisualStudioInstances(new VisualStudioInstanceQueryOptions()
            {
                DiscoveryTypes = DiscoveryType.DotNetSdk | DiscoveryType.DeveloperConsole | DiscoveryType.VisualStudioSetup
            }).ToList();

            progressReporter.ReportInfo("Available msbuild instances");
            foreach (var visualStudioInstance in instances)
            {
                progressReporter.ReportInfo($"Selected msbuild {visualStudioInstance.Name} {visualStudioInstance.Version}");
            }

            var selectedMsBuildInstance = instances.FirstOrDefault();
            if (selectedMsBuildInstance == null)
            {
                progressReporter.ReportInfo("Cannot find VisualStudio instance");
            }
            else
            {
                progressReporter.ReportInfo($"Selected msbuild {selectedMsBuildInstance.Name} {selectedMsBuildInstance.Version}");
            }
            return selectedMsBuildInstance;
        }
    }
}
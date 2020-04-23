using System.Collections.Generic;
using System.IO;
using System.Linq;
using SmartCodeGenerator.Core;

namespace SmartCodeGenerator.Engine
{
    internal static class ToolsOptionsExtensions
    {
        public static IReadOnlyList<string> GetGeneratorPluginsSearchPaths(this ToolOptions options, ProgressReporter progressReporter)
        {
            return options.GeneratorPaths.Split(";").Where(s =>
            {
                if (File.Exists(s))
                {
                    return true;
                }
                progressReporter.ReportInfo($"Cannot find generator plugin file: {s}");
                return false;
            }).Select(Path.GetFullPath).ToList();
        }
    }
}
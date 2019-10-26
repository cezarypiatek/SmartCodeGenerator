using System;
using CommandLine;

namespace SmartCodeGenerator.Engine
{
    internal static class OptionsHelper
    {
        public static ToolOptions? ReadOptions(string[] args)
        {
            ToolOptions? result = null;
            Parser.Default.ParseArguments<ToolOptions>(args).WithParsed((options) => { result = options; }).WithNotParsed(
                errors =>
                {
                    //TODO: Display help
                });
            return result;
        }

        private static void PrintInfoAboutParameters(ToolOptions options)
        {
            Console.WriteLine("Run with the following options:");
            Console.WriteLine($"{nameof(options.ProjectPath)} -> {options.ProjectPath}");
            Console.WriteLine($"{nameof(options.OutputPath)} -> {options.OutputPath}");
            Console.WriteLine($"{nameof(options.GeneratorPaths)} -> {options.GeneratorPaths}");
        }

        public static  ToolOptions?  LoadOptions(string[] args, ProgressReporter progressReporter)
        {
            var options = OptionsHelper.ReadOptions(args);
            if (options == null)
            {
                return null;
            }

            OptionsHelper.PrintInfoAboutParameters(options);

            if (string.IsNullOrWhiteSpace(options.GeneratorPaths))
            {
                progressReporter.ReportInfo("No plugins provided. Generation skipped");
                return null;
            }

            return options;
        }
    }
}
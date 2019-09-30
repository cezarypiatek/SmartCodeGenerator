using CommandLine;

namespace SmartCodeGenerator
{
    class ToolOptions
    {
        [Option('p', "project", Required = true, HelpText = "Project path")]
        public string ProjectPath { get; set; }

        [Option('o', "output", Required = true, HelpText = "Output path")]
        public string OutputPath { get; set; }

        [Option('g', "generator", Required = true, HelpText = "Generator path")]
        public string GeneratorPath { get; set; }
    }
}
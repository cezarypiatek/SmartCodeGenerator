using CommandLine;

namespace SmartCodeGenerator.Engine
{
    class ToolOptions
    {
        [Option('p', "project", Required = true, HelpText = "Project path")]
        public string ProjectPath { get; set; } = null!;

        [Option('o', "output", Required = true, HelpText = "Output path")]
        public string OutputPath { get; set; } = null!;

        [Option('g', "generators", Required = true, HelpText = "Generator paths")]
        public string GeneratorPaths { get; set; } = null!;
    }
}
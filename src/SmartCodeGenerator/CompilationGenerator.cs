// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MS-PL license. See LICENSE.txt file in the project root for full license information.

using SmartCodeGenerator;
using SmartCodeGenerator.PluginArchitectureDemo;

namespace CodeGeneration.Roslyn.Engine
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Text;

    /// <summary>
    /// Runs code generation for every applicable document and handles resulting syntax trees,
    /// saving them to <see cref="IntermediateOutputDirectory"/>.
    /// </summary>
    public class CompilationGenerator
    {
        private const string InputAssembliesIntermediateOutputFileName = "CodeGeneration.Roslyn.InputAssemblies.txt";
        private const int ProcessCannotAccessFileHR = unchecked((int)0x80070020);
        //private readonly List<string> emptyGeneratedFiles = new List<string>();
        //private readonly List<string> generatedFiles = new List<string>();
        private readonly List<string> loadedAssemblies = new List<string>();

        public CompilationGenerator(IReadOnlyList<string> generatorAssemblySearchPaths, string intermediateOutputDirectory, string projectDirectory)
        {
            GeneratorAssemblySearchPaths = generatorAssemblySearchPaths;
            IntermediateOutputDirectory = intermediateOutputDirectory;
            ProjectDirectory = projectDirectory;
        }

        /// <summary>
        /// Gets or sets the paths to directories to search for generator assemblies.
        /// </summary>
        public IReadOnlyList<string> GeneratorAssemblySearchPaths { get; }

        /// <summary>
        /// Gets or sets the path to the directory that contains generated source files.
        /// </summary>
        public string IntermediateOutputDirectory { get; }

        /// <summary>
        /// Gets or sets the directory with the project file.
        /// </summary>
        public string ProjectDirectory { get; }


        /// <summary>
        /// Runs the code generation as configured using this instance's properties.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="progress">Optional handler of diagnostics provided by code generator.</param>
        /// <param name="cancellationToken">Cancellation token to interrupt async operations.</param>
        public async Task Generate(Project project, IProgress<Diagnostic> progress,
            CancellationToken cancellationToken = default)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken) as CSharpCompilation;
            if (compilation == null)
            {
                return;
            }

            string generatorAssemblyInputsFile = Path.Combine(this.IntermediateOutputDirectory, InputAssembliesIntermediateOutputFileName);

            // For incremental build, we want to consider the input->output files as well as the assemblies involved in code generation.
            var assembliesLastModified = GetLastModifiedAssemblyTime(generatorAssemblyInputsFile);

            var fileFailures = new List<Exception>();

            using (var hasher = System.Security.Cryptography.SHA1.Create())
            {
                foreach (var document in project.Documents)
                {
                    
                    cancellationToken.ThrowIfCancellationRequested();

                    var documentFilePath = document.FilePath;
                    string sourceHash = Convert.ToBase64String(hasher.ComputeHash(Encoding.UTF8.GetBytes(documentFilePath)), 0, 6).Replace('/', '-');
                    string outputFilePath = Path.Combine(this.IntermediateOutputDirectory, Path.GetFileNameWithoutExtension(documentFilePath) + $".{sourceHash}.generated.cs");

                    // Code generation is relatively fast, but it's not free.
                    // So skip files that haven't changed since we last generated them.
                    DateTime outputLastModified = File.Exists(outputFilePath) ? File.GetLastWriteTime(outputFilePath) : DateTime.MinValue;
                    if (File.GetLastWriteTime(documentFilePath) > outputLastModified || assembliesLastModified > outputLastModified)
                    {
                        int retriesLeft = 3;
                        do
                        {
                            try
                            {
                                var generatedSyntaxTree = await DocumentTransform.TransformAsync(compilation, document, this.ProjectDirectory, this.LoadAssembly, progress, cancellationToken);

                                var outputText = generatedSyntaxTree.GetText(cancellationToken);
                                await using (var outputFileStream = File.OpenWrite(outputFilePath))
                                await using (var outputWriter = new StreamWriter(outputFileStream))
                                {
                                    outputText.Write(outputWriter, cancellationToken);

                                    // Truncate any data that may be beyond this point if the file existed previously.
                                    outputWriter.Flush();
                                    outputFileStream.SetLength(outputFileStream.Position);
                                }
                                break;
                            }
                            catch (IOException ex) when (ex.HResult == ProcessCannotAccessFileHR && retriesLeft > 0)
                            {
                                retriesLeft--;
                                await Task.Delay(200, cancellationToken);
                            }
                            catch (Exception ex)
                            {
                                ReportError(progress, "CGR001", document, ex);
                                fileFailures.Add(ex);
                                break;
                            }
                        }
                        while (true);
                    }
                }
            }

            this.SaveGeneratorAssemblyList(generatorAssemblyInputsFile);

            if (fileFailures.Count > 0)
            {
                throw new AggregateException(fileFailures);
            }
        }

        private static DateTime GetLastModifiedAssemblyTime(string assemblyListPath)
        {
            if (!File.Exists(assemblyListPath))
            {
                return DateTime.MinValue;
            }

            var timestamps = (File.ReadAllLines(assemblyListPath)
                .Where(File.Exists)
                .Select(File.GetLastWriteTime)).ToList();
            return timestamps.Any() ? timestamps.Max() : DateTime.MinValue;
        }

        private static void ReportError(IProgress<Diagnostic> progress, string id, Document inputSyntaxTree, Exception ex)
        {
            Console.Error.WriteLine($"Exception in file processing: {ex}");

            if (progress == null)
            {
                return;
            }

            const string category = "CodeGen.Roslyn: Transformation";
            const string messageFormat = "{0}";

            var descriptor = new DiagnosticDescriptor(
                id,
                "Error during transformation",
                messageFormat,
                category,
                DiagnosticSeverity.Error,
                true);

            var location = inputSyntaxTree != null ? Location.Create(inputSyntaxTree.FilePath, TextSpan.FromBounds(0, 0), new LinePositionSpan(LinePosition.Zero, LinePosition.Zero)) : Location.None;

            var messageArgs = new object[]
            {
                ex,
            };

            var reportDiagnostic = Diagnostic.Create(descriptor, location, messageArgs);

            progress.Report(reportDiagnostic);
        }

        private Assembly LoadAssembly(AssemblyName assemblyName)
        {
            var pluginPath = this.GeneratorAssemblySearchPaths.First();
            var generatorLoadContext = new GeneratorLoadContext(pluginPath, typeof(ICodeGenerator).Assembly);
            return generatorLoadContext.LoadFromAssemblyName(assemblyName);
        }

        private void SaveGeneratorAssemblyList(string assemblyListPath)
        {
            // Union our current list with the one on disk, since our incremental code generation
            // may have skipped some up-to-date files, resulting in fewer assemblies being loaded
            // this time.
            var assemblyPaths = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);

            if (File.Exists(assemblyListPath))
            {
                assemblyPaths.UnionWith(File.ReadAllLines(assemblyListPath));
            }

            assemblyPaths.UnionWith(this.loadedAssemblies);

            File.WriteAllLines(assemblyListPath, assemblyPaths);
        }
    }
}

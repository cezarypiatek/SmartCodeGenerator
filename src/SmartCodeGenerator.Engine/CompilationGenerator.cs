using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace SmartCodeGenerator.Engine
{
    /// <summary>
    /// Runs code generation for every applicable document and handles resulting syntax trees,
    /// saving them to <see cref="_intermediateOutputDirectory"/>.
    /// </summary>
    public class CompilationGenerator
    {
        private const int ProcessCannotAccessFileHR = unchecked((int)0x80070020);
        private readonly string _intermediateOutputDirectory;
        private readonly IProgressReporter _progressReporter;
        private readonly DocumentTransformer _documentTransformer;

        public CompilationGenerator(IReadOnlyList<string> generatorAssemblySearchPaths,
            string intermediateOutputDirectory, ProgressReporter progressReporter)
        {
            var generatorPluginProvider = new GeneratorPluginProvider(generatorAssemblySearchPaths);
            _intermediateOutputDirectory = intermediateOutputDirectory;
            _progressReporter = progressReporter;
            _documentTransformer = new DocumentTransformer(generatorPluginProvider, progressReporter);
        }

        /// <summary>
        /// Runs the code generation as configured using this instance's properties.
        /// </summary>
        /// <param name="project"></param>
        /// <param name="cancellationToken">Cancellation token to interrupt async operations.</param>
        public async Task Process(Project project, CancellationToken cancellationToken = default)
        {
            var compilation = await project.GetCompilationAsync(cancellationToken) as CSharpCompilation;
            if (compilation == null)
            {
                return;
            }

            var generatedFiles = new ConcurrentBag<string>();
            await project.Documents.ProcessInParallelAsync(async document =>
            {
                var outputFile = await ProcessDocument(document, compilation, cancellationToken);
                if (outputFile != null)
                {
                    generatedFiles.Add(outputFile);
                }
            });
            File.WriteAllLines(Path.Combine(this._intermediateOutputDirectory,"SmartCodeGenerator.GeneratedFileList.txt"), generatedFiles);
        }

        private async Task<string?> ProcessDocument(Document document, CSharpCompilation compilation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var outputFilePath = GenerateOutputFilePath(document.FilePath);
            var generatedSyntaxTree = await _documentTransformer.TransformAsync(compilation, document, cancellationToken);
            if (generatedSyntaxTree != null)
            {
                var outputText = generatedSyntaxTree.GetText(cancellationToken);
                await TrySaveOutputText(outputFilePath, outputText, document, cancellationToken);
                return outputFilePath;
            }

            return null;
        }

        private readonly ThreadLocal<SHA1> _hasher = new ThreadLocal<SHA1>(SHA1.Create);

        private string GenerateOutputFilePath(string inputDocumentFilePath)
        {
            var sourceHash = Convert.ToBase64String(_hasher.Value.ComputeHash(Encoding.UTF8.GetBytes(inputDocumentFilePath)), 0, 6)
                .Replace('/', '-');
            return Path.Combine(this._intermediateOutputDirectory, Path.GetFileNameWithoutExtension(inputDocumentFilePath) + $".{sourceHash}.generated.cs");
        }

        private  async Task TrySaveOutputText(string outputFilePath, SourceText outputText, Document document, CancellationToken cancellationToken)
        {
            int retriesLeft = 3;
            do
            {
                try
                {
                    await using var outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    await using var outputWriter = new StreamWriter(outputFileStream);

                    outputText.Write(outputWriter, cancellationToken);
                    break;
                }
                catch (IOException ex) when (ex.HResult == ProcessCannotAccessFileHR && retriesLeft > 0)
                {
                    retriesLeft--;
                    await Task.Delay(200, cancellationToken);
                }
                catch (Exception ex)
                {
                    _progressReporter.ReportError(document, ex);
                    break;
                }
            } while (true);
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmartCodeGenerator.Engine
{
    class TransformedDocumentPersister : ITransformedDocumentPersister
    {
        private readonly string _intermediateOutputDirectory;
        private readonly ProgressReporter _progressReporter;
        private readonly ThreadLocal<SHA1> _hasher = new ThreadLocal<SHA1>(SHA1.Create);
        private readonly ConcurrentBag<string> generatedFiles;
        private const int ProcessCannotAccessFileHR = unchecked((int)0x80070020);

        public TransformedDocumentPersister(string intermediateOutputDirectory, ProgressReporter progressReporter)
        {
            this.generatedFiles = new ConcurrentBag<string>();
            this._intermediateOutputDirectory = intermediateOutputDirectory;
            this._progressReporter = progressReporter;
        }

        private string GenerateOutputFilePath(string inputDocumentFilePath)
        {
            var sourceHash = Convert.ToBase64String(_hasher.Value!.ComputeHash(Encoding.UTF8.GetBytes(inputDocumentFilePath)), 0, 6)
                .Replace('/', '-');
            return Path.Combine(this._intermediateOutputDirectory, Path.GetFileNameWithoutExtension(inputDocumentFilePath) + $".{sourceHash}.generated.cs");
        }

        public async Task TrySaveOutputDocument(DocumentTransformation document, CancellationToken cancellationToken)
        {
            int retriesLeft = 3;
            do
            {
                try
                {
                    var outputFilePath = GenerateOutputFilePath(document.SourceDocument.FilePath);
                    await using var outputFileStream = new FileStream(outputFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
                    await using var outputWriter = new StreamWriter(outputFileStream);

                    document.OutputText.Write(outputWriter, cancellationToken);
                    _progressReporter.ReportInfo($"Generated file: {outputFilePath}");
                    generatedFiles.Add(outputFilePath);
                    break;
                }
                catch (IOException ex) when (ex.HResult == ProcessCannotAccessFileHR && retriesLeft > 0)
                {
                    retriesLeft--;
                    await Task.Delay(200, cancellationToken);
                }
                catch (Exception ex)
                {
                    _progressReporter.ReportError(document.SourceDocument, ex);
                    break;
                }
            } while (true);
        }

        public void SaveInfoAboutPersistedFiles()
        {
            var generatedListPath = Path.Combine(this._intermediateOutputDirectory, "SmartCodeGenerator.GeneratedFileList.txt");
            _progressReporter.ReportInfo($"Saving list of generated files to {generatedListPath}");
            File.WriteAllLines(generatedListPath, generatedFiles);
        }
    }
}
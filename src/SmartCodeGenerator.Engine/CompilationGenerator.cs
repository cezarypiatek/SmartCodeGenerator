using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SmartCodeGenerator.Engine
{
    /// <summary>
    /// Runs code generation for every applicable document and handles resulting syntax trees
    /// </summary>
    public class CompilationGenerator
    {
        private readonly ITransformedDocumentPersister documentPersister;
        private readonly DocumentTransformer documentTransformer;

        public CompilationGenerator(ITransformedDocumentPersister documentPersister, IGeneratorsSource generatorsSource, ProgressReporter progressReporter)
        {
            this.documentPersister = documentPersister;
            documentTransformer = new DocumentTransformer(new GeneratorPluginProvider(generatorsSource), progressReporter);
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

            
            await project.Documents.ProcessInParallelAsync(async document =>
            {
                var outputFile = await ProcessDocument(document, compilation, cancellationToken);
                if (outputFile != null)
                {
                    await documentPersister.TrySaveOutputDocument(outputFile, cancellationToken);
                }
            });
            documentPersister.SaveInfoAboutPersistedFiles();
        }

        private async Task<DocumentTransformation?> ProcessDocument(Document document, CSharpCompilation compilation, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
           
            var generatedSyntaxTree = await documentTransformer.TransformAsync(compilation, document, cancellationToken);
            if (generatedSyntaxTree != null)
            {
                var outputText = generatedSyntaxTree.GetText(cancellationToken);
                return new DocumentTransformation(outputText, document);
            }

            return null;
        }
    }
}

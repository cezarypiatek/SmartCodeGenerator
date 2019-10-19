using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SmartCodeGenerator.Sdk
{
    /// <summary>
    ///     Provides all the inputs and context necessary to perform the code generation.
    /// </summary>
    public class TransformationContext
    {
        public TransformationContext(Document document, SemanticModel semanticModel,
            CSharpCompilation compilation,
            IProgress<Diagnostic> progress)
        {
            SemanticModel = semanticModel;
            Compilation = compilation;
            Progress = progress;
            Document = document;
        }

        public SemanticModel SemanticModel { get; }

        public CSharpCompilation Compilation { get; }

        public IProgress<Diagnostic> Progress { get; }

        public Document Document { get; }
    }
}
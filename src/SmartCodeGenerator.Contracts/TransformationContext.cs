using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SmartCodeGenerator.Contracts
{
    /// <summary>
    ///     Provides all the inputs and context necessary to perform the code generation.
    /// </summary>
    public class TransformationContext
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="TransformationContext" /> class.
        /// </summary>
        /// <param name="document"></param>
        /// <param name="semanticModel">The semantic model.</param>
        /// <param name="compilation">The overall compilation being generated for.</param>
        /// <param name="progress"></param>
        public TransformationContext(Document document, SemanticModel semanticModel,
            CSharpCompilation compilation,
            IProgress<Diagnostic> progress)
        {
            SemanticModel = semanticModel;
            Compilation = compilation;
            Progress = progress;
            Document = document;
        }

        /// <summary>
        /// Gets the semantic model for the <see cref="Compilation" />.
        /// </summary>
        public SemanticModel SemanticModel { get; }

        /// <summary>
        /// Gets the overall compilation being generated for.
        /// </summary>
        public CSharpCompilation Compilation { get; }

        public IProgress<Diagnostic> Progress { get; }
        public Document Document { get; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SmartCodeGenerator.Contracts;

namespace SmartCodeGenerator
{
    /// <summary>
    /// The class responsible for generating compilation units to add to the project being built.
    /// </summary>
    public static class DocumentTransform
    {
        /// <summary>
        /// Produces a new document in response to any code generation attributes found in the specified document.
        /// </summary>
        /// <param name="compilation">The compilation to which the document belongs.</param>
        /// <param name="document"></param>
        /// <param name="generatorPluginProvider">A function that can load an assembly with the given name.</param>
        /// <param name="progress">Reports warnings and errors in code generation.</param>
        /// <param name="cancellationToken"></param>
        /// <param name="inputDocument">The document to scan for generator attributes.</param>
        /// <returns>A task whose result is the generated document.</returns>
        public static async Task<SyntaxTree?> TransformAsync(CSharpCompilation compilation,
            Document document,
            GeneratorPluginProvider generatorPluginProvider,
            IProgress<Diagnostic> progress, CancellationToken cancellationToken)
        {
            var inputSyntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
            var inputSemanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var inputCompilationUnit = inputSyntaxTree.GetCompilationUnitRoot();
            var generatedDocument = new GeneratedDocument(inputCompilationUnit, document);
            var context = new TransformationContext(document, inputSemanticModel, compilation, progress);
            var assemblyAttributes = compilation.Assembly.GetAttributes();

            foreach (var memberNode in GetMemberDeclarations(inputSyntaxTree))
            {
                var attributeData = GetAttributeData(inputSemanticModel, memberNode, assemblyAttributes);
                if (attributeData.Count == 0)
                {
                    continue;
                }

                foreach (var (markerAttribute, generator) in generatorPluginProvider.FindCodeGenerators(attributeData))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var emitted = await generator.GenerateAsync(memberNode, markerAttribute, context, cancellationToken);
                    generatedDocument.Append(emitted, generator);
                }
            }
            return await generatedDocument.GenerateSyntaxTree();
        }

        private static IEnumerable<CSharpSyntaxNode> GetMemberDeclarations(SyntaxTree inputSyntaxTree)
        {
            return inputSyntaxTree
                .GetRoot()
                .DescendantNodesAndSelf(n => n is CompilationUnitSyntax || n is NamespaceDeclarationSyntax || n is TypeDeclarationSyntax)
                .OfType<CSharpSyntaxNode>();
        }

        private static IReadOnlyCollection<AttributeData> GetAttributeData(SemanticModel semanticModel, SyntaxNode syntaxNode, IReadOnlyCollection<AttributeData> assemblyAttributes)
        {
            if (syntaxNode is CompilationUnitSyntax compilationUnitSyntax)
            {
                return assemblyAttributes.Where(x => x.ApplicationSyntaxReference.SyntaxTree == compilationUnitSyntax.SyntaxTree).ToArray();
            }
            return semanticModel.GetDeclaredSymbol(syntaxNode)?.GetAttributes() ?? (IReadOnlyCollection<AttributeData>) Array.Empty<AttributeData>();
        }
    }
}

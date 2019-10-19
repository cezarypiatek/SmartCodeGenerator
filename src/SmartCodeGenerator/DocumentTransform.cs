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
    public class DocumentTransformer
    {
        private readonly GeneratorPluginProvider _generatorPluginProvider;
        private readonly ProgressReporter _errorReporter;

        public DocumentTransformer(GeneratorPluginProvider generatorPluginProvider, ProgressReporter errorReporter)
        {
            _generatorPluginProvider = generatorPluginProvider;
            _errorReporter = errorReporter;
        }

        public async Task<SyntaxTree?> TransformAsync(CSharpCompilation compilation, Document document, CancellationToken cancellationToken)
        {
            try
            {
                var generatedDocument = await GenerateDocumentFrom(compilation, document, cancellationToken);
                return await generatedDocument.GenerateSyntaxTree();
            }
            catch (Exception exception)
            {
                _errorReporter.ReportError(document, exception);
                return null;
            }
        }

        private  async Task<GeneratedDocument> GenerateDocumentFrom(CSharpCompilation compilation, Document document, CancellationToken cancellationToken)
        {
            var inputSyntaxTree = await document.GetSyntaxTreeAsync(cancellationToken);
            var inputSemanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var inputCompilationUnit = inputSyntaxTree.GetCompilationUnitRoot();
            var generatedDocument = new GeneratedDocument(inputCompilationUnit, document);
            var context = new TransformationContext(document, inputSemanticModel, compilation, _errorReporter);
            var assemblyAttributes = compilation.Assembly.GetAttributes();

            foreach (var memberNode in GetMemberDeclarations(inputSyntaxTree))
            {
                var attributeData = GetAttributeData(inputSemanticModel, memberNode, assemblyAttributes);
                if (attributeData.Count == 0)
                {
                    continue;
                }

                foreach (var (markerAttribute, generator) in _generatorPluginProvider.FindCodeGenerators(attributeData))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        var emitted = await generator.GenerateAsync(memberNode, markerAttribute, context, cancellationToken);
                        generatedDocument.Append(emitted, generator);
                    }
                    catch (Exception exception)
                    {
                        _errorReporter.ReportError(document, exception);
                    }
                }
            }

            return generatedDocument;
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

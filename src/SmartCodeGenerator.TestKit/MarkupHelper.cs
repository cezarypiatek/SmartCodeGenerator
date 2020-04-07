using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.VisualBasic;

namespace SmartCodeGenerator.TestKit
{
    internal static class MarkupHelper
    {
        public static Document GetDocumentFromCode(string code, string languageName, IReadOnlyCollection<MetadataReference>? references = null, string? projectName = null, string documentName = null)
        {
            var metadataReferences = CreateMetadataReferences(references);

            var compilationOptions = GetCompilationOptions(languageName);

            return new AdhocWorkspace()
                .AddProject(projectName ?? "TestProject", languageName)
                .WithCompilationOptions(compilationOptions)
                .AddMetadataReferences(metadataReferences)
                .AddDocument(documentName ?? "TestDocument", code);
        }

        private static CompilationOptions GetCompilationOptions(string languageName) =>
            languageName switch
            {
                LanguageNames.CSharp => (CompilationOptions)new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                LanguageNames.VisualBasic => (CompilationOptions)new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary),
                _ => throw new NotSupportedException($"Language {languageName} is not supported")
            };

        private static ImmutableArray<MetadataReference> CreateMetadataReferences(IReadOnlyCollection<MetadataReference>? references)
        {
            var immutableReferencesBuilder = ImmutableArray.CreateBuilder<MetadataReference>();
            if (references != null)
            {
                immutableReferencesBuilder.AddRange(references);
            }

            immutableReferencesBuilder.Add(ReferenceSource.Core);
            immutableReferencesBuilder.Add(ReferenceSource.Linq);
            immutableReferencesBuilder.Add(ReferenceSource.LinqExpression);

            if (ReferenceSource.Core.Display.EndsWith("mscorlib.dll") == false)
            {
                foreach (var netStandardCoreLib in ReferenceSource.NetStandardBasicLibs.Value)
                {
                    immutableReferencesBuilder.Add(netStandardCoreLib);
                }
            }

            return immutableReferencesBuilder.ToImmutable();
        }

      

       
    }
}
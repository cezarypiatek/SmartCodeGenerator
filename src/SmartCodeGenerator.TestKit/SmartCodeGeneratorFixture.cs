using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.CodeAnalysis;
using SmartCodeGenerator.Core;

namespace SmartCodeGenerator.TestKit
{
    public class SmartCodeGeneratorFixture
    {
        private readonly IReadOnlyCollection<MetadataReference>? references;
        private readonly CompilationGenerator compilationGenerator;
        private readonly InMemoryDocumentPersister inMemoryDocumentPersister;

        public SmartCodeGeneratorFixture(Type generatorType, IReadOnlyCollection<MetadataReference>? references = null)
        {
            this.references = references;
            inMemoryDocumentPersister = new InMemoryDocumentPersister();
            this.compilationGenerator = new CompilationGenerator(inMemoryDocumentPersister, new InMemoryGeneratorSource(new []{generatorType}), new ProgressReporter());
        }


        public void AssertGeneratedCode(string inputSourceCode, string expectedGeneratedCode, params string[] ignorePatterns)
        {
            var actualGeneratedCode = Transform(inputSourceCode);
            var expectedGeneratedCodeWithIgnores = MarkIgnoredParts(expectedGeneratedCode, ignorePatterns);
            var actualGeneratedCodeWithIgnores = MarkIgnoredParts(actualGeneratedCode, ignorePatterns);

            if (actualGeneratedCodeWithIgnores != expectedGeneratedCodeWithIgnores)
            {
                DiffHelper.TryToReportDiffWithExternalTool(expectedGeneratedCodeWithIgnores, actualGeneratedCodeWithIgnores);
                var diff = DiffHelper.GenerateInlineDiff(expectedGeneratedCodeWithIgnores, actualGeneratedCodeWithIgnores);
                throw new TransformedCodeDifferentThanExpectedException(actualGeneratedCodeWithIgnores, expectedGeneratedCodeWithIgnores, diff);
            }
        }

        private static string MarkIgnoredParts(string text, params string[] ignorePatterns)
        {
            return ignorePatterns.Aggregate(text, (current, ignorePattern) => Regex.Replace(current, ignorePattern, "__IGNORED_VALUE__"));
        }

        public string Transform(string inoutSourceCode)
        {
            var document = MarkupHelper.GetDocumentFromCode(inoutSourceCode, LanguageNames.CSharp, references, "TestProject");
            this.compilationGenerator.Process(document.Project, new CancellationToken()).GetAwaiter().GetResult();
            var textWriter = new StringWriter();
            inMemoryDocumentPersister.GetPersistedDocuments().FirstOrDefault()?.OutputText.Write(textWriter);
            return textWriter.ToString();
        }
    }
}

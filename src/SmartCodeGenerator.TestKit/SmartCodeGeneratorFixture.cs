using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using SmartCodeGenerator.Engine;

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


        public void AssertGeneratedCode(string inoutSourceCode, string expectedGeneratedCode)
        {
            var actualGeneratedCode = Transform(inoutSourceCode);
            if (actualGeneratedCode != expectedGeneratedCode)
            {
                DiffHelper.TryToReportDiffWithExternalTool(expectedGeneratedCode, actualGeneratedCode);
                var diff = DiffHelper.GenerateInlineDiff(expectedGeneratedCode, actualGeneratedCode);
                throw new TransformedCodeDifferentThanExpectedException(actualGeneratedCode, expectedGeneratedCode, diff);
            }
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

    public class TransformedCodeDifferentThanExpectedException : Exception
    {
        public string Diff { get; }
        public string TransformedCode { get; }
        public string ExpectedCode { get; }

        public TransformedCodeDifferentThanExpectedException(string transformedCode, string expectedCode, string diff)
            : base($"Transformed code is different than expected:{Environment.NewLine}{diff}")
        {
            Diff = diff;
            TransformedCode = transformedCode;
            ExpectedCode = expectedCode;
        }
    }
}

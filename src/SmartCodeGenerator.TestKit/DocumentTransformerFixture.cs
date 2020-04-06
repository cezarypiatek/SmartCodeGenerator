using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using SmartCodeGenerator.Engine;

namespace SmartCodeGenerator.TestKit
{

    public class DocumentTransformerFixture
    {
        private readonly CompilationGenerator _compilationGenerator;
        private readonly InMemoryDocumentPersister _inMemoryDocumentPersister;

        public DocumentTransformerFixture(Type generatorType)
        {
            _inMemoryDocumentPersister = new InMemoryDocumentPersister();
            this._compilationGenerator = new CompilationGenerator(_inMemoryDocumentPersister, new InMemoryGeneratorSource(new []{generatorType}), new ProgressReporter());
        }

        public string Transform(string inputDocumentCode, IReadOnlyCollection<MetadataReference>? references = null)
        {
            var document = MarkupHelper.GetDocumentFromCode(inputDocumentCode, LanguageNames.CSharp, references, "TestProject");
            this._compilationGenerator.Process(document.Project, new CancellationToken()).GetAwaiter().GetResult();
            var textWriter = new StringWriter();
            _inMemoryDocumentPersister.GetPersistedDocuments().FirstOrDefault()?.OutputText.Write(textWriter);
            return textWriter.ToString();
        }
    }
}

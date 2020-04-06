using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SmartCodeGenerator.Engine
{
    public class DocumentTransformation
    {
        public DocumentTransformation(SourceText outputText, Document sourceDocument)
        {
            OutputText = outputText;
            SourceDocument = sourceDocument;
        }

        public SourceText OutputText { get; }
        public Document SourceDocument { get; }
    }
}
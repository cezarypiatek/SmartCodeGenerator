using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SmartCodeGenerator
{
    internal class ErrorReporter : IErrorReporter
    {
        private readonly IProgress<Diagnostic> _progress;

        private  readonly DiagnosticDescriptor ErrorDescriptor = new DiagnosticDescriptor(
            "CGR001",
            "Error during transformation",
            "{0}",
            "On build code generation",
            DiagnosticSeverity.Error,
            true);

        public ErrorReporter(IProgress<Diagnostic> progress)
        {
            _progress = progress;
        }

        public void ReportError(Document inputDocument, Exception ex)
        {
            var location = Location.Create(inputDocument.FilePath, TextSpan.FromBounds(0, 0), new LinePositionSpan(LinePosition.Zero, LinePosition.Zero));
            var reportDiagnostic = Diagnostic.Create(ErrorDescriptor, location, ex);
            _progress.Report(reportDiagnostic);
        }
    }

    public interface IErrorReporter
    {
        void ReportError(Document inputDocument, Exception ex);
    }
}
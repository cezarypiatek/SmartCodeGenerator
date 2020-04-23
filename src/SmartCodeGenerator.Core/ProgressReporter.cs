using System;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using Microsoft.CodeAnalysis.Text;

namespace SmartCodeGenerator.Core
{
    public class ProgressReporter :  IProgressReporter, IProgress<Diagnostic>, IProgress<ProjectLoadProgress>
    {

        private  readonly DiagnosticDescriptor ErrorDescriptor = new DiagnosticDescriptor(
            "CGR001",
            "Error during transformation",
            "{0}",
            "On build code generation",
            DiagnosticSeverity.Error,
            true);

        public void ReportError(Document inputDocument, Exception ex)
        {
            var location = Location.Create(inputDocument.FilePath, TextSpan.FromBounds(0, 0), new LinePositionSpan(LinePosition.Zero, LinePosition.Zero));
            var reportDiagnostic = Diagnostic.Create(ErrorDescriptor, location, ex);
            this.Report(reportDiagnostic);
        }

        public void ReportInfo(string message)
        {
            Console.WriteLine(message);
        }

        public void Report(Diagnostic diagnostic)
        {
            switch (diagnostic.DefaultSeverity)
            {
                case DiagnosticSeverity.Hidden:
                    return;
                case DiagnosticSeverity.Info:
                case DiagnosticSeverity.Warning:
                    Console.WriteLine(diagnostic.ToString());
                    break;
                case DiagnosticSeverity.Error:
                    Console.Error.WriteLine(diagnostic.ToString());
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void Report(ProjectLoadProgress loadProgress)
        {
            var projectDisplay = Path.GetFileName(loadProgress.FilePath);
            if (loadProgress.TargetFramework != null)
            {
                projectDisplay += $" ({loadProgress.TargetFramework})";
            }

            Console.WriteLine($"{loadProgress.Operation,-15} {loadProgress.ElapsedTime,-15:m\\:ss\\.fffffff} {projectDisplay}");
        }
    }

    public interface IProgressReporter
    {
        void ReportError(Document inputDocument, Exception ex);
        void ReportInfo(string message);
    }
}
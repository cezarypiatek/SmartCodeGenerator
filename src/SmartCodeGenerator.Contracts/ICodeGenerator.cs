using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SmartCodeGenerator.Contracts
{
    /// <summary>
    /// Describes a code generator that responds to attributes on members to generate code,
    /// and returns compilation unit members.
    /// </summary>
    public interface ICodeGenerator
    {
        Task<GenerationResult> GenerateAsync(CSharpSyntaxNode processedNode, AttributeData markerAttribute, TransformationContext context, CancellationToken cancellationToken);
    }
}

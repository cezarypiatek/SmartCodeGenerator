using System.Threading;
using System.Threading.Tasks;

namespace SmartCodeGenerator.Engine
{
    public interface ITransformedDocumentPersister
    {
        Task TrySaveOutputDocument(DocumentTransformation document, CancellationToken cancellationToken);
        void SaveInfoAboutPersistedFiles();
    }
}
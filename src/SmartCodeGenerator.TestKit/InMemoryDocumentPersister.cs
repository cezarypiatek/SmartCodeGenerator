using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SmartCodeGenerator.Engine;

namespace SmartCodeGenerator.TestKit
{
    class InMemoryDocumentPersister : ITransformedDocumentPersister
    {
        private readonly ConcurrentQueue<DocumentTransformation> _transformations = new ConcurrentQueue<DocumentTransformation>();

        public Task TrySaveOutputDocument(DocumentTransformation document, CancellationToken cancellationToken)
        {
            _transformations.Enqueue(document);
            return Task.CompletedTask;
        }

        public IReadOnlyList<DocumentTransformation> GetPersistedDocuments() => _transformations.ToList();

        public void SaveInfoAboutPersistedFiles()
        {
        }
    }
}
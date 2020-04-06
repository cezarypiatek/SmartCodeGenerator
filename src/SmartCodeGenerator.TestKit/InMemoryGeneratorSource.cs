using System;
using System.Collections.Generic;
using SmartCodeGenerator.Engine;

namespace SmartCodeGenerator.TestKit
{
    class InMemoryGeneratorSource : IGeneratorsSource
    {
        private readonly IReadOnlyCollection<Type> _generatorTypes;

        public InMemoryGeneratorSource(IReadOnlyCollection<Type> generatorTypes)
        {
            _generatorTypes = generatorTypes;
        }

        public IReadOnlyCollection<Type> GetGeneratorTypes() => this._generatorTypes;
    }
}
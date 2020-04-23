using System;
using System.Collections.Generic;

namespace SmartCodeGenerator.Core
{
    public interface IGeneratorsSource
    {
        IReadOnlyCollection<Type> GetGeneratorTypes();
    }
}
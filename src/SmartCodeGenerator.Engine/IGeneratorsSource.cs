using System;
using System.Collections.Generic;

namespace SmartCodeGenerator.Engine
{
    public interface IGeneratorsSource
    {
        IReadOnlyCollection<Type> GetGeneratorTypes();
    }
}
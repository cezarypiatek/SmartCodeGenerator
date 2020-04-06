using System;
using System.Collections.Generic;
using System.Linq;
using SmartCodeGenerator.Engine.PluginArchitectureDemo;
using SmartCodeGenerator.Sdk;

namespace SmartCodeGenerator.Engine
{
    class FileSystemGeneratorsSource : IGeneratorsSource
    {
        private readonly IReadOnlyList<string> generatorAssemblyPaths;

        public FileSystemGeneratorsSource(IReadOnlyList<string> generatorAssemblyPaths)
        {
            this.generatorAssemblyPaths = generatorAssemblyPaths;
        }

        public IReadOnlyCollection<Type> GetGeneratorTypes()
        {
            var generatorInterfaceType = typeof(ICodeGenerator);
            return generatorAssemblyPaths.SelectMany(x =>
            {
                var generatorLoadContext = new GeneratorLoadContext(x, typeof(ICodeGenerator).Assembly);
                var pluginAssembly = generatorLoadContext.LoadFromAssemblyPath(x);
                return pluginAssembly.GetTypes().Where(t => generatorInterfaceType.IsAssignableFrom(t));
            }).ToList();
        }
    }
}
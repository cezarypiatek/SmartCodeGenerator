using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using SmartCodeGenerator.Contracts;
using SmartCodeGenerator.PluginArchitectureDemo;

namespace SmartCodeGenerator
{
    public class GeneratorPluginProvider
    {
        private IReadOnlyDictionary<string,Lazy<ICodeGenerator>> _generators;

        public GeneratorPluginProvider(IReadOnlyList<string> generatorAssemblyPaths)
        {
            var generatorInterfaceType = typeof(ICodeGenerator);
            _generators =  generatorAssemblyPaths.SelectMany(x =>
            {
                var generatorLoadContext = new GeneratorLoadContext(x, typeof(ICodeGenerator).Assembly);
                var pluginAssembly = generatorLoadContext.LoadFromAssemblyPath(x);
                return pluginAssembly.GetTypes().Where(t => generatorInterfaceType.IsAssignableFrom(t))
                    .Select(type =>
                    {
                        var generatorAttribute = (GeneratorAttribute?) type.GetCustomAttribute(typeof(GeneratorAttribute));
                        var key = generatorAttribute?.ProcessMarkedWith.FullName ?? Guid.NewGuid().ToString();
                        var generator = new Lazy<ICodeGenerator>(() => (ICodeGenerator?) Activator.CreateInstance(type) ?? new EmptyGenerator());
                        return new {key, generator};

                    });
            }).ToDictionary(el=> el.key, el=>el.generator);
        }

        public ICodeGenerator? FindFor(AttributeData attributeData)
        {
            _generators.TryGetValue(attributeData.AttributeClass.ToDisplayString(), out var generator);
            return generator?.Value;
        }

        public IEnumerable<(AttributeData, ICodeGenerator)> FindCodeGenerators(ImmutableArray<AttributeData> nodeAttributes)
        {
            foreach (var attributeData in nodeAttributes)
            {
                var codeGenerator = this.FindFor(attributeData);
                if (codeGenerator != null)
                {
                    yield return (attributeData, codeGenerator);
                }
            }
        }

        class EmptyGenerator:ICodeGenerator
        {

            public Task<GenerationResult> GenerateAsync(AttributeData markerAttribute, TransformationContext context, IProgress<Diagnostic> progress,
                CancellationToken cancellationToken)
            {
                throw new NotImplementedException();
            }
        }
    }
}
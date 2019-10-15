using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.CodeAnalysis;
using SmartCodeGenerator.Contracts;

namespace SmartCodeGenerator
{
    internal static class GeneratorFinder
    {
        private static readonly IDictionary<string, ICodeGenerator> generatorCache = new Dictionary<string, ICodeGenerator>();

        public static IEnumerable<ICodeGenerator> FindCodeGenerators(ImmutableArray<AttributeData> nodeAttributes, Func<AssemblyName, Assembly> assemblyLoader)
        {
            foreach (var attributeData in nodeAttributes)
            {
                var key = attributeData.AttributeClass.ToString() ?? Guid.NewGuid().ToString("N");
                if (generatorCache.ContainsKey(key) == false)
                {
                    var generatorType = GetCodeGeneratorTypeForAttribute(attributeData, assemblyLoader);
                    if (generatorType != null)
                    {
                        var generator = TryToCreateGeneratorInstance(generatorType, attributeData);
                        if (generator != null)
                        {
                            generatorCache.Add(key, generator);
                        }
                    }
                }

                if (generatorCache.TryGetValue(key, out var cachedGenerator))
                {
                    yield return cachedGenerator;
                }
            }
        }

        private static ICodeGenerator? TryToCreateGeneratorInstance(Type generatorType, AttributeData attributeData)
        {
            try
            {
                return (ICodeGenerator?)Activator.CreateInstance(generatorType, new object[]
                {
                    attributeData
                });
                //return (ICodeGenerator?) Activator.CreateInstance(generatorType);
            }
            
            catch (MissingMethodException)
            {
                throw new InvalidOperationException(
                    $"Failed to instantiate {generatorType}. ICodeGenerator implementations must have" +
                    $" a constructor accepting Microsoft.CodeAnalysis.AttributeData argument.");
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        private static readonly string MarkerAttributeName = typeof(CodeGenerationMarkerAttribute).Name;
        private static Type? GetCodeGeneratorTypeForAttribute(AttributeData attributeType, Func<AssemblyName, Assembly?> assemblyLoader)
        {
            var assembly1 = typeof(ICodeGenerator).Assembly;
            var generatorCandidateAttribute = attributeType.AttributeClass.GetAttributes()
                .FirstOrDefault(x => x.AttributeClass.Name == MarkerAttributeName);

            if (generatorCandidateAttribute != null)
            {
                var typeName = GetTypeNameAssemblyName(generatorCandidateAttribute);
                if (typeName != null)
                {
                    var assembly = assemblyLoader(new AssemblyName(typeName.AssemblyName));
                    if (assembly != null)
                    {
                        var generatorType = assembly.GetType(typeName.FullTypeName);
                        if (generatorType == null)
                        {
                            throw new Exception($"Unable to load code generator: {typeName}");
                        }
                        return generatorType;
                    }
                }
            }

            return null;
        }

        private static FullyQualifiedTypeName? GetTypeNameAssemblyName(AttributeData generatorCandidateAttribute)
        {
            var typeParameter = generatorCandidateAttribute.ConstructorArguments.Single();
            if (typeParameter.Value is string typeName)
            {
                // This string is the full name of the type, which MAY be assembly-qualified.
                int commaIndex = typeName.IndexOf(',');
                bool isAssemblyQualified = commaIndex >= 0;
                if (isAssemblyQualified)
                {
                    var fullTypeName = typeName.Substring(0, commaIndex);
                    var assemblyName = typeName.Substring(commaIndex + 1).Trim();
                    return new FullyQualifiedTypeName(fullTypeName, assemblyName);
                }
                else
                {
                    var fullTypeName = typeName;
                    var assemblyName = generatorCandidateAttribute.AttributeClass.ContainingAssembly.Name;
                    return new FullyQualifiedTypeName(fullTypeName, assemblyName);
                }
            }
            else if (typeParameter.Value is INamedTypeSymbol typeOfValue)
            {
                // This was a typeof(T) expression
                var fullTypeName = GetFullTypeName(typeOfValue);
                var assemblyName = typeOfValue.ContainingAssembly.Name;
                return new FullyQualifiedTypeName(fullTypeName, assemblyName);
            }

            return null;
        }

        class FullyQualifiedTypeName
        {
            public string FullTypeName { get; }
            public string AssemblyName { get; }

            public FullyQualifiedTypeName(string fullTypeName, string assemblyName)
            {
                FullTypeName = fullTypeName;
                AssemblyName = assemblyName;
            }

            public override string ToString()
            {
                return $"{FullTypeName}, {AssemblyName}";
            }
        }

        private static string GetFullTypeName(INamedTypeSymbol symbol)
        {
            var nameBuilder = new StringBuilder();
            ISymbol symbolOrParent = symbol;
            while (symbolOrParent != null && !string.IsNullOrEmpty(symbolOrParent.Name))
            {
                if (nameBuilder.Length > 0)
                {
                    nameBuilder.Insert(0, ".");
                }

                nameBuilder.Insert(0, symbolOrParent.Name);
                symbolOrParent = symbolOrParent.ContainingSymbol;
            }

            return nameBuilder.ToString();
        }
    }
}
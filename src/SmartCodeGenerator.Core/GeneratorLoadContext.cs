using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace SmartCodeGenerator.Core
{
    public class GeneratorLoadContext : AssemblyLoadContext
    {
        private readonly AssemblyDependencyResolver _resolver;
        private readonly IReadOnlyDictionary<string, Version> _pluginContractAssembly;

        public GeneratorLoadContext(string pluginPath, Assembly pluginContract)
        {
            _pluginContractAssembly = GetPluginContractAssemblies(pluginContract);
            _resolver = new AssemblyDependencyResolver(pluginPath);
        }

        private static IReadOnlyDictionary<string, Version> GetPluginContractAssemblies(Assembly pluginContract)
        {
            var referencedAssemblies = pluginContract.GetReferencedAssemblies().ToList();
            referencedAssemblies.Add(pluginContract.GetName());
            var result = new Dictionary<string, Version>();
            foreach (var referencedAssembly in referencedAssemblies)
            {
                if (referencedAssembly.Name != null && referencedAssembly.Version != null)
                {
                    result.Add(referencedAssembly.Name, referencedAssembly.Version);
                }
            }
            return result;
        }

        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name != null && _pluginContractAssembly.TryGetValue(assemblyName.Name, out var version) && assemblyName.Version <= version)
            {
                return null;
            }

            var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
            if (assemblyPath != null)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            return null;
        }

        protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
        {
            var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
            if (libraryPath != null)
            {
                return LoadUnmanagedDllFromPath(libraryPath);
            }

            return IntPtr.Zero;
        }
    }
}
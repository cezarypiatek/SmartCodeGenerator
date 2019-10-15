using System;

namespace SmartCodeGenerator.Contracts
{
    /// <summary>
    /// A base attribute type for code generation attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class CodeGenerationMarkerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenerationMarkerAttribute"/> class.
        /// </summary>
        /// <param name="generatorFullTypeName">
        /// The fully-qualified type name (including assembly information)
        /// of the code generator to activate.
        /// This type must implement ICodeGenerator.
        /// </param>
        public CodeGenerationMarkerAttribute(string generatorFullTypeName)
        {
            this.GeneratorFullTypeName = generatorFullTypeName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CodeGenerationMarkerAttribute"/> class.
        /// </summary>
        /// <param name="generatorType">The code generator that implements ICodeGenerator.</param>
        public CodeGenerationMarkerAttribute(Type generatorType)
        {
            if (generatorType == null)
            {
                throw new ArgumentNullException(nameof(generatorType));
            }

            this.GeneratorFullTypeName = generatorType.AssemblyQualifiedName ?? throw new Exception($"Unknown {nameof(generatorType.AssemblyQualifiedName)} for type {generatorType.Name}");
        }

        /// <summary>
        /// Gets the fully-qualified type name (including assembly information)
        /// of the code generator to activate.
        /// </summary>
        public string GeneratorFullTypeName { get; }
    }
}

using System;

namespace SmartCodeGenerator.Contracts
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CodeGeneratorAttribute:Attribute
    {
        public Type ProcessMarkedWith { get; }

        public CodeGeneratorAttribute(Type processMarkedWith)
        {
            ProcessMarkedWith = processMarkedWith;
        }
    }
}
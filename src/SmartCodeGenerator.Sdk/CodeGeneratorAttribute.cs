using System;

namespace SmartCodeGenerator.Sdk
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
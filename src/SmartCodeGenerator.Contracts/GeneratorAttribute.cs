using System;

namespace SmartCodeGenerator.Contracts
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GeneratorAttribute:Attribute
    {
        public Type ProcessMarkedWith { get; }

        public GeneratorAttribute(Type processMarkedWith)
        {
            ProcessMarkedWith = processMarkedWith;
        }
    }
}
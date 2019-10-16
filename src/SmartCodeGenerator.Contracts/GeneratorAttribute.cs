using System;

namespace SmartCodeGenerator.Contracts
{
    [AttributeUsage(AttributeTargets.Class)]
    public class GeneratorAttribute:Attribute
    {
        public Type ProcessMarkedWith { get; set; }
        public GeneratorAttribute()
        {
        }
    }
}
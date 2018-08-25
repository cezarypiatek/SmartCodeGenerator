﻿// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE.txt file in the project root for full license information.

namespace CodeGeneration.Roslyn.Tests.Generators
{
    using System;
    using System.Diagnostics;

    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    [CodeGenerationAttribute(typeof(DuplicateInOtherNamespaceGenerator))]
    [Conditional("CodeGeneration")]
    public class DuplicateInOtherNamespaceAttribute : Attribute
    {
        public DuplicateInOtherNamespaceAttribute(string @namespace)
        {
            Namespace = @namespace;
        }

        public string Namespace { get; }
    }
}
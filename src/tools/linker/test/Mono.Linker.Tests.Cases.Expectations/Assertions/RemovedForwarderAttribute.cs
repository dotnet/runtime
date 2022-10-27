// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RemovedForwarderAttribute : BaseInAssemblyAttribute
    {
        public RemovedForwarderAttribute(string assemblyFileName, string typeName)
        {
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(assemblyFileName));
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(typeName));
        }
    }
}

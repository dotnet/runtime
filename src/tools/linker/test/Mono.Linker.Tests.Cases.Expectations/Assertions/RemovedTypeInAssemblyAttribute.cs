// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
    public class RemovedTypeInAssemblyAttribute : BaseInAssemblyAttribute
    {
        public RemovedTypeInAssemblyAttribute(string assemblyFileName, Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(assemblyFileName));
        }

        public RemovedTypeInAssemblyAttribute(string assemblyFileName, string typeName)
        {
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(assemblyFileName));
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(typeName));
        }
    }
}

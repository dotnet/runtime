// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RemovedInterfaceOnTypeInAssemblyAttribute : BaseInAssemblyAttribute
    {
        public RemovedInterfaceOnTypeInAssemblyAttribute(string assemblyFileName, Type type, string interfaceAssemblyFileName, Type interfaceType)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(assemblyFileName));

            if (string.IsNullOrEmpty(interfaceAssemblyFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(interfaceAssemblyFileName));
            if (interfaceType == null)
                throw new ArgumentException("Value cannot be null or empty.", nameof(interfaceType));
        }

        public RemovedInterfaceOnTypeInAssemblyAttribute(string assemblyFileName, string typeName, string interfaceAssemblyFileName, string interfaceTypeName)
        {
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(assemblyFileName));
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(typeName));

            if (string.IsNullOrEmpty(interfaceAssemblyFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(interfaceAssemblyFileName));
            if (string.IsNullOrEmpty(interfaceTypeName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(interfaceTypeName));
        }
    }
}

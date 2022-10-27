// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class KeptBaseOnTypeInAssemblyAttribute : BaseInAssemblyAttribute
    {
        public KeptBaseOnTypeInAssemblyAttribute(string assemblyFileName, Type type, string baseAssemblyFileName, Type baseType)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(assemblyFileName));

            if (string.IsNullOrEmpty(baseAssemblyFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(baseAssemblyFileName));
            if (baseType == null)
                throw new ArgumentException("Value cannot be null or empty.", nameof(baseType));
        }

        public KeptBaseOnTypeInAssemblyAttribute(string assemblyFileName, string typeName, string baseAssemblyFileName, string baseTypeName)
        {
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(assemblyFileName));
            if (string.IsNullOrEmpty(typeName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(typeName));

            if (string.IsNullOrEmpty(baseAssemblyFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(baseAssemblyFileName));
            if (string.IsNullOrEmpty(baseTypeName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(baseTypeName));
        }
    }
}

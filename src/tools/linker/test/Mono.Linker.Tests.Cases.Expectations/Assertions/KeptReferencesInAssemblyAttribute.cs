// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class KeptReferencesInAssemblyAttribute : BaseInAssemblyAttribute
    {
        public KeptReferencesInAssemblyAttribute(string assemblyFileName, string[] expectedReferenceAssemblyNames)
        {
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentNullException(nameof(assemblyFileName));

            if (expectedReferenceAssemblyNames == null)
                throw new ArgumentNullException(nameof(expectedReferenceAssemblyNames));
        }
    }
}

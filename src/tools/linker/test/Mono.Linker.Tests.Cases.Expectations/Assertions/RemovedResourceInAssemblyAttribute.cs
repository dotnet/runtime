// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
    /// <summary>
    /// Verifies that an embedded resource was removed from an assembly
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public class RemovedResourceInAssemblyAttribute : BaseInAssemblyAttribute
    {
        public RemovedResourceInAssemblyAttribute(string assemblyFileName, string resourceName)
        {
            if (string.IsNullOrEmpty(assemblyFileName))
                throw new ArgumentNullException(nameof(assemblyFileName));

            if (string.IsNullOrEmpty(resourceName))
                throw new ArgumentNullException(nameof(resourceName));
        }
    }
}

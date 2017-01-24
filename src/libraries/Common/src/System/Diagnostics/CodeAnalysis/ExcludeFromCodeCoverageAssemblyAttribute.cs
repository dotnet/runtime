// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics.CodeAnalysis
{
    // Matches the *.ExcludeFromCodeCoverage* filter passed to OpenCover but
    // unlike ExcludeFromCodeCoverageAttribute can be applied to assemblies.
    [Conditional("DEBUG")] // don't bloat release assemblies
    [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = false)]
    internal sealed class ExcludeFromCodeCoverageAssemblyAttribute : Attribute
    {
    }
}

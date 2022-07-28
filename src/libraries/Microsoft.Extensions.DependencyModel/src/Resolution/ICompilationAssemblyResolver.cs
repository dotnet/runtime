// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public interface ICompilationAssemblyResolver
    {
        bool TryResolveAssemblyPaths(CompilationLibrary library, List<string>? assemblies);
    }
}

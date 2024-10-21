// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class CompositeCompilationAssemblyResolver : ICompilationAssemblyResolver
    {
        private readonly ICompilationAssemblyResolver[] _resolvers;

        public CompositeCompilationAssemblyResolver(ICompilationAssemblyResolver[] resolvers)
        {
            ThrowHelper.ThrowIfNull(resolvers);

            _resolvers = resolvers;
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string>? assemblies)
        {
            foreach (ICompilationAssemblyResolver resolver in _resolvers)
            {
                if (resolver.TryResolveAssemblyPaths(library, assemblies))
                {
                    return true;
                }
            }
            return false;
        }
    }
}

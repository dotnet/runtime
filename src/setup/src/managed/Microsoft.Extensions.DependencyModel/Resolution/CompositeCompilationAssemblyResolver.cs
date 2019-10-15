// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel.Resolution
{
    public class CompositeCompilationAssemblyResolver: ICompilationAssemblyResolver
    {
        private readonly ICompilationAssemblyResolver[] _resolvers;

        public CompositeCompilationAssemblyResolver(ICompilationAssemblyResolver[] resolvers)
        {
            _resolvers = resolvers;
        }

        public bool TryResolveAssemblyPaths(CompilationLibrary library, List<string> assemblies)
        {
            foreach (var resolver in _resolvers)
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
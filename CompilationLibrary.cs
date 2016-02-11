// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Microsoft.Extensions.DependencyModel
{
    public class CompilationLibrary : Library
    {
        public CompilationLibrary(string libraryType, string packageName, string version, string hash, string[] assemblies, Dependency[] dependencies, bool serviceable)
            : base(libraryType, packageName, version, hash,  dependencies, serviceable)
        {
            Assemblies = assemblies;
        }

        public IReadOnlyList<string> Assemblies { get; }

        internal static ICompilationAssemblyResolver DefaultResolver { get; } = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
        {
            new PackageCacheCompilationAssemblyResolver(),
            new AppBaseCompilationAssemblyResolver(),
            new ReferenceAssemblyPathResolver(),
            new PackageCompilationAssemblyResolver()
        });

        public IEnumerable<string> ResolveReferencePaths(CompilationLibrary compilationLibrary)
        {
            var assemblies = new List<string>();
            if (!DefaultResolver.TryResolveAssemblyPaths(compilationLibrary, assemblies))
            {
                throw new InvalidOperationException($"Can not find compilation library location for package '{PackageName}'");
            }
            return assemblies;
        }
    }
}
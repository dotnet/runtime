// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyModel.Resolution;

namespace Microsoft.Extensions.DependencyModel
{
    public class CompilationLibrary : Library
    {
        public CompilationLibrary(string type,
            string name,
            string version,
            string? hash,
            IEnumerable<string> assemblies,
            IEnumerable<Dependency> dependencies,
            bool serviceable)
            : this(type, name, version, hash, assemblies, dependencies, serviceable, path: null, hashPath: null)
        {
        }

        public CompilationLibrary(string type,
            string name,
            string version,
            string? hash,
            IEnumerable<string> assemblies,
            IEnumerable<Dependency> dependencies,
            bool serviceable,
            string? path,
            string? hashPath)
            : base(type, name, version, hash, dependencies, serviceable, path, hashPath)
        {
            ThrowHelper.ThrowIfNull(assemblies);

            Assemblies = assemblies.ToArray();
        }

        public IReadOnlyList<string> Assemblies { get; }

        internal static ICompilationAssemblyResolver DefaultResolver { get; } = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
        {
            new AppBaseCompilationAssemblyResolver(),
            new ReferenceAssemblyPathResolver(),
            new PackageCompilationAssemblyResolver()
        });

        public IEnumerable<string> ResolveReferencePaths()
        {
            var assemblies = new List<string>();

            return ResolveReferencePaths(DefaultResolver, assemblies);
        }

        public IEnumerable<string> ResolveReferencePaths(params ICompilationAssemblyResolver[] customResolvers)
        {
            var assemblies = new List<string>();

            if (customResolvers?.Length > 0)
            {
                foreach (ICompilationAssemblyResolver resolver in customResolvers)
                {
                    if (resolver.TryResolveAssemblyPaths(this, assemblies))
                    {
                        return assemblies;
                    }
                }
            }

            return ResolveReferencePaths(DefaultResolver, assemblies);
        }

        private IEnumerable<string> ResolveReferencePaths(ICompilationAssemblyResolver resolver, List<string> assemblies)
        {
            if (!resolver.TryResolveAssemblyPaths(this, assemblies))
            {
                throw new InvalidOperationException(SR.Format(SR.LibraryLocationNotFound, Name));
            }
            return assemblies;
        }
    }
}

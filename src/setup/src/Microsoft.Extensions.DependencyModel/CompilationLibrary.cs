// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            string hash,
            IEnumerable<string> assemblies,
            IEnumerable<Dependency> dependencies,
            bool serviceable)
            : this(type, name, version, hash, assemblies, dependencies, serviceable, path: null, hashPath: null)
        {
        }

        public CompilationLibrary(string type,
            string name,
            string version,
            string hash,
            IEnumerable<string> assemblies,
            IEnumerable<Dependency> dependencies,
            bool serviceable,
            string path,
            string hashPath)
            : base(type, name, version, hash, dependencies, serviceable, path, hashPath)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }
            Assemblies = assemblies.ToArray();
        }

        public IReadOnlyList<string> Assemblies { get; }

#if !NETSTANDARD1_3
        internal static ICompilationAssemblyResolver DefaultResolver { get; } = new CompositeCompilationAssemblyResolver(new ICompilationAssemblyResolver[]
        {
            new AppBaseCompilationAssemblyResolver(),
            new ReferenceAssemblyPathResolver(),
            new PackageCompilationAssemblyResolver()
        });

        public IEnumerable<string> ResolveReferencePaths()
        {
            return ResolveReferencePaths(DefaultResolver);
        }

        public IEnumerable<string> ResolveReferencePaths(params ICompilationAssemblyResolver[] additionalResolvers)
        {
            ICompilationAssemblyResolver resolver;
            if (additionalResolvers?.Length > 0)
            {
                var allResolvers = new ICompilationAssemblyResolver[additionalResolvers.Length + 1];
                additionalResolvers.CopyTo(allResolvers, 0);
                allResolvers[additionalResolvers.Length] = DefaultResolver;

                resolver = new CompositeCompilationAssemblyResolver(allResolvers);
            }
            else
            {
                resolver = DefaultResolver;
            }

            return ResolveReferencePaths(resolver);
        }

        private IEnumerable<string> ResolveReferencePaths(ICompilationAssemblyResolver resolver)
        {
            var assemblies = new List<string>();
            if (!resolver.TryResolveAssemblyPaths(this, assemblies))
            {
                throw new InvalidOperationException($"Cannot find compilation library location for package '{Name}'");
            }
            return assemblies;
        }
#endif

    }
}

// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeLibrary : Library
    {
        public RuntimeLibrary(
            string type,
            string name,
            string version,
            string hash,
            IEnumerable<RuntimeAssembly> assemblies,
            IEnumerable<string> nativeLibraries,
            IEnumerable<ResourceAssembly> resourceAssemblies,
            IEnumerable<RuntimeTarget> subTargets,
            IEnumerable<Dependency> dependencies,
            bool serviceable)
            : base(type, name, version, hash, dependencies, serviceable)
        {
            if (assemblies == null)
            {
                throw new ArgumentNullException(nameof(assemblies));
            }
            if (nativeLibraries == null)
            {
                throw new ArgumentNullException(nameof(nativeLibraries));
            }
            if (resourceAssemblies == null)
            {
                throw new ArgumentNullException(nameof(resourceAssemblies));
            }
            if (subTargets == null)
            {
                throw new ArgumentNullException(nameof(subTargets));
            }
            Assemblies = assemblies.ToArray();
            ResourceAssemblies = resourceAssemblies.ToArray();
            RuntimeTargets = subTargets.ToArray();
            NativeLibraries = nativeLibraries.ToArray();
        }

        public IReadOnlyList<RuntimeAssembly> Assemblies { get; }

        public IReadOnlyList<string> NativeLibraries { get; }

        public IReadOnlyList<ResourceAssembly> ResourceAssemblies { get; }

        public IReadOnlyList<RuntimeTarget> RuntimeTargets { get; }
    }
}
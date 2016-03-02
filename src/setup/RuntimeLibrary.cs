// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeLibrary : Library
    {
        public RuntimeLibrary(
            string libraryType,
            string packageName,
            string version,
            string hash,
            RuntimeAssembly[] assemblies,
            RuntimeTarget[] subTargets,
            Dependency[] dependencies,
            bool serviceable)
            : base(libraryType, packageName, version, hash, dependencies, serviceable)
        {
            Assemblies = assemblies;
            SubTargets = subTargets;
        }

        public IReadOnlyList<RuntimeAssembly> Assemblies { get; }

        public IReadOnlyList<RuntimeTarget> SubTargets { get; }
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            RuntimeAssembly[] assemblies,
            RuntimeTarget[] subTargets,
            Dependency[] dependencies,
            bool serviceable)
            : base(type, name, version, hash, dependencies, serviceable)
        {
            Assemblies = assemblies;
            RuntimeTargets = subTargets;
        }

        public IReadOnlyList<RuntimeAssembly> Assemblies { get; }

        public IReadOnlyList<RuntimeTarget> RuntimeTargets { get; }
    }
}
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
            string[] assemblies,
            RuntimeTarget[] subTargets,
            Dependency[] dependencies,
            bool serviceable)
            : base(libraryType, packageName, version, hash, dependencies, serviceable)
        {
            Assemblies = assemblies.Select(path => new RuntimeAssembly(path)).ToArray();
            SubTargets = subTargets;
        }

        public IReadOnlyList<RuntimeAssembly> Assemblies { get; }

        public IReadOnlyList<RuntimeTarget> SubTargets { get; }
    }

    public class RuntimeTarget
    {
        public RuntimeTarget(string runtime, IReadOnlyList<RuntimeAssembly> assemblies, IReadOnlyList<string> nativeLibraries)
        {
            Runtime = runtime;
            Assemblies = assemblies;
            NativeLibraries = nativeLibraries;
        }

        public string Runtime { get; }

        public IReadOnlyList<RuntimeAssembly> Assemblies { get; }

        public IReadOnlyList<string> NativeLibraries { get; }
    }
}
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel
{
    public struct Library
    {
        public Library(string libraryType, string packageName, string version, string hash, string[] assemblies, Dependency[] dependencies, bool serviceable)
        {
            LibraryType = libraryType;
            PackageName = packageName;
            Version = version;
            Hash = hash;
            Assemblies = assemblies;
            Dependencies = dependencies;
            Serviceable = serviceable;
        }

        public string LibraryType { get; }

        public string PackageName { get; }

        public string Version { get; }

        public string Hash { get; }

        public IReadOnlyList<string> Assemblies { get; }

        public IReadOnlyList<Dependency> Dependencies { get; }

        public bool Serviceable { get; }
    }
}
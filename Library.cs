// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel
{
    public class Library
    {
        public Library(string libraryType, string packageName, string version, string hash, Dependency[] dependencies, bool serviceable)
        {
            LibraryType = libraryType;
            PackageName = packageName;
            Version = version;
            Hash = hash;
            Dependencies = dependencies;
            Serviceable = serviceable;
        }

        public string LibraryType { get; }

        public string PackageName { get; }

        public string Version { get; }

        public string Hash { get; }

        public IReadOnlyList<Dependency> Dependencies { get; }

        public bool Serviceable { get; }
    }
}
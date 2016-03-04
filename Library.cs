// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel
{
    public class Library
    {
        public Library(string type, string name, string version, string hash, Dependency[] dependencies, bool serviceable)
        {
            Type = type;
            Name = name;
            Version = version;
            Hash = hash;
            Dependencies = dependencies;
            Serviceable = serviceable;
        }

        public string Type { get; }

        public string Name { get; }

        public string Version { get; }

        public string Hash { get; }

        public IReadOnlyList<Dependency> Dependencies { get; }

        public bool Serviceable { get; }
    }
}
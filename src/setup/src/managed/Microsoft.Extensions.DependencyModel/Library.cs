// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel
{
    public class Library
    {
        public Library(string type,
            string name,
            string version,
            string hash,
            IEnumerable<Dependency> dependencies,
            bool serviceable)
            : this(type, name, version, hash, dependencies, serviceable, path: null, hashPath: null)
        {
        }

        public Library(string type,
            string name,
            string version,
            string hash,
            IEnumerable<Dependency> dependencies,
            bool serviceable,
            string path,
            string hashPath)
            : this(type, name, version, hash, dependencies, serviceable, path, hashPath, runtimeStoreManifestName: null)
        {
        }
        public Library(string type,
            string name,
            string version,
            string hash,
            IEnumerable<Dependency> dependencies,
            bool serviceable,
            string path,
            string hashPath,
            string runtimeStoreManifestName = null)
        {
            if (string.IsNullOrEmpty(type))
            {
                throw new ArgumentException(nameof(type));
            }
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(nameof(name));
            }
            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException(nameof(version));
            }
            if (dependencies == null)
            {
                throw new ArgumentNullException(nameof(dependencies));
            }
            Type = type;
            Name = name;
            Version = version;
            Hash = hash;
            Dependencies = dependencies.ToArray();
            Serviceable = serviceable;
            Path = path;
            HashPath = hashPath;
            RuntimeStoreManifestName = runtimeStoreManifestName;
        }

        public string Type { get; }

        public string Name { get; }

        public string Version { get; }

        public string Hash { get; }

        public IReadOnlyList<Dependency> Dependencies { get; }

        public bool Serviceable { get; }

        public string Path { get; }

        public string HashPath { get; }

        public string RuntimeStoreManifestName {get;}
    }
}

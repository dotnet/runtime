// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics.Hashing;

namespace Microsoft.Extensions.DependencyModel
{
    public readonly struct Dependency : IEquatable<Dependency>
    {
        public Dependency(string name, string version)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentException(null, nameof(name));
            }
            if (string.IsNullOrEmpty(version))
            {
                throw new ArgumentException(null, nameof(version));
            }
            Name = name;
            Version = version;
        }

        public string Name { get; }
        public string Version { get; }

        public bool Equals(Dependency other)
        {
            return string.Equals(Name, other.Name) && string.Equals(Version, other.Version);
        }

        public override bool Equals(object obj) => obj is Dependency dependency && Equals(dependency);

        public override int GetHashCode() =>
            HashHelpers.Combine(Name.GetHashCode(), Version.GetHashCode());
    }
}

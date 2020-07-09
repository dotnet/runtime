// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics.Hashing;

namespace Microsoft.Extensions.DependencyModel
{
    public struct Dependency
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

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Dependency && Equals((Dependency) obj);
        }

        public override int GetHashCode() =>
            HashHelpers.Combine(Name.GetHashCode(), Version.GetHashCode());
    }
}

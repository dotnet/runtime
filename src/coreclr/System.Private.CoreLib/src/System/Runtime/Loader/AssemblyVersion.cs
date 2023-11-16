// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Loader
{
    internal struct AssemblyVersion : IEquatable<AssemblyVersion>
    {
        public int Major;
        public int Minor;
        public int Build;
        public int Revision;

        public const int Unspecified = -1;

        public AssemblyVersion()
        {
            Major = Unspecified;
            Minor = Unspecified;
            Build = Unspecified;
            Revision = Unspecified;
        }

        public bool HasMajor => Major != Unspecified;

        public bool HasMinor => Minor != Unspecified;

        public bool HasBuild => Build != Unspecified;

        public bool HasRevision => Revision != Unspecified;

        public bool Equals(AssemblyVersion other) =>
            Major == other.Major &&
            Minor == other.Minor &&
            Build == other.Build &&
            Revision == other.Revision;

        public override bool Equals(object? obj)
            => obj is AssemblyVersion other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Major, Minor, Build, Revision);
    }
}

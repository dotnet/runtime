// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.NETCore.Platforms.BuildTasks
{

    /// <summary>
    /// A Version class that also supports a single integer (major only)
    /// </summary>
    public sealed class RuntimeVersion : IComparable, IComparable<RuntimeVersion>, IEquatable<RuntimeVersion>
    {
        private string versionString;
        private Version version;
        private bool hasMinor;

        public RuntimeVersion(string versionString)
        {
            // intentionally don't support the type of version that omits the separators as it is abiguous.
            // for example Windows 8.1 was encoded as win81, where as Windows 10.0 was encoded as win10
            this.versionString = versionString;
            string toParse = versionString;
#if NETCOREAPP
            if (!toParse.Contains('.'))
#else
            if (toParse.IndexOf('.') == -1)
#endif
            {
                toParse += ".0";
                hasMinor = false;
            }
            else
            {
                hasMinor = true;
            }
            version = Version.Parse(toParse);
        }

        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                return 1;
            }

            if (obj is RuntimeVersion version)
            {
                return CompareTo(version);
            }

            throw new ArgumentException($"Cannot compare {nameof(RuntimeVersion)} to object of type {obj.GetType()}.", nameof(obj));
        }

        public int CompareTo(RuntimeVersion other)
        {
            if (other == null)
            {
                return 1;
            }

            int versionResult = version.CompareTo(other?.version);

            if (versionResult == 0)
            {
                if (!hasMinor && other.hasMinor)
                {
                    return -1;
                }

                if (hasMinor && !other.hasMinor)
                {
                    return 1;
                }

                return string.CompareOrdinal(versionString, other.versionString);
            }

            return versionResult;
        }

        public bool Equals(RuntimeVersion other)
        {
            return object.ReferenceEquals(other, this) ||
                (other != null &&
                versionString.Equals(other.versionString, StringComparison.Ordinal));
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RuntimeVersion);
        }

        public override int GetHashCode()
        {
            return versionString.GetHashCode();
        }

        public override string ToString()
        {
            return versionString;
        }

        public static bool operator ==(RuntimeVersion v1, RuntimeVersion v2)
        {
            if (v2 is null)
            {
                return (v1 is null) ? true : false;
            }

            return ReferenceEquals(v2, v1) ? true : v2.Equals(v1);
        }

        public static bool operator !=(RuntimeVersion v1, RuntimeVersion v2) => !(v1 == v2);

        public static bool operator <(RuntimeVersion v1, RuntimeVersion v2)
        {
            if (v1 is null)
            {
                return !(v2 is null);
            }

            return v1.CompareTo(v2) < 0;
        }

        public static bool operator <=(RuntimeVersion v1, RuntimeVersion v2)
        {
            if (v1 is null)
            {
                return true;
            }

            return v1.CompareTo(v2) <= 0;
        }

        public static bool operator >(RuntimeVersion v1, RuntimeVersion v2) => v2 < v1;

        public static bool operator >=(RuntimeVersion v1, RuntimeVersion v2) => v2 <= v1;
    }
}

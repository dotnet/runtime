// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Globalization
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class SortVersion : IEquatable<SortVersion?>
    {
        private int m_NlsVersion; // Do not rename (binary serialization)
        private Guid m_SortId; // Do not rename (binary serialization)

        public int FullVersion => m_NlsVersion;

        public Guid SortId => m_SortId;

        public SortVersion(int fullVersion, Guid sortId)
        {
            m_SortId = sortId;
            m_NlsVersion = fullVersion;
        }

        internal SortVersion(int nlsVersion, int effectiveId, Guid customVersion)
        {
            m_NlsVersion = nlsVersion;

            if (customVersion == Guid.Empty)
            {
                byte b1 = (byte)(effectiveId >> 24);
                byte b2 = (byte)((effectiveId & 0x00FF0000) >> 16);
                byte b3 = (byte)((effectiveId & 0x0000FF00) >> 8);
                byte b4 = (byte)(effectiveId & 0xFF);
                customVersion = new Guid(0, 0, 0, 0, 0, 0, 0, b1, b2, b3, b4);
            }

            m_SortId = customVersion;
        }

        public override bool Equals(object? obj)
        {
            return obj is SortVersion otherVersion && Equals(otherVersion);
        }

#pragma warning disable CS8614 // TODO-NULLABLE: Covariant interface arguments (https://github.com/dotnet/roslyn/issues/35817)
        public bool Equals(SortVersion? other)
#pragma warning restore CS8614
        {
            if (other == null)
            {
                return false;
            }

            return m_NlsVersion == other.m_NlsVersion && m_SortId == other.m_SortId;
        }

        public override int GetHashCode()
        {
            return m_NlsVersion * 7 | m_SortId.GetHashCode();
        }

        // Force inline as the true/false ternary takes it above ALWAYS_INLINE size even though the asm ends up smaller
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(SortVersion? left, SortVersion? right)
        {
            // Test "right" first to allow branch elimination when inlined for null checks (== null)
            // so it can become a simple test
            if (right is null)
            {
                // return true/false not the test result https://github.com/dotnet/coreclr/issues/914
                return (left is null) ? true : false;
            }

            return right.Equals(left);
        }

        public static bool operator !=(SortVersion? left, SortVersion? right)
        {
            return !(left == right);
        }
    }
}

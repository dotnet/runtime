// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace System
{
    // Contains helpers for calculating randomized hash codes of common types.
    // Since these hash codes are randomized, callers must not persist them between
    // AppDomain restarts. There's still the potential for limited collisions
    // if two distinct types have the same bit pattern (e.g., string.Empty and (int)0).
    // This should be acceptable because the number of practical collisions is
    // limited by the number of distinct types used here, and we expect callers to
    // have a small, fixed set of accepted types for any hash-based collection.
    // If we really do need to address this in the future, we can use a seed per type
    // rather than a global seed for the entire AppDomain.
    internal static class HashCodeRandomization
    {
        public static int GetRandomizedOrdinalHashCode(this string value)
        {
#if NET
            // In .NET Core, string hash codes are already randomized.

            return value.GetHashCode();
#else
            // Downlevel, we need to perform randomization ourselves.

            ReadOnlySpan<char> charSpan = value.AsSpan();
            ReadOnlySpan<byte> byteSpan = MemoryMarshal.AsBytes(charSpan);
            return Marvin.ComputeHash32(byteSpan, Marvin.DefaultSeed);
#endif
        }

        public static int GetRandomizedHashCode(this int value)
        {
            return HashCode.Combine(value);
        }
    }
}

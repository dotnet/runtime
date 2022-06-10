// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    // Contains helpers for calculating randomized hash codes of common types.
    // These hash codes must not be persisted between AppDomain restarts.
    // There's still the potential for two distinct types with the same bit
    // pattern (for example, string.Empty and int.Zero) to produce the same
    // hash code, since the HashCode seed is global per AppDomain and not per type.
    // This shouldn't be a problem for most callers since the number of
    // possible types is expected to be small anyway for realistic scenarios.
    internal static class HashCodeRandomization
    {
        public static int GetRandomizedOrdinalHashCode(this string value)
        {
#if NETCOREAPP
            // In .NET Core, string hash codes are already randomized.

            return value.GetHashCode();
#else
            // Downlevel, we need to perform randomization ourselves.
            // This allows "Hello!" and "Hello!\0" to have the same hash
            // code, which is acceptable for the scenarios we care about.
            // We'll pull out pairs of chars and write 32-bit ints at a time.

            HashCode hashCode = default;
            int pair = 0;
            for (int i = 0; i < value.Length; i++)
            {
                int ch = value[i];
                if ((i & 1) == 0)
                {
                    pair = ch << 16; // first member of pair
                }
                else
                {
                    pair |= ch; // second member of pair
                    hashCode.Add(pair); // write pair as single unit
                    pair = 0;
                }
            }
            hashCode.Add(pair); // flush any leftover data (could be 0)
            return hashCode.ToHashCode();
#endif
        }

        public static int GetRandomizedHashCode(this int value)
        {
            return HashCode.Combine(value);
        }
    }
}

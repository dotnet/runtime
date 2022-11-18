// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
#if NETCOREAPP
            // In .NET Core, string hash codes are already randomized.

            return value.GetHashCode();
#else
            // Downlevel, we need to perform randomization ourselves. There's still
            // the potential for limited collisions ("Hello!" and "Hello!\0"), but
            // this shouldn't be a problem in practice. If we need to address it,
            // we can mix the string length into the accumulator before running the
            // string contents through.
            //
            // We'll pull out pairs of chars and write 32 bits at a time.

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
            hashCode.Add(pair); // flush any leftover data (could be 0 or 1 chars)
            return hashCode.ToHashCode();
#endif
        }

        public static int GetRandomizedHashCode(this int value)
        {
            return HashCode.Combine(value);
        }
    }
}

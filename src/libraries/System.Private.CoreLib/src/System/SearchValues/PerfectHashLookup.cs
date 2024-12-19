// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers
{
    internal static class PerfectHashLookup
    {
        private const int MaxModulus = char.MaxValue + 1;

        // Hash entries store each value from the set at the index determined by the remainder modulo the table size.
        // As every value has a unique remainder, we can check if a value is contained in the set by checking
        // hashEntries[value % hashEntries.Length] == value (see Contains below).
        // The multiplier is used for faster modulo operations when determining the hash table index.
        public static void Initialize(ReadOnlySpan<char> values, int maxInclusive, out uint multiplier, out char[] hashEntries)
        {
            Debug.Assert(!values.IsEmpty);

            uint modulus = FindModulus(values, maxInclusive);
            multiplier = GetFastModMultiplier(modulus);
            hashEntries = new char[modulus];

            // Some hash entries will remain unused.
            // We can't leave them uninitialized as we would otherwise erroneously match (char)0.
            // The exact value doesn't matter, as long as it's in the set of our values.
            hashEntries.AsSpan().Fill(values[0]);

            foreach (char c in values)
            {
                hashEntries[FastMod(c, modulus, multiplier)] = c;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(char[] hashEntries, uint multiplier, char value)
        {
            ulong offset = FastMod(value, (uint)hashEntries.Length, multiplier);
            Debug.Assert(offset < (ulong)hashEntries.Length);

#if NET8_0_OR_GREATER
            return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(hashEntries), (nuint)offset) == value;
#else
            return hashEntries[(nuint)offset] == value;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf(char[] hashEntries, uint multiplier, char value)
        {
            ulong offset = FastMod(value, (uint)hashEntries.Length, multiplier);
            Debug.Assert(offset < (ulong)hashEntries.Length);

#if NET8_0_OR_GREATER
            if (Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(hashEntries), (nuint)offset) == value)
#else
            if (hashEntries[(nuint)offset] == value)
#endif
            {
                return (int)offset;
            }

            return -1;
        }

        /// <summary>Finds a modulus where remainders for all values in the set are unique.</summary>
        private static uint FindModulus(ReadOnlySpan<char> values, int maxInclusive)
        {
            Debug.Assert(maxInclusive <= char.MaxValue);

            int modulus = HashHelpers.GetPrime(values.Length);
            bool removedDuplicates = false;

            if (modulus >= maxInclusive)
            {
                return (uint)(maxInclusive + 1);
            }

            while (true)
            {
                if (modulus >= maxInclusive)
                {
                    // Try to remove duplicates and try again.
                    if (!removedDuplicates && TryRemoveDuplicates(values, out char[]? deduplicated))
                    {
                        removedDuplicates = true;
                        values = deduplicated;
                        modulus = HashHelpers.GetPrime(values.Length);
                        continue;
                    }

                    return (uint)(maxInclusive + 1);
                }

                if (TestModulus(values, modulus))
                {
                    return (uint)modulus;
                }

                modulus = HashHelpers.GetPrime(modulus + 1);
            }

            static bool TestModulus(ReadOnlySpan<char> values, int modulus)
            {
                Debug.Assert(modulus < MaxModulus);

                bool[] seen = ArrayPool<bool>.Shared.Rent(modulus);
                seen.AsSpan(0, modulus).Clear();

                uint multiplier = GetFastModMultiplier((uint)modulus);

                foreach (char c in values)
                {
                    ulong index = FastMod(c, (uint)modulus, multiplier);

                    if (seen[index])
                    {
                        ArrayPool<bool>.Shared.Return(seen);
                        return false;
                    }

                    seen[index] = true;
                }

                // Saw no duplicates.
                ArrayPool<bool>.Shared.Return(seen);
                return true;
            }

            static bool TryRemoveDuplicates(ReadOnlySpan<char> values, [NotNullWhen(true)] out char[]? deduplicated)
            {
                HashSet<char> unique = [.. values];

                if (unique.Count == values.Length)
                {
                    deduplicated = null;
                    return false;
                }

                deduplicated = new char[unique.Count];
                unique.CopyTo(deduplicated);
                return true;
            }
        }

        // This is a variant of HashHelpers.GetFastModMultiplier, specialized for smaller divisors (<= 65536).
        private static uint GetFastModMultiplier(uint divisor)
        {
            Debug.Assert(divisor > 0);
            Debug.Assert(divisor <= MaxModulus);

            return uint.MaxValue / divisor + 1;
        }

        // This is a faster variant of HashHelpers.FastMod, specialized for smaller divisors (<= 65536).
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong FastMod(char value, uint divisor, uint multiplier)
        {
            Debug.Assert(multiplier == GetFastModMultiplier(divisor));

            ulong result = ((ulong)(multiplier * value) * divisor) >> 32;

            Debug.Assert(result == (value % divisor));
            return result;
        }
    }
}

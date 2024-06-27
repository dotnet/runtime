// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#pragma warning disable CS8500 // Takes the address of a managed type

namespace System.Buffers
{
    /// <summary>
    /// Stores the state necessary to call vectorized members on <see cref="ProbabilisticMap"/>,
    /// as well as (optionally) a precomputed perfect hash table for faster single-character lookups/match confirmations.
    /// When the hash table isn't available, the structure stores a pointer to the span of values in the set instead.
    /// </summary>
    internal unsafe struct ProbabilisticMapState
    {
        private const int MaxModulus = char.MaxValue + 1;

        public ProbabilisticMap Map;

        // Hash entries store each value from the set at the index determined by the remainder modulo the table size.
        // As every value has a unique remainder, we can check if a value is contained in the set by checking
        // _hashEntries[value % _hashEntries.Length] == value (see FastContains below).
        // The multiplier is used for faster modulo operations when determining the hash table index.
        // Exactly one of _hashEntries and _slowContainsValuesPtr may be initialized at the same time.
        private readonly uint _multiplier;
        private readonly char[]? _hashEntries;
        private readonly ReadOnlySpan<char>* _slowContainsValuesPtr;

        public ProbabilisticMapState(ReadOnlySpan<char> values, int maxInclusive)
        {
            Debug.Assert(!values.IsEmpty);

            Map = new ProbabilisticMap(values);

            uint modulus = FindModulus(values, maxInclusive);
            _multiplier = GetFastModMultiplier(modulus);
            _hashEntries = new char[modulus];

            // Some hash entries will remain unused.
            // We can't leave them uninitialized as we would otherwise erroneously match (char)0.
            // The exact value doesn't matter, as long as it's in the set of our values.
            _hashEntries.AsSpan().Fill(values[0]);

            foreach (char c in values)
            {
                _hashEntries[FastMod(c, modulus, _multiplier)] = c;
            }
        }

        // valuesPtr must remain valid for as long as this ProbabilisticMapState is used.
        public unsafe ProbabilisticMapState(ReadOnlySpan<char>* valuesPtr)
        {
            Debug.Assert((IntPtr)valuesPtr != IntPtr.Zero);

            Map = new ProbabilisticMap(*valuesPtr);
            _slowContainsValuesPtr = valuesPtr;
        }

        public char[] GetValues()
        {
            Debug.Assert(_hashEntries is not null);

            var unique = new HashSet<char>(_hashEntries);
            char[] values = new char[unique.Count];
            unique.CopyTo(values);
            return values;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool FastContains(char value)
        {
            Debug.Assert(_hashEntries is not null);
            Debug.Assert((IntPtr)_slowContainsValuesPtr == IntPtr.Zero);

            return FastContains(_hashEntries, _multiplier, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool FastContains(char[] hashEntries, uint multiplier, char value)
        {
            ulong offset = FastMod(value, (uint)hashEntries.Length, multiplier);
            Debug.Assert(offset < (ulong)hashEntries.Length);

            return Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(hashEntries), (nuint)offset) == value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SlowProbabilisticContains(char value)
        {
            Debug.Assert(_hashEntries is null);
            Debug.Assert((IntPtr)_slowContainsValuesPtr != IntPtr.Zero);

            return ProbabilisticMap.Contains(
                ref Unsafe.As<ProbabilisticMap, uint>(ref Map),
                *_slowContainsValuesPtr,
                value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SlowContains(char value)
        {
            Debug.Assert(_hashEntries is null);
            Debug.Assert((IntPtr)_slowContainsValuesPtr != IntPtr.Zero);

            return ProbabilisticMap.Contains(*_slowContainsValuesPtr, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool ConfirmProbabilisticMatch<TUseFastContains>(char value)
            where TUseFastContains : struct, SearchValues.IRuntimeConst
        {
            if (TUseFastContains.Value)
            {
                return FastContains(value);
            }
            else
            {
                // We use SlowContains instead of SlowProbabilisticContains here as we've already checked
                // the value against the probabilistic filter and are now confirming the potential match.
                return SlowContains(value);
            }
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOfAnySimpleLoop<TUseFastContains, TNegator>(ref char searchSpace, int searchSpaceLength, ref ProbabilisticMapState state)
            where TUseFastContains : struct, SearchValues.IRuntimeConst
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            ref char searchSpaceEnd = ref Unsafe.Add(ref searchSpace, searchSpaceLength);
            ref char cur = ref searchSpace;

            if (TUseFastContains.Value)
            {
                Debug.Assert(state._hashEntries is not null);

                char[] hashEntries = state._hashEntries;
                uint multiplier = state._multiplier;

                while (!Unsafe.AreSame(ref cur, ref searchSpaceEnd))
                {
                    char c = cur;
                    if (TNegator.NegateIfNeeded(FastContains(hashEntries, multiplier, c)))
                    {
                        return (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref cur) / sizeof(char));
                    }

                    cur = ref Unsafe.Add(ref cur, 1);
                }
            }
            else
            {
                while (!Unsafe.AreSame(ref cur, ref searchSpaceEnd))
                {
                    char c = cur;
                    if (TNegator.NegateIfNeeded(state.SlowProbabilisticContains(c)))
                    {
                        return (int)((nuint)Unsafe.ByteOffset(ref searchSpace, ref cur) / sizeof(char));
                    }

                    cur = ref Unsafe.Add(ref cur, 1);
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOfAnySimpleLoop<TUseFastContains, TNegator>(ref char searchSpace, int searchSpaceLength, ref ProbabilisticMapState state)
            where TUseFastContains : struct, SearchValues.IRuntimeConst
            where TNegator : struct, IndexOfAnyAsciiSearcher.INegator
        {
            if (TUseFastContains.Value)
            {
                Debug.Assert(state._hashEntries is not null);

                char[] hashEntries = state._hashEntries;
                uint multiplier = state._multiplier;

                while (--searchSpaceLength >= 0)
                {
                    char c = Unsafe.Add(ref searchSpace, searchSpaceLength);
                    if (TNegator.NegateIfNeeded(FastContains(hashEntries, multiplier, c)))
                    {
                        break;
                    }
                }
            }
            else
            {
                while (--searchSpaceLength >= 0)
                {
                    char c = Unsafe.Add(ref searchSpace, searchSpaceLength);
                    if (TNegator.NegateIfNeeded(state.SlowProbabilisticContains(c)))
                    {
                        break;
                    }
                }
            }

            return searchSpaceLength;
        }
    }
}

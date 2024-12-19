// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    /// <summary>
    /// Stores the state necessary to call vectorized members on <see cref="ProbabilisticMap"/>,
    /// as well as (optionally) a precomputed perfect hash table for faster single-character lookups/match confirmations.
    /// When the hash table isn't available, the structure stores a pointer to the span of values in the set instead.
    /// </summary>
    internal unsafe struct ProbabilisticMapState
    {
        public ProbabilisticMap Map;

        // _multiplier and _hashEntries are state required for PerfectHashLookup.
        // Exactly one of _hashEntries and _slowContainsValuesPtr may be initialized at the same time.
        private readonly uint _multiplier;
        private readonly char[]? _hashEntries;
        private readonly ReadOnlySpan<char>* _slowContainsValuesPtr;

        public ProbabilisticMapState(ReadOnlySpan<char> values, int maxInclusive)
        {
            Debug.Assert(!values.IsEmpty);

            Map = new ProbabilisticMap(values);
            PerfectHashLookup.Initialize(values, maxInclusive, out _multiplier, out _hashEntries);
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

            return PerfectHashLookup.Contains(_hashEntries, _multiplier, value);
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
                    if (TNegator.NegateIfNeeded(PerfectHashLookup.Contains(hashEntries, multiplier, c)))
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
                    if (TNegator.NegateIfNeeded(PerfectHashLookup.Contains(hashEntries, multiplier, c)))
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

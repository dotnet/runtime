// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers
{
    /// <summary>
    /// Provides a set of initialization methods for instances of the <see cref="SearchValues{T}"/> class.
    /// </summary>
    /// <remarks>
    /// SearchValues are optimized for situations where the same set of values is frequently used for searching at runtime.
    /// </remarks>
    public static class SearchValues
    {
        /// <summary>
        /// Creates an optimized representation of <paramref name="values"/> used for efficient searching.
        /// </summary>
        /// <param name="values">The set of values.</param>
        /// <returns>The optimized representation of <paramref name="values"/> used for efficient searching.</returns>
        public static SearchValues<byte> Create(params ReadOnlySpan<byte> values)
        {
            if (values.IsEmpty)
            {
                return new EmptySearchValues<byte>();
            }

            if (values.Length == 1)
            {
                return new Any1SearchValues<byte, byte>(values);
            }

            // RangeByteSearchValues is slower than SingleByteSearchValues, but faster than Any2ByteSearchValues
            if (TryGetSingleRange(values, out byte minInclusive, out byte maxInclusive))
            {
                return new RangeByteSearchValues(minInclusive, maxInclusive);
            }

            if (values.Length <= 5)
            {
                Debug.Assert(values.Length is 2 or 3 or 4 or 5);
                return values.Length switch
                {
                    2 => new Any2SearchValues<byte, byte>(values),
                    3 => new Any3SearchValues<byte, byte>(values),
                    4 => new Any4SearchValues<byte, byte>(values),
                    _ => new Any5SearchValues<byte, byte>(values),
                };
            }

            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && maxInclusive < 128)
            {
                return new AsciiByteSearchValues(values);
            }

            return new AnyByteSearchValues(values);
        }

        /// <summary>
        /// Creates an optimized representation of <paramref name="values"/> used for efficient searching.
        /// </summary>
        /// <param name="values">The set of values.</param>
        /// /// <returns>The optimized representation of <paramref name="values"/> used for efficient searching.</returns>
        public static SearchValues<char> Create(params ReadOnlySpan<char> values)
        {
            if (values.IsEmpty)
            {
                return new EmptySearchValues<char>();
            }

            // Vector128<char> isn't valid. Treat the values as shorts instead.
            ReadOnlySpan<short> shortValues = MemoryMarshal.Cast<char, short>(values);

            if (values.Length == 1)
            {
                char value = values[0];

                return PackedSpanHelpers.PackedIndexOfIsSupported && PackedSpanHelpers.CanUsePackedIndexOf(value)
                    ? new Any1CharPackedSearchValues(value)
                    : new Any1SearchValues<char, short>(shortValues);
            }

            // RangeCharSearchValues is slower than SingleCharSearchValues, but faster than Any2CharSearchValues
            if (TryGetSingleRange(values, out char minInclusive, out char maxInclusive))
            {
                return PackedSpanHelpers.PackedIndexOfIsSupported && PackedSpanHelpers.CanUsePackedIndexOf(minInclusive) && PackedSpanHelpers.CanUsePackedIndexOf(maxInclusive)
                    ? new RangeCharSearchValues<TrueConst>(minInclusive, maxInclusive)
                    : new RangeCharSearchValues<FalseConst>(minInclusive, maxInclusive);
            }

            if (values.Length == 2)
            {
                char value0 = values[0];
                char value1 = values[1];

                if (PackedSpanHelpers.PackedIndexOfIsSupported && PackedSpanHelpers.CanUsePackedIndexOf(value0) && PackedSpanHelpers.CanUsePackedIndexOf(value1))
                {
                    // If the two values are the same ASCII letter with both cases, we can use an approach that
                    // reduces the number of comparisons by masking off the bit that differs between lower and upper case (0x20).
                    // While this most commonly applies to ASCII letters, it also works for other values that differ by 0x20 (e.g. "[{" => "{").
                    return (value0 ^ value1) == 0x20
                        ? new Any1CharPackedIgnoreCaseSearchValues((char)Math.Max(value0, value1))
                        : new Any2CharPackedSearchValues(value0, value1);
                }

                return new Any2SearchValues<char, short>(shortValues);
            }

            if (values.Length == 3)
            {
                char value0 = values[0];
                char value1 = values[1];
                char value2 = values[2];

                return PackedSpanHelpers.PackedIndexOfIsSupported && PackedSpanHelpers.CanUsePackedIndexOf(value0) && PackedSpanHelpers.CanUsePackedIndexOf(value1) && PackedSpanHelpers.CanUsePackedIndexOf(value2)
                    ? new Any3CharPackedSearchValues(value0, value1, value2)
                    : new Any3SearchValues<char, short>(shortValues);
            }

            // IndexOfAnyAsciiSearcher for chars is slower than Any3CharSearchValues, but faster than Any4SearchValues
            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && maxInclusive < 128)
            {
                // If the values are sets of 2 ASCII letters with both cases, we can use an approach that
                // reduces the number of comparisons by masking off the bit that differs between lower and upper case (0x20).
                // While this most commonly applies to ASCII letters, it also works for other values that differ by 0x20 (e.g. "[]{}" => "{}").
                if (PackedSpanHelpers.PackedIndexOfIsSupported && values.Length == 4 && minInclusive > 0)
                {
                    Span<char> copy = stackalloc char[4];
                    values.CopyTo(copy);
                    copy.Sort();

                    if ((copy[0] ^ copy[2]) == 0x20 &&
                        (copy[1] ^ copy[3]) == 0x20)
                    {
                        // We pick the higher two values (with the 0x20 bit set). "AaBb" => 'a', 'b'
                        return new Any2CharPackedIgnoreCaseSearchValues(copy[2], copy[3]);
                    }
                }

                return (Ssse3.IsSupported || PackedSimd.IsSupported) && minInclusive == 0
                    ? new AsciiCharSearchValues<IndexOfAnyAsciiSearcher.Ssse3AndWasmHandleZeroInNeedle>(values)
                    : new AsciiCharSearchValues<IndexOfAnyAsciiSearcher.Default>(values);
            }

            if (values.Length == 4)
            {
                return new Any4SearchValues<char, short>(shortValues);
            }

            if (values.Length == 5)
            {
                return new Any5SearchValues<char, short>(shortValues);
            }

            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && minInclusive < 128)
            {
                // We have a mix of ASCII and non-ASCII characters.

                if (IndexOfAnyAsciiSearcher.TryComputeAsciiWithSecondSetState(values, maxInclusive, out IndexOfAnyAsciiSearcher.AsciiWithSecondSetState state))
                {
                    // All of the non-ASCII values fit within a range of 128 characters.
                    // Use an implementation that checks against two 128-bit bitmaps, with the second one at a variable offset to the start of the non-ASCII set.
                    return (Ssse3.IsSupported || PackedSimd.IsSupported) && minInclusive == 0
                        ? new AsciiWithSecondSetCharSearchValues<IndexOfAnyAsciiSearcher.Ssse3AndWasmHandleZeroInNeedle>(state)
                        : new AsciiWithSecondSetCharSearchValues<IndexOfAnyAsciiSearcher.Default>(state);
                }

                // Use an implementation that does an optimistic ASCII fast-path and then falls back to the ProbabilisticMap.
                return (Ssse3.IsSupported || PackedSimd.IsSupported) && minInclusive == 0
                    ? new ProbabilisticWithAsciiCharSearchValues<IndexOfAnyAsciiSearcher.Ssse3AndWasmHandleZeroInNeedle>(values, maxInclusive)
                    : new ProbabilisticWithAsciiCharSearchValues<IndexOfAnyAsciiSearcher.Default>(values, maxInclusive);
            }

            if (ShouldUseProbabilisticMap(values.Length, maxInclusive))
            {
                return new ProbabilisticCharSearchValues(values, maxInclusive);
            }

            // This will also match ASCII values when IndexOfAnyAsciiSearcher is not supported.
            return new BitmapCharSearchValues(values, maxInclusive);

            static bool ShouldUseProbabilisticMap(int valuesLength, int maxInclusive)
            {
                // *Rough estimates*. The current implementation uses 256 bits for the bloom filter.
                // If the implementation is vectorized we can get away with a decent false positive rate.
                const int MaxValuesForProbabilisticMap = 256;

                if (valuesLength > MaxValuesForProbabilisticMap)
                {
                    // If the number of values is too high, we won't see any benefits from the 'probabilistic' part.
                    return false;
                }

                if (Sse41.IsSupported || AdvSimd.Arm64.IsSupported)
                {
                    // If the probabilistic map is vectorized, we prefer it.
                    return true;
                }

                // The probabilistic map is more memory efficient for spare sets, while the bitmap is more efficient for dense sets.
                int bitmapFootprintBytesEstimate = 64 + (maxInclusive / 8);
                int probabilisticFootprintBytesEstimate = 128 + (valuesLength * 4);

                // The bitmap is a bit faster than the perfect hash checks employed by the probabilistic map.
                // Sacrifice some memory usage for faster lookups.
                const int AcceptableSizeMultiplier = 2;

                return AcceptableSizeMultiplier * probabilisticFootprintBytesEstimate < bitmapFootprintBytesEstimate;
            }
        }

        /// <summary>
        /// Creates an optimized representation of <paramref name="values"/> used for efficient searching.
        /// </summary>
        /// <param name="values">The set of values.</param>
        /// <param name="comparisonType">Specifies whether to use <see cref="StringComparison.Ordinal"/> or <see cref="StringComparison.OrdinalIgnoreCase"/> search semantics.</param>
        /// <returns>The optimized representation of <paramref name="values"/> used for efficient searching.</returns>
        /// <remarks>Only <see cref="StringComparison.Ordinal"/> or <see cref="StringComparison.OrdinalIgnoreCase"/> may be used.</remarks>
        public static SearchValues<string> Create(ReadOnlySpan<string> values, StringComparison comparisonType)
        {
            if (comparisonType is not (StringComparison.Ordinal or StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException(SR.Argument_SearchValues_UnsupportedStringComparison, nameof(comparisonType));
            }

            return StringSearchValues.Create(values, ignoreCase: comparisonType == StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetSingleRange<T>(ReadOnlySpan<T> values, out T minInclusive, out T maxInclusive)
            where T : struct, INumber<T>, IMinMaxValue<T>
        {
            T min = T.MaxValue;
            T max = T.MinValue;

            foreach (T value in values)
            {
                min = T.Min(min, value);
                max = T.Max(max, value);
            }

            minInclusive = min;
            maxInclusive = max;

            uint range = uint.CreateChecked(max - min) + 1;
            if (range > values.Length)
            {
                return false;
            }

            Span<bool> seenValues = range <= 256 ? stackalloc bool[256] : new bool[range];
            seenValues = seenValues.Slice(0, (int)range);
            seenValues.Clear();

            foreach (T value in values)
            {
                int offset = int.CreateChecked(value - min);
                seenValues[offset] = true;
            }

            if (seenValues.Contains(false))
            {
                return false;
            }

            return true;
        }

        internal interface IRuntimeConst
        {
            static abstract bool Value { get; }
        }

        internal readonly struct TrueConst : IRuntimeConst
        {
            public static bool Value => true;
        }

        internal readonly struct FalseConst : IRuntimeConst
        {
            public static bool Value => false;
        }
    }
}

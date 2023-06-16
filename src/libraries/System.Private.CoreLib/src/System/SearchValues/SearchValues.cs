// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Wasm;
using System.Runtime.Intrinsics.X86;

#pragma warning disable 8500 // address of managed types

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
        public static SearchValues<byte> Create(ReadOnlySpan<byte> values)
        {
            if (values.IsEmpty)
            {
                return new EmptySearchValues<byte>();
            }

            if (values.Length == 1)
            {
                return new SingleByteSearchValues(values);
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
                    2 => new Any2ByteSearchValues(values),
                    3 => new Any3ByteSearchValues(values),
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
        public static SearchValues<char> Create(ReadOnlySpan<char> values)
        {
            if (values.IsEmpty)
            {
                return new EmptySearchValues<char>();
            }

            if (values.Length == 1)
            {
                char value = values[0];
                return PackedSpanHelpers.PackedIndexOfIsSupported && PackedSpanHelpers.CanUsePackedIndexOf(value)
                    ? new SingleCharSearchValues<TrueConst>(value)
                    : new SingleCharSearchValues<FalseConst>(value);
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
                return PackedSpanHelpers.PackedIndexOfIsSupported && PackedSpanHelpers.CanUsePackedIndexOf(value0) && PackedSpanHelpers.CanUsePackedIndexOf(value1)
                    ? new Any2CharSearchValues<TrueConst>(value0, value1)
                    : new Any2CharSearchValues<FalseConst>(value0, value1);
            }

            if (values.Length == 3)
            {
                char value0 = values[0];
                char value1 = values[1];
                char value2 = values[2];
                return PackedSpanHelpers.PackedIndexOfIsSupported && PackedSpanHelpers.CanUsePackedIndexOf(value0) && PackedSpanHelpers.CanUsePackedIndexOf(value1) && PackedSpanHelpers.CanUsePackedIndexOf(value2)
                    ? new Any3CharSearchValues<TrueConst>(value0, value1, value2)
                    : new Any3CharSearchValues<FalseConst>(value0, value1, value2);
            }

            // IndexOfAnyAsciiSearcher for chars is slower than Any3CharSearchValues, but faster than Any4SearchValues
            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && maxInclusive < 128)
            {
                IndexOfAnyAsciiSearcher.ComputeBitmap(values, out Vector256<byte> bitmap, out BitVector256 lookup);

                return (Ssse3.IsSupported || PackedSimd.IsSupported) && lookup.Contains(0)
                    ? new AsciiCharSearchValues<IndexOfAnyAsciiSearcher.Ssse3AndWasmHandleZeroInNeedle>(bitmap, lookup)
                    : new AsciiCharSearchValues<IndexOfAnyAsciiSearcher.Default>(bitmap, lookup);
            }

            // Vector128<char> isn't valid. Treat the values as shorts instead.
            ReadOnlySpan<short> shortValues = MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(values)),
                values.Length);

            if (values.Length == 4)
            {
                return new Any4SearchValues<char, short>(shortValues);
            }

            if (values.Length == 5)
            {
                return new Any5SearchValues<char, short>(shortValues);
            }

            if (maxInclusive < 256)
            {
                // This will also match ASCII values when IndexOfAnyAsciiSearcher is not supported
                return new Latin1CharSearchValues(values);
            }

            return new ProbabilisticCharSearchValues(values);
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

        private readonly struct TrueConst : IRuntimeConst
        {
            public static bool Value => true;
        }

        private readonly struct FalseConst : IRuntimeConst
        {
            public static bool Value => false;
        }
    }
}

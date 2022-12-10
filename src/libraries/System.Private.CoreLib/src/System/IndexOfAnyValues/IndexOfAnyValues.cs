// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Buffers
{
    /// <summary>
    /// Provides a set of initialization methods for instances of the <see cref="IndexOfAnyValues{T}"/> class.
    /// </summary>
    /// <remarks>
    /// IndexOfAnyValues are optimized for situations where the same set of values is frequently used for searching at runtime.
    /// </remarks>
    public static class IndexOfAnyValues
    {
        /// <summary>
        /// Creates an optimized representation of <paramref name="values"/> used for efficient searching.
        /// </summary>
        /// <param name="values">The set of values.</param>
        public static IndexOfAnyValues<byte> Create(ReadOnlySpan<byte> values)
        {
            if (values.IsEmpty)
            {
                return new IndexOfEmptyValues<byte>();
            }

            if (values.Length == 1)
            {
                return new IndexOfAny1Value<byte>(values);
            }

            // IndexOfAnyValuesInRange is slower than IndexOfAny1Value, but faster than IndexOfAny2Values
            if (TryGetSingleRange(values, out byte maxInclusive) is IndexOfAnyValues<byte> range)
            {
                return range;
            }

            if (values.Length <= 5)
            {
                Debug.Assert(values.Length is 2 or 3 or 4 or 5);
                return values.Length switch
                {
                    2 => new IndexOfAny2Values<byte>(values),
                    3 => new IndexOfAny3Values<byte>(values),
                    4 => new IndexOfAny4Values<byte, byte>(values),
                    _ => new IndexOfAny5Values<byte, byte>(values),
                };
            }

            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && maxInclusive < 128)
            {
                IndexOfAnyAsciiSearcher.ComputeBitmap(values, out Vector128<byte> bitmap, out BitVector256 lookup);

                return Ssse3.IsSupported && lookup.Contains(0)
                    ? new IndexOfAnyAsciiByteValues<IndexOfAnyAsciiSearcher.Ssse3HandleZeroInNeedle>(bitmap, lookup)
                    : new IndexOfAnyAsciiByteValues<IndexOfAnyAsciiSearcher.Default>(bitmap, lookup);
            }

            return new IndexOfAnyByteValues(values);
        }

        /// <summary>
        /// Creates an optimized representation of <paramref name="values"/> used for efficient searching.
        /// </summary>
        /// <param name="values">The set of values.</param>
        public static IndexOfAnyValues<char> Create(ReadOnlySpan<char> values)
        {
            if (values.IsEmpty)
            {
                return new IndexOfEmptyValues<char>();
            }

            if (values.Length == 1)
            {
                return new IndexOfAny1Value<char>(values);
            }

            // IndexOfAnyValuesInRange is slower than IndexOfAny1Value, but faster than IndexOfAny2Values
            if (TryGetSingleRange(values, out char maxInclusive) is IndexOfAnyValues<char> charRange)
            {
                return charRange;
            }

            if (values.Length == 2)
            {
                return new IndexOfAny2Values<char>(values);
            }

            if (values.Length == 3)
            {
                return new IndexOfAny3Values<char>(values);
            }

            // IndexOfAnyAsciiSearcher for chars is slower than IndexOfAny3Values, but faster than IndexOfAny4Values
            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && maxInclusive < 128)
            {
                IndexOfAnyAsciiSearcher.ComputeBitmap(values, out Vector128<byte> bitmap, out BitVector256 lookup);

                return Ssse3.IsSupported && lookup.Contains(0)
                    ? new IndexOfAnyAsciiCharValues<IndexOfAnyAsciiSearcher.Ssse3HandleZeroInNeedle>(bitmap, lookup)
                    : new IndexOfAnyAsciiCharValues<IndexOfAnyAsciiSearcher.Default>(bitmap, lookup);
            }

            // Vector128<char> isn't valid. Treat the values as shorts instead.
            ReadOnlySpan<short> shortValues = MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(values)),
                values.Length);

            if (values.Length == 4)
            {
                return new IndexOfAny4Values<char, short>(shortValues);
            }

            if (values.Length == 5)
            {
                return new IndexOfAny5Values<char, short>(shortValues);
            }

            if (maxInclusive < 256)
            {
                // This will also match ASCII values when IndexOfAnyAsciiSearcher is not supported
                return new IndexOfAnyLatin1CharValues(values);
            }

            return new IndexOfAnyCharValuesProbabilistic(values);
        }

        private static IndexOfAnyValues<T>? TryGetSingleRange<T>(ReadOnlySpan<T> values, out T maxInclusive)
            where T : struct, INumber<T>, IMinMaxValue<T>
        {
            T min = T.MaxValue;
            T max = T.MinValue;

            foreach (T value in values)
            {
                min = T.Min(min, value);
                max = T.Max(max, value);
            }

            maxInclusive = max;

            uint range = uint.CreateChecked(max - min) + 1;
            if (range > values.Length)
            {
                return null;
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
                return null;
            }

            return (IndexOfAnyValues<T>)(object)new IndexOfAnyValuesInRange<T>(min, max);
        }
    }
}

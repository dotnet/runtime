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
#if DEBUG
        static IndexOfAnyValues()
        {
            CheckAsciiSet<AsciiLetter>("ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
            CheckAsciiSet<AsciiLetterOrDigit>("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz");
            CheckAsciiSet<AsciiHexDigit>("0123456789ABCDEFabcdef");
            CheckAsciiSet<AsciiHexDigitLower>("0123456789abcdef");
            CheckAsciiSet<AsciiHexDigitUpper>("0123456789ABCDEF");
        }

        private static void CheckAsciiSet<TAsciiSet>(string set)
            where TAsciiSet : IAsciiSet
        {
            byte[] byteSet = System.Text.Encoding.ASCII.GetBytes(set);

            IndexOfAnyAsciiSearcher.ComputeBitmap<char>(set, out Vector128<byte> bitmap, out BitVector256 lookup);
            Debug.Assert(bitmap == TAsciiSet.Bitmap);
            Debug.Assert(lookup.GetCharValues().AsSpan().SequenceEqual(set));
            Debug.Assert(lookup.GetByteValues().AsSpan().SequenceEqual(byteSet));

            IndexOfAnyAsciiSearcher.ComputeBitmap<byte>(byteSet, out bitmap, out lookup);
            Debug.Assert(bitmap == TAsciiSet.Bitmap);
            Debug.Assert(lookup.GetCharValues().AsSpan().SequenceEqual(set));
            Debug.Assert(lookup.GetByteValues().AsSpan().SequenceEqual(byteSet));

            for (int i = 0; i < 256; i++)
            {
                Debug.Assert(TAsciiSet.Contains((char)i) == lookup.Contains256((char)i), $"{i}");
            }

            for (int i = 256; i <= char.MaxValue; i++)
            {
                Debug.Assert(!TAsciiSet.Contains((char)i), $"{i}");
            }
        }
#endif

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
                return new IndexOfAny1Value<byte, byte>(values);
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
                    2 => new IndexOfAny2Values<byte, byte>(values),
                    3 => new IndexOfAny3Values<byte, byte>(values),
                    4 => new IndexOfAny4Values<byte, byte>(values),
                    _ => new IndexOfAny5Values<byte, byte>(values),
                };
            }

            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && maxInclusive < 128)
            {
                IndexOfAnyAsciiSearcher.ComputeBitmap(values, out Vector128<byte> bitmap, out BitVector256 lookup);

                if (bitmap == AsciiLetter.Bitmap) return IndexOfAnyByteInAsciiSet<AsciiLetter>.Instance;
                if (bitmap == AsciiLetterOrDigit.Bitmap) return IndexOfAnyByteInAsciiSet<AsciiLetterOrDigit>.Instance;
                if (bitmap == AsciiHexDigit.Bitmap) return IndexOfAnyByteInAsciiSet<AsciiHexDigit>.Instance;
                if (bitmap == AsciiHexDigitLower.Bitmap) return IndexOfAnyByteInAsciiSet<AsciiHexDigitLower>.Instance;
                if (bitmap == AsciiHexDigitUpper.Bitmap) return IndexOfAnyByteInAsciiSet<AsciiHexDigitUpper>.Instance;

                return Sse3.IsSupported && lookup.Contains(0)
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
            // Vector128<char> isn't valid. Treat the values as shorts instead.
            ReadOnlySpan<short> shortValues = MemoryMarshal.CreateReadOnlySpan(
                ref Unsafe.As<char, short>(ref MemoryMarshal.GetReference(values)),
                values.Length);

            if (values.IsEmpty)
            {
                return new IndexOfEmptyValues<char>();
            }

            if (values.Length == 1)
            {
                return new IndexOfAny1Value<char, short>(shortValues);
            }

            // IndexOfAnyValuesInRange is slower than IndexOfAny1Value, but faster than IndexOfAny2Values
            if (TryGetSingleRange(values, out char maxInclusive) is IndexOfAnyValues<char> charRange)
            {
                return charRange;
            }

            if (values.Length == 2)
            {
                return new IndexOfAny2Values<char, short>(shortValues);
            }

            if (values.Length == 3)
            {
                return new IndexOfAny3Values<char, short>(shortValues);
            }

            // IndexOfAnyAsciiSearcher for chars is slower than IndexOfAny3Values, but faster than IndexOfAny4Values
            if (IndexOfAnyAsciiSearcher.IsVectorizationSupported && maxInclusive < 128)
            {
                IndexOfAnyAsciiSearcher.ComputeBitmap(values, out Vector128<byte> bitmap, out BitVector256 lookup);

                if (bitmap == AsciiLetter.Bitmap) return IndexOfAnyCharInAsciiSet<AsciiLetter>.Instance;
                if (bitmap == AsciiLetterOrDigit.Bitmap) return IndexOfAnyCharInAsciiSet<AsciiLetterOrDigit>.Instance;
                if (bitmap == AsciiHexDigit.Bitmap) return IndexOfAnyCharInAsciiSet<AsciiHexDigit>.Instance;
                if (bitmap == AsciiHexDigitLower.Bitmap) return IndexOfAnyCharInAsciiSet<AsciiHexDigitLower>.Instance;
                if (bitmap == AsciiHexDigitUpper.Bitmap) return IndexOfAnyCharInAsciiSet<AsciiHexDigitUpper>.Instance;

                return Sse3.IsSupported && lookup.Contains(0)
                    ? new IndexOfAnyAsciiCharValues<IndexOfAnyAsciiSearcher.Ssse3HandleZeroInNeedle>(bitmap, lookup)
                    : new IndexOfAnyAsciiCharValues<IndexOfAnyAsciiSearcher.Default>(bitmap, lookup);
            }

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

            return values.Length < Vector128<short>.Count
                ? new IndexOfAnyCharValuesProbabilistic<ShortLoopContains>(values)
                : new IndexOfAnyCharValuesProbabilistic<StringContains>(values);
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

        internal interface IAsciiSet
        {
            public static abstract Vector128<byte> Bitmap { get; }
            public static abstract bool Contains(char c);
        }

        private readonly struct AsciiLetter : IAsciiSet
        {
            // IndexOfAnyAsciiSearcher.ComputeBitmap for "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
            public static Vector128<byte> Bitmap => Vector128.Create(160, 240, 240, 240, 240, 240, 240, 240, 240, 240, 240, 80, 80, 80, 80, 80);
            public static bool Contains(char c) => char.IsAsciiLetter(c);
        }

        private readonly struct AsciiLetterOrDigit : IAsciiSet
        {
            // IndexOfAnyAsciiSearcher.ComputeBitmap for "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz"
            public static Vector128<byte> Bitmap => Vector128.Create(168, 248, 248, 248, 248, 248, 248, 248, 248, 248, 240, 80, 80, 80, 80, 80);
            public static bool Contains(char c) => char.IsAsciiLetterOrDigit(c);
        }

        private readonly struct AsciiHexDigit : IAsciiSet
        {
            // IndexOfAnyAsciiSearcher.ComputeBitmap for "0123456789ABCDEFabcdef"
            public static Vector128<byte> Bitmap => Vector128.Create(8, 88, 88, 88, 88, 88, 88, 8, 8, 8, 0, 0, 0, 0, 0, 0).AsByte();
            public static bool Contains(char c) => char.IsAsciiHexDigit(c);
        }

        private readonly struct AsciiHexDigitLower : IAsciiSet
        {
            // IndexOfAnyAsciiSearcher.ComputeBitmap for "0123456789abcdef"
            public static Vector128<byte> Bitmap => Vector128.Create(8, 72, 72, 72, 72, 72, 72, 8, 8, 8, 0, 0, 0, 0, 0, 0).AsByte();
            public static bool Contains(char c) => char.IsAsciiHexDigitLower(c);
        }

        private readonly struct AsciiHexDigitUpper : IAsciiSet
        {
            // IndexOfAnyAsciiSearcher.ComputeBitmap for "0123456789ABCDEF"
            public static Vector128<byte> Bitmap => Vector128.Create(8, 24, 24, 24, 24, 24, 24, 8, 8, 8, 0, 0, 0, 0, 0, 0).AsByte();
            public static bool Contains(char c) => char.IsAsciiHexDigitUpper(c);
        }

        internal interface IStringContains
        {
            public static abstract bool Contains(string s, char value);
        }

        private readonly struct StringContains : IStringContains
        {
            public static bool Contains(string s, char value) => s.Contains(value);
        }

        private readonly struct ShortLoopContains : IStringContains
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static bool Contains(string s, char value)
            {
                foreach (char c in s)
                {
                    if (value == c)
                    {
                        return true;
                    }
                }

                return false;
            }
        }
    }
}

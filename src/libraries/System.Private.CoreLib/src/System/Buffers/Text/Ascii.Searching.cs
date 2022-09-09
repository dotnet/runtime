// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

#pragma warning disable SA1121 // Use built-in type alias
// used to express: check value for non-ASCII bytes/chars
using CheckValue = System.SByte;

namespace System.Buffers.Text
{
    public static partial class Ascii
    {
        private const int StackallocBytesLimit = 512;

        public static int IndexOf(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
            => IndexOf<byte, char, NarrowConverter>(text, value);

        public static int LastIndexOf(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
            => LastIndexOf<byte, char, NarrowConverter>(text, value);

        public static int IndexOf(ReadOnlySpan<char> text, ReadOnlySpan<byte> value)
            => IndexOf<char, byte, WidenConverter>(text, value);

        public static int LastIndexOf(ReadOnlySpan<char> text, ReadOnlySpan<byte> value)
            => LastIndexOf<char, byte, WidenConverter>(text, value);

        public static int IndexOfIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<byte> value)
            => IndexOfIgnoreCase<byte, byte>(text, value);

        public static int IndexOfIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<char> value)
            => IndexOfIgnoreCase<char, char>(text, value);

        public static int IndexOfIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
            => IndexOfIgnoreCase<byte, char>(text, value);

        public static int IndexOfIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<byte> value)
            => IndexOfIgnoreCase<char, byte>(text, value);

        public static int LastIndexOfIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<byte> value)
            => LastIndexOfIgnoreCase<byte, byte>(text, value);

        public static int LastIndexOfIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<char> value)
            => LastIndexOfIgnoreCase<char, char>(text, value);

        public static int LastIndexOfIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
            => LastIndexOfIgnoreCase<byte, char>(text, value);

        public static int LastIndexOfIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<byte> value)
            => LastIndexOfIgnoreCase<char, byte>(text, value);

        private static int IndexOf<TText, TValue, TConverter>(ReadOnlySpan<TText> text, ReadOnlySpan<TValue> value)
            where TText : unmanaged, IEquatable<TText>?
            where TValue : unmanaged, IEquatable<TValue>?
            where TConverter : struct, IConverter<TValue, TText>
        {
            if (value.IsEmpty)
            {
                return 0;
            }
            else if (value.Length > text.Length)
            {
                return -1;
            }

            TText[]? rented = null;
            Span<TText> converted = value.Length <= (StackallocBytesLimit / Unsafe.SizeOf<TText>())
                ? stackalloc TText[StackallocBytesLimit / Unsafe.SizeOf<TText>()]
                : (rented = ArrayPool<TText>.Shared.Rent(value.Length));

            try
            {
                TConverter.Convert(value, converted);

                return MemoryExtensions.IndexOf(text, converted.Slice(0, value.Length));
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<TText>.Shared.Return(rented);
                }
            }
        }

        private static int LastIndexOf<TText, TValue, TConverter>(ReadOnlySpan<TText> text, ReadOnlySpan<TValue> value)
            where TText : unmanaged, IEquatable<TText>?
            where TValue : unmanaged, IEquatable<TValue>?
            where TConverter : struct, IConverter<TValue, TText>
        {
            if (value.IsEmpty)
            {
                return text.Length;
            }
            else if (value.Length > text.Length)
            {
                return -1;
            }

            TText[]? rented = null;
            Span<TText> converted = value.Length <= (StackallocBytesLimit / Unsafe.SizeOf<TText>())
                ? stackalloc TText[StackallocBytesLimit / Unsafe.SizeOf<TText>()]
                : (rented = ArrayPool<TText>.Shared.Rent(value.Length));

            try
            {
                TConverter.Convert(value, converted);

                return MemoryExtensions.LastIndexOf(text, converted.Slice(0, value.Length));
            }
            finally
            {
                if (rented is not null)
                {
                    ArrayPool<TText>.Shared.Return(rented);
                }
            }
        }

        private static int IndexOfIgnoreCase<TText, TValue>(ReadOnlySpan<TText> text, ReadOnlySpan<TValue> value)
            where TText : unmanaged, IEquatable<TText>?, INumberBase<TText>
            where TValue : unmanaged, IEquatable<TValue>?, INumberBase<TValue>
        {
            if (value.IsEmpty)
            {
                return 0;
            }
            else if (value.Length > text.Length)
            {
                return -1;
            }

            TValue firstValue = value[0];
            if (!UnicodeUtility.IsAsciiCodePoint(uint.CreateTruncating(firstValue)))
            {
                ThrowNonAsciiFound();
            }
            TText valueHead = Unsafe.As<TValue, TText>(ref firstValue);
            char differentCase = GetDifferentCaseOrSame(Unsafe.As<TValue, char>(ref firstValue));
            TText valueHeadDifferentCase = Unsafe.As<char, TText>(ref differentCase);

            int valueTailLength = value.Length - 1;
            if (valueTailLength == 0)
            {
                return MemoryExtensions.IndexOfAny(text, valueHead, valueHeadDifferentCase); // for single-byte values use plain IndexOf
            }

            int searchSpaceMinusValueTailLength = text.Length - valueTailLength;
            int offset = 0;
            int remainingSearchSpaceLength = searchSpaceMinusValueTailLength;

            while (remainingSearchSpaceLength > 0)
            {
                // Do a quick search for the first element of "value".
                int relativeIndex = MemoryExtensions.IndexOfAny(text.Slice(offset), valueHead, valueHeadDifferentCase);
                if (relativeIndex < 0)
                    break;

                remainingSearchSpaceLength -= relativeIndex;
                offset += relativeIndex;

                if (remainingSearchSpaceLength <= 0)
                    break;  // The unsearched portion is now shorter than the sequence we're looking for. So it can't be there.

                // Found the first element of "value". See if the tail matches.
                if (Map(SequenceEqualIgnoreCase<TText, TValue, CheckValue>(text.Slice(offset + 1, value.Length - 1), value.Slice(1)))) // Map throws if non-ASCII char is found in value
                    return offset;  // The tail matched. Return a successful find.

                remainingSearchSpaceLength--;
                offset++;
            }

            return -1;
        }

        private static int LastIndexOfIgnoreCase<TText, TValue>(ReadOnlySpan<TText> text, ReadOnlySpan<TValue> value)
            where TText : unmanaged, IEquatable<TText>?, INumberBase<TText>
            where TValue : unmanaged, IEquatable<TValue>?, INumberBase<TValue>
        {
            if (value.IsEmpty)
            {
                return text.Length;
            }
            else if (value.Length > text.Length)
            {
                return -1;
            }

            TValue firstValue = value[0];
            if (!UnicodeUtility.IsAsciiCodePoint(uint.CreateTruncating(firstValue)))
            {
                ThrowNonAsciiFound();
            }
            TText valueHead = Unsafe.As<TValue, TText>(ref firstValue);
            char differentCase = GetDifferentCaseOrSame(Unsafe.As<TValue, char>(ref firstValue));
            TText valueHeadDifferentCase = Unsafe.As<char, TText>(ref differentCase);

            int valueTailLength = value.Length - 1;
            if (valueTailLength == 0)
            {
                return MemoryExtensions.LastIndexOfAny(text, valueHead, valueHeadDifferentCase); // for single-byte values use plain IndexOf
            }

            int offset = 0;

            while (true)
            {
                int remainingSearchSpaceLength = text.Length - offset - valueTailLength;
                if (remainingSearchSpaceLength <= 0)
                    break;  // The unsearched portion is now shorter than the sequence we're looking for. So it can't be there.

                // Do a quick search for the first element of "value".
                int relativeIndex = MemoryExtensions.LastIndexOfAny(text.Slice(0, remainingSearchSpaceLength), valueHead, valueHeadDifferentCase);
                if (relativeIndex < 0)
                    break;

                // Found the first element of "value". See if the tail matches.
                if (Map(SequenceEqualIgnoreCase<TText, TValue, CheckValue>(text.Slice(relativeIndex + 1, value.Length - 1), value.Slice(1))))
                    return relativeIndex;  // The tail matched. Return a successful find.

                offset += remainingSearchSpaceLength - relativeIndex;
            }

            return -1;
        }

        private static char GetDifferentCaseOrSame(char c)
            => char.IsAsciiLetterLower(c) ? (char)(c + 'A' - 'a') : char.IsAsciiLetterUpper(c) ? (char)(c - 'A' + 'a') : c;

        private interface IConverter<TFrom, TTo>
            where TFrom : unmanaged
            where TTo : unmanaged
        {
            static abstract void Convert(ReadOnlySpan<TFrom> source, Span<TTo> destination);
        }

        private readonly struct NarrowConverter : IConverter<char, byte>
        {
            public static unsafe void Convert(ReadOnlySpan<char> source, Span<byte> destination)
            {
                nuint asciiCharCount = 0;

                fixed (char* pValue = &MemoryMarshal.GetReference(source))
                fixed (byte* pNarrowed = &MemoryMarshal.GetReference(destination))
                {
                    asciiCharCount = NarrowUtf16ToAscii(pValue, pNarrowed, (nuint)source.Length);
                }

                if (asciiCharCount != (nuint)source.Length)
                {
                    ThrowNonAsciiFound();
                }
            }
        }

        private readonly struct WidenConverter : IConverter<byte, char>
        {
            public static unsafe void Convert(ReadOnlySpan<byte> source, Span<char> destination)
            {
                nuint asciiCharCount = 0;

                fixed (byte* pValue = &MemoryMarshal.GetReference(source))
                fixed (char* pWidened = &MemoryMarshal.GetReference(destination))
                {
                    asciiCharCount = WidenAsciiToUtf16(pValue, pWidened, (nuint)source.Length);
                }

                if (asciiCharCount != (nuint)source.Length)
                {
                    ThrowNonAsciiFound();
                }
            }
        }
    }
}

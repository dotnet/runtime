// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Buffers.Text
{
    public static partial class Ascii
    {
        private const int StackallocBytesLimit = 512;

        public static int IndexOf(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
            => IndexOf<byte, char, NarrowUtf16ToAscii>(text, value);

        public static int LastIndexOf(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
            => LastIndexOf<byte, char, NarrowUtf16ToAscii>(text, value);

        public static int IndexOf(ReadOnlySpan<char> text, ReadOnlySpan<byte> value)
            => IndexOf<char, byte, WidenAsciiToUtf16>(text, value);

        public static int LastIndexOf(ReadOnlySpan<char> text, ReadOnlySpan<byte> value)
            => LastIndexOf<char, byte, WidenAsciiToUtf16>(text, value);

        public static int IndexOfIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<byte> value)
            => IndexOfIgnoreCase<byte, byte, ByteByteComparer>(text, value);

        public static int IndexOfIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<char> value)
            => IndexOfIgnoreCase<char, char, CharCharComparer>(text, value);

        public static int IndexOfIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
            => IndexOfIgnoreCase<byte, char, ByteCharComparer>(text, value);

        public static int IndexOfIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<byte> value)
            => IndexOfIgnoreCase<char, byte, CharByteComparer>(text, value);

        public static int LastIndexOfIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<byte> value)
            => LastIndexOfIgnoreCase<byte, byte, ByteByteComparer>(text, value);

        public static int LastIndexOfIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<char> value)
            => LastIndexOfIgnoreCase<char, char, CharCharComparer>(text, value);

        public static int LastIndexOfIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<char> value)
            => LastIndexOfIgnoreCase<byte, char, ByteCharComparer>(text, value);

        public static int LastIndexOfIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<byte> value)
            => LastIndexOfIgnoreCase<char, byte, CharByteComparer>(text, value);

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

        private static int IndexOfIgnoreCase<TText, TValue, TComparer>(ReadOnlySpan<TText> text, ReadOnlySpan<TValue> value)
            where TText : unmanaged, IEquatable<TText>?
            where TValue : unmanaged, IEquatable<TValue>?
            where TComparer : struct, IComparer<TText, TValue>
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
            if (!TComparer.IsAscii(firstValue))
            {
                ThrowNonAsciiFound();
            }
            TText valueHead = Unsafe.As<TValue, TText>(ref firstValue);
            TText valueHeadDifferentCase = TComparer.GetDifferentCaseOrSame(firstValue);

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
                if (Map(TComparer.EqualsIgnoreCase(text.Slice(offset + 1, value.Length - 1), value.Slice(1)))) // Map throws if non-ASCII char is found in value
                    return offset;  // The tail matched. Return a successful find.

                remainingSearchSpaceLength--;
                offset++;
            }

            return -1;
        }

        private static int LastIndexOfIgnoreCase<TText, TValue, TComparer>(ReadOnlySpan<TText> text, ReadOnlySpan<TValue> value)
            where TText : unmanaged, IEquatable<TText>?
            where TValue : unmanaged, IEquatable<TValue>?
            where TComparer : struct, IComparer<TText, TValue>
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
            if (!TComparer.IsAscii(firstValue))
            {
                ThrowNonAsciiFound();
            }
            TText valueHead = Unsafe.As<TValue, TText>(ref firstValue);
            TText valueHeadDifferentCase = TComparer.GetDifferentCaseOrSame(firstValue);

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
                if (Map(TComparer.EqualsIgnoreCase(text.Slice(relativeIndex + 1, value.Length - 1), value.Slice(1))))
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

        private readonly struct NarrowUtf16ToAscii : IConverter<char, byte>
        {
            public static unsafe void Convert(ReadOnlySpan<char> source, Span<byte> destination)
            {
                nuint asciiCharCount = 0;

                fixed (char* pValue = &MemoryMarshal.GetReference(source))
                fixed (byte* pNarrowed = &MemoryMarshal.GetReference(destination))
                {
                    asciiCharCount = ASCIIUtility.NarrowUtf16ToAscii(pValue, pNarrowed, (nuint)source.Length);
                }

                if (asciiCharCount != (nuint)source.Length)
                {
                    ThrowNonAsciiFound();
                }
            }
        }

        private readonly struct WidenAsciiToUtf16 : IConverter<byte, char>
        {
            public static unsafe void Convert(ReadOnlySpan<byte> source, Span<char> destination)
            {
                nuint asciiCharCount = 0;

                fixed (byte* pValue = &MemoryMarshal.GetReference(source))
                fixed (char* pWidened = &MemoryMarshal.GetReference(destination))
                {
                    asciiCharCount = ASCIIUtility.WidenAsciiToUtf16(pValue, pWidened, (nuint)source.Length);
                }

                if (asciiCharCount != (nuint)source.Length)
                {
                    ThrowNonAsciiFound();
                }
            }
        }

        private interface IComparer<TText, TValue>
            where TText : unmanaged
            where TValue : unmanaged
        {
            static abstract bool IsAscii(TValue value);

            static abstract TText GetDifferentCaseOrSame(TValue value);

            static abstract EqualsResult EqualsIgnoreCase(ReadOnlySpan<TText> text, ReadOnlySpan<TValue> value);
        }

        private readonly struct ByteByteComparer : IComparer<byte, byte>
        {
            public static bool IsAscii(byte value) => value <= 127;

            public static byte GetDifferentCaseOrSame(byte value) => (byte)Ascii.GetDifferentCaseOrSame((char)value);

            public static EqualsResult EqualsIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<byte> value) => EqualsIgnoreCase<byte>(text, value);
        }

        private readonly struct ByteCharComparer : IComparer<byte, char>
        {
            public static bool IsAscii(char value) => value <= 127;

            public static byte GetDifferentCaseOrSame(char value) => (byte)Ascii.GetDifferentCaseOrSame(value);

            public static EqualsResult EqualsIgnoreCase(ReadOnlySpan<byte> text, ReadOnlySpan<char> value) => EqualsIgnoreCase<char>(value, text);
        }

        private readonly struct CharCharComparer : IComparer<char, char>
        {
            public static bool IsAscii(char value) => value <= 127;

            public static char GetDifferentCaseOrSame(char value) => Ascii.GetDifferentCaseOrSame(value);

            public static EqualsResult EqualsIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<char> value) => EqualsIgnoreCase<char>(text, value);
        }

        private readonly struct CharByteComparer : IComparer<char, byte>
        {
            public static bool IsAscii(byte value) => value <= 127;

            public static char GetDifferentCaseOrSame(byte value) => Ascii.GetDifferentCaseOrSame((char)value);

            public static EqualsResult EqualsIgnoreCase(ReadOnlySpan<char> text, ReadOnlySpan<byte> value) => EqualsIgnoreCase<byte>(text, value);
        }
    }
}

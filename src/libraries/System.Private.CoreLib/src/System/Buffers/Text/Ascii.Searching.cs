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
    }
}

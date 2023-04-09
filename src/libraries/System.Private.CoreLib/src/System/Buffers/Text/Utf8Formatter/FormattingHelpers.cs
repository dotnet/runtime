// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Buffers.Text
{
    // All the helper methods in this class assume that the by-ref is valid and that there is
    // enough space to fit the items that will be written into the underlying memory. The calling
    // code must have already done all the necessary validation.
    internal static partial class FormattingHelpers
    {
        /// <summary>
        /// Returns the symbol contained within the standard format. If the standard format
        /// has not been initialized, returns the provided fallback symbol.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char GetSymbolOrDefault(in StandardFormat format, char defaultSymbol)
        {
            // This is equivalent to the line below, but it is written in such a way
            // that the JIT is able to perform more optimizations.
            //
            // return (format.IsDefault) ? defaultSymbol : format.Symbol;

            char symbol = format.Symbol;
            if (symbol == default && format.Precision == default)
            {
                symbol = defaultSymbol;
            }
            return symbol;
        }

        /// <summary>
        /// Fills a buffer with the ASCII character '0' (0x30).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void FillWithAsciiZeros(Span<byte> buffer)
        {
            // This is a faster implementation of Span<T>.Fill() for very short buffers.
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] = (byte)'0';
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDigits<TChar>(ulong value, Span<TChar> buffer) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            // We can mutate the 'value' parameter since it's a copy-by-value local.
            // It'll be used to represent the value left over after each division by 10.

            for (int i = buffer.Length - 1; i >= 1; i--)
            {
                ulong temp = '0' + value;
                value /= 10;
                buffer[i] = TChar.CreateTruncating(temp - (value * 10));
            }

            Debug.Assert(value < 10);
            buffer[0] = TChar.CreateTruncating('0' + value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDigitsWithGroupSeparator(ulong value, Span<byte> buffer)
        {
            // We can mutate the 'value' parameter since it's a copy-by-value local.
            // It'll be used to represent the value left over after each division by 10.

            int digitsWritten = 0;
            for (int i = buffer.Length - 1; i >= 1; i--)
            {
                ulong temp = '0' + value;
                value /= 10;
                buffer[i] = (byte)(temp - (value * 10));
                if (digitsWritten == Utf8Constants.GroupSize - 1)
                {
                    buffer[--i] = Utf8Constants.Comma;
                    digitsWritten = 0;
                }
                else
                {
                    digitsWritten++;
                }
            }

            Debug.Assert(value < 10);
            buffer[0] = (byte)('0' + value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteDigits<TChar>(uint value, Span<TChar> buffer) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(buffer.Length > 0);

            for (int i = buffer.Length - 1; i >= 1; i--)
            {
                uint temp = '0' + value;
                value /= 10;
                buffer[i] = TChar.CreateTruncating(temp - (value * 10));
            }

            Debug.Assert(value < 10);
            buffer[0] = TChar.CreateTruncating('0' + value);
        }

        /// <summary>
        /// Writes a value [ 00 .. 99 ] to the buffer starting at the specified offset.
        /// This method performs best when the starting index is a constant literal.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteTwoDigits<TChar>(uint value, Span<TChar> buffer, int startingIndex = 0) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));
            Debug.Assert(value <= 99);
            Debug.Assert(startingIndex <= buffer.Length - 2);

            fixed (TChar* bufferPtr = &MemoryMarshal.GetReference(buffer))
            {
                Number.WriteTwoDigits(bufferPtr + startingIndex, value);
            }
        }

        /// <summary>
        /// Writes a value [ 0000 .. 9999 ] to the buffer starting at the specified offset.
        /// This method performs best when the starting index is a constant literal.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void WriteFourDigits<TChar>(uint value, Span<TChar> buffer, int startingIndex = 0) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            Debug.Assert(typeof(TChar) == typeof(char) || typeof(TChar) == typeof(byte));
            Debug.Assert(value <= 9999);
            Debug.Assert(startingIndex <= buffer.Length - 4);

            (value, uint remainder) = Math.DivRem(value, 100);
            fixed (TChar* bufferPtr = &MemoryMarshal.GetReference(buffer))
            {
                Number.WriteTwoDigits(bufferPtr + startingIndex, value);
                Number.WriteTwoDigits(bufferPtr + startingIndex + 2, remainder);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyFour<TChar>(ReadOnlySpan<TChar> source, Span<TChar> destination) where TChar : unmanaged, IBinaryInteger<TChar>
        {
            if (typeof(TChar) == typeof(byte))
            {
                Unsafe.WriteUnaligned(ref Unsafe.As<TChar, byte>(ref MemoryMarshal.GetReference(destination)),
                    Unsafe.ReadUnaligned<uint>(ref Unsafe.As<TChar, byte>(ref MemoryMarshal.GetReference(source))));
            }
            else
            {
                Debug.Assert(typeof(TChar) == typeof(char));
                Unsafe.WriteUnaligned(ref Unsafe.As<TChar, byte>(ref MemoryMarshal.GetReference(destination)),
                    Unsafe.ReadUnaligned<ulong>(ref Unsafe.As<TChar, byte>(ref MemoryMarshal.GetReference(source))));
            }
        }

        /// <summary>Enable use of ThrowHelper from TryFormat() routines without introducing dozens of non-code-coveraged "bytesWritten = 0; return false" boilerplate.</summary>
        public static bool TryFormatThrowFormatException(out int bytesWritten)
        {
            bytesWritten = 0;
            ThrowHelper.ThrowFormatException_BadFormatSpecifier();
            return false;
        }
    }
}

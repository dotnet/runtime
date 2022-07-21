// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Buffers.Text
{
    public static partial class Ascii
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpper(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
            => ChangeCase<byte, byte, ToUpperConversion>(source, destination, out bytesConsumed, out bytesWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpper(ReadOnlySpan<char> source, Span<char> destination, out int charsConsumed, out int charsWritten)
            => ChangeCase<char, char, ToUpperConversion>(source, destination, out charsConsumed, out charsWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpper(ReadOnlySpan<byte> source, Span<char> destination, out int bytesConsumed, out int charsWritten)
            => ChangeCase<byte, char, ToUpperConversion>(source, destination, out bytesConsumed, out charsWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToUpper(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten)
            => ChangeCase<char, byte, ToUpperConversion>(source, destination, out charsConsumed, out bytesWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten)
            => ChangeCase<byte, byte, ToLowerConversion>(source, destination, out bytesConsumed, out bytesWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<char> source, Span<char> destination, out int charsConsumed, out int charsWritten)
            => ChangeCase<char, char, ToLowerConversion>(source, destination, out charsConsumed, out charsWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<byte> source, Span<char> destination, out int bytesConsumed, out int charsWritten)
            => ChangeCase<byte, char, ToLowerConversion>(source, destination, out bytesConsumed, out charsWritten);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static OperationStatus ToLower(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten)
            => ChangeCase<char, byte, ToLowerConversion>(source, destination, out charsConsumed, out bytesWritten);

        private static unsafe OperationStatus ChangeCase<TFrom, TTo, TCasing>(ReadOnlySpan<TFrom> source, Span<TTo> destination, out int sourceElementsConsumed, out int destinationElementsWritten)
            where TFrom : unmanaged, IBinaryInteger<TFrom>
            where TTo : unmanaged, IBinaryInteger<TTo>
            where TCasing : struct
        {
            if (typeof(TFrom) == typeof(TTo) && source.Overlaps(MemoryMarshal.Cast<TTo, TFrom>(destination)))
            {
                throw new InvalidOperationException(SR.InvalidOperation_SpanOverlappedOperation);
            }

            nuint numElementsToConvert;
            OperationStatus statusToReturnOnSuccess;

            if (source.Length <= destination.Length)
            {
                numElementsToConvert = (uint)source.Length;
                statusToReturnOnSuccess = OperationStatus.Done;
            }
            else
            {
                numElementsToConvert = (uint)destination.Length;
                statusToReturnOnSuccess = OperationStatus.DestinationTooSmall;
            }

            fixed (TFrom* pSource = &MemoryMarshal.GetReference(source))
            fixed (TTo* pDestination = &MemoryMarshal.GetReference(destination))
            {
                nuint numElementsActuallyConverted = ChangeCase<TFrom, TTo, TCasing>(pSource, pDestination, numElementsToConvert);
                Debug.Assert(numElementsActuallyConverted <= numElementsToConvert);

                sourceElementsConsumed = (int)numElementsActuallyConverted;
                destinationElementsWritten = (int)numElementsActuallyConverted;
                return (numElementsToConvert == numElementsActuallyConverted) ? statusToReturnOnSuccess : OperationStatus.InvalidData;
            }
        }

        private static unsafe nuint ChangeCase<TFrom, TTo, TCasing>(TFrom* pSrc, TTo* pDest, nuint elementCount)
            where TFrom : unmanaged, IBinaryInteger<TFrom>
            where TTo : unmanaged, IBinaryInteger<TTo>
            where TCasing : struct
        {
            Debug.Assert(typeof(TFrom) == typeof(byte) || typeof(TFrom) == typeof(char));
            Debug.Assert(typeof(TTo) == typeof(byte) || typeof(TTo) == typeof(char));
            Debug.Assert(typeof(TCasing) == typeof(ToUpperConversion) || typeof(TCasing) == typeof(ToLowerConversion));

            bool SourceIsAscii = (typeof(TFrom) == typeof(byte)); // JIT turns this into a const
            bool DestIsAscii = (typeof(TTo) == typeof(byte)); // JIT turns this into a const
            bool ConversionIsToUpper = (typeof(TCasing) == typeof(ToUpperConversion)); // JIT turns this into a const

            nuint i = 0;
            for (; i < elementCount; i++)
            {
                uint element = uint.CreateTruncating(pSrc[i]);
                if (!UnicodeUtility.IsAsciiCodePoint(element)) { break; }
                if (ConversionIsToUpper)
                {
                    if (UnicodeUtility.IsInRangeInclusive(element, 'a', 'z'))
                    {
                        element -= 0x20u; // lowercase to uppercase
                    }
                }
                else
                {
                    if (UnicodeUtility.IsInRangeInclusive(element, 'A', 'Z'))
                    {
                        element += 0x20u; // uppercase to lowercase
                    }
                }
                pDest[i] = TTo.CreateTruncating(element);
            }

            return i;
        }

        private struct ToUpperConversion { }
        private struct ToLowerConversion { }
    }
}

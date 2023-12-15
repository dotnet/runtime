// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Text
{
    public static partial class Ascii
    {
        /// <summary>
        /// Copies text from a source buffer to a destination buffer, converting
        /// from ASCII to UTF-16 during the copy.
        /// </summary>
        /// <param name="source">The source buffer from which ASCII text is read.</param>
        /// <param name="destination">The destination buffer to which UTF-16 text is written.</param>
        /// <param name="charsWritten">The number of chars actually written to <paramref name="destination"/>. It's the same as the number of bytes actually read from <paramref name="source"/></param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        public static unsafe OperationStatus ToUtf16(ReadOnlySpan<byte> source, Span<char> destination, out int charsWritten)
        {
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

            fixed (byte* pSource = &MemoryMarshal.GetReference(source))
            fixed (char* pDestination = &MemoryMarshal.GetReference(destination))
            {
                nuint numElementsActuallyConverted = WidenAsciiToUtf16(pSource, pDestination, numElementsToConvert);
                Debug.Assert(numElementsActuallyConverted <= numElementsToConvert);

                charsWritten = (int)numElementsActuallyConverted;
                return (numElementsToConvert == numElementsActuallyConverted) ? statusToReturnOnSuccess : OperationStatus.InvalidData;
            }
        }

        /// <summary>
        /// Copies text from a source buffer to a destination buffer, converting
        /// from UTF-16 to ASCII during the copy.
        /// </summary>
        /// <param name="source">The source buffer from which UTF-16 text is read.</param>
        /// <param name="destination">The destination buffer to which ASCII text is written.</param>
        /// <param name="bytesWritten">The number of bytes actually written to <paramref name="destination"/>. It's the same as the number of chars actually read from <paramref name="source"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        public static unsafe OperationStatus FromUtf16(ReadOnlySpan<char> source, Span<byte> destination, out int bytesWritten)
        {
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

            fixed (char* pSource = &MemoryMarshal.GetReference(source))
            fixed (byte* pDestination = &MemoryMarshal.GetReference(destination))
            {
                nuint numElementsActuallyConverted = NarrowUtf16ToAscii(pSource, pDestination, numElementsToConvert);
                Debug.Assert(numElementsActuallyConverted <= numElementsToConvert);

                bytesWritten = (int)numElementsActuallyConverted;
                return (numElementsToConvert == numElementsActuallyConverted) ? statusToReturnOnSuccess : OperationStatus.InvalidData;
            }
        }
    }
}

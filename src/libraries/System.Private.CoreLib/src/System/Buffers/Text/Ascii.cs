// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Buffers.Text
{
    public static partial class Ascii
    {
        /// <summary>
        /// Returns the index of the first non-ASCII byte in a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to scan.</param>
        /// <returns>The index in <paramref name="buffer"/> where the first non-ASCII
        /// byte appears, or -1 if the buffer contains only ASCII bytes.</returns>
        public static unsafe int GetIndexOfFirstNonAsciiByte(ReadOnlySpan<byte> buffer)
        {
            nuint bufferLength = (uint)buffer.Length;
            fixed (byte* pBuffer = &MemoryMarshal.GetReference(buffer))
            {
                nuint idxOfFirstNonAsciiElement = ASCIIUtility.GetIndexOfFirstNonAsciiByte(pBuffer, bufferLength);
                Debug.Assert(idxOfFirstNonAsciiElement <= bufferLength);
                return (idxOfFirstNonAsciiElement == bufferLength) ? -1 : (int)idxOfFirstNonAsciiElement;
            }
        }

        /// <summary>
        /// Returns the index of the first non-ASCII char in a buffer.
        /// </summary>
        /// <param name="buffer">The buffer to scan.</param>
        /// <returns>The index in <paramref name="buffer"/> where the first non-ASCII
        /// char appears, or -1 if the buffer contains only ASCII char.</returns>
        public static unsafe int GetIndexOfFirstNonAsciiChar(ReadOnlySpan<char> buffer)
        {
            nuint bufferLength = (uint)buffer.Length;
            fixed (char* pBuffer = &MemoryMarshal.GetReference(buffer))
            {
                nuint idxOfFirstNonAsciiElement = ASCIIUtility.GetIndexOfFirstNonAsciiChar(pBuffer, bufferLength);
                Debug.Assert(idxOfFirstNonAsciiElement <= bufferLength);
                return (idxOfFirstNonAsciiElement == bufferLength) ? -1 : (int)idxOfFirstNonAsciiElement;
            }
        }

        /// <summary>
        /// Determines whether the provided value contains only ASCII bytes.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>True if <paramref name="value"/> contains only ASCII bytes or is
        /// empty; False otherwise.</returns>
        public static unsafe bool IsAscii(ReadOnlySpan<byte> value)
        {
            nuint valueLength = (uint)value.Length;
            fixed (byte* pValue = &MemoryMarshal.GetReference(value))
            {
                return ASCIIUtility.GetIndexOfFirstNonAsciiByte(pValue, valueLength) == valueLength;
            }
        }

        /// <summary>
        /// Determines whether the provided value contains only ASCII chars.
        /// </summary>
        /// <param name="value">The value to inspect.</param>
        /// <returns>True if <paramref name="value"/> contains only ASCII chars or is
        /// empty; False otherwise.</returns>
        public static unsafe bool IsAscii(ReadOnlySpan<char> value)
        {
            nuint valueLength = (uint)value.Length;
            fixed (char* pValue = &MemoryMarshal.GetReference(value))
            {
                return ASCIIUtility.GetIndexOfFirstNonAsciiChar(pValue, valueLength) == valueLength;
            }
        }

        /// <summary>
        /// Copies text from a source buffer to a destination buffer, converting
        /// from ASCII to UTF-16 during the copy.
        /// </summary>
        /// <param name="source">The source buffer from which ASCII text is read.</param>
        /// <param name="destination">The destination buffer to which UTF-16 text is written.</param>
        /// <param name="bytesConsumed">The number of bytes actually read from <paramref name="source"/>.</param>
        /// <param name="charsWritten">The number of chars actually written to <paramref name="destination"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        public static unsafe OperationStatus ToUtf16(ReadOnlySpan<byte> source, Span<char> destination, out int bytesConsumed, out int charsWritten)
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
                nuint numElementsActuallyConverted = ASCIIUtility.WidenAsciiToUtf16(pSource, pDestination, numElementsToConvert);
                Debug.Assert(numElementsActuallyConverted <= numElementsToConvert);

                bytesConsumed = (int)numElementsActuallyConverted;
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
        /// <param name="charsConsumed">The number of chars actually read from <paramref name="source"/>.</param>
        /// <param name="bytesWritten">The number of bytes actually written to <paramref name="destination"/>.</param>
        /// <returns>An <see cref="OperationStatus"/> describing the result of the operation.</returns>
        public static unsafe OperationStatus FromUtf16(ReadOnlySpan<char> source, Span<byte> destination, out int charsConsumed, out int bytesWritten)
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
                nuint numElementsActuallyConverted = ASCIIUtility.NarrowUtf16ToAscii(pSource, pDestination, numElementsToConvert);
                Debug.Assert(numElementsActuallyConverted <= numElementsToConvert);

                charsConsumed = (int)numElementsActuallyConverted;
                bytesWritten = (int)numElementsActuallyConverted;
                return (numElementsToConvert == numElementsActuallyConverted) ? statusToReturnOnSuccess : OperationStatus.InvalidData;
            }
        }
    }
}

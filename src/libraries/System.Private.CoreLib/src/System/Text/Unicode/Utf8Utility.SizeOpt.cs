// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;

namespace System.Text.Unicode
{
    internal static unsafe partial class Utf8Utility
    {
        // On method return, pInputBufferRemaining and pOutputBufferRemaining will both point to where
        // the next byte would have been consumed from / the next char would have been written to.
        // inputLength in bytes, outputCharsRemaining in chars.
        public static OperationStatus TranscodeToUtf16(byte* pInputBuffer, int inputLength, char* pOutputBuffer, int outputCharsRemaining, out byte* pInputBufferRemaining, out char* pOutputBufferRemaining)
        {
            Debug.Assert(inputLength >= 0, "Input length must not be negative.");
            Debug.Assert(pInputBuffer != null || inputLength == 0, "Input length must be zero if input buffer pointer is null.");

            Debug.Assert(outputCharsRemaining >= 0, "Destination length must not be negative.");
            Debug.Assert(pOutputBuffer != null || outputCharsRemaining == 0, "Destination length must be zero if destination buffer pointer is null.");

            var input = new ReadOnlySpan<byte>(pInputBuffer, inputLength);
            var output = new Span<char>(pOutputBuffer, outputCharsRemaining);

            OperationStatus opStatus = OperationStatus.Done;
            while (!input.IsEmpty)
            {
                opStatus = Rune.DecodeFromUtf8(input, out Rune rune, out int bytesConsumedJustNow);
                if (opStatus != OperationStatus.Done) { break; }
                if (!rune.TryEncodeToUtf16(output, out int charsWrittenJustNow)) { opStatus = OperationStatus.DestinationTooSmall; break; }
                input = input.Slice(bytesConsumedJustNow);
                output = output.Slice(charsWrittenJustNow);
            }

            pInputBufferRemaining = pInputBuffer + inputLength - input.Length;
            pOutputBufferRemaining = pOutputBuffer + outputCharsRemaining - output.Length;

            return opStatus;
        }

        // On method return, pInputBufferRemaining and pOutputBufferRemaining will both point to where
        // the next char would have been consumed from / the next byte would have been written to.
        // inputLength in chars, outputBytesRemaining in bytes.
        public static OperationStatus TranscodeToUtf8(char* pInputBuffer, int inputLength, byte* pOutputBuffer, int outputBytesRemaining, out char* pInputBufferRemaining, out byte* pOutputBufferRemaining)
        {
            Debug.Assert(inputLength >= 0, "Input length must not be negative.");
            Debug.Assert(pInputBuffer != null || inputLength == 0, "Input length must be zero if input buffer pointer is null.");

            Debug.Assert(outputBytesRemaining >= 0, "Destination length must not be negative.");
            Debug.Assert(pOutputBuffer != null || outputBytesRemaining == 0, "Destination length must be zero if destination buffer pointer is null.");


            var input = new ReadOnlySpan<char>(pInputBuffer, inputLength);
            var output = new Span<byte>(pOutputBuffer, outputBytesRemaining);

            OperationStatus opStatus = OperationStatus.Done;
            while (!input.IsEmpty)
            {
                opStatus = Rune.DecodeFromUtf16(input, out Rune rune, out int charsConsumedJustNow);
                if (opStatus != OperationStatus.Done) { break; }
                if (!rune.TryEncodeToUtf8(output, out int bytesWrittenJustNow)) { opStatus = OperationStatus.DestinationTooSmall; break; }
                input = input.Slice(charsConsumedJustNow);
                output = output.Slice(bytesWrittenJustNow);
            }

            pInputBufferRemaining = pInputBuffer + inputLength - input.Length;
            pOutputBufferRemaining = pOutputBuffer + outputBytesRemaining - output.Length;

            return opStatus;
        }

        // Returns &inputBuffer[inputLength] if the input buffer is valid.
        /// <summary>
        /// Given an input buffer <paramref name="pInputBuffer"/> of byte length <paramref name="inputLength"/>,
        /// returns a pointer to where the first invalid data appears in <paramref name="pInputBuffer"/>.
        /// </summary>
        /// <remarks>
        /// Returns a pointer to the end of <paramref name="pInputBuffer"/> if the buffer is well-formed.
        /// </remarks>
        /// <param name="pInputBuffer">Pointer to Utf8 byte buffer</param>
        /// <param name="inputLength">Buffer length in bytes</param>
        /// <param name="utf16CodeUnitCountAdjustment">Zero or negative number to be added to the "bytes processed" return value to come up with the total UTF-16 code unit count.</param>
        /// <param name="scalarCountAdjustment">Zero or negative number to be added to the "total UTF-16 code unit count" value to come up with the total scalar count.</param>
        public static byte* GetPointerToFirstInvalidByte(byte* pInputBuffer, int inputLength, out int utf16CodeUnitCountAdjustment, out int scalarCountAdjustment)
        {
            Debug.Assert(inputLength >= 0, "Input length must not be negative.");
            Debug.Assert(pInputBuffer != null || inputLength == 0, "Input length must be zero if input buffer pointer is null.");

            var input = new ReadOnlySpan<byte>(pInputBuffer, inputLength);
            int cumulativeUtf16CodeUnitCount = 0;
            int cumulativeScalarValueCount = 0;
            while (!input.IsEmpty)
            {
                if (Rune.DecodeFromUtf8(input, out Rune rune, out int bytesConsumed) != OperationStatus.Done)
                    break;
                input = input.Slice(bytesConsumed);
                cumulativeUtf16CodeUnitCount += rune.Utf16SequenceLength;
                cumulativeScalarValueCount++;
            }

            int cumulativeBytesConsumed = inputLength - input.Length;
            utf16CodeUnitCountAdjustment = cumulativeUtf16CodeUnitCount - cumulativeBytesConsumed;
            scalarCountAdjustment = cumulativeScalarValueCount - cumulativeUtf16CodeUnitCount;
            return pInputBuffer + cumulativeBytesConsumed;
        }
    }
}

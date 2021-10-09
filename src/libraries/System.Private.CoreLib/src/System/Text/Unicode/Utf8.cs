// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

#if SYSTEM_PRIVATE_CORELIB
using Internal.Runtime.CompilerServices;
#endif

namespace System.Text.Unicode
{
#if SYSTEM_PRIVATE_CORELIB
    public
#else
    internal
#endif
        static class Utf8
    {
        /*
         * OperationStatus-based APIs for transcoding of chunked data.
         * This method is similar to Encoding.UTF8.GetBytes / GetChars but has a
         * different calling convention, different error handling mechanisms, and
         * different performance characteristics.
         *
         * If 'replaceInvalidSequences' is true, the method will replace any ill-formed
         * subsequence in the source with U+FFFD when transcoding to the destination,
         * then it will continue processing the remainder of the buffers. Otherwise
         * the method will return OperationStatus.InvalidData.
         *
         * If the method does return an error code, the out parameters will represent
         * how much of the data was successfully transcoded, and the location of the
         * ill-formed subsequence can be deduced from these values.
         *
         * If 'replaceInvalidSequences' is true, the method is guaranteed never to return
         * OperationStatus.InvalidData. If 'isFinalBlock' is true, the method is
         * guaranteed never to return OperationStatus.NeedMoreData.
         */

        /// <summary>
        /// Transcodes the UTF-16 <paramref name="source"/> buffer to <paramref name="destination"/> as UTF-8.
        /// </summary>
        /// <remarks>
        /// If <paramref name="replaceInvalidSequences"/> is <see langword="true"/>, invalid UTF-16 sequences
        /// in <paramref name="source"/> will be replaced with U+FFFD in <paramref name="destination"/>, and
        /// this method will not return <see cref="OperationStatus.InvalidData"/>.
        /// </remarks>
        public static unsafe OperationStatus FromUtf16(ReadOnlySpan<char> source, Span<byte> destination, out int charsRead, out int bytesWritten, bool replaceInvalidSequences = true, bool isFinalBlock = true)
        {
            // Throwaway span accesses - workaround for https://github.com/dotnet/runtime/issues/12332

            _ = source.Length;
            _ = destination.Length;

            fixed (char* pOriginalSource = &MemoryMarshal.GetReference(source))
            fixed (byte* pOriginalDestination = &MemoryMarshal.GetReference(destination))
            {
                // We're going to bulk transcode as much as we can in a loop, iterating
                // every time we see bad data that requires replacement.

                OperationStatus operationStatus = OperationStatus.Done;
                char* pInputBufferRemaining = pOriginalSource;
                byte* pOutputBufferRemaining = pOriginalDestination;

                while (!source.IsEmpty)
                {
                    // We've pinned the spans at the entry point to this method.
                    // It's safe for us to use Unsafe.AsPointer on them during this loop.

                    operationStatus = Utf8Utility.TranscodeToUtf8(
                        pInputBuffer: (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source)),
                        inputLength: source.Length,
                        pOutputBuffer: (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination)),
                        outputBytesRemaining: destination.Length,
                        pInputBufferRemaining: out pInputBufferRemaining,
                        pOutputBufferRemaining: out pOutputBufferRemaining);

                    // If we finished the operation entirely or we ran out of space in the destination buffer,
                    // or if we need more input data and the caller told us that there's possibly more data
                    // coming, return immediately.

                    if (operationStatus <= OperationStatus.DestinationTooSmall
                        || (operationStatus == OperationStatus.NeedMoreData && !isFinalBlock))
                    {
                        break;
                    }

                    // We encountered invalid data, or we need more data but the caller told us we're
                    // at the end of the stream. In either case treat this as truly invalid.
                    // If the caller didn't tell us to replace invalid sequences, return immediately.

                    if (!replaceInvalidSequences)
                    {
                        operationStatus = OperationStatus.InvalidData; // status code may have been NeedMoreData - force to be error
                        break;
                    }

                    // We're going to attempt to write U+FFFD to the destination buffer.
                    // Do we even have enough space to do so?

                    destination = destination.Slice((int)(pOutputBufferRemaining - (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination))));

                    if (destination.Length <= 2)
                    {
                        operationStatus = OperationStatus.DestinationTooSmall;
                        break;
                    }

                    destination[0] = 0xEF; // U+FFFD = [ EF BF BD ] in UTF-8
                    destination[1] = 0xBF;
                    destination[2] = 0xBD;
                    destination = destination.Slice(3);

                    // Invalid UTF-16 sequences are always of length 1. Just skip the next character.

                    source = source.Slice((int)(pInputBufferRemaining - (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source))) + 1);

                    operationStatus = OperationStatus.Done; // we patched the error - if we're about to break out of the loop this is a success case
                    pInputBufferRemaining = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source));
                    pOutputBufferRemaining = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination));
                }

                // Not possible to make any further progress - report to our caller how far we got.

                charsRead = (int)(pInputBufferRemaining - pOriginalSource);
                bytesWritten = (int)(pOutputBufferRemaining - pOriginalDestination);
                return operationStatus;
            }
        }

        /// <summary>
        /// Transcodes the UTF-8 <paramref name="source"/> buffer to <paramref name="destination"/> as UTF-16.
        /// </summary>
        /// <remarks>
        /// If <paramref name="replaceInvalidSequences"/> is <see langword="true"/>, invalid UTF-8 sequences
        /// in <paramref name="source"/> will be replaced with U+FFFD in <paramref name="destination"/>, and
        /// this method will not return <see cref="OperationStatus.InvalidData"/>.
        /// </remarks>
        public static unsafe OperationStatus ToUtf16(ReadOnlySpan<byte> source, Span<char> destination, out int bytesRead, out int charsWritten, bool replaceInvalidSequences = true, bool isFinalBlock = true)
        {
            // Throwaway span accesses - workaround for https://github.com/dotnet/runtime/issues/12332

            _ = source.Length;
            _ = destination.Length;

            // We'll be mutating these values throughout our loop.

            fixed (byte* pOriginalSource = &MemoryMarshal.GetReference(source))
            fixed (char* pOriginalDestination = &MemoryMarshal.GetReference(destination))
            {
                // We're going to bulk transcode as much as we can in a loop, iterating
                // every time we see bad data that requires replacement.

                OperationStatus operationStatus = OperationStatus.Done;
                byte* pInputBufferRemaining = pOriginalSource;
                char* pOutputBufferRemaining = pOriginalDestination;

                while (!source.IsEmpty)
                {
                    // We've pinned the spans at the entry point to this method.
                    // It's safe for us to use Unsafe.AsPointer on them during this loop.

                    operationStatus = Utf8Utility.TranscodeToUtf16(
                        pInputBuffer: (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source)),
                        inputLength: source.Length,
                        pOutputBuffer: (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination)),
                        outputCharsRemaining: destination.Length,
                        pInputBufferRemaining: out pInputBufferRemaining,
                        pOutputBufferRemaining: out pOutputBufferRemaining);

                    // If we finished the operation entirely or we ran out of space in the destination buffer,
                    // or if we need more input data and the caller told us that there's possibly more data
                    // coming, return immediately.

                    if (operationStatus <= OperationStatus.DestinationTooSmall
                        || (operationStatus == OperationStatus.NeedMoreData && !isFinalBlock))
                    {
                        break;
                    }

                    // We encountered invalid data, or we need more data but the caller told us we're
                    // at the end of the stream. In either case treat this as truly invalid.
                    // If the caller didn't tell us to replace invalid sequences, return immediately.

                    if (!replaceInvalidSequences)
                    {
                        operationStatus = OperationStatus.InvalidData; // status code may have been NeedMoreData - force to be error
                        break;
                    }

                    // We're going to attempt to write U+FFFD to the destination buffer.
                    // Do we even have enough space to do so?

                    destination = destination.Slice((int)(pOutputBufferRemaining - (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination))));

                    if (destination.IsEmpty)
                    {
                        operationStatus = OperationStatus.DestinationTooSmall;
                        break;
                    }

                    destination[0] = (char)UnicodeUtility.ReplacementChar;
                    destination = destination.Slice(1);

                    // Now figure out how many bytes of the source we must skip over before we should retry
                    // the operation. This might be more than 1 byte.

                    source = source.Slice((int)(pInputBufferRemaining - (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source))));
                    Debug.Assert(!source.IsEmpty, "Expected 'Done' if source is fully consumed.");

                    Rune.DecodeFromUtf8(source, out _, out int bytesConsumedJustNow);
                    source = source.Slice(bytesConsumedJustNow);

                    operationStatus = OperationStatus.Done; // we patched the error - if we're about to break out of the loop this is a success case
                    pInputBufferRemaining = (byte*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(source));
                    pOutputBufferRemaining = (char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(destination));
                }

                // Not possible to make any further progress - report to our caller how far we got.

                bytesRead = (int)(pInputBufferRemaining - pOriginalSource);
                charsWritten = (int)(pOutputBufferRemaining - pOriginalDestination);
                return operationStatus;
            }
        }
    }
}

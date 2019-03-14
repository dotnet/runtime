// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Diagnostics;

namespace System.Text.Unicode
{
    public static class Utf8
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
        public static OperationStatus FromUtf16(ReadOnlySpan<char> source, Span<byte> destination, out int numCharsRead, out int numBytesWritten, bool replaceInvalidSequences = true, bool isFinalBlock = true)
        {
            int originalSourceLength = source.Length;
            int originalDestinationLength = destination.Length;
            OperationStatus status = OperationStatus.Done;

            // In a loop, this is going to read and transcode one scalar value at a time
            // from the source to the destination.

            while (!source.IsEmpty)
            {
                status = Rune.DecodeUtf16(source, out Rune firstScalarValue, out int charsConsumed);

                switch (status)
                {
                    case OperationStatus.NeedMoreData:

                        // Input buffer ended with a high surrogate. Only treat this as an error
                        // if the caller told us that we shouldn't expect additional data in a
                        // future call.

                        if (!isFinalBlock)
                        {
                            goto Finish;
                        }

                        status = OperationStatus.InvalidData;
                        goto case OperationStatus.InvalidData;

                    case OperationStatus.InvalidData:

                        // Input buffer contained invalid data. If the caller told us not to
                        // perform U+FFFD replacement, terminate the loop immediately and return
                        // an error to the caller.

                        if (!replaceInvalidSequences)
                        {
                            goto Finish;
                        }

                        firstScalarValue = Rune.ReplacementChar;
                        goto default;

                    default:

                        // We know which scalar value we need to transcode to UTF-8.
                        // Do so now, and only terminate the loop if we ran out of space
                        // in the destination buffer.

                        if (firstScalarValue.TryEncodeToUtf8Bytes(destination, out int bytesWritten))
                        {
                            source = source.Slice(charsConsumed); // don't use Rune.Utf8SequenceLength; we may have performed substitution
                            destination = destination.Slice(bytesWritten);
                            status = OperationStatus.Done; // forcibly set success
                            continue;
                        }
                        else
                        {
                            status = OperationStatus.DestinationTooSmall;
                            goto Finish;
                        }
                }
            }

        Finish:

            numCharsRead = originalSourceLength - source.Length;
            numBytesWritten = originalDestinationLength - destination.Length;

            Debug.Assert(numCharsRead < originalSourceLength || status != OperationStatus.Done,
                "Cannot report OperationStatus.Done if we haven't consumed the entire input buffer.");

            return status;
        }

        /// <summary>
        /// Transcodes the UTF-8 <paramref name="source"/> buffer to <paramref name="destination"/> as UTF-16.
        /// </summary>
        /// <remarks>
        /// If <paramref name="replaceInvalidSequences"/> is <see langword="true"/>, invalid UTF-8 sequences
        /// in <paramref name="source"/> will be replaced with U+FFFD in <paramref name="destination"/>, and
        /// this method will not return <see cref="OperationStatus.InvalidData"/>.
        /// </remarks>
        public static OperationStatus ToUtf16(ReadOnlySpan<byte> source, Span<char> destination, out int numBytesRead, out int numCharsWritten, bool replaceInvalidSequences = true, bool isFinalBlock = true)
        {
            int originalSourceLength = source.Length;
            int originalDestinationLength = destination.Length;
            OperationStatus status = OperationStatus.Done;

            // In a loop, this is going to read and transcode one scalar value at a time
            // from the source to the destination.

            while (!source.IsEmpty)
            {
                status = Rune.DecodeUtf8(source, out Rune firstScalarValue, out int bytesConsumed);

                switch (status)
                {
                    case OperationStatus.NeedMoreData:

                        // Input buffer ended with a partial UTF-8 sequence. Only treat this as an error
                        // if the caller told us that we shouldn't expect additional data in a
                        // future call.

                        if (!isFinalBlock)
                        {
                            goto Finish;
                        }

                        status = OperationStatus.InvalidData;
                        goto case OperationStatus.InvalidData;

                    case OperationStatus.InvalidData:

                        // Input buffer contained invalid data. If the caller told us not to
                        // perform U+FFFD replacement, terminate the loop immediately and return
                        // an error to the caller.

                        if (!replaceInvalidSequences)
                        {
                            goto Finish;
                        }

                        firstScalarValue = Rune.ReplacementChar;
                        goto default;

                    default:

                        // We know which scalar value we need to transcode to UTF-16.
                        // Do so now, and only terminate the loop if we ran out of space
                        // in the destination buffer.

                        if (firstScalarValue.TryEncode(destination, out int charsWritten))
                        {
                            source = source.Slice(bytesConsumed); // don't use Rune.Utf16SequenceLength; we may have performed substitution
                            destination = destination.Slice(charsWritten);
                            status = OperationStatus.Done; // forcibly set success
                            continue;
                        }
                        else
                        {
                            status = OperationStatus.DestinationTooSmall;
                            goto Finish;
                        }
                }
            }

        Finish:

            numBytesRead = originalSourceLength - source.Length;
            numCharsWritten = originalDestinationLength - destination.Length;

            Debug.Assert(numBytesRead < originalSourceLength || status != OperationStatus.Done,
                "Cannot report OperationStatus.Done if we haven't consumed the entire input buffer.");

            return status;
        }
    }
}

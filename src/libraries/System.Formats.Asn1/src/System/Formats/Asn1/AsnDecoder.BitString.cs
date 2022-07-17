// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;

namespace System.Formats.Asn1
{
    public static partial class AsnDecoder
    {
        /// <summary>
        ///   Attempts to get a Bit String value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules, if the value is contained in a single (primitive) encoding.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="unusedBitCount">
        ///   On success, receives the number of bits in the last byte which were reported as
        ///   "unused" by the writer.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="value">
        ///   On success, receives a slice of the input buffer that corresponds to
        ///   the value of the Bit String.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 3).
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the Bit String value has a primitive encoding and all of the bits
        ///   reported as unused are set to 0;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <seealso cref="TryReadBitString"/>
        public static bool TryReadPrimitiveBitString(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int unusedBitCount,
            out ReadOnlySpan<byte> value,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            if (TryReadPrimitiveBitStringCore(
                source,
                ruleSet,
                expectedTag ?? Asn1Tag.PrimitiveBitString,
                contentsLength: out _,
                headerLength: out _,
                out int localUbc,
                out ReadOnlySpan<byte> localValue,
                out int consumed,
                out byte normalizedLastByte))
            {
                // Check that this isn't a BER reader which encountered a situation where
                // an "unused" bit was not set to 0.
                if (localValue.Length == 0 || normalizedLastByte == localValue[localValue.Length - 1])
                {
                    unusedBitCount = localUbc;
                    value = localValue;
                    bytesConsumed = consumed;
                    return true;
                }
            }

            unusedBitCount = 0;
            value = default;
            bytesConsumed = 0;
            return false;
        }

        /// <summary>
        ///   Attempts to copy a Bit String value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules into <paramref name="destination"/>.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="unusedBitCount">
        ///   On success, receives the number of bits in the last byte which were reported as
        ///   "unused" by the writer.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, the total number of bytes written to <paramref name="destination"/>.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 3).
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> is large enough to receive the
        ///   value of the Bit String;
        ///   otherwise, <see langword="false"/>.
        /// </returns>
        /// <remarks>
        ///   The least significant bits in the last byte which are reported as "unused" by the
        ///   <paramref name="unusedBitCount"/> value will be copied into <paramref name="destination"/>
        ///   as unset bits, irrespective of their value in the encoded representation.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        ///
        ///   -or-
        ///
        ///   <paramref name="destination"/> overlaps <paramref name="source"/>.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveBitString"/>
        /// <seealso cref="ReadBitString"/>
        public static bool TryReadBitString(
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            AsnEncodingRules ruleSet,
            out int unusedBitCount,
            out int bytesConsumed,
            out int bytesWritten,
            Asn1Tag? expectedTag = null)
        {
            if (source.Overlaps(destination))
            {
                throw new ArgumentException(
                    SR.Argument_SourceOverlapsDestination,
                    nameof(destination));
            }

            int localUbc;
            byte normalizedLastByte;
            int consumed;
            int? contentsLength;
            int headerLength;

            if (TryReadPrimitiveBitStringCore(
                source,
                ruleSet,
                expectedTag ?? Asn1Tag.PrimitiveBitString,
                out contentsLength,
                out headerLength,
                out localUbc,
                out ReadOnlySpan<byte> value,
                out consumed,
                out normalizedLastByte))
            {
                if (value.Length > destination.Length)
                {
                    bytesConsumed = 0;
                    bytesWritten = 0;
                    unusedBitCount = 0;
                    return false;
                }

                CopyBitStringValue(value, normalizedLastByte, destination);

                bytesWritten = value.Length;
                bytesConsumed = consumed;
                unusedBitCount = localUbc;
                return true;
            }

            // If we get here, the tag was appropriate, but the encoding was constructed.

            if (TryCopyConstructedBitStringValue(
                Slice(source, headerLength, contentsLength),
                ruleSet,
                destination,
                contentsLength == null,
                out localUbc,
                out int bytesRead,
                out int written))
            {
                unusedBitCount = localUbc;
                bytesConsumed = headerLength + bytesRead;
                bytesWritten = written;
                return true;
            }

            bytesWritten = bytesConsumed = unusedBitCount = 0;
            return false;
        }

        /// <summary>
        ///   Reads a Bit String value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules, returning the contents in a new array.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="unusedBitCount">
        ///   On success, receives the number of bits in the last byte which were reported as
        ///   "unused" by the writer.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 3).
        /// </param>
        /// <returns>
        ///   An array containing the contents of the Bit String value.
        /// </returns>
        /// <remarks>
        ///   The least significant bits in the last byte which are reported as "unused" by the
        ///   <paramref name="unusedBitCount"/> value will be copied into the return value
        ///   as unset bits, irrespective of their value in the encoded representation.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="ruleSet"/> is not defined.
        /// </exception>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveBitString"/>
        /// <seealso cref="TryReadBitString"/>
        public static byte[] ReadBitString(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int unusedBitCount,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            if (TryReadPrimitiveBitStringCore(
                source,
                ruleSet,
                expectedTag ?? Asn1Tag.PrimitiveBitString,
                out int? contentsLength,
                out int headerLength,
                out int localUbc,
                out ReadOnlySpan<byte> localValue,
                out int consumed,
                out byte normalizedLastByte))
            {
                byte[] ret = localValue.ToArray();

                // Update the last byte in case it's a non-canonical byte in a BER encoding.
                if (localValue.Length > 0)
                {
                    ret[ret.Length - 1] = normalizedLastByte;
                }

                unusedBitCount = localUbc;
                bytesConsumed = consumed;
                return ret;
            }

            // If we get here, the tag was appropriate, but the encoding was constructed.

            // Guaranteed long enough
            int tooBig = contentsLength ?? SeekEndOfContents(source.Slice(headerLength), ruleSet);

            byte[] rented = CryptoPool.Rent(tooBig);

            if (TryCopyConstructedBitStringValue(
                Slice(source, headerLength, contentsLength),
                ruleSet,
                rented,
                contentsLength == null,
                out localUbc,
                out int bytesRead,
                out int written))
            {
                byte[] ret = rented.AsSpan(0, written).ToArray();
                CryptoPool.Return(rented, written);
                unusedBitCount = localUbc;
                bytesConsumed = headerLength + bytesRead;
                return ret;
            }

            Debug.Fail("TryCopyConstructedBitStringValue failed with a pre-allocated buffer");
            throw new AsnContentException();
        }

        private static void ParsePrimitiveBitStringContents(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int unusedBitCount,
            out ReadOnlySpan<byte> value,
            out byte normalizedLastByte)
        {
            // T-REC-X.690-201508 sec 9.2
            if (ruleSet == AsnEncodingRules.CER && source.Length > MaxCERSegmentSize)
            {
                throw new AsnContentException(SR.ContentException_InvalidUnderCer_TryBerOrDer);
            }

            // T-REC-X.690-201508 sec 8.6.2.3
            if (source.Length == 0)
            {
                throw new AsnContentException();
            }

            unusedBitCount = source[0];

            // T-REC-X.690-201508 sec 8.6.2.2
            if (unusedBitCount > 7)
            {
                throw new AsnContentException();
            }

            if (source.Length == 1)
            {
                // T-REC-X.690-201508 sec 8.6.2.4
                if (unusedBitCount > 0)
                {
                    throw new AsnContentException();
                }

                Debug.Assert(unusedBitCount == 0);
                value = ReadOnlySpan<byte>.Empty;
                normalizedLastByte = 0;
                return;
            }

            // Build a mask for the bits that are used so the normalized value can be computed
            //
            // If 3 bits are "unused" then build a mask for them to check for 0.
            // -1 << 3 => 0b1111_1111 << 3 => 0b1111_1000
            int mask = -1 << unusedBitCount;
            byte lastByte = source[source.Length - 1];
            byte maskedByte = (byte)(lastByte & mask);

            if (maskedByte != lastByte)
            {
                // T-REC-X.690-201508 sec 11.2.1
                if (ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER)
                {
                    throw new AsnContentException(SR.ContentException_InvalidUnderCerOrDer_TryBer);
                }
            }

            normalizedLastByte = maskedByte;
            value = source.Slice(1);
        }

        private delegate void BitStringCopyAction(
            ReadOnlySpan<byte> value,
            byte normalizedLastByte,
            Span<byte> destination);

        private static void CopyBitStringValue(
            ReadOnlySpan<byte> value,
            byte normalizedLastByte,
            Span<byte> destination)
        {
            if (value.Length == 0)
            {
                return;
            }

            value.CopyTo(destination);
            // Replace the last byte with the normalized answer.
            destination[value.Length - 1] = normalizedLastByte;
        }

        private static int CountConstructedBitString(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            bool isIndefinite)
        {
            Span<byte> destination = Span<byte>.Empty;

            return ProcessConstructedBitString(
                source,
                ruleSet,
                destination,
                null,
                isIndefinite,
                out _,
                out _);
        }

        private static void CopyConstructedBitString(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Span<byte> destination,
            bool isIndefinite,
            out int unusedBitCount,
            out int bytesRead,
            out int bytesWritten)
        {
            Span<byte> tmpDest = destination;

            bytesWritten = ProcessConstructedBitString(
                source,
                ruleSet,
                tmpDest,
                CopyBitStringValue,
                isIndefinite,
                out unusedBitCount,
                out bytesRead);
        }

        private static int ProcessConstructedBitString(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Span<byte> destination,
            BitStringCopyAction? copyAction,
            bool isIndefinite,
            out int lastUnusedBitCount,
            out int bytesRead)
        {
            lastUnusedBitCount = 0;
            bytesRead = 0;
            int lastSegmentLength = MaxCERSegmentSize;

            ReadOnlySpan<byte> cur = source;
            Stack<(int Offset, int Length, bool Indefinite, int BytesRead)>? readerStack = null;
            int totalLength = 0;
            Asn1Tag tag = Asn1Tag.ConstructedBitString;
            Span<byte> curDest = destination;

            while (true)
            {
                while (!cur.IsEmpty)
                {
                    tag = ReadTagAndLength(cur, ruleSet, out int? length, out int headerLength);

                    if (tag == Asn1Tag.PrimitiveBitString)
                    {
                        if (lastUnusedBitCount != 0)
                        {
                            // T-REC-X.690-201508 sec 8.6.4, only the last segment may have
                            // a number of bits not a multiple of 8.
                            throw new AsnContentException();
                        }

                        if (ruleSet == AsnEncodingRules.CER && lastSegmentLength != MaxCERSegmentSize)
                        {
                            // T-REC-X.690-201508 sec 9.2
                            throw new AsnContentException(SR.ContentException_InvalidUnderCer_TryBerOrDer);
                        }

                        Debug.Assert(length != null);
                        ReadOnlySpan<byte> encodedValue = Slice(cur, headerLength, length.Value);

                        ParsePrimitiveBitStringContents(
                            encodedValue,
                            ruleSet,
                            out lastUnusedBitCount,
                            out ReadOnlySpan<byte> contents,
                            out byte normalizedLastByte);

                        int localLen = headerLength + encodedValue.Length;
                        cur = cur.Slice(localLen);

                        bytesRead += localLen;
                        totalLength += contents.Length;
                        lastSegmentLength = encodedValue.Length;

                        if (copyAction != null)
                        {
                            copyAction(contents, normalizedLastByte, curDest);
                            curDest = curDest.Slice(contents.Length);
                        }
                    }
                    else if (tag == Asn1Tag.EndOfContents && isIndefinite)
                    {
                        ValidateEndOfContents(tag, length, headerLength);

                        bytesRead += headerLength;

                        if (readerStack?.Count > 0)
                        {
                            (int topOffset, int topLength, bool wasIndefinite, int pushedBytesRead) = readerStack.Pop();
                            ReadOnlySpan<byte> topSpan = source.Slice(topOffset, topLength);
                            cur = topSpan.Slice(bytesRead);

                            bytesRead += pushedBytesRead;
                            isIndefinite = wasIndefinite;
                        }
                        else
                        {
                            // We have matched the EndOfContents that brought us here.
                            break;
                        }
                    }
                    else if (tag == Asn1Tag.ConstructedBitString)
                    {
                        if (ruleSet == AsnEncodingRules.CER)
                        {
                            // T-REC-X.690-201508 sec 9.2
                            throw new AsnContentException(SR.ContentException_InvalidUnderCerOrDer_TryBer);
                        }

                        readerStack ??= new Stack<(int, int, bool, int)>();

                        if (!source.Overlaps(cur, out int curOffset))
                        {
                            Debug.Fail("Non-overlapping data encountered...");
                            throw new AsnContentException();
                        }

                        readerStack.Push((curOffset, cur.Length, isIndefinite, bytesRead));

                        cur = Slice(cur, headerLength, length);
                        bytesRead = headerLength;
                        isIndefinite = (length == null);
                    }
                    else
                    {
                        // T-REC-X.690-201508 sec 8.6.4.1 (in particular, Note 2)
                        throw new AsnContentException();
                    }
                }

                if (isIndefinite && tag != Asn1Tag.EndOfContents)
                {
                    throw new AsnContentException();
                }

                if (readerStack?.Count > 0)
                {
                    (int topOffset, int topLength, bool wasIndefinite, int pushedBytesRead) = readerStack.Pop();

                    ReadOnlySpan<byte> tmpSpan = source.Slice(topOffset, topLength);
                    cur = tmpSpan.Slice(bytesRead);

                    isIndefinite = wasIndefinite;
                    bytesRead += pushedBytesRead;
                }
                else
                {
                    return totalLength;
                }
            }
        }

        private static bool TryCopyConstructedBitStringValue(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Span<byte> dest,
            bool isIndefinite,
            out int unusedBitCount,
            out int bytesRead,
            out int bytesWritten)
        {
            // Call CountConstructedBitString to get the required byte and to verify that the
            // data is well-formed before copying into dest.
            int contentLength = CountConstructedBitString(source, ruleSet, isIndefinite);

            // Since the unused bits byte from the segments don't count, only one segment
            // returns 999 (or less), the second segment bumps the count to 1000, and is legal.
            //
            // T-REC-X.690-201508 sec 9.2
            if (ruleSet == AsnEncodingRules.CER && contentLength < MaxCERSegmentSize)
            {
                throw new AsnContentException(SR.ContentException_InvalidUnderCerOrDer_TryBer);
            }

            if (dest.Length < contentLength)
            {
                unusedBitCount = 0;
                bytesRead = 0;
                bytesWritten = 0;
                return false;
            }

            CopyConstructedBitString(
                source,
                ruleSet,
                dest,
                isIndefinite,
                out unusedBitCount,
                out bytesRead,
                out bytesWritten);

            Debug.Assert(bytesWritten == contentLength);
            return true;
        }

        private static bool TryReadPrimitiveBitStringCore(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Asn1Tag expectedTag,
            out int? contentsLength,
            out int headerLength,
            out int unusedBitCount,
            out ReadOnlySpan<byte> value,
            out int bytesConsumed,
            out byte normalizedLastByte)
        {
            Asn1Tag actualTag =
                ReadTagAndLength(source, ruleSet, out contentsLength, out headerLength);

            CheckExpectedTag(actualTag, expectedTag, UniversalTagNumber.BitString);

            // Ensure the length made sense.
            ReadOnlySpan<byte> encodedValue = Slice(source, headerLength, contentsLength);

            if (actualTag.IsConstructed)
            {
                if (ruleSet == AsnEncodingRules.DER)
                {
                    throw new AsnContentException(SR.ContentException_InvalidUnderDer_TryBerOrCer);
                }

                unusedBitCount = 0;
                value = default;
                normalizedLastByte = 0;
                bytesConsumed = 0;
                return false;
            }

            Debug.Assert(contentsLength.HasValue);

            ParsePrimitiveBitStringContents(
                encodedValue,
                ruleSet,
                out unusedBitCount,
                out value,
                out normalizedLastByte);

            bytesConsumed = headerLength + encodedValue.Length;
            return true;
        }
    }

    public partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as a BIT STRING with a specified tag, returning the contents
        ///   as a <see cref="ReadOnlyMemory{T}"/> over the original data.
        /// </summary>
        /// <param name="unusedBitCount">
        ///   On success, receives the number of bits in the last byte which were reported as
        ///   "unused" by the writer.
        /// </param>
        /// <param name="value">
        ///   On success, receives a <see cref="ReadOnlyMemory{T}"/> over the original data
        ///   corresponding to the value of the BIT STRING.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 1).
        /// </param>
        /// <returns>
        ///   <see langword="true"/> and advances the reader if the BIT STRING value had a primitive encoding,
        ///   <see langword="false"/> and does not advance the reader if it had a constructed encoding.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <seealso cref="TryReadBitString"/>
        public bool TryReadPrimitiveBitString(
            out int unusedBitCount,
            out ReadOnlyMemory<byte> value,
            Asn1Tag? expectedTag = null)
        {
            bool ret = AsnDecoder.TryReadPrimitiveBitString(
                _data.Span,
                RuleSet,
                out unusedBitCount,
                out ReadOnlySpan<byte> span,
                out int consumed,
                expectedTag);

            if (ret)
            {
                value = AsnDecoder.Slice(_data, span);
                _data = _data.Slice(consumed);
            }
            else
            {
                value = default;
            }

            return ret;
        }

        /// <summary>
        ///   Reads the next value as a BIT STRING with a specified tag, copying the value
        ///   into a provided destination buffer.
        /// </summary>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="unusedBitCount">
        ///   On success, receives the number of bits in the last byte which were reported as
        ///   "unused" by the writer.
        /// </param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 1).
        /// </param>
        /// <returns>
        ///   <see langword="true"/> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <see langword="false"/> and the reader does not advance.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveBitString"/>
        /// <seealso cref="ReadBitString"/>
        public bool TryReadBitString(
            Span<byte> destination,
            out int unusedBitCount,
            out int bytesWritten,
            Asn1Tag? expectedTag = null)
        {
            bool ret = AsnDecoder.TryReadBitString(
                _data.Span,
                destination,
                RuleSet,
                out unusedBitCount,
                out int consumed,
                out bytesWritten,
                expectedTag);

            if (ret)
            {
                _data = _data.Slice(consumed);
            }

            return ret;
        }

        /// <summary>
        ///   Reads the next value as a BIT STRING with a specified tag, returning the value
        ///   in a byte array.
        /// </summary>
        /// <param name="unusedBitCount">
        ///   On success, receives the number of bits in the last byte which were reported as
        ///   "unused" by the writer.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 1).
        /// </param>
        /// <returns>
        ///   A copy of the value in a newly allocated, precisely sized, array.
        /// </returns>
        /// <exception cref="AsnContentException">
        ///   the next value does not have the correct tag.
        ///
        ///   -or-
        ///
        ///   the length encoding is not valid under the current encoding rules.
        ///
        ///   -or-
        ///
        ///   the contents are not valid under the current encoding rules.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveBitString"/>
        /// <seealso cref="TryReadBitString"/>
        public byte[] ReadBitString(out int unusedBitCount, Asn1Tag? expectedTag = null)
        {
            byte[] ret = AsnDecoder.ReadBitString(
                _data.Span,
                RuleSet,
                out unusedBitCount,
                out int consumed,
                expectedTag);

            _data = _data.Slice(consumed);
            return ret;
        }
    }
}

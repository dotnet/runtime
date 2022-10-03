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
        ///   Attempts to get an Octet String value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules, copying the value into the provided destination buffer.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, the total number of bytes written to <paramref name="destination"/>.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 4).
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if <paramref name="destination"/> is large enough to receive the
        ///   value of the Octet String;
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
        ///
        ///   -or-
        ///
        ///   <paramref name="destination"/> overlaps <paramref name="source"/>.
        /// </exception>
        /// <seealso cref="TryReadPrimitiveOctetString"/>
        /// <seealso cref="ReadOctetString"/>
        public static bool TryReadOctetString(
            ReadOnlySpan<byte> source,
            Span<byte> destination,
            AsnEncodingRules ruleSet,
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

            if (TryReadPrimitiveOctetStringCore(
                source,
                ruleSet,
                expectedTag ?? Asn1Tag.PrimitiveOctetString,
                UniversalTagNumber.OctetString,
                out int? contentLength,
                out int headerLength,
                out ReadOnlySpan<byte> contents,
                out int consumed))
            {
                if (contents.Length > destination.Length)
                {
                    bytesWritten = 0;
                    bytesConsumed = 0;
                    return false;
                }

                contents.CopyTo(destination);
                bytesWritten = contents.Length;
                bytesConsumed = consumed;
                return true;
            }

            bool copied = TryCopyConstructedOctetStringContents(
                Slice(source, headerLength, contentLength),
                ruleSet,
                destination,
                contentLength == null,
                out int bytesRead,
                out bytesWritten);

            if (copied)
            {
                bytesConsumed = headerLength + bytesRead;
            }
            else
            {
                bytesConsumed = 0;
            }

            return copied;
        }

        /// <summary>
        ///   Reads an Octet String value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules, returning the contents in a new array.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 4).
        /// </param>
        /// <returns>
        ///   An array containing the contents of the Octet String value.
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
        /// <seealso cref="TryReadPrimitiveOctetString"/>
        /// <seealso cref="TryReadOctetString"/>
        public static byte[] ReadOctetString(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            byte[]? rented = null;

            ReadOnlySpan<byte> contents = GetOctetStringContents(
                source,
                ruleSet,
                expectedTag ?? Asn1Tag.PrimitiveOctetString,
                UniversalTagNumber.OctetString,
                out int consumed,
                ref rented);

            byte[] ret = contents.ToArray();

            if (rented != null)
            {
                CryptoPool.Return(rented, contents.Length);
            }

            bytesConsumed = consumed;
            return ret;
        }

        private static bool TryReadPrimitiveOctetStringCore(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            out int? contentLength,
            out int headerLength,
            out ReadOnlySpan<byte> contents,
            out int bytesConsumed)
        {
            Asn1Tag actualTag = ReadTagAndLength(source, ruleSet, out contentLength, out headerLength);
            CheckExpectedTag(actualTag, expectedTag, universalTagNumber);

            // Ensure the length makes sense.
            ReadOnlySpan<byte> encodedValue = Slice(source, headerLength, contentLength);

            if (actualTag.IsConstructed)
            {
                if (ruleSet == AsnEncodingRules.DER)
                {
                    throw new AsnContentException(SR.ContentException_InvalidUnderDer_TryBerOrCer);
                }

                contents = default;
                bytesConsumed = 0;
                return false;
            }

            Debug.Assert(contentLength.HasValue);

            if (ruleSet == AsnEncodingRules.CER && encodedValue.Length > MaxCERSegmentSize)
            {
                throw new AsnContentException(SR.ContentException_InvalidUnderCer_TryBerOrDer);
            }

            contents = encodedValue;
            bytesConsumed = headerLength + encodedValue.Length;
            return true;
        }

        /// <summary>
        ///   Attempts to get an Octet String value from <paramref name="source"/> with a specified tag under
        ///   the specified encoding rules, if the value is contained in a single (primitive) encoding.
        /// </summary>
        /// <param name="source">The buffer containing encoded data.</param>
        /// <param name="ruleSet">The encoding constraints to use when interpreting the data.</param>
        /// <param name="value">
        ///   On success, receives a slice of the input buffer that corresponds to
        ///   the value of the Octet String.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="bytesConsumed">
        ///   When this method returns, the total number of bytes for the encoded value.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 4).
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the Octet String value has a primitive encoding;
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
        /// <seealso cref="TryReadOctetString"/>
        public static bool TryReadPrimitiveOctetString(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out ReadOnlySpan<byte> value,
            out int bytesConsumed,
            Asn1Tag? expectedTag = null)
        {
            return TryReadPrimitiveOctetStringCore(
                source,
                ruleSet,
                expectedTag ?? Asn1Tag.PrimitiveOctetString,
                UniversalTagNumber.OctetString,
                contentLength: out _,
                headerLength: out _,
                out value,
                out bytesConsumed);
        }

        private static int CountConstructedOctetString(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            bool isIndefinite)
        {
            int contentLength = CopyConstructedOctetString(
                source,
                ruleSet,
                Span<byte>.Empty,
                false,
                isIndefinite,
                out _);

            // T-REC-X.690-201508 sec 9.2
            if (ruleSet == AsnEncodingRules.CER && contentLength <= MaxCERSegmentSize)
            {
                throw new AsnContentException(SR.ContentException_InvalidUnderCerOrDer_TryBer);
            }

            return contentLength;
        }

        private static void CopyConstructedOctetString(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Span<byte> destination,
            bool isIndefinite,
            out int bytesRead,
            out int bytesWritten)
        {
            bytesWritten = CopyConstructedOctetString(
                source,
                ruleSet,
                destination,
                true,
                isIndefinite,
                out bytesRead);
        }

        private static int CopyConstructedOctetString(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Span<byte> destination,
            bool write,
            bool isIndefinite,
            out int bytesRead)
        {
            bytesRead = 0;
            int lastSegmentLength = MaxCERSegmentSize;

            ReadOnlySpan<byte> cur = source;
            Stack<(int Offset, int Length, bool IsIndefinite, int BytesRead)>? readerStack = null;
            int totalLength = 0;
            Asn1Tag tag = Asn1Tag.ConstructedBitString;
            Span<byte> curDest = destination;

            while (true)
            {
                while (!cur.IsEmpty)
                {
                    tag = ReadTagAndLength(cur, ruleSet, out int? length, out int headerLength);

                    if (tag == Asn1Tag.PrimitiveOctetString)
                    {
                        if (ruleSet == AsnEncodingRules.CER && lastSegmentLength != MaxCERSegmentSize)
                        {
                            // T-REC-X.690-201508 sec 9.2
                            throw new AsnContentException(SR.ContentException_InvalidUnderCerOrDer_TryBer);
                        }

                        Debug.Assert(length != null);

                        // The call to Slice here sanity checks the data bounds, length.Value is not
                        // reliable unless this call has succeeded.
                        ReadOnlySpan<byte> contents = Slice(cur, headerLength, length.Value);

                        int localLen = headerLength + contents.Length;
                        cur = cur.Slice(localLen);

                        bytesRead += localLen;
                        totalLength += contents.Length;
                        lastSegmentLength = contents.Length;

                        if (ruleSet == AsnEncodingRules.CER && lastSegmentLength > MaxCERSegmentSize)
                        {
                            // T-REC-X.690-201508 sec 9.2
                            throw new AsnContentException(SR.ContentException_InvalidUnderCerOrDer_TryBer);
                        }

                        if (write)
                        {
                            contents.CopyTo(curDest);
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
                    else if (tag == Asn1Tag.ConstructedOctetString)
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
                    ReadOnlySpan<byte> topSpan = source.Slice(topOffset, topLength);

                    cur = topSpan.Slice(bytesRead);

                    isIndefinite = wasIndefinite;
                    bytesRead += pushedBytesRead;
                }
                else
                {
                    return totalLength;
                }
            }
        }

        private static bool TryCopyConstructedOctetStringContents(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Span<byte> dest,
            bool isIndefinite,
            out int bytesRead,
            out int bytesWritten)
        {
            bytesRead = 0;

            int contentLength = CountConstructedOctetString(source, ruleSet, isIndefinite);

            if (dest.Length < contentLength)
            {
                bytesWritten = 0;
                return false;
            }

            CopyConstructedOctetString(source, ruleSet, dest, isIndefinite, out bytesRead, out bytesWritten);

            Debug.Assert(bytesWritten == contentLength);
            return true;
        }

        private static ReadOnlySpan<byte> GetOctetStringContents(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            out int bytesConsumed,
            ref byte[]? rented,
            Span<byte> tmpSpace = default)
        {
            Debug.Assert(rented == null);

            if (TryReadPrimitiveOctetStringCore(
                source,
                ruleSet,
                expectedTag,
                universalTagNumber,
                out int? contentLength,
                out int headerLength,
                out ReadOnlySpan<byte> contents,
                out bytesConsumed))
            {
                return contents;
            }

            // If we get here, the tag was appropriate, but the encoding was constructed.

            // Guaranteed long enough
            contents = source.Slice(headerLength);
            int tooBig = contentLength ?? SeekEndOfContents(contents, ruleSet);

            if (tmpSpace.Length > 0 && tooBig > tmpSpace.Length)
            {
                bool isIndefinite = contentLength == null;
                tooBig = CountConstructedOctetString(contents, ruleSet, isIndefinite);
            }

            if (tooBig > tmpSpace.Length)
            {
                rented = CryptoPool.Rent(tooBig);
                tmpSpace = rented;
            }

            if (TryCopyConstructedOctetStringContents(
                Slice(source, headerLength, contentLength),
                ruleSet,
                tmpSpace,
                contentLength == null,
                out int bytesRead,
                out int bytesWritten))
            {
                bytesConsumed = headerLength + bytesRead;
                return tmpSpace.Slice(0, bytesWritten);
            }

            Debug.Fail("TryCopyConstructedOctetStringContents failed with a pre-allocated buffer");
            throw new AsnContentException();
        }
    }

    public partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as an OCTET STRING with a specified tag, copying the value
        ///   into a provided destination buffer.
        /// </summary>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 4).
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
        /// <seealso cref="TryReadPrimitiveOctetString"/>
        /// <seealso cref="ReadOctetString"/>
        public bool TryReadOctetString(
            Span<byte> destination,
            out int bytesWritten,
            Asn1Tag? expectedTag = null)
        {
            bool ret = AsnDecoder.TryReadOctetString(
                _data.Span,
                destination,
                RuleSet,
                out int bytesConsumed,
                out bytesWritten,
                expectedTag);

            if (ret)
            {
                _data = _data.Slice(bytesConsumed);
            }

            return ret;
        }

        /// <summary>
        ///   Reads the next value as an OCTET STRING with tag UNIVERSAL 4, returning the value
        ///   in a byte array.
        /// </summary>
        /// <param name="expectedTag">
        ///   The tag to check for before reading, or <see langword="null"/> for the default tag (Universal 4).
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
        /// <seealso cref="TryReadPrimitiveOctetString"/>
        /// <seealso cref="TryReadOctetString"/>
        public byte[] ReadOctetString(Asn1Tag? expectedTag = null)
        {
            byte[] ret = AsnDecoder.ReadOctetString(_data.Span, RuleSet, out int consumed, expectedTag);
            _data = _data.Slice(consumed);
            return ret;
        }

        /// <summary>
        ///   Attempts to read the next value as an OCTET STRING with a specified tag, returning the contents
        ///   as a <see cref="ReadOnlyMemory{T}"/> over the original data.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="contents">
        ///   On success, receives a <see cref="ReadOnlyMemory{T}"/> over the original data
        ///   corresponding to the value of the OCTET STRING.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> and advances the reader if the OCTET STRING value had a primitive encoding,
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
        /// <seealso cref="TryReadOctetString"/>
        public bool TryReadPrimitiveOctetString(out ReadOnlyMemory<byte> contents, Asn1Tag? expectedTag = null)
        {
            bool ret = AsnDecoder.TryReadPrimitiveOctetString(
                _data.Span,
                RuleSet,
                out ReadOnlySpan<byte> span,
                out int consumed,
                expectedTag);

            if (ret)
            {
                contents = AsnDecoder.Slice(_data, span);
                _data = _data.Slice(consumed);
            }
            else
            {
                contents = default;
            }

            return ret;
        }
    }
}

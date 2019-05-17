// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Security.Cryptography.Asn1
{
    internal partial class AsnReader
    {
        /// <summary>
        ///   Reads the next value as a BIT STRING with tag UNIVERSAL 3, returning the contents
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
        /// <returns>
        ///   <c>true</c> and advances the reader if the BIT STRING value had a primitive encoding,
        ///   <c>false</c> and does not advance the reader if it had a constructed encoding.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <seealso cref="TryCopyBitStringBytes(Span{byte},out int,out int)"/>
        public bool TryReadPrimitiveBitStringValue(out int unusedBitCount, out ReadOnlyMemory<byte> value)
            => TryReadPrimitiveBitStringValue(Asn1Tag.PrimitiveBitString, out unusedBitCount, out value);

        /// <summary>
        ///   Reads the next value as a BIT STRING with a specified tag, returning the contents
        ///   as a <see cref="ReadOnlyMemory{T}"/> over the original data.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="unusedBitCount">
        ///   On success, receives the number of bits in the last byte which were reported as
        ///   "unused" by the writer.
        /// </param>
        /// <param name="value">
        ///   On success, receives a <see cref="ReadOnlyMemory{T}"/> over the original data
        ///   corresponding to the value of the BIT STRING.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if the BIT STRING value had a primitive encoding,
        ///   <c>false</c> and does not advance the reader if it had a constructed encoding.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <seealso cref="TryCopyBitStringBytes(Asn1Tag,Span{byte},out int,out int)"/>
        public bool TryReadPrimitiveBitStringValue(
            Asn1Tag expectedTag,
            out int unusedBitCount,
            out ReadOnlyMemory<byte> value)
        {
            bool isPrimitive = TryReadPrimitiveBitStringValue(
                expectedTag,
                out Asn1Tag actualTag,
                out int? contentsLength,
                out int headerLength,
                out unusedBitCount,
                out value,
                out byte normalizedLastByte);

            if (isPrimitive)
            {
                // A BER reader which encountered a situation where an "unused" bit was not
                // set to 0.
                if (value.Length != 0 && normalizedLastByte != value.Span[value.Length - 1])
                {
                    unusedBitCount = 0;
                    value = default(ReadOnlyMemory<byte>);
                    return false;
                }

                // Skip the tag+length (header) and the unused bit count byte (1) and the contents.
                _data = _data.Slice(headerLength + value.Length + 1);
            }

            return isPrimitive;
        }

        /// <summary>
        ///   Reads the next value as a BIT STRING with tag UNIVERSAL 3, copying the value
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
        /// <returns>
        ///   <c>true</c> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <c>false</c> and the reader does not advance.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <seealso cref="TryReadPrimitiveBitStringValue(out int,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="ReadBitString(out int)"/>
        public bool TryCopyBitStringBytes(
            Span<byte> destination,
            out int unusedBitCount,
            out int bytesWritten)
        {
            return TryCopyBitStringBytes(
                Asn1Tag.PrimitiveBitString,
                destination,
                out unusedBitCount,
                out bytesWritten);
        }

        /// <summary>
        ///   Reads the next value as a BIT STRING with a specified tag, copying the value
        ///   into a provided destination buffer.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="unusedBitCount">
        ///   On success, receives the number of bits in the last byte which were reported as
        ///   "unused" by the writer.
        /// </param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <c>false</c> and the reader does not advance.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <seealso cref="TryReadPrimitiveBitStringValue(Asn1Tag,out int,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="ReadBitString(Asn1Tag,out int)"/>
        public bool TryCopyBitStringBytes(
            Asn1Tag expectedTag,
            Span<byte> destination,
            out int unusedBitCount,
            out int bytesWritten)
        {
            if (TryReadPrimitiveBitStringValue(
                expectedTag,
                out Asn1Tag actualTag,
                out int? contentsLength,
                out int headerLength,
                out unusedBitCount,
                out ReadOnlyMemory<byte> value,
                out byte normalizedLastByte))
            {
                if (value.Length > destination.Length)
                {
                    bytesWritten = 0;
                    unusedBitCount = 0;
                    return false;
                }

                CopyBitStringValue(value, normalizedLastByte, destination);

                bytesWritten = value.Length;
                // contents doesn't include the unusedBitCount value, so add one byte for that.
                _data = _data.Slice(headerLength + value.Length + 1);
                return true;
            }

            Debug.Assert(actualTag.IsConstructed);

            bool read = TryCopyConstructedBitStringValue(
                Slice(_data, headerLength, contentsLength),
                destination,
                contentsLength == null,
                out unusedBitCount,
                out int bytesRead,
                out bytesWritten);

            if (read)
            {
                _data = _data.Slice(headerLength + bytesRead);
            }

            return read;
        }

        /// <summary>
        ///   Reads the next value as a BIT STRING with tag UNIVERSAL 3, copying the value
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
        /// <returns>
        ///   <c>true</c> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <c>false</c> and the reader does not advance.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <seealso cref="TryReadPrimitiveBitStringValue(out int,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="ReadBitString(out int)"/>
        public bool TryCopyBitStringBytes(
            ArraySegment<byte> destination,
            out int unusedBitCount,
            out int bytesWritten)
        {
            return TryCopyBitStringBytes(
                Asn1Tag.PrimitiveBitString,
                destination.AsSpan(),
                out unusedBitCount,
                out bytesWritten);
        }

        /// <summary>
        ///   Reads the next value as a BIT STRING with a specified tag, copying the value
        ///   into a provided destination buffer.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="unusedBitCount">
        ///   On success, receives the number of bits in the last byte which were reported as
        ///   "unused" by the writer.
        /// </param>
        /// <param name="bytesWritten">
        ///   On success, receives the number of bytes written to <paramref name="destination"/>.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if <paramref name="destination"/> had sufficient
        ///   length to receive the value, otherwise
        ///   <c>false</c> and the reader does not advance.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <seealso cref="TryReadPrimitiveBitStringValue(Asn1Tag,out int,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="ReadBitString(Asn1Tag,out int)"/>
        public bool TryCopyBitStringBytes(
            Asn1Tag expectedTag,
            ArraySegment<byte> destination,
            out int unusedBitCount,
            out int bytesWritten)
        {
            return TryCopyBitStringBytes(
                expectedTag,
                destination.AsSpan(),
                out unusedBitCount,
                out bytesWritten);
        }

        /// <summary>
        ///   Reads the next value as a BIT STRING with tag UNIVERSAL 3, returning the value
        ///   in a byte array.
        /// </summary>
        /// <param name="unusedBitCount">
        ///   On success, receives the number of bits in the last byte which were reported as
        ///   "unused" by the writer.
        /// </param>
        /// <returns>
        ///   a copy of the value in a newly allocated, precisely sized, array.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <seealso cref="TryReadPrimitiveBitStringValue(out int,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="TryCopyBitStringBytes(Span{byte},out int,out int)"/>
        public byte[] ReadBitString(out int unusedBitCount)
        {
            return ReadBitString(Asn1Tag.PrimitiveBitString, out unusedBitCount);
        }

        /// <summary>
        ///   Reads the next value as a BIT STRING with tag UNIVERSAL 3, returning the value
        ///   in a byte array.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="unusedBitCount">
        ///   On success, receives the number of bits in the last byte which were reported as
        ///   "unused" by the writer.
        /// </param>
        /// <returns>
        ///   a copy of the value in a newly allocated, precisely sized, array.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagClass"/> is
        ///   <see cref="TagClass.Universal"/>, but
        ///   <paramref name="expectedTag"/>.<see cref="Asn1Tag.TagValue"/> is not correct for
        ///   the method
        /// </exception>
        /// <seealso cref="TryReadPrimitiveBitStringValue(Asn1Tag,out int,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="TryCopyBitStringBytes(Asn1Tag,Span{byte},out int,out int)"/>
        public byte[] ReadBitString(Asn1Tag expectedTag, out int unusedBitCount)
        {
            ReadOnlyMemory<byte> memory;

            if (TryReadPrimitiveBitStringValue(expectedTag, out unusedBitCount, out memory))
            {
                return memory.ToArray();
            }

            memory = PeekEncodedValue();

            // Guaranteed long enough
            byte[] rented = CryptoPool.Rent(memory.Length);
            int dataLength = 0;

            try
            {
                if (!TryCopyBitStringBytes(expectedTag, rented, out unusedBitCount, out dataLength))
                {
                    Debug.Fail("TryCopyBitStringBytes failed with a pre-allocated buffer");
                    throw new CryptographicException();
                }

                byte[] alloc = new byte[dataLength];
                rented.AsSpan(0, dataLength).CopyTo(alloc);
                return alloc;
            }
            finally
            {
                CryptoPool.Return(rented, dataLength);
            }
        }

        private void ParsePrimitiveBitStringContents(
            ReadOnlyMemory<byte> source,
            out int unusedBitCount,
            out ReadOnlyMemory<byte> value,
            out byte normalizedLastByte)
        {
            // T-REC-X.690-201508 sec 9.2
            if (RuleSet == AsnEncodingRules.CER && source.Length > MaxCERSegmentSize)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // T-REC-X.690-201508 sec 8.6.2.3
            if (source.Length == 0)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            ReadOnlySpan<byte> sourceSpan = source.Span;
            unusedBitCount = sourceSpan[0];

            // T-REC-X.690-201508 sec 8.6.2.2
            if (unusedBitCount > 7)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            if (source.Length == 1)
            {
                // T-REC-X.690-201508 sec 8.6.2.4
                if (unusedBitCount > 0)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                Debug.Assert(unusedBitCount == 0);
                value = ReadOnlyMemory<byte>.Empty;
                normalizedLastByte = 0;
                return;
            }

            // Build a mask for the bits that are used so the normalized value can be computed
            //
            // If 3 bits are "unused" then build a mask for them to check for 0.
            // -1 << 3 => 0b1111_1111 << 3 => 0b1111_1000
            int mask = -1 << unusedBitCount;
            byte lastByte = sourceSpan[sourceSpan.Length - 1];
            byte maskedByte = (byte)(lastByte & mask);

            if (maskedByte != lastByte)
            {
                // T-REC-X.690-201508 sec 11.2.1
                if (RuleSet == AsnEncodingRules.DER || RuleSet == AsnEncodingRules.CER)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }

            normalizedLastByte = maskedByte;
            value = source.Slice(1);
        }

        private delegate void BitStringCopyAction(
            ReadOnlyMemory<byte> value,
            byte normalizedLastByte,
            Span<byte> destination);

        private static void CopyBitStringValue(
            ReadOnlyMemory<byte> value,
            byte normalizedLastByte,
            Span<byte> destination)
        {
            if (value.Length == 0)
            {
                return;
            }

            value.Span.CopyTo(destination);
            // Replace the last byte with the normalized answer.
            destination[value.Length - 1] = normalizedLastByte;
        }

        private int CountConstructedBitString(ReadOnlyMemory<byte> source, bool isIndefinite)
        {
            Span<byte> destination = Span<byte>.Empty;

            return ProcessConstructedBitString(
                source,
                destination,
                null,
                isIndefinite,
                out _,
                out _);
        }

        private void CopyConstructedBitString(
            ReadOnlyMemory<byte> source,
            Span<byte> destination,
            bool isIndefinite,
            out int unusedBitCount,
            out int bytesRead,
            out int bytesWritten)
        {
            Span<byte> tmpDest = destination;

            bytesWritten = ProcessConstructedBitString(
                source,
                tmpDest,
                (value, lastByte, dest) => CopyBitStringValue(value, lastByte, dest),
                isIndefinite,
                out unusedBitCount,
                out bytesRead);
        }

        private int ProcessConstructedBitString(
            ReadOnlyMemory<byte> source,
            Span<byte> destination,
            BitStringCopyAction copyAction,
            bool isIndefinite,
            out int lastUnusedBitCount,
            out int bytesRead)
        {
            lastUnusedBitCount = 0;
            bytesRead = 0;
            int lastSegmentLength = MaxCERSegmentSize;

            AsnReader tmpReader = new AsnReader(source, RuleSet);
            Stack<(AsnReader, bool, int)> readerStack = null;
            int totalLength = 0;
            Asn1Tag tag = Asn1Tag.ConstructedBitString;
            Span<byte> curDest = destination;

            do
            {
                while (tmpReader.HasData)
                {
                    tag = tmpReader.ReadTagAndLength(out int? length, out int headerLength);

                    if (tag == Asn1Tag.PrimitiveBitString)
                    {
                        if (lastUnusedBitCount != 0)
                        {
                            // T-REC-X.690-201508 sec 8.6.4, only the last segment may have
                            // a number of bits not a multiple of 8.
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        if (RuleSet == AsnEncodingRules.CER && lastSegmentLength != MaxCERSegmentSize)
                        {
                            // T-REC-X.690-201508 sec 9.2
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        Debug.Assert(length != null);
                        ReadOnlyMemory<byte> encodedValue = Slice(tmpReader._data, headerLength, length.Value);

                        ParsePrimitiveBitStringContents(
                            encodedValue,
                            out lastUnusedBitCount,
                            out ReadOnlyMemory<byte> contents,
                            out byte normalizedLastByte);

                        int localLen = headerLength + encodedValue.Length;
                        tmpReader._data = tmpReader._data.Slice(localLen);

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
                            (AsnReader topReader, bool wasIndefinite, int pushedBytesRead) = readerStack.Pop();
                            topReader._data = topReader._data.Slice(bytesRead);

                            bytesRead += pushedBytesRead;
                            isIndefinite = wasIndefinite;
                            tmpReader = topReader;
                        }
                        else
                        {
                            // We have matched the EndOfContents that brought us here.
                            break;
                        }
                    }
                    else if (tag == Asn1Tag.ConstructedBitString)
                    {
                        if (RuleSet == AsnEncodingRules.CER)
                        {
                            // T-REC-X.690-201508 sec 9.2
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        if (readerStack == null)
                        {
                            readerStack = new Stack<(AsnReader, bool, int)>();
                        }

                        readerStack.Push((tmpReader, isIndefinite, bytesRead));

                        tmpReader = new AsnReader(
                            Slice(tmpReader._data, headerLength, length),
                            RuleSet);

                        bytesRead = headerLength;
                        isIndefinite = (length == null);
                    }
                    else
                    {
                        // T-REC-X.690-201508 sec 8.6.4.1 (in particular, Note 2)
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }
                }

                if (isIndefinite && tag != Asn1Tag.EndOfContents)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                if (readerStack?.Count > 0)
                {
                    (AsnReader topReader, bool wasIndefinite, int pushedBytesRead) = readerStack.Pop();

                    tmpReader = topReader;
                    tmpReader._data = tmpReader._data.Slice(bytesRead);

                    isIndefinite = wasIndefinite;
                    bytesRead += pushedBytesRead;
                }
                else
                {
                    tmpReader = null;
                }
            } while (tmpReader != null);

            return totalLength;
        }

        private bool TryCopyConstructedBitStringValue(
            ReadOnlyMemory<byte> source,
            Span<byte> dest,
            bool isIndefinite,
            out int unusedBitCount,
            out int bytesRead,
            out int bytesWritten)
        {
            // Call CountConstructedBitString to get the required byte and to verify that the
            // data is well-formed before copying into dest.
            int contentLength = CountConstructedBitString(source, isIndefinite);

            // Since the unused bits byte from the segments don't count, only one segment
            // returns 999 (or less), the second segment bumps the count to 1000, and is legal.
            //
            // T-REC-X.690-201508 sec 9.2
            if (RuleSet == AsnEncodingRules.CER && contentLength < MaxCERSegmentSize)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
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
                dest,
                isIndefinite,
                out unusedBitCount,
                out bytesRead,
                out bytesWritten);

            Debug.Assert(bytesWritten == contentLength);
            return true;
        }

        private bool TryReadPrimitiveBitStringValue(
            Asn1Tag expectedTag,
            out Asn1Tag actualTag,
            out int? contentsLength,
            out int headerLength,
            out int unusedBitCount,
            out ReadOnlyMemory<byte> value,
            out byte normalizedLastByte)
        {
            actualTag = ReadTagAndLength(out contentsLength, out headerLength);
            CheckExpectedTag(actualTag, expectedTag, UniversalTagNumber.BitString);

            if (actualTag.IsConstructed)
            {
                if (RuleSet == AsnEncodingRules.DER)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                unusedBitCount = 0;
                value = default(ReadOnlyMemory<byte>);
                normalizedLastByte = 0;
                return false;
            }

            Debug.Assert(contentsLength.HasValue);
            ReadOnlyMemory<byte> encodedValue = Slice(_data, headerLength, contentsLength.Value);

            ParsePrimitiveBitStringContents(
                encodedValue,
                out unusedBitCount,
                out value,
                out normalizedLastByte);

            return true;
        }
    }
}

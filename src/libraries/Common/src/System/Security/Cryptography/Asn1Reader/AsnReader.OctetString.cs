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
        ///   Reads the next value as an OCTET STRING with tag UNIVERSAL 4, copying the value
        ///   into a provided destination buffer.
        /// </summary>
        /// <param name="destination">The buffer in which to write.</param>
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
        /// <seealso cref="TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory{byte})"/>
        /// <seealso cref="ReadOctetString()"/>
        public bool TryCopyOctetStringBytes(
            Span<byte> destination,
            out int bytesWritten)
        {
            return TryCopyOctetStringBytes(
                Asn1Tag.PrimitiveOctetString,
                destination,
                out bytesWritten);
        }

        /// <summary>
        ///   Reads the next value as an OCTET STRING with a specified tag, copying the value
        ///   into a provided destination buffer.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="destination">The buffer in which to write.</param>
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
        /// <seealso cref="TryReadPrimitiveOctetStringBytes(Asn1Tag,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="ReadOctetString(Asn1Tag)"/>
        public bool TryCopyOctetStringBytes(
            Asn1Tag expectedTag,
            Span<byte> destination,
            out int bytesWritten)
        {
            if (TryReadPrimitiveOctetStringBytes(
                expectedTag,
                out Asn1Tag actualTag,
                out int? contentLength,
                out int headerLength,
                out ReadOnlyMemory<byte> contents))
            {
                if (contents.Length > destination.Length)
                {
                    bytesWritten = 0;
                    return false;
                }

                contents.Span.CopyTo(destination);
                bytesWritten = contents.Length;
                _data = _data.Slice(headerLength + contents.Length);
                return true;
            }

            Debug.Assert(actualTag.IsConstructed);

            bool copied = TryCopyConstructedOctetStringContents(
                Slice(_data, headerLength, contentLength),
                destination,
                contentLength == null,
                out int bytesRead,
                out bytesWritten);

            if (copied)
            {
                _data = _data.Slice(headerLength + bytesRead);
            }

            return copied;
        }

        /// <summary>
        ///   Reads the next value as an OCTET STRING with tag UNIVERSAL 4, copying the value
        ///   into a provided destination buffer.
        /// </summary>
        /// <param name="destination">The buffer in which to write.</param>
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
        /// <seealso cref="TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory{byte})"/>
        /// <seealso cref="ReadOctetString()"/>
        public bool TryCopyOctetStringBytes(
            ArraySegment<byte> destination,
            out int bytesWritten)
        {
            return TryCopyOctetStringBytes(
                Asn1Tag.PrimitiveOctetString,
                destination.AsSpan(),
                out bytesWritten);
        }

        /// <summary>
        ///   Reads the next value as an OCTET STRING with a specified tag, copying the value
        ///   into a provided destination buffer.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="destination">The buffer in which to write.</param>
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
        /// <seealso cref="TryReadPrimitiveOctetStringBytes(Asn1Tag,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="ReadOctetString(Asn1Tag)"/>
        public bool TryCopyOctetStringBytes(
            Asn1Tag expectedTag,
            ArraySegment<byte> destination,
            out int bytesWritten)
        {
            return TryCopyOctetStringBytes(
                expectedTag,
                destination.AsSpan(),
                out bytesWritten);
        }

        /// <summary>
        ///   Reads the next value as an OCTET STRING with tag UNIVERSAL 4, returning the value
        ///   in a byte array.
        /// </summary>
        /// <returns>
        ///   a copy of the contents in a newly allocated, precisely sized, array.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <seealso cref="TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory{byte})"/>
        /// <seealso cref="TryCopyOctetStringBytes(Span{byte},out int)"/>
        /// <seealso cref="ReadOctetString(Asn1Tag)"/>
        public byte[] ReadOctetString()
        {
            return ReadOctetString(Asn1Tag.PrimitiveOctetString);
        }

        /// <summary>
        ///   Reads the next value as an OCTET STRING with tag UNIVERSAL 4, returning the value
        ///   in a byte array.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
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
        /// <seealso cref="TryReadPrimitiveOctetStringBytes(Asn1Tag,out ReadOnlyMemory{byte})"/>
        /// <seealso cref="TryCopyOctetStringBytes(Asn1Tag,Span{byte},out int)"/>
        /// <seealso cref="ReadOctetString()"/>
        public byte[] ReadOctetString(Asn1Tag expectedTag)
        {
            ReadOnlyMemory<byte> memory;

            if (TryReadPrimitiveOctetStringBytes(expectedTag, out memory))
            {
                return memory.ToArray();
            }

            memory = PeekEncodedValue();

            // Guaranteed long enough
            byte[] rented = CryptoPool.Rent(memory.Length);
            int dataLength = 0;

            try
            {
                if (!TryCopyOctetStringBytes(expectedTag, rented, out dataLength))
                {
                    Debug.Fail("TryCopyOctetStringBytes failed with a pre-allocated buffer");
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

        private bool TryReadPrimitiveOctetStringBytes(
            Asn1Tag expectedTag,
            out Asn1Tag actualTag,
            out int? contentLength,
            out int headerLength,
            out ReadOnlyMemory<byte> contents,
            UniversalTagNumber universalTagNumber = UniversalTagNumber.OctetString)
        {
            actualTag = ReadTagAndLength(out contentLength, out headerLength);
            CheckExpectedTag(actualTag, expectedTag, universalTagNumber);

            if (actualTag.IsConstructed)
            {
                if (RuleSet == AsnEncodingRules.DER)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                contents = default(ReadOnlyMemory<byte>);
                return false;
            }

            Debug.Assert(contentLength.HasValue);
            ReadOnlyMemory<byte> encodedValue = Slice(_data, headerLength, contentLength.Value);

            if (RuleSet == AsnEncodingRules.CER && encodedValue.Length > MaxCERSegmentSize)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            contents = encodedValue;
            return true;
        }

        private bool TryReadPrimitiveOctetStringBytes(
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            out ReadOnlyMemory<byte> contents)
        {
            if (TryReadPrimitiveOctetStringBytes(
                expectedTag,
                out _,
                out _,
                out int headerLength,
                out contents,
                universalTagNumber))
            {
                _data = _data.Slice(headerLength + contents.Length);
                return true;
            }

            return false;
        }

        /// <summary>
        ///   Reads the next value as an OCTET STRING with tag UNIVERSAL 4, returning the contents
        ///   as a <see cref="ReadOnlyMemory{T}"/> over the original data.
        /// </summary>
        /// <param name="contents">
        ///   On success, receives a <see cref="ReadOnlyMemory{T}"/> over the original data
        ///   corresponding to the contents of the OCTET STRING.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if the OCTET STRING value had a primitive encoding,
        ///   <c>false</c> and does not advance the reader if it had a constructed encoding.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   the next value does not have the correct tag --OR--
        ///   the length encoding is not valid under the current encoding rules --OR--
        ///   the contents are not valid under the current encoding rules
        /// </exception>
        /// <seealso cref="TryCopyOctetStringBytes(Span{byte},out int)"/>
        public bool TryReadPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> contents) =>
            TryReadPrimitiveOctetStringBytes(Asn1Tag.PrimitiveOctetString, out contents);

        /// <summary>
        ///   Reads the next value as an OCTET STRING with a specified tag, returning the contents
        ///   as a <see cref="ReadOnlyMemory{T}"/> over the original data.
        /// </summary>
        /// <param name="expectedTag">The tag to check for before reading.</param>
        /// <param name="contents">
        ///   On success, receives a <see cref="ReadOnlyMemory{T}"/> over the original data
        ///   corresponding to the value of the OCTET STRING.
        /// </param>
        /// <returns>
        ///   <c>true</c> and advances the reader if the OCTET STRING value had a primitive encoding,
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
        /// <seealso cref="TryCopyOctetStringBytes(Asn1Tag,Span{byte},out int)"/>
        public bool TryReadPrimitiveOctetStringBytes(Asn1Tag expectedTag, out ReadOnlyMemory<byte> contents)
        {
            return TryReadPrimitiveOctetStringBytes(expectedTag, UniversalTagNumber.OctetString, out contents);
        }

        private int CountConstructedOctetString(ReadOnlyMemory<byte> source, bool isIndefinite)
        {
            int contentLength = CopyConstructedOctetString(
                source,
                Span<byte>.Empty,
                false,
                isIndefinite,
                out _);

            // T-REC-X.690-201508 sec 9.2
            if (RuleSet == AsnEncodingRules.CER && contentLength <= MaxCERSegmentSize)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return contentLength;
        }

        private void CopyConstructedOctetString(
            ReadOnlyMemory<byte> source,
            Span<byte> destination,
            bool isIndefinite,
            out int bytesRead,
            out int bytesWritten)
        {
            bytesWritten = CopyConstructedOctetString(
                source,
                destination,
                true,
                isIndefinite,
                out bytesRead);
        }

        private int CopyConstructedOctetString(
            ReadOnlyMemory<byte> source,
            Span<byte> destination,
            bool write,
            bool isIndefinite,
            out int bytesRead)
        {
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

                    if (tag == Asn1Tag.PrimitiveOctetString)
                    {
                        if (RuleSet == AsnEncodingRules.CER && lastSegmentLength != MaxCERSegmentSize)
                        {
                            // T-REC-X.690-201508 sec 9.2
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        Debug.Assert(length != null);

                        // The call to Slice here sanity checks the data bounds, length.Value is not
                        // reliable unless this call has succeeded.
                        ReadOnlyMemory<byte> contents = Slice(tmpReader._data, headerLength, length.Value);

                        int localLen = headerLength + contents.Length;
                        tmpReader._data = tmpReader._data.Slice(localLen);

                        bytesRead += localLen;
                        totalLength += contents.Length;
                        lastSegmentLength = contents.Length;

                        if (RuleSet == AsnEncodingRules.CER && lastSegmentLength > MaxCERSegmentSize)
                        {
                            // T-REC-X.690-201508 sec 9.2
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }

                        if (write)
                        {
                            contents.Span.CopyTo(curDest);
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
                    else if (tag == Asn1Tag.ConstructedOctetString)
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

        private bool TryCopyConstructedOctetStringContents(
            ReadOnlyMemory<byte> source,
            Span<byte> dest,
            bool isIndefinite,
            out int bytesRead,
            out int bytesWritten)
        {
            bytesRead = 0;

            int contentLength = CountConstructedOctetString(source, isIndefinite);

            if (dest.Length < contentLength)
            {
                bytesWritten = 0;
                return false;
            }

            CopyConstructedOctetString(source, dest, isIndefinite, out bytesRead, out bytesWritten);

            Debug.Assert(bytesWritten == contentLength);
            return true;
        }

        private ReadOnlySpan<byte> GetOctetStringContents(
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            out int bytesRead,
            ref byte[] rented,
            Span<byte> tmpSpace = default)
        {
            Debug.Assert(rented == null);

            if (TryReadPrimitiveOctetStringBytes(
                expectedTag,
                out Asn1Tag actualTag,
                out int? contentLength,
                out int headerLength,
                out ReadOnlyMemory<byte> contentsOctets,
                universalTagNumber))
            {
                bytesRead = headerLength + contentsOctets.Length;
                return contentsOctets.Span;
            }

            Debug.Assert(actualTag.IsConstructed);

            ReadOnlyMemory<byte> source = Slice(_data, headerLength, contentLength);
            bool isIndefinite = contentLength == null;
            int octetStringLength = CountConstructedOctetString(source, isIndefinite);

            if (tmpSpace.Length < octetStringLength)
            {
                rented = CryptoPool.Rent(octetStringLength);
                tmpSpace = rented;
            }

            CopyConstructedOctetString(
                source,
                tmpSpace,
                isIndefinite,
                out int localBytesRead,
                out int bytesWritten);

            Debug.Assert(bytesWritten == octetStringLength);

            bytesRead = headerLength + localBytesRead;
            return tmpSpace.Slice(0, bytesWritten);
        }
    }
}

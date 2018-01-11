// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Security.Cryptography.Asn1
{
    internal class AsnReader
    {
        // T-REC-X.690-201508 sec 9.2
        internal const int MaxCERSegmentSize = 1000;

        // T-REC-X.690-201508 sec 8.1.5 says only 0000 is legal.
        private const int EndOfContentsEncodedLength = 2;

        private ReadOnlyMemory<byte> _data;
        private readonly AsnEncodingRules _ruleSet;

        public bool HasData => !_data.IsEmpty;

        public AsnReader(ReadOnlyMemory<byte> data, AsnEncodingRules ruleSet)
        {
            CheckEncodingRules(ruleSet);

            _data = data;
            _ruleSet = ruleSet;
        }

        public void ThrowIfNotEmpty()
        {
            if (HasData)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

        public static bool TryPeekTag(ReadOnlySpan<byte> source, out Asn1Tag tag, out int bytesRead)
        {
            return Asn1Tag.TryParse(source, out tag, out bytesRead);
        }

        public Asn1Tag PeekTag()
        {
            if (TryPeekTag(_data.Span, out Asn1Tag tag, out int bytesRead))
            {
                return tag;
            }

            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
        }

        private static bool TryReadLength(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet,
            out int? length,
            out int bytesRead)
        {
            length = null;
            bytesRead = 0;

            CheckEncodingRules(ruleSet);

            if (source.IsEmpty)
            {
                return false;
            }

            // T-REC-X.690-201508 sec 8.1.3

            byte lengthOrLengthLength = source[bytesRead];
            bytesRead++;
            const byte MultiByteMarker = 0x80;

            // 0x00-0x7F are direct length values.
            // 0x80 is BER/CER indefinite length.
            // 0x81-0xFE says that the length takes the next 1-126 bytes.
            // 0xFF is forbidden.
            if (lengthOrLengthLength == MultiByteMarker)
            {
                // T-REC-X.690-201508 sec 10.1 (DER: Length forms)
                if (ruleSet == AsnEncodingRules.DER)
                {
                    bytesRead = 0;
                    return false;
                }

                // Null length == indefinite.
                return true;
            }

            if (lengthOrLengthLength < MultiByteMarker)
            {
                length = lengthOrLengthLength;
                return true;
            }

            if (lengthOrLengthLength == 0xFF)
            {
                bytesRead = 0;
                return false;
            }

            byte lengthLength = (byte)(lengthOrLengthLength & ~MultiByteMarker);

            // +1 for lengthOrLengthLength
            if (lengthLength + 1 > source.Length)
            {
                bytesRead = 0;
                return false;
            }

            // T-REC-X.690-201508 sec 9.1 (CER: Length forms)
            // T-REC-X.690-201508 sec 10.1 (DER: Length forms)
            bool minimalRepresentation =
                ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER;

            // The ITU-T specifications tecnically allow lengths up to ((2^128) - 1), but
            // since Span's length is a signed Int32 we're limited to identifying memory
            // that is within ((2^31) - 1) bytes of the tag start.
            if (minimalRepresentation && lengthLength > sizeof(int))
            {
                bytesRead = 0;
                return false;
            }

            uint parsedLength = 0;

            for (int i = 0; i < lengthLength; i++)
            {
                byte current = source[bytesRead];
                bytesRead++;

                if (parsedLength == 0)
                {
                    if (minimalRepresentation && current == 0)
                    {
                        bytesRead = 0;
                        return false;
                    }

                    if (!minimalRepresentation && current != 0)
                    {
                        // Under BER rules we could have had padding zeros, so
                        // once the first data bits come in check that we fit within
                        // sizeof(int) due to Span bounds.

                        if (lengthLength - i > sizeof(int))
                        {
                            bytesRead = 0;
                            return false;
                        }
                    }
                }

                parsedLength <<= 8;
                parsedLength |= current;
            }

            // This value cannot be represented as a Span length.
            if (parsedLength > int.MaxValue)
            {
                bytesRead = 0;
                return false;
            }

            if (minimalRepresentation && parsedLength < MultiByteMarker)
            {
                bytesRead = 0;
                return false;
            }

            Debug.Assert(bytesRead > 0);
            length = (int)parsedLength;
            return true;
        }

        internal Asn1Tag ReadTagAndLength(out int? contentsLength, out int bytesRead)
        {
            if (TryPeekTag(_data.Span, out Asn1Tag tag, out int tagBytesRead) &&
                TryReadLength(_data.Slice(tagBytesRead).Span, _ruleSet, out int? length, out int lengthBytesRead))
            {
                int allBytesRead = tagBytesRead + lengthBytesRead;

                if (tag.IsConstructed)
                {
                    // T-REC-X.690-201508 sec 9.1 (CER: Length forms) says constructed is always indefinite.
                    if (_ruleSet == AsnEncodingRules.CER && length != null)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }
                }
                else if (length == null)
                {
                    // T-REC-X.690-201508 sec 8.1.3.2 says primitive encodings must use a definite form.
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                bytesRead = allBytesRead;
                contentsLength = length;
                return tag;
            }

            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
        }

        private static void ValidateEndOfContents(Asn1Tag tag, int? length, int headerLength)
        {
            // T-REC-X.690-201508 sec 8.1.5 excludes the BER 8100 length form for 0.
            if (tag.IsConstructed || length != 0 || headerLength != EndOfContentsEncodedLength)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

        /// <summary>
        /// Get the number of bytes between the start of <paramref name="source" /> and
        /// the End-of-Contents marker
        /// </summary>
        private int SeekEndOfContents(ReadOnlyMemory<byte> source)
        {
            ReadOnlyMemory<byte> cur = source;
            int totalLen = 0;

            AsnReader tmpReader = new AsnReader(cur, _ruleSet);
            // Our reader is bounded by int.MaxValue.
            // The most aggressive data input would be a one-byte tag followed by
            // indefinite length "ad infinitum", which would be half the input.
            // So the depth marker can never overflow the signed integer space.
            int depth = 1;

            while (tmpReader.HasData)
            {
                Asn1Tag tag = tmpReader.ReadTagAndLength(out int? length, out int bytesRead);

                if (tag == Asn1Tag.EndOfContents)
                {
                    ValidateEndOfContents(tag, length, bytesRead);

                    depth--;

                    if (depth == 0)
                    {
                        // T-REC-X.690-201508 sec 8.1.1.1 / 8.1.1.3 indicate that the
                        // End-of-Contents octets are "after" the contents octets, not
                        // "at the end" of them, so we don't include these bytes in the
                        // accumulator.
                        return totalLen;
                    }
                }

                // We found another indefinite length, that means we need to find another
                // EndOfContents marker to balance it out.
                if (length == null)
                {
                    depth++;
                    tmpReader._data = tmpReader._data.Slice(bytesRead);
                    totalLen += bytesRead;
                }
                else
                {
                    // This will throw a CryptographicException if the length exceeds our bounds.
                    ReadOnlyMemory<byte> tlv = Slice(tmpReader._data, 0, bytesRead + length.Value);
                    
                    // No exception? Then slice the data and continue.
                    tmpReader._data = tmpReader._data.Slice(tlv.Length);
                    totalLen += tlv.Length;
                }
            }

            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
        }

        /// <summary>
        /// Get a ReadOnlyMemory view of the next encoded value without consuming it.
        /// For indefinite length encodings this includes the End of Contents marker.
        /// </summary>
        /// <returns>A ReadOnlyMemory view of the next encoded value.</returns>
        /// <exception cref="CryptographicException">
        /// The reader is positioned at a point where the tag or length is invalid
        /// under the current encoding rules.
        /// </exception>
        /// <seealso cref="PeekContentBytes"/>
        /// <seealso cref="GetEncodedValue"/>
        public ReadOnlyMemory<byte> PeekEncodedValue()
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out int bytesRead);

            if (length == null)
            {
                int contentsLength = SeekEndOfContents(_data.Slice(bytesRead));
                return Slice(_data, 0, bytesRead + contentsLength + EndOfContentsEncodedLength);
            }

            return Slice(_data, 0, bytesRead + length.Value);
        }

        /// <summary>
        /// Get a ReadOnlyMemory view of the content octets (bytes) of the next encoded
        /// value without consuming it.
        /// </summary>
        /// <returns>A ReadOnlyMemory view of the contents octets of the next encoded value.</returns>
        /// <exception cref="CryptographicException">
        /// The reader is positioned at a point where the tag or length is invalid
        /// under the current encoding rules.
        /// </exception>
        /// <seealso cref="PeekEncodedValue"/>
        public ReadOnlyMemory<byte> PeekContentBytes()
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out int bytesRead);

            if (length == null)
            {
                return Slice(_data, bytesRead, SeekEndOfContents(_data.Slice(bytesRead)));
            }

            return Slice(_data, bytesRead, length.Value);
        }

        /// <summary>
        /// Get a ReadOnlyMemory view of the next encoded value, and move the reader past it.
        /// For an indefinite length encoding this includes the End of Contents marker.
        /// </summary>
        /// <returns>A ReadOnlyMemory view of the next encoded value.</returns>
        /// <seealso cref="PeekEncodedValue"/>
        public ReadOnlyMemory<byte> GetEncodedValue()
        {
            ReadOnlyMemory<byte> encodedValue = PeekEncodedValue();
            _data = _data.Slice(encodedValue.Length);
            return encodedValue;
        }

        private static bool ReadBooleanValue(
            ReadOnlySpan<byte> source,
            AsnEncodingRules ruleSet)
        {
            // T-REC-X.690-201508 sec 8.2.1
            if (source.Length != 1)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            byte val = source[0];

            // T-REC-X.690-201508 sec 8.2.2
            if (val == 0)
            {
                return false;
            }

            // T-REC-X.690-201508 sec 11.1
            if (val != 0xFF && (ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER))
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return true;
        }

        public bool ReadBoolean() => ReadBoolean(Asn1Tag.Boolean);

        public bool ReadBoolean(Asn1Tag expectedTag)
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out int headerLength);
            CheckExpectedTag(tag, expectedTag, UniversalTagNumber.Boolean);

            // T-REC-X.690-201508 sec 8.2.1
            if (tag.IsConstructed)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            bool value = ReadBooleanValue(
                Slice(_data, headerLength, length.Value).Span,
                _ruleSet);

            _data = _data.Slice(headerLength + length.Value);
            return value;
        }

        private ReadOnlyMemory<byte> GetIntegerContents(
            Asn1Tag expectedTag,
            UniversalTagNumber tagNumber,
            out int headerLength)
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out headerLength);
            CheckExpectedTag(tag, expectedTag, tagNumber);

            // T-REC-X.690-201508 sec 8.3.1
            if (tag.IsConstructed || length < 1)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // Slice first so that an out of bounds value triggers a CryptographicException.
            ReadOnlyMemory<byte> contents = Slice(_data, headerLength, length.Value);
            ReadOnlySpan<byte> contentSpan = contents.Span;

            // T-REC-X.690-201508 sec 8.3.2
            if (contents.Length > 1)
            {
                ushort bigEndianValue = (ushort)(contentSpan[0] << 8 | contentSpan[1]);
                const ushort RedundancyMask = 0b1111_1111_1000_0000;
                ushort masked = (ushort)(bigEndianValue & RedundancyMask);

                // If the first 9 bits are all 0 or are all 1, the value is invalid.
                if (masked == 0 || masked == RedundancyMask)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }

            return contents;
        }

        public ReadOnlyMemory<byte> GetIntegerBytes() =>
            GetIntegerBytes(Asn1Tag.Integer);

        public ReadOnlyMemory<byte> GetIntegerBytes(Asn1Tag expectedTag)
        {
            ReadOnlyMemory<byte> contents =
                GetIntegerContents(expectedTag, UniversalTagNumber.Integer, out int headerLength);

            _data = _data.Slice(headerLength + contents.Length);
            return contents;
        }

        public BigInteger GetInteger() => GetInteger(Asn1Tag.Integer);

        public BigInteger GetInteger(Asn1Tag expectedTag)
        {
            ReadOnlyMemory<byte> contents =
                GetIntegerContents(expectedTag, UniversalTagNumber.Integer, out int headerLength);

            // TODO: Split this for netcoreapp/netstandard to use the Big-Endian BigInteger parsing
            byte[] tmp = ArrayPool<byte>.Shared.Rent(contents.Length);
            BigInteger value;

            try
            {
                byte fill = (contents.Span[0] & 0x80) == 0 ? (byte)0 : (byte)0xFF;
                // Fill the unused portions of tmp with positive or negative padding.
                new Span<byte>(tmp, contents.Length, tmp.Length - contents.Length).Fill(fill);
                contents.CopyTo(tmp);
                // Convert to Little-Endian.
                AsnWriter.Reverse(new Span<byte>(tmp, 0, contents.Length));
                value = new BigInteger(tmp);
            }
            finally
            {
                // Clear the whole tmp so that not even the sign bit is returned to the array pool.
                Array.Clear(tmp, 0, tmp.Length);
                ArrayPool<byte>.Shared.Return(tmp);
            }

            _data = _data.Slice(headerLength + contents.Length);
            return value;
        }

        private bool TryReadSignedInteger(
            int sizeLimit,
            Asn1Tag expectedTag,
            UniversalTagNumber tagNumber,
            out long value)
        {
            Debug.Assert(sizeLimit <= sizeof(long));

            ReadOnlyMemory<byte> contents = GetIntegerContents(expectedTag, tagNumber, out int headerLength);

            if (contents.Length > sizeLimit)
            {
                value = 0;
                return false;
            }

            ReadOnlySpan<byte> contentSpan = contents.Span;

            bool isNegative = (contentSpan[0] & 0x80) != 0;
            long accum = isNegative ? -1 : 0;

            for (int i = 0; i < contents.Length; i++)
            {
                accum <<= 8;
                accum |= contentSpan[i];
            }

            _data = _data.Slice(headerLength + contents.Length);
            value = accum;
            return true;
        }

        private bool TryReadUnsignedInteger(
            int sizeLimit,
            Asn1Tag expectedTag,
            UniversalTagNumber tagNumber,
            out ulong value)
        {
            Debug.Assert(sizeLimit <= sizeof(ulong));

            ReadOnlyMemory<byte> contents = GetIntegerContents(expectedTag, tagNumber, out int headerLength);
            ReadOnlySpan<byte> contentSpan = contents.Span;
            int contentLength = contents.Length;

            bool isNegative = (contentSpan[0] & 0x80) != 0;

            if (isNegative)
            {
                value = 0;
                return false;
            }

            // Ignore any padding zeros.
            if (contentSpan.Length > 1 && contentSpan[0] == 0)
            {
                contentSpan = contentSpan.Slice(1);
            }

            if (contentSpan.Length > sizeLimit)
            {
                value = 0;
                return false;
            }

            ulong accum = 0;

            for (int i = 0; i < contentSpan.Length; i++)
            {
                accum <<= 8;
                accum |= contentSpan[i];
            }

            _data = _data.Slice(headerLength + contentLength);
            value = accum;
            return true;
        }

        public bool TryReadInt32(out int value) =>
            TryReadInt32(Asn1Tag.Integer, out value);

        public bool TryReadInt32(Asn1Tag expectedTag, out int value)
        {
            if (TryReadSignedInteger(sizeof(int), expectedTag, UniversalTagNumber.Integer, out long longValue))
            {
                value = (int)longValue;
                return true;
            }

            value = 0;
            return false;
        }

        public bool TryReadUInt32(out uint value) =>
            TryReadUInt32(Asn1Tag.Integer, out value);

        public bool TryReadUInt32(Asn1Tag expectedTag, out uint value)
        {
            if (TryReadUnsignedInteger(sizeof(uint), expectedTag, UniversalTagNumber.Integer, out ulong ulongValue))
            {
                value = (uint)ulongValue;
                return true;
            }

            value = 0;
            return false;
        }

        public bool TryReadInt64(out long value) =>
            TryReadInt64(Asn1Tag.Integer, out value);
        
        public bool TryReadInt64(Asn1Tag expectedTag, out long value)
        {
            return TryReadSignedInteger(sizeof(long), expectedTag, UniversalTagNumber.Integer, out value);
        }

        public bool TryReadUInt64(out ulong value) =>
            TryReadUInt64(Asn1Tag.Integer, out value);

        public bool TryReadUInt64(Asn1Tag expectedTag, out ulong value)
        {
            return TryReadUnsignedInteger(sizeof(ulong), expectedTag, UniversalTagNumber.Integer, out value);
        }

        public bool TryReadInt16(out short value) =>
            TryReadInt16(Asn1Tag.Integer, out value);

        public bool TryReadInt16(Asn1Tag expectedTag, out short value)
        {
            if (TryReadSignedInteger(sizeof(short), expectedTag, UniversalTagNumber.Integer, out long longValue))
            {
                value = (short)longValue;
                return true;
            }

            value = 0;
            return false;
        }

        public bool TryReadUInt16(out ushort value) =>
            TryReadUInt16(Asn1Tag.Integer, out value);

        public bool TryReadUInt16(Asn1Tag expectedTag, out ushort value)
        {
            if (TryReadUnsignedInteger(sizeof(ushort), expectedTag, UniversalTagNumber.Integer, out ulong ulongValue))
            {
                value = (ushort)ulongValue;
                return true;
            }

            value = 0;
            return false;
        }

        public bool TryReadInt8(out sbyte value) =>
            TryReadInt8(Asn1Tag.Integer, out value);

        public bool TryReadInt8(Asn1Tag expectedTag, out sbyte value)
        {
            if (TryReadSignedInteger(sizeof(sbyte), expectedTag, UniversalTagNumber.Integer, out long longValue))
            {
                value = (sbyte)longValue;
                return true;
            }

            value = 0;
            return false;
        }

        public bool TryReadUInt8(out byte value) =>
            TryReadUInt8(Asn1Tag.Integer, out value);

        public bool TryReadUInt8(Asn1Tag expectedTag, out byte value)
        {
            if (TryReadUnsignedInteger(sizeof(byte), expectedTag, UniversalTagNumber.Integer, out ulong ulongValue))
            {
                value = (byte)ulongValue;
                return true;
            }

            value = 0;
            return false;
        }

        private void ParsePrimitiveBitStringContents(
            ReadOnlyMemory<byte> source,
            out int unusedBitCount,
            out ReadOnlyMemory<byte> value,
            out byte normalizedLastByte)
        {
            // T-REC-X.690-201508 sec 9.2
            if (_ruleSet == AsnEncodingRules.CER && source.Length > MaxCERSegmentSize)
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
                if (_ruleSet == AsnEncodingRules.DER || _ruleSet == AsnEncodingRules.CER)
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

            AsnReader tmpReader = new AsnReader(source, _ruleSet);
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

                        if (_ruleSet == AsnEncodingRules.CER && lastSegmentLength != MaxCERSegmentSize)
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
                        if (_ruleSet == AsnEncodingRules.CER)
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
                            _ruleSet);

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
            if (_ruleSet == AsnEncodingRules.CER && contentLength < MaxCERSegmentSize)
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

        private bool TryGetPrimitiveBitStringValue(
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
                if (_ruleSet == AsnEncodingRules.DER)
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

        public bool TryGetPrimitiveBitStringValue(out int unusedBitCount, out ReadOnlyMemory<byte> contents)
            => TryGetPrimitiveBitStringValue(Asn1Tag.PrimitiveBitString, out unusedBitCount, out contents);

        /// <summary>
        /// Gets a ReadOnlyMemory view over the data value portion of the contents of a bit string.
        /// </summary>
        /// <param name="expectedTag">The expected tag to read</param>
        /// <param name="unusedBitCount">The encoded value for the number of unused bits.</param>
        /// <param name="value">The data value portion of the bit string contents.</param>
        /// <returns>
        ///   <c>true</c> if the bit string uses a primitive encoding and the "unused" bits have value 0,
        ///   <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///  <ul>
        ///   <li>No data remains</li>
        ///   <li>The tag read does not match the expected tag</li>
        ///   <li>The length is invalid under the chosen encoding rules</li>
        ///   <li>The unusedBitCount value is out of bounds</li>
        ///   <li>A CER or DER encoding was chosen and an "unused" bit was set to 1</li>
        ///   <li>A CER encoding was chosen and the primitive content length exceeds the maximum allowed</li>
        /// </ul>
        /// </exception>
        public bool TryGetPrimitiveBitStringValue(
            Asn1Tag expectedTag,
            out int unusedBitCount,
            out ReadOnlyMemory<byte> value)
        {
            bool isPrimitive = TryGetPrimitiveBitStringValue(
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

        public bool TryCopyBitStringBytes(
            Asn1Tag expectedTag,
            Span<byte> destination,
            out int unusedBitCount,
            out int bytesWritten)
        {
            if (TryGetPrimitiveBitStringValue(
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

        public TFlagsEnum GetNamedBitListValue<TFlagsEnum>() where TFlagsEnum : struct =>
            GetNamedBitListValue<TFlagsEnum>(Asn1Tag.PrimitiveBitString);

        public TFlagsEnum GetNamedBitListValue<TFlagsEnum>(Asn1Tag expectedTag) where TFlagsEnum : struct
        {
            Type tFlagsEnum = typeof(TFlagsEnum);

            return (TFlagsEnum)Enum.ToObject(tFlagsEnum, GetNamedBitListValue(expectedTag, tFlagsEnum));
        }

        public Enum GetNamedBitListValue(Type tFlagsEnum) =>
            GetNamedBitListValue(Asn1Tag.PrimitiveBitString, tFlagsEnum);

        public Enum GetNamedBitListValue(Asn1Tag expectedTag, Type tFlagsEnum)
        {
            // This will throw an ArgumentException if TEnum isn't an enum type,
            // so we don't need to validate it.
            Type backingType = tFlagsEnum.GetEnumUnderlyingType();

            if (!tFlagsEnum.IsDefined(typeof(FlagsAttribute), false))
            {
                throw new ArgumentException(
                    SR.Cryptography_Asn_NamedBitListRequiresFlagsEnum,
                    nameof(tFlagsEnum));
            }

            int sizeLimit = Marshal.SizeOf(backingType);
            Span<byte> stackSpan = stackalloc byte[sizeLimit];
            ReadOnlyMemory<byte> saveData = _data;

            // If TryCopyBitStringBytes succeeds but anything else fails _data will have moved,
            // so if anything throws here just move _data back to what it was.
            try
            {
                if (!TryCopyBitStringBytes(expectedTag, stackSpan, out int unusedBitCount, out int bytesWritten))
                {
                    throw new CryptographicException(
                        SR.Format(SR.Cryptography_Asn_NamedBitListValueTooBig, tFlagsEnum.Name));
                }

                if (bytesWritten == 0)
                {
                    // The mode isn't relevant, zero is always zero.
                    return (Enum)Enum.ToObject(tFlagsEnum, 0);
                }

                ReadOnlySpan<byte> valueSpan = stackSpan.Slice(0, bytesWritten);

                // Now that the 0-bounds check is out of the way:
                // 
                // T-REC-X.690-201508 sec 11.2.2
                if (_ruleSet == AsnEncodingRules.DER ||
                    _ruleSet == AsnEncodingRules.CER)
                {
                    byte lastByte = valueSpan[bytesWritten - 1];

                    // No unused bits tests 0x01, 1 is 0x02, 2 is 0x04, etc.
                    // We already know that TryCopyBitStringBytes checked that the
                    // declared unused bits were 0, this checks that the last "used" bit
                    // isn't also zero.
                    byte testBit = (byte)(1 << unusedBitCount);

                    if ((lastByte & testBit) == 0)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }
                }

                // Consider a NamedBitList defined as
                //
                //   SomeList ::= BIT STRING {
                //     a(0), b(1), c(2), d(3), e(4), f(5), g(6), h(7), i(8), j(9), k(10)
                //   }
                //
                // The BIT STRING encoding of (a | j) is
                //   unusedBitCount = 6,
                //   contents: 0x80 0x40  (0b10000000_01000000)
                //
                // A the C# exposure of this structure we adhere to is
                //
                // [Flags]
                // enum SomeList
                // {
                //     A = 1,
                //     B = 1 << 1,
                //     C = 1 << 2,
                //     ...
                // }
                //
                // Which happens to be exactly backwards from how the bits are encoded, but the complexity
                // only needs to live here.
                return (Enum)Enum.ToObject(tFlagsEnum, InterpretNamedBitListReversed(valueSpan));
            }
            catch
            {
                _data = saveData;
                throw;
            }
        }

        private static long InterpretNamedBitListReversed(ReadOnlySpan<byte> valueSpan)
        {
            Debug.Assert(valueSpan.Length <= sizeof(long));

            long accum = 0;
            long currentBitValue = 1;

            for (int byteIdx = 0; byteIdx < valueSpan.Length; byteIdx++)
            {
                byte byteVal = valueSpan[byteIdx];

                for (int bitIndex = 7; bitIndex >= 0; bitIndex--)
                {
                    int test = 1 << bitIndex;

                    if ((byteVal & test) != 0)
                    {
                        accum |= currentBitValue;
                    }

                    currentBitValue <<= 1;
                }
            }

            return accum;
        }

        public ReadOnlyMemory<byte> GetEnumeratedBytes() =>
            GetEnumeratedBytes(Asn1Tag.Enumerated);

        public ReadOnlyMemory<byte> GetEnumeratedBytes(Asn1Tag expectedTag)
        {
            // T-REC-X.690-201508 sec 8.4 says the contents are the same as for integers.
            ReadOnlyMemory<byte> contents =
                GetIntegerContents(expectedTag, UniversalTagNumber.Enumerated, out int headerLength);

            _data = _data.Slice(headerLength + contents.Length);
            return contents;
        }

        public TEnum GetEnumeratedValue<TEnum>() where TEnum : struct
        {
            Type tEnum = typeof(TEnum);

            return (TEnum)Enum.ToObject(tEnum, GetEnumeratedValue(tEnum));
        }

        public TEnum GetEnumeratedValue<TEnum>(Asn1Tag expectedTag) where TEnum : struct
        {
            Type tEnum = typeof(TEnum);

            return (TEnum)Enum.ToObject(tEnum, GetEnumeratedValue(expectedTag, tEnum));
        }

        public Enum GetEnumeratedValue(Type tEnum) =>
            GetEnumeratedValue(Asn1Tag.Enumerated, tEnum);

        public Enum GetEnumeratedValue(Asn1Tag expectedTag, Type tEnum)
        {
            const UniversalTagNumber tagNumber = UniversalTagNumber.Enumerated;
            
            // This will throw an ArgumentException if TEnum isn't an enum type,
            // so we don't need to validate it.
            Type backingType = tEnum.GetEnumUnderlyingType();

            if (tEnum.IsDefined(typeof(FlagsAttribute), false))
            {
                throw new ArgumentException(
                    SR.Cryptography_Asn_EnumeratedValueRequiresNonFlagsEnum,
                    nameof(tEnum));
            }

            // T-REC-X.690-201508 sec 8.4 says the contents are the same as for integers.
            int sizeLimit = Marshal.SizeOf(backingType);

            if (backingType == typeof(int) ||
                backingType == typeof(long) ||
                backingType == typeof(short) ||
                backingType == typeof(sbyte))
            {
                if (!TryReadSignedInteger(sizeLimit, expectedTag, tagNumber, out long value))
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                return (Enum)Enum.ToObject(tEnum, value);
            }

            if (backingType == typeof(uint) ||
                backingType == typeof(ulong) ||
                backingType == typeof(ushort) ||
                backingType == typeof(byte))
            {
                if (!TryReadUnsignedInteger(sizeLimit, expectedTag, tagNumber, out ulong value))
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                return (Enum)Enum.ToObject(tEnum, value);
            }

            Debug.Fail($"No handler for type {backingType.Name}");
            throw new CryptographicException();
        }

        private bool TryGetPrimitiveOctetStringBytes(
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
                if (_ruleSet == AsnEncodingRules.DER)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                contents = default(ReadOnlyMemory<byte>);
                return false;
            }

            Debug.Assert(contentLength.HasValue);
            ReadOnlyMemory<byte> encodedValue = Slice(_data, headerLength, contentLength.Value);

            if (_ruleSet == AsnEncodingRules.CER && encodedValue.Length > MaxCERSegmentSize)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            contents = encodedValue;
            return true;
        }

        private bool TryGetPrimitiveOctetStringBytes(
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            out ReadOnlyMemory<byte> contents)
        {
            if (TryGetPrimitiveOctetStringBytes(expectedTag, out _, out _, out int headerLength, out contents, universalTagNumber))
            {
                _data = _data.Slice(headerLength + contents.Length);
                return true;
            }

            return false;
        }

        public bool TryGetPrimitiveOctetStringBytes(out ReadOnlyMemory<byte> contents) =>
            TryGetPrimitiveOctetStringBytes(Asn1Tag.PrimitiveOctetString, out contents);

        /// <summary>
        /// Gets the contents for an octet string under a primitive encoding.
        /// </summary>
        /// <param name="expectedTag">The expected tag value</param>
        /// <param name="contents">The contents for the octet string.</param>
        /// <returns>
        ///   <c>true</c> if the octet string uses a primitive encoding, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///  <ul>
        ///   <li>No data remains</li>
        ///   <li>The tag read did not match the expected tag</li>
        ///   <li>The length is invalid under the chosen encoding rules</li>
        ///   <li>A CER encoding was chosen and the primitive content length exceeds the maximum allowed</li>
        /// </ul>
        /// </exception>
        public bool TryGetPrimitiveOctetStringBytes(Asn1Tag expectedTag, out ReadOnlyMemory<byte> contents)
        {
            return TryGetPrimitiveOctetStringBytes(expectedTag, UniversalTagNumber.OctetString, out contents);
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
            if (_ruleSet == AsnEncodingRules.CER && contentLength <= MaxCERSegmentSize)
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

            AsnReader tmpReader = new AsnReader(source, _ruleSet);
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
                        if (_ruleSet == AsnEncodingRules.CER && lastSegmentLength != MaxCERSegmentSize)
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

                        if (_ruleSet == AsnEncodingRules.CER && lastSegmentLength > MaxCERSegmentSize)
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
                        if (_ruleSet == AsnEncodingRules.CER)
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
                            _ruleSet);

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

        public bool TryCopyOctetStringBytes(
            Span<byte> destination,
            out int bytesWritten)
        {
            return TryCopyOctetStringBytes(
                Asn1Tag.PrimitiveOctetString,
                destination,
                out bytesWritten);
        }

        public bool TryCopyOctetStringBytes(
            Asn1Tag expectedTag,
            Span<byte> destination,
            out int bytesWritten)
        {
            if (TryGetPrimitiveOctetStringBytes(
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

        public void ReadNull() => ReadNull(Asn1Tag.Null);

        public void ReadNull(Asn1Tag expectedTag)
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out int headerLength);
            CheckExpectedTag(tag, expectedTag, UniversalTagNumber.Null);

            // T-REC-X.690-201508 sec 8.8.1
            // T-REC-X.690-201508 sec 8.8.2
            if (tag.IsConstructed || length != 0)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            _data = _data.Slice(headerLength);
        }
        
        private static void ReadSubIdentifier(
            ReadOnlySpan<byte> source,
            out int bytesRead,
            out long? smallValue,
            out BigInteger? largeValue)
        {
            Debug.Assert(source.Length > 0);

            // T-REC-X.690-201508 sec 8.19.2 (last sentence)
            if (source[0] == 0x80)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // First, see how long the segment is
            int end = -1;
            int idx;

            for (idx = 0; idx < source.Length; idx++)
            {
                // If the high bit isn't set this marks the end of the sub-identifier.
                bool endOfIdentifier = (source[idx] & 0x80) == 0;

                if (endOfIdentifier)
                {
                    end = idx;
                    break;
                }
            }

            if (end < 0)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            bytesRead = end + 1;
            long accum = 0;

            // Fast path, 9 or fewer bytes => fits in a signed long.
            // (7 semantic bits per byte * 9 bytes = 63 bits, which leaves the sign bit alone)
            if (bytesRead <= 9)
            {
                for (idx = 0; idx < bytesRead; idx++)
                {
                    byte cur = source[idx];
                    accum <<= 7;
                    accum |= (byte)(cur & 0x7F);
                }

                largeValue = null;
                smallValue = accum;
                return;
            }

            // Slow path, needs temporary storage.

            const int SemanticByteCount = 7;
            const int ContentByteCount = 8;

            // Every 8 content bytes turns into 7 integer bytes, so scale the count appropriately.
            // Add one while we're shrunk to account for the needed padding byte or the len%8 discarded bytes.
            int bytesRequired = ((bytesRead / ContentByteCount) + 1) * SemanticByteCount;
            byte[] tmpBytes = ArrayPool<byte>.Shared.Rent(bytesRequired);
            // Ensure all the bytes are zeroed out for BigInteger's parsing.
            Array.Clear(tmpBytes, 0, tmpBytes.Length);

            Span<byte> writeSpan = tmpBytes;
            Span<byte> accumValueBytes = stackalloc byte[sizeof(long)];
            int nextStop = bytesRead;
            idx = bytesRead - ContentByteCount;

            while (nextStop > 0)
            {
                byte cur = source[idx];

                accum <<= 7;
                accum |= (byte)(cur & 0x7F);

                idx++;

                if (idx >= nextStop)
                {
                    Debug.Assert(idx == nextStop);
                    Debug.Assert(writeSpan.Length >= SemanticByteCount);

                    BinaryPrimitives.WriteInt64LittleEndian(accumValueBytes, accum);
                    Debug.Assert(accumValueBytes[7] == 0);
                    accumValueBytes.Slice(0, SemanticByteCount).CopyTo(writeSpan);
                    writeSpan = writeSpan.Slice(SemanticByteCount);

                    accum = 0;
                    nextStop -= ContentByteCount;
                    idx = Math.Max(0, nextStop - ContentByteCount);
                }
            }

            int bytesWritten = tmpBytes.Length - writeSpan.Length;

            // Verify our bytesRequired calculation. There should be at most 7 padding bytes.
            // If the length % 8 is 7 we'll have 0 padding bytes, but the sign bit is still clear.
            //
            // 8 content bytes had a sign bit problem, so we gave it a second 7-byte block, 7 remain.
            // 7 content bytes got a single block but used and wrote 7 bytes, but only 49 of the 56 bits.
            // 6 content bytes have a padding count of 1.
            // 1 content byte has a padding count of 6.
            // 0 content bytes is illegal, but see 8 for the cycle.
            int paddingByteCount = bytesRequired - bytesWritten;
            Debug.Assert(paddingByteCount >= 0 && paddingByteCount < sizeof(long));

            largeValue = new BigInteger(tmpBytes);
            smallValue = null;

            Array.Clear(tmpBytes, 0, bytesWritten);
            ArrayPool<byte>.Shared.Return(tmpBytes);
        }

        private string ReadObjectIdentifierAsString(Asn1Tag expectedTag, out int totalBytesRead)
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out int headerLength);
            CheckExpectedTag(tag, expectedTag, UniversalTagNumber.ObjectIdentifier);

            // T-REC-X.690-201508 sec 8.19.1
            // T-REC-X.690-201508 sec 8.19.2 says the minimum length is 1
            if (tag.IsConstructed || length < 1)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            ReadOnlyMemory<byte> contentsMemory = Slice(_data, headerLength, length.Value);
            ReadOnlySpan<byte> contents = contentsMemory.Span;

            // Each byte can contribute a 3 digit value and a '.' (e.g. "126."), but usually
            // they convey one digit and a separator.
            //
            // The OID with the most arcs which were found after a 30 minute search is
            // "1.3.6.1.4.1.311.60.2.1.1" (EV cert jurisdiction of incorporation - locality)
            // which has 11 arcs.
            // The longest "known" segment is 16 bytes, a UUID-as-an-arc value.
            // 16 * 11 = 176 bytes for an "extremely long" OID.
            //
            // So pre-allocate the StringBuilder with at most 1020 characters, an input longer than
            // 255 encoded bytes will just have to re-allocate.
            StringBuilder builder = new StringBuilder(((byte)contents.Length) * 4);

            ReadSubIdentifier(contents, out int bytesRead, out long? smallValue, out BigInteger? largeValue);

            // T-REC-X.690-201508 sec 8.19.4
            // The first two subidentifiers (X.Y) are encoded as (X * 40) + Y, because Y is
            // bounded [0, 39] for X in {0, 1}, and only X in {0, 1, 2} are legal.
            // So:
            // * identifier < 40 => X = 0, Y = identifier.
            // * identifier < 80 => X = 1, Y = identifier - 40.
            // * else: X = 2, Y = identifier - 80.
            byte firstArc;

            if (smallValue != null)
            {
                long firstIdentifier = smallValue.Value;

                if (firstIdentifier < 40)
                {
                    firstArc = 0;
                }
                else if (firstIdentifier < 80)
                {
                    firstArc = 1;
                    firstIdentifier -= 40;
                }
                else
                {
                    firstArc = 2;
                    firstIdentifier -= 80;
                }

                builder.Append(firstArc);
                builder.Append('.');
                builder.Append(firstIdentifier);
            }
            else
            {
                Debug.Assert(largeValue != null);
                BigInteger firstIdentifier = largeValue.Value;

                // We're only here because we were bigger than long.MaxValue, so
                // we're definitely on arc 2.
                Debug.Assert(firstIdentifier > long.MaxValue);

                firstArc = 2;
                firstIdentifier -= 80;

                builder.Append(firstArc);
                builder.Append('.');
                builder.Append(firstIdentifier.ToString());
            }

            contents = contents.Slice(bytesRead);

            while (!contents.IsEmpty)
            {
                ReadSubIdentifier(contents, out bytesRead, out smallValue, out largeValue);
                // Exactly one should be non-null.
                Debug.Assert((smallValue == null) != (largeValue == null));

                builder.Append('.');

                if (smallValue != null)
                {
                    builder.Append(smallValue.Value);
                }
                else
                {
                    builder.Append(largeValue.Value.ToString());
                }

                contents = contents.Slice(bytesRead);
            }

            totalBytesRead = headerLength + length.Value;
            return builder.ToString();
        }

        public string ReadObjectIdentifierAsString() =>
            ReadObjectIdentifierAsString(Asn1Tag.ObjectIdentifier);
        
        public string ReadObjectIdentifierAsString(Asn1Tag expectedTag)
        {
            string oidValue = ReadObjectIdentifierAsString(expectedTag, out int bytesRead);

            _data = _data.Slice(bytesRead);

            return oidValue;
        }

        public Oid ReadObjectIdentifier(bool skipFriendlyName = false) =>
            ReadObjectIdentifier(Asn1Tag.ObjectIdentifier, skipFriendlyName);

        public Oid ReadObjectIdentifier(Asn1Tag expectedTag, bool skipFriendlyName=false)
        {
            string oidValue = ReadObjectIdentifierAsString(expectedTag, out int bytesRead);
            Oid oid = skipFriendlyName ? new Oid(oidValue, oidValue) : new Oid(oidValue);

            // Don't slice until the return object has been created.
            _data = _data.Slice(bytesRead);

            return oid;
        }

        // T-REC-X.690-201508 sec 8.23
        private bool TryCopyCharacterStringBytes(
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            Span<byte> destination,
            out int bytesRead,
            out int bytesWritten)
        {
            // T-REC-X.690-201508 sec 8.23.3, all character strings are encoded as octet strings.
            if (TryGetPrimitiveOctetStringBytes(
                expectedTag,
                out Asn1Tag actualTag,
                out int? contentLength,
                out int headerLength,
                out ReadOnlyMemory<byte> contents,
                universalTagNumber))
            {
                bytesWritten = contents.Length;

                if (destination.Length < bytesWritten)
                {
                    bytesWritten = 0;
                    bytesRead = 0;
                    return false;
                }

                contents.Span.CopyTo(destination);
                bytesRead = headerLength + bytesWritten;
                return true;
            }

            Debug.Assert(actualTag.IsConstructed);

            bool copied = TryCopyConstructedOctetStringContents(
                Slice(_data, headerLength, contentLength),
                destination,
                contentLength == null,
                out int contentBytesRead,
                out bytesWritten);

            if (copied)
            {
                bytesRead = headerLength + contentBytesRead;
            }
            else
            {
                bytesRead = 0;
            }

            return copied;
        }

        private static unsafe bool TryCopyCharacterString(
            ReadOnlySpan<byte> source,
            Span<char> destination,
            Text.Encoding encoding,
            out int charsWritten)
        {
            if (source.Length == 0)
            {
                charsWritten = 0;
                return true;
            }

            fixed (byte* bytePtr = &MemoryMarshal.GetReference(source))
            fixed (char* charPtr = &MemoryMarshal.GetReference(destination))
            {
                try
                {
                    int charCount = encoding.GetCharCount(bytePtr, source.Length);

                    if (charCount > destination.Length)
                    {
                        charsWritten = 0;
                        return false;
                    }

                    charsWritten = encoding.GetChars(bytePtr, source.Length, charPtr, destination.Length);
                    Debug.Assert(charCount == charsWritten);
                }
                catch (DecoderFallbackException e)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                }

                return true;
            }
        }

        private string GetCharacterString(
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            Text.Encoding encoding)
        {
            byte[] rented = null;

            // T-REC-X.690-201508 sec 8.23.3, all character strings are encoded as octet strings.
            ReadOnlySpan<byte> contents = GetOctetStringContents(
                expectedTag,
                universalTagNumber,
                out int bytesRead,
                ref rented);

            try
            {
                string str;

                if (contents.Length == 0)
                {
                    str = string.Empty;
                }
                else
                {
                    unsafe
                    {
                        fixed (byte* bytePtr = &MemoryMarshal.GetReference(contents))
                        {
                            try
                            {
                                str = encoding.GetString(bytePtr, contents.Length);
                            }
                            catch (DecoderFallbackException e)
                            {
                                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
                            }
                        }
                    }
                }

                _data = _data.Slice(bytesRead);
                return str;
            }
            finally
            {
                if (rented != null)
                {
                    Array.Clear(rented, 0, contents.Length);
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        private bool TryCopyCharacterString(
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            Text.Encoding encoding,
            Span<char> destination,
            out int charsWritten)
        {
            byte[] rented = null;

            // T-REC-X.690-201508 sec 8.23.3, all character strings are encoded as octet strings.
            ReadOnlySpan<byte> contents = GetOctetStringContents(
                expectedTag,
                universalTagNumber,
                out int bytesRead,
                ref rented);

            try
            {
                bool copied = TryCopyCharacterString(
                    contents,
                    destination,
                    encoding,
                    out charsWritten);

                if (copied)
                {
                    _data = _data.Slice(bytesRead);
                }

                return copied;
            }
            finally
            {
                if (rented != null)
                {
                    Array.Clear(rented, 0, contents.Length);
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
        }

        /// <summary>
        /// Gets the source data for a character string under a primitive encoding and tagged as
        /// the universal class tag for the encoding type.
        /// </summary>
        /// <param name="encodingType">The UniversalTagNumber for the string encoding type.</param>
        /// <param name="contents">The content bytes for the UTF8String payload.</param>
        /// <returns>
        ///   <c>true</c> if the character string uses a primitive encoding, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///  <ul>
        ///   <li>No data remains</li>
        ///   <li>The tag read does not match the expected tag</li>
        ///   <li>The length is invalid under the chosen encoding rules</li>
        ///   <li>A CER encoding was chosen and the primitive content length exceeds the maximum allowed</li>
        /// </ul>
        /// </exception>
        public bool TryGetPrimitiveCharacterStringBytes(UniversalTagNumber encodingType, out ReadOnlyMemory<byte> contents)
        {
            return TryGetPrimitiveCharacterStringBytes(new Asn1Tag(encodingType), encodingType, out contents);
        }

        /// <summary>
        /// Gets the uninterpreted contents for a character string under a primitive encoding.
        /// The contents are not validated as belonging to the requested encoding type.
        /// </summary>
        /// <param name="expectedTag">The expected tag</param>
        /// <param name="encodingType">The UniversalTagNumber for the string encoding type.</param>
        /// <param name="contents">The contents for the character string.</param>
        /// <returns>
        ///   <c>true</c> if the character string uses a primitive encoding, <c>false</c> otherwise.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///  <ul>
        ///   <li>No data remains</li>
        ///   <li>The tag read does not match the expected tag</li>
        ///   <li>The length is invalid under the chosen encoding rules</li>
        ///   <li>A CER encoding was chosen and the primitive content length exceeds the maximum allowed</li>
        /// </ul>
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="encodingType"/> is not a known character string encoding type.
        /// </exception>
        public bool TryGetPrimitiveCharacterStringBytes(
            Asn1Tag expectedTag,
            UniversalTagNumber encodingType,
            out ReadOnlyMemory<byte> contents)
        {
            CheckCharacterStringEncodingType(encodingType);

            // T-REC-X.690-201508 sec 8.23.3, all character strings are encoded as octet strings.
            return TryGetPrimitiveOctetStringBytes(expectedTag, encodingType, out contents);
        }

        public bool TryCopyCharacterStringBytes(
            UniversalTagNumber encodingType,
            Span<byte> destination,
            out int bytesWritten)
        {
            return TryCopyCharacterStringBytes(
                new Asn1Tag(encodingType),
                encodingType,
                destination,
                out bytesWritten);
        }

        public bool TryCopyCharacterStringBytes(
            Asn1Tag expectedTag,
            UniversalTagNumber encodingType,
            Span<byte> destination,
            out int bytesWritten)
        {
            CheckCharacterStringEncodingType(encodingType);

            bool copied = TryCopyCharacterStringBytes(
                expectedTag,
                encodingType,
                destination,
                out int bytesRead,
                out bytesWritten);

            if (copied)
            {
                _data = _data.Slice(bytesRead);
            }

            return copied;
        }

        public bool TryCopyCharacterString(
            UniversalTagNumber encodingType,
            Span<char> destination,
            out int charsWritten)
        {
            return TryCopyCharacterString(
                new Asn1Tag(encodingType),
                encodingType,
                destination,
                out charsWritten);
        }

        public bool TryCopyCharacterString(
            Asn1Tag expectedTag,
            UniversalTagNumber encodingType,
            Span<char> destination,
            out int charsWritten)
        {
            Text.Encoding encoding = AsnCharacterStringEncodings.GetEncoding(encodingType);
            return TryCopyCharacterString(expectedTag, encodingType, encoding, destination, out charsWritten);
        }

        public string GetCharacterString(UniversalTagNumber encodingType) =>
            GetCharacterString(new Asn1Tag(encodingType), encodingType);
            
        public string GetCharacterString(Asn1Tag expectedTag, UniversalTagNumber encodingType)
        {
            Text.Encoding encoding = AsnCharacterStringEncodings.GetEncoding(encodingType);
            return GetCharacterString(expectedTag, encodingType, encoding);
        }

        public AsnReader ReadSequence() => ReadSequence(Asn1Tag.Sequence);

        public AsnReader ReadSequence(Asn1Tag expectedTag)
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out int headerLength);
            CheckExpectedTag(tag, expectedTag, UniversalTagNumber.Sequence);

            // T-REC-X.690-201508 sec 8.9.1
            // T-REC-X.690-201508 sec 8.10.1
            if (!tag.IsConstructed)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            int suffix = 0;

            if (length == null)
            {
                length = SeekEndOfContents(_data.Slice(headerLength));
                suffix = EndOfContentsEncodedLength;
            }

            ReadOnlyMemory<byte> contents = Slice(_data, headerLength, length.Value);

            _data = _data.Slice(headerLength + contents.Length + suffix);
            return new AsnReader(contents, _ruleSet);
        }

        /// <summary>
        /// Builds a new AsnReader over the bytes bounded by the current position which
        /// corresponds to an ASN.1 SET OF value, validating the CER or DER sort ordering
        /// unless suppressed.
        /// </summary>
        /// <param name="skipSortOrderValidation">
        ///   <c>false</c> to validate the sort ordering of the contents, <c>true</c> to
        ///   allow reading the data without verifying it was properly sorted by the writer.
        /// </param>
        /// <returns>An AsnReader over the current position, bounded by the contained length value.</returns>
        public AsnReader ReadSetOf(bool skipSortOrderValidation = false) =>
            ReadSetOf(Asn1Tag.SetOf, skipSortOrderValidation);

        public AsnReader ReadSetOf(Asn1Tag expectedTag, bool skipSortOrderValidation = false)
        {
            Asn1Tag tag = ReadTagAndLength(out int? length, out int headerLength);
            CheckExpectedTag(tag, expectedTag, UniversalTagNumber.SetOf);

            // T-REC-X.690-201508 sec 8.12.1
            if (!tag.IsConstructed)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            int suffix = 0;

            if (length == null)
            {
                length = SeekEndOfContents(_data.Slice(headerLength));
                suffix = EndOfContentsEncodedLength;
            }

            ReadOnlyMemory<byte> contents = Slice(_data, headerLength, length.Value);

            if (!skipSortOrderValidation)
            {
                // T-REC-X.690-201508 sec 11.6
                // BER data is not required to be sorted.
                if (_ruleSet == AsnEncodingRules.DER ||
                    _ruleSet == AsnEncodingRules.CER)
                {
                    AsnReader reader = new AsnReader(contents, _ruleSet);
                    ReadOnlyMemory<byte> current = ReadOnlyMemory<byte>.Empty;
                    SetOfValueComparer comparer = SetOfValueComparer.Instance;

                    while (reader.HasData)
                    {
                        ReadOnlyMemory<byte> previous = current;
                        current = reader.GetEncodedValue();

                        if (comparer.Compare(current, previous) < 0)
                        {
                            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                        }
                    }
                }
            }

            _data = _data.Slice(headerLength + contents.Length + suffix);
            return new AsnReader(contents, _ruleSet);
        }

        private static int ParseNonNegativeIntAndSlice(ref ReadOnlySpan<byte> data, int bytesToRead)
        {
            int value = ParseNonNegativeInt(Slice(data, 0, bytesToRead));
            data = data.Slice(bytesToRead);

            return value;
        }

        private static int ParseNonNegativeInt(ReadOnlySpan<byte> data)
        {
            if (Utf8Parser.TryParse(data, out uint value, out int consumed) && value <= int.MaxValue && consumed == data.Length)
            {
                return (int)value;
            }

            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
        }

        private DateTimeOffset ParseUtcTime(ReadOnlySpan<byte> contentOctets, int twoDigitYearMax)
        {
            // The full allowed formats (T-REC-X.680-201510 sec 47.3)
            // a) YYMMDD
            // b1) hhmm
            // b2) hhmmss
            // c1) Z
            // c2) {+|-}hhmm
            //
            // YYMMDDhhmmZ  (a, b1, c1)
            // YYMMDDhhmm+hhmm (a, b1, c2+)
            // YYMMDDhhmm-hhmm (a, b1, c2-)
            // YYMMDDhhmmssZ (a, b2, c1)
            // YYMMDDhhmmss+hhmm (a, b2, c2+)
            // YYMMDDhhmmss-hhmm (a, b2, c2-)

            const int NoSecondsZulu = 11;
            const int NoSecondsOffset = 15;
            const int HasSecondsZulu = 13;
            const int HasSecondsOffset = 17;

            // T-REC-X.690-201510 sec 11.8
            if (_ruleSet == AsnEncodingRules.DER || _ruleSet == AsnEncodingRules.CER)
            {
                if (contentOctets.Length != HasSecondsZulu)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }

            // 11, 13, 15, 17 are legal.
            // Range check + odd.
            if (contentOctets.Length < NoSecondsZulu ||
                contentOctets.Length > HasSecondsOffset ||
                (contentOctets.Length & 1) != 1)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            ReadOnlySpan<byte> contents = contentOctets;

            int year = ParseNonNegativeIntAndSlice(ref contents, 2);
            int month = ParseNonNegativeIntAndSlice(ref contents, 2);
            int day = ParseNonNegativeIntAndSlice(ref contents, 2);
            int hour = ParseNonNegativeIntAndSlice(ref contents, 2);
            int minute = ParseNonNegativeIntAndSlice(ref contents, 2);
            int second = 0;
            int offsetHour = 0;
            int offsetMinute = 0;
            bool minus = false;

            if (contentOctets.Length == HasSecondsOffset ||
                contentOctets.Length == HasSecondsZulu)
            {
                second = ParseNonNegativeIntAndSlice(ref contents, 2);
            }

            if (contentOctets.Length == NoSecondsZulu ||
                contentOctets.Length == HasSecondsZulu)
            {
                if (contents[0] != 'Z')
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }
            else
            {
                Debug.Assert(
                    contentOctets.Length == NoSecondsOffset ||
                    contentOctets.Length == HasSecondsOffset);

                if (contents[0] == '-')
                {
                    minus = true;
                }
                else if (contents[0] != '+')
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                contents = contents.Slice(1);
                offsetHour = ParseNonNegativeIntAndSlice(ref contents, 2);
                offsetMinute = ParseNonNegativeIntAndSlice(ref contents, 2);
                Debug.Assert(contents.IsEmpty);
            }

            // ISO 8601:2004 4.2.1 restricts a "minute" value to [00,59].
            // The "hour" value is effectively bound to [00,23] by the same section, but
            // is bound to [00,14] by DateTimeOffset, so no additional check is required here.
            if (offsetMinute > 59)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            TimeSpan offset = new TimeSpan(offsetHour, offsetMinute, 0);

            if (minus)
            {
                offset = -offset;
            }

            // Apply the twoDigitYearMax value.
            // Example: year=50, TDYM=2049
            //  century = 20
            //  year > 49 => century = 19
            //  scaledYear = 1900 + 50 = 1950
            //
            // Example: year=49, TDYM=2049
            //  century = 20
            //  year is not > 49 => century = 20
            //  scaledYear = 2000 + 49 = 2049
            int century = twoDigitYearMax / 100;

            if (year > twoDigitYearMax % 100)
            {
                century--;
            }

            int scaledYear = century * 100 + year;

            try
            {
                return new DateTimeOffset(scaledYear, month, day, hour, minute, second, offset);
            }
            catch (Exception e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        public DateTimeOffset GetUtcTime(int twoDigitYearMax = 2049) =>
            GetUtcTime(Asn1Tag.UtcTime, twoDigitYearMax);

        /// <summary>
        /// Gets the DateTimeOffset represented by a UTCTime value.
        /// </summary>
        /// <param name="expectedTag">The expected tag</param>
        /// <param name="twoDigitYearMax">
        /// The largest year to represent with this value.
        /// The default value, 2049, represents the 1950-2049 range for X.509 certificates.
        /// </param>
        /// <returns>
        /// A DateTimeOffset representing the value encoded in the UTCTime.
        /// </returns>
        /// <seealso cref="System.Globalization.Calendar.TwoDigitYearMax"/>
        public DateTimeOffset GetUtcTime(Asn1Tag expectedTag, int twoDigitYearMax = 2049)
        {
            // T-REC-X.680-201510 sec 47.3 says it is IMPLICIT VisibleString, which means
            // that BER is allowed to do complex constructed forms.

            // The full allowed formats (T-REC-X.680-201510 sec 47.3)
            // YYMMDDhhmmZ  (a, b1, c1)
            // YYMMDDhhmm+hhmm (a, b1, c2+)
            // YYMMDDhhmm-hhmm (a, b1, c2-)
            // YYMMDDhhmmssZ (a, b2, c1)
            // YYMMDDhhmmss+hhmm (a, b2, c2+)
            // YYMMDDhhmmss-hhmm (a, b2, c2-)

            // CER and DER are restricted to YYMMDDhhmmssZ
            // T-REC-X.690-201510 sec 11.8

            byte[] rented = null;
            // The longest format is 17 bytes.
            Span<byte> tmpSpace = stackalloc byte[17];

            ReadOnlySpan<byte> contents = GetOctetStringContents(
                expectedTag,
                UniversalTagNumber.UtcTime,
                out int bytesRead,
                ref rented,
                tmpSpace);

            DateTimeOffset value = ParseUtcTime(contents, twoDigitYearMax);

            if (rented != null)
            {
                Debug.Fail($"UtcTime did not fit in tmpSpace ({contents.Length} total)");
                Array.Clear(rented, 0, contents.Length);
                ArrayPool<byte>.Shared.Return(rented);
            }

            _data = _data.Slice(bytesRead);
            return value;
        }

        private static DateTimeOffset ParseGeneralizedTime(
            AsnEncodingRules ruleSet,
            ReadOnlySpan<byte> contentOctets,
            bool disallowFractions)
        {
            // T-REC-X.680-201510 sec 46 defines a lot of formats for GeneralizedTime.
            //
            // All formats start with yyyyMMdd.
            //
            // "Local time" formats are
            //   [date]HH.fractionOfAnHourToAnArbitraryPrecision
            //   [date]HHmm.fractionOfAMinuteToAnArbitraryPrecision
            //   [date]HHmmss.fractionOfASecondToAnArbitraryPrecision
            //
            // "UTC time" formats are the local formats suffixed with 'Z'
            //
            // "UTC offset time" formats are the local formats suffixed with
            //  +HH
            //  +HHmm
            //  -HH
            //  -HHmm
            //
            // Since T-REC-X.680-201510 46.3(a)(1) and 46.3(a)(2) both specify the ISO 8601:2004
            // Basic format, we shall presume that 46.3(a)(3) also meant only the Basic format,
            // and therefore [+/-]HH:mm (with the colon) are prohibited. (based on ISO 8601:201x-DIS)
            
            // Since DateTimeOffset doesn't have a notion of
            // "I'm a local time, but with an unknown offset", the computer's current offset will
            // be used.

            // T-REC-X.690-201510 sec 11.7 binds CER and DER to a much smaller set of inputs:
            //  * Only the UTC/Z format can be used.
            //  * HHmmss must always be used
            //  * If fractions are present they will be separated by period, never comma.
            //  * If fractions are present the last digit mustn't be 0.

            bool strict = ruleSet == AsnEncodingRules.DER || ruleSet == AsnEncodingRules.CER;
            if (strict && contentOctets.Length < 15)
            {
                // yyyyMMddHHmmssZ
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
            else if (contentOctets.Length < 10)
            {
                // yyyyMMddHH
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            ReadOnlySpan<byte> contents = contentOctets;

            int year = ParseNonNegativeIntAndSlice(ref contents, 4);
            int month = ParseNonNegativeIntAndSlice(ref contents, 2);
            int day = ParseNonNegativeIntAndSlice(ref contents, 2);
            int hour = ParseNonNegativeIntAndSlice(ref contents, 2);
            int? minute = null;
            int? second = null;
            ulong fraction = 0;
            ulong fractionScale = 1;
            byte lastFracDigit = 0xFF;
            TimeSpan? timeOffset = null;
            bool isZulu = false;

            const byte HmsState = 0;
            const byte FracState = 1;
            const byte SuffixState = 2;
            byte state = HmsState;

            byte? GetNextState(byte octet)
            {
                if (octet == 'Z' || octet == '-' || octet == '+')
                {
                    return SuffixState;
                }

                if (octet == '.' || octet == ',')
                {
                    return FracState;
                }

                return null;
            }

            // This while loop could be rewritten to include the FracState and Suffix
            // processing steps.  But since there's a forward flow to the state machine
            // the loop body then needs to account for that.
            while (state == HmsState && contents.Length != 0)
            {
                byte? nextState = GetNextState(contents[0]);

                if (nextState == null)
                {
                    if (minute == null)
                    {
                        minute = ParseNonNegativeIntAndSlice(ref contents, 2);
                    }
                    else if (second == null)
                    {
                        second = ParseNonNegativeIntAndSlice(ref contents, 2);
                    }
                    else
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }
                }
                else
                {
                    state = nextState.Value;
                }
            }

            if (state == FracState)
            {
                if (disallowFractions)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                Debug.Assert(!contents.IsEmpty);
                byte octet = contents[0];
                Debug.Assert(state == GetNextState(octet));

                if (octet == '.')
                {
                    // Always valid
                }
                else if (octet == ',')
                {
                    // Valid for BER, but not CER or DER.
                    // T-REC-X.690-201510 sec 11.7.4
                    if (strict)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }
                }
                else
                {
                    Debug.Fail($"Unhandled value '{octet:X2}' in {nameof(FracState)}");
                    throw new CryptographicException();
                }

                contents = contents.Slice(1);

                if (contents.IsEmpty)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                // There are 36,000,000,000 ticks per hour, and hour is our largest scale.
                // In case the double -> Ticks conversion allows for rounding up we can allow
                // for a 12th digit.
                
                if (!Utf8Parser.TryParse(SliceAtMost(contents, 12), out fraction, out int fracLength) || fracLength == 0)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                lastFracDigit = (byte)(fraction % 10);

                for (int i = 0; i < fracLength; i++)
                {
                    fractionScale *= 10;
                }

                contents = contents.Slice(fracLength);

                // Drain off any remaining digits.
                // The unsigned parsers will not accept + or - as a leading character, so
                // they won't eat timezone suffix.
                // But Utf8Parser.TryParse reports false on overflow, so limit it to 9 digits at a time.
                while (Utf8Parser.TryParse(SliceAtMost(contents, 9), out uint nonSemantic, out fracLength))
                {
                    contents = contents.Slice(fracLength);
                    lastFracDigit = (byte)(nonSemantic % 10);
                }

                if (contents.Length != 0)
                {
                    byte? nextState = GetNextState(contents[0]);

                    if (nextState == null)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    // If this produces FracState we'll finish with a non-empty contents, and still throw.
                    state = nextState.Value;
                }
            }

            if (state == SuffixState)
            {
                Debug.Assert(!contents.IsEmpty);
                byte octet = contents[0];
                Debug.Assert(state == GetNextState(octet));
                contents = contents.Slice(1);

                if (octet == 'Z')
                {
                    timeOffset = TimeSpan.Zero;
                    isZulu = true;
                }
                else
                {
                    bool isMinus;

                    if (octet == '+')
                    {
                        isMinus = false;
                    }
                    else if (octet == '-')
                    {
                        isMinus = true;
                    }
                    else
                    {
                        Debug.Fail($"Unhandled value '{octet:X2}' in {nameof(SuffixState)}");
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    if (contents.IsEmpty)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    int offsetHour = ParseNonNegativeIntAndSlice(ref contents, 2);
                    int offsetMinute = 0;

                    if (contents.Length != 0)
                    {
                        offsetMinute = ParseNonNegativeIntAndSlice(ref contents, 2);
                    }

                    // ISO 8601:2004 4.2.1 restricts a "minute" value to [00,59].
                    // The "hour" value is effectively bound to [00,23] by the same section, but
                    // is bound to [00,14] by DateTimeOffset, so no additional check is required here.
                    if (offsetMinute > 59)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                    }

                    TimeSpan tmp = new TimeSpan(offsetHour, offsetMinute, 0);

                    if (isMinus)
                    {
                        tmp = -tmp;
                    }

                    timeOffset = tmp;
                }
            }

            // Was there data after a suffix, or fracstate went re-entrant?
            if (!contents.IsEmpty)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // T-REC-X.690-201510 sec 11.7
            if (strict)
            {
                if (!isZulu || !second.HasValue)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }

                if (lastFracDigit == 0)
                {
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
                }
            }

            double frac = (double)fraction / fractionScale;
            TimeSpan fractionSpan = TimeSpan.Zero;

            if (!minute.HasValue)
            {
                minute = 0;
                second = 0;

                if (fraction != 0)
                {
                    // No minutes means this is fractions of an hour
                    fractionSpan = new TimeSpan((long)(frac * TimeSpan.TicksPerHour));
                }
            }
            else if (!second.HasValue)
            {
                second = 0;

                if (fraction != 0)
                {
                    // No seconds means this is fractions of a minute
                    fractionSpan = new TimeSpan((long)(frac * TimeSpan.TicksPerMinute));
                }
            }
            else if (fraction != 0)
            {
                // Both minutes and seconds means fractions of a second.
                fractionSpan = new TimeSpan((long)(frac * TimeSpan.TicksPerSecond));
            }
            
            DateTimeOffset value;

            try
            {
                if (timeOffset == null)
                {
                    // Use the local timezone offset since there's no information in the contents.
                    // T-REC-X.680-201510 sec 46.2(a).
                    value = new DateTimeOffset(new DateTime(year, month, day, hour, minute.Value, second.Value));
                }
                else
                {
                    // T-REC-X.680-201510 sec 46.2(b) or 46.2(c).
                    value = new DateTimeOffset(year, month, day, hour, minute.Value, second.Value, timeOffset.Value);
                }

                value += fractionSpan;
                return value;
            }
            catch (Exception e)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, e);
            }
        }

        public DateTimeOffset GetGeneralizedTime(bool disallowFractions=false) =>
            GetGeneralizedTime(Asn1Tag.GeneralizedTime, disallowFractions);

        public DateTimeOffset GetGeneralizedTime(Asn1Tag expectedTag, bool disallowFractions=false)
        {
            byte[] rented = null;

            ReadOnlySpan<byte> contents = GetOctetStringContents(
                expectedTag,
                UniversalTagNumber.GeneralizedTime,
                out int bytesRead,
                ref rented);

            DateTimeOffset value = ParseGeneralizedTime(_ruleSet, contents, disallowFractions);

            if (rented != null)
            {
                Array.Clear(rented, 0, contents.Length);
                ArrayPool<byte>.Shared.Return(rented);
            }

            _data = _data.Slice(bytesRead);
            return value;
        }

        private ReadOnlySpan<byte> GetOctetStringContents(
            Asn1Tag expectedTag,
            UniversalTagNumber universalTagNumber,
            out int bytesRead,
            ref byte[] rented,
            Span<byte> tmpSpace = default)
        {
            Debug.Assert(rented == null);

            if (TryGetPrimitiveOctetStringBytes(
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
                rented = ArrayPool<byte>.Shared.Rent(octetStringLength);
                tmpSpace = rented;
            }

            CopyConstructedOctetString(source, tmpSpace, isIndefinite, out int localBytesRead, out int bytesWritten);
            Debug.Assert(bytesWritten == octetStringLength);

            bytesRead = headerLength + localBytesRead;
            return tmpSpace.Slice(0, bytesWritten);
        }

        private static ReadOnlySpan<byte> SliceAtMost(ReadOnlySpan<byte> source, int longestPermitted)
        {
            int len = Math.Min(longestPermitted, source.Length);
            return source.Slice(0, len);
        }

        private static ReadOnlySpan<byte> Slice(ReadOnlySpan<byte> source, int offset, int length)
        {
            Debug.Assert(offset >= 0);

            if (length < 0 || source.Length - offset < length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return source.Slice(offset, length);
        }

        private static ReadOnlyMemory<byte> Slice(ReadOnlyMemory<byte> source, int offset, int? length)
        {
            Debug.Assert(offset >= 0);

            if (length == null)
            {
                return source.Slice(offset);
            }

            int lengthVal = length.Value;

            if (lengthVal < 0 || source.Length - offset < lengthVal)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return source.Slice(offset, lengthVal);
        }

        private static void CheckEncodingRules(AsnEncodingRules ruleSet)
        {
            if (ruleSet != AsnEncodingRules.BER &&
                ruleSet != AsnEncodingRules.CER &&
                ruleSet != AsnEncodingRules.DER)
            {
                throw new ArgumentOutOfRangeException(nameof(ruleSet));
            }
        }

        private static void CheckExpectedTag(Asn1Tag tag, Asn1Tag expectedTag, UniversalTagNumber tagNumber)
        {
            if (expectedTag.TagClass == TagClass.Universal && expectedTag.TagValue != (int)tagNumber)
            {
                throw new ArgumentException(
                    SR.Cryptography_Asn_UniversalValueIsFixed,
                    nameof(expectedTag));
            }

            if (expectedTag.TagClass != tag.TagClass || expectedTag.TagValue != tag.TagValue)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

        private static void CheckCharacterStringEncodingType(UniversalTagNumber encodingType)
        {
            // T-REC-X.680-201508 sec 41
            switch (encodingType)
            {
                case UniversalTagNumber.BMPString:
                case UniversalTagNumber.GeneralString:
                case UniversalTagNumber.GraphicString:
                case UniversalTagNumber.IA5String:
                case UniversalTagNumber.ISO646String:
                case UniversalTagNumber.NumericString:
                case UniversalTagNumber.PrintableString:
                case UniversalTagNumber.TeletexString:
                // T61String is an alias for TeletexString (already listed)
                case UniversalTagNumber.UniversalString:
                case UniversalTagNumber.UTF8String:
                case UniversalTagNumber.VideotexString:
                    // VisibleString is an alias for ISO646String (already listed)
                    return;
            }

            throw new ArgumentOutOfRangeException(nameof(encodingType));
        }
    }
}

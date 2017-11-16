// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Threading;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Reads data encoded via the Distinguished Encoding Rules for Abstract
    /// Syntax Notation 1 (ASN.1) data.
    /// </summary>
    internal class DerSequenceReader
    {
        internal const byte ContextSpecificTagFlag = 0x80;
        internal const byte ConstructedFlag = 0x20;
        internal const byte ContextSpecificConstructedTag0 = ContextSpecificTagFlag | ConstructedFlag;
        internal const byte ContextSpecificConstructedTag1 = ContextSpecificConstructedTag0 | 1;
        internal const byte ContextSpecificConstructedTag2 = ContextSpecificConstructedTag0 | 2;
        internal const byte ContextSpecificConstructedTag3 = ContextSpecificConstructedTag0 | 3;
        internal const byte ConstructedSequence = ConstructedFlag | (byte)DerTag.Sequence;

        // 0b1100_0000
        internal const byte TagClassMask = 0xC0; 
        internal const byte TagNumberMask = 0x1F;

        internal static DateTimeFormatInfo s_validityDateTimeFormatInfo;

        private readonly byte[] _data;
        private readonly int _end;
        private int _position;

        internal int ContentLength { get; private set; }

        private DerSequenceReader(bool startAtPayload, byte[] data, int offset, int length)
        {
            Debug.Assert(startAtPayload, "This overload is only for bypassing the sequence tag");
            Debug.Assert(data != null, "Data is null");
            Debug.Assert(offset >= 0, "Offset is negative");

            _data = data;
            _position = offset;
            _end = offset + length;

            ContentLength = length;
        }

        internal DerSequenceReader(byte[] data)
            : this(data, 0, data.Length)
        {
        }

        internal DerSequenceReader(byte[] data, int offset, int length)
            : this(DerTag.Sequence, data, offset, length)
        {
        }

        private DerSequenceReader(DerTag tagToEat, byte[] data, int offset, int length)
        {
            Debug.Assert(data != null, "Data is null");

            if (offset < 0 || length < 2 || length > data.Length - offset)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            _data = data;
            _end = offset + length;
            _position = offset;
            EatTag(tagToEat);
            int contentLength = EatLength();
            Debug.Assert(_end - contentLength >= _position);
            ContentLength = contentLength;

            // If the sequence reports being smaller than the buffer, shrink the end-of-validity.
            _end = _position + contentLength;
        }

        internal static DerSequenceReader CreateForPayload(byte[] payload)
        {
            return new DerSequenceReader(true, payload, 0, payload.Length);
        }

        internal bool HasData
        {
            get { return _position < _end; }
        }

        internal byte PeekTag()
        {
            if (!HasData)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            byte tag = _data[_position];

            if ((tag & TagNumberMask) == TagNumberMask)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return tag;
        }

        internal bool HasTag(DerTag expectedTag)
        {
            return HasTag((byte)expectedTag);
        }

        internal bool HasTag(byte expectedTag)
        {
            return HasData && _data[_position] == expectedTag;
        }

        internal void SkipValue()
        {
            EatTag((DerTag)PeekTag());
            int contentLength = EatLength();
            _position += contentLength;
        }

        internal void ValidateAndSkipDerValue()
        {
            byte tag = PeekTag();

            // If the tag is in the UNIVERSAL class
            if ((tag & TagClassMask) == 0)
            {
                // Tag 0 is special ("reserved for use by the encoding rules"), but mainly is used
                // as the End-of-Contents marker for the indefinite length encodings, which DER prohibits.
                //
                // Tag 15 is reserved.
                //
                // So either of these are invalid.

                if (tag == 0 || tag == 15)
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

                // DER limits the constructed encoding to SEQUENCE and SET, as well as anything which gets
                // a defined encoding as being an IMPLICIT SEQUENCE.

                bool expectConstructed = false;

                switch (tag & TagNumberMask)
                {
                    case 0x08: // External or Instance-Of
                    case 0x0B: // EmbeddedPDV
                    case (byte)DerTag.Sequence:
                    case (byte)DerTag.Set:
                    case 0x1D: // Unrestricted Character String
                        expectConstructed = true;
                        break;
                }

                bool isConstructed = (tag & ConstructedFlag) == ConstructedFlag;

                if (expectConstructed != isConstructed)
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            EatTag((DerTag)tag);
            int contentLength = EatLength();

            if (contentLength > 0 && (tag & ConstructedFlag) == ConstructedFlag)
            {
                var childReader = new DerSequenceReader(true, _data, _position, _end - _position);

                while (childReader.HasData)
                {
                    childReader.ValidateAndSkipDerValue();
                }
            }

            _position += contentLength;
        }

        /// <summary>
        /// Returns the next value encoded (this includes tag and length)
        /// </summary>
        internal byte[] ReadNextEncodedValue()
        {
            // Check that the tag is legal, but the value isn't relevant.
            PeekTag();

            int lengthLength;
            int contentLength = ScanContentLength(_data, _position + 1, _end, out lengthLength);
            // Length of tag, encoded length, and the content
            int totalLength = 1 + lengthLength + contentLength;
            Debug.Assert(_end - totalLength >= _position);
            
            byte[] encodedValue = new byte[totalLength];
            Buffer.BlockCopy(_data, _position, encodedValue, 0, totalLength);

            _position += totalLength;
            return encodedValue;
        }

        internal bool ReadBoolean()
        {
            EatTag(DerTag.Boolean);

            int length = EatLength();

            if (length != 1)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            bool value = _data[_position] != 0;
            _position += length;
            return value;
        }

        internal int ReadInteger()
        {
            byte[] integerBytes = ReadIntegerBytes();

            // integerBytes is currently Big-Endian, need to reverse it for
            // Little-Endian to pass into BigInteger.
            Array.Reverse(integerBytes);
            BigInteger bigInt = new BigInteger(integerBytes);
            return (int)bigInt;
        }

        internal byte[] ReadIntegerBytes()
        {
            EatTag(DerTag.Integer);

            return ReadContentAsBytes();
        }

        internal byte[] ReadBitString()
        {
            EatTag(DerTag.BitString);

            int contentLength = EatLength();

            if (contentLength < 1)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            byte unusedBits = _data[_position];

            if (unusedBits > 7)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            // skip the "unused bits" byte
            contentLength--;
            _position++;

            byte[] octets = new byte[contentLength];
            Buffer.BlockCopy(_data, _position, octets, 0, contentLength);

            _position += contentLength;
            return octets;
        }

        internal byte[] ReadOctetString()
        {
            EatTag(DerTag.OctetString);

            return ReadContentAsBytes();
        }

        internal string ReadOidAsString()
        {
            EatTag(DerTag.ObjectIdentifier);
            int contentLength = EatLength();

            if (contentLength < 1)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            // Each byte could cause 3 decimal characters to be written, plus a period. Over-allocate
            // and avoid re-alloc.
            StringBuilder builder = new StringBuilder(contentLength * 4);

            // The first byte is ((X * 40) + Y), where X is the first segment and Y the second.
            // ISO/IEC 8825-1:2003 section 8.19.4

            byte firstByte = _data[_position];
            byte first = (byte)(firstByte / 40);
            byte second = (byte)(firstByte % 40);

            builder.Append(first);
            builder.Append('.');
            builder.Append(second);

            // For the rest of the segments, the high bit on the byte is a continuation marker,
            // and data is loaded into a BigInteger 7 bits at a time.
            //
            // When the high bit is 0, the segment ends, so emit a '.' between it and the next one.
            //
            // ISO/IEC 8825-1:2003 section 8.19.2, and the .NET representation of Oid.Value.
            bool needDot = true;
            BigInteger bigInt = new BigInteger(0);

            for (int i = 1; i < contentLength; i++)
            {
                byte current = _data[_position + i];
                byte data = (byte)(current & 0x7F);

                if (needDot)
                {
                    builder.Append('.');
                    needDot = false;
                }

                bigInt <<= 7;
                bigInt += data;

                if (current == data)
                {
                    builder.Append(bigInt);
                    bigInt = 0;
                    needDot = true;
                }
            }

            _position += contentLength;
            return builder.ToString();
        }

        internal Oid ReadOid()
        {
            return new Oid(ReadOidAsString());
        }

        internal string ReadUtf8String()
        {
            EatTag(DerTag.UTF8String);
            int contentLength = EatLength();

            string str = System.Text.Encoding.UTF8.GetString(_data, _position, contentLength);
            _position += contentLength;

            return TrimTrailingNulls(str);
        }

        private DerSequenceReader ReadCollectionWithTag(DerTag expected)
        {
            // DerSequenceReader wants to read its own tag, so don't EatTag here.
            CheckTag(expected, _data, _position);

            int lengthLength;
            int contentLength = ScanContentLength(_data, _position + 1, _end, out lengthLength);
            int totalLength = 1 + lengthLength + contentLength;

            DerSequenceReader reader = new DerSequenceReader(expected, _data, _position, totalLength);
            _position += totalLength;
            return reader;
        }

        internal DerSequenceReader ReadSequence()
        {
            return ReadCollectionWithTag(DerTag.Sequence);
        }

        internal DerSequenceReader ReadSet()
        {
            return ReadCollectionWithTag(DerTag.Set);
        }

        internal string ReadPrintableString()
        {
            EatTag(DerTag.PrintableString);
            int contentLength = EatLength();

            // PrintableString is a subset of ASCII, so just return the ASCII interpretation.
            string str = System.Text.Encoding.ASCII.GetString(_data, _position, contentLength);
            _position += contentLength;

            return TrimTrailingNulls(str);
        }

        internal string ReadIA5String()
        {
            EatTag(DerTag.IA5String);
            int contentLength = EatLength();

            // IA5 (International Alphabet - 5) is functionally equivalent to 7-bit ASCII.

            string ia5String = System.Text.Encoding.ASCII.GetString(_data, _position, contentLength);
            _position += contentLength;

            return TrimTrailingNulls(ia5String);
        }

        internal DateTime ReadX509Date()
        {
            byte tag = PeekTag();

            switch ((DerTag)tag)
            {
                case DerTag.UTCTime:
                    return ReadUtcTime();
                case DerTag.GeneralizedTime:
                    return ReadGeneralizedTime();
            }

            throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
        }

        internal DateTime ReadUtcTime()
        {
            return ReadTime(DerTag.UTCTime, "yyMMddHHmmss'Z'");
        }

        internal DateTime ReadGeneralizedTime()
        {
            // Currently only supports reading times with no fractional seconds or time differentials
            // as RFC 2630 doesn't allow these. In case this is done, the format string has to be parsed
            // to follow rules on X.680 and X.690.
            return ReadTime(DerTag.GeneralizedTime, "yyyyMMddHHmmss'Z'");
        }

        internal string ReadBMPString()
        {
            EatTag(DerTag.BMPString);
            int contentLength = EatLength();

            // BMPString or Basic Multilingual Plane, is equal to UCS-2.
            // And since this is cryptography, it's Big Endian.
            string str = System.Text.Encoding.BigEndianUnicode.GetString(_data, _position, contentLength);
            _position += contentLength;

            return TrimTrailingNulls(str);
        }

        private static string TrimTrailingNulls(string value)
        {
            // .NET's string comparisons start by checking the length, so a trailing
            // NULL character which was literally embedded in the DER would cause a
            // failure in .NET whereas it wouldn't have with strcmp.
            if (value?.Length > 0)
            {
                int newLength = value.Length;

                while (newLength > 0 && value[newLength - 1] == 0)
                {
                    newLength--;
                }

                if (newLength != value.Length)
                {
                    return value.Substring(0, newLength);
                }
            }

            return value;
        }

        private DateTime ReadTime(DerTag timeTag, string formatString)
        {
            EatTag(timeTag);
            int contentLength = EatLength();

            string decodedTime = System.Text.Encoding.ASCII.GetString(_data, _position, contentLength);
            _position += contentLength;

            Debug.Assert(
                decodedTime[decodedTime.Length - 1] == 'Z',
                $"The date doesn't follow the X.690 format, ending with {decodedTime[decodedTime.Length - 1]}");

            DateTime time;

            DateTimeFormatInfo fi = LazyInitializer.EnsureInitialized(
                ref s_validityDateTimeFormatInfo,
                () =>
                {
                    var clone = (DateTimeFormatInfo)CultureInfo.InvariantCulture.DateTimeFormat.Clone();
                    clone.Calendar.TwoDigitYearMax = 2049;

                    return clone;
                });

            if (!DateTime.TryParseExact(
                    decodedTime,
                    formatString,
                    fi,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out time))
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            return time;
        }

        private byte[] ReadContentAsBytes()
        {
            int contentLength = EatLength();

            byte[] octets = new byte[contentLength];
            Buffer.BlockCopy(_data, _position, octets, 0, contentLength);

            _position += contentLength;
            return octets;
        }

        private void EatTag(DerTag expected)
        {
            if (!HasData)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            CheckTag(expected, _data, _position);
            _position++;
        }

        private static void CheckTag(DerTag expected, byte[] data, int position)
        {
            if (position >= data.Length)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            byte actual = data[position];
            byte relevant = (byte)(actual & TagNumberMask);

            // Multi-byte tags are not supported by this implementation.
            if (relevant == TagNumberMask)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // Context-specific datatypes cannot be tag-verified
            if ((actual & ContextSpecificTagFlag) != 0)
            {
                return;
            }

            byte expectedByte = (byte)((byte)expected & TagNumberMask);

            if (expectedByte != relevant)
            {
                throw new CryptographicException(
                    SR.Cryptography_Der_Invalid_Encoding
#if DEBUG
                    ,
                    new InvalidOperationException(
                        "Expected tag '0x" + expectedByte.ToString("X2") +
                            "', got '0x" + actual.ToString("X2") +
                            "' at position " + position)
#endif
                    );
            }
        }

        private int EatLength()
        {
            int bytesConsumed;
            int answer = ScanContentLength(_data, _position, _end, out bytesConsumed);

            _position += bytesConsumed;
            return answer;
        }

        private static int ScanContentLength(byte[] data, int offset, int end, out int bytesConsumed)
        {
            Debug.Assert(end <= data.Length);

            if (offset >= end)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            byte lengthOrLengthLength = data[offset];

            if (lengthOrLengthLength < 0x80)
            {
                bytesConsumed = 1;

                if (lengthOrLengthLength > end - offset - bytesConsumed)
                    throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

                return lengthOrLengthLength;
            }

            int lengthLength = (lengthOrLengthLength & 0x7F);

            if (lengthLength > sizeof(int))
            {
                // .NET Arrays cannot exceed int.MaxValue in length. Since we're bounded by an
                // array we know that this is invalid data.
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            // The one byte which was lengthLength, plus the number of bytes it said to consume.
            bytesConsumed = 1 + lengthLength;

            if (bytesConsumed > end - offset)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            // CER indefinite length is not supported.
            if (bytesConsumed == 1)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            int lengthEnd = offset + bytesConsumed;
            int accum = 0;
            
            // data[offset] is lengthLength, so start at data[offset + 1] and stop before
            // data[offset + 1 + lengthLength], aka data[end].
            for (int i = offset + 1; i < lengthEnd; i++)
            {
                accum <<= 8;
                accum |= data[i];
            }

            if (accum < 0)
            {
                // .NET Arrays cannot exceed int.MaxValue in length. Since we're bounded by an
                // array we know that this is invalid data.
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }

            if (accum > end - offset - bytesConsumed)
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);

            return accum;
        }

        internal enum DerTag : byte
        {
            Boolean = 0x01,
            Integer = 0x02,
            BitString = 0x03,
            OctetString = 0x04,
            Null = 0x05,
            ObjectIdentifier = 0x06,
            UTF8String = 0x0C,
            Sequence = 0x10,
            Set = 0x11,
            PrintableString = 0x13,
            T61String = 0x14,
            IA5String = 0x16,
            UTCTime = 0x17,
            GeneralizedTime = 0x18,
            BMPString = 0x1E,
        }
    }
}

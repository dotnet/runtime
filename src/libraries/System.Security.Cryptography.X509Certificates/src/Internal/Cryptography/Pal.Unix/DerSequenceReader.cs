// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace Internal.Cryptography.Pal
{
    /// <summary>
    /// Reads data encoded via the Distinguished Encoding Rules for Abstract
    /// Syntax Notation 1 (ASN.1) data.
    /// </summary>
    internal class DerSequenceReader
    {
        internal const byte ContextSpecificTagFlag = 0x80;

        private readonly byte[] _data;
        private readonly int _end;
        private int _position;

        internal int ContentLength { get; private set; }

        private DerSequenceReader(bool startAtPayload, byte[] data)
        {
            Debug.Assert(startAtPayload, "This overload is only for bypassing the sequence tag");
            Debug.Assert(data != null, "Data is null");

            _data = data;
            _position = 0;
            _end = data.Length;

            ContentLength = data.Length;
        }

        internal DerSequenceReader(byte[] data)
            : this(data, 0, data.Length)
        {
        }

        internal DerSequenceReader(byte[] data, int offset, int length)
        {
            _data = data;
            _end = offset + length;

            Debug.Assert(data != null, "Data is null");
            Debug.Assert(offset >= 0, "Offset is negative");
            Debug.Assert(length > 2, "Length is too short");
            Debug.Assert(data.Length >= offset + length, "Array is too short");

            _position = offset;
            EatTag(DerTag.Sequence);
            ContentLength = EatLength();
        }

        internal static DerSequenceReader CreateForPayload(byte[] payload)
        {
            return new DerSequenceReader(true, payload);
        }

        internal bool HasData
        {
            get { return _position < _end; }
        }

        internal byte PeekTag()
        {
            return _data[_position];
        }

        internal void SkipValue()
        {
            EatTag((DerTag)PeekTag());
            int contentLength = EatLength();
            _position += contentLength;
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

        internal byte[] ReadOctetString()
        {
            EatTag(DerTag.OctetString);

            return ReadContentAsBytes();
        }

        internal string ReadOidAsString()
        {
            EatTag(DerTag.ObjectIdentifier);
            int contentLength = EatLength();

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

        internal DerSequenceReader ReadSequence()
        {
            // DerSequenceReader wants to read its own tag, so don't EatTag here.
            CheckTag(DerTag.Sequence, _data, _position);

            int lengthLength;
            int contentLength = ScanContentLength(_data, _position + 1, out lengthLength);
            int totalLength = 1 + lengthLength + contentLength;

            DerSequenceReader reader = new DerSequenceReader(_data, _position, totalLength);
            _position += totalLength;
            return reader;
        }

        internal string ReadIA5String()
        {
            EatTag(DerTag.IA5String);
            int contentLength = EatLength();

            // IA5 (International Alphabet - 5) is functionally equivalent to 7-bit ASCII.

            string ia5String = Encoding.ASCII.GetString(_data, _position, contentLength);
            _position += contentLength;

            return ia5String;
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
            CheckTag(expected, _data, _position);
            _position++;
        }

        private static void CheckTag(DerTag expected, byte[] data, int position)
        {
            byte actual = data[position];

            // Context-specific datatypes cannot be tag-verified
            if ((actual & ContextSpecificTagFlag) != 0)
            {
                return;
            }

            byte relevant = (byte)(actual & 0x1F);
            byte expectedByte = (byte)expected;

            if (expectedByte != relevant)
            {
                throw new InvalidOperationException(
                    "Expected tag '0x" + expectedByte.ToString("X2") +
                        "', got '0x" + actual.ToString("X2") +
                        "' at position " + position);
            }
        }

        private int EatLength()
        {
            int bytesConsumed;
            int answer = ScanContentLength(_data, _position, out bytesConsumed);

            _position += bytesConsumed;
            return answer;
        }

        private static int ScanContentLength(byte[] data, int offset, out int bytesConsumed)
        {
            byte lengthOrLengthLength = data[offset];

            if (lengthOrLengthLength < 0x80)
            {
                bytesConsumed = 1;
                return lengthOrLengthLength;
            }

            // The one byte which was lengthLength, plus the number of bytes it said to consume.
            bytesConsumed = 1 + (lengthOrLengthLength & 0x7F);

            int end = offset + bytesConsumed;
            int accum = 0;
            
            // data[offset] is lengthLength, so start at data[offset + 1] and stop before
            // data[offset + 1 + lengthLength], aka data[end].
            for (int i = offset + 1; i < end; i++)
            {
                accum <<= 8;
                accum += data[i];
            }

            return accum;
        }

        private enum DerTag : byte
        {
            Integer = 0x02,
            BitString = 0x03,
            OctetString = 0x04,
            Null = 0x05,
            ObjectIdentifier = 0x06,
            Sequence = 0x10,
            Set = 0x11,
            PrintableString = 0x13,
            T61String = 0x14,
            IA5String = 0x15,
            UTCTime = 0x17,
        }
    }
}

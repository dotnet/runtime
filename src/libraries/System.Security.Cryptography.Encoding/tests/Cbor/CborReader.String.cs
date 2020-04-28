// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace System.Security.Cryptography.Encoding.Tests.Cbor
{
    internal partial class CborReader
    {
        private static readonly System.Text.Encoding s_utf8Encoding = new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        // Implements major type 2 decoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public byte[] ReadByteString()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.ByteString);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                return ReadChunkedByteStringConcatenated();
            }

            int length = checked((int)ReadUnsignedInteger(_buffer.Span, header, out int additionalBytes));
            EnsureBuffer(1 + additionalBytes + length);
            byte[] result = new byte[length];
            _buffer.Slice(1 + additionalBytes, length).CopyTo(result);
            AdvanceBuffer(1 + additionalBytes + length);
            AdvanceDataItemCounters();
            return result;
        }

        public bool TryReadByteString(Span<byte> destination, out int bytesWritten)
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.ByteString);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                return TryReadChunkedByteStringConcatenated(destination, out bytesWritten);
            }

            int length = checked((int)ReadUnsignedInteger(_buffer.Span, header, out int additionalBytes));
            EnsureBuffer(1 + additionalBytes + length);

            if (length > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            _buffer.Span.Slice(1 + additionalBytes, length).CopyTo(destination);
            AdvanceBuffer(1 + additionalBytes + length);
            AdvanceDataItemCounters();

            bytesWritten = length;
            return true;
        }

        // Implements major type 3 decoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public string ReadTextString()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.TextString);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                return ReadChunkedTextStringConcatenated();
            }

            int length = checked((int)ReadUnsignedInteger(_buffer.Span, header, out int additionalBytes));
            EnsureBuffer(1 + additionalBytes + length);
            ReadOnlySpan<byte> encodedString = _buffer.Span.Slice(1 + additionalBytes, length);

            string result;
            try
            {
                result = s_utf8Encoding.GetString(encodedString);
            }
            catch (DecoderFallbackException e)
            {
                throw new FormatException("Text string payload is not a valid UTF8 string.", e);
            }

            AdvanceBuffer(1 + additionalBytes + length);
            AdvanceDataItemCounters();
            return result;
        }

        public bool TryReadTextString(Span<char> destination, out int charsWritten)
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.TextString);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                return TryReadChunkedTextStringConcatenated(destination, out charsWritten);
            }

            int byteLength = checked((int)ReadUnsignedInteger(_buffer.Span, header, out int additionalBytes));
            EnsureBuffer(1 + additionalBytes + byteLength);

            ReadOnlySpan<byte> encodedSlice = _buffer.Span.Slice(1 + additionalBytes, byteLength);

            int charLength = ValidateUtf8AndGetCharCount(encodedSlice);

            if (charLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            s_utf8Encoding.GetChars(encodedSlice, destination);
            AdvanceBuffer(1 + additionalBytes + byteLength);
            AdvanceDataItemCounters();
            charsWritten = charLength;
            return true;
        }

        public void ReadStartTextStringIndefiniteLength()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.TextString);

            if (header.AdditionalInfo != CborAdditionalInfo.IndefiniteLength)
            {
                throw new InvalidOperationException("CBOR text string is not of indefinite length.");
            }

            AdvanceDataItemCounters();
            AdvanceBuffer(1);

            PushDataItem(CborMajorType.TextString, expectedNestedItems: null);
        }

        public void ReadEndTextStringIndefiniteLength()
        {
            ReadNextIndefiniteLengthBreakByte();
            PopDataItem(CborMajorType.TextString);
            AdvanceBuffer(1);
        }

        public void ReadStartByteStringIndefiniteLength()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.ByteString);

            if (header.AdditionalInfo != CborAdditionalInfo.IndefiniteLength)
            {
                throw new InvalidOperationException("CBOR text string is not of indefinite length.");
            }

            AdvanceDataItemCounters();
            AdvanceBuffer(1);

            PushDataItem(CborMajorType.ByteString, expectedNestedItems: null);
        }

        public void ReadEndByteStringIndefiniteLength()
        {
            ReadNextIndefiniteLengthBreakByte();
            PopDataItem(CborMajorType.ByteString);
            AdvanceBuffer(1);
        }

        private bool TryReadChunkedByteStringConcatenated(Span<byte> destination, out int bytesWritten)
        {
            List<(int offset, int length)> ranges = ReadChunkedStringRanges(CborMajorType.ByteString, out int encodingLength, out int concatenatedBufferSize);

            if (concatenatedBufferSize > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            ReadOnlySpan<byte> source = _buffer.Span;

            foreach ((int o, int l) in ranges)
            {
                source.Slice(o, l).CopyTo(destination);
                destination = destination.Slice(l);
            }

            bytesWritten = concatenatedBufferSize;
            AdvanceBuffer(encodingLength);
            AdvanceDataItemCounters();
            ReturnRangeList(ranges);
            return true;
        }

        private bool TryReadChunkedTextStringConcatenated(Span<char> destination, out int charsWritten)
        {
            List<(int offset, int length)> ranges = ReadChunkedStringRanges(CborMajorType.TextString, out int encodingLength, out int _);
            ReadOnlySpan<byte> buffer = _buffer.Span;

            int concatenatedStringSize = 0;
            foreach ((int o, int l) in ranges)
            {
                concatenatedStringSize += ValidateUtf8AndGetCharCount(buffer.Slice(o, l));
            }

            if (concatenatedStringSize > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            foreach ((int o, int l) in ranges)
            {
                s_utf8Encoding.GetChars(buffer.Slice(o, l), destination);
                destination = destination.Slice(l);
            }

            charsWritten = concatenatedStringSize;
            AdvanceBuffer(encodingLength);
            AdvanceDataItemCounters();
            ReturnRangeList(ranges);
            return true;
        }

        private byte[] ReadChunkedByteStringConcatenated()
        {
            List<(int offset, int length)> ranges = ReadChunkedStringRanges(CborMajorType.ByteString, out int encodingLength, out int concatenatedBufferSize);
            var output = new byte[concatenatedBufferSize];

            ReadOnlySpan<byte> source = _buffer.Span;
            Span<byte> target = output;

            foreach ((int o, int l) in ranges)
            {
                source.Slice(o, l).CopyTo(target);
                target = target.Slice(l);
            }

            Debug.Assert(target.IsEmpty);
            AdvanceBuffer(encodingLength);
            AdvanceDataItemCounters();
            ReturnRangeList(ranges);
            return output;
        }

        private string ReadChunkedTextStringConcatenated()
        {
            List<(int offset, int length)> ranges = ReadChunkedStringRanges(CborMajorType.TextString, out int encodingLength, out int concatenatedBufferSize);
            ReadOnlySpan<byte> buffer = _buffer.Span;
            int concatenatedStringSize = 0;

            foreach ((int o, int l) in ranges)
            {
                concatenatedStringSize += ValidateUtf8AndGetCharCount(buffer.Slice(o, l));
            }

            string output = string.Create(concatenatedStringSize, (ranges, _buffer), BuildString);

            AdvanceBuffer(encodingLength);
            AdvanceDataItemCounters();
            ReturnRangeList(ranges);
            return output;

            static void BuildString(Span<char> target, (List<(int offset, int length)> ranges, ReadOnlyMemory<byte> source) input)
            {
                ReadOnlySpan<byte> source = input.source.Span;

                foreach ((int o, int l) in input.ranges)
                {
                    s_utf8Encoding.GetChars(source.Slice(o, l), target);
                    target = target.Slice(l);
                }

                Debug.Assert(target.IsEmpty);
            }
        }

        // reads a buffer starting with an indefinite-length string,
        // performing validation and returning a list of ranges containing the individual chunk payloads
        private List<(int offset, int length)> ReadChunkedStringRanges(CborMajorType type, out int encodingLength, out int concatenatedBufferSize)
        {
            var ranges = AcquireRangeList();
            ReadOnlySpan<byte> buffer = _buffer.Span;
            concatenatedBufferSize = 0;

            int i = 1; // skip the indefinite-length initial byte
            CborInitialByte nextInitialByte = ReadNextInitialByte(buffer.Slice(i), type);

            while (nextInitialByte.InitialByte != CborInitialByte.IndefiniteLengthBreakByte)
            {
                checked
                {
                    int chunkLength = (int)ReadUnsignedInteger(buffer.Slice(i), nextInitialByte, out int additionalBytes);
                    ranges.Add((i + 1 + additionalBytes, chunkLength));
                    i += 1 + additionalBytes + chunkLength;
                    concatenatedBufferSize += chunkLength;
                }

                nextInitialByte = ReadNextInitialByte(buffer.Slice(i), type);
            }

            encodingLength = i + 1; // include the break byte
            return ranges;

            static CborInitialByte ReadNextInitialByte(ReadOnlySpan<byte> buffer, CborMajorType expectedType)
            {
                EnsureBuffer(buffer, 1);
                var cib = new CborInitialByte(buffer[0]);

                if (cib.InitialByte != CborInitialByte.IndefiniteLengthBreakByte && cib.MajorType != expectedType)
                {
                    throw new FormatException("Indefinite-length CBOR string containing invalid data item.");
                }

                return cib;
            }
        }

        // SkipValue() helper: reads a cbor string without allocating or copying to a buffer
        // NB this only handles definite-length chunks
        private void SkipString(CborMajorType type)
        {
            CborInitialByte header = PeekInitialByte(expectedType: type);

            ReadOnlySpan<byte> buffer = _buffer.Span;
            int byteLength = checked((int)ReadUnsignedInteger(buffer, header, out int additionalBytes));
            EnsureBuffer(1 + additionalBytes + byteLength);

            // force any utf8 decoding errors if text string
            if (type == CborMajorType.TextString)
            {
                ReadOnlySpan<byte> encodedSlice = buffer.Slice(1 + additionalBytes, byteLength);
                ValidateUtf8AndGetCharCount(encodedSlice);
            }

            AdvanceBuffer(1 + additionalBytes + byteLength);
            AdvanceDataItemCounters();
        }

        private int ValidateUtf8AndGetCharCount(ReadOnlySpan<byte> buffer)
        {
            try
            {
                return s_utf8Encoding.GetCharCount(buffer);
            }
            catch (DecoderFallbackException e)
            {
                throw new FormatException("Text string payload is not a valid UTF8 string.", e);
            }
        }
    }
}

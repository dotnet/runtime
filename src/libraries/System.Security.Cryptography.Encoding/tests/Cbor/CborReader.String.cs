// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        // stores a reusable List allocation for keeping ranges in the buffer
        private List<(int Offset, int Length)>? _indefiniteLengthStringRangeAllocation = null;

        // Implements major type 2 decoding per https://tools.ietf.org/html/rfc7049#section-2.1
        public byte[] ReadByteString()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.ByteString);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                if (_isConformanceLevelCheckEnabled && CborConformanceLevelHelpers.RequiresDefiniteLengthItems(ConformanceLevel))
                {
                    throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_IndefiniteLengthItemsNotSupported, ConformanceLevel));
                }

                return ReadChunkedByteStringConcatenated();
            }

            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            int length = checked((int)ReadUnsignedInteger(buffer, header, out int additionalBytes));
            EnsureReadCapacity(1 + additionalBytes + length);
            byte[] result = new byte[length];
            buffer.Slice(1 + additionalBytes, length).CopyTo(result);
            AdvanceBuffer(1 + additionalBytes + length);
            AdvanceDataItemCounters();
            return result;
        }

        public bool TryReadByteString(Span<byte> destination, out int bytesWritten)
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.ByteString);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                if (_isConformanceLevelCheckEnabled && CborConformanceLevelHelpers.RequiresDefiniteLengthItems(ConformanceLevel))
                {
                    throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_IndefiniteLengthItemsNotSupported, ConformanceLevel));
                }

                return TryReadChunkedByteStringConcatenated(destination, out bytesWritten);
            }

            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            int length = checked((int)ReadUnsignedInteger(buffer, header, out int additionalBytes));
            EnsureReadCapacity(1 + additionalBytes + length);

            if (length > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            buffer.Slice(1 + additionalBytes, length).CopyTo(destination);
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
                if (_isConformanceLevelCheckEnabled && CborConformanceLevelHelpers.RequiresDefiniteLengthItems(ConformanceLevel))
                {
                    throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_IndefiniteLengthItemsNotSupported, ConformanceLevel));
                }

                return ReadChunkedTextStringConcatenated();
            }

            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            int length = checked((int)ReadUnsignedInteger(buffer, header, out int additionalBytes));
            EnsureReadCapacity(1 + additionalBytes + length);
            ReadOnlySpan<byte> encodedString = buffer.Slice(1 + additionalBytes, length);
            Encoding utf8Encoding = CborConformanceLevelHelpers.GetUtf8Encoding(ConformanceLevel);

            string result;
            try
            {
                result = utf8Encoding.GetString(encodedString);
            }
            catch (DecoderFallbackException e)
            {
                throw new FormatException(SR.Cbor_Reader_InvalidCbor_InvalidUtf8StringEncoding, e);
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
                if (_isConformanceLevelCheckEnabled && CborConformanceLevelHelpers.RequiresDefiniteLengthItems(ConformanceLevel))
                {
                    throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_IndefiniteLengthItemsNotSupported, ConformanceLevel));
                }

                return TryReadChunkedTextStringConcatenated(destination, out charsWritten);
            }

            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            int byteLength = checked((int)ReadUnsignedInteger(buffer, header, out int additionalBytes));
            EnsureReadCapacity(1 + additionalBytes + byteLength);

            Encoding utf8Encoding = CborConformanceLevelHelpers.GetUtf8Encoding(ConformanceLevel);
            ReadOnlySpan<byte> encodedSlice = buffer.Slice(1 + additionalBytes, byteLength);

            int charLength = ValidateUtf8AndGetCharCount(encodedSlice, utf8Encoding);

            if (charLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            utf8Encoding.GetChars(encodedSlice, destination);
            AdvanceBuffer(1 + additionalBytes + byteLength);
            AdvanceDataItemCounters();
            charsWritten = charLength;
            return true;
        }

        public void ReadStartTextString()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.TextString);

            if (header.AdditionalInfo != CborAdditionalInfo.IndefiniteLength)
            {
                throw new InvalidOperationException(SR.Cbor_Reader_NotIndefiniteLengthString);
            }

            if (_isConformanceLevelCheckEnabled && CborConformanceLevelHelpers.RequiresDefiniteLengthItems(ConformanceLevel))
            {
                throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_IndefiniteLengthItemsNotSupported, ConformanceLevel));
            }

            AdvanceBuffer(1);
            PushDataItem(CborMajorType.TextString, definiteLength: null);
        }

        public void ReadEndTextString()
        {
            ValidateNextByteIsBreakByte();
            PopDataItem(CborMajorType.TextString);
            AdvanceDataItemCounters();
            AdvanceBuffer(1);
        }

        public void ReadStartByteString()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.ByteString);

            if (header.AdditionalInfo != CborAdditionalInfo.IndefiniteLength)
            {
                throw new InvalidOperationException(SR.Cbor_Reader_NotIndefiniteLengthString);
            }

            if (_isConformanceLevelCheckEnabled && CborConformanceLevelHelpers.RequiresDefiniteLengthItems(ConformanceLevel))
            {
                throw new FormatException(SR.Format(SR.Cbor_ConformanceLevel_IndefiniteLengthItemsNotSupported, ConformanceLevel));
            }

            AdvanceBuffer(1);
            PushDataItem(CborMajorType.ByteString, definiteLength: null);
        }

        public void ReadEndByteString()
        {
            ValidateNextByteIsBreakByte();
            PopDataItem(CborMajorType.ByteString);
            AdvanceDataItemCounters();
            AdvanceBuffer(1);
        }

        private bool TryReadChunkedByteStringConcatenated(Span<byte> destination, out int bytesWritten)
        {
            List<(int Offset, int Length)> ranges = ReadChunkedStringRanges(CborMajorType.ByteString, out int encodingLength, out int concatenatedBufferSize);

            if (concatenatedBufferSize > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            ReadOnlySpan<byte> source = GetRemainingBytes();

            foreach ((int o, int l) in ranges)
            {
                source.Slice(o, l).CopyTo(destination);
                destination = destination.Slice(l);
            }

            bytesWritten = concatenatedBufferSize;
            AdvanceBuffer(encodingLength);
            AdvanceDataItemCounters();
            ReturnIndefiniteLengthStringRangeList(ranges);
            return true;
        }

        private bool TryReadChunkedTextStringConcatenated(Span<char> destination, out int charsWritten)
        {
            List<(int Offset, int Length)> ranges = ReadChunkedStringRanges(CborMajorType.TextString, out int encodingLength, out int _);
            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            Encoding utf8Encoding = CborConformanceLevelHelpers.GetUtf8Encoding(ConformanceLevel);

            int concatenatedStringSize = 0;
            foreach ((int o, int l) in ranges)
            {
                concatenatedStringSize += ValidateUtf8AndGetCharCount(buffer.Slice(o, l), utf8Encoding);
            }

            if (concatenatedStringSize > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            foreach ((int o, int l) in ranges)
            {
                utf8Encoding.GetChars(buffer.Slice(o, l), destination);
                destination = destination.Slice(l);
            }

            charsWritten = concatenatedStringSize;
            AdvanceBuffer(encodingLength);
            AdvanceDataItemCounters();
            ReturnIndefiniteLengthStringRangeList(ranges);
            return true;
        }

        private byte[] ReadChunkedByteStringConcatenated()
        {
            List<(int Offset, int Length)> ranges = ReadChunkedStringRanges(CborMajorType.ByteString, out int encodingLength, out int concatenatedBufferSize);
            var output = new byte[concatenatedBufferSize];

            ReadOnlySpan<byte> source = GetRemainingBytes();
            Span<byte> target = output;

            foreach ((int o, int l) in ranges)
            {
                source.Slice(o, l).CopyTo(target);
                target = target.Slice(l);
            }

            Debug.Assert(target.IsEmpty);
            AdvanceBuffer(encodingLength);
            AdvanceDataItemCounters();
            ReturnIndefiniteLengthStringRangeList(ranges);
            return output;
        }

        private string ReadChunkedTextStringConcatenated()
        {
            List<(int Offset, int Length)> ranges = ReadChunkedStringRanges(CborMajorType.TextString, out int encodingLength, out int concatenatedBufferSize);
            Encoding utf8Encoding = CborConformanceLevelHelpers.GetUtf8Encoding(ConformanceLevel);
            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            int concatenatedStringSize = 0;

            foreach ((int o, int l) in ranges)
            {
                concatenatedStringSize += ValidateUtf8AndGetCharCount(buffer.Slice(o, l), utf8Encoding);
            }

            string output = string.Create(concatenatedStringSize, (ranges, _buffer.Slice(_offset), utf8Encoding), BuildString);

            AdvanceBuffer(encodingLength);
            AdvanceDataItemCounters();
            ReturnIndefiniteLengthStringRangeList(ranges);
            return output;

            static void BuildString(Span<char> target, (List<(int Offset, int Length)> ranges, ReadOnlyMemory<byte> source, Encoding utf8Encoding) input)
            {
                ReadOnlySpan<byte> source = input.source.Span;

                foreach ((int o, int l) in input.ranges)
                {
                    int charsWritten = input.utf8Encoding.GetChars(source.Slice(o, l), target);
                    target = target.Slice(charsWritten);
                }

                Debug.Assert(target.IsEmpty);
            }
        }

        // reads a buffer starting with an indefinite-length string,
        // performing validation and returning a list of ranges containing the individual chunk payloads
        private List<(int Offset, int Length)> ReadChunkedStringRanges(CborMajorType type, out int encodingLength, out int concatenatedBufferSize)
        {
            List<(int Offset, int Length)> ranges = AcquireIndefiniteLengthStringRangeList();
            ReadOnlySpan<byte> buffer = GetRemainingBytes();
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
                EnsureReadCapacity(buffer, 1);
                var header = new CborInitialByte(buffer[0]);

                if (header.InitialByte != CborInitialByte.IndefiniteLengthBreakByte && header.MajorType != expectedType)
                {
                    throw new FormatException(SR.Cbor_Reader_InvalidCbor_IndefiniteLengthStringContainsInvalidDataItem);
                }

                return header;
            }
        }

        // SkipValue() helper: reads a cbor string without allocating or copying to a buffer
        private void SkipString(CborMajorType type)
        {
            Debug.Assert(type == CborMajorType.ByteString || type == CborMajorType.TextString);

            CborInitialByte header = PeekInitialByte(expectedType: type);

            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            int byteLength = checked((int)ReadUnsignedInteger(buffer, header, out int additionalBytes));
            EnsureReadCapacity(1 + additionalBytes + byteLength);

            // if conformance level requires it, validate the utf-8 encoding that is being skipped
            if (type == CborMajorType.TextString && _isConformanceLevelCheckEnabled &&
                CborConformanceLevelHelpers.RequiresUtf8Validation(ConformanceLevel))
            {
                ReadOnlySpan<byte> encodedSlice = buffer.Slice(1 + additionalBytes, byteLength);
                Encoding utf8Encoding = CborConformanceLevelHelpers.GetUtf8Encoding(ConformanceLevel);
                ValidateUtf8AndGetCharCount(encodedSlice, utf8Encoding);
            }

            AdvanceBuffer(1 + additionalBytes + byteLength);
            AdvanceDataItemCounters();
        }

        private int ValidateUtf8AndGetCharCount(ReadOnlySpan<byte> buffer, Encoding utf8Encoding)
        {
            try
            {
                return utf8Encoding.GetCharCount(buffer);
            }
            catch (DecoderFallbackException e)
            {
                throw new FormatException(SR.Cbor_Reader_InvalidCbor_InvalidUtf8StringEncoding, e);
            }
        }

        private List<(int Offset, int Length)> AcquireIndefiniteLengthStringRangeList()
        {
            List<(int Offset, int Length)>? ranges = Interlocked.Exchange(ref _indefiniteLengthStringRangeAllocation, null);

            if (ranges != null)
            {
                ranges.Clear();
                return ranges;
            }

            return new List<(int Offset, int Length)>();
        }

        private void ReturnIndefiniteLengthStringRangeList(List<(int Offset, int Length)> ranges)
        {
            _indefiniteLengthStringRangeAllocation = ranges;
        }
    }
}

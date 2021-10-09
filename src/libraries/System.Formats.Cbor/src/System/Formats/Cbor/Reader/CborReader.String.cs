// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace System.Formats.Cbor
{
    public partial class CborReader
    {
        // Implements major type 2,3 decoding per https://tools.ietf.org/html/rfc7049#section-2.1

        // stores a reusable List allocation for storing indefinite length string chunk offsets
        private List<(int Offset, int Length)>? _indefiniteLengthStringRangeAllocation;

        /// <summary>Reads the next data item as a byte string (major type 2).</summary>
        /// <returns>The decoded byte array.</returns>
        /// <exception cref="InvalidOperationException">The next date item does not have the correct major type.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        /// <remarks>The method accepts indefinite length strings, which it concatenates to a single string.</remarks>
        public byte[] ReadByteString()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.ByteString);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                if (_isConformanceModeCheckEnabled && CborConformanceModeHelpers.RequiresDefiniteLengthItems(ConformanceMode))
                {
                    throw new CborContentException(SR.Format(SR.Cbor_ConformanceMode_IndefiniteLengthItemsNotSupported, ConformanceMode));
                }

                return ReadIndefiniteLengthByteStringConcatenated();
            }

            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            int length = DecodeDefiniteLength(header, buffer, out int bytesRead);
            EnsureReadCapacity(bytesRead + length);
            byte[] result = new byte[length];
            buffer.Slice(bytesRead, length).CopyTo(result);
            AdvanceBuffer(bytesRead + length);
            AdvanceDataItemCounters();
            return result;
        }

        /// <summary>Reads the next data item as a byte string (major type 2).</summary>
        /// <param name="destination">The buffer in which to write the read bytes.</param>
        /// <param name="bytesWritten">On success, receives the number of bytes written to <paramref name="destination" />.</param>
        /// <returns><see langword="true" /> if <paramref name="destination" /> had sufficient length to receive the value and the reader advances; otherwise, <see langword="false" />.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        /// <remarks>The method accepts indefinite length strings, which it will concatenate to a single string.</remarks>
        public bool TryReadByteString(Span<byte> destination, out int bytesWritten)
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.ByteString);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                if (_isConformanceModeCheckEnabled && CborConformanceModeHelpers.RequiresDefiniteLengthItems(ConformanceMode))
                {
                    throw new CborContentException(SR.Format(SR.Cbor_ConformanceMode_IndefiniteLengthItemsNotSupported, ConformanceMode));
                }

                return TryReadIndefiniteLengthByteStringConcatenated(destination, out bytesWritten);
            }

            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            int length = DecodeDefiniteLength(header, buffer, out int bytesRead);
            EnsureReadCapacity(bytesRead + length);

            if (length > destination.Length)
            {
                bytesWritten = 0;
                return false;
            }

            buffer.Slice(bytesRead, length).CopyTo(destination);
            AdvanceBuffer(bytesRead + length);
            AdvanceDataItemCounters();

            bytesWritten = length;
            return true;
        }

        /// <summary>Reads the next data item as a definite-length byte string (major type 2).</summary>
        /// <returns>A <see cref="ReadOnlyMemory{T}" /> view of the byte string payload.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.
        /// -or-
        /// The data item is an indefinite-length byte string.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        public ReadOnlyMemory<byte> ReadDefiniteLengthByteString()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.ByteString);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                throw new InvalidOperationException();
            }

            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            int length = DecodeDefiniteLength(header, buffer, out int bytesRead);

            EnsureReadCapacity(bytesRead + length);
            ReadOnlyMemory<byte> byteSlice = _data.Slice(_offset + bytesRead, length);
            AdvanceBuffer(bytesRead + length);
            AdvanceDataItemCounters();
            return byteSlice;
        }

        /// <summary>Reads the next data item as the start of an indefinite-length byte string (major type 2).</summary>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.
        /// -or-
        /// The next data item is a definite-length encoded string.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        public void ReadStartIndefiniteLengthByteString()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.ByteString);

            if (header.AdditionalInfo != CborAdditionalInfo.IndefiniteLength)
            {
                throw new InvalidOperationException(SR.Cbor_Reader_NotIndefiniteLengthString);
            }

            if (_isConformanceModeCheckEnabled && CborConformanceModeHelpers.RequiresDefiniteLengthItems(ConformanceMode))
            {
                throw new CborContentException(SR.Format(SR.Cbor_ConformanceMode_IndefiniteLengthItemsNotSupported, ConformanceMode));
            }

            AdvanceBuffer(1);
            PushDataItem(CborMajorType.ByteString, definiteLength: null);
        }

        /// <summary>Ends reading an indefinite-length byte string (major type 2).</summary>
        /// <exception cref="InvalidOperationException">The current context is not an indefinite-length string.
        /// -or-
        /// The reader is not at the end of the string.</exception>
        /// <exception cref="CborContentException">There was an unexpected end of CBOR encoding data.</exception>
        public void ReadEndIndefiniteLengthByteString()
        {
            ValidateNextByteIsBreakByte();
            PopDataItem(CborMajorType.ByteString);
            AdvanceDataItemCounters();
            AdvanceBuffer(1);
        }

        /// <summary>Reads the next data item as a UTF-8 text string (major type 3).</summary>
        /// <returns>The decoded string.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        /// <remarks>The method accepts indefinite length strings, which it will concatenate to a single string.</remarks>
        public string ReadTextString()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.TextString);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                if (_isConformanceModeCheckEnabled && CborConformanceModeHelpers.RequiresDefiniteLengthItems(ConformanceMode))
                {
                    throw new CborContentException(SR.Format(SR.Cbor_ConformanceMode_IndefiniteLengthItemsNotSupported, ConformanceMode));
                }

                return ReadIndefiniteLengthTextStringConcatenated();
            }

            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            int length = DecodeDefiniteLength(header, buffer, out int bytesRead);
            EnsureReadCapacity(bytesRead + length);
            ReadOnlySpan<byte> encodedString = buffer.Slice(bytesRead, length);
            Encoding utf8Encoding = CborConformanceModeHelpers.GetUtf8Encoding(ConformanceMode);

            string result;
            try
            {
                result = utf8Encoding.GetString(encodedString);
            }
            catch (DecoderFallbackException e)
            {
                throw new CborContentException(SR.Cbor_Reader_InvalidCbor_InvalidUtf8StringEncoding, e);
            }

            AdvanceBuffer(bytesRead + length);
            AdvanceDataItemCounters();
            return result;
        }

        /// <summary>Reads the next data item as a UTF-8 text string (major type 3).</summary>
        /// <param name="destination">The buffer in which to write.</param>
        /// <param name="charsWritten">On success, receives the number of chars written to <paramref name="destination" />.</param>
        /// <returns><see langword="true" /> and advances the reader if <paramref name="destination" /> had sufficient length to receive the value, otherwise <see langword="false" /> and the reader does not advance.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        /// <remarks>The method accepts indefinite length strings, which it will concatenate to a single string.</remarks>
        public bool TryReadTextString(Span<char> destination, out int charsWritten)
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.TextString);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                if (_isConformanceModeCheckEnabled && CborConformanceModeHelpers.RequiresDefiniteLengthItems(ConformanceMode))
                {
                    throw new CborContentException(SR.Format(SR.Cbor_ConformanceMode_IndefiniteLengthItemsNotSupported, ConformanceMode));
                }

                return TryReadIndefiniteLengthTextStringConcatenated(destination, out charsWritten);
            }

            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            int byteLength = DecodeDefiniteLength(header, buffer, out int bytesRead);
            EnsureReadCapacity(bytesRead + byteLength);

            Encoding utf8Encoding = CborConformanceModeHelpers.GetUtf8Encoding(ConformanceMode);
            ReadOnlySpan<byte> encodedSlice = buffer.Slice(bytesRead, byteLength);

            int charLength = ValidateUtf8AndGetCharCount(encodedSlice, utf8Encoding);

            if (charLength > destination.Length)
            {
                charsWritten = 0;
                return false;
            }

            utf8Encoding.GetChars(encodedSlice, destination);
            AdvanceBuffer(bytesRead + byteLength);
            AdvanceDataItemCounters();
            charsWritten = charLength;
            return true;
        }

        /// <summary>Reads the next data item as a definite-length UTF-8 text string (major type 3).</summary>
        /// <returns>A <see cref="ReadOnlyMemory{T}" /> view of the raw UTF-8 payload.</returns>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.
        /// -or-
        /// The data item is an indefinite-length text string.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        public ReadOnlyMemory<byte> ReadDefiniteLengthTextStringBytes()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.TextString);

            if (header.AdditionalInfo == CborAdditionalInfo.IndefiniteLength)
            {
                throw new InvalidOperationException();
            }

            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            int byteLength = DecodeDefiniteLength(header, buffer, out int bytesRead);
            EnsureReadCapacity(bytesRead + byteLength);

            ReadOnlyMemory<byte> encodedSlice = _data.Slice(_offset + bytesRead, byteLength);

            if (_isConformanceModeCheckEnabled && CborConformanceModeHelpers.RequiresUtf8Validation(ConformanceMode))
            {
                Encoding encoding = CborConformanceModeHelpers.GetUtf8Encoding(ConformanceMode);
                ValidateUtf8AndGetCharCount(encodedSlice.Span, encoding);
            }

            AdvanceBuffer(bytesRead + byteLength);
            AdvanceDataItemCounters();
            return encodedSlice;
        }

        /// <summary>Reads the next data item as the start of an indefinite-length UTF-8 text string (major type 3).</summary>
        /// <exception cref="InvalidOperationException">The next data item does not have the correct major type.
        /// -or-
        /// The next data item is a definite-length encoded string.</exception>
        /// <exception cref="CborContentException">The next value has an invalid CBOR encoding.
        /// -or-
        /// There was an unexpected end of CBOR encoding data.
        /// -or-
        /// The next value uses a CBOR encoding that is not valid under the current conformance mode.</exception>
        public void ReadStartIndefiniteLengthTextString()
        {
            CborInitialByte header = PeekInitialByte(expectedType: CborMajorType.TextString);

            if (header.AdditionalInfo != CborAdditionalInfo.IndefiniteLength)
            {
                throw new InvalidOperationException(SR.Cbor_Reader_NotIndefiniteLengthString);
            }

            if (_isConformanceModeCheckEnabled && CborConformanceModeHelpers.RequiresDefiniteLengthItems(ConformanceMode))
            {
                throw new CborContentException(SR.Format(SR.Cbor_ConformanceMode_IndefiniteLengthItemsNotSupported, ConformanceMode));
            }

            AdvanceBuffer(1);
            PushDataItem(CborMajorType.TextString, definiteLength: null);
        }

        /// <summary>Ends reading an indefinite-length UTF-8 text string (major type 3).</summary>
        /// <exception cref="InvalidOperationException">The current context is not an indefinite-length string.
        /// -or-
        /// The reader is not at the end of the string.</exception>
        /// <exception cref="CborContentException">There was an unexpected end of CBOR encoding data.</exception>
        public void ReadEndIndefiniteLengthTextString()
        {
            ValidateNextByteIsBreakByte();
            PopDataItem(CborMajorType.TextString);
            AdvanceDataItemCounters();
            AdvanceBuffer(1);
        }

        private byte[] ReadIndefiniteLengthByteStringConcatenated()
        {
            List<(int Offset, int Length)> ranges = ReadIndefiniteLengthStringChunkRanges(CborMajorType.ByteString, out int encodingLength, out int concatenatedBufferSize);
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

        private bool TryReadIndefiniteLengthByteStringConcatenated(Span<byte> destination, out int bytesWritten)
        {
            List<(int Offset, int Length)> ranges = ReadIndefiniteLengthStringChunkRanges(CborMajorType.ByteString, out int encodingLength, out int concatenatedBufferSize);

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

        private string ReadIndefiniteLengthTextStringConcatenated()
        {
            List<(int Offset, int Length)> ranges = ReadIndefiniteLengthStringChunkRanges(CborMajorType.TextString, out int encodingLength, out int concatenatedBufferSize);
            Encoding utf8Encoding = CborConformanceModeHelpers.GetUtf8Encoding(ConformanceMode);
            ReadOnlySpan<byte> buffer = GetRemainingBytes();

            // calculate the string character length
            int concatenatedStringSize = 0;
            foreach ((int o, int l) in ranges)
            {
                concatenatedStringSize += ValidateUtf8AndGetCharCount(buffer.Slice(o, l), utf8Encoding);
            }

            // build the string using range data
            string output = string.Create(concatenatedStringSize, (ranges, _data.Slice(_offset), utf8Encoding), BuildString);

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

        private bool TryReadIndefiniteLengthTextStringConcatenated(Span<char> destination, out int charsWritten)
        {
            List<(int Offset, int Length)> ranges = ReadIndefiniteLengthStringChunkRanges(CborMajorType.TextString, out int encodingLength, out int _);
            ReadOnlySpan<byte> buffer = GetRemainingBytes();
            Encoding utf8Encoding = CborConformanceModeHelpers.GetUtf8Encoding(ConformanceMode);

            // calculate the string character length
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

        // Reads a buffer starting with an indefinite-length string,
        // performing validation and returning a list of ranges
        // containing the individual chunk payloads
        private List<(int Offset, int Length)> ReadIndefiniteLengthStringChunkRanges(CborMajorType type, out int encodingLength, out int concatenatedBufferSize)
        {
            List<(int Offset, int Length)> ranges = AcquireIndefiniteLengthStringRangeList();
            ReadOnlySpan<byte> data = GetRemainingBytes();
            concatenatedBufferSize = 0;

            int i = 1; // skip the indefinite-length initial byte
            CborInitialByte nextInitialByte = ReadNextInitialByte(data.Slice(i), type);

            while (nextInitialByte.InitialByte != CborInitialByte.IndefiniteLengthBreakByte)
            {
                int chunkLength = DecodeDefiniteLength(nextInitialByte, data.Slice(i), out int bytesRead);
                ranges.Add((i + bytesRead, chunkLength));
                i += bytesRead + chunkLength;
                concatenatedBufferSize += chunkLength;

                nextInitialByte = ReadNextInitialByte(data.Slice(i), type);
            }

            encodingLength = i + 1; // include the break byte
            return ranges;

            static CborInitialByte ReadNextInitialByte(ReadOnlySpan<byte> buffer, CborMajorType expectedType)
            {
                EnsureReadCapacity(buffer, 1);
                var header = new CborInitialByte(buffer[0]);

                if (header.InitialByte != CborInitialByte.IndefiniteLengthBreakByte && header.MajorType != expectedType)
                {
                    throw new CborContentException(SR.Cbor_Reader_InvalidCbor_IndefiniteLengthStringContainsInvalidDataItem);
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
            int byteLength = DecodeDefiniteLength(header, buffer, out int bytesRead);
            EnsureReadCapacity(bytesRead + byteLength);

            // if conformance mode requires it, validate the utf-8 encoding that is being skipped
            if (type == CborMajorType.TextString && _isConformanceModeCheckEnabled &&
                CborConformanceModeHelpers.RequiresUtf8Validation(ConformanceMode))
            {
                ReadOnlySpan<byte> encodedSlice = buffer.Slice(bytesRead, byteLength);
                Encoding utf8Encoding = CborConformanceModeHelpers.GetUtf8Encoding(ConformanceMode);
                ValidateUtf8AndGetCharCount(encodedSlice, utf8Encoding);
            }

            AdvanceBuffer(bytesRead + byteLength);
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
                throw new CborContentException(SR.Cbor_Reader_InvalidCbor_InvalidUtf8StringEncoding, e);
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

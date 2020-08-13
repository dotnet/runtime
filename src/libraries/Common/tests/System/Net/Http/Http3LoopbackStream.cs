// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Quic;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{

    internal sealed class Http3LoopbackStream : IDisposable
    {
        private const int MaximumVarIntBytes = 8;
        private const long VarIntMax = (1L << 62) - 1;

        private const long DataFrame = 0x0;
        private const long HeadersFrame = 0x1;
        private const long SettingsFrame = 0x4;

        private readonly QuicStream _stream;

        public bool CanRead => _stream.CanRead;
        public bool CanWrite => _stream.CanWrite;

        public Http3LoopbackStream(QuicStream stream)
        {
            _stream = stream;
        }

        public void Dispose()
        {
            _stream.Dispose();
        }

        public async Task SendUnidirectionalStreamTypeAsync(long streamType)
        {
            var buffer = new byte[MaximumVarIntBytes];
            int bytesWritten = EncodeHttpInteger(streamType, buffer);
            await _stream.WriteAsync(buffer.AsMemory(0, bytesWritten)).ConfigureAwait(false);
        }

        public async Task SendSettingsFrameAsync(ICollection<(long settingId, long settingValue)> settings)
        {
            var buffer = new byte[settings.Count * MaximumVarIntBytes * 2];

            int bytesWritten = 0;

            foreach ((long settingId, long settingValue) in settings)
            {
                bytesWritten += EncodeHttpInteger(settingId, buffer.AsSpan(bytesWritten));
                bytesWritten += EncodeHttpInteger(settingValue, buffer.AsSpan(bytesWritten));
            }

            await SendFrameAsync(SettingsFrame, buffer.AsMemory(0, bytesWritten)).ConfigureAwait(false);
        }

        public async Task SendHeadersFrameAsync(IEnumerable<HttpHeaderData> headers)
        {
            int bufferLength = QPackTestEncoder.MaxPrefixLength;

            foreach (HttpHeaderData header in headers)
            {
                Debug.Assert(header.Name != null);
                Debug.Assert(header.Value != null);

                // Two varints for length, and double the name/value lengths to account for expanding Huffman coding.
                bufferLength += QPackTestEncoder.MaxVarIntLength * 2 + header.Name.Length * 2 + header.Value.Length * 2;
            }

            var buffer = new byte[bufferLength];
            int bytesWritten = 0;

            bytesWritten += QPackTestEncoder.EncodePrefix(buffer.AsSpan(bytesWritten), 0, 0);

            foreach (HttpHeaderData header in headers)
            {
                bytesWritten += QPackTestEncoder.EncodeHeader(buffer.AsSpan(bytesWritten), header.Name, header.Value, header.ValueEncoding, header.HuffmanEncoded ? QPackFlags.HuffmanEncode : QPackFlags.None);
            }

            await SendFrameAsync(HeadersFrame, buffer.AsMemory(0, bytesWritten)).ConfigureAwait(false);
        }

        public async Task SendDataFrameAsync(ReadOnlyMemory<byte> data)
        {
            await SendFrameAsync(DataFrame, data).ConfigureAwait(false);
        }

        public async Task SendFrameAsync(long frameType, ReadOnlyMemory<byte> framePayload)
        {
            var buffer = new byte[MaximumVarIntBytes * 2];

            int bytesWritten = 0;

            bytesWritten += EncodeHttpInteger(frameType, buffer.AsSpan(bytesWritten));
            bytesWritten += EncodeHttpInteger(framePayload.Length, buffer.AsSpan(bytesWritten));

            await _stream.WriteAsync(buffer.AsMemory(0, bytesWritten)).ConfigureAwait(false);
            await _stream.WriteAsync(framePayload).ConfigureAwait(false);
        }

        public async Task ShutdownSendAsync()
        {
            _stream.Shutdown();
            await _stream.ShutdownWriteCompleted().ConfigureAwait(false);
        }

        static int EncodeHttpInteger(long longToEncode, Span<byte> buffer)
        {
            Debug.Assert(longToEncode >= 0);
            Debug.Assert(longToEncode <= VarIntMax);

            const uint OneByteLimit = (1U << 6) - 1;
            const uint TwoByteLimit = (1U << 14) - 1;
            const uint FourByteLimit = (1U << 30) - 1;

            if (longToEncode < OneByteLimit)
            {
                buffer[0] = (byte)longToEncode;
                return 1;
            }
            else if (longToEncode < TwoByteLimit)
            {
                BinaryPrimitives.WriteUInt16BigEndian(buffer, (ushort)((uint)longToEncode | 0x4000u));
                return 2;
            }
            else if (longToEncode < FourByteLimit)
            {
                BinaryPrimitives.WriteUInt32BigEndian(buffer, (uint)longToEncode | 0x80000000);
                return 4;
            }
            else
            {
                BinaryPrimitives.WriteUInt64BigEndian(buffer, (ulong)longToEncode | 0xC000000000000000);
                return 8;
            }
        }

        public async Task<byte[]> ReadRequestBodyAsync()
        {
            var buffer = new MemoryStream();

            while (true)
            {
                (long? frameType, byte[] payload) = await ReadFrameAsync().ConfigureAwait(false);

                switch (frameType)
                {
                    case DataFrame:
                        buffer.Write(payload);
                        break;
                    case null:
                        return buffer.ToArray();
                }
            }
        }

        public async Task<HttpRequestData> ReadRequestDataAsync(bool readBody = true)
        {
            (long? frameType, byte[] payload) = await ReadFrameAsync().ConfigureAwait(false);

            if (frameType == null) throw new Exception("unable to read request headers; unexpected end of stream.");
            if (frameType != HeadersFrame) throw new Exception($"unable to read request headers; received frame type 0x{frameType:x}.");

            HttpRequestData requestData = ParseHeaders(payload);

            if (readBody)
            {
                requestData.Body = await ReadRequestBodyAsync().ConfigureAwait(false);
            }

            return requestData;
        }

        private HttpRequestData ParseHeaders(ReadOnlySpan<byte> buffer)
        {
            HttpRequestData request = new HttpRequestData { RequestId = Http3LoopbackConnection.GetRequestId(_stream) };

            (int prefixLength, int requiredInsertCount, int deltaBase) = QPackTestDecoder.DecodePrefix(buffer);
            if (requiredInsertCount != 0 || deltaBase != 0) throw new Exception("QPack dynamic table not yet supported.");

            buffer = buffer.Slice(prefixLength);

            while (!buffer.IsEmpty)
            {
                (int headerLength, HttpHeaderData header) = QPackTestDecoder.DecodeHeader(buffer);

                request.Headers.Add(header);
                buffer = buffer.Slice(headerLength);

                switch (header.Name)
                {
                    case ":method":
                        request.Method = header.Value;
                        break;
                    case ":path":
                        request.Path = header.Value;
                        break;
                }
            }
            request.Version = HttpVersion30.Value;

            return request;
        }

        public async Task WaitForCancellationAsync(bool ignoreIncomingData = true)
        {
            while (true)
            {
                (long? frameType, _) = await ReadFrameAsync().ConfigureAwait(false);

                switch (frameType)
                {
                    case null:
                        // end of stream reached.
                        return;
                    case DataFrame when ignoreIncomingData == true:
                        break;
                    default:
                        Debug.Fail("Unexpected frame type while waiting for client cancellation.");
                        throw new Exception();
                }
            }
        }

        public async Task<(long? frameType, byte[] payload)> ReadFrameAsync()
        {
            long? frameType = await ReadInteger().ConfigureAwait(false);
            if (frameType == null) return (null, null);

            long? payloadLength = await ReadInteger().ConfigureAwait(false);
            if (payloadLength == null) throw new Exception("Unable to read frame; unexpected end of stream.");

            byte[] payload = new byte[checked((int)payloadLength)];
            int totalBytesRead = 0;

            while (totalBytesRead != payloadLength)
            {
                int bytesRead = await _stream.ReadAsync(payload.AsMemory(totalBytesRead)).ConfigureAwait(false);
                if (bytesRead == 0) throw new Exception("Unable to read frame; unexpected end of stream.");

                totalBytesRead += bytesRead;
            }

            return (frameType, payload);
        }

        public async Task<long?> ReadInteger()
        {
            byte[] buffer = new byte[MaximumVarIntBytes];
            int bufferActiveLength = 0;

            long integerValue;
            int bytesRead;

            do
            {
                bytesRead = await _stream.ReadAsync(buffer.AsMemory(bufferActiveLength++, 1)).ConfigureAwait(false);
                if (bytesRead == 0)
                {
                    return bufferActiveLength == 1 ? (long?)null : throw new Exception("Unable to read varint; unexpected end of stream.");
                }
                Debug.Assert(bytesRead == 1);
            }
            while (!TryDecodeHttpInteger(buffer.AsSpan(0, bufferActiveLength), out integerValue, out bytesRead));

            Debug.Assert(bytesRead == bufferActiveLength);

            return integerValue;
        }

        static bool TryDecodeHttpInteger(ReadOnlySpan<byte> buffer, out long value, out int bytesRead)
        {
            const byte LengthMask = 0xC0;
            const byte LengthOneByte = 0x00;
            const byte LengthTwoByte = 0x40;
            const byte LengthFourByte = 0x80;
            const byte LengthEightByte = 0xC0;

            const uint TwoByteSubtract = 0x4000;
            const uint FourByteSubtract = 0x80000000;
            const ulong EightByteSubtract = 0xC000000000000000;

            if (buffer.Length != 0)
            {
                byte firstByte = buffer[0];

                switch (firstByte & LengthMask)
                {
                    case LengthOneByte:
                        value = firstByte;
                        bytesRead = 1;
                        return true;
                    case LengthTwoByte:
                        if (BinaryPrimitives.TryReadUInt16BigEndian(buffer, out ushort serializedShort))
                        {
                            value = serializedShort - TwoByteSubtract;
                            bytesRead = 2;
                            return true;
                        }
                        break;
                    case LengthFourByte:
                        if (BinaryPrimitives.TryReadUInt32BigEndian(buffer, out uint serializedInt))
                        {
                            value = serializedInt - FourByteSubtract;
                            bytesRead = 4;
                            return true;
                        }
                        break;
                    default: // LengthEightByte
                        Debug.Assert((firstByte & LengthMask) == LengthEightByte);
                        if (BinaryPrimitives.TryReadUInt64BigEndian(buffer, out ulong serializedLong))
                        {
                            value = (long)(serializedLong - EightByteSubtract);
                            Debug.Assert(value >= 0 && value <= VarIntMax, "Serialized values are within [0, 2^62).");

                            bytesRead = 8;
                            return true;
                        }
                        break;
                }
            }

            value = 0;
            bytesRead = 0;
            return false;
        }
    }

}

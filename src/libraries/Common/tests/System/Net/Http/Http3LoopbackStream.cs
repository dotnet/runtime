// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Quic;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    public sealed class Http3LoopbackStream : IAsyncDisposable
    {
        private const int MaximumVarIntBytes = 8;
        private const long VarIntMax = (1L << 62) - 1;

        public const long DataFrame = 0x0;
        public const long HeadersFrame = 0x1;
        public const long SettingsFrame = 0x4;
        public const long GoAwayFrame = 0x7;

        public const long ControlStream = 0x0;
        public const long PushStream = 0x1;

        public const long MaxHeaderListSize = 0x6;

        public QuicStream Stream { get; }

        public bool CanRead => Stream.CanRead;
        public bool CanWrite => Stream.CanWrite;

        public Http3LoopbackStream(QuicStream stream)
        {
            Stream = stream;
        }

        public ValueTask DisposeAsync() => Stream.DisposeAsync();

        public long StreamId => Stream.Id;

        public async Task<HttpRequestData> HandleRequestAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "")
        {
            HttpRequestData request = await ReadRequestDataAsync().ConfigureAwait(false);
            await SendResponseAsync(statusCode, headers, content).ConfigureAwait(false);
            return request;
        }

        public async Task SendUnidirectionalStreamTypeAsync(long streamType)
        {
            var buffer = new byte[MaximumVarIntBytes];
            int bytesWritten = EncodeHttpInteger(streamType, buffer);
            await Stream.WriteAsync(buffer.AsMemory(0, bytesWritten)).ConfigureAwait(false);
        }

        public async Task SendSettingsFrameAsync(SettingsEntry[] settingsEntries)
        {
            var buffer = new byte[settingsEntries.Length * MaximumVarIntBytes * 2];

            int bytesWritten = 0;

            foreach (SettingsEntry setting in settingsEntries)
            {
                bytesWritten += EncodeHttpInteger((int)setting.SettingId, buffer.AsSpan(bytesWritten));
                bytesWritten += EncodeHttpInteger(setting.Value, buffer.AsSpan(bytesWritten));
            }

            await SendFrameAsync(SettingsFrame, buffer.AsMemory(0, bytesWritten)).ConfigureAwait(false);
        }

        private Memory<byte> ConstructHeadersPayload(HttpStatusCode? statusCode, IEnumerable<HttpHeaderData> headers, bool qpackEncodeStatus = false)
        {
            int bufferLength = QPackTestEncoder.MaxPrefixLength;

            if (statusCode.HasValue)
            {
                if (qpackEncodeStatus)
                {
                    bufferLength += QPackTestEncoder.MaxVarIntLength * 2 + ":status".Length + 3;
                }
                else
                {
                    headers = headers.Prepend(new HttpHeaderData(":status", ((int)statusCode.Value).ToString(CultureInfo.InvariantCulture)));
                };
            }

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

            if (statusCode.HasValue && qpackEncodeStatus)
            {
                bytesWritten += QPackTestEncoder.EncodeStatusCode((int)statusCode.Value, buffer.AsSpan(bytesWritten));
            }

            foreach (HttpHeaderData header in headers)
            {
                bytesWritten += QPackTestEncoder.EncodeHeader(buffer.AsSpan(bytesWritten), header.Name, header.Value, header.ValueEncoding, header.HuffmanEncoded ? QPackFlags.HuffmanEncode : QPackFlags.None);
            }

            return buffer.AsMemory(0, bytesWritten);
        }

        private async Task SendHeadersFrameAsync(HttpStatusCode? statusCode, IEnumerable<HttpHeaderData> headers, bool qpackEncodeStatus = false)
        {
            await SendFrameAsync(HeadersFrame, ConstructHeadersPayload(statusCode, headers, qpackEncodeStatus)).ConfigureAwait(false);
        }

        private async Task SendPartialHeadersFrameAsync(HttpStatusCode? statusCode, IEnumerable<HttpHeaderData> headers)
        {
            Memory<byte> payload = ConstructHeadersPayload(statusCode, headers);

            await SendFrameHeaderAsync(HeadersFrame, payload.Length).ConfigureAwait(false);

            // Slice off final byte so the payload is not complete
            payload = payload.Slice(0, payload.Length - 1);

            await Stream.WriteAsync(payload).ConfigureAwait(false);
        }

        public async Task SendDataFrameAsync(ReadOnlyMemory<byte> data)
        {
            await SendFrameAsync(DataFrame, data).ConfigureAwait(false);
        }

        // Note that unlike HTTP2, the stream ID here indicates the *first invalid* stream.
        public async Task SendGoAwayFrameAsync(long firstInvalidStreamId)
        {
            var buffer = new byte[QPackTestEncoder.MaxVarIntLength];
            int bytesWritten = 0;

            bytesWritten += EncodeHttpInteger(firstInvalidStreamId, buffer);
            await SendFrameAsync(GoAwayFrame, buffer.AsMemory(0, bytesWritten)).ConfigureAwait(false);
        }

        private async Task SendFrameHeaderAsync(long frameType, int payloadLength)
        {
            var buffer = new byte[MaximumVarIntBytes * 2];

            int bytesWritten = 0;

            bytesWritten += EncodeHttpInteger(frameType, buffer.AsSpan(bytesWritten));
            bytesWritten += EncodeHttpInteger(payloadLength, buffer.AsSpan(bytesWritten));

            await Stream.WriteAsync(buffer.AsMemory(0, bytesWritten)).ConfigureAwait(false);
        }

        public async Task SendFrameAsync(long frameType, ReadOnlyMemory<byte> framePayload)
        {
            await SendFrameHeaderAsync(frameType, framePayload.Length).ConfigureAwait(false);
            await Stream.WriteAsync(framePayload).ConfigureAwait(false);
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

        public async Task<byte[]> ReadRequestBodyAsync(int minimumBytes = -1)
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
                if (minimumBytes >= 0 && buffer.Length >= minimumBytes)
                {
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

        public async Task SendResponseAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "", bool isFinal = true)
        {
            IEnumerable<HttpHeaderData> newHeaders = headers ?? Enumerable.Empty<HttpHeaderData>();

            if (content != null && !newHeaders.Any(x => x.Name == "Content-Length"))
            {
                newHeaders = newHeaders.Append(new HttpHeaderData("Content-Length", content.Length.ToString(CultureInfo.InvariantCulture)));
            }

            await SendResponseHeadersAsync(statusCode, newHeaders).ConfigureAwait(false);
            await SendResponseBodyAsync(Encoding.UTF8.GetBytes(content ?? ""), isFinal).ConfigureAwait(false);
        }

        private IEnumerable<HttpHeaderData> PrepareHeaders(IEnumerable<HttpHeaderData> headers)
        {
            headers ??= Enumerable.Empty<HttpHeaderData>();

            // Some tests use Content-Length with a null value to indicate Content-Length should not be set.
            headers = headers.Where(x => x.Name != "Content-Length" || x.Value != null);

            return headers;
        }

        public async Task SendResponseHeadersAsync(HttpStatusCode? statusCode = HttpStatusCode.OK, IEnumerable<HttpHeaderData> headers = null)
        {
            headers = PrepareHeaders(headers);
            await SendHeadersFrameAsync(statusCode, headers).ConfigureAwait(false);
        }

        public async Task SendResponseHeadersWithEncodedStatusAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IEnumerable<HttpHeaderData> headers = null)
        {
            headers = PrepareHeaders(headers);
            await SendHeadersFrameAsync(statusCode, headers, qpackEncodeStatus: true).ConfigureAwait(false);
        }

        public async Task SendPartialResponseHeadersAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IEnumerable<HttpHeaderData> headers = null)
        {
            headers = PrepareHeaders(headers);
            await SendPartialHeadersFrameAsync(statusCode, headers).ConfigureAwait(false);
        }

        public async Task SendResponseBodyAsync(byte[] content, bool isFinal = true)
        {
            if (content?.Length != 0)
            {
                await SendDataFrameAsync(content).ConfigureAwait(false);
            }

            if (isFinal)
            {
                Stream.CompleteWrites();
            }
        }

        public async Task<List<(long settingId, long settingValue)>> ReadSettingsAsync()
        {
            (long? frameType, byte[] payload) = await ReadFrameAsync().ConfigureAwait(false);

            if (frameType == null) throw new Exception("Unable to read settings; unexpected end of stream.");
            if (frameType != SettingsFrame) throw new Exception($"Unable to read settings; received frame type 0x{frameType:x}.");

            return ParseSettingsPayload(payload);
        }

        private List<(long settingId, long settingValue)> ParseSettingsPayload(ReadOnlySpan<byte> settingsPayload)
        {
            var settings = new List<(long settingId, long settingValue)>();

            while (settingsPayload.Length != 0)
            {
                if (!TryDecodeHttpInteger(settingsPayload, out long settingId, out int bytesRead))
                {
                    throw new Exception("Unable to read setting ID; unexpected end of payload.");
                }

                settingsPayload = settingsPayload.Slice(bytesRead);

                if (!TryDecodeHttpInteger(settingsPayload, out long settingValue, out bytesRead))
                {
                    throw new Exception($"Unable to read value for setting 0x{settingId:x}; unexpected end of payload.");
                }

                settingsPayload = settingsPayload.Slice(bytesRead);
                settings.Add((settingId, settingValue));
            }

            return settings;
        }

        private HttpRequestData ParseHeaders(ReadOnlySpan<byte> buffer)
        {
            HttpRequestData request = new HttpRequestData { RequestId = Http3LoopbackConnection.GetRequestId(Stream) };

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
            request.Version = HttpVersion.Version30;

            return request;
        }

        public async Task WaitForCancellationAsync(bool ignoreIncomingData = true)
        {
            bool readCanceled = false;
            bool writeCanceled = false;

            async Task WaitForReadCancellation()
            {
                try
                {
                    if (ignoreIncomingData)
                    {
                        await DrainResponseData().ConfigureAwait(false);
                    }
                    else
                    {
                        int bytesRead = await Stream.ReadAsync(new byte[1]).ConfigureAwait(false);
                        if (bytesRead != 0)
                        {
                            throw new Exception($"Unexpected data received while waiting for client cancllation.");
                        }
                    }
                }
                catch (QuicException ex) when (ex.QuicError == QuicError.StreamAborted && ex.ApplicationErrorCode == Http3LoopbackConnection.H3_REQUEST_CANCELLED)
                {
                    readCanceled = true;
                }
            }

            async Task WaitForWriteCancellation()
            {
                try
                {
                    await Stream.WritesClosed.ConfigureAwait(false);
                }
                catch (QuicException ex) when (ex.QuicError == QuicError.StreamAborted && ex.ApplicationErrorCode == Http3LoopbackConnection.H3_REQUEST_CANCELLED)
                {
                    writeCanceled = true;
                }
            }

            await Task.WhenAll(WaitForReadCancellation(), WaitForWriteCancellation()).ConfigureAwait(false);

            if (!readCanceled && !writeCanceled)
            {
                throw new Exception("Both read and write completed successfully; expected clien cancellation");
            }
        }

        private async Task DrainResponseData()
        {
            while (true)
            {
                (long? frameType, _) = await ReadFrameAsync().ConfigureAwait(false);

                switch (frameType)
                {
                    case null:
                        // end of stream reached.
                        return;
                    case DataFrame:
                        break;
                    default:
                        throw new Exception($"Unexpected frame type {frameType} while draining response data.");
                }
            }
        }

        public void Abort(long errorCode, QuicAbortDirection direction = QuicAbortDirection.Both)
        {
            Stream.Abort(direction, errorCode);
        }

        public async Task<(long? frameType, byte[] payload)> ReadFrameAsync()
        {
            long? frameType = await ReadIntegerAsync().ConfigureAwait(false);
            if (frameType == null) return (null, null);

            long? payloadLength = await ReadIntegerAsync().ConfigureAwait(false);
            if (payloadLength == null) throw new Exception("Unable to read frame; unexpected end of stream.");

            byte[] payload = new byte[checked((int)payloadLength)];
            int totalBytesRead = 0;

            while (totalBytesRead != payloadLength)
            {
                int bytesRead = await Stream.ReadAsync(payload.AsMemory(totalBytesRead)).ConfigureAwait(false);
                if (bytesRead == 0) throw new Exception("Unable to read frame; unexpected end of stream.");

                totalBytesRead += bytesRead;
            }

            return (frameType, payload);
        }

        public async Task<long?> ReadIntegerAsync()
        {
            byte[] buffer = new byte[MaximumVarIntBytes];
            int bufferActiveLength = 0;

            long integerValue;
            int bytesRead;

            do
            {
                bytesRead = await Stream.ReadAsync(buffer.AsMemory(bufferActiveLength++, 1)).ConfigureAwait(false);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Functional.Tests;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Test.Common
{
    public class Http2LoopbackConnection : GenericLoopbackConnection
    {
        public const string Http2Prefix = "PRI * HTTP/2.0\r\n\r\nSM\r\n\r\n";

        private Socket _connectionSocket;
        private Stream _connectionStream;
        private TaskCompletionSource<bool> _ignoredSettingsAckPromise;
        private bool _ignoreWindowUpdates;
        private readonly TimeSpan _timeout;
        private int _lastStreamId;

        private readonly byte[] _prefix = new byte[24];
        public string PrefixString => Encoding.UTF8.GetString(_prefix, 0, _prefix.Length);
        public bool IsInvalid => _connectionSocket == null;
        public Stream Stream => _connectionStream;
        public Task<bool> SettingAckWaiter => _ignoredSettingsAckPromise?.Task;

        private Http2LoopbackConnection(Socket socket, Stream stream, TimeSpan timeout)
        {
            _connectionSocket = socket;
            _connectionStream = stream;
            _timeout = timeout;
        }

        public static Task<Http2LoopbackConnection> CreateAsync(Socket socket, Stream stream, Http2Options httpOptions)
        {
            return CreateAsync(socket, stream, httpOptions, Http2LoopbackServer.Timeout);
        }

        public static async Task<Http2LoopbackConnection> CreateAsync(Socket socket, Stream stream, Http2Options httpOptions, TimeSpan timeout)
        {
            if (httpOptions.UseSsl)
            {
                var sslStream = new SslStream(stream, false, delegate { return true; });

                using (X509Certificate2 cert = Configuration.Certificates.GetServerCertificate())
                {
#if !NETFRAMEWORK
                    SslServerAuthenticationOptions options = new SslServerAuthenticationOptions();

                    options.EnabledSslProtocols = httpOptions.SslProtocols;

                    var protocols = new List<SslApplicationProtocol>();
                    protocols.Add(SslApplicationProtocol.Http2);
                    options.ApplicationProtocols = protocols;

                    options.ServerCertificate = cert;

                    options.ClientCertificateRequired = httpOptions.ClientCertificateRequired;

                    await sslStream.AuthenticateAsServerAsync(options, CancellationToken.None).ConfigureAwait(false);
#else
                    await sslStream.AuthenticateAsServerAsync(cert, httpOptions.ClientCertificateRequired, httpOptions.SslProtocols, checkCertificateRevocation: false).ConfigureAwait(false);
#endif
                }

                stream = sslStream;
            }

            var con = new Http2LoopbackConnection(socket, stream, timeout);
            await con.ReadPrefixAsync().ConfigureAwait(false);

            return con;
        }

        private async Task ReadPrefixAsync()
        {
            if (!await FillBufferAsync(_prefix))
            {
                throw new Exception("Connection stream closed while attempting to read connection preface.");
            }

            if (Text.Encoding.ASCII.GetString(_prefix).Contains("HTTP/1.1"))
            {
                throw new Exception("HTTP 1.1 request received.");
            }
        }

        public async Task SendConnectionPrefaceAsync()
        {
            // Send the initial server settings frame.
            Frame emptySettings = new Frame(0, FrameType.Settings, FrameFlags.None, 0);
            await WriteFrameAsync(emptySettings).ConfigureAwait(false);

            // Receive and ACK the client settings frame.
            Frame clientSettings = await ReadFrameAsync(_timeout).ConfigureAwait(false);
            clientSettings.Flags = clientSettings.Flags | FrameFlags.Ack;
            await WriteFrameAsync(clientSettings).ConfigureAwait(false);

            // Receive the client ACK of the server settings frame.
            clientSettings = await ReadFrameAsync(_timeout).ConfigureAwait(false);
        }

        public async Task WriteFrameAsync(Frame frame)
        {
            byte[] writeBuffer = new byte[Frame.FrameHeaderLength + frame.Length];
            frame.WriteTo(writeBuffer);
            await _connectionStream.WriteAsync(writeBuffer, 0, writeBuffer.Length).ConfigureAwait(false);
        }

        // Read until the buffer is full
        // Return false on EOF, throw on partial read
        private async Task<bool> FillBufferAsync(Memory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            int readBytes = await _connectionStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (readBytes == 0)
            {
                return false;
            }

            buffer = buffer.Slice(readBytes);
            while (buffer.Length > 0)
            {
                readBytes = await _connectionStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (readBytes == 0)
                {
                    throw new Exception("Connection closed when expecting more data.");
                }

                buffer = buffer.Slice(readBytes);
            }

            return true;
        }

        public async Task<Frame> ReadFrameAsync(TimeSpan timeout)
        {
            using CancellationTokenSource timeoutCts = new CancellationTokenSource(timeout);
            return await ReadFrameAsync(timeoutCts.Token).ConfigureAwait(false);
        }

        private async Task<Frame> ReadFrameAsync(CancellationToken cancellationToken)
        {
            // First read the frame headers, which should tell us how long the rest of the frame is.
            byte[] headerBytes = new byte[Frame.FrameHeaderLength];

            try
            {
                if (!await FillBufferAsync(headerBytes, cancellationToken).ConfigureAwait(false))
                {
                    return null;
                }
            }
            catch (IOException)
            {
                // eat errors when client aborts connection and return null.
                return null;
            }

            Frame header = Frame.ReadFrom(headerBytes);

            // Read the data segment of the frame, if it is present.
            byte[] data = new byte[header.Length];
            if (header.Length > 0 && !await FillBufferAsync(data, cancellationToken).ConfigureAwait(false))
            {
                throw new Exception("Connection stream closed while attempting to read frame body.");
            }

            if (_ignoredSettingsAckPromise != null && header.Type == FrameType.Settings && header.Flags == FrameFlags.Ack)
            {
                _ignoredSettingsAckPromise.TrySetResult(true);
                _ignoredSettingsAckPromise = null;
                return await ReadFrameAsync(cancellationToken).ConfigureAwait(false);
            }

            if (_ignoreWindowUpdates && header.Type == FrameType.WindowUpdate)
            {
                return await ReadFrameAsync(cancellationToken).ConfigureAwait(false);
            }

            // Construct the correct frame type and return it.
            switch (header.Type)
            {
                case FrameType.Settings:
                    return SettingsFrame.ReadFrom(header, data);
                case FrameType.Data:
                    return DataFrame.ReadFrom(header, data);
                case FrameType.Headers:
                    return HeadersFrame.ReadFrom(header, data);
                case FrameType.Priority:
                    return PriorityFrame.ReadFrom(header, data);
                case FrameType.RstStream:
                    return RstStreamFrame.ReadFrom(header, data);
                case FrameType.Ping:
                    return PingFrame.ReadFrom(header, data);
                case FrameType.GoAway:
                    return GoAwayFrame.ReadFrom(header, data);
                default:
                    return header;
            }
        }

        // Reset and return underlying networking objects.
        public (Socket, Stream) ResetNetwork()
        {
            Socket oldSocket = _connectionSocket;
            Stream oldStream = _connectionStream;
            _connectionSocket = null;
            _connectionStream = null;
            _ignoredSettingsAckPromise = null;

            return (oldSocket, oldStream);
        }

        // Set up loopback server to silently ignore the next inbound settings ack frame.
        // If there already is a pending ack frame, wait until it has been read.
        public async Task ExpectSettingsAckAsync(int timeoutMs = 5000)
        {
            // The timing of when we receive the settings ack is not guaranteed.
            // To simplify frame processing, just record that we are expecting one,
            // and then filter it out in ReadFrameAsync above.

            Task currentTask = _ignoredSettingsAckPromise?.Task;
            if (currentTask != null)
            {
                var timeout = TimeSpan.FromMilliseconds(timeoutMs);
                await currentTask.TimeoutAfter(timeout);
            }

            _ignoredSettingsAckPromise = new TaskCompletionSource<bool>();
        }

        public void IgnoreWindowUpdates()
        {
            _ignoreWindowUpdates = true;
        }

        public async Task ReadRstStreamAsync(int streamId)
        {
            Frame frame = await ReadFrameAsync(_timeout);

            if (frame == null)
            {
                throw new Exception($"Expected RST_STREAM, saw EOF");
            }

            if (frame.Type != FrameType.RstStream)
            {
                throw new Exception($"Expected RST_STREAM, saw {frame.Type}");
            }

            if (frame.StreamId != streamId)
            {
                throw new Exception($"Expected RST_STREAM on stream {streamId}, actual streamId={frame.StreamId}");
            }
        }

        // Wait for the client to close the connection, e.g. after the HttpClient is disposed.
        public async Task WaitForClientDisconnectAsync(bool ignoreUnexpectedFrames = false)
        {
            IgnoreWindowUpdates();

            Frame frame = await ReadFrameAsync(_timeout).ConfigureAwait(false);
            if (frame != null)
            {
                if (!ignoreUnexpectedFrames)
                {
                    throw new Exception($"Unexpected frame received while waiting for client disconnect: {frame}");
                }
            }

            _connectionStream.Close();

            _connectionSocket = null;
            _connectionStream = null;

            _ignoredSettingsAckPromise = null;
            _ignoreWindowUpdates = false;
        }

        public void ShutdownSend()
        {
            _connectionSocket?.Shutdown(SocketShutdown.Send);
        }

        // This will cause a server-initiated shutdown of the connection.
        // For normal operation, you should send a GOAWAY and complete any remaining streams
        // before calling this method.
        public async Task WaitForConnectionShutdownAsync(bool ignoreUnexpectedFrames = false)
        {
            // Shutdown our send side, so the client knows there won't be any more frames coming.
            ShutdownSend();

            await WaitForClientDisconnectAsync(ignoreUnexpectedFrames: ignoreUnexpectedFrames);
        }

        // This is similar to WaitForConnectionShutdownAsync but will send GOAWAY for you
        // and will ignore any errors if client has already shutdown
        public async Task ShutdownIgnoringErrorsAsync(int lastStreamId, ProtocolErrors errorCode = ProtocolErrors.NO_ERROR)
        {
            try
            {
                await SendGoAway(lastStreamId, errorCode).ConfigureAwait(false);
                await WaitForConnectionShutdownAsync(ignoreUnexpectedFrames: true).ConfigureAwait(false);
            }
            catch (IOException)
            {
                // Ignore connection errors
            }
            catch (SocketException)
            {
                // Ignore connection errors
            }
        }

        public async Task<int> ReadRequestHeaderAsync()
        {
            HeadersFrame frame = await ReadRequestHeaderFrameAsync();
            return frame.StreamId;
        }

        public async Task<HeadersFrame> ReadRequestHeaderFrameAsync()
        {
            // Receive HEADERS frame for request.
            Frame frame = await ReadFrameAsync(_timeout).ConfigureAwait(false);
            if (frame == null)
            {
                throw new IOException("Failed to read Headers frame.");
            }

            Assert.Equal(FrameType.Headers, frame.Type);
            Assert.Equal(FrameFlags.EndHeaders | FrameFlags.EndStream, frame.Flags);
            return (HeadersFrame)frame;
        }

        private static (int bytesConsumed, int value) DecodeInteger(ReadOnlySpan<byte> headerBlock, byte prefixMask)
        {
            return QPackTestDecoder.DecodeInteger(headerBlock, prefixMask);
        }

        private static (int bytesConsumed, string value) DecodeString(ReadOnlySpan<byte> headerBlock)
        {
            (int bytesConsumed, int stringLength) = DecodeInteger(headerBlock, 0b01111111);
            if ((headerBlock[0] & 0b10000000) != 0)
            {
                // Huffman encoded
                byte[] buffer = new byte[stringLength * 2];
                int bytesDecoded = HuffmanDecoder.Decode(headerBlock.Slice(bytesConsumed, stringLength), buffer);
                string value = Encoding.ASCII.GetString(buffer, 0, bytesDecoded);
                return (bytesConsumed + stringLength, value);
            }
            else
            {
                string value = Encoding.ASCII.GetString(headerBlock.Slice(bytesConsumed, stringLength).ToArray());
                return (bytesConsumed + stringLength, value);
            }
        }

        private static readonly HttpHeaderData[] s_staticTable = new HttpHeaderData[]
        {
            new HttpHeaderData(":authority", ""),
            new HttpHeaderData(":method", "GET"),
            new HttpHeaderData(":method", "POST"),
            new HttpHeaderData(":path", "/"),
            new HttpHeaderData(":path", "/index.html"),
            new HttpHeaderData(":scheme", "http"),
            new HttpHeaderData(":scheme", "https"),
            new HttpHeaderData(":status", "200"),
            new HttpHeaderData(":status", "204"),
            new HttpHeaderData(":status", "206"),
            new HttpHeaderData(":status", "304"),
            new HttpHeaderData(":status", "400"),
            new HttpHeaderData(":status", "404"),
            new HttpHeaderData(":status", "500"),
            new HttpHeaderData("accept-charset", ""),
            new HttpHeaderData("accept-encoding", "gzip, deflate"),
            new HttpHeaderData("accept-language", ""),
            new HttpHeaderData("accept-ranges", ""),
            new HttpHeaderData("accept", ""),
            new HttpHeaderData("access-control-allow-origin", ""),
            new HttpHeaderData("age", ""),
            new HttpHeaderData("allow", ""),
            new HttpHeaderData("authorization", ""),
            new HttpHeaderData("cache-control", ""),
            new HttpHeaderData("content-disposition", ""),
            new HttpHeaderData("content-encoding", ""),
            new HttpHeaderData("content-language", ""),
            new HttpHeaderData("content-length", ""),
            new HttpHeaderData("content-location", ""),
            new HttpHeaderData("content-range", ""),
            new HttpHeaderData("content-type", ""),
            new HttpHeaderData("cookie", ""),
            new HttpHeaderData("date", ""),
            new HttpHeaderData("etag", ""),
            new HttpHeaderData("expect", ""),
            new HttpHeaderData("expires", ""),
            new HttpHeaderData("from", ""),
            new HttpHeaderData("host", ""),
            new HttpHeaderData("if-match", ""),
            new HttpHeaderData("if-modified-since", ""),
            new HttpHeaderData("if-none-match", ""),
            new HttpHeaderData("if-range", ""),
            new HttpHeaderData("if-unmodified-since", ""),
            new HttpHeaderData("last-modified", ""),
            new HttpHeaderData("link", ""),
            new HttpHeaderData("location", ""),
            new HttpHeaderData("max-forwards", ""),
            new HttpHeaderData("proxy-authenticate", ""),
            new HttpHeaderData("proxy-authorization", ""),
            new HttpHeaderData("range", ""),
            new HttpHeaderData("referer", ""),
            new HttpHeaderData("refresh", ""),
            new HttpHeaderData("retry-after", ""),
            new HttpHeaderData("server", ""),
            new HttpHeaderData("set-cookie", ""),
            new HttpHeaderData("strict-transport-security", ""),
            new HttpHeaderData("transfer-encoding", ""),
            new HttpHeaderData("user-agent", ""),
            new HttpHeaderData("vary", ""),
            new HttpHeaderData("via", ""),
            new HttpHeaderData("www-authenticate", "")
        };

        private static HttpHeaderData GetHeaderForIndex(int index)
        {
            return s_staticTable[index - 1];
        }

        private static (int bytesConsumed, HttpHeaderData headerData) DecodeLiteralHeader(ReadOnlySpan<byte> headerBlock, byte prefixMask)
        {
            int i = 0;

            (int bytesConsumed, int index) = DecodeInteger(headerBlock, prefixMask);
            i += bytesConsumed;

            string name;
            if (index == 0)
            {
                (bytesConsumed, name) = DecodeString(headerBlock.Slice(i));
                i += bytesConsumed;
            }
            else
            {
                name = GetHeaderForIndex(index).Name;
            }

            string value;
            (bytesConsumed, value) = DecodeString(headerBlock.Slice(i));
            i += bytesConsumed;

            return (i, new HttpHeaderData(name, value));
        }

        private static (int bytesConsumed, HttpHeaderData headerData) DecodeHeader(ReadOnlySpan<byte> headerBlock)
        {
            int i = 0;

            byte b = headerBlock[0];
            if ((b & 0b10000000) != 0)
            {
                // Indexed header
                (int bytesConsumed, int index) = DecodeInteger(headerBlock, 0b01111111);
                i += bytesConsumed;

                return (i, GetHeaderForIndex(index));
            }
            else if ((b & 0b11000000) == 0b01000000)
            {
                // Literal with indexing
                return DecodeLiteralHeader(headerBlock, 0b00111111);
            }
            else if ((b & 0b11100000) == 0b00100000)
            {
                // Table size update
                throw new Exception("table size update not supported");
            }
            else
            {
                // Literal, never indexed
                return DecodeLiteralHeader(headerBlock, 0b00001111);
            }
        }

        public async Task<byte[]> ReadBodyAsync(bool expectEndOfStream = false)
        {
            byte[] body = null;
            Frame frame;

            do
            {
                frame = await ReadFrameAsync(_timeout).ConfigureAwait(false);
                if (frame == null && expectEndOfStream)
                {
                    break;
                }
                else if (frame == null || frame.Type == FrameType.RstStream)
                {
                    throw new IOException( frame == null ? "End of stream" : "Got RST");
                }

                Assert.Equal(FrameType.Data, frame.Type);

                if (frame.Length > 1)
                {
                    DataFrame dataFrame = (DataFrame)frame;

                    if (body == null)
                    {
                        body = dataFrame.Data.ToArray();
                    }
                    else
                    {
                        byte[] newBuffer = new byte[body.Length + dataFrame.Data.Length];

                        body.CopyTo(newBuffer, 0);
                        dataFrame.Data.Span.CopyTo(newBuffer.AsSpan().Slice(body.Length));
                        body= newBuffer;
                    }
                }
            }
            while ((frame.Flags & FrameFlags.EndStream) == 0);

            return body;
        }

        public async Task<(int streamId, HttpRequestData requestData)> ReadAndParseRequestHeaderAsync(bool readBody = true)
        {
            HttpRequestData requestData = new HttpRequestData();

            // Receive HEADERS frame for request.
            Frame frame = await ReadFrameAsync(_timeout).ConfigureAwait(false);
            if (frame == null)
            {
                throw new IOException("Failed to read Headers frame.");
            }
            Assert.Equal(FrameType.Headers, frame.Type);
            HeadersFrame headersFrame = (HeadersFrame) frame;

            // TODO CONTINUATION support
            Assert.Equal(FrameFlags.EndHeaders, FrameFlags.EndHeaders & headersFrame.Flags);

            int streamId = headersFrame.StreamId;
            requestData.RequestId = streamId;

            Memory<byte> data = headersFrame.Data;
            int i = 0;
            while (i < data.Length)
            {
                (int bytesConsumed, HttpHeaderData headerData) = DecodeHeader(data.Span.Slice(i));

                byte[] headerRaw = data.Span.Slice(i, bytesConsumed).ToArray();
                headerData = new HttpHeaderData(headerData.Name, headerData.Value, headerData.HuffmanEncoded, headerRaw);

                requestData.Headers.Add(headerData);
                i += bytesConsumed;
            }

            // Extract method and path
            requestData.Method = requestData.GetSingleHeaderValue(":method");
            requestData.Path = requestData.GetSingleHeaderValue(":path");

            if (readBody && (frame.Flags & FrameFlags.EndStream) == 0)
            {
                // Read body until end of stream if needed.
                requestData.Body = await ReadBodyAsync().ConfigureAwait(false);
            }

            return (streamId, requestData);
        }

        public override Task InitializeConnectionAsync()
        {
            return ReadAndSendSettingsAsync(ackTimeout: null);
        }

        public async Task<SettingsFrame> ReadAndSendSettingsAsync(TimeSpan? ackTimeout, params SettingsEntry[] settingsEntries)
        {
            // Receive the initial client settings frame.
            Frame receivedFrame = await ReadFrameAsync(_timeout).ConfigureAwait(false);
            Assert.Equal(FrameType.Settings, receivedFrame.Type);
            Assert.Equal(FrameFlags.None, receivedFrame.Flags);
            Assert.Equal(0, receivedFrame.StreamId);

            var clientSettingsFrame = (SettingsFrame)receivedFrame;

            // Receive the initial client window update frame.
            receivedFrame = await ReadFrameAsync(_timeout).ConfigureAwait(false);
            Assert.Equal(FrameType.WindowUpdate, receivedFrame.Type);
            Assert.Equal(FrameFlags.None, receivedFrame.Flags);
            Assert.Equal(0, receivedFrame.StreamId);

            // Send the initial server settings frame.
            SettingsFrame settingsFrame = new SettingsFrame(settingsEntries);
            await WriteFrameAsync(settingsFrame).ConfigureAwait(false);

            // Send the client settings frame ACK.
            Frame settingsAck = new Frame(0, FrameType.Settings, FrameFlags.Ack, 0);
            await WriteFrameAsync(settingsAck).ConfigureAwait(false);

            // The client will send us a SETTINGS ACK eventually, but not necessarily right away.
            await ExpectSettingsAckAsync((int?)ackTimeout?.TotalMilliseconds ?? 5000);

            return clientSettingsFrame;
        }

        public async Task SendGoAway(int lastStreamId, ProtocolErrors errorCode = ProtocolErrors.NO_ERROR)
        {
            GoAwayFrame frame = new GoAwayFrame(lastStreamId, (int)errorCode, new byte[] { }, 0);
            await WriteFrameAsync(frame).ConfigureAwait(false);
        }

        public async Task PingPong()
        {
            byte[] pingData = new byte[8] { 1, 2, 3, 4, 50, 60, 70, 80 };
            PingFrame ping = new PingFrame(pingData, FrameFlags.None, 0);
            await WriteFrameAsync(ping).ConfigureAwait(false);
            PingFrame pingAck = (PingFrame)await ReadFrameAsync(_timeout).ConfigureAwait(false);
            if (pingAck == null || pingAck.Type != FrameType.Ping || !pingAck.AckFlag)
            {
                throw new Exception("Expected PING ACK");
            }

            Assert.Equal(pingData, pingAck.Data);
        }

        public async Task SendDefaultResponseHeadersAsync(int streamId)
        {
            byte[] headers = new byte[] { 0x88 };   // Encoding for ":status: 200"

            HeadersFrame headersFrame = new HeadersFrame(headers, FrameFlags.EndHeaders, 0, 0, 0, streamId);
            await WriteFrameAsync(headersFrame).ConfigureAwait(false);
        }

        public async Task SendDefaultResponseAsync(int streamId)
        {
            byte[] headers = new byte[] { 0x88 };   // Encoding for ":status: 200"

            HeadersFrame headersFrame = new HeadersFrame(headers, FrameFlags.EndHeaders | FrameFlags.EndStream, 0, 0, 0, streamId);
            await WriteFrameAsync(headersFrame).ConfigureAwait(false);
        }

        public async Task SendResponseHeadersAsync(int streamId, bool endStream = true, HttpStatusCode statusCode = HttpStatusCode.OK, bool isTrailingHeader = false, bool endHeaders = true, IList<HttpHeaderData> headers = null)
        {
            // For now, only support headers that fit in a single frame
            byte[] headerBlock = new byte[Frame.MaxFrameLength];
            int bytesGenerated = 0;

            if (!isTrailingHeader)
            {
                string statusCodeString = ((int)statusCode).ToString();
                bytesGenerated += HPackEncoder.EncodeHeader(":status", statusCodeString, HPackFlags.None, headerBlock.AsSpan());
            }

            if (headers != null)
            {
                foreach (HttpHeaderData headerData in headers)
                {
                    bytesGenerated += HPackEncoder.EncodeHeader(headerData.Name, headerData.Value, headerData.ValueEncoding, headerData.HuffmanEncoded ? HPackFlags.HuffmanEncode : HPackFlags.None, headerBlock.AsSpan(bytesGenerated));
                }
            }

            FrameFlags flags = endHeaders ? FrameFlags.EndHeaders : FrameFlags.None;
            if (endStream)
            {
                flags |= FrameFlags.EndStream;
            }

            HeadersFrame headersFrame = new HeadersFrame(headerBlock.AsMemory().Slice(0, bytesGenerated), flags, 0, 0, 0, streamId);
            await WriteFrameAsync(headersFrame).ConfigureAwait(false);
        }

        public async Task SendResponseDataAsync(int streamId, ReadOnlyMemory<byte> responseData, bool endStream)
        {
            DataFrame dataFrame = new DataFrame(responseData, endStream ? FrameFlags.EndStream : FrameFlags.None, 0, streamId);
            await WriteFrameAsync(dataFrame).ConfigureAwait(false);
        }

        public async Task SendResponseBodyAsync(int streamId, ReadOnlyMemory<byte> responseBody, bool isFinal = true)
        {
            // Only support response body if it fits in a single frame, for now
            // In the future we should separate the body into chunks as needed,
            // and if it's larger than the default window size, we will need to process window updates as well.
            if (responseBody.Length > Frame.MaxFrameLength)
            {
                throw new Exception("Response body too long");
            }

            await SendResponseDataAsync(streamId, responseBody, isFinal).ConfigureAwait(false);
        }

        public override void Dispose()
        {
            // Might have been already shutdown manually via WaitForConnectionShutdownAsync which nulls the _connectionStream.
            if (_connectionStream != null)
            {
                ShutdownIgnoringErrorsAsync(_lastStreamId).GetAwaiter().GetResult();
            }
        }

        //
        // GenericLoopbackServer implementation
        //

        public override async Task<HttpRequestData> ReadRequestDataAsync(bool readBody = true)
        {
            (int streamId, HttpRequestData requestData) = await ReadAndParseRequestHeaderAsync(readBody).ConfigureAwait(false);
            _lastStreamId = streamId;

            return requestData;
        }

        public override Task<Byte[]> ReadRequestBodyAsync()
        {
            return ReadBodyAsync();
        }

        public override async Task SendResponseAsync(HttpStatusCode? statusCode = null, IList<HttpHeaderData> headers = null, string body = null, bool isFinal = true, int requestId = 0)
        {
            // TODO: Header continuation support.
            Assert.NotNull(statusCode);

            if (headers != null)
            {
                bool hasDate = false;
                bool stripContentLength = false;
                foreach (HttpHeaderData headerData in headers)
                {
                    // Check if we should inject Date header to match HTTP/1.
                    if (headerData.Name.Equals("Date", StringComparison.OrdinalIgnoreCase))
                    {
                        hasDate = true;
                    }
                    else if (headerData.Name.Equals("Content-Length") && headerData.Value == null)
                    {
                        // Hack used for Http/1 to avoid sending content-length header.
                        stripContentLength = true;
                    }
                }

                if (!hasDate || stripContentLength)
                {
                    var newHeaders = new List<HttpHeaderData>();
                    foreach (HttpHeaderData headerData in headers)
                    {
                        if (headerData.Name.Equals("Content-Length") && headerData.Value == null)
                        {
                            continue;
                        }

                        newHeaders.Add(headerData);
                    }
                    newHeaders.Add(new HttpHeaderData("Date", $"{DateTimeOffset.UtcNow:R}"));
                    headers = newHeaders;
                }
            }

            int streamId = requestId == 0 ? _lastStreamId : requestId;
            bool endHeaders = body != null || isFinal;

            if (string.IsNullOrEmpty(body))
            {
                await SendResponseHeadersAsync(streamId, endStream: isFinal, (HttpStatusCode)statusCode, endHeaders: endHeaders, headers: headers);
            }
            else
            {
                await SendResponseHeadersAsync(streamId, endStream: false, (HttpStatusCode)statusCode, endHeaders: endHeaders, headers: headers);
                await SendResponseBodyAsync(body, isFinal: isFinal, requestId: streamId);
            }
        }

        public override Task SendResponseHeadersAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, int requestId = 0)
        {
            int streamId = requestId == 0 ? _lastStreamId : requestId;
            return SendResponseHeadersAsync(streamId, endStream: false, statusCode, isTrailingHeader: false, endHeaders: true, headers);
        }

        public override Task SendResponseBodyAsync(byte[] body, bool isFinal = true, int requestId = 0)
        {
            int streamId = requestId == 0 ? _lastStreamId : requestId;
            return SendResponseBodyAsync(streamId, body, isFinal);
        }

        public override async Task<HttpRequestData> HandleRequestAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "")
        {
            (int streamId, HttpRequestData requestData) = await ReadAndParseRequestHeaderAsync().ConfigureAwait(false);

            // We are about to close the connection, after we send the response.
            // So, send a GOAWAY frame now so the client won't inadvertantly try to reuse the connection.
            await SendGoAway(streamId).ConfigureAwait(false);

            if (string.IsNullOrEmpty(content))
            {
                await SendResponseHeadersAsync(streamId, endStream: true, statusCode, isTrailingHeader: false, headers : headers).ConfigureAwait(false);
            }
            else
            {
                await SendResponseHeadersAsync(streamId, endStream: false, statusCode, isTrailingHeader: false, headers : headers).ConfigureAwait(false);
                await SendResponseBodyAsync(streamId, Encoding.ASCII.GetBytes(content)).ConfigureAwait(false);
            }

            await WaitForConnectionShutdownAsync().ConfigureAwait(false);

            return requestData;
        }

        public override async Task WaitForCancellationAsync(bool ignoreIncomingData = true, int requestId = 0)
        {
            int streamId = requestId == 0 ? _lastStreamId : requestId;

            Frame frame;
            do
            {
                frame = await ReadFrameAsync(TimeSpan.FromMilliseconds(TestHelper.PassingTestTimeoutMilliseconds));
                Assert.NotNull(frame); // We should get Rst before closing connection.
                Assert.Equal(0, (int)(frame.Flags & FrameFlags.EndStream));
                if (ignoreIncomingData)
                {
                    Assert.True(frame.Type == FrameType.Data || frame.Type == FrameType.RstStream, $"Expected Data or RstStream, got {frame.Type}");
                }
                else
                {
                    Assert.Equal(FrameType.RstStream, frame.Type);
                }
            } while (frame.Type != FrameType.RstStream);

            Assert.Equal(streamId, frame.StreamId);
        }
    }
}

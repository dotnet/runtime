// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Test.Common
{
    public class Http2LoopbackServer : IDisposable
    {
        private Socket _listenSocket;
        private Stream _connectionStream;
        private Http2Options _options;
        private Uri _uri;
        private bool _ignoreSettingsAck;
        private bool _ignoreWindowUpdates;

        public Uri Address
        {
            get
            {
                var localEndPoint = (IPEndPoint)_listenSocket.LocalEndPoint;
                string host = _options.Address.AddressFamily == AddressFamily.InterNetworkV6 ?
                    $"[{localEndPoint.Address}]" :
                    localEndPoint.Address.ToString();

                string scheme = _options.UseSsl ? "https" : "http";

                _uri = new Uri($"{scheme}://{host}:{localEndPoint.Port}/");

                return _uri;
            }
        }

        public static Http2LoopbackServer CreateServer()
        {
            return new Http2LoopbackServer(new Http2Options());
        }

        public static Http2LoopbackServer CreateServer(Http2Options options)
        {
            return new Http2LoopbackServer(options);
        }

        private Http2LoopbackServer(Http2Options options)
        {
            _options = options;
            _listenSocket = new Socket(_options.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            _listenSocket.Bind(new IPEndPoint(_options.Address, 0));
            _listenSocket.Listen(_options.ListenBacklog);
        }

        public async Task SendConnectionPrefaceAsync()
        {
            // Send the initial server settings frame.
            Frame emptySettings = new Frame(0, FrameType.Settings, FrameFlags.None, 0);
            await WriteFrameAsync(emptySettings).ConfigureAwait(false);

            // Receive and ACK the client settings frame.
            Frame clientSettings = await ReadFrameAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
            clientSettings.Flags = clientSettings.Flags | FrameFlags.Ack;
            await WriteFrameAsync(clientSettings).ConfigureAwait(false);

            // Receive the client ACK of the server settings frame.
            clientSettings = await ReadFrameAsync(TimeSpan.FromSeconds(30)).ConfigureAwait(false);
        }

        public async Task WriteFrameAsync(Frame frame)
        {
            byte[] writeBuffer = new byte[Frame.FrameHeaderLength + frame.Length];
            frame.WriteTo(writeBuffer);
            await _connectionStream.WriteAsync(writeBuffer, 0, writeBuffer.Length).ConfigureAwait(false);
        }

        public async Task<Frame> ReadFrameAsync(TimeSpan timeout)
        {
            // Prep the timeout cancellation token.
            CancellationTokenSource timeoutCts = new CancellationTokenSource(timeout);

            // First read the frame headers, which should tell us how long the rest of the frame is.
            byte[] headerBytes = new byte[Frame.FrameHeaderLength];

            int totalReadBytes = 0;
            while(totalReadBytes < Frame.FrameHeaderLength)
            {
                int readBytes = await _connectionStream.ReadAsync(headerBytes, totalReadBytes, Frame.FrameHeaderLength - totalReadBytes, timeoutCts.Token).ConfigureAwait(false);
                totalReadBytes += readBytes;
                if (readBytes == 0)
                {
                    throw new Exception("Connection stream closed while attempting to read frame header.");
                }
            }

            Frame header = Frame.ReadFrom(headerBytes);

            // Read the data segment of the frame, if it is present.
            byte[] data = new byte[header.Length];

            totalReadBytes = 0;
            while(totalReadBytes < header.Length)
            {
                int readBytes = await _connectionStream.ReadAsync(data, totalReadBytes, header.Length - totalReadBytes, timeoutCts.Token).ConfigureAwait(false);
                totalReadBytes += readBytes;
                if (readBytes == 0)
                {
                    throw new Exception("Connection stream closed while attempting to read frame body.");
                }
            }

            if (_ignoreSettingsAck && header.Type == FrameType.Settings && header.Flags == FrameFlags.Ack)
            {
                _ignoreSettingsAck = false;
                return await ReadFrameAsync(timeout);
            }

            if (_ignoreWindowUpdates && header.Type == FrameType.WindowUpdate)
            {
                return await ReadFrameAsync(timeout);
            }

            // Construct the correct frame type and return it.
            switch (header.Type)
            {
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

        // Returns the first 24 bytes read, which should be the connection preface.
        public async Task<string> AcceptConnectionAsync()
        {
            _connectionStream = new NetworkStream(await _listenSocket.AcceptAsync().ConfigureAwait(false), true);

            if (_options.UseSsl)
            {
                var sslStream = new SslStream(_connectionStream, false, delegate
                { return true; });
                using (var cert = Configuration.Certificates.GetServerCertificate())
                {
                    SslServerAuthenticationOptions options = new SslServerAuthenticationOptions();

                    options.EnabledSslProtocols = _options.SslProtocols;

                    var protocols = new List<SslApplicationProtocol>();
                    protocols.Add(SslApplicationProtocol.Http2);
                    protocols.Add(SslApplicationProtocol.Http11);
                    options.ApplicationProtocols = protocols;

                    options.ServerCertificate = cert;

                    options.ClientCertificateRequired = false;

                    await sslStream.AuthenticateAsServerAsync(options, CancellationToken.None).ConfigureAwait(false);
                }
                _connectionStream = sslStream;
            }
            byte[] prefix = new byte[24];
            
            int totalReadBytes = 0;
            while(totalReadBytes < Frame.FrameHeaderLength)
            {
                int readBytes = await _connectionStream.ReadAsync(prefix, totalReadBytes, prefix.Length).ConfigureAwait(false);;
                totalReadBytes += readBytes;
                if (readBytes == 0)
                {
                    throw new Exception("Connection stream closed while attempting to read connection preface.");
                }
            }

            return System.Text.Encoding.UTF8.GetString(prefix, 0, prefix.Length);
        }

        public void ExpectSettingsAck()
        {
            // The timing of when we receive the settings ack is not guaranteed.
            // To simplify frame processing, just record that we are expecting one,
            // and then filter it out in ReadFrameAsync above.

            Assert.False(_ignoreSettingsAck);
            _ignoreSettingsAck = true;
        }

        public void IgnoreWindowUpdates()
        {
            _ignoreWindowUpdates = true;
        }

        // Accept connection and handle connection setup
        public async Task EstablishConnectionAsync(params SettingsEntry[] settingsEntries)
        {
            await AcceptConnectionAsync();

            // Receive the initial client settings frame.
            Frame receivedFrame = await ReadFrameAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(FrameType.Settings, receivedFrame.Type);
            Assert.Equal(FrameFlags.None, receivedFrame.Flags);
            Assert.Equal(0, receivedFrame.StreamId);

            // Receive the initial client window update frame.
            receivedFrame = await ReadFrameAsync(TimeSpan.FromSeconds(30));
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
            ExpectSettingsAck();
        }

        public async Task<int> ReadRequestHeaderAsync()
        {
            // Receive HEADERS frame for request.
            Frame frame = await ReadFrameAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(FrameType.Headers, frame.Type);
            Assert.Equal(FrameFlags.EndHeaders | FrameFlags.EndStream, frame.Flags);
            return frame.StreamId;
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

        public void Dispose()
        {
            if (_listenSocket != null)
            {
                _listenSocket.Dispose();
                _listenSocket = null;
            }
        }
    }

    public class Http2Options
    {
        public IPAddress Address { get; set; } = IPAddress.Loopback;
        public int ListenBacklog { get; set; } = 1;
        public bool UseSsl { get; set; } = true;
        public SslProtocols SslProtocols { get; set; } = SslProtocols.Tls12;
    }
}

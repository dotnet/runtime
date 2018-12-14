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

        // Accept connection and handle connection setup
        public async Task EstablishConnectionAsync()
        {
            await AcceptConnectionAsync();

            // Receive the initial client settings frame.
            Frame receivedFrame = await ReadFrameAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(FrameType.Settings, receivedFrame.Type);
            Assert.Equal(FrameFlags.None, receivedFrame.Flags);

            // Send the initial server settings frame.
            Frame emptySettings = new Frame(0, FrameType.Settings, FrameFlags.None, 0);
            await WriteFrameAsync(emptySettings).ConfigureAwait(false);

            // Send the client settings frame ACK.
            Frame settingsAck = new Frame(0, FrameType.Settings, FrameFlags.Ack, 0);
            await WriteFrameAsync(settingsAck).ConfigureAwait(false);

            // Receive the server settings frame ACK.
            receivedFrame = await ReadFrameAsync(TimeSpan.FromSeconds(30));
            Assert.Equal(FrameType.Settings, receivedFrame.Type);
            Assert.True(receivedFrame.AckFlag);
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

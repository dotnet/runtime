// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    public sealed class LoopbackServer : IDisposable
    {
        private Socket _listenSocket;
        private Options _options;
        private Uri _uri;

        // Use CreateServerAsync or similar to create
        private LoopbackServer(Socket listenSocket, Options options)
        {
            _listenSocket = listenSocket;
            _options = options;

            var localEndPoint = (IPEndPoint)listenSocket.LocalEndPoint;
            string host = options.Address.AddressFamily == AddressFamily.InterNetworkV6 ?
                $"[{localEndPoint.Address}]" :
                localEndPoint.Address.ToString();

            string scheme = options.UseSsl ? "https" : "http";
            if (options.WebSocketEndpoint)
            {
                scheme = options.UseSsl ? "wss" : "ws";
            }

            _uri = new Uri($"{scheme}://{host}:{localEndPoint.Port}/");
        }

        public void Dispose()
        {
            if (_listenSocket != null)
            {
                _listenSocket.Dispose();
                _listenSocket = null;
            }
        }

        public Uri Uri => _uri;

        public static async Task CreateServerAsync(Func<LoopbackServer, Task> funcAsync, Options options = null)
        {
            options = options ?? new Options();

            using (var listenSocket = new Socket(options.Address.AddressFamily, SocketType.Stream, ProtocolType.Tcp))
            {
                listenSocket.Bind(new IPEndPoint(options.Address, 0));
                listenSocket.Listen(options.ListenBacklog);

                using (var server = new LoopbackServer(listenSocket, options))
                {
                    await funcAsync(server);
                }
            }
        }

        public static Task CreateServerAsync(Func<LoopbackServer, Uri, Task> funcAsync, Options options = null)
        {
            return CreateServerAsync(server => funcAsync(server, server.Uri), options);
        }

        public static Task CreateClientAndServerAsync(Func<Uri, Task> clientFunc, Func<LoopbackServer, Task> serverFunc)
        {
            return CreateServerAsync(async server =>
            {
                Task clientTask = clientFunc(server.Uri);
                Task serverTask = serverFunc(server);

                await new Task[] { clientTask, serverTask }.WhenAllOrAnyFailed();
            });
        }

        private static string GetDefaultHttpResponse() => $"HTTP/1.1 200 OK\r\nDate: {DateTimeOffset.UtcNow:R}\r\nContent-Length: 0\r\n\r\n";

        public async Task<List<string>> AcceptConnectionSendResponseAndCloseAsync(string response)
        {
            List<string> lines = null;

            // Note, we assume there's no request body.  
            // We'll close the connection after reading the request header and sending the response.
            await AcceptConnectionAsync(async connection =>
            {
                lines = await connection.ReadRequestHeaderAndSendResponseAsync(response);
            });

            return lines;
        }

        public Task<List<string>> AcceptConnectionSendDefaultResponseAndCloseAsync()
        {
            return AcceptConnectionSendResponseAndCloseAsync(GetDefaultHttpResponse());
        }

        public async Task AcceptConnectionAsync(Func<Connection, Task> funcAsync)
        {
            Socket s = await _listenSocket.AcceptAsync().ConfigureAwait(false);

            using (s)
            {
                s.NoDelay = true;

                Stream stream = new NetworkStream(s, ownsSocket: false);
                if (_options.UseSsl)
                {
                    var sslStream = new SslStream(stream, false, delegate { return true; });
                    using (var cert = Configuration.Certificates.GetServerCertificate())
                    {
                        await sslStream.AuthenticateAsServerAsync(
                            cert,
                            clientCertificateRequired: true, // allowed but not required
                            enabledSslProtocols: _options.SslProtocols,
                            checkCertificateRevocation: false).ConfigureAwait(false);
                    }
                    stream = sslStream;
                }

                if (_options.StreamWrapper != null)
                {
                    stream = _options.StreamWrapper(stream);
                }

                using (var connection = new Connection(s, stream))
                {
                    await funcAsync(connection);
                }
            }
        }

        public class Options
        {
            public IPAddress Address { get; set; } = IPAddress.Loopback;
            public int ListenBacklog { get; set; } = 1;
            public bool UseSsl { get; set; } = false;
            public SslProtocols SslProtocols { get; set; } = SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Tls12;
            public bool WebSocketEndpoint { get; set; } = false;
            public Func<Stream, Stream> StreamWrapper { get; set; }
        }

        public sealed class Connection : IDisposable
        {
            private Socket _socket;
            private Stream _stream;
            private StreamReader _reader;
            private StreamWriter _writer;

            public Connection(Socket socket, Stream stream)
            {
                _socket = socket;
                _stream = stream;

                _reader = new StreamReader(stream, Encoding.ASCII);
                _writer = new StreamWriter(stream, Encoding.ASCII) { AutoFlush = true };
            }

            public Socket Socket => _socket;
            public Stream Stream => _stream;
            public StreamReader Reader => _reader;
            public StreamWriter Writer => _writer;

            public void Dispose()
            {
                try
                {
                    // Try to shutdown the send side of the socket.
                    // This seems to help avoid connection reset issues caused by buffered data
                    // that has not been sent/acked when the graceful shutdown timeout expires.
                    // This may throw if the socket was already closed, so eat any exception.
                    _socket.Shutdown(SocketShutdown.Send);
                }
                catch (Exception) { }

                _reader.Dispose();
                _writer.Dispose();
                _stream.Dispose();
                _socket.Dispose();
            }

            public async Task<List<string>> ReadRequestHeaderAsync()
            {
                var lines = new List<string>();
                string line;
                while (!string.IsNullOrEmpty(line = await _reader.ReadLineAsync().ConfigureAwait(false)))
                {
                    lines.Add(line);
                }

                return lines;
            }

            public async Task<List<string>> ReadRequestHeaderAndSendResponseAsync(string response)
            {
                List<string> lines = await ReadRequestHeaderAsync().ConfigureAwait(false);

                await _writer.WriteAsync(response).ConfigureAwait(false);

                return lines;
            }

            public Task<List<string>> ReadRequestHeaderAndSendDefaultResponseAsync()
            {
                return ReadRequestHeaderAndSendResponseAsync(GetDefaultHttpResponse());
            }
        }
    }
}

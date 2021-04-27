// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Xunit;

namespace System.Net.Test.Common
{
    public class Http2LoopbackServer : GenericLoopbackServer, IDisposable
    {
        private Socket _listenSocket;
        private Http2Options _options;
        private Uri _uri;
        private List<Http2LoopbackConnection> _connections = new List<Http2LoopbackConnection>();

        public bool AllowMultipleConnections { get; set; }

        private Http2LoopbackConnection Connection
        {
            get
            {
                RemoveInvalidConnections();
                return _connections[0];
            }
        }

        public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

        public override Uri Address
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

        private void RemoveInvalidConnections()
        {
            _connections.RemoveAll((c) => c.IsInvalid);
        }

        public Task<Http2LoopbackConnection> AcceptConnectionAsync()
        {
            return AcceptConnectionAsync(null);
        }

        public async Task<Http2LoopbackConnection> AcceptConnectionAsync(TimeSpan? timeout)
        {
            RemoveInvalidConnections();

            if (!AllowMultipleConnections && _connections.Count != 0)
            {
                throw new InvalidOperationException("Connection already established. Set `AllowMultipleConnections = true` to bypass.");
            }

            Socket connectionSocket = await _listenSocket.AcceptAsync().ConfigureAwait(false);

            var stream = new NetworkStream(connectionSocket, ownsSocket: true);
            Http2LoopbackConnection connection =
                timeout != null ? await Http2LoopbackConnection.CreateAsync(connectionSocket, stream, _options, timeout.Value).ConfigureAwait(false) :
                await Http2LoopbackConnection.CreateAsync(connectionSocket, stream, _options).ConfigureAwait(false);
            _connections.Add(connection);

            return connection;
        }

        public override async Task<GenericLoopbackConnection> EstablishGenericConnectionAsync()
        {
            return await EstablishConnectionAsync();
        }

        public Task<Http2LoopbackConnection> EstablishConnectionAsync(params SettingsEntry[] settingsEntries)
        {
            return EstablishConnectionAsync(timeout: null, ackTimeout: null, settingsEntries);
        }

        public async Task<Http2LoopbackConnection> EstablishConnectionAsync(TimeSpan? timeout, TimeSpan? ackTimeout, params SettingsEntry[] settingsEntries)
        {
            (Http2LoopbackConnection connection, _) = await EstablishConnectionGetSettingsAsync(timeout, ackTimeout, settingsEntries).ConfigureAwait(false);
            return connection;
        }

        public Task<(Http2LoopbackConnection, SettingsFrame)> EstablishConnectionGetSettingsAsync(params SettingsEntry[] settingsEntries)
        {
            return EstablishConnectionGetSettingsAsync(timeout: null, ackTimeout: null, settingsEntries);
        }

        public async Task<(Http2LoopbackConnection, SettingsFrame)> EstablishConnectionGetSettingsAsync(TimeSpan? timeout, TimeSpan? ackTimeout, params SettingsEntry[] settingsEntries)
        {
            Http2LoopbackConnection connection = await AcceptConnectionAsync(timeout).ConfigureAwait(false);
            SettingsFrame clientSettingsFrame = await connection.ReadAndSendSettingsAsync(ackTimeout, settingsEntries).ConfigureAwait(false);

            return (connection, clientSettingsFrame);
        }

        public override void Dispose()
        {
            if (_listenSocket != null)
            {
                _listenSocket.Dispose();
                _listenSocket = null;
            }
        }

        //
        // GenericLoopbackServer implementation
        //

        public override async Task<HttpRequestData> HandleRequestAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "")
        {
            using (Http2LoopbackConnection connection = await EstablishConnectionAsync().ConfigureAwait(false))
            {
                return await connection.HandleRequestAsync(statusCode, headers, content).ConfigureAwait(false);
			}
        }

        public override async Task AcceptConnectionAsync(Func<GenericLoopbackConnection, Task> funcAsync)
        {
            using (Http2LoopbackConnection connection = await EstablishConnectionAsync().ConfigureAwait(false))
            {
                await funcAsync(connection).ConfigureAwait(false);
            }
        }

        public static Task CreateClientAndServerAsync(Func<Uri, Task> clientFunc, Func<Http2LoopbackServer, Task> serverFunc, int timeout = 60_000)
        {
            return CreateClientAndServerAsync(clientFunc, serverFunc, null, timeout);
        }

        public static async Task CreateClientAndServerAsync(Func<Uri, Task> clientFunc, Func<Http2LoopbackServer, Task> serverFunc, Http2Options http2Options, int timeout = 60_000)
        {
            using (var server = Http2LoopbackServer.CreateServer(http2Options ?? new Http2Options()))
            {
                Task clientTask = clientFunc(server.Address);
                Task serverTask = serverFunc(server);

                await new Task[] { clientTask, serverTask }.WhenAllOrAnyFailed(timeout).ConfigureAwait(false);
            }
        }
    }

    public class Http2Options : GenericLoopbackOptions
    {
        public bool ClientCertificateRequired { get; set; }

        public Http2Options()
        {
            SslProtocols = SslProtocols.Tls12;
        }
    }

    public sealed class Http2LoopbackServerFactory : LoopbackServerFactory
    {
        public static readonly Http2LoopbackServerFactory Singleton = new Http2LoopbackServerFactory();

        public static async Task CreateServerAsync(Func<Http2LoopbackServer, Uri, Task> funcAsync, int millisecondsTimeout = 60_000)
        {
            using (var server = Http2LoopbackServer.CreateServer())
            {
                await funcAsync(server, server.Address).WaitAsync(TimeSpan.FromMilliseconds(millisecondsTimeout));
            }
        }

        public override GenericLoopbackServer CreateServer(GenericLoopbackOptions options = null)
        {
            return Http2LoopbackServer.CreateServer(CreateOptions(options));
        }

        public override async Task<GenericLoopbackConnection> CreateConnectionAsync(Socket socket, Stream stream, GenericLoopbackOptions options = null)
        {
            return await Http2LoopbackConnection.CreateAsync(socket, stream, CreateOptions(options)).ConfigureAwait(false);
        }

        private static Http2Options CreateOptions(GenericLoopbackOptions options)
        {
            Http2Options http2Options = new Http2Options();
            if (options != null)
            {
                http2Options.Address = options.Address;
                http2Options.UseSsl = options.UseSsl;
                http2Options.SslProtocols = options.SslProtocols;
                http2Options.ListenBacklog = options.ListenBacklog;
            }
            return http2Options;
        }

        public override async Task CreateServerAsync(Func<GenericLoopbackServer, Uri, Task> funcAsync, int millisecondsTimeout = 60_000, GenericLoopbackOptions options = null)
        {
            using (var server = CreateServer(options))
            {
                await funcAsync(server, server.Address).WaitAsync(TimeSpan.FromMilliseconds(millisecondsTimeout));
            }
        }

        public override Version Version => HttpVersion20.Value;
    }

    public enum ProtocolErrors
    {
        NO_ERROR = 0x0,
        PROTOCOL_ERROR = 0x1,
        INTERNAL_ERROR = 0x2,
        FLOW_CONTROL_ERROR = 0x3,
        SETTINGS_TIMEOUT = 0x4,
        STREAM_CLOSED = 0x5,
        FRAME_SIZE_ERROR = 0x6,
        REFUSED_STREAM = 0x7,
        CANCEL = 0x8,
        COMPRESSION_ERROR = 0x9,
        CONNECT_ERROR = 0xa,
        ENHANCE_YOUR_CALM = 0xb,
        INADEQUATE_SECURITY = 0xc,
        HTTP_1_1_REQUIRED = 0xd
    }

    public static class HttpVersion20
    {
        public static readonly Version Value = new Version(2, 0);
    }
}

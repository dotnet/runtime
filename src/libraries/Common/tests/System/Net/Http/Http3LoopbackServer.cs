// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Quic;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    public sealed class Http3LoopbackServer : GenericLoopbackServer
    {
        private X509Certificate2 _cert;
        private QuicListener _listener;

        public override Uri Address => new Uri($"https://{_listener.LocalEndPoint}/");

        public Http3LoopbackServer(Http3Options options = null)
        {
            options ??= new Http3Options();

            _cert = Configuration.Certificates.GetServerCertificate();

            var listenerOptions = new QuicListenerOptions()
            {
                ListenEndPoint = new IPEndPoint(options.Address, 0),
                ApplicationProtocols = new List<SslApplicationProtocol>
                {
                    new SslApplicationProtocol(options.Alpn)
                },
                ConnectionOptionsCallback = (_, _, _) =>
                {
                    var serverOptions = new QuicServerConnectionOptions()
                    {
                        DefaultStreamErrorCode = Http3LoopbackConnection.H3_REQUEST_CANCELLED,
                        DefaultCloseErrorCode = Http3LoopbackConnection.H3_NO_ERROR,
                        MaxInboundBidirectionalStreams = options.MaxInboundBidirectionalStreams,
                        MaxInboundUnidirectionalStreams = options.MaxInboundUnidirectionalStreams,
                        ServerAuthenticationOptions = new SslServerAuthenticationOptions
                        {
                            EnabledSslProtocols = options.SslProtocols,
                            ApplicationProtocols = new List<SslApplicationProtocol>
                            {
                                new SslApplicationProtocol(options.Alpn)
                            },
                            ServerCertificate = _cert,
                            ClientCertificateRequired = false
                        }
                    };
                    return ValueTask.FromResult(serverOptions);
                }
            };

            ValueTask<QuicListener> valueTask = QuicListener.ListenAsync(listenerOptions);
            Debug.Assert(valueTask.IsCompleted);
            _listener = valueTask.Result;
        }

        public override void Dispose()
        {
            _listener.DisposeAsync().GetAwaiter().GetResult();
            _cert.Dispose();
        }

        private async Task<Http3LoopbackConnection> EstablishHttp3ConnectionAsync()
        {
            QuicConnection con = await _listener.AcceptConnectionAsync().ConfigureAwait(false);
            Http3LoopbackConnection connection = new Http3LoopbackConnection(con);

            await connection.EstablishControlStreamAsync();
            return connection;
        }

        public override async Task<GenericLoopbackConnection> EstablishGenericConnectionAsync()
        {
            return await EstablishHttp3ConnectionAsync();
        }

        public override async Task AcceptConnectionAsync(Func<GenericLoopbackConnection, Task> funcAsync)
        {
            await using Http3LoopbackConnection con = await EstablishHttp3ConnectionAsync().ConfigureAwait(false);
            await funcAsync(con).ConfigureAwait(false);
            await con.ShutdownAsync();
        }

        public override async Task<HttpRequestData> HandleRequestAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "")
        {
            await using Http3LoopbackConnection con = (Http3LoopbackConnection)await EstablishGenericConnectionAsync().ConfigureAwait(false);
            return await con.HandleRequestAsync(statusCode, headers, content).ConfigureAwait(false);
        }
    }

    public sealed class Http3LoopbackServerFactory : LoopbackServerFactory
    {
        public static Http3LoopbackServerFactory Singleton { get; } = new Http3LoopbackServerFactory();

        public override Version Version { get; } = new Version(3, 0);

        public override GenericLoopbackServer CreateServer(GenericLoopbackOptions options = null)
        {
            return new Http3LoopbackServer(CreateOptions(options));
        }

        public override async Task CreateServerAsync(Func<GenericLoopbackServer, Uri, Task> funcAsync, int millisecondsTimeout = 60000, GenericLoopbackOptions options = null)
        {
            using GenericLoopbackServer server = CreateServer(options);
            await funcAsync(server, server.Address).WaitAsync(TimeSpan.FromMilliseconds(millisecondsTimeout));
        }

        public override Task<GenericLoopbackConnection> CreateConnectionAsync(SocketWrapper socket, Stream stream, GenericLoopbackOptions options = null)
        {
            // TODO: make a new overload that takes a MultiplexedConnection.
            // This method is always unacceptable to call for HTTP/3.
            throw new NotImplementedException("HTTP/3 does not operate over a Socket.");
        }

        private static Http3Options CreateOptions(GenericLoopbackOptions options)
        {
            if (options is Http3Options http3Options)
            {
                return http3Options;
            }

            http3Options = new Http3Options();
            if (options != null)
            {
                http3Options.Address = options.Address;
                http3Options.UseSsl = options.UseSsl;
                http3Options.SslProtocols = options.SslProtocols;
                http3Options.ListenBacklog = options.ListenBacklog;
            }
            return http3Options;
        }
    }
    public class Http3Options : GenericLoopbackOptions
    {
        public int MaxInboundUnidirectionalStreams { get; set; }

        public int MaxInboundBidirectionalStreams { get; set; }

        public string Alpn { get; set; }

        public Http3Options()
        {
            MaxInboundUnidirectionalStreams = 10;
            MaxInboundBidirectionalStreams = 100;
            Alpn = SslApplicationProtocol.Http3.ToString();
        }
    }
}

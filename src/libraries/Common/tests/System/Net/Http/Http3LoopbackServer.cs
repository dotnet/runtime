// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Quic;
using System.Net.Quic.Implementations;
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

        public override Uri Address => new Uri($"https://{_listener.ListenEndPoint}/");

        public Http3LoopbackServer(QuicImplementationProvider quicImplementationProvider = null, GenericLoopbackOptions options = null)
        {
            options ??= new GenericLoopbackOptions();

            _cert = Configuration.Certificates.GetSelfSigned13ServerCertificate();

            var sslOpts = new SslServerAuthenticationOptions
            {
                EnabledSslProtocols = options.SslProtocols,
                ApplicationProtocols = new List<SslApplicationProtocol>
                {
                    new SslApplicationProtocol("h3-31"),
                    new SslApplicationProtocol("h3-30"),
                    new SslApplicationProtocol("h3-29")
                },
                //ServerCertificate = _cert,
                ClientCertificateRequired = false
            };

            _listener = new QuicListener(quicImplementationProvider ?? QuicImplementationProviders.Default, new IPEndPoint(options.Address, 0), sslOpts);
            _listener.Start();
        }

        public override void Dispose()
        {
            _listener.Dispose();
            _cert.Dispose();
        }

        public override async Task<GenericLoopbackConnection> EstablishGenericConnectionAsync()
        {
            QuicConnection con = await _listener.AcceptConnectionAsync().ConfigureAwait(false);
            return new Http3LoopbackConnection(con);
        }

        public override async Task AcceptConnectionAsync(Func<GenericLoopbackConnection, Task> funcAsync)
        {
            using GenericLoopbackConnection con = await EstablishGenericConnectionAsync().ConfigureAwait(false);
            await funcAsync(con).ConfigureAwait(false);
        }

        public override async Task<HttpRequestData> HandleRequestAsync(HttpStatusCode statusCode = HttpStatusCode.OK, IList<HttpHeaderData> headers = null, string content = "")
        {
            using var con = (Http3LoopbackConnection)await EstablishGenericConnectionAsync().ConfigureAwait(false);
            return await con.HandleRequestAsync(statusCode, headers, content).ConfigureAwait(false);
        }
    }

    public sealed class Http3LoopbackServerFactory : LoopbackServerFactory
    {
        private QuicImplementationProvider _quicImplementationProvider;

        public Http3LoopbackServerFactory(QuicImplementationProvider quicImplementationProvider)
        {
            _quicImplementationProvider = quicImplementationProvider;
        }

        public static Http3LoopbackServerFactory Singleton { get; } = new Http3LoopbackServerFactory(null);

        public override Version Version { get; } = new Version(3, 0);

        public override GenericLoopbackServer CreateServer(GenericLoopbackOptions options = null)
        {
            return new Http3LoopbackServer(_quicImplementationProvider, options);
        }

        public override async Task CreateServerAsync(Func<GenericLoopbackServer, Uri, Task> funcAsync, int millisecondsTimeout = 60000, GenericLoopbackOptions options = null)
        {
            using GenericLoopbackServer server = CreateServer(options);
            await funcAsync(server, server.Address).WaitAsync(TimeSpan.FromMilliseconds(millisecondsTimeout));
        }

        public override Task<GenericLoopbackConnection> CreateConnectionAsync(Socket socket, Stream stream, GenericLoopbackOptions options = null)
        {
            // TODO: make a new overload that takes a MultiplexedConnection.
            // This method is always unacceptable to call for HTTP/3.
            throw new NotImplementedException("HTTP/3 does not operate over a Socket.");
        }
    }
}

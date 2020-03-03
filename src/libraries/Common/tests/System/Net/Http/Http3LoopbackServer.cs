// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Quic;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    public sealed class Http3LoopbackServer : GenericLoopbackServer
    {
        private X509Certificate2 _cert;
        private QuicListener _listener;

        public override Uri Address => new Uri($"https://{_listener.ListenEndPoint}/");

        public Http3LoopbackServer(GenericLoopbackOptions options = null)
        {
            options ??= new GenericLoopbackOptions();

            _cert = Configuration.Certificates.GetSelfSigned13ServerCertificate();

            var sslOpts = new SslServerAuthenticationOptions
            {
                EnabledSslProtocols = options.SslProtocols,
                ApplicationProtocols = new List<SslApplicationProtocol> { SslApplicationProtocol.Http3 },
                //ServerCertificate = _cert,
                ClientCertificateRequired = false
            };

            _listener = new QuicListener(new IPEndPoint(options.Address, 0), sslOpts);
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

            HttpRequestData request = await con.ReadRequestDataAsync().ConfigureAwait(false);
            await con.SendResponseAsync(statusCode, headers, content).ConfigureAwait(false);
            await con.CloseAsync(Http3LoopbackConnection.H3_NO_ERROR);
            return request;
        }
    }

    public sealed class Http3LoopbackServerFactory : LoopbackServerFactory
    {
        public static Http3LoopbackServerFactory Singleton { get; } = new Http3LoopbackServerFactory();

        public override Version Version => HttpVersion.Version30;

        public override GenericLoopbackServer CreateServer(GenericLoopbackOptions options = null)
        {
            return new Http3LoopbackServer(options);
        }

        public override async Task CreateServerAsync(Func<GenericLoopbackServer, Uri, Task> funcAsync, int millisecondsTimeout = 60000, GenericLoopbackOptions options = null)
        {
            using GenericLoopbackServer server = CreateServer(options);
            await funcAsync(server, server.Address).TimeoutAfter(millisecondsTimeout).ConfigureAwait(false);
        }
    }
}

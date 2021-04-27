// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic
{
    public sealed class QuicListener : IDisposable
    {
        private readonly QuicListenerProvider _provider;

        /// <summary>
        /// Create a QUIC listener.
        /// </summary>
        /// <param name="listenEndPoint">The local endpoint to listen on.</param>
        /// <param name="sslServerAuthenticationOptions">TLS options for the listener.</param>
        public QuicListener(IPEndPoint listenEndPoint, SslServerAuthenticationOptions sslServerAuthenticationOptions)
            : this(QuicImplementationProviders.Default, listenEndPoint, sslServerAuthenticationOptions)
        {
        }

        /// <summary>
        /// Create a QUIC listener.
        /// </summary>
        /// <param name="options">The listener options.</param>
        public QuicListener(QuicListenerOptions options)
            : this(QuicImplementationProviders.Default, options)
        {
        }

        // !!! TEMPORARY: Remove or make internal before shipping
        public QuicListener(QuicImplementationProvider implementationProvider, IPEndPoint listenEndPoint, SslServerAuthenticationOptions sslServerAuthenticationOptions)
            : this(implementationProvider, new QuicListenerOptions() { ListenEndPoint = listenEndPoint, ServerAuthenticationOptions = sslServerAuthenticationOptions })
        {
        }

        // !!! TEMPORARY: Remove or make internal before shipping
        public QuicListener(QuicImplementationProvider implementationProvider, QuicListenerOptions options)
        {
            _provider = implementationProvider.CreateListener(options);
        }

        public IPEndPoint ListenEndPoint => _provider.ListenEndPoint;

        /// <summary>
        /// Accept a connection.
        /// </summary>
        /// <returns></returns>
        public async ValueTask<QuicConnection> AcceptConnectionAsync(CancellationToken cancellationToken = default) =>
            new QuicConnection(await _provider.AcceptConnectionAsync(cancellationToken).ConfigureAwait(false));

        public void Dispose() => _provider.Dispose();
    }
}

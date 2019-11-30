// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net.Quic.Implementations;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic
{
    public sealed class QuicConnection : IDisposable, IAsyncDisposable
    {
        private readonly QuicConnectionProvider _provider;

        /// <summary>
        /// Create an outbound QUIC connection.
        /// </summary>
        /// <param name="remoteEndPoint">The remote endpoint to connect to.</param>
        /// <param name="sslClientAuthenticationOptions">TLS options</param>
        /// <param name="localEndPoint">The local endpoint to connect from.</param>
        public QuicConnection(IPEndPoint remoteEndPoint, SslClientAuthenticationOptions sslClientAuthenticationOptions, IPEndPoint localEndPoint = null)
            : this(QuicImplementationProviders.Default, remoteEndPoint, sslClientAuthenticationOptions, localEndPoint)
        {
        }

        // !!! TEMPORARY: Remove "implementationProvider" before shipping
        public QuicConnection(QuicImplementationProvider implementationProvider, IPEndPoint remoteEndPoint, SslClientAuthenticationOptions sslClientAuthenticationOptions, IPEndPoint localEndPoint = null)
        {
            _provider = implementationProvider.CreateConnection(remoteEndPoint, sslClientAuthenticationOptions, localEndPoint);
        }

        internal QuicConnection(QuicConnectionProvider provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Indicates whether the QuicConnection is connected.
        /// </summary>
        public bool Connected => _provider.Connected;

        public IPEndPoint LocalEndPoint => _provider.LocalEndPoint;

        public IPEndPoint RemoteEndPoint => _provider.RemoteEndPoint;

        public SslApplicationProtocol NegotiatedApplicationProtocol => _provider.NegotiatedApplicationProtocol;

        /// <summary>
        /// Connect to the remote endpoint.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public ValueTask ConnectAsync(CancellationToken cancellationToken = default) => _provider.ConnectAsync(cancellationToken);

        /// <summary>
        /// Create an outbound unidirectional stream.
        /// </summary>
        /// <returns></returns>
        public QuicStream OpenUnidirectionalStream() => new QuicStream(_provider.OpenUnidirectionalStream());

        /// <summary>
        /// Create an outbound bidirectional stream.
        /// </summary>
        /// <returns></returns>
        public QuicStream OpenBidirectionalStream() => new QuicStream(_provider.OpenBidirectionalStream());

        /// <summary>
        /// Accept an incoming stream.
        /// </summary>
        /// <returns></returns>
        public async ValueTask<QuicStream> AcceptStreamAsync(CancellationToken cancellationToken = default) => new QuicStream(await _provider.AcceptStreamAsync(cancellationToken).ConfigureAwait(false));

        /// <summary>
        /// Close the connection and terminate any active streams.
        /// </summary>
        public void Close() => _provider.Close();

        public void Dispose() => _provider.Dispose();

        public ValueTask DisposeAsync() => _provider.DisposeAsync();
    }
}

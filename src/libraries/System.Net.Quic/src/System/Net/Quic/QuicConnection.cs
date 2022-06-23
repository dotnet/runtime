// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations;
using System.Net.Quic.Implementations.MsQuic;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic
{
    public sealed class QuicConnection : IDisposable
    {
        public static bool IsSupported => MsQuicApi.IsQuicSupported;

        public static ValueTask<QuicConnection> ConnectAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken = default)
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.SystemNetQuic_PlatformNotSupported);
            }

            return ValueTask.FromResult(new QuicConnection(new MsQuicConnection(options)));
        }

        private readonly MsQuicConnection _provider;

        internal QuicConnection(MsQuicConnection provider)
        {
            _provider = provider;
        }

        /// <summary>
        /// Indicates whether the QuicConnection is connected.
        /// </summary>
        public bool Connected => _provider.Connected;

        public IPEndPoint? LocalEndPoint => _provider.LocalEndPoint;

        public EndPoint RemoteEndPoint => _provider.RemoteEndPoint;

        public X509Certificate? RemoteCertificate => _provider.RemoteCertificate;

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
        public async ValueTask<QuicStream> OpenUnidirectionalStreamAsync(CancellationToken cancellationToken = default) => new QuicStream(await _provider.OpenUnidirectionalStreamAsync(cancellationToken).ConfigureAwait(false));

        /// <summary>
        /// Create an outbound bidirectional stream.
        /// </summary>
        /// <returns></returns>
        public async ValueTask<QuicStream> OpenBidirectionalStreamAsync(CancellationToken cancellationToken = default) => new QuicStream(await _provider.OpenBidirectionalStreamAsync(cancellationToken).ConfigureAwait(false));


        /// <summary>
        /// Accept an incoming stream.
        /// </summary>
        /// <returns></returns>
        public async ValueTask<QuicStream> AcceptStreamAsync(CancellationToken cancellationToken = default) => new QuicStream(await _provider.AcceptStreamAsync(cancellationToken).ConfigureAwait(false));

        /// <summary>
        /// Close the connection and terminate any active streams.
        /// </summary>
        public ValueTask CloseAsync(long errorCode, CancellationToken cancellationToken = default) => _provider.CloseAsync(errorCode, cancellationToken);

        public void Dispose() => _provider.Dispose();

        /// <summary>
        /// Gets the maximum number of bidirectional streams that can be made to the peer.
        /// </summary>
        public int GetRemoteAvailableUnidirectionalStreamCount() => _provider.GetRemoteAvailableUnidirectionalStreamCount();

        /// <summary>
        /// Gets the maximum number of unidirectional streams that can be made to the peer.
        /// </summary>
        public int GetRemoteAvailableBidirectionalStreamCount() => _provider.GetRemoteAvailableBidirectionalStreamCount();
    }
}

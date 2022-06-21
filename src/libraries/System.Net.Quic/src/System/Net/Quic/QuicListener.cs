// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations;
using System.Net.Quic.Implementations.MsQuic;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic
{
    public sealed class QuicListener : IDisposable
    {
        public static bool IsSupported => MsQuicApi.IsQuicSupported;

        public static ValueTask<QuicListener> ListenAsync(QuicListenerOptions options, CancellationToken cancellationToken = default)
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.SystemNetQuic_PlatformNotSupported);
            }

            return ValueTask.FromResult(new QuicListener(new MsQuicListener(options)));
        }

        private readonly MsQuicListener _provider;

        internal QuicListener(MsQuicListener provider)
        {
            _provider = provider;
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

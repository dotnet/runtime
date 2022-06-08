// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.MsQuic;
using System.Net.Quic.Implementations.MsQuic.Internal;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Quic
{
    public static partial class QuicProvider
    {
        public static bool IsSupported => MsQuicApi.IsQuicSupported;

        public static ValueTask<QuicListener> CreateListenerAsync(QuicListenerOptions options, CancellationToken cancellationToken = default)
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.SystemNetQuic_PlatformNotSupported);
            }

            return ValueTask.FromResult(new QuicListener(new MsQuicListener(options)));
        }

        public static ValueTask<QuicConnection> CreateConnectionAsync(QuicClientConnectionOptions options, CancellationToken cancellationToken = default)
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.SystemNetQuic_PlatformNotSupported);
            }

            return ValueTask.FromResult(new QuicConnection(new MsQuicConnection(options)));
        }
    }
}

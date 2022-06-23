// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.MsQuic.Internal;

namespace System.Net.Quic.Implementations.MsQuic
{
    internal sealed class MsQuicImplementationProvider : QuicImplementationProvider
    {
        public override bool IsSupported => MsQuicApi.IsQuicSupported;

        internal override QuicListenerProvider CreateListener(QuicListenerOptions options)
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.SystemNetQuic_PlatformNotSupported);
            }

            return new MsQuicListener(options);
        }

        internal override QuicConnectionProvider CreateConnection(QuicClientConnectionOptions options)
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.SystemNetQuic_PlatformNotSupported);
            }

            return new MsQuicConnection(options);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.Managed.Internal.Tls.OpenSsl;

namespace System.Net.Quic.Implementations.Managed.Internal.Tls
{
    internal abstract class TlsFactory
    {
        internal static readonly TlsFactory Instance =
            Interop.OpenSslQuic.IsSupported
                ? (TlsFactory)new OpenSslTlsFactory()
                : (TlsFactory) new MockTlsFactory();
                // new OpenSslTlsFactory();

        internal abstract ITls CreateClient(ManagedQuicConnection connection, QuicClientConnectionOptions options,
            TransportParameters localTransportParams);

        internal abstract ITls CreateServer(ManagedQuicConnection connection, QuicListenerOptions options,
            TransportParameters localTransportParams);
    }
}

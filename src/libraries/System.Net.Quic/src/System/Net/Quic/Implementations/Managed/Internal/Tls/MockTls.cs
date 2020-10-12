// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Net.Quic.Implementations.Managed.Internal.Headers;
using System.Net.Security;
using System.Text;

namespace System.Net.Quic.Implementations.Managed.Internal.Tls
{
    internal sealed class MockTls : ITls
    {
        // magic bytes to distinguish this implementation from the other TLS implementations
        // private static readonly byte[] _magicBytes = Encoding.UTF8.GetBytes(".NET QUIC mock TLS");

        // private readonly ManagedQuicConnection _connection;
        // private readonly TransportParameters _localTransportParams;
        // private readonly TransportParameters? _remoteTransportParams;
        // private readonly List<SslApplicationProtocol> _alpn;

        // private ArrayBuffer _sendBuffer = new ArrayBuffer(1200, true);

        // private readonly bool _isServer;

        public MockTls(ManagedQuicConnection connection, QuicClientConnectionOptions options, TransportParameters localTransportParams)
            : this(connection, localTransportParams, options.ClientAuthenticationOptions?.ApplicationProtocols)
        {
            // _isServer = false;
        }

        public MockTls(ManagedQuicConnection connection, QuicListenerOptions options, TransportParameters localTransportParams)
            : this(connection, localTransportParams, options.ServerAuthenticationOptions?.ApplicationProtocols)
        {
            // _isServer = true;
        }

        private MockTls(ManagedQuicConnection connection, TransportParameters localTransportParams,
            List<SslApplicationProtocol>? alpn)
        {
            // _connection = connection;
            // _localTransportParams = localTransportParams;
            // _alpn = alpn ?? throw new ArgumentNullException(nameof(SslServerAuthenticationOptions.ApplicationProtocols));
        }

        public void Dispose()
        {
            // _sendBuffer.Dispose();
        }

        public bool IsHandshakeComplete { get; private set; }
        public EncryptionLevel WriteLevel { get; private set; }
        public void OnHandshakeDataReceived(EncryptionLevel level, ReadOnlySpan<byte> data) => throw new NotImplementedException();

        public bool TryAdvanceHandshake()
        {
            return false;
        }

        public TlsCipherSuite GetNegotiatedCipher() => QuicConstants.InitialCipherSuite;

        public TransportParameters? GetPeerTransportParameters(bool isServer) => throw new NotImplementedException();

        public SslApplicationProtocol GetNegotiatedProtocol() => throw new NotImplementedException();
    }
}

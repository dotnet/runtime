using System.Net.Security;

namespace System.Net.Quic.Implementations.Managed.Internal.Tls
{
    internal interface ITls : IDisposable
    {
        bool IsHandshakeComplete { get; }
        EncryptionLevel WriteLevel { get; }
        void OnHandshakeDataReceived(EncryptionLevel level, ReadOnlySpan<byte> data);
        bool TryAdvanceHandshake();
        TlsCipherSuite GetNegotiatedCipher();
        TransportParameters? GetPeerTransportParameters(bool isServer);
        SslApplicationProtocol GetNegotiatedProtocol();
    }
}

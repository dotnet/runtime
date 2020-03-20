using System.Net.Quic.Implementations.Managed.Internal.OpenSsl;

namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal interface IQuicCallback
    {
        int SetEncryptionSecrets(SslEncryptionLevel level, byte[] readSecret, byte[] writeSecret);
        int AddHandshakeData(SslEncryptionLevel level, byte[] data);
        int Flush();
        int SendAlert(SslEncryptionLevel level, TlsAlert alert);
    }
}

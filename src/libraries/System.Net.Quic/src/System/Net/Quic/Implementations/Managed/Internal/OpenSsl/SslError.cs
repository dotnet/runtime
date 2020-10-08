namespace System.Net.Quic.Implementations.Managed.Internal.OpenSsl
{
    internal enum SslError
    {
        None = 0,
        Ssl = 1,
        WantRead = 2,
        WantWrite = 3,
        WantX509Lookup = 4,
        Syscall = 5,
        ZeroReturn = 6,
        WantConnect = 7,
        WantAccept = 8,
        WantAsync = 9,
        WantAsyncJob = 10,
        WantClientHelloCb = 11
    }
}

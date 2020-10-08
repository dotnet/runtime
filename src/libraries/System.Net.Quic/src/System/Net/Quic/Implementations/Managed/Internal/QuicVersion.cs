namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Version of the QUIC protocol.
    /// </summary>
    internal enum QuicVersion : uint
    {
        Negotiation = 0x0000_0000,
        Quic1 = 0x0000_0001,
        Draft27 = 0xff00_0000 + 27,
        EnforceNegotiationMask = 0x0f0f_0f0f,
        EnforceNegotiation = 0x0a0a_0a0a
    }
}
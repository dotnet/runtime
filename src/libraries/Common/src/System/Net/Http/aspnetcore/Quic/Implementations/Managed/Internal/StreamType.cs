namespace System.Net.Quic.Implementations.Managed.Internal
{
    /// <summary>
    ///     Type of the stream.
    /// </summary>
    public enum StreamType : uint
    {
        ClientInitiatedBidirectional = 0x0,
        ServerInitiatedBidirectional = 0x1,
        ClientInitiatedUnidirectional = 0x2,
        ServerInitiatedUnidirectional = 0x3,
    }
}

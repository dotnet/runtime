namespace System.Net.Quic.Implementations.Managed.Internal
{
    internal enum QuicTransportParameter
    {
        OriginalConnectionId = 0x00,
        MaxIdleTimeout = 0x01,
        StatelessResetToken = 0x02,
        MaxPacketSize = 0x03,
        InitialMaxData = 0x04,
        InitialMaxStreamDataBidiLocal = 0x05,
        InitialMaxStreamDataBidiRemote = 0x06,
        InitialMaxStreamDataUni = 0x07,
        InitialMaxStreamsBidi = 0x08,
        InitialMaxStreamsUni = 0x09,
        AckDelayExponent = 0x0a,
        MaxAckDelay = 0x0b,
        DisableActiveMigration = 0x0c,
        PreferredAddress = 0x0d
    }
}

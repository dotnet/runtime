using System.Collections.Generic;

namespace System.Net.Quic.Implementations.Managed.Internal.Recovery
{
    /// <summary>
    ///     Defines method for managing congestion control window.
    /// </summary>
    internal interface ICongestionController
    {
        /// <summary>
        ///     Current width of the congestion window.
        /// </summary>
        int CongestionWindow { get; }

        /// <summary>
        ///     The sum of the size in bytes of all sent packets not acked or declared lost.
        ///     The size does not include IP or UDP overhead, but does include the QUIC header and AEAD overhead.
        /// </summary>
        int BytesInFlight { get; }

        /// <summary>
        ///     Resets the controller to the initial state.
        /// </summary>
        void Reset();

        /// <summary>
        ///     Called when a packet is sent.
        /// </summary>
        /// <param name="packet">Packet sent.</param>
        void OnPacketSent(SentPacket packet);

        /// <summary>
        ///     Called when a previously sent packet is acked.
        /// </summary>
        /// <param name="packet">Packet acked.</param>
        void OnPacketAcked(SentPacket packet);

        /// <summary>
        ///     Called when detecting loss of group of consecutive packets.
        /// </summary>
        /// <param name="packets">Packets lost.</param>
        /// <param name="now">Timestamp when loss occured.</param>
        void OnPacketsLost(List<SentPacket> packets, DateTime now);
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace System.Net.Quic.Implementations.Managed.Internal.Recovery
{
    /// <summary>
    ///     Defines method for managing congestion control window.
    /// </summary>
    internal interface ICongestionController
    {
        /// <summary>
        ///     Called when a packet is sent.
        /// </summary>
        /// <param name="recovery">The recovery controller.</param>
        /// <param name="packet">Packet sent.</param>
        void OnPacketSent(RecoveryController recovery, SentPacket packet);

        /// <summary>
        ///     Called when a previously sent packet is acked.
        /// </summary>
        /// <param name="recovery">The recovery controller.</param>
        /// <param name="packet">Packet acked.</param>
        /// <param name="now">Timestamp when ack was received in ticks</param>
        void OnPacketAcked(RecoveryController recovery, SentPacket packet, long now);

        /// <summary>
        ///     Called when detecting loss of group of consecutive packets.
        /// </summary>
        /// <param name="recovery">The recovery controller.</param>
        /// <param name="packets">Packets lost.</param>
        /// <param name="now">Timestamp when loss occured in ticks.</param>
        void OnPacketsLost(RecoveryController recovery, Span<SentPacket> packets, long now);
    }
}

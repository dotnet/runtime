// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Recovery;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class NewRenoTests
    {
        private NewRenoCongestionController reno => NewRenoCongestionController.Instance;

        private readonly RecoveryController recovery = new RecoveryController();

        private long Now = Timestamp.Now;

        [Fact]
        public void InitialWindowIsNotEmpty()
        {
            Assert.True(recovery.CongestionWindow > 0);
            Assert.Equal(0, recovery.BytesInFlight);
        }

        [Fact]
        public void SendAddsToBytesInFlight()
        {
            reno.OnPacketSent(recovery, new SentPacket(){ TimeSent = Now, BytesSent = 1000, InFlight = true});
            Assert.Equal(1000, recovery.BytesInFlight);
        }

        [Fact]
        public void SlowStart()
        {
            var packet = new SentPacket {TimeSent = Now, BytesSent = 5000, InFlight = true};

            // saturate initial congestion window (the congestion window does not increase when app-limited)
            reno.OnPacketSent(recovery, packet);
            reno.OnPacketSent(recovery, packet);
            reno.OnPacketSent(recovery, packet);
            int window = recovery.CongestionWindow;

            reno.OnPacketAcked(recovery, packet, Now);
            // congestion window should increase
            Assert.Equal(window + packet.BytesSent, recovery.CongestionWindow);
        }

        [Fact]
        public void ReducesWindowAfterCongestionEvent()
        {
            int window = recovery.CongestionWindow;
            reno.OnCongestionEvent(recovery, Now - Timestamp.FromMilliseconds(15), Now);

            Assert.Equal(window / 2, recovery.CongestionWindow);

            // further congestion events should not change the window
            reno.OnCongestionEvent(recovery, Now - Timestamp.FromMilliseconds(15), Now);
            Assert.Equal(window / 2, recovery.CongestionWindow);
        }

        [Fact]
        public void AckedPacketsInCongestionRecoveryDontChangeCongestionWindow()
        {
            var packet = new SentPacket {TimeSent = Now, BytesSent = 5000, InFlight = true};

            // saturate initial congestion window (the congestion window does not increase when app-limited)
            reno.OnPacketSent(recovery, packet);
            reno.OnPacketSent(recovery, packet);
            reno.OnPacketSent(recovery, packet);

            reno.OnCongestionEvent(recovery, Now, Now + Timestamp.FromMilliseconds(15));
            int window = recovery.CongestionWindow;
            reno.OnPacketAcked(recovery, packet, Now); // sent before congestion recovery started
            Assert.Equal(window, recovery.CongestionWindow);
        }

        [Fact]
        public void CongestionAvoidance()
        {
            var packet = new SentPacket {TimeSent = Now, BytesSent = 5000, InFlight = true};

            // saturate initial congestion window (the congestion window does not increase when app-limited)
            reno.OnPacketSent(recovery, packet);
            reno.OnPacketSent(recovery, packet);
            reno.OnPacketSent(recovery, packet);

            // experience congestion to set slow start threshold
            int window = recovery.CongestionWindow;
            reno.OnCongestionEvent(recovery, Now, Now + Timestamp.FromMilliseconds(15));
            Assert.Equal(window/2, recovery.CongestionWindow);

            // send another packet some time later
            Now += Timestamp.FromMilliseconds(15);
            packet = new SentPacket {TimeSent = Now, BytesSent = 5000, InFlight = true};
            reno.OnPacketSent(recovery, packet);

            // this one gets acked
            window = recovery.CongestionWindow;
            reno.OnPacketAcked(recovery, packet, Now + Timestamp.FromMilliseconds(10));

            // congestion window should be increased by smaller value than packet size
            Assert.True(recovery.CongestionWindow - window < packet.BytesSent);
        }
    }
}

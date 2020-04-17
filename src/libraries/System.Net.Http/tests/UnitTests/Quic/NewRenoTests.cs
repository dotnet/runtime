using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal.Recovery;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class NewRenoTests
    {
        private readonly NewRenoCongestionController reno;

        private long Now = Timestamp.Now;

        public NewRenoTests()
        {
            reno = new NewRenoCongestionController();
            reno.Reset();
        }

        [Fact]
        public void InitialWindowIsNotEmpty()
        {
            Assert.True(reno.CongestionWindow > 0);
            Assert.Equal(0, reno.BytesInFlight);
        }

        [Fact]
        public void SendAddsToBytesInFlight()
        {
            reno.OnPacketSent(new SentPacket(){ TimeSent = Now, BytesSent = 1000, InFlight = true});
            Assert.Equal(1000, reno.BytesInFlight);
        }

        [Fact]
        public void SlowStart()
        {
            var packet = new SentPacket {TimeSent = Now, BytesSent = 5000, InFlight = true};

            // saturate initial congestion window (the congestion window does not increase when app-limited)
            reno.OnPacketSent(packet);
            reno.OnPacketSent(packet);
            reno.OnPacketSent(packet);
            int window = reno.CongestionWindow;

            reno.OnPacketAcked(packet, Now);
            // congestion window should increase
            Assert.Equal(window + packet.BytesSent, reno.CongestionWindow);
        }

        [Fact]
        public void ReducesWindowAfterCongestionEvent()
        {
            int window = reno.CongestionWindow;
            reno.OnCongestionEvent(Now - Timestamp.FromMilliseconds(15), Now);

            Assert.Equal(window / 2, reno.CongestionWindow);

            // further congestion events should not change the window
            reno.OnCongestionEvent(Now - Timestamp.FromMilliseconds(15), Now);
            Assert.Equal(window / 2, reno.CongestionWindow);
        }

        [Fact]
        public void AckedPacketsInCongestionRecoveryDontChangeCongestionWindow()
        {
            var packet = new SentPacket {TimeSent = Now, BytesSent = 5000, InFlight = true};

            // saturate initial congestion window (the congestion window does not increase when app-limited)
            reno.OnPacketSent(packet);
            reno.OnPacketSent(packet);
            reno.OnPacketSent(packet);

            reno.OnCongestionEvent(Now, Now + Timestamp.FromMilliseconds(15));
            int window = reno.CongestionWindow;
            reno.OnPacketAcked(packet, Now); // sent before congestion recovery started
            Assert.Equal(window, reno.CongestionWindow);
        }

        [Fact]
        public void CongestionAvoidance()
        {
            var packet = new SentPacket {TimeSent = Now, BytesSent = 5000, InFlight = true};

            // saturate initial congestion window (the congestion window does not increase when app-limited)
            reno.OnPacketSent(packet);
            reno.OnPacketSent(packet);
            reno.OnPacketSent(packet);

            // experience congestion to set slow start threshold
            int window = reno.CongestionWindow;
            reno.OnCongestionEvent(Now, Now + Timestamp.FromMilliseconds(15));
            Assert.Equal(window/2, reno.CongestionWindow);

            // send another packet some time later
            Now += Timestamp.FromMilliseconds(15);
            packet = new SentPacket {TimeSent = Now, BytesSent = 5000, InFlight = true};
            reno.OnPacketSent(packet);

            // this one gets acked
            window = reno.CongestionWindow;
            reno.OnPacketAcked(packet, Now + Timestamp.FromMilliseconds(10));

            // congestion window should be increased by smaller value than packet size
            Assert.True(reno.CongestionWindow - window < packet.BytesSent);
        }
    }
}

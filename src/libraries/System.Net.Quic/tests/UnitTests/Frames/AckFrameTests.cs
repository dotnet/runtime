// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Linq;
using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using System.Net.Quic.Tests.Harness;
using Xunit;
using Xunit.Abstractions;
using AckFrame = System.Net.Quic.Tests.Harness.AckFrame;
using StreamFrame = System.Net.Quic.Tests.Harness.StreamFrame;

namespace System.Net.Quic.Tests.Frames
{
    public class AckFrameTests : ManualTransmissionQuicTestBase
    {
        public AckFrameTests(ITestOutputHelper output)
            : base(output)
        {
            // all tests start after connection has been established
            EstablishConnection();
        }

        [Fact]
        public void ConnectionCloseWhenAckingFuturePacket()
        {
            Client.Ping();
            Intercept1RttFrame<AckFrame>(Client, Server, ack =>
            {
                // ack one more than intended
                ack.LargestAcknowledged++;
                ack.FirstAckRange++;
            });

            Send1Rtt(Server, Client)
                .ShouldHaveConnectionClose(TransportErrorCode.ProtocolViolation,
                    QuicError.InvalidAckRange,
                    FrameType.Ack);
        }

        [Fact]
        public void ConnectionCloseWhenAckingNegativePacket()
        {
            Client.Ping();
            Intercept1RttFrame<AckFrame>(Client, Server, ack =>
            {
                // ack one more than intended
                ack.FirstAckRange++;
            });

            Send1Rtt(Server, Client)
                .ShouldHaveConnectionClose(TransportErrorCode.ProtocolViolation,
                    QuicError.InvalidAckRange,
                    FrameType.Ack);
        }

        [Fact]
        public void TestNotAckingPastFrames()
        {
            // since PING frames are ack-eliciting, the endpoint should always send an ack frame, leading to each endpoint always acking only the last received packet.
            var sender = Client;
            var receiver = Server;
            for (int i = 0; i < 3; i++)
            {
                sender.Ping();
                var flight = SendFlight(sender, receiver);
                var packet = Assert.IsType<OneRttPacket>(flight.Packets[0]);
                var ack = Assert.Single(packet.Frames.OfType<AckFrame>());

                Assert.Equal(0u, ack.FirstAckRange);
                Assert.Empty(ack.AckRanges);

                var tmp = sender;
                sender = receiver;
                receiver = tmp;
            }
        }

        [Fact]
        public void TestAckNonContiguousRanges()
        {
            // make sure the end has enough consecutive PNs to guarantee that earlier packets are determined lost
            var received = new[] {2, 3, 4, 7, 8, 10, 13, 14, 15, 16};
            var lost = Enumerable.Range(0, 17).Except(received).ToArray();

            long last = received[^1];

            // we enforce sending packets by writing one byte, coincidentally containing the value of expected packet
            // number for better testing
            var clientStream = Client.OpenUnidirectionalStream();

            for (int i = 0; i <= last; i++)
            {
                // enforce sending a packet
                clientStream.Write(new[] {(byte)i});
                clientStream.Flush();
                PacketBase packet = Get1RttToSend(Client);

                // make sure our testing strategy works
                Assert.Equal(i, packet.PacketNumber);

                if (received.Contains((int)packet.PacketNumber))
                {
                    // let the packet be received
                    SendPacket(Client, Server, packet);
                }

                // else drop the packet
                LogFlightPackets(packet, Client, true);
            }

            // send ack back to server
            var frame = Send1Rtt(Server, Client).ShouldHaveFrame<AckFrame>();

            Assert.Equal(last, frame.LargestAcknowledged);
            Assert.Equal(3, frame.FirstAckRange);

            Assert.Equal(new[]
            {
                // remember that all numbers are encoded as 1 lesser, and encoding starts from the largest
                new AckFrame.AckRange {Acked = 0, Gap = 1}, // 10
                new AckFrame.AckRange {Acked = 1, Gap = 0}, // 7, 8
                new AckFrame.AckRange {Acked = 2, Gap = 1}, // 2, 3, 4
            }, frame.AckRanges);

            // the data should be resent by now
            var resent = Get1RttToSend(Client).Frames
                .OfType<StreamFrame>().SelectMany(f => f.StreamData.Select(i => (int)i)).ToArray();

            Assert.Equal(lost, resent);
        }
    }
}

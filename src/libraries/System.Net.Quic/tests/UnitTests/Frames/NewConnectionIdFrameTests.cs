// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using Xunit;
using Xunit.Abstractions;
using ConnectionCloseFrame = System.Net.Quic.Tests.Harness.ConnectionCloseFrame;
using NewConnectionIdFrame = System.Net.Quic.Tests.Harness.NewConnectionIdFrame;

namespace System.Net.Quic.Tests.Frames
{
    public class NewConnectionIdFrameTests : ManualTransmissionQuicTestBase
    {
        public NewConnectionIdFrameTests(ITestOutputHelper output)
            : base(output)
        {
            // all tests start after connection has been established
            EstablishConnection();
        }

        private void SendNewConnectionIdFrame(ManagedQuicConnection source, ManagedQuicConnection destination,
            byte[] cid, long sequenceNumber, StatelessResetToken token)
        {
            source.Ping(); // ensure a frame is indeed sent
            Intercept1Rtt(source, destination,
                packet =>
                {
                    packet.Frames.Add(new NewConnectionIdFrame()
                    {
                        ConnectionId = cid,
                        SequenceNumber = sequenceNumber,
                        RetirePriorTo = 0,
                        StatelessResetToken = token
                    });
                });
        }

        [Fact]
        public void ClosesConnectionWhenChangingSequenceNumber()
        {
            byte[] cid = {1, 2, 3, 4, 5, 6, 6, 8};
            StatelessResetToken token = StatelessResetToken.Random();
            SendNewConnectionIdFrame(Server, Client, cid, 1, token);

            // everything okay still
            Send1Rtt(Client, Server).ShouldNotHaveFrame<ConnectionCloseFrame>();

            // send same connection id with different sequence number
            SendNewConnectionIdFrame(Server, Client, cid, 2, token);

            // now we are in trouble
            Send1Rtt(Client, Server).ShouldHaveConnectionClose(
                TransportErrorCode.ProtocolViolation,
                QuicError.InconsistentNewConnectionIdFrame,
                FrameType.NewConnectionId);
        }

        [Fact]
        public void ClosesConnectionWhenChangingStatelessResetToken()
        {
            byte[] cid = {1, 2, 3, 4, 5, 6, 6, 8};
            StatelessResetToken token = StatelessResetToken.Random();
            SendNewConnectionIdFrame(Server, Client, cid, 1, token);

            // everything okay still
            Send1Rtt(Client, Server).ShouldNotHaveFrame<ConnectionCloseFrame>();

            // send same connection id with different stateless reset token
            SendNewConnectionIdFrame(Server, Client, cid, 1, StatelessResetToken.Random());

            // now we are in trouble
            Send1Rtt(Client, Server).ShouldHaveConnectionClose(
                TransportErrorCode.ProtocolViolation,
                QuicError.InconsistentNewConnectionIdFrame,
                FrameType.NewConnectionId);
        }
    }
}

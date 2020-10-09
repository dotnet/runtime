// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.Managed;
using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Tests.Harness;
using Xunit;
using Xunit.Abstractions;

namespace System.Net.Quic.Tests.Frames
{
    public class StopSendingFrameTests : ManualTransmissionQuicTestBase
    {
        public StopSendingFrameTests(ITestOutputHelper output) : base(output)
        {
            EstablishConnection();
        }

        [Fact]
        public void ElicitsResetStream()
        {
            var stream = Client.OpenStream(false);
            long errorCode = 15;
            stream.AbortRead(errorCode);

            Send1RttWithFrame<StopSendingFrame>(Client, Server);

            var frame = Send1RttWithFrame<ResetStreamFrame>(Server, Client);

            Assert.Equal(stream.StreamId, frame.StreamId);
            Assert.Equal(errorCode, frame.ApplicationErrorCode);
            Assert.Equal(0, frame.FinalSize);
        }

        private void CloseConnectionCommon(StopSendingFrame frame, TransportErrorCode errorCode, string reason)
        {
            Client.Ping();
            Intercept1Rtt(Client, Server, packet => { packet.Frames.Add(frame); });

            Send1Rtt(Server, Client).ShouldHaveConnectionClose(
                errorCode,
                reason,
                FrameType.StopSending);
        }

        [Fact]
        public void ClosesConnection_WhenReceivedForNonWritableStream()
        {
            var stream = Client.OpenStream(true);

            CloseConnectionCommon(new StopSendingFrame()
                {
                    StreamId = stream.StreamId,
                    ApplicationErrorCode = 14
                },
                TransportErrorCode.StreamStateError, QuicError.StreamNotWritable);
        }

        [Fact]
        public void ClosesConnection_WhenReceivedForUncreatedLocallyInitiatedStream()
        {
            CloseConnectionCommon(
                new StopSendingFrame()
                {
                    StreamId = StreamHelpers.ComposeStreamId(StreamType.ServerInitiatedBidirectional, 0),
                    ApplicationErrorCode = 14
                },
                TransportErrorCode.StreamStateError, QuicError.StreamNotCreated);
        }

        [Fact]
        public void ClosesConnection_WhenViolatingStreamLimit()
        {
            CloseConnectionCommon(
                new StopSendingFrame()
                {
                    StreamId = StreamHelpers.ComposeStreamId(StreamType.ClientInitiatedBidirectional, ListenerOptions.MaxBidirectionalStreams + 1),
                    ApplicationErrorCode = 14
                },
                TransportErrorCode.StreamLimitError, QuicError.StreamsLimitViolated);
        }

        [Fact]
        public void RetransmittedAfterLoss()
        {
            var stream = Client.OpenStream(false);
            long errorCode = 15;
            stream.AbortRead(errorCode);

            Lose1RttWithFrameAndCheckIfItIsResentLater<StopSendingFrame>(Client, Server);
        }
    }
}

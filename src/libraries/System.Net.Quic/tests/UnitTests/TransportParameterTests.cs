// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Quic.Implementations.Managed.Internal;
using System.Net.Quic.Implementations.Managed.Internal.Frames;
using Xunit;

namespace System.Net.Quic.Tests
{
    public class TransportParameterTests
    {
        public TransportParameterTests()
        {
            buffer = new byte[2048];
            reader = new QuicReader(buffer);
            writer = new QuicWriter(buffer);
        }

        private readonly QuicReader reader;
        private readonly QuicWriter writer;

        private readonly byte[] buffer;

        [Fact]
        public void SerializeTransportParameters()
        {
            var expected = new TransportParameters()
            {
                PreferredAddress = null, // not supported yet
                AckDelayExponent = 5,
                DisableActiveMigration = true,
                InitialMaxData = 6,
                MaxAckDelay = 10,
                MaxIdleTimeout = 100,
                MaxPacketSize = 2034,
                OriginalConnectionId = new ConnectionId(new byte[] {1, 2, 3, 4, 5, 6, 7, 8, 9, 0}, 0, StatelessResetToken.Random()),
                StatelessResetToken = new StatelessResetToken(3412414, 1231231),
                ActiveConnectionIdLimit = 399,
                InitialMaxStreamsBidi = 10,
                InitialMaxStreamsUni = 3423423422,
                InitialMaxStreamDataUni = 33,
                InitialMaxStreamDataBidiLocal = 44,
                InitialMaxStreamDataBidiRemote = 4339,
            };

            int written = TransportParameters.Write(buffer, true, expected);
            reader.Reset(buffer.AsMemory(0, writer.BytesWritten));
            Assert.True(TransportParameters.Read(buffer.AsSpan(0, written), true, out var actual));

            Assert.Equal(expected.PreferredAddress, actual.PreferredAddress);
            Assert.Equal(expected.AckDelayExponent, actual.AckDelayExponent);
            Assert.Equal(expected.DisableActiveMigration, actual.DisableActiveMigration);
            Assert.Equal(expected.InitialMaxData, actual.InitialMaxData);
            Assert.Equal(expected.MaxAckDelay, actual.MaxAckDelay);
            Assert.Equal(expected.MaxIdleTimeout, actual.MaxIdleTimeout);
            Assert.Equal(expected.MaxPacketSize, actual.MaxPacketSize);
            Assert.Equal(expected.OriginalConnectionId, actual.OriginalConnectionId);
            Assert.Equal(expected.StatelessResetToken, actual.StatelessResetToken);
            Assert.Equal(expected.ActiveConnectionIdLimit, actual.ActiveConnectionIdLimit);
            Assert.Equal(expected.InitialMaxStreamsBidi, actual.InitialMaxStreamsBidi);
            Assert.Equal(expected.InitialMaxStreamsUni, actual.InitialMaxStreamsUni);
            Assert.Equal(expected.InitialMaxStreamDataUni, actual.InitialMaxStreamDataUni);
            Assert.Equal(expected.InitialMaxStreamDataBidiLocal, actual.InitialMaxStreamDataBidiLocal);
            Assert.Equal(expected.InitialMaxStreamDataBidiRemote, actual.InitialMaxStreamDataBidiRemote);

            Assert.Equal(0, reader.BytesLeft);
        }
    }
}

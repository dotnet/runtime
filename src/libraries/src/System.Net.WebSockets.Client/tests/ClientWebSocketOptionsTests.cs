// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Net.Test.Common;
using System.Threading;
using System.Threading.Tasks;

using Xunit;
using Xunit.Abstractions;

namespace System.Net.WebSockets.Client.Tests
{
    public class ClientWebSocketOptionsTests : ClientWebSocketTestBase
    {
        public ClientWebSocketOptionsTests(ITestOutputHelper output) : base(output) { }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public static void UseDefaultCredentials_Roundtrips()
        {
            var cws = new ClientWebSocket();
            Assert.False(cws.Options.UseDefaultCredentials);
            cws.Options.UseDefaultCredentials = true;
            Assert.True(cws.Options.UseDefaultCredentials);
            cws.Options.UseDefaultCredentials = false;
            Assert.False(cws.Options.UseDefaultCredentials);
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public static void SetBuffer_InvalidArgs_Throws()
        {
            var cws = new ClientWebSocket();
            AssertExtensions.Throws<ArgumentOutOfRangeException>("receiveBufferSize", () => cws.Options.SetBuffer(0, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("receiveBufferSize", () => cws.Options.SetBuffer(0, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sendBufferSize", () => cws.Options.SetBuffer(1, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("receiveBufferSize", () => cws.Options.SetBuffer(0, 0, new ArraySegment<byte>(new byte[1])));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("receiveBufferSize", () => cws.Options.SetBuffer(0, 1, new ArraySegment<byte>(new byte[1])));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("sendBufferSize", () => cws.Options.SetBuffer(1, 0, new ArraySegment<byte>(new byte[1])));
            AssertExtensions.Throws<ArgumentNullException>("buffer.Array", () => cws.Options.SetBuffer(1, 1, default(ArraySegment<byte>)));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("buffer", () => cws.Options.SetBuffer(1, 1, new ArraySegment<byte>(new byte[0])));
        }

        [ConditionalFact(nameof(WebSocketsSupported))]
        public static void KeepAliveInterval_Roundtrips()
        {
            var cws = new ClientWebSocket();
            Assert.True(cws.Options.KeepAliveInterval > TimeSpan.Zero);

            cws.Options.KeepAliveInterval = TimeSpan.Zero;
            Assert.Equal(TimeSpan.Zero, cws.Options.KeepAliveInterval);

            cws.Options.KeepAliveInterval = TimeSpan.MaxValue;
            Assert.Equal(TimeSpan.MaxValue, cws.Options.KeepAliveInterval);

            cws.Options.KeepAliveInterval = Timeout.InfiniteTimeSpan;
            Assert.Equal(Timeout.InfiniteTimeSpan, cws.Options.KeepAliveInterval);

            AssertExtensions.Throws<ArgumentOutOfRangeException>("value", () => cws.Options.KeepAliveInterval = TimeSpan.MinValue);
        }
    }
}

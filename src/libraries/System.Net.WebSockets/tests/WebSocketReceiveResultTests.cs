// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.WebSockets.Tests
{
    public sealed class WebSocketReceiveResultTests
    {
        public static object[][] ConstructorData = {
            new object[] { 0, WebSocketMessageType.Text, false, null, null },
            new object[] { 1, WebSocketMessageType.Text, true, null, null },
            new object[] { 2, WebSocketMessageType.Binary, false, null, null },
            new object[] { 3, WebSocketMessageType.Binary, true, null, null },
            new object[] { 4, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, null },
            new object[] { 5, WebSocketMessageType.Close, true, WebSocketCloseStatus.NormalClosure, "normal" },
        };

        [Theory, MemberData(nameof(ConstructorData))]
        public void ConstructorTest_Success(int count, WebSocketMessageType messageType, bool endOfMessage, WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            WebSocketReceiveResult wsrr;

            if (closeStatus == null && closeStatusDescription == null)
            {
                wsrr = new WebSocketReceiveResult(count, messageType, endOfMessage);
                Assert.Equal(count, wsrr.Count);
                Assert.Equal(messageType, wsrr.MessageType);
                Assert.Equal(endOfMessage, wsrr.EndOfMessage);
                Assert.Equal(null, wsrr.CloseStatus);
                Assert.Equal(null, wsrr.CloseStatusDescription);
            }

            wsrr = new WebSocketReceiveResult(count, messageType, endOfMessage, closeStatus, closeStatusDescription);
            Assert.Equal(count, wsrr.Count);
            Assert.Equal(messageType, wsrr.MessageType);
            Assert.Equal(endOfMessage, wsrr.EndOfMessage);
            Assert.Equal(closeStatus, wsrr.CloseStatus);
            Assert.Equal(closeStatusDescription, wsrr.CloseStatusDescription);
        }

        [Fact]
        public void ConstructorTest_Invalid_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WebSocketReceiveResult(-1, WebSocketMessageType.Text, false, null, null));
        }
    }
}

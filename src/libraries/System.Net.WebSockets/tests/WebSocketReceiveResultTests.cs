// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        [Theory, MemberData("ConstructorData")]
        public void ConstructorTest_Success(int count, WebSocketMessageType messageType, bool endOfMessage, WebSocketCloseStatus? closeStatus, string closeStatusDescription)
        {
            var wsrr = new WebSocketReceiveResult(count, messageType, endOfMessage, closeStatus, closeStatusDescription);
            Assert.Equal(wsrr.Count, count);
            Assert.Equal(wsrr.MessageType, messageType);
            Assert.Equal(wsrr.EndOfMessage, endOfMessage);
            Assert.Equal(wsrr.CloseStatus, closeStatus);
            Assert.Equal(wsrr.CloseStatusDescription, closeStatusDescription);
        }

        [Fact]
        public void ConstructorTest_Invalid_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WebSocketReceiveResult(-1, WebSocketMessageType.Text, false, null, null));
        }
    }
}

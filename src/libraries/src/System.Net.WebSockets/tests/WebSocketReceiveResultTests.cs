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

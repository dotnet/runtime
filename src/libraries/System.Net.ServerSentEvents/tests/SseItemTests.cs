// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Net.ServerSentEvents.Tests
{
    public partial class SseItemTests
    {
        [Fact]
        public void SseItem_Roundtrips()
        {
            SseItem<string> item;

            item = default;
            Assert.Null(item.Data);
            Assert.Equal(SseParser.EventTypeDefault, item.EventType);

            item = new SseItem<string>("some data", null);
            Assert.Equal("some data", item.Data);
            Assert.Equal(SseParser.EventTypeDefault, item.EventType);

            item = new SseItem<string>("some data", "eventType");
            Assert.Equal("some data", item.Data);
            Assert.Equal("eventType", item.EventType);
        }
    }
}

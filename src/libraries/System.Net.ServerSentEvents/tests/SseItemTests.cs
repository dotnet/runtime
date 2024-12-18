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
            Assert.Null(item.EventId);
            Assert.Null(item.ReconnectionInterval);

            item = new SseItem<string>("some data", null);
            Assert.Equal("some data", item.Data);
            Assert.Equal(SseParser.EventTypeDefault, item.EventType);
            Assert.Null(item.EventId);
            Assert.Null(item.ReconnectionInterval);

            item = new SseItem<string>("some data", "eventType") { EventId = "eventId", ReconnectionInterval = TimeSpan.FromSeconds(3) };
            Assert.Equal("some data", item.Data);
            Assert.Equal("eventType", item.EventType);
            Assert.Equal("eventId", item.EventId);
            Assert.Equal(TimeSpan.FromSeconds(3), item.ReconnectionInterval);
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("Hello, World!\n")]
        [InlineData("Hello, \r\nWorld!")]
        [InlineData("Hello, \rWorld!")]
        public void SseItem_MetadataWithLineBreak_ThrowsArgumentException(string metadataWithLineBreak)
        {
            Assert.Throws<ArgumentException>("eventType", () => new SseItem<string>("data", eventType: metadataWithLineBreak));
            Assert.Throws<ArgumentException>("EventId", () => new SseItem<string>("data", "eventType") { EventId = metadataWithLineBreak });
        }

        [Fact]
        public void SseItem_ReconnectionInterval_NegativeTimeSpan_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>("ReconnectionInterval", () => new SseItem<string>("data") { ReconnectionInterval = TimeSpan.FromSeconds(-1) });
        }
    }
}

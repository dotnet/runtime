// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.ServerSentEvents.Tests
{
    public partial class SseParserTests
    {
        [Fact]
        public void Parse_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("sseStream", () => SseParser.Create(null));
            AssertExtensions.Throws<ArgumentNullException>("sseStream", () => SseParser.Create(null, delegate { return ""; }));
            AssertExtensions.Throws<ArgumentNullException>("itemParser", () => SseParser.Create<string>(Stream.Null, null));
        }

        [Fact]
        public async Task Parse_Sync_SupportsOnlyOneEnumeration_Throws()
        {
            SseParser<string> parser = SseParser.Create(Stream.Null);
            parser.Enumerate().GetEnumerator().MoveNext();
            var e = parser.Enumerate().GetEnumerator();
            var ea = parser.EnumerateAsync().GetAsyncEnumerator();
            Assert.Throws<InvalidOperationException>(() => e.MoveNext());
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await ea.MoveNextAsync());
        }

        [Fact]
        public async Task Parse_Async_SupportsOnlyOneEnumeration_Throws()
        {
            SseParser<string> parser = SseParser.Create(Stream.Null);
            await parser.EnumerateAsync().GetAsyncEnumerator().MoveNextAsync();
            var ea = parser.EnumerateAsync().GetAsyncEnumerator();
            var e = parser.Enumerate().GetEnumerator();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await ea.MoveNextAsync());
            Assert.Throws<InvalidOperationException>(() => e.MoveNext());
        }

        [Fact]
        public void SseItem_Roundtrips()
        {
            SseItem<string> item;

            item = new SseItem<string>();
            Assert.Null(item.EventType);
            Assert.Null(item.Data);

            item = new SseItem<string>("some data", "eventType");
            Assert.Equal("eventType", item.EventType);
            Assert.Equal("some data", item.Data);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Parse_Empty_NoItems(string newline, bool trickle, bool useAsync)
        {
            _ = newline;

            using Stream stream = GetStream("", trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);
            Assert.Equal(stream.Length, stream.Position);

            Assert.Equal(0, items.Count);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Parse_BlankLine_NoItems(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream(newline, trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);
            Assert.Equal(stream.Length, stream.Position);

            Assert.Equal(0, items.Count);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Parse_TwoBlankLines_NoItems(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream(newline + newline, trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);
            Assert.Equal(stream.Length, stream.Position);

            Assert.Equal(0, items.Count);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Parse_MultipleBlankLinesBetweenItems_AllItemsProduces(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream(
                $"event:A{newline}" +
                $"data:1{newline}" +
                $"id:2{newline}" +
                $"retry:300{newline}" +
                $"{newline}{newline}{newline}{newline}{newline}" +

                $"event:B{newline}" +
                $"data:4{newline}" +
                $"id:5{newline}" +
                $"retry:600{newline}" +
                $"{newline}{newline}{newline}{newline}{newline}" +

                $"event:C{newline}" +
                $"data:7{newline}" +
                $"id:8{newline}" +
                $"retry:900{newline}" +
                $"{newline}{newline}{newline}{newline}{newline}",
                trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);
            Assert.Equal(stream.Length, stream.Position);

            Assert.Equal(3, items.Count);
            AssertSseItemEqual(new SseItem<string>("1", "A"), items[0]);
            AssertSseItemEqual(new SseItem<string>("4", "B"), items[1]);
            AssertSseItemEqual(new SseItem<string>("7", "C"), items[2]);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Parse_HtmlSpec_Example1(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream(
                $"data: This is the first message.{newline}" +
                $"{newline}" +
                $"data: This is the second message, it{newline}" +
                $"data: has two lines.{newline}" +
                $"{newline}" +
                $"data: This is the third message.{newline}" +
                $"{newline}",
                trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);

            Assert.Equal(3, items.Count);
            AssertSseItemEqual(new SseItem<string>("This is the first message.", "message"), items[0]);
            AssertSseItemEqual(new SseItem<string>("This is the second message, it\nhas two lines.", "message"), items[1]);
            AssertSseItemEqual(new SseItem<string>("This is the third message.", "message"), items[2]);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Parse_HtmlSpec_Example2(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream(
                $"event: add{newline}data: 73857293{newline}" +
                $"{newline}" +
                $"event: remove{newline}data: 2153{newline}" +
                $"{newline}" +
                $"event: add{newline}data: 113411{newline}" +
                $"{newline}",
                trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);

            Assert.Equal(3, items.Count);
            AssertSseItemEqual(new SseItem<string>("73857293", "add"), items[0]);
            AssertSseItemEqual(new SseItem<string>("2153", "remove"), items[1]);
            AssertSseItemEqual(new SseItem<string>("113411", "add"), items[2]);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Parse_HtmlSpec_Example3(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream(
                $"data: YHOO{newline}" +
                $"data: +2{newline}" +
                $"data: 10{newline}" +
                $"{newline}",
                trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);

            Assert.Equal(1, items.Count);
            AssertSseItemEqual(new SseItem<string>("YHOO\n+2\n10", "message"), items[0]);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Parse_HtmlSpec_Example4(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream(
                $": test stream{newline}" +
                $"{newline}" +
                $"data: first event{newline}" +
                $"id: 1{newline}" +
                $"{newline}" +
                $"data:second event{newline}" +
                $"id{newline}" +
                $"{newline}" +
                $"data:  third event{newline}" +
                $"{newline}",
                trickle);

            SseParser<string> parser = SseParser.Create(stream);
            if (useAsync)
            {
                Assert.Equal(string.Empty, parser.LastEventId);

                using IEnumerator<SseItem<string>> e = parser.Enumerate().GetEnumerator();

                Assert.True(e.MoveNext());
                AssertSseItemEqual(new SseItem<string>("first event", "message"), e.Current);
                Assert.Equal("1", parser.LastEventId);

                Assert.True(e.MoveNext());
                AssertSseItemEqual(new SseItem<string>("second event", "message"), e.Current);
                Assert.Equal(string.Empty, parser.LastEventId);

                Assert.True(e.MoveNext());
                AssertSseItemEqual(new SseItem<string>(" third event", "message"), e.Current);
                Assert.Equal(string.Empty, parser.LastEventId);
            }
            else
            {
                Assert.Equal(string.Empty, parser.LastEventId);

                await using IAsyncEnumerator<SseItem<string>> e = parser.EnumerateAsync().GetAsyncEnumerator();

                Assert.True(await e.MoveNextAsync());
                AssertSseItemEqual(new SseItem<string>("first event", "message"), e.Current);
                Assert.Equal("1", parser.LastEventId);

                Assert.True(await e.MoveNextAsync());
                AssertSseItemEqual(new SseItem<string>("second event", "message"), e.Current);
                Assert.Equal(string.Empty, parser.LastEventId);

                Assert.True(await e.MoveNextAsync());
                AssertSseItemEqual(new SseItem<string>(" third event", "message"), e.Current);
                Assert.Equal(string.Empty, parser.LastEventId);
            }
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Parse_HtmlSpec_Example4_InheritedIDs(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream(
                $": test stream{newline}" +
                $"{newline}" +
                $"data: first event{newline}" +
                $"id: 1{newline}" +
                $"{newline}" +
                $"data:second event{newline}" +
                $"{newline}" +
                $"data:  third event{newline}" +
                $"id: 42{newline}" +
                $"{newline}",
                trickle);

            SseParser<string> parser = SseParser.Create(stream);
            if (useAsync)
            {
                Assert.Equal(string.Empty, parser.LastEventId);

                using IEnumerator<SseItem<string>> e = parser.Enumerate().GetEnumerator();

                Assert.True(e.MoveNext());
                AssertSseItemEqual(new SseItem<string>("first event", "message"), e.Current);
                Assert.Equal("1", parser.LastEventId);

                Assert.True(e.MoveNext());
                AssertSseItemEqual(new SseItem<string>("second event", "message"), e.Current);
                Assert.Equal("1", parser.LastEventId);

                Assert.True(e.MoveNext());
                AssertSseItemEqual(new SseItem<string>(" third event", "message"), e.Current);
                Assert.Equal("42", parser.LastEventId);
            }
            else
            {
                Assert.Equal(string.Empty, parser.LastEventId);

                await using IAsyncEnumerator<SseItem<string>> e = parser.EnumerateAsync().GetAsyncEnumerator();

                Assert.True(await e.MoveNextAsync());
                AssertSseItemEqual(new SseItem<string>("first event", "message"), e.Current);
                Assert.Equal("1", parser.LastEventId);

                Assert.True(await e.MoveNextAsync());
                AssertSseItemEqual(new SseItem<string>("second event", "message"), e.Current);
                Assert.Equal("1", parser.LastEventId);

                Assert.True(await e.MoveNextAsync());
                AssertSseItemEqual(new SseItem<string>(" third event", "message"), e.Current);
                Assert.Equal("42", parser.LastEventId);
            }
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Parse_HtmlSpec_Example5(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream(
                $"data{newline}" +
                $"{newline}" +
                $"data{newline}" +
                $"data{newline}" +
                $"{newline}" +
                $"data:",
                trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);

            Assert.Equal(2, items.Count);
            AssertSseItemEqual(new SseItem<string>("", "message"), items[0]);
            AssertSseItemEqual(new SseItem<string>("\n", "message"), items[1]);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Parse_HtmlSpec_Example5_WithColon(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream(
                $"data{newline}" +
                $"{newline}" +
                $"data:{newline}" +
                $"data{newline}" +
                $"{newline}" +
                $"data:",
                trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);

            Assert.Equal(2, items.Count);
            AssertSseItemEqual(new SseItem<string>("", "message"), items[0]);
            AssertSseItemEqual(new SseItem<string>("\n", "message"), items[1]);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Parse_HtmlSpec_Example6(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream(
                $"data:test{newline}" +
                $"{newline}" +
                $"data: test{newline}" +
                $"{newline}",
                trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);

            Assert.Equal(2, items.Count);
            AssertSseItemEqual(new SseItem<string>("test", "message"), items[0]);
            AssertSseItemEqual(new SseItem<string>("test", "message"), items[1]);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Delegate_EventTypeArgument_MatchesValueFromEvent(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream(
                $"event: add{newline}data: 73857293{newline}" +
                $"{newline}" +
                $"event: remove{newline}data: 2153{newline}" +
                $"{newline}" +
                $"event: add{newline}data: 113411{newline}" +
                $"{newline}",
                trickle);

            SseItemParser<string> itemParser = (eventType, bytes) => eventType;

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream, itemParser) :
                ReadAllEvents(stream, itemParser);

            Assert.Equal(3, items.Count);
            AssertSseItemEqual(new SseItem<string>("add", "add"), items[0]);
            AssertSseItemEqual(new SseItem<string>("remove", "remove"), items[1]);
            AssertSseItemEqual(new SseItem<string>("add", "add"), items[2]);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Retry_SetsReconnectionInterval(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream(
                $": test stream{newline}" +
                $"{newline}" +
                $"data: first event{newline}" +
                $"{newline}" +
                $"data:second event{newline}" +
                $"retry: 42{newline}" +
                $"{newline}" +
                $"data:  third event{newline}" +
                $"retry: 12345678910{newline}" +
                $"{newline}" +
                $"data:fourth event{newline}" +
                $"{newline}" +
                $"data:fifth event{newline}" +
                $"retry: invalidmilliseconds{newline}" +
                $"{newline}",
                trickle);

            SseParser<string> parser = SseParser.Create(stream);
            Assert.Equal(Timeout.InfiniteTimeSpan, parser.ReconnectionInterval);

            if (useAsync)
            {
                Assert.Equal(string.Empty, parser.LastEventId);

                using IEnumerator<SseItem<string>> e = parser.Enumerate().GetEnumerator();
                Assert.Equal(Timeout.InfiniteTimeSpan, parser.ReconnectionInterval);

                Assert.True(e.MoveNext());
                AssertSseItemEqual(new SseItem<string>("first event", "message"), e.Current);
                Assert.Equal(Timeout.InfiniteTimeSpan, parser.ReconnectionInterval);

                Assert.True(e.MoveNext());
                AssertSseItemEqual(new SseItem<string>("second event", "message"), e.Current);
                Assert.Equal(TimeSpan.FromMilliseconds(42), parser.ReconnectionInterval);

                Assert.True(e.MoveNext());
                AssertSseItemEqual(new SseItem<string>(" third event", "message"), e.Current);
                Assert.Equal(TimeSpan.FromMilliseconds(12345678910), parser.ReconnectionInterval);

                Assert.True(e.MoveNext());
                AssertSseItemEqual(new SseItem<string>("fourth event", "message"), e.Current);
                Assert.Equal(TimeSpan.FromMilliseconds(12345678910), parser.ReconnectionInterval);

                Assert.True(e.MoveNext());
                AssertSseItemEqual(new SseItem<string>("fifth event", "message"), e.Current);
                Assert.Equal(TimeSpan.FromMilliseconds(12345678910), parser.ReconnectionInterval);
            }
            else
            {
                Assert.Equal(string.Empty, parser.LastEventId);

                await using IAsyncEnumerator<SseItem<string>> e = parser.EnumerateAsync().GetAsyncEnumerator();
                Assert.Equal(Timeout.InfiniteTimeSpan, parser.ReconnectionInterval);

                Assert.True(await e.MoveNextAsync());
                AssertSseItemEqual(new SseItem<string>("first event", "message"), e.Current);

                Assert.True(await e.MoveNextAsync());
                AssertSseItemEqual(new SseItem<string>("second event", "message"), e.Current);
                Assert.Equal(TimeSpan.FromMilliseconds(42), parser.ReconnectionInterval);

                Assert.True(await e.MoveNextAsync());
                AssertSseItemEqual(new SseItem<string>(" third event", "message"), e.Current);
                Assert.Equal(TimeSpan.FromMilliseconds(12345678910), parser.ReconnectionInterval);

                Assert.True(await e.MoveNextAsync());
                AssertSseItemEqual(new SseItem<string>("fourth event", "message"), e.Current);
                Assert.Equal(TimeSpan.FromMilliseconds(12345678910), parser.ReconnectionInterval);

                Assert.True(await e.MoveNextAsync());
                AssertSseItemEqual(new SseItem<string>("fifth event", "message"), e.Current);
                Assert.Equal(TimeSpan.FromMilliseconds(12345678910), parser.ReconnectionInterval);
            }
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task JsonContent_DelegateInvoked(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream(
                $"data: {{{newline}" +
                $"data:    \"title\": \"The Catcher in the Rye\",{newline}" +
                $"data:    \"author\": \"J.D. Salinger\",{newline}" +
                $"data:    \"published_year\": 1951,{newline}" +
                $"data:    \"genre\": \"Fiction\"{newline}" +
                $"data: }}{newline}" +
                $"{newline}" +
                $"data: {{{newline}" +
                $"data:    \"title\": \"1984\",{newline}" +
                $"data:    \"author\": \"George Orwell\",{newline}" +
                $"data:    \"published_year\": 1949,{newline}" +
                $"data:    \"genre\": \"Fiction\"{newline}" +
                $"data: }}{newline}" +
                $"{newline}",
                trickle);

            List<SseItem<Book>> items = useAsync ?
                await ReadAllEventsAsync(stream, static (eventType, data) => JsonSerializer.Deserialize(data, JsonSerializerTestContext.Default.Book)) :
                ReadAllEvents(stream, static (eventType, data) => JsonSerializer.Deserialize(data, JsonSerializerTestContext.Default.Book));

            Assert.Equal(2, items.Count);
            AssertSseItemEqual(new SseItem<Book>(new Book { title = "The Catcher in the Rye", author = "J.D. Salinger", published_year = 1951, genre = "Fiction" }, "message"), items[0]);
            AssertSseItemEqual(new SseItem<Book>(new Book { title = "1984", author = "George Orwell", published_year = 1949, genre = "Fiction" }, "message"), items[1]);
        }

        private struct Book
        {
            public string title { get; set; }
            public string author { get; set; }
            public int published_year { get; set; }
            public string genre { get; set; }
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Bom_Valid_Skipped(string newline, bool trickle, bool useAsync)
        {
            byte[] newlineBytes = Encoding.UTF8.GetBytes(newline);
            using Stream stream = GetStream(
                [
                    0xEF, 0xBB, 0xBF,
                    (byte)'d', (byte)'a', (byte)'t', (byte)'a', (byte)':',
                    (byte)'h', (byte)'i',
                    ..newlineBytes,
                    ..newlineBytes,
                    (byte)'d', (byte)'a', (byte)'t', (byte)'a', (byte)':',
                    (byte)'t', (byte)'h', (byte)'e', (byte)'r', (byte)'e',
                    ..newlineBytes,
                    ..newlineBytes,
                ], trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);

            Assert.Equal(2, items.Count);
            AssertSseItemEqual(new SseItem<string>("hi", "message"), items[0]);
            AssertSseItemEqual(new SseItem<string>("there", "message"), items[1]);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Bom_Partial1_LineSkipped(string newline, bool trickle, bool useAsync)
        {
            byte[] newlineBytes = Encoding.UTF8.GetBytes(newline);
            using Stream stream = GetStream(
                [
                    0xEF,
                    (byte)'d', (byte)'a', (byte)'t', (byte)'a', (byte)':',
                    (byte)'h', (byte)'i',
                    ..newlineBytes,
                    ..newlineBytes,
                    (byte)'d', (byte)'a', (byte)'t', (byte)'a', (byte)':',
                    (byte)'t', (byte)'h', (byte)'e', (byte)'r', (byte)'e',
                    ..newlineBytes,
                    ..newlineBytes,
                ], trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);

            Assert.Equal(1, items.Count);
            AssertSseItemEqual(new SseItem<string>("there", "message"), items[0]);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Bom_Partial2_LineSkipped(string newline, bool trickle, bool useAsync)
        {
            byte[] newlineBytes = Encoding.UTF8.GetBytes(newline);
            using Stream stream = GetStream(
                [
                    0xEF, 0xBB,
                    (byte)'d', (byte)'a', (byte)'t', (byte)'a', (byte)':',
                    (byte)'h', (byte)'i',
                    ..newlineBytes,
                    ..newlineBytes,
                    (byte)'d', (byte)'a', (byte)'t', (byte)'a', (byte)':',
                    (byte)'t', (byte)'h', (byte)'e', (byte)'r', (byte)'e',
                    ..newlineBytes,
                    ..newlineBytes,
                ], trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);

            Assert.Equal(1, items.Count);
            AssertSseItemEqual(new SseItem<string>("there", "message"), items[0]);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Bom_Interrupted1_NoItems(string newline, bool trickle, bool useAsync)
        {
            _ = newline;

            using Stream stream = GetStream([0xEF], trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);

            Assert.Equal(0, items.Count);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Bom_Interrupted2_NoItems(string newline, bool trickle, bool useAsync)
        {
            _ = newline;

            using Stream stream = GetStream([0xEF, 0xBB], trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);

            Assert.Equal(0, items.Count);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Bom_Interrupted3_NoItems(string newline, bool trickle, bool useAsync)
        {
            _ = newline;

            using Stream stream = GetStream([0xEF, 0xBB, 0xBF], trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);

            Assert.Equal(0, items.Count);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task LongLines_ItemsProducedCorrectly(string newline, bool trickle, bool useAsync)
        {
            string[] expected = Enumerable.Range(1, 100).Select(i => string.Concat(Enumerable.Repeat($"{i} ", i))).ToArray();

            using Stream stream = GetStream([.. expected.Select(s => $"data: {s}{newline}{newline}").SelectMany(Encoding.UTF8.GetBytes)], trickle);

            List<SseItem<string>> items = useAsync ?
                await ReadAllEventsAsync(stream) :
                ReadAllEvents(stream);

            Assert.Equal(expected.Length, items.Count);
            for (int i = 0; i < expected.Length; i++)
            {
                AssertSseItemEqual(new SseItem<string>(expected[i], "message"), items[i]);
            }
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task Delegate_ThrowsException_Propagates(string newline, bool trickle, bool useAsync)
        {
            using Stream stream = GetStream($"data: hello{newline}{newline}data:world{newline}{newline}", trickle);

            SseParser<string> parser = SseParser.Create<string>(stream, (eventType, bytes) => throw new FormatException(Encoding.UTF8.GetString(bytes.ToArray())));

            FormatException fe;
            if (useAsync)
            {
                await using IAsyncEnumerator<SseItem<string>> e = parser.EnumerateAsync().GetAsyncEnumerator();
                fe = await Assert.ThrowsAsync<FormatException>(async () => await e.MoveNextAsync());
            }
            else
            {
                using IEnumerator<SseItem<string>> e = parser.Enumerate().GetEnumerator();
                fe = Assert.Throws<FormatException>(() => e.MoveNext());
            }

            Assert.Equal("hello", fe.Message);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task Cancellation_Propagates(bool cancelEnumerator)
        {
            using Stream stream = GetStream($"data: hello\n\ndata:world\n\n", trickle: true);

            SseParser<string> parser = SseParser.Create(stream);

            var cts = new CancellationTokenSource();
            cts.Cancel();

            await using IAsyncEnumerator<SseItem<string>> e = cancelEnumerator ?
                parser.EnumerateAsync().GetAsyncEnumerator(cts.Token) :
                parser.EnumerateAsync(cts.Token).GetAsyncEnumerator();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await e.MoveNextAsync());
        }

        [Fact]
        public void NonGenericEnumerator_ProducesExpectedItems()
        {
            using Stream stream = GetStream($"data: hello\n\ndata:world\n\n", trickle: false);

            IEnumerable sse = SseParser.Create(stream).Enumerate();
            IEnumerator e = sse.GetEnumerator();

            Assert.True(e.MoveNext());
            AssertSseItemEqual(new SseItem<string>("hello", "message"), (SseItem<string>)e.Current);

            Assert.True(e.MoveNext());
            AssertSseItemEqual(new SseItem<string>("world", "message"), (SseItem<string>)e.Current);

            Assert.False(e.MoveNext());
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task MultipleItemParsers_OpenAI_StreamingResponse(string newline, bool trickle, bool useAsync)
        {
            string exampleResponse =
                $"data: {{\"id\":\"xxx\",\"object\":\"chat.completion.chunk\",\"created\":1679168243,\"model\":\"mmm\",\"choices\":[{{\"delta\":{{\"content\":\"!\"}},\"index\":0,\"finish_reason\":null}}]}}{newline}{newline}" +
                $"data: {{\"id\":\"yyy\",\"object\":\"chat.completion.chunk\",\"created\":1679168243,\"model\":\"mmm\",\"choices\":[{{\"delta\":{{}},\"index\":0,\"finish_reason\":\"stop\"}}]}}{newline}{newline}" +
                $"data: [DONE]{newline}{newline}";

            using Stream stream = GetStream(exampleResponse, trickle);

            SseItemParser<ChunkOrDone> itemParser = (eventType, bytes) =>
            {
                return bytes.SequenceEqual("[DONE]"u8) ?
                    new ChunkOrDone { Done = true } :
                    new ChunkOrDone { Json = JsonSerializer.Deserialize(bytes, JsonSerializerTestContext.Default.JsonElement) };
            };

            List<SseItem<ChunkOrDone>> items = useAsync ?
                await ReadAllEventsAsync(stream, itemParser) :
                ReadAllEvents(stream, itemParser);

            Assert.Equal(3, items.Count);
            Assert.False(items[0].Data.Done);
            Assert.False(items[1].Data.Done);
            Assert.True(items[2].Data.Done);
        }

        private struct ChunkOrDone
        {
            public JsonElement Json { get; set; }
            public bool Done { get; set; }
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task ArrayPoolRental_PerItem(string newline, bool trickle, bool useAsync)
        {
            string exampleResponse =
                $"data: {{\"id\":\"xxx\",\"object\":\"chat.completion.chunk\",\"created\":1679168243,\"model\":\"mmm\",\"choices\":[{{\"delta\":{{\"content\":\"!\"}},\"index\":0,\"finish_reason\":null}}]}}{newline}{newline}" +
                $"data: {{\"id\":\"yyy\",\"object\":\"chat.completion.chunk\",\"created\":1679168243,\"model\":\"mmm\",\"choices\":[{{\"delta\":{{}},\"index\":0,\"finish_reason\":\"stop\"}}]}}{newline}{newline}" +
                $"data: [DONE]{newline}{newline}";

            using Stream stream = GetStream(exampleResponse, trickle);

            SseItemParser<ArraySegment<byte>> itemParser = (eventType, bytes) =>
            {
                byte[] array = ArrayPool<byte>.Shared.Rent(bytes.Length);
                bytes.CopyTo(array.AsSpan());
                return new ArraySegment<byte>(array, 0, bytes.Length);
            };

            int count = 0;
            if (useAsync)
            {
                foreach (var e in SseParser.Create(stream, itemParser).Enumerate())
                {
                    try
                    {
                        if ("[DONE]"u8.SequenceEqual(e.Data))
                        {
                            break;
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(e.Data.Array);
                    }

                    count++;
                }
            }
            else
            {
                await foreach (var e in SseParser.Create(stream, itemParser).EnumerateAsync())
                {
                    try
                    {
                        if ("[DONE]"u8.SequenceEqual(e.Data))
                        {
                            break;
                        }
                    }
                    finally
                    {
                        ArrayPool<byte>.Shared.Return(e.Data.Array);
                    }

                    count++;
                }
            }

            Assert.Equal(2, count);
        }

        [Theory]
        [MemberData(nameof(NewlineTrickleAsyncData))]
        public async Task ArrayPoolRental_Closure(string newline, bool trickle, bool useAsync)
        {
            string exampleResponse =
                $"data: {{\"id\":\"xxx\",\"object\":\"chat.completion.chunk\",\"created\":1679168243,\"model\":\"mmm\",\"choices\":[{{\"delta\":{{\"content\":\"!\"}},\"index\":0,\"finish_reason\":null}}]}}{newline}{newline}" +
                $"data: {{\"id\":\"yyy\",\"object\":\"chat.completion.chunk\",\"created\":1679168243,\"model\":\"mmm\",\"choices\":[{{\"delta\":{{}},\"index\":0,\"finish_reason\":\"stop\"}}]}}{newline}{newline}" +
                $"data: [DONE]{newline}{newline}";

            using Stream stream = GetStream(exampleResponse, trickle);

            byte[] arrayPoolArray = ArrayPool<byte>.Shared.Rent(1);

            SseItemParser<ReadOnlyMemory<byte>> itemParser = (eventType, bytes) =>
            {
                if (arrayPoolArray.Length < bytes.Length)
                {
                    byte[] temp = arrayPoolArray;
                    arrayPoolArray = ArrayPool<byte>.Shared.Rent(bytes.Length);
                    ArrayPool<byte>.Shared.Return(temp);
                }
                bytes.CopyTo(arrayPoolArray.AsSpan());
                return new ReadOnlyMemory<byte>(arrayPoolArray, 0, bytes.Length);
            };

            int count = 0;
            if (useAsync)
            {
                foreach (var e in SseParser.Create(stream, itemParser).Enumerate())
                {
                    if ("[DONE]"u8.SequenceEqual(e.Data.Span))
                    {
                        break;
                    }
                    count++;
                }
            }
            else
            {
                await foreach (var e in SseParser.Create(stream, itemParser).EnumerateAsync())
                {
                    if ("[DONE]"u8.SequenceEqual(e.Data.Span))
                    {
                        break;
                    }
                    count++;
                }
            }

            ArrayPool<byte>.Shared.Return(arrayPoolArray);

            Assert.Equal(2, count);
        }

        private static void AssertSseItemEqual<T>(SseItem<T> left, SseItem<T> right)
        {
            Assert.Equal(left.EventType, right.EventType);
            if (left.Data is string leftData && right.Data is string rightData)
            {
                Assert.Equal($"{leftData.Length} {leftData}", $"{rightData.Length} {rightData}");
            }
            else
            {
                Assert.Equal(left.Data, right.Data);
            }
        }

        public static IEnumerable<object[]> NewlineTrickleAsyncData() =>
            from newline in new[] { "\r", "\n", "\r\n" }
            from trickle in new[] { false, true }
            from async in new[] { false, true }
            select new object[] { newline, trickle, async };

        private static Stream GetStream(string data, bool trickle) =>
            GetStream(Encoding.UTF8.GetBytes(data), trickle);

        private static Stream GetStream(byte[] bytes, bool trickle) =>
            trickle ? new TrickleStream(bytes) : new MemoryStream(bytes);

        private static List<SseItem<string>> ReadAllEvents(Stream stream)
        {
            return new List<SseItem<string>>(SseParser.Create(stream).Enumerate());
        }

        private static List<SseItem<T>> ReadAllEvents<T>(Stream stream, SseItemParser<T> parser)
        {
            return new List<SseItem<T>>(SseParser.Create(stream, parser).Enumerate());
        }

        private static async Task<List<SseItem<T>>> ReadAllEventsAsync<T>(Stream stream, SseItemParser<T> parser)
        {
            var list = new List<SseItem<T>>();
            await foreach (SseItem<T> item in SseParser.Create(stream, parser).EnumerateAsync())
            {
                list.Add(item);
            }

            return list;
        }

        private static async Task<List<SseItem<string>>> ReadAllEventsAsync(Stream stream)
        {
            var list = new List<SseItem<string>>();
            await foreach (SseItem<string> item in SseParser.Create(stream).EnumerateAsync())
            {
                list.Add(item);
            }

            return list;
        }

        /// <summary>Stream where each read reads at most one byte and where every asynchronous operation yields.</summary>
        private sealed class TrickleStream : MemoryStream
        {
            public TrickleStream(byte[] buffer) : base(buffer) { }

            public override int Read(byte[] buffer, int offset, int count) =>
                base.Read(buffer, offset, Math.Min(count, 1));

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                return await base.ReadAsync(buffer, offset, Math.Min(count, 1), cancellationToken);
            }

#if NET
            public override int Read(Span<byte> buffer) =>
                base.Read(buffer.Slice(0, Math.Min(buffer.Length, 1)));

            public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();
                return await base.ReadAsync(buffer.Slice(0, Math.Min(buffer.Length, 1)), cancellationToken);
            }
#endif
        }

        [JsonSerializable(typeof(Book))]
        [JsonSerializable(typeof(JsonElement))]
        private sealed partial class JsonSerializerTestContext : JsonSerializerContext;
    }
}

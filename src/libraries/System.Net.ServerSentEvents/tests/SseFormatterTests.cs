// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.ServerSentEvents.Tests
{
    public static partial class SseFormatterTests
    {
        [Fact]
        public static void WriteAsync_InvalidArguments_Throws()
        {
            AssertExtensions.Throws<ArgumentNullException>("source", () => SseFormatter.WriteAsync(source: null, new MemoryStream()));
            AssertExtensions.Throws<ArgumentNullException>("source", () => SseFormatter.WriteAsync<string>(source: null, new MemoryStream(), (_,_) => { }));

            AssertExtensions.Throws<ArgumentNullException>("destination", () => SseFormatter.WriteAsync(GetItemsAsync(), destination: null));
            AssertExtensions.Throws<ArgumentNullException>("destination", () => SseFormatter.WriteAsync<string>(GetItemsAsync(), destination: null, (_,_) => { }));

            AssertExtensions.Throws<ArgumentNullException>("itemFormatter", () => SseFormatter.WriteAsync(GetItemsAsync(), new MemoryStream(), itemFormatter: null));
        }

        [Fact]
        public static async Task WriteAsync_HasExpectedFormat()
        {
            // Arrange
            string expectedFormat =
                "event: eventType1\ndata: data1\n\n" +
                "event: eventType2\ndata: data2\nretry: 300\n\n" +
                "data: data3\n\n" +
                "data: \n\n" +
                "event: eventType4\ndata: data4\nid: id4\n\n" +
                "event: eventType4\ndata: data\ndata: \ndata:  with\ndata: multiple \ndata: line\ndata: breaks\n\n" +
                "data: LF at end\ndata: \n\n" +
                "data: CR at end\ndata: \n\n" +
                "data: CRLF at end\ndata: \n\n" +
                "data: LFCR at end\ndata: \ndata: \n\n";

            using MemoryStream stream = new();

            // Act
            await SseFormatter.WriteAsync(GetItemsAsync(), stream);

            // Assert
            string actualFormat = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expectedFormat, actualFormat);
        }

        [Fact]
        public static async Task WriteAsync_ItemFormatter_HasExpectedFormat()
        {
            // Arrange
            string expectedFormat =
                "event: eventType1\ndata: data1_suffix\n\n" +
                "event: eventType2\ndata: data2_suffix\nretry: 300\n\n" +
                "data: data3_suffix\n\n" +
                "data: _suffix\n\n" +
                "event: eventType4\ndata: data4_suffix\nid: id4\n\n" +
                "event: eventType4\ndata: data\ndata: \ndata:  with\ndata: multiple \ndata: line\ndata: breaks_suffix\n\n" +
                "data: LF at end\ndata: _suffix\n\n" +
                "data: CR at end\ndata: _suffix\n\n" +
                "data: CRLF at end\ndata: _suffix\n\n" +
                "data: LFCR at end\ndata: \ndata: _suffix\n\n";

            using MemoryStream stream = new();

            // Act
            await SseFormatter.WriteAsync(GetItemsAsync(), stream, (item, writer) => writer.Write(Encoding.UTF8.GetBytes(item.Data + "_suffix")));

            // Assert
            string actualFormat = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(expectedFormat, actualFormat);
        }

        private static async IAsyncEnumerable<SseItem<string>> GetItemsAsync()
        {
            yield return new SseItem<string>("data1", "eventType1");
            yield return new SseItem<string>("data2", "eventType2") { ReconnectionInterval = TimeSpan.FromMilliseconds(300) };
            await Task.Yield();
            yield return new SseItem<string>("data3", null);
            yield return new SseItem<string>(data: null!, null);
            yield return new SseItem<string>("data4", "eventType4") { EventId = "id4" };
            await Task.Yield();
            yield return new SseItem<string>("data\n\r with\nmultiple \rline\r\nbreaks", "eventType4");
            yield return new SseItem<string>("LF at end\n", null);
            yield return new SseItem<string>("CR at end\r", null);
            yield return new SseItem<string>("CRLF at end\r\n", null);
            yield return new SseItem<string>("LFCR at end\n\r", null);
        }

        [Fact]
        public static async Task WriteAsync_HonorsCancellationToken()
        {
            CancellationToken token = new(canceled: true);

            await Assert.ThrowsAsync<TaskCanceledException>(() => SseFormatter.WriteAsync(GetItemsAsync(), new MemoryStream(), token));
            await Assert.ThrowsAsync<TaskCanceledException>(() =>
                SseFormatter.WriteAsync(
                    GetItemsAsync(),
                    new MemoryStream(),
                    (item, writer) => writer.Write(Encoding.UTF8.GetBytes(item.Data)),
                    token));

            async IAsyncEnumerable<SseItem<string>> GetItemsAsync([EnumeratorCancellation] CancellationToken token = default)
            {
                yield return new SseItem<string>("data");
                await Task.Delay(20);
                token.ThrowIfCancellationRequested();
            }
        }

        [Fact]
        public static async Task WriteLargeItems_DataWrittenSuccessfully()
        {
            const int NumberOfItems = 10;
            byte[] expected = Encoding.UTF8.GetBytes(string.Concat(Enumerable.Repeat("This is a test. This is only a test.", 100)));

            MemoryStream memoryStream = new();
            await SseFormatter.WriteAsync(GetBuffersAsync(), memoryStream, (item, writer) => writer.Write(item.Data));

            memoryStream.Position = 0;
            int count = 0;
            foreach (SseItem<byte[]> item in SseParser.Create(memoryStream, (eventType, data) => data.ToArray()).Enumerate())
            {
                Assert.Equal(expected, item.Data);
                count++;
            }

            Assert.Equal(NumberOfItems, count);

            async IAsyncEnumerable<SseItem<byte[]>> GetBuffersAsync()
            {
                await Task.Yield();
                for (int i = 0; i < NumberOfItems; i++)
                {
                    yield return new SseItem<byte[]>(expected);
                }
            }
        }

        [Fact]
        public static async Task WriteAsync_ParserCanRoundtripJsonEvents()
        {
            MemoryStream stream = new();
            await SseFormatter.WriteAsync(GetItemsAsync(), stream, FormatJson);

            stream.Position = 0;
            SseParser<MyPoco> parser = SseParser.Create(stream, ParseJson);
            await ValidateParseResults(parser.EnumerateAsync());

            async IAsyncEnumerable<SseItem<MyPoco>> GetItemsAsync()
            {
                for (int i = 0; i < 50; i++)
                {
                    string? eventType = i % 2 == 0 ? null : "eventType";
                    string? eventId = i % 3 == 2 ? i.ToString() : null;
                    TimeSpan? reconnectionInterval = i % 5 == 4 ? TimeSpan.FromSeconds(i) : null;
                    yield return new SseItem<MyPoco>(new MyPoco(i), eventType) { EventId = eventId, ReconnectionInterval = reconnectionInterval };
                    await Task.Yield();
                }
            }

            async Task ValidateParseResults(IAsyncEnumerable<SseItem<MyPoco>> results)
            {
                int i = 0;
                await foreach (SseItem<MyPoco> item in results)
                {
                    Assert.Equal(i % 2 == 0 ? "message" : "eventType", item.EventType);
                    Assert.Equal(i % 3 == 2 ? i.ToString() : null, item.EventId);
                    Assert.Equal(i % 5 == 4 ? TimeSpan.FromSeconds(i) : null, item.ReconnectionInterval);
                    Assert.Equal(i, item.Data.Value);
                    i++;
                }
            }

            static void FormatJson(SseItem<MyPoco> item, IBufferWriter<byte> writer)
            {
                JsonWriterOptions writerOptions = new() { Indented = true };
                using Utf8JsonWriter jsonWriter = new(writer, writerOptions);
                JsonSerializer.Serialize(jsonWriter, item.Data, JsonContext.Default.MyPoco);
            }

            static MyPoco ParseJson(string eventType, ReadOnlySpan<byte> data)
            {
                return JsonSerializer.Deserialize(data, JsonContext.Default.MyPoco);
            }
        }

        public record MyPoco(int Value);

        [JsonSerializable(typeof(MyPoco))]
        partial class JsonContext : JsonSerializerContext;
    }
}

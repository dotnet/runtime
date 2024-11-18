// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.IO;
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
                "event: eventType4\ndata: data with\ndata: multiple \rline\ndata: breaks\n\n" +
                "data: line break at end\ndata: \n\n";

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
                "event: eventType4\ndata: data with\ndata: multiple \rline\ndata: breaks_suffix\n\n" +
                "data: line break at end\ndata: _suffix\n\n";

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
            yield return new SseItem<string>("data with\nmultiple \rline\r\nbreaks", "eventType4");
            yield return new SseItem<string>("line break at end\n", null);
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

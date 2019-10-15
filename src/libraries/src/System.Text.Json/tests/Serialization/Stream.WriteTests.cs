// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class StreamTests
    {
        [Fact]
        public static async Task VerifyValueFail()
        {
            MemoryStream stream = new MemoryStream();
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.SerializeAsync(stream, "", (Type)null));
        }

        [Fact]
        public static async Task VerifyTypeFail()
        {
            MemoryStream stream = new MemoryStream();
            await Assert.ThrowsAsync<ArgumentException>(async () => await JsonSerializer.SerializeAsync(stream, 1, typeof(string)));
        }

        [Fact]
        public static async Task NullObjectValue()
        {
            MemoryStream stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, (object)null);

            stream.Seek(0, SeekOrigin.Begin);

            byte[] readBuffer = new byte[4];
            int bytesRead = stream.Read(readBuffer, 0, 4);

            Assert.Equal(4, bytesRead);
            string value = Encoding.UTF8.GetString(readBuffer);
            Assert.Equal("null", value);
        }

        [Fact]
        public static async Task RoundTripAsync()
        {
            byte[] buffer;

            using (TestStream stream = new TestStream(1))
            {
                await WriteAsync(stream);

                // Make a copy
                buffer = stream.ToArray();
            }

            using (TestStream stream = new TestStream(buffer))
            {
                await ReadAsync(stream);
            }
        }

        [Fact]
        public static async Task RoundTripLargeJsonViaJsonElementAsync()
        {
            // Generating tailored json
            int i = 0;
            StringBuilder json = new StringBuilder();
            json.Append("{");
            while (true)
            {
                if (json.Length >= 14757)
                {
                    break;
                }
                json.AppendFormat(@"""Key_{0}"":""{0}"",", i);
                i++;
            }
            json.Remove(json.Length - 1, 1).Append("}");

            JsonElement root = JsonSerializer.Deserialize<JsonElement>(json.ToString());
            var ms = new MemoryStream();
            await JsonSerializer.SerializeAsync(ms, root, root.GetType());
        }

        [Fact]
        public static async Task RoundTripLargeJsonViaPocoAsync()
        {
            byte[] array = JsonSerializer.Deserialize<byte[]>(JsonSerializer.Serialize(new byte[11056]));
            var ms = new MemoryStream();
            await JsonSerializer.SerializeAsync(ms, array, array.GetType());
        }

        private static async Task WriteAsync(TestStream stream)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                // Will likely default to 4K due to buffer pooling.
                DefaultBufferSize = 1
            };

            {
                LargeDataTestClass obj = new LargeDataTestClass();
                obj.Initialize();
                obj.Verify();

                await JsonSerializer.SerializeAsync(stream, obj, options: options);
            }

            // Must be changed if the test classes change:
            Assert.Equal(551_368, stream.TestWriteBytesCount);

            // We should have more than one write called due to the large byte count.
            Assert.InRange(stream.TestWriteCount, 1, int.MaxValue);

            // We don't auto-flush.
            Assert.Equal(0, stream.TestFlushCount);
        }

        private static async Task ReadAsync(TestStream stream)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                // Will likely default to 4K due to buffer pooling.
                DefaultBufferSize = 1
            };

            LargeDataTestClass obj = await JsonSerializer.DeserializeAsync<LargeDataTestClass>(stream, options);
            // Must be changed if the test classes change; may be > since last read may not have filled buffer.
            Assert.InRange(stream.TestRequestedReadBytesCount, 551368, int.MaxValue);

            // We should have more than one read called due to the large byte count.
            Assert.InRange(stream.TestReadCount, 1, int.MaxValue);

            // We don't auto-flush.
            Assert.Equal(0, stream.TestFlushCount);

            obj.Verify();
        }

        [Fact]
        public static async Task WritePrimitivesAsync()
        {
            MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(@"1"));
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = 1
            };

            int i = await JsonSerializer.DeserializeAsync<int>(stream, options);
            Assert.Equal(1, i);
        }

        private class Session
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public virtual string Abstract { get; set; }
            public virtual DateTimeOffset? StartTime { get; set; }
            public virtual DateTimeOffset? EndTime { get; set; }
            public TimeSpan Duration => EndTime?.Subtract(StartTime ?? EndTime ?? DateTimeOffset.MinValue) ?? TimeSpan.Zero;
            public int? TrackId { get; set; }
        }

        private class SessionResponse : Session
        {
            public Track Track { get; set; }
            public List<Speaker> Speakers { get; set; } = new List<Speaker>();
        }

        private class Track
        {
            public int Id { get; set; }
            public string Name { get; set; }
        }

        private class Speaker
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public string Bio { get; set; }
            public virtual string WebSite { get; set; }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(10)]
        [InlineData(1000)]
        [InlineData(4000)]
        [InlineData(8000)]
        [InlineData(16000)]
        public static async Task LargeJsonFile(int bufferSize)
        {
            const int SessionResponseCount = 100;

            // Build up a large list to serialize.
            var list = new List<SessionResponse>();
            for (int i = 0; i < SessionResponseCount; i++)
            {
                SessionResponse response = new SessionResponse
                {
                    Id = i,
                    Abstract = new string('A', i * 2),
                    Title = new string('T', i),
                    StartTime = new DateTime(i, DateTimeKind.Utc),
                    EndTime = new DateTime(i * 10000, DateTimeKind.Utc),
                    TrackId = i,
                    Track = new Track()
                    {
                        Id = i,
                        Name = new string('N', i),
                    },
                };

                for (int j = 0; j < 5; j++)
                {
                    response.Speakers.Add(new Speaker()
                    {
                        Bio = new string('B', 50),
                        Id = j,
                        Name = new string('N', i),
                        WebSite = new string('W', 20),
                    });
                }

                list.Add(response);
            }

            // Adjust buffer length to encourage buffer flusing at several levels.
            JsonSerializerOptions options = new JsonSerializerOptions();
            if (bufferSize != 0)
            {
                options.DefaultBufferSize = bufferSize;
            }

            string json = JsonSerializer.Serialize(list, options);
            Assert.True(json.Length > 100_000); // Verify data is large and will cause buffer flushing.
            Assert.True(json.Length < 200_000); // But not too large for memory considerations.

            // Sync case.
            {
                List<SessionResponse> deserializedList = JsonSerializer.Deserialize<List<SessionResponse>>(json, options);
                Assert.Equal(SessionResponseCount, deserializedList.Count);

                string jsonSerialized = JsonSerializer.Serialize(deserializedList, options);
                Assert.Equal(json, jsonSerialized);
            }

            // Async case.
            using (var memoryStream = new MemoryStream())
            {
                await JsonSerializer.SerializeAsync(memoryStream, list, options);
                string jsonSerialized = Encoding.UTF8.GetString(memoryStream.ToArray());
                Assert.Equal(json, jsonSerialized);

                memoryStream.Position = 0;
                List<SessionResponse> deserializedList = await JsonSerializer.DeserializeAsync<List<SessionResponse>>(memoryStream, options);
                Assert.Equal(SessionResponseCount, deserializedList.Count);
            }
        }
    }

    public sealed class TestStream : Stream
    {
        private readonly MemoryStream _stream;

        public TestStream(int capacity) { _stream = new MemoryStream(capacity); }

        public TestStream(byte[] buffer) { _stream = new MemoryStream(buffer); }

        public int TestFlushCount { get; private set; }

        public int TestWriteCount { get; private set; }
        public int TestWriteBytesCount { get; private set; }
        public int TestReadCount { get; private set; }
        public int TestRequestedReadBytesCount { get; private set; }

        public byte[] ToArray() => _stream.ToArray();

        public override void Flush()
        {
            TestFlushCount++;
            _stream.Flush();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            TestWriteCount++;
            TestWriteBytesCount += (count - offset);
            _stream.Write(buffer, offset, count);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            TestReadCount++;
            TestRequestedReadBytesCount += count;
            return _stream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin) => _stream.Seek(offset, origin);
        public override void SetLength(long value) => _stream.SetLength(value);
        public override bool CanRead => _stream.CanRead;
        public override bool CanSeek => _stream.CanSeek;
        public override bool CanWrite => _stream.CanWrite;
        public override long Length => _stream.Length;
        public override long Position { get => _stream.Position; set => _stream.Position = value; }
    }
}

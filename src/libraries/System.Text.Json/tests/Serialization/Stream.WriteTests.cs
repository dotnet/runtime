// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class StreamTests
    {
        [Fact]
        public async static Task VerifyValueFail()
        {
            MemoryStream stream = new MemoryStream();
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.WriteAsync("", null, stream));
        }

        [Fact]
        public async static Task VerifyTypeFail()
        {
            MemoryStream stream = new MemoryStream();
            await Assert.ThrowsAsync<ArgumentException>(async () => await JsonSerializer.WriteAsync(1, typeof(string), stream));
        }

        [Fact]
        public static async Task NullObjectValue()
        {
            MemoryStream stream = new MemoryStream();
            await JsonSerializer.WriteAsync((object)null, stream);

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

                await JsonSerializer.WriteAsync(obj, stream, options: options);
            }

            // Must be changed if the test classes change:
            Assert.Equal(551_368, stream.TestWriteBytesCount);

            // We should have more than one write called due to the large byte count.
            Assert.True(stream.TestWriteCount > 0);

            // We don't auto-flush.
            Assert.True(stream.TestFlushCount == 0);
        }

        private static async Task ReadAsync(TestStream stream)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                // Will likely default to 4K due to buffer pooling.
                DefaultBufferSize = 1
            };

            LargeDataTestClass obj = await JsonSerializer.ReadAsync<LargeDataTestClass>(stream, options);
            // Must be changed if the test classes change; may be > since last read may not have filled buffer.
            Assert.True(stream.TestRequestedReadBytesCount >= 551368);

            // We should have more than one read called due to the large byte count.
            Assert.True(stream.TestReadCount > 0);

            // We don't auto-flush.
            Assert.True(stream.TestFlushCount == 0);

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

            int i = await JsonSerializer.ReadAsync<int>(stream, options);
            Assert.Equal(1, i);
        }
    }

    public class TestStream : MemoryStream
    {
        public int TestFlushCount { get; private set; }

        public int TestWriteCount { get; private set; }
        public int TestWriteBytesCount { get; private set; }

        public int TestReadCount { get; private set; }
        public int TestRequestedReadBytesCount { get; private set; }

        public TestStream(int capacity) : base(capacity) { }

        public TestStream(byte[] buffer) : base(buffer) { }

        public override Task FlushAsync(CancellationToken cancellationToken)
        {
            TestFlushCount++;
            return base.FlushAsync(cancellationToken);
        }

#if BUILDING_INBOX_LIBRARY
        public override ValueTask WriteAsync(ReadOnlyMemory<byte> source, CancellationToken cancellationToken)
        {
            TestWriteCount++;
            TestWriteBytesCount += source.Length;
            return base.WriteAsync(source, cancellationToken);
        }
#else
        public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            TestWriteCount++;
            TestWriteBytesCount += (count - offset);
            return base.WriteAsync(buffer, offset, count, cancellationToken);
        }
#endif

        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            TestReadCount++;
            TestRequestedReadBytesCount += count;

            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Tests
{
    public partial class BinaryDataTests
    {
        [Fact]
        public void CanCreateBinaryDataFromBytes()
        {
            byte[] payload = "some data"u8.ToArray();
            BinaryData data = BinaryData.FromBytes(payload);
            Assert.Equal(payload, data.ToArray());

            MemoryMarshal.TryGetArray<byte>(payload, out ArraySegment<byte> array);
            Assert.Same(payload, array.Array);

            // using implicit conversion
            ReadOnlyMemory<byte> bytes = data;
            Assert.Equal(payload, bytes.ToArray());

            // using implicit conversion
            ReadOnlySpan<byte> span = data;
            Assert.Equal(payload, span.ToArray());

            // using implicit conversion from null
            BinaryData nullData = null;
            ReadOnlyMemory<byte> emptyBytes = nullData;
            Assert.True(emptyBytes.IsEmpty);

            // using implicit conversion from null
            ReadOnlySpan<byte> emptySpan = nullData;
            Assert.True(emptySpan.IsEmpty);
        }

        [Fact]
        public void CanCreateBinaryDataFromString()
        {
            string payload = "some data";
            BinaryData data = new BinaryData(payload);
            Assert.Equal(payload, data.ToString());

            data = BinaryData.FromString(payload);
            Assert.Equal(payload, data.ToString());
        }

        [Fact]
        public void ToStringRespectsArraySegmentBoundaries()
        {
            string payload = "pre payload post";
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            ArraySegment<byte> segment = new ArraySegment<byte>(bytes, 4, 7);
            BinaryData data = BinaryData.FromBytes(segment);
            Assert.Equal("payload", data.ToString());

            data = BinaryData.FromBytes(segment.Array);
            Assert.Equal("pre payload post", data.ToString());
        }

        [Fact]
        public async Task ToStreamRespectsArraySegmentBoundaries()
        {
            string payload = "pre payload post";
            byte[] bytes = Encoding.UTF8.GetBytes(payload);
            ArraySegment<byte> segment = new ArraySegment<byte>(bytes, 4, 7);
            BinaryData data = BinaryData.FromBytes(segment);
            Stream stream = data.ToStream();
            StreamReader sr = new StreamReader(stream);
            Assert.Equal("payload", await sr.ReadToEndAsync());
        }

        [Fact]
        public async Task CannotWriteToReadOnlyMemoryStream()
        {
            byte[] buffer = "some data"u8.ToArray();
            using MemoryStream payload = new MemoryStream(buffer);
            BinaryData data = BinaryData.FromStream(payload);
            Stream stream = data.ToStream();
            Assert.Throws<NotSupportedException>(() => stream.Write(buffer, 0, buffer.Length));
            await Assert.ThrowsAsync<NotSupportedException>(() => stream.WriteAsync(buffer, 0, buffer.Length));
            Assert.Throws<NotSupportedException>(() => stream.WriteByte(1));
            Assert.False(stream.CanWrite);
            StreamReader sr = new StreamReader(stream);
            Assert.Equal("some data", await sr.ReadToEndAsync());
        }

        [Fact]
        public async Task ToStreamIsMutatedWhenCustomerOwnsBuffer()
        {
            byte[] buffer = "some data"u8.ToArray();
            BinaryData data = BinaryData.FromBytes(buffer);
            Stream stream = data.ToStream();
            buffer[0] = (byte)'z';
            StreamReader sr = new StreamReader(stream);
            Assert.Equal("zome data", await sr.ReadToEndAsync());
        }

        [Fact]
        public async Task ToStreamIsNotMutatedWhenBinaryDataOwnsBuffer()
        {
            byte[] buffer = "some data"u8.ToArray();
            BinaryData data = BinaryData.FromStream(new MemoryStream(buffer));
            Stream stream = data.ToStream();
            buffer[0] = (byte)'z';
            StreamReader sr = new StreamReader(stream);
            Assert.Equal("some data", await sr.ReadToEndAsync());
        }

        [Fact]
        public async Task CanCreateBinaryDataFromStream()
        {
            byte[] buffer = "some data"u8.ToArray();
            using MemoryStream stream = new MemoryStream(buffer, 0, buffer.Length, true, true);
            BinaryData data = BinaryData.FromStream(stream);
            Assert.Equal(buffer, data.ToArray());

            byte[] output = new byte[buffer.Length];
            var outputStream = data.ToStream();
            outputStream.Read(output, 0, (int) outputStream.Length);
            Assert.Equal(buffer, output);

            stream.Position = 0;
            data = await BinaryData.FromStreamAsync(stream);
            Assert.Equal(buffer, data.ToArray());

            outputStream = data.ToStream();
            outputStream.Read(output, 0, (int)outputStream.Length);
            Assert.Equal(buffer, output);

            //changing the backing buffer should not affect the BD instance
            buffer[3] = (byte)'z';
            Assert.NotEqual(buffer, data.ToMemory().ToArray());
        }

        [Fact]
        public async Task CanCreateBinaryDataFromLongStream()
        {
            byte[] buffer = "some data"u8.ToArray();
            using MemoryStream stream = new OverFlowStream(offset: int.MaxValue - 10000, buffer);
            BinaryData data = BinaryData.FromStream(stream);
            Assert.Equal(buffer, data.ToArray());

            byte[] output = new byte[buffer.Length];
            var outputStream = data.ToStream();
            outputStream.Read(output, 0, (int)outputStream.Length);
            Assert.Equal(buffer, output);

            stream.Position = 0;
            data = await BinaryData.FromStreamAsync(stream);
            Assert.Equal(buffer, data.ToArray());

            outputStream = data.ToStream();
            outputStream.Read(output, 0, (int)outputStream.Length);
            Assert.Equal(buffer, output);
        }

        [Fact]
        public async Task CanCreateBinaryDataFromEmptyStream()
        {
            //completely empty stream
            using MemoryStream stream = new MemoryStream();
            BinaryData data = BinaryData.FromStream(stream);
            Assert.Empty(data.ToArray());

            data = await BinaryData.FromStreamAsync(stream);
            Assert.Empty(data.ToArray());

            // stream at end
            byte[] buffer = "some data"u8.ToArray();
            stream.Write(buffer, 0, buffer.Length);
            data = BinaryData.FromStream(stream);
            Assert.Empty(data.ToArray());

            data = await BinaryData.FromStreamAsync(stream);
            Assert.Empty(data.ToArray());
        }

        [Fact]
        public async Task CanCreateBinaryDataFromStreamUsingBackingBuffer()
        {
            byte[] buffer = "some data"u8.ToArray();
            using MemoryStream stream = new MemoryStream();
            stream.Write(buffer, 0, buffer.Length);
            stream.Position = 0;
            BinaryData data = BinaryData.FromStream(stream);
            Assert.Equal(buffer, data.ToMemory().ToArray());

            byte[] output = new byte[buffer.Length];
            var outputStream = data.ToStream();
            outputStream.Read(output, 0, (int)outputStream.Length);
            Assert.Equal(buffer, output);

            stream.Position = 0;
            data = await BinaryData.FromStreamAsync(stream);
            Assert.Equal(buffer, data.ToMemory().ToArray());

            outputStream = data.ToStream();
            outputStream.Read(output, 0, (int)outputStream.Length);
            Assert.Equal(buffer, output);
        }

        [Fact]
        public async Task CanCreateBinaryDataFromNonSeekableStream()
        {
            byte[] buffer = "some data"u8.ToArray();
            using MemoryStream stream = new NonSeekableStream(buffer);
            BinaryData data = BinaryData.FromStream(stream);
            Assert.Equal(buffer, data.ToArray());

            byte[] output = new byte[buffer.Length];
            var outputStream = data.ToStream();
            outputStream.Read(output, 0, (int)outputStream.Length);
            Assert.Equal(buffer, output);

            stream.Position = 0;
            data = await BinaryData.FromStreamAsync(stream);
            Assert.Equal(buffer, data.ToArray());

            outputStream = data.ToStream();
            outputStream.Read(output, 0, (int)outputStream.Length);
            Assert.Equal(buffer, output);
        }

        [Fact]
        public async Task CanCreateBinaryDataFromFileStream()
        {
            byte[] buffer = "some data"u8.ToArray();
            using FileStream stream = new FileStream(Path.GetTempFileName(), FileMode.Open);
            stream.Write(buffer, 0, buffer.Length);
            stream.Position = 0;
            BinaryData data = BinaryData.FromStream(stream);
            Assert.Equal(buffer, data.ToArray());

            byte[] output = new byte[buffer.Length];
            var outputStream = data.ToStream();
            outputStream.Read(output, 0, (int)outputStream.Length);
            Assert.Equal(buffer, output);

            stream.Position = 0;
            data = await BinaryData.FromStreamAsync(stream);
            Assert.Equal(buffer, data.ToArray());

            outputStream = data.ToStream();
            outputStream.Read(output, 0, (int)outputStream.Length);
            Assert.Equal(buffer, output);
        }

        [Theory]
        [InlineData(1, 4)]
        [InlineData(0, 4)]
        [InlineData(4, 1)]
        [InlineData(4, 0)]
        public async Task StartPositionOfStreamRespected(int bufferOffset, long streamStart)
        {
            var input = "some data";
            ArraySegment<byte> buffer = new ArraySegment<byte>("some data"u8.ToArray(), bufferOffset, input.Length - bufferOffset);
            MemoryStream stream = new MemoryStream(buffer.Array, buffer.Offset, buffer.Count);
            var payload = new ReadOnlyMemory<byte>(buffer.Array, buffer.Offset, buffer.Count).Slice((int)streamStart);

            stream.Position = streamStart;
            BinaryData data = BinaryData.FromStream(stream);
            Assert.Equal(payload.ToArray(), data.ToMemory().ToArray());
            Assert.Equal(buffer.Count - streamStart, data.ToStream().Length);

            stream.Position = streamStart;
            data = await BinaryData.FromStreamAsync(stream);
            Assert.Equal(payload.ToArray(), data.ToMemory().ToArray());
            Assert.Equal(buffer.Count - streamStart, data.ToStream().Length);
        }

        [Theory]
        [InlineData(1, 4)]
        [InlineData(0, 4)]
        [InlineData(4, 1)]
        [InlineData(4, 0)]
        public async Task StartPositionOfStreamRespectedBackingBuffer(int bufferOffset, long streamStart)
        {
            var input = "some data";
            ArraySegment<byte> buffer = new ArraySegment<byte>("some data"u8.ToArray(), bufferOffset, input.Length - bufferOffset);
            MemoryStream stream = new MemoryStream();
            stream.Write(buffer.Array, buffer.Offset, buffer.Count);

            var payload = new ReadOnlyMemory<byte>(buffer.Array, buffer.Offset, buffer.Count).Slice((int)streamStart);

            stream.Position = streamStart;
            BinaryData data = BinaryData.FromStream(stream);
            Assert.Equal(payload.ToArray(), data.ToArray());
            Assert.Equal(buffer.Count - streamStart, data.ToStream().Length);

            stream.Position = streamStart;
            data = await BinaryData.FromStreamAsync(stream);
            Assert.Equal(payload.ToArray(), data.ToArray());
            Assert.Equal(buffer.Count - streamStart, data.ToStream().Length);
        }

        [Fact]
        public void MaxStreamLengthRespected()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryData.FromStream(new OverFlowStream(offset: 0)));
            Assert.Throws<ArgumentOutOfRangeException>(() => BinaryData.FromStream(new OverFlowStream(offset: int.MaxValue + 2L)));

            // should not throw

            var data = BinaryData.FromStream(new OverFlowStream(offset: int.MaxValue - 1000));
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        public void CanCreateBinaryDataFromCustomType()
        {
            TestModel payload = new TestModel { A = "value", B = 5, C = true, D = null };
            JsonSerializerOptions options = new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull };

            AssertData(BinaryData.FromObjectAsJson(payload));
            AssertData(BinaryData.FromObjectAsJson(payload, options));
            AssertData(BinaryData.FromObjectAsJson(payload, TestModelJsonContext.Default.TestModel));
            AssertData(new BinaryData(payload, type: typeof(TestModel)));
            AssertData(new BinaryData(payload));
            AssertData(new BinaryData(payload, type: null));
            AssertData(new BinaryData(payload, options: null, typeof(TestModel)));
            AssertData(new BinaryData(payload, options, typeof(TestModel)));
            AssertData(new BinaryData(payload, context: TestModelJsonContext.Default, type: typeof(TestModel)));

            void AssertData(BinaryData data)
            {
                TestModel model = data.ToObjectFromJson<TestModel>();
                Assert.Equal(payload.A, model.A);
                Assert.Equal(payload.B, model.B);
                Assert.Equal(payload.C, model.C);
                Assert.Equal(payload.D, model.D);
            }
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        public void CanSerializeNullData()
        {
            BinaryData data = new BinaryData(jsonSerializable: null);
            Assert.Null(data.ToObjectFromJson<object>());
            data = BinaryData.FromObjectAsJson<object>(null);
            Assert.Null(data.ToObjectFromJson<object>());

            data = new BinaryData(jsonSerializable: null, type: typeof(TestModel));
            Assert.Null(data.ToObjectFromJson<TestModel>());

            data = new BinaryData(jsonSerializable: null);
            Assert.Null(data.ToObjectFromJson<TestModel>());

            data = new BinaryData(jsonSerializable: null, type: null);
            Assert.Null(data.ToObjectFromJson<TestModel>());

            data = BinaryData.FromObjectAsJson<TestModel>(null);
            Assert.Null(data.ToObjectFromJson<TestModel>());

            data = BinaryData.FromObjectAsJson<TestModel>(null, TestModelJsonContext.Default.TestModel as JsonTypeInfo<TestModel>);
            Assert.Null(data.ToObjectFromJson<TestModel>(TestModelJsonContext.Default.TestModel));
        }

        [Fact]
        public async Task CreateThrowsOnNullStream()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => BinaryData.FromStream(null));
            Assert.Contains("stream", ex.Message);

            ex = await Assert.ThrowsAsync<ArgumentNullException>(() => BinaryData.FromStreamAsync(null));
            Assert.Contains("stream", ex.Message);

        }

        [Fact]
        public void CreateThrowsOnNullString()
        {
            string payload = null;
            var ex = Assert.Throws<ArgumentNullException>(() => new BinaryData(payload));
            Assert.Contains("data", ex.Message);

            ex = Assert.Throws<ArgumentNullException>(() => BinaryData.FromString(payload));
            Assert.Contains("data", ex.Message);
        }

        [Fact]
        public void CreateThrowsOnNullArray()
        {
            byte[] payload = null;
            var ex = Assert.Throws<ArgumentNullException>(() => new BinaryData(payload));
            Assert.Contains("data", ex.Message);

            ex = Assert.Throws<ArgumentNullException>(() => BinaryData.FromBytes(null));
            Assert.Contains("data", ex.Message);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        public void ToObjectHandlesBOM()
        {
            TestModel payload = new TestModel { A = "string", B = 42, C = true };
            using var buffer = new MemoryStream();
            using var writer = new StreamWriter(buffer, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            writer.Write(JsonSerializer.Serialize(payload));
            writer.Flush();

            BinaryData data = new BinaryData(buffer.ToArray());
            var model = data.ToObjectFromJson<TestModel>();
            Assert.Equal(payload.A, model.A);
            Assert.Equal(payload.B, model.B);
            Assert.Equal(payload.C, model.C);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBuiltWithAggressiveTrimming))]
        public void ToObjectThrowsExceptionOnIncompatibleType()
        {
            TestModel payload = new TestModel { A = "value", B = 5, C = true };
            BinaryData data = BinaryData.FromObjectAsJson(payload);
            Assert.ThrowsAny<Exception>(() => data.ToObjectFromJson<string>());
            Assert.ThrowsAny<Exception>(() => data.ToObjectFromJson<MismatchedTestModel>(jsonTypeInfo: MismatchedTestModelJsonContext.Default.MismatchedTestModel));
        }

        [Fact]
        public void EqualsRespectsReferenceEquality()
        {
            byte[] payload = "some data"u8.ToArray();
            BinaryData a = BinaryData.FromBytes(payload);
            BinaryData b = BinaryData.FromBytes(payload);
            Assert.NotEqual(a, b);

            BinaryData c = BinaryData.FromBytes("some data"u8.ToArray());
            Assert.NotEqual(a, c);

            Assert.False(a.Equals("string data"));
        }

        [Fact]
        public void GetHashCodeWorks()
        {
            byte[] payload = "some data"u8.ToArray();
            BinaryData a = BinaryData.FromBytes(payload);
            BinaryData b = BinaryData.FromBytes(payload);
            HashSet<BinaryData> set = new HashSet<BinaryData>
            {
                a
            };
            // hashcodes of a and b should not match since instances are different.
            Assert.DoesNotContain(b, set);

            BinaryData c = BinaryData.FromBytes("some data"u8.ToArray());
            // c should have a different hash code
            Assert.DoesNotContain(c, set);
            set.Add(c);
            Assert.Contains(c, set);
        }

        [Fact]
        public async Task CanRead()
        {
            byte[] buffer = "some data"u8.ToArray();
            var stream = new BinaryData(buffer).ToStream();

            var read = new byte[buffer.Length];
            stream.Read(read, 0, buffer.Length);
            Assert.Equal(buffer, read);

            read = new byte[buffer.Length];
            stream.Position = 0;
            await stream.ReadAsync(read, 0, buffer.Length);
            Assert.Equal(buffer, read);

            // no-op as we are at end of stream
            stream.Read(read, 0, buffer.Length);
            await stream.ReadAsync(read, 0, buffer.Length);
        }

        [Fact]
        public async Task CanReadPartial()
        {
            byte[] buffer = "some data"u8.ToArray();
            var stream = new BinaryData(buffer).ToStream();
            var length = 4;
            var read = new byte[length];
            stream.Read(read, 0, length);
            Assert.Equal(buffer.AsMemory(0, length).ToArray(), read.AsMemory(0, length).ToArray());

            read = new byte[length];
            stream.Position = 0;
            await stream.ReadAsync(read, 0, length);
            Assert.Equal(buffer.AsMemory(0, length).ToArray(), read.AsMemory(0, length).ToArray());

            // no-op as we are at end of stream
            stream.Read(read, 0, length);
            await stream.ReadAsync(read, 0, length);
            Assert.Equal(-1, stream.ReadByte());
        }

        [Fact]
        public void ReadAsyncRespectsCancellation()
        {
            byte[] buffer = "some data"u8.ToArray();
            var stream = new BinaryData(buffer).ToStream();

            var read = new byte[buffer.Length];
            var cts = new CancellationTokenSource();
            cts.Cancel();

            var task = stream.ReadAsync(read, 0, buffer.Length, cts.Token);
            Assert.True(task.IsCanceled);

            cts = new CancellationTokenSource();
            task = stream.ReadAsync(read, 0, buffer.Length, cts.Token);
            Assert.False(task.IsCanceled);
            Assert.True(task.IsCompleted);
        }

        [Fact]
        public async Task CanSeek()
        {
            byte[] buffer = "some data"u8.ToArray();
            var stream = new BinaryData(buffer).ToStream();

            stream.Seek(5, SeekOrigin.Begin);
            Assert.Equal(buffer[5], stream.ReadByte());
            stream.Seek(1, SeekOrigin.Current);
            Assert.Equal(buffer[7], stream.ReadByte());
            stream.Seek(-2, SeekOrigin.End);
            Assert.Equal(buffer.Length - 2, stream.Position);
            Assert.Equal(buffer[buffer.Length - 2], stream.ReadByte());
            stream.Seek(-2, SeekOrigin.End);
            var read = new byte[buffer.Length - stream.Position];
            await stream.ReadAsync(read, 0, read.Length);
            Assert.Equal(
                new ReadOnlyMemory<byte>(buffer, buffer.Length - 2, 2).ToArray(),
                read);
        }

        [Fact]
        public void ValidatesSeekArguments()
        {
            byte[] buffer = "some data"u8.ToArray();
            var stream = new BinaryData(buffer).ToStream();

            Assert.Throws<IOException>(() => stream.Seek(-1, SeekOrigin.Begin));

            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Seek((long)int.MaxValue + 1, SeekOrigin.Begin));
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Seek(0, (SeekOrigin)3));
        }


        [Fact]
        public async Task ValidatesReadArguments()
        {
            byte[] buffer = "some data"u8.ToArray();
            var stream = new BinaryData(buffer).ToStream();
            stream.Seek(3, SeekOrigin.Begin);
            var read = new byte[buffer.Length - stream.Position];
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => stream.ReadAsync(read, 0, buffer.Length));
            await Assert.ThrowsAsync<ArgumentNullException>(() => stream.ReadAsync(null, 0, buffer.Length));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => stream.ReadAsync(read, -1, read.Length));
            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => stream.ReadAsync(read, 0, -1));
            await stream.ReadAsync(read, 0, read.Length);
            Assert.Equal(
                new ReadOnlyMemory<byte>(buffer, 3, buffer.Length - 3).ToArray(),
                read);
        }

        [Fact]
        public void ValidatesPositionValue()
        {
            byte[] buffer = "some data"u8.ToArray();
            var stream = new BinaryData(buffer).ToStream();
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Position = -1);
            Assert.Throws<ArgumentOutOfRangeException>(() => stream.Position = (long)int.MaxValue + 1);
        }

        [Fact]
        public void CloseStreamValidation()
        {
            byte[] buffer = "some data"u8.ToArray();
            Stream stream = new BinaryData(buffer).ToStream();
            stream.Dispose();
            Assert.Throws<ObjectDisposedException>(() => stream.Position = -1);
            Assert.Throws<ObjectDisposedException>(() => stream.Position);
            Assert.Throws<ObjectDisposedException>(() => stream.Seek(0, SeekOrigin.Begin));
            Assert.Throws<ObjectDisposedException>(() => stream.Read(buffer, 0, buffer.Length));
            Assert.ThrowsAsync<ObjectDisposedException>(() => stream.ReadAsync(buffer, 0, buffer.Length));
            Assert.Throws<ObjectDisposedException>(() => stream.ReadByte());
            Assert.Throws<ObjectDisposedException>(() => stream.Length);
            Assert.False(stream.CanRead);
            Assert.False(stream.CanSeek);

        }

        [Fact]
        public void EmptyIsEmpty()
        {
            Assert.Equal(Array.Empty<byte>(), BinaryData.Empty.ToArray());
        }

        [Fact]
        public void EmptyIsSingleton()
        {
            Assert.Same(BinaryData.Empty, BinaryData.Empty);
        }

        [Fact]
        public void ToStringReturnEmptyStringWhenBinaryDataEmpty()
        {
            Assert.Equal(string.Empty, BinaryData.Empty.ToString());
        }

        internal class TestModel
        {
            public string A { get; set; }
            public int B { get; set; }
            public bool C { get; set; }
            public object D { get; set; }
        }

        internal class MismatchedTestModel 
        {
            public int A { get; set; }
        }

        [JsonSerializable(typeof(TestModel))]
        internal partial class TestModelJsonContext : JsonSerializerContext
        {
        }

        [JsonSerializable(typeof(MismatchedTestModel))]
        internal partial class MismatchedTestModelJsonContext: JsonSerializerContext 
        {
        }

        private class OverFlowStream : MemoryStream
        {
            private readonly long _offset;

            public OverFlowStream(long offset)
            {
                _offset = offset;
            }

            public OverFlowStream(long offset, byte[] buffer) : base(buffer)
            {
                _offset = offset;
            }

            public override long Length => (long)int.MaxValue + 1;

            public override long Position => _offset;
        }

        private class NonSeekableStream : MemoryStream
        {
            public NonSeekableStream(byte[] buffer) : base(buffer) { }
            public override bool CanSeek => false;
        }
    }
}

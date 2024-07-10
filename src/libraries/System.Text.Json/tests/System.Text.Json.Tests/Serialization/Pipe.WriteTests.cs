// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public partial class PipeTests
    {
        [Fact]
        public async Task WriteNullArgumentFail()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.SerializeAsync((PipeWriter)null, 1));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.SerializeAsync((PipeWriter)null, 1, typeof(int)));
        }

        [Fact]
        public async Task VerifyValueFail()
        {
            Pipe pipe = new Pipe();
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.SerializeAsync(pipe.Writer, "", (Type)null));
        }

        [Fact]
        public async Task VerifyTypeFail()
        {
            Pipe pipe = new Pipe();
            await Assert.ThrowsAsync<ArgumentException>(async () => await JsonSerializer.SerializeAsync(pipe.Writer, 1, typeof(string)));
        }

        [Fact]
        public async Task CompletedPipeWithExceptionThrowsFromSerialize()
        {
            Pipe pipe = new Pipe();
            pipe.Reader.Complete(new FormatException());

            await Assert.ThrowsAsync<FormatException>(() => JsonSerializer.SerializeAsync(pipe.Writer, 1));
        }

        [Fact]
        public async Task FlushesPeriodicallyWhenWritingLargeJson()
        {
            Pipe pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 1000000));
            IEnumerable<int> obj = Enumerable.Range(0, PipeOptions.Default.MinimumSegmentSize * 2);
            CustomPipeWriter writer = new CustomPipeWriter(pipe.Writer);
            await JsonSerializer.SerializeAsync(writer, obj);

            Assert.Equal(3, writer.Flushes.Count);
            foreach (long flush in writer.Flushes)
            {
                // Fragile check, but since we're writing integers that are 5 or less digits
                // it should always exit to a flush while under the flush threshold
                Assert.True(flush < PipeOptions.Default.MinimumSegmentSize * 4);
            }
        }

        class CustomPipeWriter : PipeWriter
        {
            private readonly PipeWriter _originalWriter;

            public CustomPipeWriter(PipeWriter originalWriter)
            {
                _originalWriter = originalWriter;
            }

            public List<long> Flushes { get; } = new List<long>();

            public override void Advance(int bytes) => _originalWriter.Advance(bytes);
            public override void CancelPendingFlush() => _originalWriter.CancelPendingFlush();
            public override void Complete(Exception? exception = null) => _originalWriter.Complete(exception);
            public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
            {
                Flushes.Add(UnflushedBytes);
                return _originalWriter.FlushAsync(cancellationToken);
            }
            public override Memory<byte> GetMemory(int sizeHint = 0) => _originalWriter.GetMemory(sizeHint);
            public override Span<byte> GetSpan(int sizeHint = 0) => _originalWriter.GetSpan(sizeHint);
            public override bool CanGetUnflushedBytes => _originalWriter.CanGetUnflushedBytes;
            public override long UnflushedBytes => _originalWriter.UnflushedBytes;
        }

        [Fact]
        public async Task CancelPendingFlushDuringBackpressureThrows()
        {
            int i = 0;
            Pipe pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 10, resumeWriterThreshold: 5));
            await pipe.Writer.WriteAsync("123456789"u8.ToArray());
            Task serializeTask = JsonSerializer.SerializeAsync(pipe.Writer, GetNumbersAsync());
            Assert.False(serializeTask.IsCompleted);

            pipe.Writer.CancelPendingFlush();

            await Assert.ThrowsAsync<OperationCanceledException>(() => serializeTask);

            ReadResult result = await pipe.Reader.ReadAsync();

            // Technically this check is not needed, but helps confirm behavior, that Pipe had written but was waiting for flush to continue.
            // result.Buffer: 123456789[0...
            Assert.InRange(result.Buffer.Length, 10 + i - 1, 10 + i + 1);
            pipe.Reader.AdvanceTo(result.Buffer.End);

            async IAsyncEnumerable<int> GetNumbersAsync()
            {
                while (true)
                {
                    await Task.Delay(10);
                    yield return i++;
                }
            }
        }

        [Fact]
        public async Task BackpressureIsObservedWhenWritingJson()
        {
            Pipe pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 10, resumeWriterThreshold: 5));
            await pipe.Writer.WriteAsync("123456789"u8.ToArray());
            Task serializeTask = JsonSerializer.SerializeAsync(pipe.Writer, 1);
            Assert.False(serializeTask.IsCompleted);

            ReadResult result = await pipe.Reader.ReadAsync();
            pipe.Reader.AdvanceTo(result.Buffer.GetPosition(5));

            // Still need to read 1 more byte to unblock flush
            Assert.False(serializeTask.IsCompleted);

            result = await pipe.Reader.ReadAsync();
            pipe.Reader.AdvanceTo(result.Buffer.GetPosition(1));

            await serializeTask;
        }

        [Fact]
        public async Task CanCancelBackpressuredJsonWrite()
        {
            Pipe pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 10, resumeWriterThreshold: 5));
            await pipe.Writer.WriteAsync("123456789"u8.ToArray());

            CancellationTokenSource cts = new();
            Task serializeTask = JsonSerializer.SerializeAsync(pipe.Writer, 1, cancellationToken: cts.Token);
            Assert.False(serializeTask.IsCompleted);

            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(() => serializeTask);

            ReadResult result = await pipe.Reader.ReadAsync();
            // Even though flush was canceled, the bytes are still written to the Pipe
            Assert.Equal(10, result.Buffer.Length);
            pipe.Reader.AdvanceTo(result.Buffer.End);
        }

        [Theory]
        [InlineData(32)]
        [InlineData(128)]
        [InlineData(1024)]
        [InlineData(1024 * 16)] // the default JsonSerializerOptions.DefaultBufferSize value
        [InlineData(1024 * 1024)]
        public async Task ShouldUseFastPathOnSmallPayloads(int defaultBufferSize)
        {
            var instrumentedResolver = new PocoWithInstrumentedFastPath.Context(
                    new JsonSerializerOptions
                    {
                        DefaultBufferSize = defaultBufferSize,
                    });

            // The current implementation uses a heuristic
            int smallValueThreshold = defaultBufferSize / 2;
            PocoWithInstrumentedFastPath smallValue = PocoWithInstrumentedFastPath.CreateValueWithSerializationSize(smallValueThreshold);

            // We don't care about backpressure in this test
            Pipe pipe = new Pipe(new PipeOptions(pauseWriterThreshold: defaultBufferSize, resumeWriterThreshold: defaultBufferSize / 2));
            ReadResult result;

            // The first 10 serializations should not call into the fast path
            for (int i = 0; i < 10; i++)
            {
                await JsonSerializer.SerializeAsync(pipe.Writer, smallValue, instrumentedResolver.Options);
                result = await pipe.Reader.ReadAsync();
                pipe.Reader.AdvanceTo(result.Buffer.End);
                Assert.Equal(0, instrumentedResolver.FastPathInvocationCount);
            }

            // Subsequent iterations do call into the fast path
            for (int i = 0; i < 10; i++)
            {
                await JsonSerializer.SerializeAsync(pipe.Writer, smallValue, instrumentedResolver.Options);
                result = await pipe.Reader.ReadAsync();
                pipe.Reader.AdvanceTo(result.Buffer.End);
                Assert.Equal(i + 1, instrumentedResolver.FastPathInvocationCount);
            }

            // Polymorphic serialization should use the fast path
            await JsonSerializer.SerializeAsync(pipe.Writer, (object)smallValue, instrumentedResolver.Options);
            result = await pipe.Reader.ReadAsync();
            pipe.Reader.AdvanceTo(result.Buffer.End);
            Assert.Equal(11, instrumentedResolver.FastPathInvocationCount);

            // Attempt to serialize a value that is deemed large
            var largeValue = PocoWithInstrumentedFastPath.CreateValueWithSerializationSize(smallValueThreshold + 1);
            await JsonSerializer.SerializeAsync(pipe.Writer, largeValue, instrumentedResolver.Options);
            result = await pipe.Reader.ReadAsync();
            pipe.Reader.AdvanceTo(result.Buffer.End);
            Assert.Equal(12, instrumentedResolver.FastPathInvocationCount);

            // Any subsequent attempts no longer call into the fast path
            for (int i = 0; i < 10; i++)
            {
                await JsonSerializer.SerializeAsync(pipe.Writer, smallValue, instrumentedResolver.Options);
                result = await pipe.Reader.ReadAsync();
                pipe.Reader.AdvanceTo(result.Buffer.End);
                Assert.Equal(12, instrumentedResolver.FastPathInvocationCount);
            }
        }

        [Fact]
        public async Task FastPathObservesBackpressure()
        {
            int defaultBufferSize = 4096;
            var instrumentedResolver = new PocoWithInstrumentedFastPath.Context(
                    new JsonSerializerOptions
                    {
                        DefaultBufferSize = defaultBufferSize,
                    });

            // The current implementation uses a heuristic
            int smallValueThreshold = defaultBufferSize / 2;
            PocoWithInstrumentedFastPath smallValue = PocoWithInstrumentedFastPath.CreateValueWithSerializationSize(smallValueThreshold);

            Pipe pipe = new Pipe(new PipeOptions(pauseWriterThreshold: defaultBufferSize / 2, resumeWriterThreshold: defaultBufferSize / 4));
            ReadResult result;

            // The first 10 serializations should not call into the fast path
            for (int i = 0; i < 10; i++)
            {
                await JsonSerializer.SerializeAsync(pipe.Writer, smallValue, instrumentedResolver.Options);
                result = await pipe.Reader.ReadAsync();
                pipe.Reader.AdvanceTo(result.Buffer.End);
                Assert.Equal(0, instrumentedResolver.FastPathInvocationCount);
            }

            Task serializeTask = JsonSerializer.SerializeAsync(pipe.Writer, smallValue, instrumentedResolver.Options);
            Assert.False(serializeTask.IsCompleted);
            Assert.Equal(1, instrumentedResolver.FastPathInvocationCount);

            result = await pipe.Reader.ReadAsync();
            pipe.Reader.AdvanceTo(result.Buffer.End);
            await serializeTask;
        }

        [Fact]
        public async Task CanCancelBackpressuredFastPath()
        {
            int defaultBufferSize = 4096;
            var instrumentedResolver = new PocoWithInstrumentedFastPath.Context(
                    new JsonSerializerOptions
                    {
                        DefaultBufferSize = defaultBufferSize,
                    });

            // The current implementation uses a heuristic
            int smallValueThreshold = defaultBufferSize / 2;
            PocoWithInstrumentedFastPath smallValue = PocoWithInstrumentedFastPath.CreateValueWithSerializationSize(smallValueThreshold);

            Pipe pipe = new Pipe(new PipeOptions(pauseWriterThreshold: defaultBufferSize / 2, resumeWriterThreshold: defaultBufferSize / 4));
            ReadResult result;

            // The first 10 serializations should not call into the fast path
            for (int i = 0; i < 10; i++)
            {
                await JsonSerializer.SerializeAsync(pipe.Writer, smallValue, instrumentedResolver.Options);
                result = await pipe.Reader.ReadAsync();
                pipe.Reader.AdvanceTo(result.Buffer.End);
                Assert.Equal(0, instrumentedResolver.FastPathInvocationCount);
            }

            CancellationTokenSource cts = new();

            Task serializeTask = JsonSerializer.SerializeAsync(pipe.Writer, smallValue, instrumentedResolver.Options, cts.Token);
            Assert.False(serializeTask.IsCompleted);

            cts.Cancel();
            await Assert.ThrowsAsync<OperationCanceledException>(() => serializeTask);
        }

        [Fact]
        public async Task BuffersBehaveAsExpected()
        {
            TestPool pool = new TestPool(2000);
            Pipe pipe = new Pipe(new PipeOptions(pool));

            // Many small writes
            for (int i = 0; i < 100; ++i)
            {
                await JsonSerializer.SerializeAsync(pipe.Writer, "a");
            }
            // Should fit into a single 2000 byte buffer
            Assert.Equal(1, pool.BufferCount);
            ReadResult result = await pipe.Reader.ReadAsync();
            pipe.Reader.AdvanceTo(result.Buffer.End);
            Assert.Equal(0, pool.BufferCount);

            // Partially fill Pipe so next write needs a new buffer
            await JsonSerializer.SerializeAsync(pipe.Writer, new string('a', 600));
            Assert.Equal(1, pool.BufferCount);

            // Writing strings incurs a 3x buffer size due to max potential transcoding
            // 600 + 600*3 > 2000 means a second buffer will be grabbed
            await JsonSerializer.SerializeAsync(pipe.Writer, new string('a', 600));
            Assert.Equal(2, pool.BufferCount);
            result = await pipe.Reader.ReadAsync();
            Assert.Equal(1204, result.Buffer.Length);
            SequencePosition pos = result.Buffer.Start;
            int segments = 0;
            while (result.Buffer.TryGet(ref pos, out ReadOnlyMemory<byte> memory))
            {
                segments++;
                Assert.Equal(602, memory.Length);
            }
            Assert.Equal(2, segments);
            pipe.Reader.AdvanceTo(result.Buffer.End);

            // Large write
            await JsonSerializer.SerializeAsync(pipe.Writer, new string('a', 2000));
            // Write is larger than pools max buffer size so Pipes will provide a buffer from elsewhere.
            Assert.Equal(0, pool.BufferCount);
            result = await pipe.Reader.ReadAsync();
            Assert.Equal(2002, result.Buffer.Length);
        }

        [Fact]
        public async Task ThrowForPipeWriterWithoutUnflushedBytesImplemented()
        {
            var pipeWriter = new BadPipeWriter();
            var exception = await Assert.ThrowsAnyAsync<InvalidOperationException>(() => JsonSerializer.SerializeAsync(pipeWriter, 0));
            Assert.Equal("The PipeWriter 'BadPipeWriter' does not implement PipeWriter.UnflushedBytes.", exception.Message);
        }

        class BadPipeWriter : PipeWriter
        {
            public override void Advance(int bytes) => throw new NotImplementedException();
            public override void CancelPendingFlush() => throw new NotImplementedException();
            public override void Complete(Exception? exception = null) => throw new NotImplementedException();
            public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default) => throw new NotImplementedException();
            public override Memory<byte> GetMemory(int sizeHint = 0) => throw new NotImplementedException();
            public override Span<byte> GetSpan(int sizeHint = 0) => throw new NotImplementedException();
            public override bool CanGetUnflushedBytes => false;
            public override long UnflushedBytes => throw new NotImplementedException();
        }

        [Fact]
        public async Task NestedSerializeAsyncCallsFlushAtThreshold()
        {
            string data = new string('a', 300);
            var options = new JsonSerializerOptions();
            options.Converters.Add(new MyStringConverter());

            var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 10000000));
            var writer = new CustomPipeWriter(pipe.Writer);
            await JsonSerializer.SerializeAsync(writer, CreateManyTestObjects(), options);

            // Flush should happen every ~14,745 bytes (+36 for writing data when just below threshold)
            Assert.True(writer.Flushes.Count > (data.Length * 10_000 / 16_000), $"Flush count: {writer.Flushes.Count}");

            foreach (long flush in writer.Flushes)
            {
                Assert.True(flush < PipeOptions.Default.MinimumSegmentSize * 4);
            }

            IEnumerable<string> CreateManyTestObjects()
            {
                int i = 0;
                while (true)
                {
                    if (++i % 10_000 == 0)
                    {
                        break;
                    }
                    yield return data;
                }
            }
        }

        [Fact]
        public async Task SerializeAsyncCallsFlushAtThreshold()
        {
            string data = new string('a', 300);
            var options = new JsonSerializerOptions();

            var pipe = new Pipe(new PipeOptions(pauseWriterThreshold: 10000000));
            var writer = new CustomPipeWriter(pipe.Writer);
            await JsonSerializer.SerializeAsync(writer, CreateManyTestObjects(), options);

            // Flush should happen every ~14,745 bytes (+36 for writing data when just below threshold)
            Assert.True(writer.Flushes.Count > (data.Length * 10_000 / 16_000), $"Flush count: {writer.Flushes.Count}");

            foreach (long flush in writer.Flushes)
            {
                Assert.True(flush < PipeOptions.Default.MinimumSegmentSize * 4);
            }

            IEnumerable<string> CreateManyTestObjects()
            {
                int i = 0;
                while (true)
                {
                    if (++i % 10_000 == 0)
                    {
                        break;
                    }
                    yield return data;
                }
            }
        }

        class MyStringConverter : JsonConverter<string>
        {
            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                throw new NotImplementedException();

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value);
            }
        }

        internal class TestPool : MemoryPool<byte>
        {
            private readonly int _bufferSize;
            private int _bufferCount;

            public int BufferCount => _bufferCount;

            public TestPool(int bufferSize)
            {
                _bufferSize = bufferSize;
            }

            public override int MaxBufferSize => _bufferSize;

            public override IMemoryOwner<byte> Rent(int minBufferSize = -1)
            {
                _bufferCount++;
                return new MemoryPoolBuffer(_bufferSize, this);
            }

            protected override void Dispose(bool disposing)
            { }

            private sealed class MemoryPoolBuffer : IMemoryOwner<byte>
            {
                private readonly TestPool _pool;
                private byte[]? _array;
                private int _length;

                public MemoryPoolBuffer(int size, TestPool pool)
                {
                    _array = ArrayPool<byte>.Shared.Rent(size);
                    _length = size;
                    _pool = pool;
                }

                public Memory<byte> Memory
                {
                    get
                    {
                        byte[]? array = _array;

                        return new Memory<byte>(array, 0, _length);
                    }
                }

                public void Dispose()
                {
                    byte[]? array = _array;
                    if (array != null)
                    {
                        _array = null;
                        ArrayPool<byte>.Shared.Return(array);
                        _pool._bufferCount--;
                    }
                }
            }
        }

        internal class PocoWithInstrumentedFastPath
        {
            public static PocoWithInstrumentedFastPath CreateValueWithSerializationSize(int targetSerializationSize)
            {
                int objectSerializationPaddingSize = """{"Value":""}""".Length; // 12
                return new PocoWithInstrumentedFastPath { Value = new string('a', targetSerializationSize - objectSerializationPaddingSize) };
            }

            public string? Value { get; set; }

            public class Context : JsonSerializerContext, IJsonTypeInfoResolver
            {
                public int FastPathInvocationCount { get; private set; }

                public Context(JsonSerializerOptions options) : base(options)
                { }

                protected override JsonSerializerOptions? GeneratedSerializerOptions => Options;
                public override JsonTypeInfo? GetTypeInfo(Type type) => GetTypeInfo(type, Options);

                public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
                {
                    if (type == typeof(string))
                    {
                        return JsonMetadataServices.CreateValueInfo<string>(options, JsonMetadataServices.StringConverter);
                    }

                    if (type == typeof(object))
                    {
                        return JsonMetadataServices.CreateValueInfo<object>(options, JsonMetadataServices.ObjectConverter);
                    }

                    if (type == typeof(PocoWithInstrumentedFastPath))
                    {
                        return JsonMetadataServices.CreateObjectInfo<PocoWithInstrumentedFastPath>(options,
                            new JsonObjectInfoValues<PocoWithInstrumentedFastPath>
                            {
                                PropertyMetadataInitializer = _ => new JsonPropertyInfo[1]
                                {
                                    JsonMetadataServices.CreatePropertyInfo<string>(options,
                                        new JsonPropertyInfoValues<string>
                                        {
                                            DeclaringType = typeof(PocoWithInstrumentedFastPath),
                                            PropertyName = "Value",
                                            Getter = obj => ((PocoWithInstrumentedFastPath)obj).Value,
                                        })
                                },

                                SerializeHandler = (writer, value) =>
                                {
                                    writer.WriteStartObject();
                                    writer.WriteString("Value", value.Value);
                                    writer.WriteEndObject();
                                    FastPathInvocationCount++;
                                }
                            });
                    }

                    return null;
                }
            }
        }
    }
}

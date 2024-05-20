// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
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
        public async Task CompletedPipeThrowsFromSerialize()
        {
            Pipe pipe = new Pipe();
            pipe.Reader.Complete();

            await Assert.ThrowsAsync<OperationCanceledException>(() => JsonSerializer.SerializeAsync(pipe.Writer, 1));
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
            Assert.Equal(10 + i - 1, result.Buffer.Length);
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class AsyncEnumerableTests : SerializerTests
    {
        public AsyncEnumerableTests(JsonSerializerWrapper serializerWrapper) : base(serializerWrapper)
        {
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(10, 1)]
        [InlineData(100, 1)]
        [InlineData(1000, 1)]
        [InlineData(1000, 1000)]
        [InlineData(1000, 32000)]
        public async Task DeserializeAsyncEnumerable_ReadSimpleObjectAsync(int count, int bufferSize)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = bufferSize
            };

            using var stream = new MemoryStream(GenerateJsonArray(count));

            int callbackCount = 0;
            await foreach (SimpleTestClass item in Serializer.DeserializeAsyncEnumerable<SimpleTestClass>(stream, options))
            {
                Assert.Equal(callbackCount, item.MyInt32);

                item.MyInt32 = 2; // Put correct value back for Verify()
                item.Verify();

                callbackCount++;
            }

            Assert.Equal(count, callbackCount);

            static byte[] GenerateJsonArray(int count)
            {
                SimpleTestClass[] collection = new SimpleTestClass[count];
                for (int i = 0; i < collection.Length; i++)
                {
                    var obj = new SimpleTestClass();
                    obj.Initialize();
                    obj.MyInt32 = i; // verify order correctness
                    collection[i] = obj;
                }

                return JsonSerializer.SerializeToUtf8Bytes(collection);
            }
        }

        [Theory]
        [MemberData(nameof(GetAsyncEnumerableSources))]
        public async Task DeserializeAsyncEnumerable_ReadSourceAsync<TElement>(IEnumerable<TElement> source, int bufferSize, DeserializeAsyncEnumerableOverload overload)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = bufferSize,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver()
            };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(source);

            using var stream = new MemoryStream(data);
            List<TElement> results = await DeserializeAsyncEnumerableWrapper<TElement>(stream, options, overload: overload).ToListAsync();
            Assert.Equal(source, results);
        }

        [Theory]
        [InlineData(DeserializeAsyncEnumerableOverload.JsonSerializerOptions)]
        [InlineData(DeserializeAsyncEnumerableOverload.JsonTypeInfo)]
        public async Task DeserializeAsyncEnumerable_ShouldStreamPartialData(DeserializeAsyncEnumerableOverload overload)
        {
            string json = JsonSerializer.Serialize(Enumerable.Range(0, 100));

            using var stream = new Utf8MemoryStream(json);
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = 1
            };

            IAsyncEnumerable<int> asyncEnumerable = DeserializeAsyncEnumerableWrapper<int>(stream, options, overload: overload);
            await using IAsyncEnumerator<int> asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();

            for (int i = 0; i < 20; i++)
            {
                bool success = await asyncEnumerator.MoveNextAsync();
                Assert.True(success, "AsyncEnumerator.MoveNextAsync() should return true.");
                Assert.True(stream.Position < stream.Capacity / 2, "should have consumed less than half of the stream contents.");
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        [InlineData(1000)]
        public async Task DeserializeAsyncEnumerable_Object_TopLevelValues(int count)
        {
            JsonSerializerOptions options = new() { DefaultBufferSize = 1 };
            string json = GenerateJsonTopLevelValues(count);
            using var stream = new Utf8MemoryStream(json);

            IAsyncEnumerable<SimpleTestClass> asyncEnumerable = Serializer.DeserializeAsyncEnumerable<SimpleTestClass>(stream, topLevelValues:true, options);

            int i = 0;
            await foreach (SimpleTestClass item in asyncEnumerable)
            {
                Assert.Equal(i++, item.MyInt32);
                item.MyInt32 = 2; // Put correct value back for Verify()
                item.Verify();
            }

            Assert.Equal(count, i);

            static string GenerateJsonTopLevelValues(int count)
            {
                StringBuilder sb = new();
                for (int i = 0; i < count; i++)
                {
                    var obj = new SimpleTestClass();
                    obj.Initialize();
                    obj.MyInt32 = i; // verify order correctness

                    sb.Append(JsonSerializer.Serialize(obj));
                    sb.Append((i % 5) switch { 0 => "", 1 => " ", 2 => "\t", 3 => "\r\n", _ => "   \n\n\n\n\n   " });
                }

                return sb.ToString();
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        [InlineData(1000)]
        public async Task DeserializeAsyncEnumerable_Array_TopLevelValues(int count)
        {
            JsonSerializerOptions options = new() { DefaultBufferSize = 1 };
            string json = GenerateJsonTopLevelValues(count);
            using var stream = new Utf8MemoryStream(json);

            IAsyncEnumerable<List<int>?> asyncEnumerable = Serializer.DeserializeAsyncEnumerable<List<int>?>(stream, topLevelValues:true, options);

            int i = 0;
            await foreach (List<int>? item in asyncEnumerable)
            {
                switch (i++ % 4)
                {
                    case 0:
                        Assert.Null(item);
                        break;
                    case 1:
                        Assert.Equal([], item);
                        break;
                    case 2:
                        Assert.Equal([1], item);
                        break;
                    case 3:
                        Assert.Equal([1, 2, 3], item);
                        break;
                }
            }

            Assert.Equal(count, i);

            static string GenerateJsonTopLevelValues(int count)
            {
                StringBuilder sb = new();
                for (int i = 0; i < count; i++)
                {
                    sb.Append((i % 4) switch { 0 => " null", 1 => "[]", 2 => "[1]", _ => "[1,2,3]" });
                    sb.Append((i % 5) switch { 0 => "", 1 => " ", 2 => "\t", 3 => "\r\n", _ => "   \n\n\n\n\n   " });
                }

                return sb.ToString();
            }
        }

        [Fact]
        public async Task DeserializeAsyncEnumerable_TopLevelValues_TrailingData_ThrowsJsonException()
        {
            JsonSerializerOptions options = new() { DefaultBufferSize = 1 };
            using var stream = new Utf8MemoryStream("[] [1] [1,2,3] <NotJson/>");

            IAsyncEnumerable<List<int>> asyncEnumerable = Serializer.DeserializeAsyncEnumerable<List<int>>(stream, topLevelValues:true, options);
            await using var asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();

            await Assert.ThrowsAnyAsync<JsonException>(async () =>
            {
                while (await asyncEnumerator.MoveNextAsync());
            });
        }

        [Theory]
        [InlineData(DeserializeAsyncEnumerableOverload.JsonSerializerOptions)]
        [InlineData(DeserializeAsyncEnumerableOverload.JsonTypeInfo)]
        public async Task DeserializeAsyncEnumerable_ShouldTolerateCustomQueueConverters(DeserializeAsyncEnumerableOverload overload)
        {
            const int expectedCount = 20;

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Converters = { new DegenerateQueueConverterFactory() }
            };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(Enumerable.Repeat(Enumerable.Repeat(1,3), expectedCount));

            using var stream = new MemoryStream(data);

            int callbackCount = 0;
            await foreach (Queue<int> nestedQueue in DeserializeAsyncEnumerableWrapper<Queue<int>>(stream, options, overload: overload))
            {
                Assert.Equal(1, nestedQueue.Count);
                Assert.Equal(0, nestedQueue.Peek());
                callbackCount++;
            }

            Assert.Equal(expectedCount, callbackCount);
        }

        private class DegenerateQueueConverterFactory : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeof(Queue<>) == typeToConvert.GetGenericTypeDefinition();
            public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                Type queueElement = typeToConvert.GetGenericArguments()[0];
                Type converterType = typeof(DegenerateQueueConverter<>).MakeGenericType(queueElement);
                return (JsonConverter)Activator.CreateInstance(converterType, nonPublic: true);
            }

            private class DegenerateQueueConverter<T> : JsonConverter<Queue<T>>
            {
                public override bool CanConvert(Type typeToConvert) => typeof(Queue<T>).IsAssignableFrom(typeToConvert);
                public override Queue<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                {
                    while (reader.Read() && reader.TokenType != JsonTokenType.EndArray);
                    var queue = new Queue<T>();
                    queue.Enqueue(default);
                    return queue;
                }

                public override void Write(Utf8JsonWriter writer, Queue<T> value, JsonSerializerOptions options) => throw new NotImplementedException();
            }
        }

        [Theory]
        [InlineData("42")]
        [InlineData("\"\"")]
        [InlineData("{}")]
        public async Task DeserializeAsyncEnumerable_NotARootLevelJsonArray_ThrowsJsonException(string json)
        {
            using var utf8Json = new Utf8MemoryStream(json);

            {
                IAsyncEnumerable<int> asyncEnumerable = Serializer.DeserializeAsyncEnumerable<int>(utf8Json);
                await using IAsyncEnumerator<int> enumerator = asyncEnumerable.GetAsyncEnumerator();
                await Assert.ThrowsAsync<JsonException>(async () => await enumerator.MoveNextAsync());
            }

            {
                IAsyncEnumerable<int> asyncEnumerable = Serializer.DeserializeAsyncEnumerable(utf8Json, ResolveJsonTypeInfo<int>());
                await using IAsyncEnumerator<int> enumerator = asyncEnumerable.GetAsyncEnumerator();
                await Assert.ThrowsAsync<JsonException>(async () => await enumerator.MoveNextAsync());
            }
        }

        [Theory]
        [InlineData(DeserializeAsyncEnumerableOverload.JsonSerializerOptions)]
        [InlineData(DeserializeAsyncEnumerableOverload.JsonTypeInfo)]
        public async Task DeserializeAsyncEnumerable_CancellationToken_ThrowsOnCancellation(DeserializeAsyncEnumerableOverload overload)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = 1,
            };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(Enumerable.Range(1, 100));

            var token = new CancellationToken(canceled: true);
            using var stream = new MemoryStream(data);
            var cancellableAsyncEnumerable = DeserializeAsyncEnumerableWrapper<int>(stream, options, token, overload);

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await foreach (int element in cancellableAsyncEnumerable)
                {
                }
            });
        }

        [Theory]
        [InlineData(DeserializeAsyncEnumerableOverload.JsonSerializerOptions)]
        [InlineData(DeserializeAsyncEnumerableOverload.JsonTypeInfo)]
        public async Task DeserializeAsyncEnumerable_EnumeratorWithCancellationToken_ThrowsOnCancellation(DeserializeAsyncEnumerableOverload overload)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = 1
            };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(Enumerable.Range(1, 100));

            var token = new CancellationToken(canceled: true);
            using var stream = new MemoryStream(data);
            var cancellableAsyncEnumerable = DeserializeAsyncEnumerableWrapper<int>(stream, options, overload: overload).WithCancellation(token);

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await foreach (int element in cancellableAsyncEnumerable)
                {
                }
            });
        }

        [Theory]
        [InlineData(5, 1024)]
        [InlineData(5, 1024 * 1024)]
        public async Task DeserializeAsyncEnumerable_SlowStreamWithLargeStrings(int totalStrings, int stringLength)
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new StringLengthConverter() }
            };

            using var stream = new SlowStream(GenerateJsonCharacters());
            string expectedElement = stringLength.ToString(CultureInfo.InvariantCulture);
            IAsyncEnumerable<string?> asyncEnumerable = Serializer.DeserializeAsyncEnumerable<string>(stream, options);

            await foreach (string? value in asyncEnumerable)
            {
                Assert.Equal(expectedElement, value);
            }

            IEnumerable<byte> GenerateJsonCharacters()
            {
                // ["xxx...x","xxx...x",...,"xxx...x"]
                yield return (byte)'[';
                for (int i = 0; i < totalStrings; i++)
                {
                    yield return (byte)'"';
                    for (int j = 0; j < stringLength; j++)
                    {
                        yield return (byte)'x';
                    }
                    yield return (byte)'"';

                    if (i < totalStrings - 1)
                    {
                        yield return (byte)',';
                    }
                }
                yield return (byte)']';
            }
        }

        public static IEnumerable<object[]> GetAsyncEnumerableSources()
        {
            yield return WrapArgs(Enumerable.Empty<int>(), 1, DeserializeAsyncEnumerableOverload.JsonSerializerOptions);
            yield return WrapArgs(Enumerable.Empty<int>(), 1, DeserializeAsyncEnumerableOverload.JsonTypeInfo);
            yield return WrapArgs(Enumerable.Range(0, 20), 1, DeserializeAsyncEnumerableOverload.JsonSerializerOptions);
            yield return WrapArgs(Enumerable.Range(0, 100), 20, DeserializeAsyncEnumerableOverload.JsonSerializerOptions);
            yield return WrapArgs(Enumerable.Range(0, 100).Select(i => $"lorem ipsum dolor: {i}"), 500, DeserializeAsyncEnumerableOverload.JsonSerializerOptions);
            yield return WrapArgs(Enumerable.Range(0, 100).Select(i => $"lorem ipsum dolor: {i}"), 500, DeserializeAsyncEnumerableOverload.JsonTypeInfo);
            yield return WrapArgs(Enumerable.Range(0, 10).Select(i => new { Field1 = i, Field2 = $"lorem ipsum dolor: {i}", Field3 = i % 2 == 0 }), 100, DeserializeAsyncEnumerableOverload.JsonSerializerOptions);
            yield return WrapArgs(Enumerable.Range(0, 10).Select(i => new { Field1 = i, Field2 = $"lorem ipsum dolor: {i}", Field3 = i % 2 == 0 }), 100, DeserializeAsyncEnumerableOverload.JsonTypeInfo);
            yield return WrapArgs(Enumerable.Range(0, 100).Select(i => new { Field1 = i, Field2 = $"lorem ipsum dolor: {i}", Field3 = i % 2 == 0 }), 500, DeserializeAsyncEnumerableOverload.JsonSerializerOptions);

            static object[] WrapArgs<TSource>(IEnumerable<TSource> source, int bufferSize, DeserializeAsyncEnumerableOverload overload) => new object[] { source, bufferSize, overload };
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(5)]
        [InlineData(43)]
        public async Task SerializeAsyncEnumerable_Cancellation_DisposesEnumerators(int depth)
        {
            // Regression test for https://github.com/dotnet/runtime/issues/120010

            using SelfCancellingAsyncEnumerable enumerable = new();
            using MemoryStream stream = new MemoryStream();

            object wrappingValue = enumerable;
            while (depth-- > 0)
            {
                // Use a LINQ enumerable instead of array/list
                // to force use of enumerators in every layer.
                wrappingValue = Enumerable.Repeat(wrappingValue, 1);
            }

            await Assert.ThrowsAsync<TaskCanceledException>(() => StreamingSerializer.SerializeWrapper(stream, wrappingValue, cancellationToken: enumerable.CancellationToken));
            Assert.True(enumerable.IsEnumeratorDisposed);
        }

        public enum DeserializeAsyncEnumerableOverload { JsonSerializerOptions, JsonTypeInfo };

        private IAsyncEnumerable<T> DeserializeAsyncEnumerableWrapper<T>(Stream stream, JsonSerializerOptions options = null, CancellationToken cancellationToken = default, DeserializeAsyncEnumerableOverload overload = DeserializeAsyncEnumerableOverload.JsonSerializerOptions)
        {
            return overload switch
            {
                DeserializeAsyncEnumerableOverload.JsonTypeInfo => Serializer.DeserializeAsyncEnumerable<T>(stream, ResolveJsonTypeInfo<T>(options), cancellationToken),
                DeserializeAsyncEnumerableOverload.JsonSerializerOptions or _ => Serializer.DeserializeAsyncEnumerable<T>(stream, options, cancellationToken),
            };
        }

        internal static JsonTypeInfo<T> ResolveJsonTypeInfo<T>(JsonSerializerOptions? options = null)
        {
            return (JsonTypeInfo<T>)ResolveJsonTypeInfo(typeof(T), options);
        }

        private static JsonTypeInfo ResolveJsonTypeInfo(Type type, JsonSerializerOptions? options = null)
        {
            options ??= JsonSerializerOptions.Default;
            options.TypeInfoResolver ??= new DefaultJsonTypeInfoResolver();
            options.MakeReadOnly(); // Lock the options instance before initializing metadata
            return options.TypeInfoResolver.GetTypeInfo(type, options);
        }

        private sealed class SlowStream(IEnumerable<byte> byteSource) : Stream, IDisposable
        {
            private readonly IEnumerator<byte> _enumerator = byteSource.GetEnumerator();
            private long _position;

            public override bool CanRead => true;
            public override int Read(byte[] buffer, int offset, int count)
            {
                Debug.Assert(buffer != null);
                Debug.Assert(offset >= 0 && count <= buffer.Length - offset);

                if (count == 0 || !_enumerator.MoveNext())
                {
                    return 0;
                }

                _position++;
                buffer[offset] = _enumerator.Current;
                return 1;
            }

            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Position { get => _position; set => throw new NotSupportedException(); }
            public override long Length => throw new NotSupportedException();
            public override void Flush() => throw new NotSupportedException();
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
            void IDisposable.Dispose() => _enumerator.Dispose();
        }

        private sealed class StringLengthConverter : JsonConverter<string>
        {
            public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                Debug.Assert(!reader.ValueIsEscaped);
                if (reader.HasValueSequence)
                {
                    return reader.ValueSequence.Length.ToString(CultureInfo.InvariantCulture);
                }
                return reader.ValueSpan.Length.ToString(CultureInfo.InvariantCulture);
            }

            public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) => throw new NotImplementedException();
        }

        sealed class SelfCancellingAsyncEnumerable : IAsyncEnumerable<int>, IDisposable
        {
            private readonly CancellationTokenSource _cts = new();
            public CancellationToken CancellationToken => _cts.Token;
            public bool IsEnumeratorDisposed { get; private set; }
            public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken _) => new Enumerator(this);
            private sealed class Enumerator(SelfCancellingAsyncEnumerable parent) : IAsyncEnumerator<int>
            {
                public int Current { get; private set; }
                public async ValueTask<bool> MoveNextAsync()
                {
                    await Task.Yield();
                    if (++Current == 10)
                    {
                        parent._cts.Cancel();
                    }

                    return true;
                }

                public ValueTask DisposeAsync()
                {
                    parent.IsEnumeratorDisposed = true;
                    return default;
                }
            }

            public void Dispose() => _cts.Dispose();
        }

        private sealed class ThrowingValue
        {
            private readonly int _v;
            private readonly bool _throwOnSerialize;

            public ThrowingValue() { }

            public ThrowingValue(int value, bool throwOnSerialize = false)
            {
                _v = value;
                _throwOnSerialize = throwOnSerialize;
            }

            public int V => _throwOnSerialize
                ? throw new InvalidOperationException("Simulated serialization failure.")
                : _v;
        }

        private sealed class DisposableAsyncEnumerable : IAsyncEnumerable<int>
        {
            public bool Disposed { get; private set; }

            public IAsyncEnumerator<int> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return new Enumerator(this, cancellationToken);
            }

            private sealed class Enumerator : IAsyncEnumerator<int>
            {
                private readonly DisposableAsyncEnumerable _parent;
                private readonly CancellationToken _ct;
                private int _index;

                public Enumerator(DisposableAsyncEnumerable parent, CancellationToken ct)
                {
                    _parent = parent;
                    _ct = ct;
                }

                public int Current { get; private set; }

                public async ValueTask<bool> MoveNextAsync()
                {
                    _ct.ThrowIfCancellationRequested();
                    await Task.Yield();
                    if (_index < 3)
                    {
                        Current = _index++;
                        return true;
                    }
                    return false;
                }

                public ValueTask DisposeAsync()
                {
                    _parent.Disposed = true;
                    return default;
                }
            }
        }

        private sealed class EmptyResolver : IJsonTypeInfoResolver
        {
            public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options) => null;
        }

        private sealed class ElementOnlyInt32Resolver : IJsonTypeInfoResolver
        {
            public JsonTypeInfo? GetTypeInfo(Type type, JsonSerializerOptions options)
            {
                // Simulates a source-gen context that only knows the element type.
                if (type == typeof(int))
                {
                    return JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter);
                }

                return null;
            }
        }

        // -----------------------------
        // SerializeAsyncEnumerable tests
        // -----------------------------

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        public async Task SerializeAsyncEnumerable_TopLevelValues_ProducesJsonLines(int count)
        {
            using MemoryStream stream = new();

            await JsonSerializer.SerializeAsyncEnumerable(
                stream,
                GenerateItems(count),
                ResolveJsonTypeInfo<SimpleTestClass>(),
                topLevelValues: true);

            byte[] bytes = stream.ToArray();
            if (count == 0)
            {
                Assert.Empty(bytes);
                return;
            }

            // Per JSONL spec each value is followed by a single \n, including the final one.
            Assert.Equal((byte)'\n', bytes[bytes.Length - 1]);
            // No \r should appear anywhere (the canonical JSONL terminator is \n only).
            Assert.DoesNotContain((byte)'\r', bytes);

            string result = Encoding.UTF8.GetString(bytes);
            string[] lines = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(count, lines.Length);

            for (int i = 0; i < count; i++)
            {
                SimpleTestClass deserialized = JsonSerializer.Deserialize<SimpleTestClass>(lines[i]);
                Assert.Equal(i, deserialized.MyInt32);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        public async Task SerializeAsyncEnumerable_PipeWriter_TopLevelValues_ProducesJsonLines(int count)
        {
            Pipe pipe = new();

            Task writeTask = JsonSerializer.SerializeAsyncEnumerable(
                pipe.Writer,
                GenerateItems(count),
                ResolveJsonTypeInfo<SimpleTestClass>(),
                topLevelValues: true);

            byte[] bytes = await ReadAllAsync(pipe, writeTask);
            if (count == 0)
            {
                Assert.Empty(bytes);
                return;
            }

            Assert.Equal((byte)'\n', bytes[bytes.Length - 1]);
            Assert.DoesNotContain((byte)'\r', bytes);

            string result = Encoding.UTF8.GetString(bytes);
            string[] lines = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(count, lines.Length);

            for (int i = 0; i < count; i++)
            {
                SimpleTestClass deserialized = JsonSerializer.Deserialize<SimpleTestClass>(lines[i]);
                Assert.Equal(i, deserialized.MyInt32);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(5)]
        [InlineData(100)]
        public async Task SerializeAsyncEnumerable_DefaultMode_ProducesJsonArray(int count)
        {
            using MemoryStream stream = new();

            await JsonSerializer.SerializeAsyncEnumerable(
                stream,
                GenerateItems(count),
                ResolveJsonTypeInfo<SimpleTestClass>());

            stream.Position = 0;
            SimpleTestClass[] roundTripped = JsonSerializer.Deserialize<SimpleTestClass[]>(stream);
            Assert.Equal(count, roundTripped.Length);
            for (int i = 0; i < count; i++)
            {
                Assert.Equal(i, roundTripped[i].MyInt32);
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_TopLevelValues_RoundTripsWithDeserializeAsyncEnumerable()
        {
            const int count = 50;
            using MemoryStream stream = new();

            await JsonSerializer.SerializeAsyncEnumerable(
                stream,
                GenerateItems(count),
                ResolveJsonTypeInfo<SimpleTestClass>(),
                topLevelValues: true);

            stream.Position = 0;

            int i = 0;
            await foreach (SimpleTestClass item in JsonSerializer.DeserializeAsyncEnumerable<SimpleTestClass>(stream, topLevelValues: true))
            {
                Assert.Equal(i++, item.MyInt32);
            }

            Assert.Equal(count, i);
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_PipeWriter_TopLevelValues_RoundTripsWithDeserializeAsyncEnumerable()
        {
            const int count = 50;
            Pipe pipe = new();

            Task writeTask = JsonSerializer.SerializeAsyncEnumerable(
                pipe.Writer,
                GenerateItems(count),
                ResolveJsonTypeInfo<SimpleTestClass>(),
                topLevelValues: true);

            byte[] bytes = await ReadAllAsync(pipe, writeTask);
            using MemoryStream stream = new(bytes);

            int i = 0;
            await foreach (SimpleTestClass item in JsonSerializer.DeserializeAsyncEnumerable<SimpleTestClass>(stream, topLevelValues: true))
            {
                Assert.Equal(i++, item.MyInt32);
            }

            Assert.Equal(count, i);
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_TopLevelValues_PrimitiveValues_ProducesJsonLines()
        {
            using MemoryStream stream = new();

            await JsonSerializer.SerializeAsyncEnumerable(
                stream,
                GenerateInts(),
                ResolveJsonTypeInfo<int>(),
                topLevelValues: true);

            byte[] bytes = stream.ToArray();
            // JSONL line terminator is canonical \n irrespective of platform.
            Assert.Equal("1\n2\n3\n", Encoding.UTF8.GetString(bytes));

            static async IAsyncEnumerable<int> GenerateInts()
            {
                yield return 1;
                yield return 2;
                yield return 3;
                await Task.CompletedTask;
            }
        }

        [Theory]
        [InlineData("\n")]
        [InlineData("\r\n")]
        public async Task SerializeAsyncEnumerable_TopLevelValues_LineTerminatorIsAlwaysLineFeed(string optionsNewLine)
        {
            // Per API review feedback (#126395): the JSONL line terminator is canonical \n
            // regardless of JsonSerializerOptions.NewLine.
            JsonSerializerOptions options = new() { NewLine = optionsNewLine };

            using MemoryStream stream = new();
            await JsonSerializer.SerializeAsyncEnumerable(
                stream,
                GenerateItems(3),
                ResolveJsonTypeInfo<SimpleTestClass>(options),
                topLevelValues: true);

            byte[] bytes = stream.ToArray();
            Assert.DoesNotContain((byte)'\r', bytes);
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_TopLevelValues_IgnoresWriteIndented()
        {
            // Per API review feedback (#126395): WriteIndented is ignored in JSONL mode so
            // that each value fits on a single line.
            JsonSerializerOptions options = new() { WriteIndented = true };

            using MemoryStream stream = new();
            await JsonSerializer.SerializeAsyncEnumerable(
                stream,
                GenerateItems(5),
                ResolveJsonTypeInfo<SimpleTestClass>(options),
                topLevelValues: true);

            byte[] bytes = stream.ToArray();
            Assert.Equal((byte)'\n', bytes[bytes.Length - 1]);

            // Each non-trailing newline marks the boundary between top-level values; the
            // payload between them must be a single self-contained JSON value.
            string result = Encoding.UTF8.GetString(bytes);
            string[] lines = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            Assert.Equal(5, lines.Length);

            foreach (string line in lines)
            {
                // Each entry is parseable as a single JSON value (no embedded raw newlines, no indentation breaking it apart).
                SimpleTestClass parsed = JsonSerializer.Deserialize<SimpleTestClass>(line);
                Assert.NotNull(parsed);
                Assert.DoesNotContain('\n', line);
                Assert.DoesNotContain('\r', line);
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_TopLevelValues_EmptySequence_ProducesEmptyOutput()
        {
            using MemoryStream stream = new();

            await JsonSerializer.SerializeAsyncEnumerable(
                stream,
                EmptyAsyncEnumerable(),
                ResolveJsonTypeInfo<int>(),
                topLevelValues: true);

            Assert.Equal(0, stream.Length);

            static async IAsyncEnumerable<int> EmptyAsyncEnumerable()
            {
                await Task.CompletedTask;
                yield break;
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_TopLevelValues_PartialItemFailure_PriorItemsAreFullyWritten()
        {
            // When element serialization throws mid-item, items written prior to the failure
            // are fully visible to the Stream consumer. Bytes for the failing item are not
            // flushed to the stream because the JSONL writer only flushes after each fully
            // serialized item.

            using MemoryStream stream = new();

            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                JsonSerializer.SerializeAsyncEnumerable(
                    stream,
                    Items(),
                    ResolveJsonTypeInfo<ThrowingValue>(),
                    topLevelValues: true));

            string actual = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal("{\"V\":1}\n{\"V\":2}\n", actual);

            static async IAsyncEnumerable<ThrowingValue> Items()
            {
                yield return new ThrowingValue(1);
                yield return new ThrowingValue(2);
                yield return new ThrowingValue(3, throwOnSerialize: true);
                await Task.CompletedTask;
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_PipeWriter_TopLevelValues_PartialItemFailure_PriorItemsAreFullyWritten()
        {
            // When element serialization throws mid-item, items written prior to the failure
            // are fully visible to the PipeWriter consumer. Bytes for the failing item itself
            // may or may not be visible depending on internal buffering — we make no guarantee
            // either way for the partial item.

            Pipe pipe = new();

            Task writeTask = JsonSerializer.SerializeAsyncEnumerable(
                pipe.Writer,
                Items(),
                ResolveJsonTypeInfo<ThrowingValue>(),
                topLevelValues: true);

            Task<byte[]> readerTask = Task.Run(async () =>
            {
                try
                {
                    using MemoryStream output = new();
                    while (true)
                    {
                        ReadResult result = await pipe.Reader.ReadAsync();
                        foreach (ReadOnlyMemory<byte> segment in result.Buffer)
                        {
                            byte[] tmp = segment.ToArray();
                            output.Write(tmp, 0, tmp.Length);
                        }

                        pipe.Reader.AdvanceTo(result.Buffer.End);
                        if (result.IsCompleted)
                        {
                            break;
                        }
                    }

                    return output.ToArray();
                }
                finally
                {
                    await pipe.Reader.CompleteAsync();
                }
            });

            await Assert.ThrowsAsync<InvalidOperationException>(() => writeTask);
            await pipe.Writer.CompleteAsync();
            byte[] bytes = await readerTask;

            string actual = Encoding.UTF8.GetString(bytes);
            // The first two items are guaranteed to be fully written; we don't make guarantees
            // about whether bytes for the failing third item are observable.
            const string ExpectedPrefix = "{\"V\":1}\n{\"V\":2}\n";
            Assert.StartsWith(ExpectedPrefix, actual);

            // Whatever follows must not be a self-contained third value; the third item failed.
            string trailing = actual.Substring(ExpectedPrefix.Length).TrimEnd('\n');
            Assert.DoesNotContain("\"V\":3", trailing);

            static async IAsyncEnumerable<ThrowingValue> Items()
            {
                yield return new ThrowingValue(1);
                yield return new ThrowingValue(2);
                yield return new ThrowingValue(3, throwOnSerialize: true);
                await Task.CompletedTask;
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_TopLevelValues_DisposesAsyncEnumerator()
        {
            DisposableAsyncEnumerable enumerable = new();
            using MemoryStream stream = new();

            await JsonSerializer.SerializeAsyncEnumerable(
                stream,
                enumerable,
                ResolveJsonTypeInfo<int>(),
                topLevelValues: true);

            Assert.True(enumerable.Disposed);
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_TopLevelValues_CancellationToken_DisposesAsyncEnumerator()
        {
            DisposableAsyncEnumerable enumerable = new();
            using MemoryStream stream = new();
            using CancellationTokenSource cts = new();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                JsonSerializer.SerializeAsyncEnumerable(
                    stream,
                    enumerable,
                    ResolveJsonTypeInfo<int>(),
                    topLevelValues: true,
                    cts.Token));

            Assert.True(enumerable.Disposed);
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_JsonTypeInfo_ArrayMode_WorksWithoutAsyncEnumerableMetadata()
        {
            // The JsonTypeInfo<TValue> overload should not require the resolver to know
            // IAsyncEnumerable<TValue>; the wrapper type info is synthesized from the supplied element metadata.
            JsonSerializerOptions options = new() { TypeInfoResolver = new EmptyResolver() };

            JsonTypeInfo<int> elementInfo = JsonMetadataServices.CreateValueInfo<int>(options, JsonMetadataServices.Int32Converter);
            options.MakeReadOnly();

            using MemoryStream stream = new();
            await JsonSerializer.SerializeAsyncEnumerable(stream, GenerateInts(), elementInfo);

            string actual = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal("[1,2,3]", actual);

            static async IAsyncEnumerable<int> GenerateInts()
            {
                yield return 1;
                yield return 2;
                yield return 3;
                await Task.CompletedTask;
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_Options_ArrayMode_WorksWithoutAsyncEnumerableMetadata()
        {
            // The JsonSerializerOptions-based overload must also synthesize IAsyncEnumerable<TValue>
            // metadata from the element type info: a source-gen resolver may register only the element type.
            JsonSerializerOptions options = new() { TypeInfoResolver = new ElementOnlyInt32Resolver() };

            using MemoryStream stream = new();
            await JsonSerializer.SerializeAsyncEnumerable(stream, GenerateInts(), options: options);

            string actual = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal("[1,2,3]", actual);

            static async IAsyncEnumerable<int> GenerateInts()
            {
                yield return 1;
                yield return 2;
                yield return 3;
                await Task.CompletedTask;
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_Options_TopLevelValues_WorksWithoutAsyncEnumerableMetadata()
        {
            JsonSerializerOptions options = new() { TypeInfoResolver = new ElementOnlyInt32Resolver() };

            using MemoryStream stream = new();
            await JsonSerializer.SerializeAsyncEnumerable(stream, GenerateInts(), topLevelValues: true, options);

            string actual = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal("1\n2\n3\n", actual);

            static async IAsyncEnumerable<int> GenerateInts()
            {
                yield return 1;
                yield return 2;
                yield return 3;
                await Task.CompletedTask;
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_TopLevelValues_PropagatesCustomEncoder()
        {
            // JsonSerializerOptions.Encoder must propagate to the JSONL writer. UnsafeRelaxedJsonEscaping
            // leaves '+' unescaped; the default encoder escapes it as \u002B.
            JsonSerializerOptions strict = new();
            JsonSerializerOptions relaxed = new() { Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

            using MemoryStream strictStream = new();
            using MemoryStream relaxedStream = new();

            await JsonSerializer.SerializeAsyncEnumerable(strictStream, Strings(), ResolveJsonTypeInfo<string>(strict), topLevelValues: true);
            await JsonSerializer.SerializeAsyncEnumerable(relaxedStream, Strings(), ResolveJsonTypeInfo<string>(relaxed), topLevelValues: true);

            Assert.Equal("\"a\\u002Bb\"\n", Encoding.UTF8.GetString(strictStream.ToArray()));
            Assert.Equal("\"a+b\"\n", Encoding.UTF8.GetString(relaxedStream.ToArray()));

            static async IAsyncEnumerable<string> Strings()
            {
                yield return "a+b";
                await Task.CompletedTask;
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_TopLevelValues_StringsContainingNewlinesAreEscaped()
        {
            // Round-trip: strings containing raw newline characters must be JSON-escaped (\n inside the
            // quoted string) so they don't break JSONL line parsing.
            using MemoryStream stream = new();
            await JsonSerializer.SerializeAsyncEnumerable(
                stream,
                Strings(),
                ResolveJsonTypeInfo<string>(),
                topLevelValues: true);

            byte[] bytes = stream.ToArray();
            // Each item is followed by exactly one literal '\n' (the JSONL terminator). Embedded \n
            // characters in the strings must appear escaped (as the two-char sequence "\n").
            int rawNewlines = bytes.Count(b => b == (byte)'\n');
            Assert.Equal(3, rawNewlines);

            stream.Position = 0;
            int i = 0;
            string[] expected = ["line1\nline2", "tab\there", "carriage\rreturn"];
            await foreach (string? entry in JsonSerializer.DeserializeAsyncEnumerable<string>(stream, topLevelValues: true))
            {
                Assert.Equal(expected[i++], entry);
            }
            Assert.Equal(3, i);

            static async IAsyncEnumerable<string> Strings()
            {
                yield return "line1\nline2";
                yield return "tab\there";
                yield return "carriage\rreturn";
                await Task.CompletedTask;
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_TopLevelValues_NullElementsAreWrittenAsNull()
        {
            using MemoryStream stream = new();
            await JsonSerializer.SerializeAsyncEnumerable(
                stream,
                Items(),
                ResolveJsonTypeInfo<string?>(),
                topLevelValues: true);

            Assert.Equal("\"a\"\nnull\n\"b\"\n", Encoding.UTF8.GetString(stream.ToArray()));

            stream.Position = 0;
            List<string?> roundTripped = new();
            await foreach (string? item in JsonSerializer.DeserializeAsyncEnumerable<string?>(stream, topLevelValues: true))
            {
                roundTripped.Add(item);
            }
            Assert.Equal(["a", null, "b"], roundTripped);

            static async IAsyncEnumerable<string?> Items()
            {
                yield return "a";
                yield return null;
                yield return "b";
                await Task.CompletedTask;
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_TopLevelValues_HandlesItemsLargerThanDefaultBufferSize()
        {
            // The Utf8JsonWriter calls _output.Advance + GetMemory mid-write (Grow) when an item is
            // larger than its current span. Verify large items round-trip cleanly.
            const int LargeStringLength = 16 * 1024;
            string payload = new('x', LargeStringLength);

            JsonSerializerOptions options = new() { DefaultBufferSize = 64 };

            using MemoryStream stream = new();
            await JsonSerializer.SerializeAsyncEnumerable(
                stream,
                Items(),
                ResolveJsonTypeInfo<string>(options),
                topLevelValues: true);

            stream.Position = 0;
            int i = 0;
            await foreach (string? item in JsonSerializer.DeserializeAsyncEnumerable<string>(stream, topLevelValues: true, options))
            {
                Assert.Equal(payload, item);
                i++;
            }
            Assert.Equal(3, i);

            async IAsyncEnumerable<string> Items()
            {
                yield return payload;
                yield return payload;
                yield return payload;
                await Task.CompletedTask;
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_TopLevelValues_PolymorphicItems()
        {
            // Polymorphic root-level values: each derived type must serialize with its discriminator
            // and round-trip back to the correct derived instance.
            using MemoryStream stream = new();
            await JsonSerializer.SerializeAsyncEnumerable(
                stream,
                Items(),
                ResolveJsonTypeInfo<PolymorphicBase>(),
                topLevelValues: true);

            stream.Position = 0;
            List<PolymorphicBase?> roundTripped = new();
            await foreach (PolymorphicBase? item in JsonSerializer.DeserializeAsyncEnumerable<PolymorphicBase>(stream, topLevelValues: true))
            {
                roundTripped.Add(item);
            }

            Assert.Equal(2, roundTripped.Count);
            Assert.IsType<PolymorphicDerivedA>(roundTripped[0]);
            Assert.Equal(7, ((PolymorphicDerivedA)roundTripped[0]!).A);
            Assert.IsType<PolymorphicDerivedB>(roundTripped[1]);
            Assert.Equal("hi", ((PolymorphicDerivedB)roundTripped[1]!).B);

            static async IAsyncEnumerable<PolymorphicBase> Items()
            {
                yield return new PolymorphicDerivedA { A = 7 };
                yield return new PolymorphicDerivedB { B = "hi" };
                await Task.CompletedTask;
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_PipeWriter_TopLevelValues_StopsWhenReaderCompletes()
        {
            // Verify that when FlushAsync returns IsCompleted=true (the reader has completed), the
            // writer breaks out of the loop and does not consume the rest of the IAsyncEnumerable.
            int yielded = 0;
            CompletingPipeWriter pipeWriter = new(completeAfterFlushes: 2);

            await JsonSerializer.SerializeAsyncEnumerable(
                pipeWriter,
                Items(),
                ResolveJsonTypeInfo<int>(),
                topLevelValues: true);

            // Stops after producing the items that triggered the first two flushes; the rest of the
            // 100-item enumerable is never consumed.
            Assert.InRange(yielded, 1, 10);
            Assert.Equal(2, pipeWriter.FlushCount);

            async IAsyncEnumerable<int> Items()
            {
                for (int i = 0; i < 100; i++)
                {
                    yielded++;
                    yield return i;
                    await Task.Yield();
                }
            }
        }

        private sealed class CompletingPipeWriter : PipeWriter
        {
            private readonly int _completeAfterFlushes;
            private byte[] _buffer = new byte[1024];
            private int _written;

            public CompletingPipeWriter(int completeAfterFlushes)
            {
                _completeAfterFlushes = completeAfterFlushes;
            }

            public int FlushCount { get; private set; }
            public override bool CanGetUnflushedBytes => true;
            public override long UnflushedBytes => _written;

            public override void Advance(int bytes) => _written += bytes;

            public override Memory<byte> GetMemory(int sizeHint = 0)
            {
                if (sizeHint <= 0) sizeHint = 256;
                if (_written + sizeHint > _buffer.Length)
                {
                    Array.Resize(ref _buffer, Math.Max(_buffer.Length * 2, _written + sizeHint));
                }
                return _buffer.AsMemory(_written);
            }

            public override Span<byte> GetSpan(int sizeHint = 0) => GetMemory(sizeHint).Span;

            public override void CancelPendingFlush() { }
            public override void Complete(Exception? exception = null) { }

            public override ValueTask<FlushResult> FlushAsync(CancellationToken cancellationToken = default)
            {
                FlushCount++;
                bool isCompleted = FlushCount >= _completeAfterFlushes;
                return new ValueTask<FlushResult>(new FlushResult(isCanceled: false, isCompleted: isCompleted));
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_TopLevelValues_PipeReader_RoundTrip()
        {
            // Round-trip serialize-via-Stream → deserialize-via-PipeReader.
            using MemoryStream stream = new();
            await JsonSerializer.SerializeAsyncEnumerable(
                stream,
                GenerateItems(20),
                ResolveJsonTypeInfo<SimpleTestClass>(),
                topLevelValues: true);

            stream.Position = 0;
            PipeReader reader = PipeReader.Create(stream);

            try
            {
                int i = 0;
                await foreach (SimpleTestClass? item in JsonSerializer.DeserializeAsyncEnumerable<SimpleTestClass>(reader, topLevelValues: true))
                {
                    Assert.NotNull(item);
                    Assert.Equal(i++, item.MyInt32);
                }
                Assert.Equal(20, i);
            }
            finally
            {
                await reader.CompleteAsync();
            }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_TopLevelValues_CancellationMidIteration()
        {
            // Cancel after a few items have been emitted; verify the remaining items are not consumed
            // and an OperationCanceledException is raised.
            using CancellationTokenSource cts = new();
            using MemoryStream stream = new();
            int yielded = 0;

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                JsonSerializer.SerializeAsyncEnumerable(
                    stream,
                    Items(),
                    ResolveJsonTypeInfo<int>(),
                    topLevelValues: true,
                    cts.Token));

            // We expected to produce a few items before cancellation took effect.
            Assert.InRange(yielded, 1, 10);

            async IAsyncEnumerable<int> Items()
            {
                for (int i = 0; i < 1000; i++)
                {
                    if (i == 3)
                    {
                        cts.Cancel();
                    }
                    yielded++;
                    yield return i;
                    await Task.Yield();
                }
            }
        }

        [JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
        [JsonDerivedType(typeof(PolymorphicDerivedA), typeDiscriminator: "a")]
        [JsonDerivedType(typeof(PolymorphicDerivedB), typeDiscriminator: "b")]
        public abstract class PolymorphicBase { }

        public sealed class PolymorphicDerivedA : PolymorphicBase
        {
            public int A { get; set; }
        }

        public sealed class PolymorphicDerivedB : PolymorphicBase
        {
            public string? B { get; set; }
        }

        [Fact]
        public async Task SerializeAsyncEnumerable_NullArguments_ThrowsArgumentNullException()
        {
            JsonTypeInfo<int> typeInfo = ResolveJsonTypeInfo<int>();
            IAsyncEnumerable<int> source = AsyncEnumerable();

            await AssertExtensions.ThrowsAsync<ArgumentNullException>("utf8Json", () =>
                JsonSerializer.SerializeAsyncEnumerable<int>(utf8Json: (Stream)null!, source));
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("value", () =>
                JsonSerializer.SerializeAsyncEnumerable<int>(new MemoryStream(), value: null!));
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("utf8Json", () =>
                JsonSerializer.SerializeAsyncEnumerable<int>(utf8Json: (Stream)null!, source, typeInfo));
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("value", () =>
                JsonSerializer.SerializeAsyncEnumerable<int>(new MemoryStream(), value: null!, typeInfo));
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("jsonTypeInfo", () =>
                JsonSerializer.SerializeAsyncEnumerable<int>(new MemoryStream(), source, jsonTypeInfo: null!));

            await AssertExtensions.ThrowsAsync<ArgumentNullException>("utf8Json", () =>
                JsonSerializer.SerializeAsyncEnumerable<int>(utf8Json: (PipeWriter)null!, source));
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("value", () =>
                JsonSerializer.SerializeAsyncEnumerable<int>(new Pipe().Writer, value: null!));
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("utf8Json", () =>
                JsonSerializer.SerializeAsyncEnumerable<int>(utf8Json: (PipeWriter)null!, source, typeInfo));
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("value", () =>
                JsonSerializer.SerializeAsyncEnumerable<int>(new Pipe().Writer, value: null!, typeInfo));
            await AssertExtensions.ThrowsAsync<ArgumentNullException>("jsonTypeInfo", () =>
                JsonSerializer.SerializeAsyncEnumerable<int>(new Pipe().Writer, source, jsonTypeInfo: null!));

            static async IAsyncEnumerable<int> AsyncEnumerable()
            {
                await Task.CompletedTask;
                yield break;
            }
        }

        // ----------------------------------------------------------
        // DeserializeAsyncEnumerable JSONL spec coverage (issue 126395)
        // ----------------------------------------------------------

        public static IEnumerable<object[]> JsonLinesValidShapes()
        {
            // Per https://jsonlines.org/, every line is a valid JSON value separated by \n.
            // The reader is intentionally lenient (Postel's law) so it accepts a superset of strict JSONL.

            // Strict JSONL with trailing line feed.
            yield return new object[] { "1\n2\n3\n", new[] { 1, 2, 3 } };
            // Strict JSONL without trailing line feed.
            yield return new object[] { "1\n2\n3", new[] { 1, 2, 3 } };
            // Single value, no trailing newline.
            yield return new object[] { "42", new[] { 42 } };
            // Single value, with trailing newline.
            yield return new object[] { "42\n", new[] { 42 } };
            // Empty document.
            yield return new object[] { "", Array.Empty<int>() };
            // Document containing only whitespace.
            yield return new object[] { "   \n\t\n", Array.Empty<int>() };
            // Document containing only line terminators.
            yield return new object[] { "\n\n\n", Array.Empty<int>() };
            // Lenient: \r\n separator (still valid JSONL since values are self-delimiting).
            yield return new object[] { "1\r\n2\r\n3\r\n", new[] { 1, 2, 3 } };
            // Lenient: extra whitespace (tabs, spaces) between/around values.
            yield return new object[] { "  1  \n\t2\t\n 3 \n", new[] { 1, 2, 3 } };
        }

        [Theory]
        [MemberData(nameof(JsonLinesValidShapes))]
        public async Task DeserializeAsyncEnumerable_TopLevelValues_AcceptsAllJsonLinesShapes(string input, int[] expected)
        {
            using Utf8MemoryStream stream = new(input);

            List<int> actual = new();
            await foreach (int item in Serializer.DeserializeAsyncEnumerable<int>(stream, topLevelValues: true))
            {
                actual.Add(item);
            }

            Assert.Equal(expected, actual);
        }

        [Fact]
        public async Task DeserializeAsyncEnumerable_TopLevelValues_HeterogeneousJsonValueTypes()
        {
            // JSONL spec admits any JSON value, not just objects.
            // Verify we round-trip through JsonElement to cover all six JSON token kinds.
            const string Document = "null\ntrue\n42\n\"hello\"\n[1,2,3]\n{\"k\":\"v\"}\n";

            using Utf8MemoryStream stream = new(Document);

            List<JsonElement> values = new();
            await foreach (JsonElement element in Serializer.DeserializeAsyncEnumerable<JsonElement>(stream, topLevelValues: true))
            {
                values.Add(element.Clone());
            }

            Assert.Equal(6, values.Count);
            Assert.Equal(JsonValueKind.Null, values[0].ValueKind);
            Assert.Equal(JsonValueKind.True, values[1].ValueKind);
            Assert.Equal(JsonValueKind.Number, values[2].ValueKind);
            Assert.Equal(JsonValueKind.String, values[3].ValueKind);
            Assert.Equal(JsonValueKind.Array, values[4].ValueKind);
            Assert.Equal(JsonValueKind.Object, values[5].ValueKind);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(8)]
        [InlineData(64)]
        [InlineData(4096)]
        public async Task DeserializeAsyncEnumerable_TopLevelValues_HandlesArbitraryBufferBoundaries(int bufferSize)
        {
            // Stress test: ensure the reader correctly resumes across small/large stream chunks
            // when JSONL line terminators land in different places relative to buffer boundaries.
            const int Count = 200;

            StringBuilder sb = new();
            for (int i = 0; i < Count; i++)
            {
                sb.Append('{').Append("\"V\":").Append(i).Append("}\n");
            }

            using Utf8MemoryStream stream = new(sb.ToString());
            JsonSerializerOptions options = new() { DefaultBufferSize = bufferSize };

            int next = 0;
            await foreach (Dictionary<string, int> entry in Serializer.DeserializeAsyncEnumerable<Dictionary<string, int>>(stream, topLevelValues: true, options))
            {
                Assert.Equal(next, entry["V"]);
                next++;
            }

            Assert.Equal(Count, next);
        }

        // -----------------------------
        // Helpers
        // -----------------------------

        private static async IAsyncEnumerable<SimpleTestClass> GenerateItems(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var obj = new SimpleTestClass();
                obj.Initialize();
                obj.MyInt32 = i;
                yield return obj;
                await Task.Yield();
            }
        }

        private static async Task<byte[]> ReadAllAsync(Pipe pipe, Task writerTask)
        {
            // Run the writer to completion in parallel with the reader.
            // When the writer finishes, signal end-of-stream by completing the PipeWriter.
            Task completer = writerTask.ContinueWith(
                t => pipe.Writer.CompleteAsync(t.Exception?.InnerException).AsTask(),
                TaskScheduler.Default).Unwrap();

            using MemoryStream output = new();
            try
            {
                while (true)
                {
                    ReadResult result = await pipe.Reader.ReadAsync();
                    foreach (ReadOnlyMemory<byte> segment in result.Buffer)
                    {
                        byte[] tmp = segment.ToArray();
                        output.Write(tmp, 0, tmp.Length);
                    }

                    pipe.Reader.AdvanceTo(result.Buffer.End);
                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await pipe.Reader.CompleteAsync();
            }

            await completer;
            await writerTask;
            return output.ToArray();
        }
    }
}

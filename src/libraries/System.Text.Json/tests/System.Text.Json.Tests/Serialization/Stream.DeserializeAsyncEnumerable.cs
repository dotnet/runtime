// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class StreamTests_DeserializeAsyncEnumerable
    {
        [Theory]
        [InlineData(0, 1)]
        [InlineData(1, 1)]
        [InlineData(10, 1)]
        [InlineData(100, 1)]
        [InlineData(1000, 1)]
        [InlineData(1000, 1000)]
        [InlineData(1000, 32000)]
        public static async Task DeserializeAsyncEnumerable_ReadSimpleObjectAsync(int count, int bufferSize)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = bufferSize
            };

            using var stream = new MemoryStream(GenerateJsonArray(count));

            int callbackCount = 0;
            await foreach (SimpleTestClass item in JsonSerializer.DeserializeAsyncEnumerable<SimpleTestClass>(stream, options))
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
        public static async Task DeserializeAsyncEnumerable_ReadSourceAsync<TElement>(IEnumerable<TElement> source, int bufferSize, bool useJsonTypeInfoOverload)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = bufferSize
            };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(source);

            using var stream = new MemoryStream(data);
            List<TElement> results = await DeserializeAsyncEnumerableWrapper<TElement>(stream, options, useJsonTypeInfoOverload: useJsonTypeInfoOverload).ToListAsync();
            Assert.Equal(source, results);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static async Task DeserializeAsyncEnumerable_ShouldStreamPartialData(bool useJsonTypeInfoOverload)
        {
            string json = JsonSerializer.Serialize(Enumerable.Range(0, 100));

            using var stream = new Utf8MemoryStream(json);
            IAsyncEnumerable<int> asyncEnumerable = DeserializeAsyncEnumerableWrapper<int>(stream, new JsonSerializerOptions { DefaultBufferSize = 1 }, useJsonTypeInfoOverload: useJsonTypeInfoOverload);
            await using IAsyncEnumerator<int> asyncEnumerator = asyncEnumerable.GetAsyncEnumerator();

            for (int i = 0; i < 20; i++)
            {
                bool success = await asyncEnumerator.MoveNextAsync();
                Assert.True(success, "AsyncEnumerator.MoveNextAsync() should return true.");
                Assert.True(stream.Position < stream.Capacity / 2, "should have consumed less than half of the stream contents.");
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static async Task DeserializeAsyncEnumerable_ShouldTolerateCustomQueueConverters(bool useJsonTypeInfoOverload)
        {
            const int expectedCount = 20;

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                Converters = { new DegenerateQueueConverterFactory() }
            };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(Enumerable.Repeat(Enumerable.Repeat(1,3), expectedCount));

            using var stream = new MemoryStream(data);

            int callbackCount = 0;
            await foreach (Queue<int> nestedQueue in DeserializeAsyncEnumerableWrapper<Queue<int>>(stream, options, useJsonTypeInfoOverload: useJsonTypeInfoOverload))
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

        [Fact]
        public static void DeserializeAsyncEnumerable_NullArgument_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("utf8Json", () => JsonSerializer.DeserializeAsyncEnumerable<int>(utf8Json: null));
            AssertExtensions.Throws<ArgumentNullException>("utf8Json", () => JsonSerializer.DeserializeAsyncEnumerable<int>(utf8Json: null, jsonTypeInfo: ResolveJsonTypeInfo<int>()));
            AssertExtensions.Throws<ArgumentNullException>("jsonTypeInfo", () => JsonSerializer.DeserializeAsyncEnumerable<int>(utf8Json: new MemoryStream(), jsonTypeInfo: null));
        }

        [Theory]
        [InlineData("42")]
        [InlineData("\"\"")]
        [InlineData("{}")]
        public static async Task DeserializeAsyncEnumerable_NotARootLevelJsonArray_ThrowsJsonException(string json)
        {
            using var utf8Json = new Utf8MemoryStream(json);

            {
                IAsyncEnumerable<int> asyncEnumerable = JsonSerializer.DeserializeAsyncEnumerable<int>(utf8Json);
                await using IAsyncEnumerator<int> enumerator = asyncEnumerable.GetAsyncEnumerator();
                await Assert.ThrowsAsync<JsonException>(async () => await enumerator.MoveNextAsync());
            }

            {
                IAsyncEnumerable<int> asyncEnumerable = JsonSerializer.DeserializeAsyncEnumerable<int>(utf8Json, ResolveJsonTypeInfo<int>());
                await using IAsyncEnumerator<int> enumerator = asyncEnumerable.GetAsyncEnumerator();
                await Assert.ThrowsAsync<JsonException>(async () => await enumerator.MoveNextAsync());
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static async Task DeserializeAsyncEnumerable_CancellationToken_ThrowsOnCancellation(bool useJsonTypeInfoOverload)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = 1
            };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(Enumerable.Range(1, 100));

            var token = new CancellationToken(canceled: true);
            using var stream = new MemoryStream(data);
            var cancellableAsyncEnumerable = DeserializeAsyncEnumerableWrapper<int>(stream, options, token, useJsonTypeInfoOverload);

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await foreach (int element in cancellableAsyncEnumerable)
                {
                }
            });
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public static async Task DeserializeAsyncEnumerable_EnumeratorWithCancellationToken_ThrowsOnCancellation(bool useJsonTypeInfoOverload)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = 1
            };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(Enumerable.Range(1, 100));

            var token = new CancellationToken(canceled: true);
            using var stream = new MemoryStream(data);
            var cancellableAsyncEnumerable = DeserializeAsyncEnumerableWrapper<int>(stream, options, useJsonTypeInfoOverload: useJsonTypeInfoOverload).WithCancellation(token);

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await foreach (int element in cancellableAsyncEnumerable)
                {
                }
            });
        }

        public static IEnumerable<object[]> GetAsyncEnumerableSources()
        {
            yield return WrapArgs(Enumerable.Empty<int>(), 1, false);
            yield return WrapArgs(Enumerable.Empty<int>(), 1, true);
            yield return WrapArgs(Enumerable.Range(0, 20), 1, false);
            yield return WrapArgs(Enumerable.Range(0, 100), 20, false);
            yield return WrapArgs(Enumerable.Range(0, 100).Select(i => $"lorem ipsum dolor: {i}"), 500, false);
            yield return WrapArgs(Enumerable.Range(0, 100).Select(i => $"lorem ipsum dolor: {i}"), 500, true);
            yield return WrapArgs(Enumerable.Range(0, 10).Select(i => new { Field1 = i, Field2 = $"lorem ipsum dolor: {i}", Field3 = i % 2 == 0 }), 100, false);
            yield return WrapArgs(Enumerable.Range(0, 10).Select(i => new { Field1 = i, Field2 = $"lorem ipsum dolor: {i}", Field3 = i % 2 == 0 }), 100, false);
            yield return WrapArgs(Enumerable.Range(0, 100).Select(i => new { Field1 = i, Field2 = $"lorem ipsum dolor: {i}", Field3 = i % 2 == 0 }), 500, false);

            static object[] WrapArgs<TSource>(IEnumerable<TSource> source, int bufferSize, bool useJsonTypeInfoOverload) => new object[] { source, bufferSize, useJsonTypeInfoOverload };
        }

        private static IAsyncEnumerable<T> DeserializeAsyncEnumerableWrapper<T>(Stream stream, JsonSerializerOptions options = null, CancellationToken cancellationToken = default, bool useJsonTypeInfoOverload = false)
        {
            return useJsonTypeInfoOverload
                ? JsonSerializer.DeserializeAsyncEnumerable<T>(stream, ResolveJsonTypeInfo<T>(options), cancellationToken)
                : JsonSerializer.DeserializeAsyncEnumerable<T>(stream, options, cancellationToken);
        }

        private static JsonTypeInfo<T> ResolveJsonTypeInfo<T>(JsonSerializerOptions? options = null)
        {
            // TODO replace with contract resolver once implemented -- only works with value converters.
            options ??= JsonSerializerOptions.Default;
            JsonConverter<T> converter = (JsonConverter<T>)options.GetConverter(typeof(T));
            return JsonMetadataServices.CreateValueInfo<T>(options, converter);
        }

        private static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> source)
        {
            var list = new List<T>();
            await foreach (T item in source)
            {
                list.Add(item);
            }
            return list;
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
            await foreach(SimpleTestClass item in JsonSerializer.DeserializeAsyncEnumerable<SimpleTestClass>(stream, options))
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
        public static async Task DeserializeAsyncEnumerable_ReadSourceAsync<TElement>(IEnumerable<TElement> source, int bufferSize)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = bufferSize
            };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(source);

            using var stream = new MemoryStream(data);
            List<TElement> results = await JsonSerializer.DeserializeAsyncEnumerable<TElement>(stream, options).ToListAsync();
            Assert.Equal(source, results);
        }

        [Fact]
        public static void DeserializeAsyncEnumerable_NullStream_ThrowsArgumentNullException()
        {
            AssertExtensions.Throws<ArgumentNullException>("utf8Json", () => JsonSerializer.DeserializeAsyncEnumerable<int>(utf8Json: null));
        }

        [Fact]
        public static async Task DeserializeAsyncEnumerable_CancellationToken_ThrowsOnCancellation()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = 1
            };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(Enumerable.Range(1, 100));

            var token = new CancellationToken(canceled: true);
            using var stream = new MemoryStream(data);
            var cancellableAsyncEnumerable = JsonSerializer.DeserializeAsyncEnumerable<int>(stream, options, token);

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await foreach (int element in cancellableAsyncEnumerable)
                {
                }
            });
        }

        [Fact]
        public static async Task DeserializeAsyncEnumerable_EnumeratorWithCancellationToken_ThrowsOnCancellation()
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = 1
            };

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(Enumerable.Range(1, 100));

            var token = new CancellationToken(canceled: true);
            using var stream = new MemoryStream(data);
            var cancellableAsyncEnumerable = JsonSerializer.DeserializeAsyncEnumerable<int>(stream, options).WithCancellation(token);

            await Assert.ThrowsAsync<TaskCanceledException>(async () =>
            {
                await foreach (int element in cancellableAsyncEnumerable)
                {
                }
            });
        }

        public static IEnumerable<object[]> GetAsyncEnumerableSources()
        {
            yield return WrapArgs(Enumerable.Empty<int>(), 1);
            yield return WrapArgs(Enumerable.Range(0, 20), 1);
            yield return WrapArgs(Enumerable.Range(0, 100), 20);
            yield return WrapArgs(Enumerable.Range(0, 100).Select(i => $"lorem ipsum dolor: {i}"), 500);
            yield return WrapArgs(Enumerable.Range(0, 10).Select(i => new { Field1 = i, Field2 = $"lorem ipsum dolor: {i}", Field3 = i % 2 == 0 }), 100);
            yield return WrapArgs(Enumerable.Range(0, 100).Select(i => new { Field1 = i, Field2 = $"lorem ipsum dolor: {i}", Field3 = i % 2 == 0 }), 500);

            static object[] WrapArgs<TSource>(IEnumerable<TSource> source, int bufferSize) => new object[] { source, bufferSize };
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

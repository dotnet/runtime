// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class StreamTests_IAsyncEnumerable
    {
        [Theory]
        [InlineData(1, 1)]
        [InlineData(10, 1)]
        [InlineData(100, 1)]
        [InlineData(1000, 1)]
        [InlineData(1000, 1000)]
        [InlineData(1000, 32000)]
        public static async Task ReadSimpleObjectAsync(int count, int bufferSize)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = bufferSize
            };

            // Produce the JSON
            SimpleTestClass[] collection = new SimpleTestClass[count];
            for (int i = 0; i < collection.Length; i++)
            {
                var obj = new SimpleTestClass();
                obj.Initialize();
                obj.MyInt32 = i; // verify order correctness
                collection[i] = obj;
            }

            byte[] data = JsonSerializer.SerializeToUtf8Bytes(collection);

            // Use async await on the Stream.
            using (MemoryStream stream = new MemoryStream(data))
            {
                int callbackCount = 0;

                await foreach(SimpleTestClass item in
                        JsonSerializer.DeserializeAsyncEnumerable<SimpleTestClass>(stream, options))
                {
                    Assert.Equal(callbackCount, item.MyInt32);

                    item.MyInt32 = 2; // Put correct value back for Verify()
                    item.Verify();

                    callbackCount++;
                }

                Assert.Equal(count, callbackCount);
            }
        }

        private class SimpleObjectProvider : IAsyncEnumerable<SimpleTestClass>
        {
            private int _count;

            public SimpleObjectProvider(int count)
            {
                _count = count;
            }

            public async IAsyncEnumerator<SimpleTestClass> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                for (int i = 0; i < _count; i++)
                {
                    await Task.Delay(1);
                    var obj = new SimpleTestClass();
                    obj.Initialize();
                    yield return obj;
                }
            }
        }

        [Theory]
        [InlineData(1, 1)]
        [InlineData(10, 1)]
        [InlineData(100, 1)]
        [InlineData(100, 1000)]
        [InlineData(100, 32000)]
        public static async Task WriteSimpleObjectAsync(int count, int bufferSize)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = bufferSize
            };

            // Calculate the byte length of a single item not in a JSON array.
            long singleLength;
            using (MemoryStream singleObjectStream = new MemoryStream())
            {
                var obj = new SimpleTestClass();
                obj.Initialize();

                await JsonSerializer.SerializeAsync<SimpleTestClass>(singleObjectStream, obj, options);
                singleLength = singleObjectStream.Length;
            }

            using (MemoryStream stream = new MemoryStream())
            {
                await JsonSerializer.SerializeAsyncEnumerable(
                    stream,
                    new SimpleObjectProvider(count),
                    options);

                long allLength = stream.Length;
                allLength -= 1; // account for start array token.
                allLength -= count; // account for commas; includes end array token since there is no trailing comma.

                Assert.Equal(singleLength * count, allLength);

                // Verify the contents.
                stream.Position = 0;
                SimpleTestClass[] result = await JsonSerializer.DeserializeAsync<SimpleTestClass[]>(stream);
                Assert.Equal(count, result.Length);
                for (int i = 0; i < count; i++)
                {
                    result[i].Verify();
                }
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
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
    }
}

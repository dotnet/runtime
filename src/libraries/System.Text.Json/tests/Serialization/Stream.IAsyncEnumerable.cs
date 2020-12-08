// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class StreamTests_IAsyncEnumerable
    {
        [Theory]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        [InlineData(1000)]
        public static async Task ReadSimpleObjectAsync(int count)
        {
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                DefaultBufferSize = 1
            };

            // Produce the JSON
            SimpleTestClass[] collection = new SimpleTestClass[count];
            for (int i = 0; i < collection.Length; i++)
            {
                var obj = new SimpleTestClass();
                obj.Initialize();
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
                    item.Verify();
                    callbackCount++;
                }

                Assert.Equal(count, callbackCount);
            }
        }
    }
}

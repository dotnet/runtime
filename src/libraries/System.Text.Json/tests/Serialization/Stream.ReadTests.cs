// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class StreamTests
    {
        [Fact]
        public static async Task NullArgumentFail()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.DeserializeAsync<string>((Stream)null));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.DeserializeAsync(new MemoryStream(), (Type)null));
        }

        [Fact]
        public static async Task ReadSimpleObjectAsync()
        {
            using (MemoryStream stream = new MemoryStream(SimpleTestClass.s_data))
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 1
                };

                SimpleTestClass obj = await JsonSerializer.DeserializeAsync<SimpleTestClass>(stream, options);
                obj.Verify();
            }
        }

        [Fact]
        public static async Task ReadSimpleObjectWithTrailingTriviaAsync()
        {
            byte[] data = Encoding.UTF8.GetBytes(SimpleTestClass.s_json + " /* Multi\r\nLine Comment */\t");
            using (MemoryStream stream = new MemoryStream(data))
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 1,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                };

                SimpleTestClass obj = await JsonSerializer.DeserializeAsync<SimpleTestClass>(stream, options);
                obj.Verify();
            }
        }

        [Fact]
        public static async Task ReadPrimitivesAsync()
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(@"1")))
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 1
                };

                int i = await JsonSerializer.DeserializeAsync<int>(stream, options);
                Assert.Equal(1, i);
            }
        }

        [Fact]
        public static async Task ReadPrimitivesWithTrailingTriviaAsync()
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(" 1\t// Comment\r\n/* Multi\r\nLine */")))
            {
                JsonSerializerOptions options = new JsonSerializerOptions
                {
                    DefaultBufferSize = 1,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                };

                int i = await JsonSerializer.DeserializeAsync<int>(stream, options);
                Assert.Equal(1, i);
            }
        }

        [Fact]
        public static async Task ReadReferenceTypeCollectionPassingNullValueAsync()
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("null")))
            {
                IList<object> referenceTypeCollection = await JsonSerializer.DeserializeAsync<IList<object>>(stream);
                Assert.Null(referenceTypeCollection);
            }
        }

        [Fact]
        public static async Task ReadValueTypeCollectionPassingNullValueAsync()
        {
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("null")))
            {
                IList<int> valueTypeCollection = await JsonSerializer.DeserializeAsync<IList<int>>(stream);
                Assert.Null(valueTypeCollection);
            }
        }

        public static IEnumerable<object[]> BOMTestData =>
            new List<object[]>
            {
                new object[] {new byte[] { 0xEF, 0xBB, 0xBF, 49 }, default, 1 },
                new object[] {new byte[] { 0xEF, 0xBB, 0xBF, 49 }, new JsonSerializerOptions { DefaultBufferSize = 1 }, 1 },
                new object[] {new byte[] { 0xEF, 0xBB, 0xBF, 49 }, new JsonSerializerOptions { DefaultBufferSize = 2 }, 1 },
                new object[] {new byte[] { 0xEF, 0xBB, 0xBF, 49 }, new JsonSerializerOptions { DefaultBufferSize = 3 }, 1 },
                new object[] {new byte[] { 0xEF, 0xBB, 0xBF, 49 }, new JsonSerializerOptions { DefaultBufferSize = 4 }, 1 },
                new object[] {new byte[] { 0xEF, 0xBB, 0xBF, 49 }, new JsonSerializerOptions { DefaultBufferSize = 15 }, 1 },
                new object[] {new byte[] { 0xEF, 0xBB, 0xBF, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49 }, new JsonSerializerOptions { DefaultBufferSize = 15 }, 1111111111111111111 },
                new object[] {new byte[] { 0xEF, 0xBB, 0xBF, 49 }, new JsonSerializerOptions { DefaultBufferSize = 16 }, 1 },
                new object[] {new byte[] { 0xEF, 0xBB, 0xBF, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49 }, new JsonSerializerOptions { DefaultBufferSize = 16 }, 1111111111111111111 },
                new object[] {new byte[] { 0xEF, 0xBB, 0xBF, 49 }, new JsonSerializerOptions { DefaultBufferSize = 17 }, 1 },
                new object[] {new byte[] { 0xEF, 0xBB, 0xBF, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49, 49 }, new JsonSerializerOptions { DefaultBufferSize = 17 }, 1111111111111111111 },
            };

        [Theory]
        [MemberData(nameof(BOMTestData))]
        public static async Task TestBOMWithSingleJsonValue(byte[] utf8BomAndValueArray, JsonSerializerOptions options, ulong expected)
        {
            ulong value;
            using (Stream stream = new MemoryStream(utf8BomAndValueArray))
            {
                value = await JsonSerializer.DeserializeAsync<ulong>(stream, options);
            }
            Assert.Equal(expected, value);
        }

        [Fact]
        public static async Task TestBOMWithNoJsonValue()
        {
            byte[] utf8BomAndValueArray = new byte[] { 0xEF, 0xBB, 0xBF };
            using (Stream stream = new MemoryStream(utf8BomAndValueArray))
            {
                await Assert.ThrowsAsync<JsonException>(
                    async () => await JsonSerializer.DeserializeAsync<byte>(stream));
            }
        }
    }
}

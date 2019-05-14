// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class StreamTests
    {
        [Fact]
        public static async void NullArgumentFail()
        {
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.ReadAsync<string>((Stream)null));
            await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.ReadAsync(new MemoryStream(), (Type)null));
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

                SimpleTestClass obj = await JsonSerializer.ReadAsync<SimpleTestClass>(stream, options);
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

                SimpleTestClass obj = await JsonSerializer.ReadAsync<SimpleTestClass>(stream, options);
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

                int i = await JsonSerializer.ReadAsync<int>(stream, options);
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

                int i = await JsonSerializer.ReadAsync<int>(stream, options);
                Assert.Equal(1, i);
            }
        }
    }
}

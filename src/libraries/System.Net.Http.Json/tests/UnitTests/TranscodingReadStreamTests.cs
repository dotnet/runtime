// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Taken from https://github.com/dotnet/aspnetcore/blob/master/src/Mvc/Mvc.Core/test/Formatters/TranscodingReadStreamTest.cs

using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace System.Net.Http.Json.Functional.Tests
{
    public class TranscodingReadStreamTest
    {
        [Fact]
        public async Task ReadAsync_SingleByte()
        {
            // Arrange
            string input = "Hello world";
            Encoding encoding = Encoding.Unicode;
            using (TranscodingReadStream stream = new TranscodingReadStream(new MemoryStream(encoding.GetBytes(input)), encoding))
            {
                var bytes = new byte[4];

                // Act
                int readBytes = await stream.ReadAsync(bytes, 0, 1);

                // Assert
                Assert.Equal(1, readBytes);
                Assert.Equal((byte)'H', bytes[0]);
                Assert.Equal(0, bytes[1]);

                Assert.Equal(0, stream.ByteBufferCount);
                Assert.Equal(0, stream.CharBufferCount);
                Assert.Equal(10, stream.OverflowCount);
            }
        }

        [Fact]
        public async Task ReadAsync_FillsBuffer()
        {
            // Arrange
            string input = "Hello world";
            Encoding encoding = Encoding.Unicode;
            using (TranscodingReadStream stream = new TranscodingReadStream(new MemoryStream(encoding.GetBytes(input)), encoding))
            {
                byte[] bytes = new byte[3];
                byte[] expected = Encoding.UTF8.GetBytes(input.Substring(0, bytes.Length));

                // Act
                int readBytes = await stream.ReadAsync(bytes, 0, bytes.Length);

                // Assert
                Assert.Equal(3, readBytes);
                Assert.Equal(expected, bytes);
                Assert.Equal(0, stream.ByteBufferCount);
                Assert.Equal(0, stream.CharBufferCount);
                Assert.Equal(8, stream.OverflowCount);
            }
        }

        [Fact]
        public async Task ReadAsync_CompletedInSecondIteration()
        {
            // Arrange
            string input = new string('A', 1024 + 10);
            Encoding encoding = Encoding.Unicode;
            using (TranscodingReadStream stream = new TranscodingReadStream(new MemoryStream(encoding.GetBytes(input)), encoding))
            {
                var bytes = new byte[1024];
                byte[] expected = Encoding.UTF8.GetBytes(input.Substring(0, bytes.Length));

                // Act
                int readBytes = await stream.ReadAsync(bytes, 0, bytes.Length);

                // Assert
                Assert.Equal(bytes.Length, readBytes);
                Assert.Equal(expected, bytes);
                Assert.Equal(0, stream.ByteBufferCount);
                Assert.Equal(10, stream.CharBufferCount);
                Assert.Equal(0, stream.OverflowCount);

                readBytes = await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.Equal(10, readBytes);
                Assert.Equal(0, stream.ByteBufferCount);
                Assert.Equal(0, stream.CharBufferCount);
                Assert.Equal(0, stream.OverflowCount);
            }
        }

        [Fact]
        public async Task ReadAsync_WithOverflowBuffer()
        {
            // Arrange
            // Test ensures that the overflow buffer works correctly
            string input = "\u2600";
            Encoding encoding = Encoding.Unicode;
            using (TranscodingReadStream stream = new TranscodingReadStream(new MemoryStream(encoding.GetBytes(input)), encoding))
            {
                var bytes = new byte[1];
                byte[] expected = Encoding.UTF8.GetBytes(input);

                // Act
                int readBytes = await stream.ReadAsync(bytes, 0, bytes.Length);

                // Assert
                Assert.Equal(1, readBytes);
                Assert.Equal(expected[0], bytes[0]);
                Assert.Equal(0, stream.ByteBufferCount);
                Assert.Equal(0, stream.CharBufferCount);
                Assert.Equal(2, stream.OverflowCount);

                bytes = new byte[expected.Length - 1];
                readBytes = await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.Equal(bytes.Length, readBytes);
                Assert.Equal(0, stream.ByteBufferCount);
                Assert.Equal(0, stream.CharBufferCount);
                Assert.Equal(0, stream.OverflowCount);

                readBytes = await stream.ReadAsync(bytes, 0, bytes.Length);
                Assert.Equal(0, readBytes);
            }
        }

        public static TheoryData<string> ReadAsync_WithOverflowBuffer_AtBoundariesData(string encoding)
        {
            int maxCharBufferSize = Encoding.GetEncoding(encoding).GetMaxCharCount(TranscodingReadStream.MaxByteBufferSize);

            return new TheoryData<string>
            {
                new string('a', maxCharBufferSize - 1) + '\u2600',
                new string('a', maxCharBufferSize - 2) + '\u2600',
                new string('a', maxCharBufferSize) + '\u2600',
            };
        }

        [Theory]
        [MemberData(nameof(ReadAsync_WithOverflowBuffer_AtBoundariesData), "utf-16")]
        public Task ReadAsync_WithOverflowBuffer_WithBufferSize1(string input) => ReadAsync_WithOverflowBufferAtCharBufferBoundaries(input, bufferSize: 1);

        private static async Task ReadAsync_WithOverflowBufferAtCharBufferBoundaries(string input, int bufferSize)
        {
            // Arrange
            // Test ensures that the overflow buffer works correctly
            Encoding encoding = Encoding.Unicode;
            using (TranscodingReadStream stream = new TranscodingReadStream(new MemoryStream(encoding.GetBytes(input)), encoding))
            {
                byte[] expected = Encoding.UTF8.GetBytes(input);

                // Act
                var buffer = new byte[bufferSize];
                var actual = new List<byte>();

                while (await stream.ReadAsync(buffer, 0, bufferSize) != 0)
                {
                    actual.AddRange(buffer);
                }

                Assert.Equal(expected, actual);
            }
        }

        public static TheoryData ReadAsyncInputLatin(string encoding)
        {
            int maxCharBufferSize = Encoding.GetEncoding(encoding).GetMaxCharCount(TranscodingReadStream.MaxByteBufferSize);
            return GetLatinTextInput(maxCharBufferSize, TranscodingReadStream.MaxByteBufferSize);
        }

        public static TheoryData ReadAsyncInputUnicode(string encoding)
        {
            int maxCharBufferSize = Encoding.GetEncoding(encoding).GetMaxCharCount(TranscodingReadStream.MaxByteBufferSize);
            return GetUnicodeText(maxCharBufferSize);
        }

        internal static TheoryData GetLatinTextInput(int maxCharBufferSize, int maxByteBufferSize)
        {
            return new TheoryData<string>
            {
                "Hello world",
                string.Join(string.Empty, Enumerable.Repeat("AB", 9000)),
                new string('A', count: maxByteBufferSize),
                new string('A', count: maxCharBufferSize),
                new string('A', count: maxByteBufferSize + 1),
                new string('A', count: maxCharBufferSize + 1),
            };
        }

        internal static TheoryData GetUnicodeText(int maxCharBufferSize)
        {
            return new TheoryData<string>
            {
                new string('\u00c6', count: 7),

                new string('A', count: maxCharBufferSize - 1) + '\u00c6',

                "Ab\u0100\u0101\u0102\u0103\u0104\u0105\u0106\u014a\u014b\u014c\u014d\u014e\u014f\u0150\u0151\u0152\u0153\u0154\u0155\u0156\u0157\u0158\u0159\u015a\u015f\u0160\u0161\u0162\u0163\u0164\u0165\u0166\u0167\u0168\u0169\u016a\u016b\u016c\u016d\u016e\u016f\u0170\u0171\u0172\u0173\u0174\u0175\u0176\u0177\u0178\u0179\u017a\u017b\u017c\u017d\u017e\u017fAbc",

               "Abc\u0b90\u0b92\u0b93\u0b94\u0b95\u0b99\u0b9a\u0b9c\u0b9e\u0b9f\u0ba3\u0ba4\u0ba8\u0ba9\u0baa\u0bae\u0baf\u0bb0\u0bb1\u0bb2\u0bb3\u0bb4\u0bb5\u0bb7\u0bb8\u0bb9",

               "\u2600\u2601\u2602\u2603\u2604\u2605\u2606\u2607\u2608\u2609\u260a\u260b\u260c\u260d\u260e\u260f\u2610\u2611\u2612\u2613\u261a\u261b\u261c\u261d\u261e\u261f\u2620\u2621\u2622\u2623\u2624\u2625\u2626\u2627\u2628\u2629\u262a\u262b\u262c\u262d\u262e\u262f\u2630\u2631\u2632\u2633\u2634\u2635\u2636\u2637\u2638",

                new string('\u00c6', count: 64 * 1024),

                new string('\u00c6', count: 64 * 1024 + 1),

               "ping\u00fcino",

                new string('\u0904', count: maxCharBufferSize + 1), // This uses 3 bytes to represent in UTF8
            };
        }

        [Theory]
        [MemberData(nameof(ReadAsyncInputLatin), "utf-32")]
        [MemberData(nameof(ReadAsyncInputUnicode), "utf-32")]
        public Task ReadAsync_Works_WhenInputIs_UTF32(string message)
        {
            Encoding sourceEncoding = Encoding.UTF32;
            return ReadAsyncTest(sourceEncoding, message);
        }

        [Theory]
        [MemberData(nameof(ReadAsyncInputLatin), "utf-16")]
        [MemberData(nameof(ReadAsyncInputUnicode), "utf-16")]
        public Task ReadAsync_Works_WhenInputIs_Unicode(string message)
        {
            Encoding sourceEncoding = Encoding.Unicode;
            return ReadAsyncTest(sourceEncoding, message);
        }

        [Theory]
        [MemberData(nameof(ReadAsyncInputLatin), "iso-8859-1")]
        public Task ReadAsync_Works_WhenInputIs_WesternEuropeanEncoding(string message)
        {
            // Arrange
            Encoding sourceEncoding = Encoding.GetEncoding(28591);
            return ReadAsyncTest(sourceEncoding, message);
        }

        [Theory]
        [MemberData(nameof(ReadAsyncInputLatin), "us-ascii")]
        public Task ReadAsync_Works_WhenInputIs_ASCII(string message)
        {
            // Arrange
            Encoding sourceEncoding = Encoding.ASCII;
            return ReadAsyncTest(sourceEncoding, message);
        }

        private static async Task ReadAsyncTest(Encoding sourceEncoding, string message)
        {
            string input = $"{{ \"Message\": \"{message}\" }}";
            var stream = new MemoryStream(sourceEncoding.GetBytes(input));

            using (var transcodingStream = new TranscodingReadStream(stream, sourceEncoding))
            {

                object model = await JsonSerializer.DeserializeAsync(transcodingStream, typeof(TestModel));
                TestModel testModel = Assert.IsType<TestModel>(model);

                Assert.Equal(message, testModel.Message);
            }
        }

        public class TestModel
        {
            public string Message { get; set; }
        }

        [Fact]
        public async Task TestOneToOneTranscodingAsync()
        {
            Encoding sourceEncoding = Encoding.GetEncoding(28591);
            string message = '"' + new string('A', TranscodingReadStream.MaxByteBufferSize - 2 + 1) + '"';

            Stream stream = new MemoryStream(sourceEncoding.GetBytes(message));
            using (TranscodingReadStream transcodingStream = new TranscodingReadStream(stream, sourceEncoding))
            {
                string deserializedMessage = await JsonSerializer.DeserializeAsync<string>(transcodingStream);
                Assert.Equal(message.Trim('"'), deserializedMessage);
            }
        }
    }
}

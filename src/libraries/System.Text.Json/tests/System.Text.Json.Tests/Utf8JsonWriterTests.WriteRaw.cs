// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Text.Json.Tests
{
    public partial class Utf8JsonWriterTests
    {
        private const string TestGuidAsStr = "eb97fadd-3ebf-4781-8722-f4773989160e";
        private readonly static Guid s_guid = Guid.Parse(TestGuidAsStr);
        private static byte[] s_guidAsJson = WrapInQuotes(TestGuidAsStr);

        private static byte[] s_oneAsJson = new byte[] { (byte)'1' };

        [Theory]
        [MemberData(nameof(GetRootLevelPrimitives))]
        [MemberData(nameof(GetArrays))]
        public static void WriteRawValidJson(byte[] rawJson, Action<byte[]> verifyWithDeserialize)
        {
            using MemoryStream ms = new();
            using Utf8JsonWriter writer = new(ms);

            RunTests(skipInputValidation: true);
            RunTests(skipInputValidation: false);

            void RunTests(bool skipInputValidation)
            {
                // ROS<byte>
                writer.Reset();
                ms.SetLength(0);
                writer.WriteRawValue(rawJson, skipInputValidation);
                writer.Flush();
                verifyWithDeserialize(ms.ToArray());

                // string
                string rawJsonAsStr = Encoding.UTF8.GetString(rawJson);
                writer.Reset();
                ms.SetLength(0);
                writer.WriteRawValue(rawJsonAsStr, skipInputValidation);
                writer.Flush();
                verifyWithDeserialize(ms.ToArray());

                // ROS<char>
                writer.Reset();
                ms.SetLength(0);
                writer.WriteRawValue(rawJsonAsStr.AsSpan(), skipInputValidation);
                writer.Flush();
                verifyWithDeserialize(ms.ToArray());
            }
        }

        public static IEnumerable<object[]> GetRootLevelPrimitives()
        {
            Action<byte[]> validate;

            validate = (data) => Assert.Equal(123456789, JsonSerializer.Deserialize<long>(data));
            yield return new object[] { Encoding.UTF8.GetBytes("123456789"), validate };

            validate = (data) => Assert.Equal(1234.56789, JsonSerializer.Deserialize<double>(data));
            yield return new object[] { Encoding.UTF8.GetBytes("1234.56789"), validate };

            validate = (data) => Assert.Equal(@"Hello", JsonSerializer.Deserialize<string>(data));
            yield return new object[] { Encoding.UTF8.GetBytes(@"""Hello"""), validate };

            validate = (data) => Assert.Equal(s_guid, JsonSerializer.Deserialize<Guid>(data));
            yield return new object[] { s_guidAsJson, validate };
        }

        public static IEnumerable<object[]> GetArrays()
        {
            Action<byte[]> validate;

            byte[] json = JsonSerializer.SerializeToUtf8Bytes(Enumerable.Repeat(1234.56789, 4));
            validate = (data) =>
            {
                foreach (double d in JsonSerializer.Deserialize<double[]>(data))
                {
                    Assert.Equal(1234.56789, d);
                }
            };
            yield return new object[] { json, validate };

            json = JsonSerializer.SerializeToUtf8Bytes(Enumerable.Repeat("Hello", 4));
            validate = (data) =>
            {
                foreach (string str in JsonSerializer.Deserialize<string[]>(data))
                {
                    Assert.Equal("Hello", str);
                }
            };
            yield return new object[] { json, validate };

            json = JsonSerializer.SerializeToUtf8Bytes(Enumerable.Repeat("Hello", 4));
            validate = (data) =>
            {
                foreach (string str in JsonSerializer.Deserialize<string[]>(data))
                {
                    Assert.Equal("Hello", str);
                }
            };
            yield return new object[] { json, validate };
        }

        private static byte[] WrapInQuotes(string json)
        {
            byte[] buffer = new byte[json.Length + 2];
            buffer[0] = (byte)'"';
            Encoding.UTF8.GetBytes(json).CopyTo(buffer, 1);
            buffer[json.Length + 1] = (byte)'"';
            return buffer;
        }

        [Theory]
        [InlineData(true, 0, "[]")]
        [InlineData(false, 0, "[]")]
        [InlineData(true, 1, "[1]")]
        [InlineData(false, 1, "[1]")]
        [InlineData(true, 5, "[1,1,1,1,1]")]
        [InlineData(false, 5, "[1,1,1,1,1]")]
        public static void WriteRawArrayElements(bool skipInputValidation, int numElements, string expectedJson)
        {
            using MemoryStream ms = new();
            using Utf8JsonWriter writer = new(ms);
            writer.WriteStartArray();

            for (int i = 0; i < numElements; i++)
            {
                writer.WriteRawValue(s_oneAsJson, skipInputValidation);
            }

            writer.WriteEndArray();

            writer.Flush();
            Assert.Equal(expectedJson, Encoding.UTF8.GetString(ms.ToArray()));
        }

        [Theory]
        [InlineData(true, 0, "{}")]
        [InlineData(false, 0, "{}")]
        [InlineData(true, 1, @"{""int"":1}")]
        [InlineData(false, 1, @"{""int"":1}")]
        [InlineData(true, 3, @"{""int"":1,""int"":1,""int"":1}")]
        [InlineData(false, 3, @"{""int"":1,""int"":1,""int"":1}")]
        public static void WriteRawObjectProperty(bool skipInputValidation, int numElements, string expectedJson)
        {
            using MemoryStream ms = new();
            using Utf8JsonWriter writer = new(ms);
            writer.WriteStartObject();

            for (int i = 0; i < numElements; i++)
            {
                writer.WritePropertyName("int");
                writer.WriteRawValue(s_oneAsJson, skipInputValidation);
            }

            writer.WriteEndObject();

            writer.Flush();
            Assert.Equal(expectedJson, Encoding.UTF8.GetString(ms.ToArray()));
        }

        [Theory]
        [InlineData("[")]
        [InlineData("}")]
        [InlineData("[}")]
        [InlineData("xxx")]
        [InlineData("{hello:")]
        public static void WriteRawInvalidJson(string json)
        {
            RunTest(true);
            RunTest(false);

            void RunTest(bool skipValidation)
            {
                using MemoryStream ms = new();
                using Utf8JsonWriter writer = new(ms);

                if (!skipValidation)
                {
                    Assert.Throws<ArgumentException>(() => writer.WriteRawValue(json));
                }
                else
                {
                    writer.WriteRawValue(json, true);
                }
            }
        }

        /// <summary>
        /// This test is constrained to run on Windows and MacOSX because it causes
        /// problems on Linux due to the way deferred memory allocation works. On Linux, the allocation can
        /// succeed even if there is not enough memory but then the test may get killed by the OOM killer at the
        /// time the memory is accessed which triggers the full memory allocation.
        /// Also see <see cref="Utf8JsonWriterTests.WriteLargeJsonToStreamWithoutFlushing"/>
        /// </summary>
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        [ConditionalFact(nameof(IsX64))]
        [OuterLoop]
        public void WriteLargeRawJsonToStreamWithoutFlushing()
        {
            var largeArray = new char[150_000_000];
            largeArray.AsSpan().Fill('a');

            // Text size chosen so that after several doublings of the underlying buffer we reach ~2 GB (but don't go over)
            JsonEncodedText text1 = JsonEncodedText.Encode(largeArray.AsSpan(0, 7_500));
            JsonEncodedText text2 = JsonEncodedText.Encode(largeArray.AsSpan(0, 5_000));
            JsonEncodedText text3 = JsonEncodedText.Encode(largeArray.AsSpan(0, 150_000_000));

            using (var output = new MemoryStream())
            using (var writer = new Utf8JsonWriter(output))
            {
                writer.WriteStartArray();
                writer.WriteRawValue(text1.EncodedUtf8Bytes);
                Assert.Equal(7_503, writer.BytesPending);

                for (int i = 0; i < 30_000; i++)
                {
                    writer.WriteRawValue(text2.EncodedUtf8Bytes);
                }
                Assert.Equal(150_097_503, writer.BytesPending);

                for (int i = 0; i < 13; i++)
                {
                    writer.WriteRawValue(text3.EncodedUtf8Bytes);
                }
                Assert.Equal(2_100_097_542, writer.BytesPending);

                // Next write forces a grow beyond max array length

                Assert.Throws<OutOfMemoryException>(() => writer.WriteRawValue(text3.EncodedUtf8Bytes));

                Assert.Equal(2_100_097_542, writer.BytesPending);

                var text4 = JsonEncodedText.Encode(largeArray.AsSpan(0, 1));
                for (int i = 0; i < 10_000_000; i++)
                {
                    writer.WriteRawValue(text4.EncodedUtf8Bytes);
                }

                Assert.Equal(2_100_097_542 + (4 * 10_000_000), writer.BytesPending);
            }
        }

        [Fact]
        public static void WriteRawNullOrEmptyTokenInvalid()
        {
            using MemoryStream ms = new();
            using Utf8JsonWriter writer = new(ms);
            Assert.Throws<ArgumentNullException>(() => writer.WriteRawValue(json: default(string)));
            Assert.Throws<ArgumentException>(() => writer.WriteRawValue(json: ""));
            Assert.Throws<ArgumentException>(() => writer.WriteRawValue(json: default(ReadOnlySpan<char>)));
            Assert.Throws<ArgumentException>(() => writer.WriteRawValue(utf8Json: default));
        }
    }
}

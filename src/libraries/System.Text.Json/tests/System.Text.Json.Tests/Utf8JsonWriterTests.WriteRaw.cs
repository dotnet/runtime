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
            yield return new object[] { "123456789"u8.ToArray(), validate };

            validate = (data) => Assert.Equal(1234.56789, JsonSerializer.Deserialize<double>(data));
            yield return new object[] { "1234.56789"u8.ToArray(), validate };

            validate = (data) => Assert.Equal(1234.56789, JsonSerializer.Deserialize<double>(data));
            yield return new object[] { " 1234.56789 "u8.ToArray(), validate };

            validate = (data) => Assert.Equal(@"Hello", JsonSerializer.Deserialize<string>(data));
            yield return new object[] { Encoding.UTF8.GetBytes(@"""Hello"""), validate };

            validate = (data) => Assert.Equal(@"Hello", JsonSerializer.Deserialize<string>(data));
            yield return new object[] { Encoding.UTF8.GetBytes(@"  ""Hello""  "), validate };

            validate = (data) => Assert.Equal(s_guid, JsonSerializer.Deserialize<Guid>(data));
            byte[] guidAsJson = WrapInQuotes(Encoding.UTF8.GetBytes(TestGuidAsStr));
            yield return new object[] { guidAsJson, validate };
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

            json = "[ 1, 1,1,1,1 ] "u8.ToArray();
            validate = (data) =>
            {
                foreach (int val in JsonSerializer.Deserialize<int[]>(data))
                {
                    Assert.Equal(1, val);
                }
            };
            yield return new object[] { json, validate };
        }

        public static IEnumerable<object[]> GetObjects()
        {
            Action<byte[]> validate;

            byte[] json = Encoding.UTF8.GetBytes(@"{""Hello"":""World""}"); ;
            validate = (data) =>
            {
                KeyValuePair<string, string> kvp = JsonSerializer.Deserialize<Dictionary<string, string>>(data).Single();
                Assert.Equal("Hello", kvp.Key);
                Assert.Equal("World", kvp.Value);
            };
            yield return new object[] { json, validate };

            json = Encoding.UTF8.GetBytes(@" {  ""Hello""    :""World""  }   "); ;
            validate = (data) =>
            {
                KeyValuePair<string, string> kvp = JsonSerializer.Deserialize<Dictionary<string, string>>(data).Single();
                Assert.Equal("Hello", kvp.Key);
                Assert.Equal("World", kvp.Value);
            };
            yield return new object[] { json, validate };
        }

        private static byte[] WrapInQuotes(ReadOnlySpan<byte> buffer)
        {
            byte[] quotedBuffer = new byte[buffer.Length + 2];
            quotedBuffer[0] = (byte)'"';
            buffer.CopyTo(quotedBuffer.AsSpan(1));
            quotedBuffer[buffer.Length + 1] = (byte)'"';
            return quotedBuffer;
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
        [InlineData("\\u007Bhello:")]
        [InlineData(@"{""hello:""""")]
        [InlineData(" ")]
        [InlineData("// This is a single line comment")]
        [InlineData("/* This is a multi-\nline comment*/")]
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
                    Assert.ThrowsAny<JsonException>(() => writer.WriteRawValue(json));
                }
                else
                {
                    writer.WriteRawValue(json, true);
                    writer.Flush();
                    Assert.True(Encoding.UTF8.GetBytes(json).SequenceEqual(ms.ToArray()));
                }
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void WriteRawHonorSkipValidation(bool skipValidation)
        {
            RunTest(true);
            RunTest(false);

            void RunTest(bool skipInputValidation)
            {
                using MemoryStream ms = new();
                using Utf8JsonWriter writer = new(ms, new JsonWriterOptions { SkipValidation = skipValidation });

                writer.WriteStartObject();

                if (skipValidation)
                {
                    writer.WriteRawValue(@"{}", skipInputValidation);
                    writer.Flush();
                    Assert.True(ms.ToArray().SequenceEqual(new byte[] { (byte)'{',  (byte)'{', (byte)'}' }));
                }
                else
                {
                    Assert.Throws<InvalidOperationException>(() => writer.WriteRawValue(@"{}", skipInputValidation));
                }
            }
        }

        [Fact]
        public static void WriteRawDepthExceedsMaxOf64Fail()
        {

            RunTest(GenerateJsonUsingDepth(1), false);
            RunTest(GenerateJsonUsingDepth(64), false);
            RunTest(GenerateJsonUsingDepth(65), true);
            RunTest(GenerateJsonUsingDepth(65), false, true);

            void RunTest(string json, bool expectFail, bool skipInputValidation = false)
            {
                using MemoryStream ms = new();
                using Utf8JsonWriter writer = new(ms);

                if (expectFail)
                {
                    Assert.ThrowsAny<JsonException>(() => writer.WriteRawValue(json, skipInputValidation));
                }
                else
                {
                    writer.WriteRawValue(json, skipInputValidation);
                    writer.Flush();

                    Assert.Equal(json, Encoding.UTF8.GetString(ms.ToArray()));
                }
            }
        }

        private static string GenerateJsonUsingDepth(int depth)
        {
            Assert.True(depth > 0 && depth <= 65, "Test depth out of range");

            StringBuilder sb = new();
            sb.Append("{");

            for (int i = 0; i < depth - 1; i++)
            {
                sb.Append(@"""prop"":{");
            }

            for (int i = 0; i < depth - 1; i++)
            {
                sb.Append("}");
            }

            sb.Append("}");

            return sb.ToString();
        }

        /// <summary>
        /// This test is constrained to run on Windows and MacOSX because it causes
        /// problems on Linux due to the way deferred memory allocation works. On Linux, the allocation can
        /// succeed even if there is not enough memory but then the test may get killed by the OOM killer at the
        /// time the memory is accessed which triggers the full memory allocation.
        /// Also see <see cref="WriteLargeJsonToStreamWithoutFlushing"/>
        /// </summary>
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        [ConditionalFact(nameof(IsX64))]
        [OuterLoop]
        public void WriteRawLargeJsonToStreamWithoutFlushing()
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
                writer.WriteRawValue(WrapInQuotes(text1.EncodedUtf8Bytes));
                Assert.Equal(7_503, writer.BytesPending);

                for (int i = 0; i < 30_000; i++)
                {
                    writer.WriteRawValue(WrapInQuotes(text2.EncodedUtf8Bytes));
                }
                Assert.Equal(150_097_503, writer.BytesPending);

                for (int i = 0; i < 13; i++)
                {
                    writer.WriteRawValue(WrapInQuotes(text3.EncodedUtf8Bytes));
                }
                Assert.Equal(2_100_097_542, writer.BytesPending);

                // Next write forces a grow beyond max array length

                Assert.Throws<OutOfMemoryException>(() => writer.WriteRawValue(WrapInQuotes(text3.EncodedUtf8Bytes)));

                Assert.Equal(2_100_097_542, writer.BytesPending);

                var text4 = JsonEncodedText.Encode(largeArray.AsSpan(0, 1));
                for (int i = 0; i < 10_000_000; i++)
                {
                    writer.WriteRawValue(WrapInQuotes(text4.EncodedUtf8Bytes));
                }

                Assert.Equal(2_100_097_542 + (4 * 10_000_000), writer.BytesPending);
            }
        }

        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        [ConditionalTheory(nameof(IsX64))]
        [OuterLoop]
        [InlineData(JsonTokenType.String)]
        [InlineData(JsonTokenType.StartArray)]
        [InlineData(JsonTokenType.StartObject)]
        public static void WriteRawMaxUtf16InputLength(JsonTokenType tokenType)
        {
            // Max raw payload length supported by the writer.
            int maxLength = int.MaxValue / 3;

            StringBuilder sb = new();
            sb.Append('"');

            for (int i = 1; i < maxLength - 1; i++)
            {
                sb.Append('a');
            }

            sb.Append('"');

            string payload = sb.ToString();

            RunTest(OverloadParamType.ROSChar);
            RunTest(OverloadParamType.String);
            RunTest(OverloadParamType.ByteArray);

            void RunTest(OverloadParamType paramType)
            {
                using MemoryStream ms = new();
                using Utf8JsonWriter writer = new(ms);

                switch (tokenType)
                {
                    case JsonTokenType.String:
                        WriteRawValueWithSetting(writer, payload, paramType);
                        writer.Flush();
                        Assert.Equal(payload.Length, writer.BytesCommitted);
                        break;
                    case JsonTokenType.StartArray:
                        writer.WriteStartArray();
                        WriteRawValueWithSetting(writer, payload, paramType);
                        WriteRawValueWithSetting(writer, payload, paramType);
                        writer.WriteEndArray();
                        writer.Flush();
                        // Start/EndArray + comma, 2 array elements
                        Assert.Equal(3 + (payload.Length * 2), writer.BytesCommitted);
                        break;
                    case JsonTokenType.StartObject:
                        writer.WriteStartObject();
                        writer.WritePropertyName("1");
                        WriteRawValueWithSetting(writer, payload, paramType);
                        writer.WritePropertyName("2");
                        WriteRawValueWithSetting(writer, payload, paramType);
                        writer.WriteEndObject();
                        writer.Flush();
                        // Start/EndToken + comma, 2 property names, 2 property values
                        Assert.Equal(3 + (4 * 2) + (payload.Length * 2), writer.BytesCommitted);
                        break;
                    default:
                        Assert.True(false, "Unexpected test configuration");
                        break;
                }
            }
        }

        private enum OverloadParamType
        {
            ROSChar,
            String,
            ByteArray
        }

        private static void WriteRawValueWithSetting(Utf8JsonWriter writer, string payload, OverloadParamType param)
        {
            switch (param)
            {
                case OverloadParamType.ROSChar:
                    writer.WriteRawValue(payload.AsSpan());
                    break;
                case OverloadParamType.String:
                    writer.WriteRawValue(payload);
                    break;
                case OverloadParamType.ByteArray:
                    byte[] payloadAsBytes = Encoding.UTF8.GetBytes(payload);
                    writer.WriteRawValue(payloadAsBytes);
                    break;
            }
        }

        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        [ConditionalTheory(nameof(IsX64))]
        [InlineData((int.MaxValue / 3) + 1)]
        [InlineData(int.MaxValue / 3 + 2)]
        [OuterLoop]
        public static void WriteRawUtf16LengthGreaterThanMax(int len)
        {
            StringBuilder sb = new();
            sb.Append('"');

            for (int i = 1; i < len - 1; i++)
            {
                sb.Append('a');
            }

            sb.Append('"');

            string payload = sb.ToString();

            using MemoryStream ms = new();
            using Utf8JsonWriter writer = new(ms);

            // UTF-16 overloads not compatible with this length.
            Assert.Throws<ArgumentException>(() => WriteRawValueWithSetting(writer, payload, OverloadParamType.ROSChar));
            Assert.Throws<ArgumentException>(() => WriteRawValueWithSetting(writer, payload, OverloadParamType.String));

            // UTF-8 overload is okay.
            WriteRawValueWithSetting(writer, payload, OverloadParamType.ByteArray);
            writer.Flush();

            Assert.Equal(payload.Length, Encoding.UTF8.GetString(ms.ToArray()).Length);
        }

        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        [ConditionalFact(nameof(IsX64))]
        [OuterLoop]
        public static void WriteRawTranscodeFromUtf16ToUtf8TooLong()
        {
            // Max raw payload length supported by the writer.
            int maxLength = int.MaxValue / 3;

            StringBuilder sb = new();
            sb.Append('"');

            for (int i = 1; i < maxLength - 1; i++)
            {
                sb.Append('的'); // Non-UTF-8 character than will expand during transcoding
            }

            sb.Append('"');

            string payload = sb.ToString();

            RunTest(OverloadParamType.ROSChar);
            RunTest(OverloadParamType.String);
            RunTest(OverloadParamType.ByteArray);

            void RunTest(OverloadParamType paramType)
            {
                using MemoryStream ms = new();
                using Utf8JsonWriter writer = new(ms);

                try
                {
                    WriteRawValueWithSetting(writer, payload, paramType);
                    writer.Flush();

                    // All characters in the payload will be expanded during transcoding, except for the quotes.
                    int expectedLength = ((payload.Length - 2) * 3) + 2;
                    Assert.Equal(expectedLength, writer.BytesCommitted);
                }
                catch (OutOfMemoryException) { } // OutOfMemoryException is okay since the transcoding output is probably too large.
            }
        }
    }
}

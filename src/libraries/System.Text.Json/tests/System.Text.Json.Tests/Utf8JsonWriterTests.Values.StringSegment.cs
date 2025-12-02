// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.



using System.Buffers;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Xunit;

namespace System.Text.Json.Tests
{
    public partial class Utf8JsonWriterTests
    {
        public static IEnumerable<JsonWriterOptions> BasicStringJsonOptions =>
            from indented in new[] { true, false }
            from encoding in new[] { JavaScriptEncoder.Default, JavaScriptEncoder.UnsafeRelaxedJsonEscaping, JavaScriptEncoder.Create() }
            select new JsonWriterOptions
            {
                Indented = indented,
                Encoder = encoding
            };

        public static IEnumerable<object[]> BasicStringJsonOptions_TestData =>
            from option in BasicStringJsonOptions
            select new object[] { option };

        public static IEnumerable<char[]> InvalidUtf16Data()
        {
            char[][] input = [
                // Unpaired low surrogate
                ['\uDC00'],

                // Unpaired high surrogate
                ['\uD800'],
                ['\uD800', '\uD800'],

                // Two unpaired low surrogates
                ['a', '\uDC00', '\uDC00'],
            ];

            // Separate each case with a character
            yield return input.SelectMany(arr => arr.Concat(['j'])).ToArray();

            // Test without separation
            yield return input.SelectMany(arr => arr).ToArray();
        }

        public static IEnumerable<object[]> InvalidUtf16DataWithOptions_TestData =>
            from data in InvalidUtf16Data()
            from option in BasicStringJsonOptions
            select new object[] { data, option };

        [Theory]
        [MemberData(nameof(InvalidUtf16DataWithOptions_TestData))]
        public static void WriteStringValueSegment_Utf16_SplitCodePointsReplacement(char[] inputArr, JsonWriterOptions options)
        {
            var expectedChars = new char[inputArr.Length * MaxExpansionFactorWhileEscaping];

            options.Encoder.Encode(inputArr, expectedChars, out int charsConsumed, out int charsWritten);
            Assert.Equal(inputArr.Length, charsConsumed);

            SplitStringDataHelper(inputArr, options, $@"""{new string(expectedChars, 0, charsWritten)}""", StringValueEncodingType.Utf16);
        }

        public static IEnumerable<byte[]> InvalidUtf8Data()
        {
            byte[][] input = [
                // Continuation without start
                [0b10_111111],

                // 2-byte sequence containing < 2 bytes
                [0b110_11111],

                // 2-byte overlong
                [0b110_00000, 0b10_111111],

                // 3-byte sequence containing < 3 bytes
                [0b1110_1111],
                [0b1110_1111, 0b10_111111],
                
                // 3-byte overlong
                [0b1110_0000, 0b10_000000, 0b10_000000],

                // 4-byte sequence containing < 4 bytes
                [0b11110_100],
                [0b11110_100, 0b10_001111],
                [0b11110_100, 0b10_001111, 0b10_111111],
                
                // 4-byte overlong
                [0b11110_000, 0b10_000000, 0b10_000000, 0b10_000000],

                // Greater than Unicode max value
                [0b11110_111, 0b10_000000],
                [0b11110_100, 0b10_100000, 0b10_000000],
            ];

            // Separate each case with a character
            yield return input.SelectMany(arr => arr.Concat([(byte)'j'])).ToArray();

            // Test without separation
            yield return input.SelectMany(arr => arr).ToArray();
        }

        public static IEnumerable<object[]> InvalidUtf8DataWithOptions_TestData =>
            from data in InvalidUtf8Data()
            from option in BasicStringJsonOptions
            select new object[] { data, option };

        [Theory]
        [MemberData(nameof(InvalidUtf8DataWithOptions_TestData))]
        public static void WriteStringValueSegment_Utf8_SplitCodePointsReplacement(byte[] inputArr, JsonWriterOptions options)
        {
            var expectedBytes = new byte[inputArr.Length * MaxExpansionFactorWhileEscaping];

            options.Encoder.EncodeUtf8(inputArr, expectedBytes, out int bytesConsumed, out int bytesWritten);
            Assert.Equal(inputArr.Length, bytesConsumed);

            string expectedString = $@"""{Encoding.UTF8.GetString(expectedBytes, 0, bytesWritten)}""";

            SplitStringDataHelper(inputArr, options, expectedString, StringValueEncodingType.Utf8);
        }

        private static void SplitStringDataHelper<T>(
            T[] inputArr,
            JsonWriterOptions options,
            string expected,
            StringValueEncodingType encoding)
            where T : struct
        {
            SplitStringDataHelper<T>(inputArr, options, output => JsonTestHelper.AssertContents(expected, output), encoding);
        }

        private static void SplitStringDataHelper<T>(
            T[] inputArr,
            JsonWriterOptions options,
            Action<ArrayBufferWriter<byte>> assert,
            StringValueEncodingType encoding)
            where T : struct
        {
            SplitStringDataHelper<T>(inputArr.AsSpan(), options, assert, encoding);
        }

        private static void SplitStringDataHelper<T>(
            ReadOnlySpan<T> inputArr,
            JsonWriterOptions options,
            Action<ArrayBufferWriter<byte>> assert,
            StringValueEncodingType encoding)
            where T : struct
        {
            ReadOnlySpan<T> input = inputArr;

            // Sanity check with non-segmented API
            {
                var output = new ArrayBufferWriter<byte>(1024);

                using (var writer = new Utf8JsonWriter(output, options))
                {
                    WriteStringValueHelper(writer, input, encoding);
                    writer.Flush();
                }

                assert(output);
            }

            for (int splitIndex = 0; splitIndex <= input.Length; splitIndex++)
            {
                var output = new ArrayBufferWriter<byte>(1024);

                using (var writer = new Utf8JsonWriter(output, options))
                {
                    WriteStringValueSegmentsHelper(writer, input.Slice(0, splitIndex), input.Slice(splitIndex), encoding);
                    writer.Flush();
                }

                assert(output);
            }

            for (int splitIndex = 0; splitIndex <= input.Length; splitIndex++)
            {
                for (int splitIndex2 = splitIndex; splitIndex2 <= input.Length; splitIndex2++)
                {
                    var output = new ArrayBufferWriter<byte>(1024);

                    using (var writer = new Utf8JsonWriter(output, options))
                    {
                        WriteStringValueSegmentsHelper(writer, input.Slice(0, splitIndex), input.Slice(splitIndex, splitIndex2 - splitIndex), input.Slice(splitIndex2), encoding);
                        writer.Flush();
                    }

                    assert(output);
                }
            }
        }

        [Theory]
        [MemberData(nameof(BasicStringJsonOptions_TestData))]
        public static void WriteStringValueSegment_Utf16_Basic(JsonWriterOptions options)
        {
            WriteStringValueSegment_BasicHelper(
                "Hello".AsSpan(),
                " Wor".AsSpan(),
                "ld!".AsSpan(),
                options.Encoder.Encode("Hello"),
                options.Encoder.Encode("Hello Wor"),
                options.Encoder.Encode("Hello World!"),
                options,
                StringValueEncodingType.Utf16);
        }

        [Theory]
        [MemberData(nameof(BasicStringJsonOptions_TestData))]
        public static void WriteStringValueSegment_Utf16_BasicSplit(JsonWriterOptions options)
        {
            WriteStringValueSegment_BasicHelper(
                "\uD800 <- Invalid Partial -> \uD800".AsSpan(),
                "\uDC00 <- Partial".AsSpan(),
                " Invalid -> \uD800".AsSpan(),
                options.Encoder.Encode("\uD800 <- Invalid Partial -> \uD800"),
                options.Encoder.Encode("\uD800 <- Invalid Partial -> \uD800\uDC00 <- Partial"),
                options.Encoder.Encode("\uD800 <- Invalid Partial -> \uD800\uDC00 <- Partial Invalid -> \uD800"),
                options,
                StringValueEncodingType.Utf16);
        }

        [Theory]
        [MemberData(nameof(BasicStringJsonOptions_TestData))]
        public static void WriteStringValueSegment_Utf8_Basic(JsonWriterOptions options)
        {
            WriteStringValueSegment_BasicHelper(
                "Hello"u8,
                " Wor"u8,
                "ld!"u8,
                options.Encoder.Encode("Hello"),
                options.Encoder.Encode("Hello Wor"),
                options.Encoder.Encode("Hello World!"),
                options,
                StringValueEncodingType.Utf8);
        }

        [Theory]
        [MemberData(nameof(BasicStringJsonOptions_TestData))]
        public static void WriteStringValueSegment_Utf8_BasicSplit(JsonWriterOptions options)
        {
            byte[] segment1 = [0b10_000000, .. " <- Invalid Partial -> "u8, 0b110_11111];
            byte[] segment2 = [0b10_111111, .. " <- Partial"u8];
            byte[] segment3 = [.. " Invalid -> "u8, 0b110_11111];
            WriteStringValueSegment_BasicHelper(
                segment1,
                segment2,
                segment3,
                // Since we're using string (base-16) encode for convenience, we just use an invalid utf-16 character
                options.Encoder.Encode("\udc00 <- Invalid Partial -> \udc00"),
                options.Encoder.Encode("\udc00 <- Invalid Partial -> \u07ff <- Partial"),
                options.Encoder.Encode("\udc00 <- Invalid Partial -> \u07ff <- Partial Invalid -> \udc00"),
                options,
                StringValueEncodingType.Utf8);
        }

        [Fact]
        public static void WriteStringValueSegment_Base64_Basic()
        {
            {
                WriteStringValueSegment_BasicHelper(
                    "Hello"u8,
                    " Worl"u8,
                    "d!"u8,
                    "SGVsbG8=",
                    "SGVsbG8gV29ybA==",
                    "SGVsbG8gV29ybGQh",
                    new JsonWriterOptions { Indented = false },
                    StringValueEncodingType.Base64);
            }

            {
                WriteStringValueSegment_BasicHelper(
                    "Hello"u8,
                    " Worl"u8,
                    "d!"u8,
                    "SGVsbG8=",
                    "SGVsbG8gV29ybA==",
                    "SGVsbG8gV29ybGQh",
                    new JsonWriterOptions { Indented = true },
                    StringValueEncodingType.Base64);
            }
        }

        private static void WriteStringValueSegment_BasicHelper<T>(
            ReadOnlySpan<T> segment1,
            ReadOnlySpan<T> segment2,
            ReadOnlySpan<T> segment3,
            string expected1,
            string expected2,
            string expected3,
            JsonWriterOptions options,
            StringValueEncodingType encoding)
            where T : struct
        {
            JavaScriptEncoder encoder = options.Encoder ?? JavaScriptEncoder.Default;
            string indent = options.Indented ? new string(options.IndentCharacter, options.IndentSize) : "";
            string n = options.Indented ? options.NewLine : "";
            string ni = n + indent;
            string nii = ni + indent;
            string s = options.Indented ? " " : "";
            string e1 = '"' + expected1 + '"';
            string e2 = '"' + expected2 + '"';
            string e3 = '"' + expected3 + '"';
            string foo = '"' + encoder.Encode("foo") + '"';
            string bar = '"' + encoder.Encode("bar") + '"';
            string baz = '"' + encoder.Encode("baz") + '"';
            string inner = '"' + encoder.Encode("inner") + '"';

            // JSON string
            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, encoding);
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(e1, output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, segment2, encoding);
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(e2, output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, segment2, segment3, encoding);
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(e3, output);
            }

            // JSON array
            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartArray();
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, encoding);
                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $"[{n}{indent}{e1}{n}]",
                    output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartArray();
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, segment2, encoding);
                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $"[{ni}{e2}{n}]",
                    output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartArray();
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, segment2, segment3, encoding);
                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $"[{ni}{e3}{n}]",
                    output);
            }

            // Middle item in array
            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteBooleanValue(true);
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, encoding);
                jsonUtf8.WriteBooleanValue(false);
                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $"[{ni}true,{ni}{e1},{ni}false{n}]",
                    output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteBooleanValue(true);
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, segment2, encoding);
                jsonUtf8.WriteBooleanValue(false);
                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $"[{ni}true,{ni}{e2},{ni}false{n}]",
                    output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteBooleanValue(true);
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, segment2, segment3, encoding);
                jsonUtf8.WriteBooleanValue(false);
                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $"[{ni}true,{ni}{e3},{ni}false{n}]",
                    output);
            }

            // Nested array
            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteBooleanValue(true);
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, encoding);
                jsonUtf8.WriteBooleanValue(false);
                jsonUtf8.WriteEndArray();
                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $"[{ni}[{nii}true,{nii}{e1},{nii}false{ni}]{n}]",
                    output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteBooleanValue(true);
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, segment2, encoding);
                jsonUtf8.WriteBooleanValue(false);
                jsonUtf8.WriteEndArray();
                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $"[{ni}[{nii}true,{nii}{e2},{nii}false{ni}]{n}]",
                    output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteBooleanValue(true);
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, segment2, segment3, encoding);
                jsonUtf8.WriteBooleanValue(false);
                jsonUtf8.WriteEndArray();
                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $"[{ni}[{nii}true,{nii}{e3},{nii}false{ni}]{n}]",
                    output);
            }

            // JSON object
            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartObject();
                jsonUtf8.WritePropertyName("foo");
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, encoding);
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $@"{{{ni}{foo}:{s}{e1}{n}}}",
                    output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartObject();
                jsonUtf8.WritePropertyName("foo");
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, segment2, encoding);
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $@"{{{ni}{foo}:{s}{e2}{n}}}",
                    output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartObject();
                jsonUtf8.WritePropertyName("foo");
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, segment2, segment3, encoding);
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $@"{{{ni}{foo}:{s}{e3}{n}}}",
                    output);
            }

            // Middle item in object
            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteBoolean("bar", true);
                jsonUtf8.WritePropertyName("foo");
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, encoding);
                jsonUtf8.WriteBoolean("baz", false);
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $@"{{{ni}{bar}:{s}true,{ni}{foo}:{s}{e1},{ni}{baz}:{s}false{n}}}",
                    output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteBoolean("bar", true);
                jsonUtf8.WritePropertyName("foo");
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, segment2, encoding);
                jsonUtf8.WriteBoolean("baz", false);
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $@"{{{ni}{bar}:{s}true,{ni}{foo}:{s}{e2},{ni}{baz}:{s}false{n}}}",
                    output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteBoolean("bar", true);
                jsonUtf8.WritePropertyName("foo");
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, segment2, segment3, encoding);
                jsonUtf8.WriteBoolean("baz", false);
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $@"{{{ni}{bar}:{s}true,{ni}{foo}:{s}{e3},{ni}{baz}:{s}false{n}}}",
                    output);
            }

            // Nested object
            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteStartObject("inner");
                jsonUtf8.WriteBoolean("bar", true);
                jsonUtf8.WritePropertyName("foo");
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, encoding);
                jsonUtf8.WriteBoolean("baz", false);
                jsonUtf8.WriteEndObject();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $@"{{{ni}{inner}:{s}{{{nii}{bar}:{s}true,{nii}{foo}:{s}{e1},{nii}{baz}:{s}false{ni}}}{n}}}",
                    output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteStartObject("inner");
                jsonUtf8.WriteBoolean("bar", true);
                jsonUtf8.WritePropertyName("foo");
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, segment2, encoding);
                jsonUtf8.WriteBoolean("baz", false);
                jsonUtf8.WriteEndObject();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $@"{{{ni}{inner}:{s}{{{nii}{bar}:{s}true,{nii}{foo}:{s}{e2},{nii}{baz}:{s}false{ni}}}{n}}}",
                    output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, options);
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteStartObject("inner");
                jsonUtf8.WriteBoolean("bar", true);
                jsonUtf8.WritePropertyName("foo");
                WriteStringValueSegmentsHelper(jsonUtf8, segment1, segment2, segment3, encoding);
                jsonUtf8.WriteBoolean("baz", false);
                jsonUtf8.WriteEndObject();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                JsonTestHelper.AssertContents(
                    $@"{{{ni}{inner}:{s}{{{nii}{bar}:{s}true,{nii}{foo}:{s}{e3},{nii}{baz}:{s}false{ni}}}{n}}}",
                    output);
            }
        }

        [Fact]
        public static void WriteStringValueSegment_Utf16_BadSurrogatePairs()
        {
            const string result = "\\uFFFD\\uD83D\\uDE00\\uFFFD";

            ReadOnlySpan<char> surrogates = ['\uD83D', '\uD83D', '\uDE00', '\uDE00'];

            var output = new ArrayBufferWriter<byte>();
            using var jsonUtf8 = new Utf8JsonWriter(output);
            jsonUtf8.WriteStartObject();
            jsonUtf8.WritePropertyName("full");
            // complete string -> expect 0xFFFD 0xD83D 0xDE00 0xFFFD 
            jsonUtf8.WriteStringValue(surrogates);
            jsonUtf8.WritePropertyName("segmented");
            // only high surrogate -> expect cached
            jsonUtf8.WriteStringValueSegment(surrogates.Slice(0, 1), isFinalSegment: false);
            // only high surrogate -> expect 0xFFFD
            jsonUtf8.WriteStringValueSegment(surrogates.Slice(0, 1), isFinalSegment: false);
            // only low surrogate -> expect 0xD83D 0xDE00
            jsonUtf8.WriteStringValueSegment(surrogates.Slice(2, 1), isFinalSegment: false);
            // only low surrogate -> expect 0xFFFD
            jsonUtf8.WriteStringValueSegment(surrogates.Slice(2, 1), isFinalSegment: true);
            jsonUtf8.WriteEndObject();
            jsonUtf8.Flush();

            JsonTestHelper.AssertContents($"{{\"full\":\"{result}\",\"segmented\":\"{result}\"}}", output);
        }

        [Fact]
        public static void WriteStringValueSegment_Utf16_SplitInSurrogatePair()
        {
            const string result = "\\uD83D\\uDE00\\uD83D\\uDE00\\uD83D\\uDE00";

            Span<char> surrogates = stackalloc char[] { '\uD83D', '\uDE00', '\uD83D', '\uDE00', '\uD83D', '\uDE00' };

            var output = new ArrayBufferWriter<byte>();
            using var jsonUtf8 = new Utf8JsonWriter(output);
            jsonUtf8.WriteStartObject();
            jsonUtf8.WritePropertyName("full");
            // complete string -> expect 0xD83D 0xDE00 0xD83D 0xDE00 0xD83D 0xDE00
            jsonUtf8.WriteStringValue(surrogates);
            jsonUtf8.WritePropertyName("segmented");
            // only high surrogate -> expect cached
            jsonUtf8.WriteStringValueSegment(surrogates.Slice(0, 2), isFinalSegment: false);
            // only low surrogate -> expect 0xD83D 0xDE00
            jsonUtf8.WriteStringValueSegment(surrogates.Slice(0, 1), isFinalSegment: false);
            // low surrogate followed by another high surrogate -> expect 0xD83D 0xDE00 + cached
            jsonUtf8.WriteStringValueSegment(surrogates.Slice(1, 2), isFinalSegment: false);
            // only low surrogate -> expect 0xD83D 0xDE00
            jsonUtf8.WriteStringValueSegment(surrogates.Slice(1, 1), isFinalSegment: true);
            jsonUtf8.WriteEndObject();
            jsonUtf8.Flush();

            JsonTestHelper.AssertContents($"{{\"full\":\"{result}\",\"segmented\":\"{result}\"}}", output);
        }

        [Fact]
        public static void WriteStringValueSegment_Utf8_Split8CodePointsBasic()
        {
            const string result = "\\uD83D\\uDE00";

            Span<byte> utf8Bytes = Encoding.UTF8.GetBytes("\uD83D\uDE00");

            var output = new ArrayBufferWriter<byte>();
            using var jsonUtf8 = new Utf8JsonWriter(output);
            jsonUtf8.WriteStartObject();
            jsonUtf8.WritePropertyName("full");
            // complete string -> expect 0xD83D 0xDE00
            jsonUtf8.WriteStringValue(utf8Bytes);
            jsonUtf8.WritePropertyName("segmented");
            // incomplete UTf-8 sequence -> expect cached
            jsonUtf8.WriteStringValueSegment(utf8Bytes.Slice(0, 1), isFinalSegment: false);
            // incomplete UTf-8 sequence -> expect cached
            jsonUtf8.WriteStringValueSegment(utf8Bytes.Slice(1, 1), isFinalSegment: false);
            // remainder of UTF-8 sequence -> expect 0xD83D 0xDE00
            jsonUtf8.WriteStringValueSegment(utf8Bytes.Slice(2, 2), isFinalSegment: true);
            jsonUtf8.WriteEndObject();
            jsonUtf8.Flush();

            JsonTestHelper.AssertContents($"{{\"full\":\"{result}\",\"segmented\":\"{result}\"}}", output);
        }

        [Theory]
        [InlineData("", "")]
        [InlineData("0 padding", "MCBwYWRkaW5n")]
        [InlineData("_1 padding", "XzEgcGFkZGluZw==")]
        [InlineData("__2 padding", "X18yIHBhZGRpbmc=")]
        public static void WriteStringValueSegment_Base64_SplitDataBasic(string input, string expected)
        {
            byte[] bytes = input.Select(c => (byte)c).ToArray();
            SplitStringDataHelper(bytes, new JsonWriterOptions { Indented = true }, "\"" + expected + "\"", StringValueEncodingType.Base64);
        }

        [Fact]
        public static void WriteStringValueSegment_Utf8_ClearedPartial()
        {
            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);

                jsonUtf8.WriteStartArray();

                jsonUtf8.WriteStringValueSegment([0b110_11111], false);
                jsonUtf8.WriteStringValueSegment([0b10_111111], true);

                jsonUtf8.WriteStringValueSegment([0b10_111111], true);

                jsonUtf8.WriteStringValueSegment([0b110_11111], false);
                jsonUtf8.WriteStringValueSegment([0b10_111111], false);
                jsonUtf8.WriteStringValueSegment([0b10_111111], true);

                jsonUtf8.WriteEndArray();

                jsonUtf8.Flush();

                // First code point is written (escaped) and the second is replaced.
                JsonTestHelper.AssertContents("""["\u07ff","\uFFFD","\u07ff\uFFFD"]""", output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });

                jsonUtf8.WriteStartArray();

                jsonUtf8.WriteStringValueSegment([0b110_11111], false);
                jsonUtf8.WriteStringValueSegment([0b10_111111], true);

                jsonUtf8.WriteStringValueSegment([0b10_111111], true);

                jsonUtf8.WriteStringValueSegment([0b110_11111], false);
                jsonUtf8.WriteStringValueSegment([0b10_111111], false);
                jsonUtf8.WriteStringValueSegment([0b10_111111], true);

                jsonUtf8.WriteEndArray();

                jsonUtf8.Flush();

                // First code point is written (unescaped) and the second is replaced.
                JsonTestHelper.AssertContents($"""["{'\u07ff'}","\uFFFD","{'\u07ff'}\uFFFD"]""", output);
            }
        }

        [Fact]
        public static void WriteStringValueSegment_Utf16_ClearedPartial()
        {
            var output = new ArrayBufferWriter<byte>();

            {
                using var jsonUtf8 = new Utf8JsonWriter(output);

                jsonUtf8.WriteStartArray();

                WriteStringValueSegmentsHelper(jsonUtf8, ['\uD800'], ['\uDC00'], StringValueEncodingType.Utf16);
                WriteStringValueSegmentsHelper(jsonUtf8, ['\uDC00'], StringValueEncodingType.Utf16);
                WriteStringValueSegmentsHelper(jsonUtf8, ['\uD800'], ['\uDC00'], ['\uDC00'], StringValueEncodingType.Utf16);

                jsonUtf8.WriteEndArray();

                jsonUtf8.Flush();

                // First code point is written and the second is replaced.
                JsonTestHelper.AssertContents("""["\uD800\uDC00","\uFFFD","\uD800\uDC00\uFFFD"]""", output);
            }
        }

        [Fact]
        public static void WriteStringValueSegment_Base64_ClearedPartial()
        {
            var output = new ArrayBufferWriter<byte>();

            {
                var expected = new StringBuilder();
                using var jsonUtf8 = new Utf8JsonWriter(output);

                jsonUtf8.WriteStartArray();
                expected.Append('[');

                // Helpers to build up the expected string
                var buffer = new List<byte>();
                ReadOnlySpan<byte> AddPartial(ReadOnlySpan<byte> arr)
                {
                    foreach (byte b in arr) buffer.Add(b);

                    return arr;
                }

                ReadOnlySpan<byte> AddFinal(ReadOnlySpan<byte> arr)
                {
                    foreach (byte b in arr) buffer.Add(b);

                    expected.Append('"');
                    expected.Append(Convert.ToBase64String(buffer.ToArray()));
                    expected.Append('"');
                    expected.Append(',');
                    buffer.Clear();

                    return arr;
                }

                // 1 segment
                WriteStringValueSegmentsHelper(jsonUtf8, AddFinal([]), StringValueEncodingType.Base64);
                WriteStringValueSegmentsHelper(jsonUtf8, AddFinal([0]), StringValueEncodingType.Base64);
                WriteStringValueSegmentsHelper(jsonUtf8, AddFinal([0, 1]), StringValueEncodingType.Base64);
                WriteStringValueSegmentsHelper(jsonUtf8, AddFinal([0, 1, 2]), StringValueEncodingType.Base64);

                // 2 segments
                for (int i = 0; i <= 3; i++)
                {
                    for (int j = 0; j <= 3; j++)
                    {
                        WriteStringValueSegmentsHelper(
                            jsonUtf8,
                            AddPartial([..Enumerable.Range(0, i).Select(x => (byte)x)]),
                            AddFinal([..Enumerable.Range(i, j).Select(x => (byte)x)]),
                            StringValueEncodingType.Base64);
                    }
                }

                // 3 segments
                for (int i = 0; i <= 3; i++)
                {
                    for (int j = 0; j <= 3; j++)
                    {
                        for (int k = 0; k <= 3; k++)
                        {
                            WriteStringValueSegmentsHelper(
                                jsonUtf8,
                                AddPartial([..Enumerable.Range(0, i).Select(x => (byte)x)]),
                                AddPartial([..Enumerable.Range(i, j).Select(x => (byte)x)]),
                                AddFinal([.. Enumerable.Range(i + j, k).Select(x => (byte)x)]),
                                StringValueEncodingType.Base64);
                        }
                    }
                }

                // Remove trailing comma
                expected.Remove(expected.Length - 1, 1);

                jsonUtf8.WriteEndArray();
                expected.Append(']');

                jsonUtf8.Flush();

                // First code point is written and the second is replaced.
                JsonTestHelper.AssertContents(expected.ToString(), output);
            }
        }

        [Fact]
        public static void WriteStringValueSegment_Flush()
        {
            var noEscape = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
            TestFlushImpl(['\uD800'], ['\uDC00'], new(), @"""\uD800\uDC00""", StringValueEncodingType.Utf16);
            TestFlushImpl<byte>([0b110_11111], [0b10_111111], new(), @"""\u07FF""", StringValueEncodingType.Utf8);
            TestFlushImpl<byte>([0b110_11111], [0b10_111111], new() { Encoder = noEscape }, "\"\u07FF\"", StringValueEncodingType.Utf8);
            TestFlushImpl<byte>([], [0, 0, 0], new(), "\"AAAA\"", StringValueEncodingType.Base64);
            TestFlushImpl<byte>([0], [0, 0], new(), "\"AAAA\"", StringValueEncodingType.Base64);
            TestFlushImpl<byte>([0, 0], [0], new(), "\"AAAA\"", StringValueEncodingType.Base64);

            void TestFlushImpl<T>(ReadOnlySpan<T> unit1, ReadOnlySpan<T> unit2, JsonWriterOptions options, string expected, StringValueEncodingType encoding)
                where T : struct
            {
                byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
                var output = new ArrayBufferWriter<byte>();
                using Utf8JsonWriter jsonUtf8 = new(output, options);

                WriteStringValueSegmentHelper(jsonUtf8, unit1, false, encoding);

                Assert.Equal(0, output.WrittenCount);
                Assert.Equal(0, jsonUtf8.BytesCommitted);
                Assert.Equal(1, jsonUtf8.BytesPending);

                jsonUtf8.Flush();
                Assert.Equal(1, output.WrittenCount);
                Assert.Equal(1, jsonUtf8.BytesCommitted);
                Assert.Equal(0, jsonUtf8.BytesPending);

                WriteStringValueSegmentHelper(jsonUtf8, unit2, true, encoding);

                Assert.Equal(1, output.WrittenCount);
                Assert.Equal(1, jsonUtf8.BytesCommitted);
                Assert.Equal(expectedBytes.Length - 1, jsonUtf8.BytesPending);

                jsonUtf8.Flush();
                Assert.Equal(expectedBytes.Length, output.WrittenCount);
                Assert.Equal(expectedBytes.Length, jsonUtf8.BytesCommitted);
                Assert.Equal(0, jsonUtf8.BytesPending);

                JsonTestHelper.AssertContents(expected, output);
            }
        }

        [Fact]
        public static void WriteStringValueSegment_Utf16_Reset()
        {
            var output = new ArrayBufferWriter<byte>();
            using var jsonUtf8 = new Utf8JsonWriter(output);

            jsonUtf8.WriteStringValueSegment("\uD800".AsSpan(), false);
            jsonUtf8.Flush();

            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(1, jsonUtf8.BytesCommitted);

            jsonUtf8.Reset();

            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(0, jsonUtf8.BytesCommitted);

            jsonUtf8.WriteStringValueSegment("\uDC00".AsSpan(), true);

            string expected = @"""\uFFFD""";
            Assert.Equal(expected.Length, jsonUtf8.BytesPending);
            Assert.Equal(0, jsonUtf8.BytesCommitted);

            jsonUtf8.Flush();

            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(expected.Length, jsonUtf8.BytesCommitted);
            JsonTestHelper.AssertContents('"' + expected, output);
        }

        [Fact]
        public static void WriteStringValueSegment_Utf8_Reset()
        {
            var output = new ArrayBufferWriter<byte>();
            using var jsonUtf8 = new Utf8JsonWriter(output);

            jsonUtf8.WriteStringValueSegment([0b110_11111], false);
            jsonUtf8.Flush();

            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(1, jsonUtf8.BytesCommitted);

            jsonUtf8.Reset();

            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(0, jsonUtf8.BytesCommitted);

            jsonUtf8.WriteStringValueSegment([0b10_111111], true);

            string expected = @"""\uFFFD""";
            Assert.Equal(expected.Length, jsonUtf8.BytesPending);
            Assert.Equal(0, jsonUtf8.BytesCommitted);

            jsonUtf8.Flush();

            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(expected.Length, jsonUtf8.BytesCommitted);
            JsonTestHelper.AssertContents('"' + expected, output);
        }

        [Fact]
        public static void WriteStringValueSegment_Base64_Reset()
        {
            var output = new ArrayBufferWriter<byte>();
            using var jsonUtf8 = new Utf8JsonWriter(output);

            jsonUtf8.WriteBase64StringSegment([0], false);
            jsonUtf8.Flush();

            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(1, jsonUtf8.BytesCommitted);

            jsonUtf8.Reset();

            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(0, jsonUtf8.BytesCommitted);

            jsonUtf8.WriteBase64StringSegment([0, 0, 0], true);

            string expected = @"""AAAA""";
            Assert.Equal(expected.Length, jsonUtf8.BytesPending);
            Assert.Equal(0, jsonUtf8.BytesCommitted);

            jsonUtf8.Flush();

            Assert.Equal(0, jsonUtf8.BytesPending);
            Assert.Equal(expected.Length, jsonUtf8.BytesCommitted);
            JsonTestHelper.AssertContents('"' + expected, output);
        }

        [Fact]
        public static void WriteStringValueSegment_MixEncoding()
        {
            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);

                // High surrogate
                jsonUtf8.WriteStringValueSegment("\uD8D8".AsSpan(), false);

                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStringValueSegment([0b10_111111], true));
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);

                // High surrogate
                jsonUtf8.WriteStringValueSegment("\uD8D8".AsSpan(), false);

                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteBase64StringSegment([0], true));
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);

                // Start of a 3-byte sequence
                jsonUtf8.WriteStringValueSegment([0b1110_1111], false);

                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStringValueSegment("\u8080".AsSpan(), true));
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);

                // Start of a 3-byte sequence
                jsonUtf8.WriteStringValueSegment([0b1110_1111], false);

                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteBase64StringSegment([0], true));
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);

                // Partial Base64
                jsonUtf8.WriteBase64StringSegment([0], false);

                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStringValueSegment([0b10_111111], true));
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);

                // Partial Base64
                jsonUtf8.WriteBase64StringSegment([0], false);

                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStringValueSegment("\u8080".AsSpan(), true));
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteStringValueSegment([0b110_11111], false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);

                // Writing empty UTF-8 sequence will still keep the partial code point
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<byte>.Empty, false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);

                // Writing empty UTF-16 sequence will throw
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStringValueSegment(ReadOnlySpan<char>.Empty, false));
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteStringValueSegment([0b110_11111], false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);

                // Writing empty base64 sequence will still keep the partial code point
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<byte>.Empty, false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);

                // Writing empty UTF-8 sequence will throw
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteBase64StringSegment(ReadOnlySpan<byte>.Empty, false));
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteStringValueSegment(['\uD800'], false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);

                // Writing empty UTF-16 sequence will still keep the partial code point
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<char>.Empty, false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);

                // Writing empty UTF-8 sequence will throw
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStringValueSegment(ReadOnlySpan<byte>.Empty, false));
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteStringValueSegment(['\uD800'], false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);

                // Writing empty UTF-16 sequence will still keep the partial code point
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<char>.Empty, false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);

                // Writing empty base64 sequence will throw
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteBase64StringSegment(ReadOnlySpan<byte>.Empty, false));
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteBase64StringSegment([0], false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);

                // Writing empty base64 sequence will still keep the partial code point
                jsonUtf8.WriteBase64StringSegment(ReadOnlySpan<byte>.Empty, false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);

                // Writing empty UTF-8 sequence will throw
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStringValueSegment(ReadOnlySpan<byte>.Empty, false));
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteBase64StringSegment([0], false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);

                // Writing empty base64 sequence will still keep the partial code point
                jsonUtf8.WriteBase64StringSegment(ReadOnlySpan<byte>.Empty, false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);

                // Writing empty UTF-16 sequence will throw
                Assert.Throws<InvalidOperationException>(() => jsonUtf8.WriteStringValueSegment(ReadOnlySpan<char>.Empty, false));
            }
        }

        [Fact]
        public static void WriteStringValueSegment_Empty()
        {
            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<byte>.Empty, true);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"\"", output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<byte>.Empty, false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<byte>.Empty, false);
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<byte>.Empty, true);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"\"", output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<byte>.Empty, false);
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<byte>.Empty, false);
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<byte>.Empty, true);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"\"", output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<char>.Empty, true);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"\"", output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<char>.Empty, false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<char>.Empty, false);
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<char>.Empty, true);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"\"", output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<char>.Empty, false);
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<char>.Empty, false);
                jsonUtf8.WriteStringValueSegment(ReadOnlySpan<char>.Empty, true);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"\"", output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteBase64StringSegment([], true);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"\"", output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteBase64StringSegment([], false);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"", output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteBase64StringSegment([], false);
                jsonUtf8.WriteBase64StringSegment([], true);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"\"", output);
            }

            {
                var output = new ArrayBufferWriter<byte>();
                using var jsonUtf8 = new Utf8JsonWriter(output);
                jsonUtf8.WriteBase64StringSegment([], false);
                jsonUtf8.WriteBase64StringSegment([], false);
                jsonUtf8.WriteBase64StringSegment([], true);
                jsonUtf8.Flush();
                JsonTestHelper.AssertContents("\"\"", output);
            }
        }

        enum StringValueEncodingType
        {
            Utf8,
            Utf16,
            Base64,
        }

        private static void EnsureByteOrChar<T>([CallerMemberName]string caller = "<unknown>")
        {
            if (typeof(T) != typeof(byte) && typeof(T) != typeof(char))
            {
                if (typeof(T) == typeof(int))
                {
                    Assert.Fail($"Did you pass in int or int[] instead of byte or byte[]? Type {typeof(T)} is not supported by {caller}.");
                }

                Assert.Fail($"Type {typeof(T)} is not supported by {caller}.");
            }
        }

        private static void WriteStringValueHelper<T>(Utf8JsonWriter writer, ReadOnlySpan<T> value, StringValueEncodingType encoding)
            where T : struct
        {
            EnsureByteOrChar<T>();

            switch (encoding)
            {
                case StringValueEncodingType.Utf16:
                    writer.WriteStringValue(MemoryMarshal.Cast<T, char>(value));
                    break;
                case StringValueEncodingType.Utf8:
                    writer.WriteStringValue(MemoryMarshal.Cast<T, byte>(value));
                    break;
                case StringValueEncodingType.Base64:
                    writer.WriteBase64StringValue(MemoryMarshal.Cast<T, byte>(value));
                    break;
                default:
                    Assert.Fail($"Encoding {encoding} not valid.");
                    break;
            }
        }

        private static void WriteStringValueSegmentHelper<T>(Utf8JsonWriter writer, ReadOnlySpan<T> value, bool isFinal, StringValueEncodingType encoding)
            where T : struct
        {
            EnsureByteOrChar<T>();

            switch (encoding)
            {
                case StringValueEncodingType.Utf16:
                    writer.WriteStringValueSegment(MemoryMarshal.Cast<T, char>(value), isFinal);
                    break;
                case StringValueEncodingType.Utf8:
                    writer.WriteStringValueSegment(MemoryMarshal.Cast<T, byte>(value), isFinal);
                    break;
                case StringValueEncodingType.Base64:
                    writer.WriteBase64StringSegment(MemoryMarshal.Cast<T, byte>(value), isFinal);
                    break;
                default:
                    Assert.Fail($"Encoding {encoding} not valid.");
                    break;
            }
        }

        private static void WriteStringValueSegmentsHelper<T>(Utf8JsonWriter writer, ReadOnlySpan<T> value, StringValueEncodingType encoding)
            where T : struct
        {
            EnsureByteOrChar<T>();

            switch (encoding)
            {
                case StringValueEncodingType.Utf16:
                    writer.WriteStringValueSegment(MemoryMarshal.Cast<T, char>(value), true);
                    break;
                case StringValueEncodingType.Utf8:
                    writer.WriteStringValueSegment(MemoryMarshal.Cast<T, byte>(value), true);
                    break;
                case StringValueEncodingType.Base64:
                    writer.WriteBase64StringSegment(MemoryMarshal.Cast<T, byte>(value), true);
                    break;
                default:
                    Assert.Fail($"Encoding {encoding} not valid.");
                    break;
            }
        }

        // Switch this to use an enum discriminator input when base64 is supported
        private static void WriteStringValueSegmentsHelper<T>(Utf8JsonWriter writer, ReadOnlySpan<T> value1, ReadOnlySpan<T> value2, StringValueEncodingType encoding)
            where T : struct
        {
            EnsureByteOrChar<T>();

            switch (encoding)
            {
                case StringValueEncodingType.Utf16:
                    writer.WriteStringValueSegment(MemoryMarshal.Cast<T, char>(value1), false);
                    writer.WriteStringValueSegment(MemoryMarshal.Cast<T, char>(value2), true);
                    break;
                case StringValueEncodingType.Utf8:
                    writer.WriteStringValueSegment(MemoryMarshal.Cast<T, byte>(value1), false);
                    writer.WriteStringValueSegment(MemoryMarshal.Cast<T, byte>(value2), true);
                    break;
                case StringValueEncodingType.Base64:
                    writer.WriteBase64StringSegment(MemoryMarshal.Cast<T, byte>(value1), false);
                    writer.WriteBase64StringSegment(MemoryMarshal.Cast<T, byte>(value2), true);
                    break;
                default:
                    Assert.Fail($"Encoding {encoding} not valid.");
                    break;
            }
        }

        // Switch this to use an enum discriminator input when base64 is supported
        private static void WriteStringValueSegmentsHelper<T>(Utf8JsonWriter writer, ReadOnlySpan<T> value1, ReadOnlySpan<T> value2, ReadOnlySpan<T> value3, StringValueEncodingType encoding)
            where T : struct
        {
            EnsureByteOrChar<T>();

            switch (encoding)
            {
                case StringValueEncodingType.Utf16:
                    writer.WriteStringValueSegment(MemoryMarshal.Cast<T, char>(value1), false);
                    writer.WriteStringValueSegment(MemoryMarshal.Cast<T, char>(value2), false);
                    writer.WriteStringValueSegment(MemoryMarshal.Cast<T, char>(value3), true);
                    break;
                case StringValueEncodingType.Utf8:
                    writer.WriteStringValueSegment(MemoryMarshal.Cast<T, byte>(value1), false);
                    writer.WriteStringValueSegment(MemoryMarshal.Cast<T, byte>(value2), false);
                    writer.WriteStringValueSegment(MemoryMarshal.Cast<T, byte>(value3), true);
                    break;
                case StringValueEncodingType.Base64:
                    writer.WriteBase64StringSegment(MemoryMarshal.Cast<T, byte>(value1), false);
                    writer.WriteBase64StringSegment(MemoryMarshal.Cast<T, byte>(value2), false);
                    writer.WriteBase64StringSegment(MemoryMarshal.Cast<T, byte>(value3), true);
                    break;
                default:
                    Assert.Fail($"Encoding {encoding} not valid.");
                    break;
            }
        }

        private static void WriteStringValueSegmentsHelper(Utf8JsonWriter writer, string value, StringValueEncodingType encoding)
            => WriteStringValueSegmentsHelper(writer, value.AsSpan(), encoding);

        private static void WriteStringValueSegmentsHelper(Utf8JsonWriter writer, string value1, string value2, StringValueEncodingType encoding)
            => WriteStringValueSegmentsHelper(writer, value1.AsSpan(), value2.AsSpan(), encoding);

        private static void WriteStringValueSegmentsHelper(Utf8JsonWriter writer, string value1, string value2, string value3, StringValueEncodingType encoding)
            => WriteStringValueSegmentsHelper(writer, value1.AsSpan(), value2.AsSpan(), value3.AsSpan(), encoding);
    }
}

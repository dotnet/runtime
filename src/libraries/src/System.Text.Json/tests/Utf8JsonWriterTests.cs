// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System.IO;
using Newtonsoft.Json;
using System.Globalization;

namespace System.Text.Json.Tests
{
    public class Utf8JsonWriterTests
    {
        public static bool IsX64 { get; } = IntPtr.Size >= 8;

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void NullCtor(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            try
            {
                var jsonUtf8 = new Utf8JsonWriter(null);
                Assert.True(false, "Expected ArgumentNullException to be thrown when null IBufferWriter is passed in.");
            }
            catch (ArgumentNullException) { }

            try
            {
                var jsonUtf8 = new Utf8JsonWriter(null, state);
                Assert.True(false, "Expected ArgumentNullException to be thrown when null IBufferWriter is passed in.");
            }
            catch (ArgumentNullException) { }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void FlushEmpty(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });
            var output = new FixedSizedBufferWriter(0);
            try
            {
                var jsonUtf8 = new Utf8JsonWriter(output, state);
                jsonUtf8.Flush();
                WriterDidNotThrow(skipValidation, "Expected InvalidOperationException to be thrown when calling Flush on an empty JSON payload.");
            }
            catch (InvalidOperationException) { }

            output = new FixedSizedBufferWriter(10);
            try
            {
                var jsonUtf8 = new Utf8JsonWriter(output, state);
                jsonUtf8.WriteCommentValue("hi");
                jsonUtf8.Flush();
                WriterDidNotThrow(skipValidation, "Expected InvalidOperationException to be thrown when calling Flush on an empty JSON payload.");
            }
            catch (InvalidOperationException) { }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void FlushMultipleTimes(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });
            var output = new FixedSizedBufferWriter(10);

            var jsonUtf8 = new Utf8JsonWriter(output, state);
            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteEndObject();
            jsonUtf8.Flush();
            Assert.Equal(2, jsonUtf8.BytesCommitted);
            jsonUtf8.Flush();
            Assert.Equal(2, jsonUtf8.BytesCommitted);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InvalidBufferWriter(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new InvalidBufferWriter();

            var jsonUtf8 = new Utf8JsonWriter(output, state);

            try
            {
                jsonUtf8.WriteNumberValue((ulong)12345678901);
                Assert.True(false, "Expected ArgumentException to be thrown when IBufferWriter doesn't have enough space.");
            }
            catch (ArgumentException) { }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void FixedSizeBufferWriter_Guid(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new FixedSizedBufferWriter(37);

            var jsonUtf8 = new Utf8JsonWriter(output, state);

            Guid guid = Guid.NewGuid();

            try
            {
                jsonUtf8.WriteStringValue(guid);
                Assert.True(false, "Expected ArgumentException to be thrown when IBufferWriter doesn't have enough space.");
            }
            catch (ArgumentException) { }

            output = new FixedSizedBufferWriter(39);
            jsonUtf8 = new Utf8JsonWriter(output, state);
            jsonUtf8.WriteStringValue(guid);
            jsonUtf8.Flush();
            string actualStr = Encoding.UTF8.GetString(output.Formatted);

            Assert.Equal(38, output.Formatted.Length);
            Assert.Equal($"\"{guid.ToString()}\"", actualStr);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void FixedSizeBufferWriter_DateTime(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new FixedSizedBufferWriter(28);

            var jsonUtf8 = new Utf8JsonWriter(output, state);

            var date = new DateTime(2019, 1, 1);

            try
            {
                jsonUtf8.WriteStringValue(date);
                Assert.True(false, "Expected ArgumentException to be thrown when IBufferWriter doesn't have enough space.");
            }
            catch (ArgumentException) { }

            output = new FixedSizedBufferWriter(29);
            jsonUtf8 = new Utf8JsonWriter(output, state);
            jsonUtf8.WriteStringValue(date);
            jsonUtf8.Flush();
            string actualStr = Encoding.UTF8.GetString(output.Formatted);

            Assert.Equal(29, output.Formatted.Length);
            Assert.Equal($"\"{date.ToString("O")}\"", actualStr);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void FixedSizeBufferWriter_DateTimeOffset(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new FixedSizedBufferWriter(34);

            var jsonUtf8 = new Utf8JsonWriter(output, state);

            DateTimeOffset date = new DateTime(2019, 1, 1);

            try
            {
                jsonUtf8.WriteStringValue(date);
                Assert.True(false, "Expected ArgumentException to be thrown when IBufferWriter doesn't have enough space.");
            }
            catch (ArgumentException) { }

            output = new FixedSizedBufferWriter(35);
            jsonUtf8 = new Utf8JsonWriter(output, state);
            jsonUtf8.WriteStringValue(date);
            jsonUtf8.Flush();
            string actualStr = Encoding.UTF8.GetString(output.Formatted);

            Assert.Equal(35, output.Formatted.Length);
            Assert.Equal($"\"{date.ToString("O")}\"", actualStr);
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void FixedSizeBufferWriter_Decimal(bool formatted, bool skipValidation)
        {
            var random = new Random(42);

            for (int i = 0; i < 1_000; i++)
            {
                var output = new FixedSizedBufferWriter(31);
                decimal value = JsonTestHelper.NextDecimal(random, 78E14, -78E14);
                var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });
                var jsonUtf8 = new Utf8JsonWriter(output, state);
                jsonUtf8.WriteNumberValue(value);

                jsonUtf8.Flush();
                string actualStr = Encoding.UTF8.GetString(output.Formatted);

                Assert.True(output.Formatted.Length <= 31);
                Assert.Equal(decimal.Parse(actualStr, CultureInfo.InvariantCulture), value);
            }

            for (int i = 0; i < 1_000; i++)
            {
                var output = new FixedSizedBufferWriter(31);
                decimal value = JsonTestHelper.NextDecimal(random, 1_000_000, -1_000_000);
                var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });
                var jsonUtf8 = new Utf8JsonWriter(output, state);
                jsonUtf8.WriteNumberValue(value);

                jsonUtf8.Flush();
                string actualStr = Encoding.UTF8.GetString(output.Formatted);

                Assert.True(output.Formatted.Length <= 31);
                Assert.Equal(decimal.Parse(actualStr, CultureInfo.InvariantCulture), value);
            }

            {
                var output = new FixedSizedBufferWriter(31);
                decimal value = 9999999999999999999999999999m;
                var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });
                var jsonUtf8 = new Utf8JsonWriter(output, state);
                jsonUtf8.WriteNumberValue(value);

                jsonUtf8.Flush();
                string actualStr = Encoding.UTF8.GetString(output.Formatted);

                Assert.Equal(value.ToString().Length, output.Formatted.Length);
                Assert.Equal(decimal.Parse(actualStr, CultureInfo.InvariantCulture), value);
            }

            {
                var output = new FixedSizedBufferWriter(31);
                decimal value = -9999999999999999999999999999m;
                var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });
                var jsonUtf8 = new Utf8JsonWriter(output, state);
                jsonUtf8.WriteNumberValue(value);

                jsonUtf8.Flush();
                string actualStr = Encoding.UTF8.GetString(output.Formatted);

                Assert.Equal(value.ToString().Length, output.Formatted.Length);
                Assert.Equal(decimal.Parse(actualStr, CultureInfo.InvariantCulture), value);
            }

            {
                var output = new FixedSizedBufferWriter(30);
                decimal value = -0.9999999999999999999999999999m;
                var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                try
                {
                    jsonUtf8.WriteNumberValue(value);
                    Assert.True(false, "Expected ArgumentException to be thrown when IBufferWriter doesn't have enough space.");
                }
                catch (ArgumentException) { }

                output = new FixedSizedBufferWriter(31);
                jsonUtf8 = new Utf8JsonWriter(output, state);
                jsonUtf8.WriteNumberValue(value);

                jsonUtf8.Flush();
                string actualStr = Encoding.UTF8.GetString(output.Formatted);

                Assert.Equal(value.ToString().Length, output.Formatted.Length);
                Assert.Equal(decimal.Parse(actualStr, CultureInfo.InvariantCulture), value);
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InvalidJsonMismatch(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new ArrayBufferWriter(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteEndArray();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteEndObject();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartArray("property at start", escape: false);
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject("property at start", escape: false);
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteStartArray("property inside array", escape: false);
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteStartObject();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteEndObject();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteStringValue("key");
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteString("key", "value");
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteEndArray();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteEndArray();
                jsonUtf8.WriteEndObject();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteStartObject("some object", escape: false);
                jsonUtf8.WriteEndObject();
                jsonUtf8.WriteEndArray();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteStartObject("some object", escape: false);
                jsonUtf8.WriteEndObject();
                jsonUtf8.WriteEndObject();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteStartArray("test array", escape: false);
                jsonUtf8.WriteEndArray();
                jsonUtf8.WriteEndArray();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteEndArray();
                jsonUtf8.WriteEndArray();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteEndObject();
                jsonUtf8.WriteEndObject();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteEndObject();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteStartObject("test object", escape: false);
                jsonUtf8.WriteEndArray();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            output.Dispose();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InvalidJsonIncomplete(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new ArrayBufferWriter(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartArray();
                jsonUtf8.Flush(isFinalBlock: true);
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.Flush(isFinalBlock: true);
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush(isFinalBlock: true);
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteStartObject("some object", escape: false);
                jsonUtf8.Flush(isFinalBlock: true);
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteStartObject("some object", escape: false);
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush(isFinalBlock: true);
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteStartArray("test array", escape: false);
                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush(isFinalBlock: true);
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            output.Dispose();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InvalidJsonPrimitive(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new ArrayBufferWriter(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteNumberValue(12345);
                jsonUtf8.WriteNumberValue(12345);
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteNumberValue(12345);
                jsonUtf8.WriteStartArray();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteNumberValue(12345);
                jsonUtf8.WriteStartObject();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteNumberValue(12345);
                jsonUtf8.WriteStartArray("property name", escape: false);
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteNumberValue(12345);
                jsonUtf8.WriteStartObject("property name", escape: false);
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteNumberValue(12345);
                jsonUtf8.WriteString("property name", "value", escape: false);
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteNumberValue(12345);
                jsonUtf8.WriteEndArray();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteNumberValue(12345);
                jsonUtf8.WriteEndObject();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            output.Dispose();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InvalidNumbersJson(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new ArrayBufferWriter(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteNumberValue(double.NegativeInfinity);
                Assert.True(false, "Expected ArgumentException to be thrown for unsupported number values.");
            }
            catch (ArgumentException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteNumberValue(double.PositiveInfinity);
                Assert.True(false, "Expected ArgumentException to be thrown for unsupported number values.");
            }
            catch (ArgumentException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteNumberValue(double.NaN);
                Assert.True(false, "Expected ArgumentException to be thrown for unsupported number values.");
            }
            catch (ArgumentException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteNumberValue(float.PositiveInfinity);
                Assert.True(false, "Expected ArgumentException to be thrown for unsupported number values.");
            }
            catch (ArgumentException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteNumberValue(float.NegativeInfinity);
                Assert.True(false, "Expected ArgumentException to be thrown for unsupported number values.");
            }
            catch (ArgumentException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteNumberValue(float.NaN);
                Assert.True(false, "Expected ArgumentException to be thrown for unsupported number values.");
            }
            catch (ArgumentException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteNumber("name", double.NegativeInfinity);
                Assert.True(false, "Expected ArgumentException to be thrown for unsupported number values.");
            }
            catch (ArgumentException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteNumber("name", double.PositiveInfinity);
                Assert.True(false, "Expected ArgumentException to be thrown for unsupported number values.");
            }
            catch (ArgumentException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteNumber("name", double.NaN);
                Assert.True(false, "Expected ArgumentException to be thrown for unsupported number values.");
            }
            catch (ArgumentException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteNumber("name", float.PositiveInfinity);
                Assert.True(false, "Expected ArgumentException to be thrown for unsupported number values.");
            }
            catch (ArgumentException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteNumber("name", float.NegativeInfinity);
                Assert.True(false, "Expected ArgumentException to be thrown for unsupported number values.");
            }
            catch (ArgumentException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteNumber("name", float.NaN);
                Assert.True(false, "Expected ArgumentException to be thrown for unsupported number values.");
            }
            catch (ArgumentException) { }

            output.Dispose();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void InvalidJsonContinueShouldSucceed(bool formatted)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = true });

            var output = new ArrayBufferWriter(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, state);

            for (int i = 0; i < 100; i++)
                jsonUtf8.WriteEndArray();
            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteEndArray();
            jsonUtf8.Flush();

            ArraySegment<byte> arraySegment = output.Formatted;
            string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

            var sb = new StringBuilder();
            for (int i = 0; i < 100; i++)
            {
                if (formatted)
                    sb.Append(Environment.NewLine);
                sb.Append("]");
            }
            sb.Append(",");
            if (formatted)
                sb.Append(Environment.NewLine);
            sb.Append("[]");

            Assert.Equal(sb.ToString(), actualStr);

            output.Dispose();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WritingTooDeep(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new ArrayBufferWriter(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, state);
            try
            {
                for (int i = 0; i < 1001; i++)
                {
                    jsonUtf8.WriteStartArray();
                }
                Assert.True(false, "Expected InvalidOperationException to be thrown for depth >= 1000.");
            }
            catch (InvalidOperationException) { }

            output.Dispose();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WritingTooDeepProperty(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new ArrayBufferWriter(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, state);

            try
            {
                jsonUtf8.WriteStartObject();
                for (int i = 0; i < 1000; i++)
                {
                    jsonUtf8.WriteStartArray("name");
                }
                Assert.True(false, "Expected InvalidOperationException to be thrown for depth >= 1000.");
            }
            catch (InvalidOperationException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);

            try
            {
                jsonUtf8.WriteStartObject();
                for (int i = 0; i < 1000; i++)
                {
                    jsonUtf8.WriteStartArray(Encoding.UTF8.GetBytes("name"));
                }
                Assert.True(false, "Expected InvalidOperationException to be thrown for depth >= 1000.");
            }
            catch (InvalidOperationException) { }

            output.Dispose();
        }

        [ConditionalTheory(nameof(IsX64))]
        [OuterLoop]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WritingTooLargeProperty(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new ArrayBufferWriter(1024);

            var jsonUtf8 = new Utf8JsonWriter(output, state);

            Span<byte> key;
            Span<char> keyChars;

            try
            {
                key = new byte[1_000_000_000];
                keyChars = new char[1_000_000_000];
            }
            catch (OutOfMemoryException)
            {
                return;
            }

            key.Fill((byte)'a');
            keyChars.Fill('a');

            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteStartArray(keyChars);
                Assert.True(false, "Expected ArgumentException to be thrown for depth >= 1000.");
            }
            catch (ArgumentException) { }

            jsonUtf8 = new Utf8JsonWriter(output, state);

            try
            {
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteStartArray(key);
                Assert.True(false, "Expected ArgumentException to be thrown for depth >= 1000.");
            }
            catch (ArgumentException) { }

            output.Dispose();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteSingleValue(bool formatted, bool skipValidation)
        {
            string expectedStr = "123456789012345";

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteNumberValue(123456789012345);

                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                Assert.True(expectedStr == actualStr, $"Case: {i}, | Expected: {expectedStr}, | Actual: {actualStr}");

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteHelloWorld(bool formatted, bool skipValidation)
        {
            string expectedStr = GetHelloWorldExpectedString(prettyPrint: formatted);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            for (int i = 0; i < 9; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteString("message", "Hello, World!", escape: false);
                        break;
                    case 1:
                        jsonUtf8.WriteString("message", "Hello, World!".AsSpan(), escape: false);
                        break;
                    case 2:
                        jsonUtf8.WriteString("message", Encoding.UTF8.GetBytes("Hello, World!"), escape: false);
                        break;
                    case 3:
                        jsonUtf8.WriteString("message".AsSpan(), "Hello, World!", escape: false);
                        break;
                    case 4:
                        jsonUtf8.WriteString("message".AsSpan(), "Hello, World!".AsSpan(), escape: false);
                        break;
                    case 5:
                        jsonUtf8.WriteString("message".AsSpan(), Encoding.UTF8.GetBytes("Hello, World!"), escape: false);
                        break;
                    case 6:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes("message"), "Hello, World!", escape: false);
                        break;
                    case 7:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes("message"), "Hello, World!".AsSpan(), escape: false);
                        break;
                    case 8:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes("message"), Encoding.UTF8.GetBytes("Hello, World!"), escape: false);
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                Assert.True(expectedStr == actualStr, $"Case: {i}, | Expected: {expectedStr}, | Actual: {actualStr}");

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WritePartialHelloWorld(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new ArrayBufferWriter(10);
            var jsonUtf8 = new Utf8JsonWriter(output, state);

            jsonUtf8.WriteStartObject();

            Assert.Equal(0, jsonUtf8.BytesCommitted);
            Assert.Equal(1, jsonUtf8.BytesWritten);

            jsonUtf8.WriteString("message", "Hello, World!");

            Assert.Equal(16, jsonUtf8.BytesCommitted);
            if (formatted)
                Assert.Equal(26 + 2 + Environment.NewLine.Length + 1, jsonUtf8.BytesWritten);
            else
                Assert.Equal(26, jsonUtf8.BytesWritten);

            jsonUtf8.Flush(isFinalBlock: false);

            if (formatted)
                Assert.Equal(26 + 2 + Environment.NewLine.Length + 1, jsonUtf8.BytesCommitted); // new lines, indentation, white space
            else
                Assert.Equal(26, jsonUtf8.BytesCommitted);

            Assert.Equal(jsonUtf8.BytesCommitted, jsonUtf8.BytesWritten);

            jsonUtf8.WriteString("message", "Hello, World!");
            jsonUtf8.WriteEndObject();

            if (formatted)
                Assert.Equal(26 + 2 + Environment.NewLine.Length + 1, jsonUtf8.BytesCommitted);
            else
                Assert.Equal(26, jsonUtf8.BytesCommitted);

            if (formatted)
                Assert.Equal(53 + (2 * 2) + (3 * Environment.NewLine.Length) + (1 * 2), jsonUtf8.BytesWritten); // new lines, indentation, white space
            else
                Assert.Equal(53, jsonUtf8.BytesWritten);

            jsonUtf8.Flush(isFinalBlock: true);

            if (formatted)
                Assert.Equal(53 + (2 * 2) + (3 * Environment.NewLine.Length) + (1 * 2), jsonUtf8.BytesCommitted); // new lines, indentation, white space
            else
                Assert.Equal(53, jsonUtf8.BytesCommitted);

            Assert.Equal(jsonUtf8.BytesCommitted, jsonUtf8.BytesWritten);

            Assert.Equal(0, state.BytesCommitted);
            Assert.Equal(0, state.BytesWritten);

            state = jsonUtf8.GetCurrentState();
            Assert.Equal(jsonUtf8.BytesCommitted, state.BytesCommitted);
            Assert.Equal(jsonUtf8.BytesWritten, state.BytesWritten);

            output.Dispose();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WritePartialHelloWorldSaveState(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new ArrayBufferWriter(10);
            var jsonUtf8 = new Utf8JsonWriter(output, state);

            Assert.Equal(0, jsonUtf8.CurrentDepth);
            jsonUtf8.WriteStartObject();
            Assert.Equal(1, jsonUtf8.CurrentDepth);
            jsonUtf8.Flush(isFinalBlock: false);

            state = jsonUtf8.GetCurrentState();

            Assert.Equal(1, state.BytesCommitted);
            Assert.Equal(1, state.BytesWritten);

            jsonUtf8 = new Utf8JsonWriter(output, state);

            Assert.Equal(1, jsonUtf8.CurrentDepth);

            jsonUtf8.WriteString("message", "Hello, World!");
            jsonUtf8.WriteEndObject();
            jsonUtf8.Flush();

            Assert.Equal(jsonUtf8.BytesCommitted, jsonUtf8.BytesWritten);

            if (formatted)
                Assert.Equal(26 + 2 + (2 * Environment.NewLine.Length) + 1, jsonUtf8.BytesCommitted);
            else
                Assert.Equal(26, jsonUtf8.BytesCommitted);

            Assert.Equal(1, state.BytesCommitted);
            Assert.Equal(1, state.BytesWritten);

            state = jsonUtf8.GetCurrentState();

            if (formatted)
            {
                Assert.Equal(26 + 2 + (2 * Environment.NewLine.Length) + 1, state.BytesCommitted);
                Assert.Equal(26 + 2 + (2 * Environment.NewLine.Length) + 1, state.BytesWritten);
            }
            else
            {
                Assert.Equal(26, state.BytesCommitted);
                Assert.Equal(26, state.BytesWritten);
            }

            output.Dispose();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteInvalidPartialJson(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new ArrayBufferWriter(10);
            var jsonUtf8 = new Utf8JsonWriter(output, state);

            jsonUtf8.WriteStartObject();

            Assert.Equal(0, state.BytesCommitted);
            Assert.Equal(0, state.BytesWritten);

            jsonUtf8.Flush(isFinalBlock: false);

            state = jsonUtf8.GetCurrentState();

            Assert.Equal(1, state.BytesCommitted);
            Assert.Equal(1, state.BytesWritten);

            jsonUtf8 = new Utf8JsonWriter(output, state);

            try
            {
                jsonUtf8.WriteStringValue("Hello, World!");
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }
            try
            {
                jsonUtf8.WriteEndArray();
                WriterDidNotThrow(skipValidation);
            }
            catch (InvalidOperationException) { }

            output.Dispose();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WritePartialJsonSkipFlush(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new ArrayBufferWriter(10);
            var jsonUtf8 = new Utf8JsonWriter(output, state);

            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteString("message", "Hello, World!");

            Assert.Equal(1, jsonUtf8.CurrentDepth);

            try
            {
                state = jsonUtf8.GetCurrentState();
                Assert.True(false, "Expected InvalidOperationException when trying to get current state without flushing first.");
            }
            catch (InvalidOperationException)
            {

            }
            finally
            {
                jsonUtf8.Flush(isFinalBlock: false);
                state = jsonUtf8.GetCurrentState();
            }

            if (formatted)
                Assert.Equal(26 + 2 + Environment.NewLine.Length + 1, state.BytesWritten);
            else
                Assert.Equal(26, state.BytesWritten);

            Assert.Equal(jsonUtf8.BytesWritten, jsonUtf8.BytesCommitted);

            jsonUtf8 = new Utf8JsonWriter(output, state);
            Assert.Equal(1, jsonUtf8.CurrentDepth);
            Assert.Equal(0, jsonUtf8.BytesWritten);
            Assert.Equal(0, jsonUtf8.BytesCommitted);
            jsonUtf8.WriteEndObject();
            jsonUtf8.Flush();

            output.Dispose();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteInvalidDepthPartial(bool formatted, bool skipValidation)
        {
            {
                var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

                var output = new ArrayBufferWriter(10);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                Assert.Equal(0, jsonUtf8.CurrentDepth);

                state = jsonUtf8.GetCurrentState();

                jsonUtf8 = new Utf8JsonWriter(output, state);

                try
                {
                    jsonUtf8.WriteStartObject();
                    WriterDidNotThrow(skipValidation);
                }
                catch (InvalidOperationException) { }

                output.Dispose();
            }

            {
                var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

                var output = new ArrayBufferWriter(10);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                Assert.Equal(0, jsonUtf8.CurrentDepth);
                state = jsonUtf8.GetCurrentState();

                jsonUtf8 = new Utf8JsonWriter(output, state);

                try
                {
                    jsonUtf8.WriteStartObject("name");
                    WriterDidNotThrow(skipValidation);
                }
                catch (InvalidOperationException) { }

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true, "comment")]
        [InlineData(true, false, "comment")]
        [InlineData(false, true, "comment")]
        [InlineData(false, false, "comment")]
        [InlineData(true, true, "comm><ent")]
        [InlineData(true, false, "comm><ent")]
        [InlineData(false, true, "comm><ent")]
        [InlineData(false, false, "comm><ent")]
        [InlineData(true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void WriteComments(bool formatted, bool skipValidation, string comment)
        {
            string expectedStr = GetCommentExpectedString(prettyPrint: formatted, comment);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartArray();

                for (int j = 0; j < 10; j++)
                {
                    WriteCommentValue(ref jsonUtf8, i, comment);
                }

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteStringValue(comment);
                        break;
                    case 1:
                        jsonUtf8.WriteStringValue(comment.AsSpan());
                        break;
                    case 2:
                        jsonUtf8.WriteStringValue(Encoding.UTF8.GetBytes(comment));
                        break;
                }

                WriteCommentValue(ref jsonUtf8, i, comment);

                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                Assert.True(expectedStr == actualStr, $"Case: {i}, | Expected: {expectedStr}, | Actual: {actualStr}");

                output.Dispose();
            }
        }

        private static void WriteCommentValue(ref Utf8JsonWriter jsonUtf8, int i, string comment)
        {
            switch (i)
            {
                case 0:
                    jsonUtf8.WriteCommentValue(comment, escape: false);
                    break;
                case 1:
                    jsonUtf8.WriteCommentValue(comment.AsSpan(), escape: false);
                    break;
                case 2:
                    jsonUtf8.WriteCommentValue(Encoding.UTF8.GetBytes(comment), escape: false);
                    break;
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteStrings(bool formatted, bool skipValidation)
        {
            string value = "temp";
            string expectedStr = GetStringsExpectedString(prettyPrint: formatted, value);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            for (int i = 0; i < 6; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartArray();

                for (int j = 0; j < 10; j++)
                {
                    switch (i)
                    {
                        case 0:
                            jsonUtf8.WriteStringValue(value);
                            break;
                        case 1:
                            jsonUtf8.WriteStringValue(value.AsSpan());
                            break;
                        case 2:
                            jsonUtf8.WriteStringValue(Encoding.UTF8.GetBytes(value));
                            break;
                        case 3:
                            jsonUtf8.WriteStringValue(value, escape: false);
                            break;
                        case 4:
                            jsonUtf8.WriteStringValue(value.AsSpan(), escape: false);
                            break;
                        case 5:
                            jsonUtf8.WriteStringValue(Encoding.UTF8.GetBytes(value), escape: false);
                            break;
                    }
                }

                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                Assert.True(expectedStr == actualStr, $"Case: {i}, | Expected: {expectedStr}, | Actual: {actualStr}");

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true, "mess\nage", "Hello, \nWorld!")]
        [InlineData(true, false, "mess\nage", "Hello, \nWorld!")]
        [InlineData(false, true, "mess\nage", "Hello, \nWorld!")]
        [InlineData(false, false, "mess\nage", "Hello, \nWorld!")]
        [InlineData(true, true, "message", "Hello, \nWorld!")]
        [InlineData(true, false, "message", "Hello, \nWorld!")]
        [InlineData(false, true, "message", "Hello, \nWorld!")]
        [InlineData(false, false, "message", "Hello, \nWorld!")]
        [InlineData(true, true, "mess\nage", "Hello, World!")]
        [InlineData(true, false, "mess\nage", "Hello, World!")]
        [InlineData(false, true, "mess\nage", "Hello, World!")]
        [InlineData(false, false, "mess\nage", "Hello, World!")]
        [InlineData(true, true, "message", "Hello, World!")]
        [InlineData(true, false, "message", "Hello, World!")]
        [InlineData(false, true, "message", "Hello, World!")]
        [InlineData(false, false, "message", "Hello, World!")]
        [InlineData(true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>mess\nage", ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>Hello, \nWorld!")]
        [InlineData(true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>mess\nage", ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>Hello, \nWorld!")]
        [InlineData(false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>mess\nage", ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>Hello, \nWorld!")]
        [InlineData(false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>mess\nage", ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>Hello, \nWorld!")]
        public void WriteHelloWorldEscaped(bool formatted, bool skipValidation, string key, string value)
        {
            string expectedStr = GetEscapedExpectedString(prettyPrint: formatted, key, value, StringEscapeHandling.EscapeHtml);
            string expectedStrNoEscape = GetEscapedExpectedString(prettyPrint: formatted, key, value, StringEscapeHandling.EscapeHtml, escape: false);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            for (int i = 0; i < 18; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteString(key, value, escape: true);
                        break;
                    case 1:
                        jsonUtf8.WriteString(key.AsSpan(), value.AsSpan(), escape: true);
                        break;
                    case 2:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value), escape: true);
                        break;
                    case 3:
                        jsonUtf8.WriteString(key, value.AsSpan(), escape: true);
                        break;
                    case 4:
                        jsonUtf8.WriteString(key, Encoding.UTF8.GetBytes(value), escape: true);
                        break;
                    case 5:
                        jsonUtf8.WriteString(key.AsSpan(), value, escape: true);
                        break;
                    case 6:
                        jsonUtf8.WriteString(key.AsSpan(), Encoding.UTF8.GetBytes(value), escape: true);
                        break;
                    case 7:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(key), value, escape: true);
                        break;
                    case 8:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(key), value.AsSpan(), escape: true);
                        break;
                    case 9:
                        jsonUtf8.WriteString(key, value, escape: false);
                        break;
                    case 10:
                        jsonUtf8.WriteString(key.AsSpan(), value.AsSpan(), escape: false);
                        break;
                    case 11:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value), escape: false);
                        break;
                    case 12:
                        jsonUtf8.WriteString(key, value.AsSpan(), escape: false);
                        break;
                    case 13:
                        jsonUtf8.WriteString(key, Encoding.UTF8.GetBytes(value), escape: false);
                        break;
                    case 14:
                        jsonUtf8.WriteString(key.AsSpan(), value, escape: false);
                        break;
                    case 15:
                        jsonUtf8.WriteString(key.AsSpan(), Encoding.UTF8.GetBytes(value), escape: false);
                        break;
                    case 16:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(key), value, escape: false);
                        break;
                    case 17:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(key), value.AsSpan(), escape: false);
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                if (i >= 9)
                    Assert.True(expectedStrNoEscape == actualStr, $"Case: {i}, | Expected: {expectedStr}, | Actual: {actualStr}");
                else
                    Assert.True(expectedStr == actualStr, $"Case: {i}, | Expected: {expectedStr}, | Actual: {actualStr}");

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void EscapeAsciiCharacters(bool formatted, bool skipValidation)
        {
            var propertyArray = new char[128];

            char[] specialCases = { '+', '`', (char)0x7F };
            for (int i = 0; i < propertyArray.Length; i++)
            {
                if (Array.IndexOf(specialCases, (char)i) != -1)
                {
                    propertyArray[i] = (char)0;
                }
                else
                {
                    propertyArray[i] = (char)i;
                }
            }

            string propertyName = new string(propertyArray);
            string value = new string(propertyArray);

            string expectedStr = GetEscapedExpectedString(prettyPrint: formatted, propertyName, value, StringEscapeHandling.EscapeHtml);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });
            for (int i = 0; i < 4; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteString(propertyName, value, escape: true);
                        break;
                    case 1:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(propertyName), Encoding.UTF8.GetBytes(value), escape: true);
                        break;
                    case 2:
                        expectedStr = GetEscapedExpectedString(prettyPrint: formatted, propertyName, value, StringEscapeHandling.EscapeHtml, escape: false);
                        jsonUtf8.WriteString(propertyName, value, escape: false);
                        break;
                    case 3:
                        expectedStr = GetEscapedExpectedString(prettyPrint: formatted, propertyName, value, StringEscapeHandling.EscapeHtml, escape: false);
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(propertyName), Encoding.UTF8.GetBytes(value), escape: false);
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                Assert.Equal(expectedStr, actualStr);

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void EscapeCharacters(bool formatted, bool skipValidation)
        {
            // Do not include surrogate pairs.
            var propertyArray = new char[0xD800 + (0xFFFF - 0xE000) + 1];

            for (int i = 128; i < propertyArray.Length; i++)
            {
                if (i < 0xD800 || i > 0xDFFF)
                {
                    propertyArray[i] = (char)i;
                }
            }

            string propertyName = new string(propertyArray);
            string value = new string(propertyArray);

            string expectedStr = GetEscapedExpectedString(prettyPrint: formatted, propertyName, value, StringEscapeHandling.EscapeNonAscii);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });
            for (int i = 0; i < 4; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteString(propertyName, value, escape: true);
                        break;
                    case 1:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(propertyName), Encoding.UTF8.GetBytes(value), escape: true);
                        break;
                    case 2:
                        expectedStr = GetEscapedExpectedString(prettyPrint: formatted, propertyName, value, StringEscapeHandling.EscapeNonAscii, escape: false);
                        jsonUtf8.WriteString(propertyName, value, escape: false);
                        break;
                    case 3:
                        expectedStr = GetEscapedExpectedString(prettyPrint: formatted, propertyName, value, StringEscapeHandling.EscapeNonAscii, escape: false);
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(propertyName), Encoding.UTF8.GetBytes(value), escape: false);
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                Assert.Equal(expectedStr, actualStr);

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void EscapeSurrogatePairs(bool formatted, bool skipValidation)
        {
            var propertyArray = new char[10] { 'a', (char)0xD800, (char)0xDC00, (char)0xD803, (char)0xDE6D, (char)0xD834, (char)0xDD1E, (char)0xDBFF, (char)0xDFFF, 'a' };

            string propertyName = new string(propertyArray);
            string value = new string(propertyArray);

            string expectedStr = GetEscapedExpectedString(prettyPrint: formatted, propertyName, value, StringEscapeHandling.EscapeNonAscii);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });
            for (int i = 0; i < 4; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteString(propertyName, value, escape: true);
                        break;
                    case 1:
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(propertyName), Encoding.UTF8.GetBytes(value), escape: true);
                        break;
                    case 2:
                        expectedStr = GetEscapedExpectedString(prettyPrint: formatted, propertyName, value, StringEscapeHandling.EscapeNonAscii, escape: false);
                        jsonUtf8.WriteString(propertyName, value, escape: false);
                        break;
                    case 3:
                        expectedStr = GetEscapedExpectedString(prettyPrint: formatted, propertyName, value, StringEscapeHandling.EscapeNonAscii, escape: false);
                        jsonUtf8.WriteString(Encoding.UTF8.GetBytes(propertyName), Encoding.UTF8.GetBytes(value), escape: false);
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                Assert.Equal(expectedStr, actualStr);

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void InvalidUTF8(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new ArrayBufferWriter(1024);
            var jsonUtf8 = new Utf8JsonWriter(output, state);

            jsonUtf8.WriteStartObject();
            for (int i = 0; i < 8; i++)
            {
                try
                {
                    switch (i)
                    {
                        case 0:
                            jsonUtf8.WriteString(new byte[2] { 0xc3, 0x28 }, new byte[2] { 0xc3, 0x28 }, escape: false);
                            AssertWriterThrow(noThrow: false);
                            break;
                        case 1:
                            jsonUtf8.WriteString(new byte[2] { 0xc3, 0x28 }, new byte[2] { 0xc3, 0xb1 }, escape: false);
                            AssertWriterThrow(noThrow: true);
                            break;
                        case 2:
                            jsonUtf8.WriteString(new byte[2] { 0xc3, 0xb1 }, new byte[2] { 0xc3, 0x28 }, escape: false);
                            AssertWriterThrow(noThrow: false);
                            break;
                        case 3:
                            jsonUtf8.WriteString(new byte[2] { 0xc3, 0xb1 }, new byte[2] { 0xc3, 0xb1 }, escape: false);
                            AssertWriterThrow(noThrow: true);
                            break;
                        case 4:
                            jsonUtf8.WriteString(new byte[2] { 0xc3, 0x28 }, new byte[2] { 0xc3, 0x28 }, escape: true);
                            AssertWriterThrow(noThrow: false);
                            break;
                        case 5:
                            jsonUtf8.WriteString(new byte[2] { 0xc3, 0x28 }, new byte[2] { 0xc3, 0xb1 }, escape: true);
                            AssertWriterThrow(noThrow: false);
                            break;
                        case 6:
                            jsonUtf8.WriteString(new byte[2] { 0xc3, 0xb1 }, new byte[2] { 0xc3, 0x28 }, escape: true);
                            AssertWriterThrow(noThrow: false);
                            break;
                        case 7:
                            jsonUtf8.WriteString(new byte[2] { 0xc3, 0xb1 }, new byte[2] { 0xc3, 0xb1 }, escape: true);
                            AssertWriterThrow(noThrow: true);
                            break;
                    }
                }
                catch (ArgumentException) { }
            }
            jsonUtf8.WriteEndObject();
            jsonUtf8.Flush();

            output.Dispose();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteCustomStrings(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var output = new ArrayBufferWriter(10);
            var jsonUtf8 = new Utf8JsonWriter(output, state);

            jsonUtf8.WriteStartObject();

            for (int i = 0; i < 1_000; i++)
                jsonUtf8.WriteString("message", "Hello, World!", escape: false);

            jsonUtf8.WriteEndObject();
            jsonUtf8.Flush();

            ArraySegment<byte> arraySegment = output.Formatted;
            string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

            Assert.Equal(GetCustomExpectedString(formatted), actualStr);

            output.Dispose();
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteStartEnd(bool formatted, bool skipValidation)
        {
            string expectedStr = GetStartEndExpectedString(prettyPrint: formatted);

            var output = new ArrayBufferWriter(1024);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            var jsonUtf8 = new Utf8JsonWriter(output, state);

            jsonUtf8.WriteStartArray();
            jsonUtf8.WriteStartObject();
            jsonUtf8.WriteEndObject();
            jsonUtf8.WriteEndArray();
            jsonUtf8.Flush();

            ArraySegment<byte> arraySegment = output.Formatted;
            string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

            Assert.Equal(expectedStr, actualStr);

            output.Dispose();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void WriteStartEndInvalid(bool formatted)
        {
            {
                string expectedStr = "[}";

                var output = new ArrayBufferWriter(1024);

                var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = true });

                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                Assert.Equal(expectedStr, actualStr);

                output.Dispose();
            }

            {
                string expectedStr = "{]";

                var output = new ArrayBufferWriter(1024);

                var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = true });

                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteEndArray();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                Assert.Equal(expectedStr, actualStr);

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteStartEndWithPropertyNameArray(bool formatted, bool skipValidation)
        {
            string expectedStr = GetStartEndWithPropertyArrayExpectedString(prettyPrint: formatted);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            for (int i = 0; i < 6; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteStartArray("property name", escape: false);
                        break;
                    case 1:
                        jsonUtf8.WriteStartArray("property name".AsSpan(), escape: false);
                        break;
                    case 2:
                        jsonUtf8.WriteStartArray(Encoding.UTF8.GetBytes("property name"), escape: false);
                        break;
                    case 3:
                        jsonUtf8.WriteStartArray("property name", escape: true);
                        break;
                    case 4:
                        jsonUtf8.WriteStartArray("property name".AsSpan(), escape: true);
                        break;
                    case 5:
                        jsonUtf8.WriteStartArray(Encoding.UTF8.GetBytes("property name"), escape: true);
                        break;
                }

                jsonUtf8.WriteEndArray();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                Assert.Equal(expectedStr, actualStr);

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true, 10)]
        [InlineData(true, false, 10)]
        [InlineData(false, true, 10)]
        [InlineData(false, false, 10)]
        [InlineData(true, true, 100)]
        [InlineData(true, false, 100)]
        [InlineData(false, true, 100)]
        [InlineData(false, false, 100)]
        public void WriteStartEndWithPropertyNameArray(bool formatted, bool skipValidation, int keyLength)
        {
            var keyChars = new char[keyLength];
            for (int i = 0; i < keyChars.Length; i++)
            {
                keyChars[i] = '<';
            }
            var key = new string(keyChars);

            string expectedStr = GetStartEndWithPropertyArrayExpectedString(key, prettyPrint: formatted, escape: true);
            string expectedStrNoEscape = GetStartEndWithPropertyArrayExpectedString(key, prettyPrint: formatted, escape: false);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            for (int i = 0; i < 6; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteStartArray(key, escape: false);
                        break;
                    case 1:
                        jsonUtf8.WriteStartArray(key.AsSpan(), escape: false);
                        break;
                    case 2:
                        jsonUtf8.WriteStartArray(Encoding.UTF8.GetBytes(key), escape: false);
                        break;
                    case 3:
                        jsonUtf8.WriteStartArray(key, escape: true);
                        break;
                    case 4:
                        jsonUtf8.WriteStartArray(key.AsSpan(), escape: true);
                        break;
                    case 5:
                        jsonUtf8.WriteStartArray(Encoding.UTF8.GetBytes(key), escape: true);
                        break;
                }

                jsonUtf8.WriteEndArray();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                if (i < 3)
                    Assert.Equal(expectedStrNoEscape, actualStr);
                else
                    Assert.Equal(expectedStr, actualStr);

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteStartEndWithPropertyNameObject(bool formatted, bool skipValidation)
        {
            string expectedStr = GetStartEndWithPropertyObjectExpectedString(prettyPrint: formatted);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            for (int i = 0; i < 6; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteStartObject("property name", escape: false);
                        break;
                    case 1:
                        jsonUtf8.WriteStartObject("property name".AsSpan(), escape: false);
                        break;
                    case 2:
                        jsonUtf8.WriteStartObject(Encoding.UTF8.GetBytes("property name"), escape: false);
                        break;
                    case 3:
                        jsonUtf8.WriteStartObject("property name", escape: true);
                        break;
                    case 4:
                        jsonUtf8.WriteStartObject("property name".AsSpan(), escape: true);
                        break;
                    case 5:
                        jsonUtf8.WriteStartObject(Encoding.UTF8.GetBytes("property name"), escape: true);
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                Assert.Equal(expectedStr, actualStr);

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true, 10)]
        [InlineData(true, false, 10)]
        [InlineData(false, true, 10)]
        [InlineData(false, false, 10)]
        [InlineData(true, true, 100)]
        [InlineData(true, false, 100)]
        [InlineData(false, true, 100)]
        [InlineData(false, false, 100)]
        public void WriteStartEndWithPropertyNameObject(bool formatted, bool skipValidation, int keyLength)
        {
            var keyChars = new char[keyLength];
            for (int i = 0; i < keyChars.Length; i++)
            {
                keyChars[i] = '<';
            }
            var key = new string(keyChars);

            string expectedStr = GetStartEndWithPropertyObjectExpectedString(key, prettyPrint: formatted, escape: true);
            string expectedStrNoEscape = GetStartEndWithPropertyObjectExpectedString(key, prettyPrint: formatted, escape: false);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            for (int i = 0; i < 6; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteStartObject(key, escape: false);
                        break;
                    case 1:
                        jsonUtf8.WriteStartObject(key.AsSpan(), escape: false);
                        break;
                    case 2:
                        jsonUtf8.WriteStartObject(Encoding.UTF8.GetBytes(key), escape: false);
                        break;
                    case 3:
                        jsonUtf8.WriteStartObject(key, escape: true);
                        break;
                    case 4:
                        jsonUtf8.WriteStartObject(key.AsSpan(), escape: true);
                        break;
                    case 5:
                        jsonUtf8.WriteStartObject(Encoding.UTF8.GetBytes(key), escape: true);
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                if (i < 3)
                    Assert.Equal(expectedStrNoEscape, actualStr);
                else
                    Assert.Equal(expectedStr, actualStr);

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteArrayWithProperty(bool formatted, bool skipValidation)
        {
            string expectedStr = GetArrayWithPropertyExpectedString(prettyPrint: formatted);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter(1024);

                var jsonUtf8 = new Utf8JsonWriter(output, state);
                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteStartArray("message", escape: false);
                        break;
                    case 1:
                        jsonUtf8.WriteStartArray("message".AsSpan(), escape: false);
                        break;
                    case 2:
                        jsonUtf8.WriteStartArray(Encoding.UTF8.GetBytes("message"), escape: false);
                        break;
                }

                jsonUtf8.WriteEndArray();
                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                Assert.True(expectedStr == actualStr, $"Case: {i}, | Expected: {expectedStr}, | Actual: {actualStr}");

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true, true, "message")]
        [InlineData(true, false, true, "message")]
        [InlineData(false, true, true, "message")]
        [InlineData(false, false, true, "message")]
        [InlineData(true, true, true, "mess><age")]
        [InlineData(true, false, true, "mess><age")]
        [InlineData(false, true, true, "mess><age")]
        [InlineData(false, false, true, "mess><age")]
        [InlineData(true, true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, true, false, "message")]
        [InlineData(true, false, false, "message")]
        [InlineData(false, true, false, "message")]
        [InlineData(false, false, false, "message")]
        [InlineData(true, true, false, "mess><age")]
        [InlineData(true, false, false, "mess><age")]
        [InlineData(false, true, false, "mess><age")]
        [InlineData(false, false, false, "mess><age")]
        [InlineData(true, true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void WriteBooleanValue(bool formatted, bool skipValidation, bool value, string keyString)
        {
            string expectedStr = GetBooleanExpectedString(prettyPrint: formatted, keyString, value, escape: true);
            string expectedStrNoEscape = GetBooleanExpectedString(prettyPrint: formatted, keyString, value, escape: false);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            for (int i = 0; i < 6; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteBoolean(keyString, value, escape: false);
                        break;
                    case 1:
                        jsonUtf8.WriteBoolean(keyString.AsSpan(), value, escape: false);
                        break;
                    case 2:
                        jsonUtf8.WriteBoolean(Encoding.UTF8.GetBytes(keyString), value, escape: false);
                        break;
                    case 3:
                        jsonUtf8.WriteBoolean(keyString, value, escape: true);
                        break;
                    case 4:
                        jsonUtf8.WriteBoolean(keyString.AsSpan(), value, escape: true);
                        break;
                    case 5:
                        jsonUtf8.WriteBoolean(Encoding.UTF8.GetBytes(keyString), value, escape: true);
                        break;
                }

                jsonUtf8.WriteStartArray("temp");
                jsonUtf8.WriteBooleanValue(true);
                jsonUtf8.WriteBooleanValue(true);
                jsonUtf8.WriteBooleanValue(false);
                jsonUtf8.WriteBooleanValue(false);
                jsonUtf8.WriteEndArray();

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                if (i < 3)
                    Assert.True(expectedStrNoEscape == actualStr, $"Case: {i}, | Expected: {expectedStrNoEscape}, | Actual: {actualStr}, | Value: {value}");
                else
                    Assert.True(expectedStr == actualStr, $"Case: {i}, | Expected: {expectedStr}, | Actual: {actualStr}, | Value: {value}");

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true, "message")]
        [InlineData(true, false, "message")]
        [InlineData(false, true, "message")]
        [InlineData(false, false, "message")]
        [InlineData(true, true, "mess><age")]
        [InlineData(true, false, "mess><age")]
        [InlineData(false, true, "mess><age")]
        [InlineData(false, false, "mess><age")]
        [InlineData(true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void WriteNullValue(bool formatted, bool skipValidation, string keyString)
        {
            string expectedStr = GetNullExpectedString(prettyPrint: formatted, keyString, escape: true);
            string expectedStrNoEscape = GetNullExpectedString(prettyPrint: formatted, keyString, escape: false);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            for (int i = 0; i < 6; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteNull(keyString, escape: false);
                        break;
                    case 1:
                        jsonUtf8.WriteNull(keyString.AsSpan(), escape: false);
                        break;
                    case 2:
                        jsonUtf8.WriteNull(Encoding.UTF8.GetBytes(keyString), escape: false);
                        break;
                    case 3:
                        jsonUtf8.WriteNull(keyString, escape: true);
                        break;
                    case 4:
                        jsonUtf8.WriteNull(keyString.AsSpan(), escape: true);
                        break;
                    case 5:
                        jsonUtf8.WriteNull(Encoding.UTF8.GetBytes(keyString), escape: true);
                        break;
                }

                jsonUtf8.WriteStartArray("temp");
                jsonUtf8.WriteNullValue();
                jsonUtf8.WriteNullValue();
                jsonUtf8.WriteEndArray();

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                if (i < 3)
                    Assert.True(expectedStrNoEscape == actualStr, $"Case: {i}, | Expected: {expectedStrNoEscape}, | Actual: {actualStr}");
                else
                    Assert.True(expectedStr == actualStr, $"Case: {i}, | Expected: {expectedStr}, | Actual: {actualStr}");

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true, 0)]
        [InlineData(true, false, 0)]
        [InlineData(false, true, 0)]
        [InlineData(false, false, 0)]
        [InlineData(true, true, -1)]
        [InlineData(true, false, -1)]
        [InlineData(false, true, -1)]
        [InlineData(false, false, -1)]
        [InlineData(true, true, 1)]
        [InlineData(true, false, 1)]
        [InlineData(false, true, 1)]
        [InlineData(false, false, 1)]
        [InlineData(true, true, int.MaxValue)]
        [InlineData(true, false, int.MaxValue)]
        [InlineData(false, true, int.MaxValue)]
        [InlineData(false, false, int.MaxValue)]
        [InlineData(true, true, int.MinValue)]
        [InlineData(true, false, int.MinValue)]
        [InlineData(false, true, int.MinValue)]
        [InlineData(false, false, int.MinValue)]
        [InlineData(true, true, 12345)]
        [InlineData(true, false, 12345)]
        [InlineData(false, true, 12345)]
        [InlineData(false, false, 12345)]
        public void WriteIntegerValue(bool formatted, bool skipValidation, int value)
        {
            string expectedStr = GetIntegerExpectedString(prettyPrint: formatted, value);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            for (int i = 0; i < 3; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        jsonUtf8.WriteNumber("message", value, escape: false);
                        break;
                    case 1:
                        jsonUtf8.WriteNumber("message".AsSpan(), value, escape: false);
                        break;
                    case 2:
                        jsonUtf8.WriteNumber(Encoding.UTF8.GetBytes("message"), value, escape: false);
                        break;
                }

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                Assert.True(expectedStr == actualStr, $"Case: {i}, | Expected: {expectedStr}, | Actual: {actualStr}, | Value: {value}");

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true, "message")]
        [InlineData(true, false, "message")]
        [InlineData(false, true, "message")]
        [InlineData(false, false, "message")]
        [InlineData(true, true, "mess><age")]
        [InlineData(true, false, "mess><age")]
        [InlineData(false, true, "mess><age")]
        [InlineData(false, false, "mess><age")]
        [InlineData(true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void WriteNumbers(bool formatted, bool skipValidation, string keyString)
        {
            var random = new Random(42);
            const int numberOfItems = 1_000;

            var ints = new int[numberOfItems];
            ints[0] = 0;
            ints[1] = int.MaxValue;
            ints[2] = int.MinValue;
            ints[3] = 12345;
            ints[4] = -12345;
            for (int i = 5; i < numberOfItems; i++)
            {
                ints[i] = random.Next(int.MinValue, int.MaxValue);
            }

            var uints = new uint[numberOfItems];
            uints[0] = uint.MaxValue;
            uints[1] = uint.MinValue;
            uints[2] = 3294967295;
            for (int i = 3; i < numberOfItems; i++)
            {
                uint thirtyBits = (uint)random.Next(1 << 30);
                uint twoBits = (uint)random.Next(1 << 2);
                uint fullRange = (thirtyBits << 2) | twoBits;
                uints[i] = fullRange;
            }

            var longs = new long[numberOfItems];
            longs[0] = 0;
            longs[1] = long.MaxValue;
            longs[2] = long.MinValue;
            longs[3] = 12345678901;
            longs[4] = -12345678901;
            for (int i = 5; i < numberOfItems; i++)
            {
                long value = random.Next(int.MinValue, int.MaxValue);
                value += value < 0 ? int.MinValue : int.MaxValue;
                longs[i] = value;
            }

            var ulongs = new ulong[numberOfItems];
            ulongs[0] = ulong.MaxValue;
            ulongs[1] = ulong.MinValue;
            ulongs[2] = 10446744073709551615;
            for (int i = 3; i < numberOfItems; i++)
            {

            }

            var doubles = new double[numberOfItems * 2];
            doubles[0] = 0.00;
            doubles[1] = double.MaxValue;
            doubles[2] = double.MinValue;
            doubles[3] = 12.345e1;
            doubles[4] = -123.45e1;
            for (int i = 5; i < numberOfItems; i++)
            {
                var value = random.NextDouble();
                if (value < 0.5)
                {
                    doubles[i] = random.NextDouble() * double.MinValue;
                }
                else
                {
                    doubles[i] = random.NextDouble() * double.MaxValue;
                }
            }

            for (int i = numberOfItems; i < numberOfItems * 2; i++)
            {
                var value = random.NextDouble();
                if (value < 0.5)
                {
                    doubles[i] = random.NextDouble() * -1_000_000;
                }
                else
                {
                    doubles[i] = random.NextDouble() * 1_000_000;
                }
            }

            var floats = new float[numberOfItems];
            floats[0] = 0.00f;
            floats[1] = float.MaxValue;
            floats[2] = float.MinValue;
            floats[3] = 12.345e1f;
            floats[4] = -123.45e1f;
            for (int i = 5; i < numberOfItems; i++)
            {
                double mantissa = (random.NextDouble() * 2.0) - 1.0;
                double exponent = Math.Pow(2.0, random.Next(-126, 128));
                floats[i] = (float)(mantissa * exponent);
            }

            var decimals = new decimal[numberOfItems * 2];
            decimals[0] = (decimal)0.00;
            decimals[1] = decimal.MaxValue;
            decimals[2] = decimal.MinValue;
            decimals[3] = (decimal)12.345e1;
            decimals[4] = (decimal)-123.45e1;
            for (int i = 5; i < numberOfItems; i++)
            {
                var value = random.NextDouble();
                if (value < 0.5)
                {
                    decimals[i] = (decimal)(random.NextDouble() * -78E14);
                }
                else
                {
                    decimals[i] = (decimal)(random.NextDouble() * 78E14);
                }
            }

            for (int i = numberOfItems; i < numberOfItems * 2; i++)
            {
                var value = random.NextDouble();
                if (value < 0.5)
                {
                    decimals[i] = (decimal)(random.NextDouble() * -1_000_000);
                }
                else
                {
                    decimals[i] = (decimal)(random.NextDouble() * 1_000_000);
                }
            }

            string expectedStr = GetNumbersExpectedString(prettyPrint: formatted, keyString, ints, uints, longs, ulongs, floats, doubles, decimals, escape: false);
            string expectedStrNoEscape = GetNumbersExpectedString(prettyPrint: formatted, keyString, ints, uints, longs, ulongs, floats, doubles, decimals, escape: false);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            for (int j = 0; j < 6; j++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                ReadOnlySpan<char> keyUtf16 = keyString.AsSpan();
                ReadOnlySpan<byte> keyUtf8 = Encoding.UTF8.GetBytes(keyString);

                jsonUtf8.WriteStartObject();

                switch (j)
                {
                    case 0:
                        for (int i = 0; i < floats.Length; i++)
                            jsonUtf8.WriteNumber(keyString, floats[i], escape: false);
                        for (int i = 0; i < ints.Length; i++)
                            jsonUtf8.WriteNumber(keyString, ints[i], escape: false);
                        for (int i = 0; i < uints.Length; i++)
                            jsonUtf8.WriteNumber(keyString, uints[i], escape: false);
                        for (int i = 0; i < doubles.Length; i++)
                            jsonUtf8.WriteNumber(keyString, doubles[i], escape: false);
                        for (int i = 0; i < longs.Length; i++)
                            jsonUtf8.WriteNumber(keyString, longs[i], escape: false);
                        for (int i = 0; i < ulongs.Length; i++)
                            jsonUtf8.WriteNumber(keyString, ulongs[i], escape: false);
                        for (int i = 0; i < decimals.Length; i++)
                            jsonUtf8.WriteNumber(keyString, decimals[i], escape: false);
                        jsonUtf8.WriteStartArray(keyString, escape: false);
                        break;
                    case 1:
                        for (int i = 0; i < floats.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, floats[i], escape: false);
                        for (int i = 0; i < ints.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, ints[i], escape: false);
                        for (int i = 0; i < uints.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, uints[i], escape: false);
                        for (int i = 0; i < doubles.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, doubles[i], escape: false);
                        for (int i = 0; i < longs.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, longs[i], escape: false);
                        for (int i = 0; i < ulongs.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, ulongs[i], escape: false);
                        for (int i = 0; i < decimals.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, decimals[i], escape: false);
                        jsonUtf8.WriteStartArray(keyUtf16, escape: false);
                        break;
                    case 2:
                        for (int i = 0; i < floats.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, floats[i], escape: false);
                        for (int i = 0; i < ints.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, ints[i], escape: false);
                        for (int i = 0; i < uints.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, uints[i], escape: false);
                        for (int i = 0; i < doubles.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, doubles[i], escape: false);
                        for (int i = 0; i < longs.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, longs[i], escape: false);
                        for (int i = 0; i < ulongs.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, ulongs[i], escape: false);
                        for (int i = 0; i < decimals.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, decimals[i], escape: false);
                        jsonUtf8.WriteStartArray(keyUtf8, escape: false);
                        break;
                    case 3:
                        for (int i = 0; i < floats.Length; i++)
                            jsonUtf8.WriteNumber(keyString, floats[i], escape: true);
                        for (int i = 0; i < ints.Length; i++)
                            jsonUtf8.WriteNumber(keyString, ints[i], escape: true);
                        for (int i = 0; i < uints.Length; i++)
                            jsonUtf8.WriteNumber(keyString, uints[i], escape: true);
                        for (int i = 0; i < doubles.Length; i++)
                            jsonUtf8.WriteNumber(keyString, doubles[i], escape: true);
                        for (int i = 0; i < longs.Length; i++)
                            jsonUtf8.WriteNumber(keyString, longs[i], escape: true);
                        for (int i = 0; i < ulongs.Length; i++)
                            jsonUtf8.WriteNumber(keyString, ulongs[i], escape: true);
                        for (int i = 0; i < decimals.Length; i++)
                            jsonUtf8.WriteNumber(keyString, decimals[i], escape: true);
                        jsonUtf8.WriteStartArray(keyString, escape: true);
                        break;
                    case 4:
                        for (int i = 0; i < floats.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, floats[i], escape: true);
                        for (int i = 0; i < ints.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, ints[i], escape: true);
                        for (int i = 0; i < uints.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, uints[i], escape: true);
                        for (int i = 0; i < doubles.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, doubles[i], escape: true);
                        for (int i = 0; i < longs.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, longs[i], escape: true);
                        for (int i = 0; i < ulongs.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, ulongs[i], escape: true);
                        for (int i = 0; i < decimals.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf16, decimals[i], escape: true);
                        jsonUtf8.WriteStartArray(keyUtf16, escape: true);
                        break;
                    case 5:
                        for (int i = 0; i < floats.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, floats[i], escape: true);
                        for (int i = 0; i < ints.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, ints[i], escape: true);
                        for (int i = 0; i < uints.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, uints[i], escape: true);
                        for (int i = 0; i < doubles.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, doubles[i], escape: true);
                        for (int i = 0; i < longs.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, longs[i], escape: true);
                        for (int i = 0; i < ulongs.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, ulongs[i], escape: true);
                        for (int i = 0; i < decimals.Length; i++)
                            jsonUtf8.WriteNumber(keyUtf8, decimals[i], escape: true);
                        jsonUtf8.WriteStartArray(keyUtf8, escape: true);
                        break;
                }

                jsonUtf8.WriteNumberValue(floats[0]);
                jsonUtf8.WriteNumberValue(ints[0]);
                jsonUtf8.WriteNumberValue(uints[0]);
                jsonUtf8.WriteNumberValue(doubles[0]);
                jsonUtf8.WriteNumberValue(longs[0]);
                jsonUtf8.WriteNumberValue(ulongs[0]);
                jsonUtf8.WriteNumberValue(decimals[0]);
                jsonUtf8.WriteEndArray();

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                // TODO: The output doesn't match what JSON.NET does (different rounding/e-notation).
                //if (j < 3)
                //    Assert.Equal(expectedStrNoEscape, actualStr);
                //else
                //    Assert.Equal(expectedStr, actualStr);

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true, "message")]
        [InlineData(true, false, "message")]
        [InlineData(false, true, "message")]
        [InlineData(false, false, "message")]
        [InlineData(true, true, "mess><age")]
        [InlineData(true, false, "mess><age")]
        [InlineData(false, true, "mess><age")]
        [InlineData(false, false, "mess><age")]
        [InlineData(true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void WriteGuidsValue(bool formatted, bool skipValidation, string keyString)
        {
            const int numberOfItems = 1_000;

            var guids = new Guid[numberOfItems];
            for (int i = 0; i < numberOfItems; i++)
                guids[i] = Guid.NewGuid();

            string expectedStr = GetGuidsExpectedString(prettyPrint: formatted, keyString, guids, escape: true);
            string expectedStrNoEscape = GetGuidsExpectedString(prettyPrint: formatted, keyString, guids, escape: false);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            ReadOnlySpan<char> keyUtf16 = keyString.AsSpan();
            ReadOnlySpan<byte> keyUtf8 = Encoding.UTF8.GetBytes(keyString);

            for (int i = 0; i < 6; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyString, guids[j], escape: false);
                        jsonUtf8.WriteStartArray(keyString, escape: false);
                        break;
                    case 1:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf16, guids[j], escape: false);
                        jsonUtf8.WriteStartArray(keyUtf16, escape: false);
                        break;
                    case 2:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf8, guids[j], escape: false);
                        jsonUtf8.WriteStartArray(keyUtf8, escape: false);
                        break;
                    case 3:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyString, guids[j], escape: true);
                        jsonUtf8.WriteStartArray(keyString, escape: true);
                        break;
                    case 4:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf16, guids[j], escape: true);
                        jsonUtf8.WriteStartArray(keyUtf16, escape: true);
                        break;
                    case 5:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf8, guids[j], escape: true);
                        jsonUtf8.WriteStartArray(keyUtf8, escape: true);
                        break;
                }

                jsonUtf8.WriteStringValue(guids[0]);
                jsonUtf8.WriteStringValue(guids[1]);
                jsonUtf8.WriteEndArray();

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                if (i < 3)
                    Assert.Equal(expectedStrNoEscape, actualStr);
                else
                    Assert.Equal(expectedStr, actualStr);

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true, "message")]
        [InlineData(true, false, "message")]
        [InlineData(false, true, "message")]
        [InlineData(false, false, "message")]
        [InlineData(true, true, "mess><age")]
        [InlineData(true, false, "mess><age")]
        [InlineData(false, true, "mess><age")]
        [InlineData(false, false, "mess><age")]
        [InlineData(true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void WriteDateTimesValue(bool formatted, bool skipValidation, string keyString)
        {
            var random = new Random(42);
            const int numberOfItems = 1_000;

            var start = new DateTime(1995, 1, 1);
            int range = (DateTime.Today - start).Days;

            var dates = new DateTime[numberOfItems];
            for (int i = 0; i < numberOfItems; i++)
                dates[i] = start.AddDays(random.Next(range));

            string expectedStr = GetDatesExpectedString(prettyPrint: formatted, keyString, dates, escape: true);
            string expectedStrNoEscape = GetDatesExpectedString(prettyPrint: formatted, keyString, dates, escape: false);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            ReadOnlySpan<char> keyUtf16 = keyString.AsSpan();
            ReadOnlySpan<byte> keyUtf8 = Encoding.UTF8.GetBytes(keyString);

            for (int i = 0; i < 6; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyString, dates[j], escape: false);
                        jsonUtf8.WriteStartArray(keyString, escape: false);
                        break;
                    case 1:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf16, dates[j], escape: false);
                        jsonUtf8.WriteStartArray(keyUtf16, escape: false);
                        break;
                    case 2:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf8, dates[j], escape: false);
                        jsonUtf8.WriteStartArray(keyUtf8, escape: false);
                        break;
                    case 3:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyString, dates[j], escape: true);
                        jsonUtf8.WriteStartArray(keyString, escape: true);
                        break;
                    case 4:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf16, dates[j], escape: true);
                        jsonUtf8.WriteStartArray(keyUtf16, escape: true);
                        break;
                    case 5:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf8, dates[j], escape: true);
                        jsonUtf8.WriteStartArray(keyUtf8, escape: true);
                        break;
                }

                jsonUtf8.WriteStringValue(dates[0]);
                jsonUtf8.WriteStringValue(dates[1]);
                jsonUtf8.WriteEndArray();

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                if (i < 3)
                    Assert.Equal(expectedStrNoEscape, actualStr);
                else
                    Assert.Equal(expectedStr, actualStr);

                output.Dispose();
            }
        }

        [Theory]
        [InlineData(true, true, "message")]
        [InlineData(true, false, "message")]
        [InlineData(false, true, "message")]
        [InlineData(false, false, "message")]
        [InlineData(true, true, "mess><age")]
        [InlineData(true, false, "mess><age")]
        [InlineData(false, true, "mess><age")]
        [InlineData(false, false, "mess><age")]
        [InlineData(true, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(true, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, true, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        [InlineData(false, false, ">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>")]
        public void WriteDateTimeOffsetsValue(bool formatted, bool skipValidation, string keyString)
        {
            var random = new Random(42);
            const int numberOfItems = 1_000;

            var start = new DateTime(1995, 1, 1);
            int range = (DateTime.Today - start).Days;

            var dates = new DateTimeOffset[numberOfItems];
            for (int i = 0; i < numberOfItems; i++)
                dates[i] = new DateTimeOffset(start.AddDays(random.Next(range)));

            string expectedStr = GetDatesExpectedString(prettyPrint: formatted, keyString, dates, escape: true);
            string expectedStrNoEscape = GetDatesExpectedString(prettyPrint: formatted, keyString, dates, escape: false);

            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            ReadOnlySpan<char> keyUtf16 = keyString.AsSpan();
            ReadOnlySpan<byte> keyUtf8 = Encoding.UTF8.GetBytes(keyString);

            for (int i = 0; i < 6; i++)
            {
                var output = new ArrayBufferWriter(1024);
                var jsonUtf8 = new Utf8JsonWriter(output, state);

                jsonUtf8.WriteStartObject();

                switch (i)
                {
                    case 0:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyString, dates[j], escape: false);
                        jsonUtf8.WriteStartArray(keyString, escape: false);
                        break;
                    case 1:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf16, dates[j], escape: false);
                        jsonUtf8.WriteStartArray(keyUtf16, escape: false);
                        break;
                    case 2:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf8, dates[j], escape: false);
                        jsonUtf8.WriteStartArray(keyUtf8, escape: false);
                        break;
                    case 3:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyString, dates[j], escape: true);
                        jsonUtf8.WriteStartArray(keyString, escape: true);
                        break;
                    case 4:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf16, dates[j], escape: true);
                        jsonUtf8.WriteStartArray(keyUtf16, escape: true);
                        break;
                    case 5:
                        for (int j = 0; j < numberOfItems; j++)
                            jsonUtf8.WriteString(keyUtf8, dates[j], escape: true);
                        jsonUtf8.WriteStartArray(keyUtf8, escape: true);
                        break;
                }

                jsonUtf8.WriteStringValue(dates[0]);
                jsonUtf8.WriteStringValue(dates[1]);
                jsonUtf8.WriteEndArray();

                jsonUtf8.WriteEndObject();
                jsonUtf8.Flush();

                ArraySegment<byte> arraySegment = output.Formatted;
                string actualStr = Encoding.UTF8.GetString(arraySegment.Array, arraySegment.Offset, arraySegment.Count);

                if (i < 3)
                    Assert.Equal(expectedStrNoEscape, actualStr);
                else
                    Assert.Equal(expectedStr, actualStr);

                output.Dispose();
            }
        }

        [ConditionalTheory(nameof(IsX64))]
        [OuterLoop]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteLargeKeyOrValue(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            Span<byte> key;
            Span<byte> value;

            try
            {
                key = new byte[1_000_000_001];
                value = new byte[1_000_000_001];
            }
            catch (OutOfMemoryException)
            {
                return;
            }

            key.Fill((byte)'a');
            value.Fill((byte)'b');

            var output = new ArrayBufferWriter(1024);

            try
            {
                var jsonUtf8 = new Utf8JsonWriter(output, state);
                jsonUtf8.WriteStartObject();
                jsonUtf8.WriteString(key, DateTime.Now, escape: false);
                Assert.True(false, $"Expected ArgumentException for data too large wasn't thrown. KeyLength: {key.Length}");
            }
            catch (ArgumentException) { }

            try
            {
                var jsonUtf8 = new Utf8JsonWriter(output, state);
                jsonUtf8.WriteStartArray();
                jsonUtf8.WriteStringValue(value, escape: false);
                Assert.True(false, $"Expected ArgumentException for data too large wasn't thrown. ValueLength: {value.Length}");
            }
            catch (ArgumentException) { }

            output.Dispose();
        }

        [ConditionalTheory(nameof(IsX64))]
        [OuterLoop]
        [InlineData(true, true)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(false, false)]
        public void WriteLargeKeyValue(bool formatted, bool skipValidation)
        {
            var state = new JsonWriterState(options: new JsonWriterOptions { Indented = formatted, SkipValidation = skipValidation });

            Span<byte> key;
            Span<byte> value;

            try
            {
                key = new byte[1_000_000_001];
                value = new byte[1_000_000_001];
            }
            catch (OutOfMemoryException)
            {
                return;
            }

            WriteTooLargeHelper(state, key, value);
            WriteTooLargeHelper(state, key.Slice(0, 1_000_000_000), value);
            WriteTooLargeHelper(state, key, value.Slice(0, 1_000_000_000));
            WriteTooLargeHelper(state, key.Slice(0, 10_000_000 / 3), value.Slice(0, 10_000_000 / 3), noThrow: true);
        }

        private static void WriteTooLargeHelper(JsonWriterState state, ReadOnlySpan<byte> key, ReadOnlySpan<byte> value, bool noThrow = false)
        {
            var output = new ArrayBufferWriter(1024);
            var jsonUtf8 = new Utf8JsonWriter(output, state);

            jsonUtf8.WriteStartObject();

            try
            {
                jsonUtf8.WriteString(key, value, escape: false);

                if (!noThrow)
                {
                    Assert.True(false, $"Expected ArgumentException for data too large wasn't thrown. KeyLength: {key.Length} | ValueLength: {value.Length}");
                }
            }
            catch (ArgumentException)
            {
                if (noThrow)
                {
                    Assert.True(false, $"Expected writing large key/value to succeed. KeyLength: {key.Length} | ValueLength: {value.Length}");
                }
            }

            jsonUtf8.WriteEndObject();
            jsonUtf8.Flush();

            output.Dispose();
        }

        private static void WriterDidNotThrow(bool skipValidation)
        {
            if (skipValidation)
                Assert.True(true, "Did not expect InvalidOperationException to be thrown since validation was skipped.");
            else
                Assert.True(false, "Expected InvalidOperationException to be thrown when validation is enabled.");
        }

        private static void WriterDidNotThrow(bool skipValidation, string message)
        {
            if (skipValidation)
                Assert.True(true, message);
            else
                Assert.True(false, message);
        }

        private static void AssertWriterThrow(bool noThrow)
        {
            if (noThrow)
                Assert.True(true, "Did not expect InvalidOperationException to be thrown since input was valid (or suppressEscaping was true).");
            else
                Assert.True(false, "Expected InvalidOperationException to be thrown when user passes invalid UTF-8.");
        }

        private static string GetHelloWorldExpectedString(bool prettyPrint)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartObject();
            json.WritePropertyName("message");
            json.WriteValue("Hello, World!");
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetCommentExpectedString(bool prettyPrint, string comment)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            };

            json.WriteStartArray();
            for (int j = 0; j < 10; j++)
                json.WriteComment(comment);
            json.WriteValue(comment);
            json.WriteComment(comment);
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetStringsExpectedString(bool prettyPrint, string value)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartArray();
            for (int j = 0; j < 10; j++)
                json.WriteValue(value);
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetEscapedExpectedString(bool prettyPrint, string propertyName, string value, StringEscapeHandling escaping, bool escape = true)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = escaping
            };

            json.WriteStartObject();
            json.WritePropertyName(propertyName, escape);
            json.WriteValue(value);
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetCustomExpectedString(bool prettyPrint)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartObject();
            for (int i = 0; i < 1_000; i++)
            {
                json.WritePropertyName("message");
                json.WriteValue("Hello, World!");
            }
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetStartEndExpectedString(bool prettyPrint)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartArray();
            json.WriteStartObject();
            json.WriteEnd();
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetStartEndWithPropertyArrayExpectedString(bool prettyPrint)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartObject();
            json.WritePropertyName("property name");
            json.WriteStartArray();
            json.WriteEnd();
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetStartEndWithPropertyArrayExpectedString(string key, bool prettyPrint, bool escape = false)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml
            };

            json.WriteStartObject();
            json.WritePropertyName(key, escape);
            json.WriteStartArray();
            json.WriteEnd();
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetStartEndWithPropertyObjectExpectedString(bool prettyPrint)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartObject();
            json.WritePropertyName("property name");
            json.WriteStartObject();
            json.WriteEnd();
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetStartEndWithPropertyObjectExpectedString(string key, bool prettyPrint, bool escape = false)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml
            };

            json.WriteStartObject();
            json.WritePropertyName(key, escape);
            json.WriteStartObject();
            json.WriteEnd();
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetArrayWithPropertyExpectedString(bool prettyPrint)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartObject();
            json.WritePropertyName("message");
            json.WriteStartArray();
            json.WriteEndArray();
            json.WriteEndObject();
            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetBooleanExpectedString(bool prettyPrint, string keyString, bool value, bool escape = false)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            };

            json.WriteStartObject();
            json.WritePropertyName(keyString, escape);
            json.WriteValue(value);

            json.WritePropertyName("temp");
            json.WriteStartArray();
            json.WriteValue(true);
            json.WriteValue(true);
            json.WriteValue(false);
            json.WriteValue(false);
            json.WriteEnd();

            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetNullExpectedString(bool prettyPrint, string keyString, bool escape = false)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml,
            };

            json.WriteStartObject();
            json.WritePropertyName(keyString, escape);
            json.WriteNull();

            json.WritePropertyName("temp");
            json.WriteStartArray();
            json.WriteValue((string)null);
            json.WriteValue((string)null);
            json.WriteEnd();

            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetIntegerExpectedString(bool prettyPrint, int value)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartObject();
            json.WritePropertyName("message");
            json.WriteValue(value);
            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetNumbersExpectedString(bool prettyPrint, string keyString, int[] ints, uint[] uints, long[] longs, ulong[] ulongs, float[] floats, double[] doubles, decimal[] decimals, bool escape = false)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None
            };

            json.WriteStartObject();

            for (int i = 0; i < floats.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(floats[i]);
            }
            for (int i = 0; i < ints.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(ints[i]);
            }
            for (int i = 0; i < uints.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(uints[i]);
            }
            for (int i = 0; i < doubles.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(doubles[i]);
            }
            for (int i = 0; i < longs.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(longs[i]);
            }
            for (int i = 0; i < ulongs.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(ulongs[i]);
            }
            for (int i = 0; i < decimals.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(decimals[i]);
            }

            json.WritePropertyName(keyString, escape);
            json.WriteStartArray();
            json.WriteValue(floats[0]);
            json.WriteValue(ints[0]);
            json.WriteValue(uints[0]);
            json.WriteValue(doubles[0]);
            json.WriteValue(longs[0]);
            json.WriteValue(ulongs[0]);
            json.WriteValue(decimals[0]);
            json.WriteEndArray();

            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetGuidsExpectedString(bool prettyPrint, string keyString, Guid[] guids, bool escape = false)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml
            };

            json.WriteStartObject();

            for (int i = 0; i < guids.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(guids[i]);
            }

            json.WritePropertyName(keyString, escape);
            json.WriteStartArray();
            json.WriteValue(guids[0]);
            json.WriteValue(guids[1]);
            json.WriteEnd();

            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetDatesExpectedString(bool prettyPrint, string keyString, DateTime[] dates, bool escape = false)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml,
                DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffffff"
            };

            json.WriteStartObject();

            for (int i = 0; i < dates.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(dates[i]);
            }

            json.WritePropertyName(keyString, escape);
            json.WriteStartArray();
            json.WriteValue(dates[0]);
            json.WriteValue(dates[1]);
            json.WriteEnd();

            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static string GetDatesExpectedString(bool prettyPrint, string keyString, DateTimeOffset[] dates, bool escape = false)
        {
            MemoryStream ms = new MemoryStream();
            TextWriter streamWriter = new StreamWriter(ms, new UTF8Encoding(false), 1024, true);

            var json = new JsonTextWriter(streamWriter)
            {
                Formatting = prettyPrint ? Formatting.Indented : Formatting.None,
                StringEscapeHandling = StringEscapeHandling.EscapeHtml,
                DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffffffzzz"
            };

            json.WriteStartObject();

            for (int i = 0; i < dates.Length; i++)
            {
                json.WritePropertyName(keyString, escape);
                json.WriteValue(dates[i]);
            }

            json.WritePropertyName(keyString, escape);
            json.WriteStartArray();
            json.WriteValue(dates[0]);
            json.WriteValue(dates[1]);
            json.WriteEnd();

            json.WriteEnd();

            json.Flush();

            return Encoding.UTF8.GetString(ms.ToArray());
        }
    }
}

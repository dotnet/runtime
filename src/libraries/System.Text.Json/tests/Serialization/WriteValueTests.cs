// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text.Encodings.Web;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class WriteValueTests
    {
        public static bool IsX64 { get; } = IntPtr.Size >= 8;

        [Fact]
        public static void NullWriterThrows()
        {
            ArgumentNullException ex;

            ex = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(null, 1));
            Assert.Contains("writer", ex.ToString());

            ex = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(null, 1, typeof(int)));
            Assert.Contains("writer", ex.ToString());
        }

        [Fact]
        public async static void NullInputTypeThrows()
        {
            ArgumentException ex;
            Utf8JsonWriter writer = new Utf8JsonWriter(new MemoryStream());

            ex = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(writer: writer, value: null, inputType: null));
            Assert.Contains("inputType", ex.ToString());

            ex = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(writer, value: null, inputType: null));
            Assert.Contains("inputType", ex.ToString());

            ex = Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(1, inputType: null));
            Assert.Contains("inputType", ex.ToString());

            ex = Assert.Throws<ArgumentNullException>(() => JsonSerializer.SerializeToUtf8Bytes(null, inputType: null));
            Assert.Contains("inputType", ex.ToString());

            ex = await Assert.ThrowsAsync<ArgumentNullException>(async () => await JsonSerializer.SerializeAsync(new MemoryStream(), null, inputType: null));
            Assert.Contains("inputType", ex.ToString());
        }

        [Fact]
        public async static void NullValueWithValueTypeThrows()
        {
            JsonException ex;

            Utf8JsonWriter writer = new Utf8JsonWriter(new MemoryStream());
            ex = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(writer: writer, value: null, inputType: typeof(int)));
            Assert.Contains(typeof(int).ToString(), ex.ToString());

            ex = Assert.Throws<JsonException>(() => JsonSerializer.Serialize(value: null, inputType: typeof(int)));
            Assert.Contains(typeof(int).ToString(), ex.ToString());

            ex = Assert.Throws<JsonException>(() => JsonSerializer.SerializeToUtf8Bytes(value: null, inputType: typeof(int)));
            Assert.Contains(typeof(int).ToString(), ex.ToString());

            ex = await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializer.SerializeAsync(new MemoryStream(), value: null, inputType: typeof(int)));
            Assert.Contains(typeof(int).ToString(), ex.ToString());
        }

        [Fact]
        public async static void NullValueWithNullableSuccess()
        {
            byte[] nullUtf8Literal = Encoding.UTF8.GetBytes("null");

            var stream = new MemoryStream();
            Utf8JsonWriter writer = new Utf8JsonWriter(stream);
            JsonSerializer.Serialize(writer: writer, value: null, inputType: typeof(int?));
            byte[] jsonBytes = stream.ToArray();
            Assert.Equal(nullUtf8Literal, jsonBytes);

            string jsonString = JsonSerializer.Serialize(value: null, inputType: typeof(int?));
            Assert.Equal("null", jsonString);

            jsonBytes = JsonSerializer.SerializeToUtf8Bytes(value: null, inputType: typeof(int?));
            Assert.Equal(nullUtf8Literal, jsonBytes);

            stream = new MemoryStream();
            await JsonSerializer.SerializeAsync(stream, value: null, inputType: typeof(int?));
            Assert.Equal(nullUtf8Literal, stream.ToArray());
        }

        [Fact]
        public static void CanWriteValueToJsonArray()
        {
            using MemoryStream memoryStream = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(memoryStream);

            writer.WriteStartObject();
            writer.WriteStartArray("test");
            JsonSerializer.Serialize<int>(writer, 1);
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.Flush();

            string json = Encoding.UTF8.GetString(memoryStream.ToArray());
            Assert.Equal("{\"test\":[1]}", json);
        }

        public class CustomClassWithEscapedProperty
        {
            public int pizza { get; set; }
            public int hello\u6C49\u5B57 { get; set; }
            public int normal { get; set; }
        }

        [Fact]
        public static void SerializeToWriterRoundTripEscaping()
        {
            const string jsonIn = " { \"p\\u0069zza\": 1, \"hello\\u6C49\\u5B57\": 2, \"normal\": 3 }";

            CustomClassWithEscapedProperty input = JsonSerializer.Deserialize<CustomClassWithEscapedProperty>(jsonIn);

            Assert.Equal(1, input.pizza);
            Assert.Equal(2, input.hello\u6C49\u5B57);
            Assert.Equal(3, input.normal);

            string normalizedString = JsonSerializer.Serialize(input);
            Assert.Equal("{\"pizza\":1,\"hello\\u6C49\\u5B57\":2,\"normal\":3}", normalizedString);

            CustomClassWithEscapedProperty inputNormalized = JsonSerializer.Deserialize<CustomClassWithEscapedProperty>(normalizedString);
            Assert.Equal(1, inputNormalized.pizza);
            Assert.Equal(2, inputNormalized.hello\u6C49\u5B57);
            Assert.Equal(3, inputNormalized.normal);

            using MemoryStream memoryStream = new MemoryStream();
            using Utf8JsonWriter writer = new Utf8JsonWriter(memoryStream);
            JsonSerializer.Serialize(writer, inputNormalized);
            writer.Flush();

            string json = Encoding.UTF8.GetString(memoryStream.ToArray());

            Assert.Equal(normalizedString, json);
        }

        [Fact]
        public static void WriterOptionsWinIndented()
        {
            int[] input = new int[3] { 1, 2, 3 };

            var serializerOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
            };

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    JsonSerializer.Serialize(writer, input, serializerOptions);
                }
                Assert.Equal("[1,2,3]", Encoding.UTF8.GetString(stream.ToArray()));
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
                {
                    JsonSerializer.Serialize(writer, input, serializerOptions);
                }
                Assert.Equal("[1,2,3]", Encoding.UTF8.GetString(stream.ToArray()));
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    JsonSerializer.Serialize(writer, input);
                }
                Assert.Equal($"[{Environment.NewLine}  1,{Environment.NewLine}  2,{Environment.NewLine}  3{Environment.NewLine}]", Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        [Fact]
        public static void WriterOptionsWinEncoder()
        {
            string input = "abcd+<>&";

            var serializerOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            };

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    JsonSerializer.Serialize(writer, input, serializerOptions);
                }
                Assert.Equal("\"abcd\\u002B\\u003C\\u003E\\u0026\"", Encoding.UTF8.GetString(stream.ToArray()));
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.Default }))
                {
                    JsonSerializer.Serialize(writer, input, serializerOptions);
                }
                Assert.Equal("\"abcd\\u002B\\u003C\\u003E\\u0026\"", Encoding.UTF8.GetString(stream.ToArray()));
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
                {
                    JsonSerializer.Serialize(writer, input);
                }
                Assert.Equal("\"abcd+<>&\"", Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        [Fact]
        public static void WriterOptionsSkipValidation()
        {
            int[] input = new int[3] { 1, 2, 3 };

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    writer.WriteStartObject();
                    Assert.Throws<JsonException>(() => JsonSerializer.Serialize(writer, input));
                }
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { SkipValidation = true }))
                {
                    writer.WriteStartObject();
                    JsonSerializer.Serialize(writer, input);
                }
                Assert.Equal("{[1,2,3]", Encoding.UTF8.GetString(stream.ToArray()));
            }

            var serializerOptions = new JsonSerializerOptions
            {
                Converters = { new InvalidArrayConverter() },
            };

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    Assert.Throws<JsonException>(() => JsonSerializer.Serialize(writer, input, serializerOptions));
                }
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { SkipValidation = true }))
                {
                    JsonSerializer.Serialize(writer, input, serializerOptions);
                }
                Assert.Equal("[}", Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        public class InvalidArrayConverter : JsonConverter<int[]>
        {
            public override int[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, int[] value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                writer.WriteEndObject();
            }
        }

        [Fact]
        public static void OptionsFollowToConverter()
        {
            string json = "{\"type\":\"array\",\"array\":[1]}";
            string jsonFormatted =
@"{
  ""type"": ""array"",
  ""array"": [
    1
  ]
}";
            string expectedInner = "{\"array\":[1]}";

            var tempOptions = new JsonSerializerOptions();
            tempOptions.Converters.Add(new CustomConverter());
            DeepArray direct = JsonSerializer.Deserialize<DeepArray>(json, tempOptions);
            IContent custom = JsonSerializer.Deserialize<IContent>(json, tempOptions);

            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new CustomConverter());

                Assert.Equal(expectedInner, JsonSerializer.Serialize(direct, options));
                Assert.Equal(json, JsonSerializer.Serialize(custom, options));
            }

            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new CustomConverter() }
                };
                WriteAndValidate(direct, typeof(DeepArray), expectedInner, options, writerOptions: default);
                WriteAndValidate(custom, typeof(IContent), json, options, writerOptions: default);
            }

            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new CustomConverter() }
                };
                var writerOptions = new JsonWriterOptions { Indented = false };
                WriteAndValidate(direct, typeof(DeepArray), expectedInner, options, writerOptions);
                WriteAndValidate(custom, typeof(IContent), json, options, writerOptions);
            }

            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Converters = { new CustomConverter() }
                };
                WriteAndValidate(direct, typeof(DeepArray), expectedInner, options, writerOptions: default);
                WriteAndValidate(custom, typeof(IContent), json, options, writerOptions: default);
            }

            {
                var options = new JsonSerializerOptions
                {
                    Converters = { new CustomConverter() }
                };
                var writerOptions = new JsonWriterOptions { Indented = true };
                WriteAndValidate(direct, typeof(DeepArray), $"{{{Environment.NewLine}  \"array\": [{Environment.NewLine}    1{Environment.NewLine}  ]{Environment.NewLine}}}", options, writerOptions);
                WriteAndValidate(custom, typeof(IContent), jsonFormatted, options, writerOptions);
            }

            static void WriteAndValidate(object input, Type type, string expected, JsonSerializerOptions options, JsonWriterOptions writerOptions)
            {
                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream, writerOptions))
                    {
                        JsonSerializer.Serialize(writer, input, type, options);
                    }
                    Assert.Equal(expected, Encoding.UTF8.GetString(stream.ToArray()));
                }
            }
        }

        public class CustomClassToExceedMaxBufferSize
        {
            private static readonly string s_name = new string('a', 100_000_000);//Large enough value to cause integer overflow exception when allocating buffer and small enought to not cause a "The JSON value of length X is too large and not supported."
            public string GetName1 => s_name;
            public string GetName2 => s_name;
            public string GetName3 => s_name;
            public string GetName4 => s_name;
            public string GetName5 => s_name;
            public string GetName6 => s_name;
            public string GetName7 => s_name;
            public string GetName8 => s_name;
            public string GetName9 => s_name;
            public string GetName10 => s_name;
            public string GetName11 => s_name;
            public string GetName12 => s_name;
            public string GetName13 => s_name;
            public string GetName14 => s_name;
            public string GetName15 => s_name;
            public string GetName16 => s_name;
            public string GetName17 => s_name;
            public string GetName18 => s_name;
            public string GetName19 => s_name;
            public string GetName20 => s_name;
        }

        // NOTE: SerializeExceedMaximumBufferSize test is constrained to run on Windows and MacOSX because it causes 
        //       problems on Linux due to the way deferred memory allocation works. On Linux, the allocation can 
        //       succeed even if there is not enough memory but then the test may get killed by the OOM killer at the 
        //       time the memory is accessed which triggers the full memory allocation. 
        [PlatformSpecific(TestPlatforms.Windows | TestPlatforms.OSX)]
        [ConditionalFact(nameof(IsX64))]
        [OuterLoop]
        public static void SerializeExceedMaximumBufferSize()
        {
            CustomClassToExceedMaxBufferSize temp = new CustomClassToExceedMaxBufferSize();

            Assert.Throws<OutOfMemoryException>(() => JsonSerializer.Serialize(temp, typeof(CustomClassToExceedMaxBufferSize)));
        }
    }
}

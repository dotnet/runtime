// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Text.Encodings.Web;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class WriteValueTests
    {
        [Fact]
        public static void NullWriterThrows()
        {
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(null, 1));
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(null, 1, typeof(int)));
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
            var input = new int[3] { 1, 2, 3 };

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
                Assert.Equal("[1, 2,3]", Encoding.UTF8.GetString(stream.ToArray()));
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
                {
                    JsonSerializer.Serialize(writer, input, serializerOptions);
                }
                Assert.Equal("[1, 2,3]", Encoding.UTF8.GetString(stream.ToArray()));
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                {
                    JsonSerializer.Serialize(writer, input);
                }
                Assert.Equal("[1, 2,3]", Encoding.UTF8.GetString(stream.ToArray()));
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
                Assert.Equal("abcd+<>&", Encoding.UTF8.GetString(stream.ToArray()));
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.Default }))
                {
                    JsonSerializer.Serialize(writer, input, serializerOptions);
                }
                Assert.Equal("abcd+<>&", Encoding.UTF8.GetString(stream.ToArray()));
            }

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping }))
                {
                    JsonSerializer.Serialize(writer, input);
                }
                Assert.Equal("abcd+<>&", Encoding.UTF8.GetString(stream.ToArray()));
            }
        }

        [Fact]
        public static void WriterOptionsSkipValidation()
        {
            var input = new int[3] { 1, 2, 3 };

            var serializerOptions = new JsonSerializerOptions
            {
                Converters = { new InvalidArrayConverter() },
            };

            using (var stream = new MemoryStream())
            {
                using (var writer = new Utf8JsonWriter(stream))
                {
                    Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize(writer, input, serializerOptions));
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
  ""array"": [1]
}";

            var tempOptions = new JsonSerializerOptions();
            tempOptions.Converters.Add(new CustomConverter());
            DeepArray direct = JsonSerializer.Deserialize<DeepArray>(json, tempOptions);
            IContent custom = JsonSerializer.Deserialize<IContent>(json, tempOptions);

            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new CustomConverter());

                Assert.Equal(json, JsonSerializer.Serialize(direct, options));
                Assert.Equal(json, JsonSerializer.Serialize(custom, options));
            }

            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new CustomConverter());

                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream))
                    {
                        JsonSerializer.Serialize(writer, direct, options);
                    }
                    Assert.Equal(json, Encoding.UTF8.GetString(stream.ToArray()));
                }

                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream))
                    {
                        JsonSerializer.Serialize(writer, custom, options);
                    }
                    Assert.Equal(json, Encoding.UTF8.GetString(stream.ToArray()));
                }
            }

            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new CustomConverter());

                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
                    {
                        JsonSerializer.Serialize(writer, direct, options);
                    }
                    Assert.Equal(json, Encoding.UTF8.GetString(stream.ToArray()));
                }

                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = false }))
                    {
                        JsonSerializer.Serialize(writer, custom, options);
                    }
                    Assert.Equal(json, Encoding.UTF8.GetString(stream.ToArray()));
                }
            }

            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                options.Converters.Add(new CustomConverter());

                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream))
                    {
                        JsonSerializer.Serialize(writer, direct, options);
                    }
                    Assert.Equal(jsonFormatted, Encoding.UTF8.GetString(stream.ToArray()));
                }

                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream))
                    {
                        JsonSerializer.Serialize(writer, custom, options);
                    }
                    Assert.Equal(jsonFormatted, Encoding.UTF8.GetString(stream.ToArray()));
                }
            }

            {
                var options = new JsonSerializerOptions();
                options.Converters.Add(new CustomConverter());

                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                    {
                        JsonSerializer.Serialize(writer, direct, options);
                    }
                    Assert.Equal(jsonFormatted, Encoding.UTF8.GetString(stream.ToArray()));
                }

                using (var stream = new MemoryStream())
                {
                    using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true }))
                    {
                        JsonSerializer.Serialize(writer, custom, options);
                    }
                    Assert.Equal(jsonFormatted, Encoding.UTF8.GetString(stream.ToArray()));
                }
            }
        }
    }
}

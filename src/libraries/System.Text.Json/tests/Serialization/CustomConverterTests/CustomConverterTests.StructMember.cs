// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        private class StructToInterfaceConverter : JsonConverter<IMemberInterface>
        {
            public int ReadCallCount { get; private set; }
            public int WriteCallCount { get; private set; }

            public override bool HandleNull => true;

            public override bool CanConvert(Type typeToConvert)
            {
                typeToConvert = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
                return typeof(IMemberInterface).IsAssignableFrom(typeToConvert);
            }

            public override IMemberInterface Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                ReadCallCount++;

                string value = reader.GetString();

                return new StructMember(value);
            }

            public override void Write(Utf8JsonWriter writer, IMemberInterface value, JsonSerializerOptions options)
            {
                WriteCallCount++;

                JsonSerializer.Serialize(writer, value.Value, typeof(string), options);
            }
        }

        private class StructToObjectConverter : JsonConverter<object>
        {
            public int ReadCallCount { get; private set; }
            public int WriteCallCount { get; private set; }

            public override bool HandleNull => true;

            public override bool CanConvert(Type typeToConvert)
            {
                return typeof(IMemberInterface).IsAssignableFrom(typeToConvert);
            }

            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                ReadCallCount++;

                string value = reader.GetString();

                return new StructMember(value);
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                WriteCallCount++;

                JsonSerializer.Serialize(writer, ((IMemberInterface)value).Value, typeof(string), options);
            }
        }

        [Fact]
        public static void StructMemberToInterfaceConverter()
        {
            const string expected = @"{""MyStructProperty"":""StructProperty"",""MyClassProperty"":""ClassProperty"",""MyStructField"":""StructField"",""MyClassField"":""ClassField""}";

            var converter = new StructToInterfaceConverter();
            var options = new JsonSerializerOptions()
            {
                IncludeFields = true,
            };
            options.Converters.Add(converter);

            string json;

            {
                var obj = new TestClassWithStructMember();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj, options);

                Assert.Equal(4, converter.WriteCallCount);
                Assert.Equal(expected, json);
            }

            {
                var obj = JsonSerializer.Deserialize<TestClassWithStructMember>(json, options);
                obj.Verify();

                Assert.Equal(4, converter.ReadCallCount);
            }
        }

        [Fact]
        public static void StructMemberToObjectConverter()
        {
            const string expected = @"{""MyStructProperty"":""StructProperty"",""MyClassProperty"":""ClassProperty"",""MyStructField"":""StructField"",""MyClassField"":""ClassField""}";

            var converter = new StructToObjectConverter();
            var options = new JsonSerializerOptions()
            {
                IncludeFields = true,
            };
            options.Converters.Add(converter);

            string json;

            {
                var obj = new TestClassWithStructMember();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj, options);

                Assert.Equal(4, converter.WriteCallCount);
                Assert.Equal(expected, json);
            }

            {
                var obj = JsonSerializer.Deserialize<TestClassWithStructMember>(json, options);
                obj.Verify();

                Assert.Equal(4, converter.ReadCallCount);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36329")]
        public static void NullableStructMemberToInterfaceConverter()
        {
            const string expected = @"{""MyStructProperty"":""StructProperty"",""MyClassProperty"":""ClassProperty"",""MyStructField"":""StructField"",""MyClassField"":""ClassField""}";

            var converter = new StructToInterfaceConverter();
            var options = new JsonSerializerOptions()
            {
                IncludeFields = true,
            };
            options.Converters.Add(converter);

            string json;

            {
                var obj = new TestClassWithNullableStructMember();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj, options);

                Assert.Equal(4, converter.WriteCallCount);
                Assert.Equal(expected, json);
            }

            {
                var obj = JsonSerializer.Deserialize<TestClassWithNullableStructMember>(json, options);
                obj.Verify();

                Assert.Equal(4, converter.ReadCallCount);
            }
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/36329")]
        public static void NullableStructMemberToObjectConverter()
        {
            const string expected = @"{""MyStructProperty"":""StructProperty"",""MyClassProperty"":""ClassProperty"",""MyStructField"":""StructField"",""MyClassField"":""ClassField""}";

            var converter = new StructToObjectConverter();
            var options = new JsonSerializerOptions()
            {
                IncludeFields = true,
            };
            options.Converters.Add(converter);

            string json;

            {
                var obj = new TestClassWithNullableStructMember();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj, options);

                Assert.Equal(4, converter.WriteCallCount);
                Assert.Equal(expected, json);
            }

            {
                var obj = JsonSerializer.Deserialize<TestClassWithNullableStructMember>(json, options);
                obj.Verify();

                Assert.Equal(4, converter.ReadCallCount);
            }
        }

    }
}

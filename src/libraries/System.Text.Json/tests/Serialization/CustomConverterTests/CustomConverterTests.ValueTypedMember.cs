// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        private class ValueTypeToInterfaceConverter : JsonConverter<IMemberInterface>
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

                if (value == null)
                {
                    return null;
                }

                return value.IndexOf("ValueTyped", StringComparison.Ordinal) >= 0
                    ? new ValueTypedMember(value)
                    : new RefTypedMember(value);
            }

            public override void Write(Utf8JsonWriter writer, IMemberInterface value, JsonSerializerOptions options)
            {
                WriteCallCount++;

                JsonSerializer.Serialize<string>(writer, value == null ? null : value.Value, options);
            }
        }

        private class ValueTypeToObjectConverter : JsonConverter<object>
        {
            public int ReadCallCount { get; private set; }
            public int WriteCallCount { get; private set; }

            public override bool HandleNull => true;

            public override bool CanConvert(Type typeToConvert)
            {
                typeToConvert = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
                return typeof(IMemberInterface).IsAssignableFrom(typeToConvert);
            }

            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                ReadCallCount++;

                string value = reader.GetString();

                if (value == null)
                {
                    return null;
                }

                return value.IndexOf("ValueTyped", StringComparison.Ordinal) >= 0
                    ? new ValueTypedMember(value)
                    : new RefTypedMember(value);
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                WriteCallCount++;

                JsonSerializer.Serialize<string>(writer, value == null ? null : ((IMemberInterface)value).Value, options);
            }
        }

        [Fact]
        public static void ValueTypedMemberToInterfaceConverter()
        {
            const string expected = @"{""MyValueTypedProperty"":""ValueTypedProperty"",""MyRefTypedProperty"":""RefTypedProperty"",""MyValueTypedField"":""ValueTypedField"",""MyRefTypedField"":""RefTypedField""}";

            var converter = new ValueTypeToInterfaceConverter();
            var options = new JsonSerializerOptions()
            {
                IncludeFields = true,
            };
            options.Converters.Add(converter);

            string json;

            {
                var obj = new TestClassWithValueTypedMember();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj, options);

                Assert.Equal(4, converter.WriteCallCount);
                Assert.Equal(expected, json);
            }

            {
                var obj = JsonSerializer.Deserialize<TestClassWithValueTypedMember>(json, options);
                obj.Verify();

                Assert.Equal(4, converter.ReadCallCount);
            }
        }

        [Fact]
        public static void ValueTypedMemberToObjectConverter()
        {
            const string expected = @"{""MyValueTypedProperty"":""ValueTypedProperty"",""MyRefTypedProperty"":""RefTypedProperty"",""MyValueTypedField"":""ValueTypedField"",""MyRefTypedField"":""RefTypedField""}";

            var converter = new ValueTypeToObjectConverter();
            var options = new JsonSerializerOptions()
            {
                IncludeFields = true,
            };
            options.Converters.Add(converter);

            string json;

            {
                var obj = new TestClassWithValueTypedMember();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj, options);

                Assert.Equal(4, converter.WriteCallCount);
                Assert.Equal(expected, json);
            }

            {
                var obj = JsonSerializer.Deserialize<TestClassWithValueTypedMember>(json, options);
                obj.Verify();

                Assert.Equal(4, converter.ReadCallCount);
            }
        }

        [Fact]
        public static void NullableValueTypedMemberToInterfaceConverter()
        {
            const string expected = @"{""MyValueTypedProperty"":""ValueTypedProperty"",""MyRefTypedProperty"":""RefTypedProperty"",""MyValueTypedField"":""ValueTypedField"",""MyRefTypedField"":""RefTypedField""}";

            var converter = new ValueTypeToInterfaceConverter();
            var options = new JsonSerializerOptions()
            {
                IncludeFields = true,
            };
            options.Converters.Add(converter);

            string json;

            {
                var obj = new TestClassWithNullableValueTypedMember();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj, options);

                Assert.Equal(4, converter.WriteCallCount);
                Assert.Equal(expected, json);
            }

            {
                var obj = JsonSerializer.Deserialize<TestClassWithNullableValueTypedMember>(json, options);
                obj.Verify();

                Assert.Equal(4, converter.ReadCallCount);
            }
        }

        [Fact]
        public static void NullableValueTypedMemberToObjectConverter()
        {
            const string expected = @"{""MyValueTypedProperty"":""ValueTypedProperty"",""MyRefTypedProperty"":""RefTypedProperty"",""MyValueTypedField"":""ValueTypedField"",""MyRefTypedField"":""RefTypedField""}";

            var converter = new ValueTypeToObjectConverter();
            var options = new JsonSerializerOptions()
            {
                IncludeFields = true,
            };
            options.Converters.Add(converter);

            string json;

            {
                var obj = new TestClassWithNullableValueTypedMember();
                obj.Initialize();
                obj.Verify();
                json = JsonSerializer.Serialize(obj, options);

                Assert.Equal(4, converter.WriteCallCount);
                Assert.Equal(expected, json);
            }

            {
                var obj = JsonSerializer.Deserialize<TestClassWithNullableValueTypedMember>(json, options);
                obj.Verify();

                Assert.Equal(4, converter.ReadCallCount);
            }
        }

        [Fact]
        public static void NullableValueTypedMemberWithNullsToInterfaceConverter()
        {
            const string expected = @"{""MyValueTypedProperty"":null,""MyRefTypedProperty"":null,""MyValueTypedField"":null,""MyRefTypedField"":null}";

            var converter = new ValueTypeToInterfaceConverter();
            var options = new JsonSerializerOptions()
            {
                IncludeFields = true,
            };
            options.Converters.Add(converter);

            string json;

            {
                var obj = new TestClassWithNullableValueTypedMember();
                json = JsonSerializer.Serialize(obj, options);

                Assert.Equal(4, converter.WriteCallCount);
                Assert.Equal(expected, json);
            }

            {
                var obj = JsonSerializer.Deserialize<TestClassWithNullableValueTypedMember>(json, options);

                Assert.Equal(4, converter.ReadCallCount);
                Assert.Null(obj.MyValueTypedProperty);
                Assert.Null(obj.MyValueTypedField);
                Assert.Null(obj.MyRefTypedProperty);
                Assert.Null(obj.MyRefTypedField);
            }
        }

        [Fact]
        public static void NullableValueTypedMemberWithNullsToObjectConverter()
        {
            const string expected = @"{""MyValueTypedProperty"":null,""MyRefTypedProperty"":null,""MyValueTypedField"":null,""MyRefTypedField"":null}";

            var converter = new ValueTypeToObjectConverter();
            var options = new JsonSerializerOptions()
            {
                IncludeFields = true,
            };
            options.Converters.Add(converter);

            string json;

            {
                var obj = new TestClassWithNullableValueTypedMember();
                json = JsonSerializer.Serialize(obj, options);

                Assert.Equal(4, converter.WriteCallCount);
                Assert.Equal(expected, json);
            }

            {
                var obj = JsonSerializer.Deserialize<TestClassWithNullableValueTypedMember>(json, options);

                Assert.Equal(4, converter.ReadCallCount);
                Assert.Null(obj.MyValueTypedProperty);
                Assert.Null(obj.MyValueTypedField);
                Assert.Null(obj.MyRefTypedProperty);
                Assert.Null(obj.MyRefTypedField);
            }
        }

    }
}

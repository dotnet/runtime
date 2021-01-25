// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        private class JsonTestStructConverter : JsonConverter<TestStruct>
        {
            public override TestStruct Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new TestStruct
                {
                    InnerValue = reader.GetInt32()
                };
            }

            public override void Write(Utf8JsonWriter writer, TestStruct value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(value.InnerValue);
            }
        }

        private class JsonTestStructThrowingConverter : JsonConverter<TestStruct>
        {
            public override TestStruct Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotSupportedException();
            }

            public override void Write(Utf8JsonWriter writer, TestStruct value, JsonSerializerOptions options)
            {
                throw new NotSupportedException();
            }
        }

        private struct TestStruct
        {
            public int InnerValue { get; set; }
        }

        private class TestStructClass
        {
            [JsonConverter(typeof(JsonTestStructConverter))]
            public TestStruct? MyStruct { get; set; }
        }

        private class TestStructInvalidClass
        {
            // Note: JsonTestStructConverter does not convert int, this is for negative testing.
            [JsonConverter(typeof(JsonTestStructConverter))]
            public int? MyInt { get; set; }
        }

        [Fact]
        public static void NullableCustomValueTypeUsingOptions()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonTestStructConverter());

            {
                TestStruct myStruct = JsonSerializer.Deserialize<TestStruct>("1", options);
                Assert.Equal(1, myStruct.InnerValue);
            }

            {
                TestStruct? myStruct = JsonSerializer.Deserialize<TestStruct?>("null", options);
                Assert.False(myStruct.HasValue);
            }

            {
                TestStruct? myStruct = JsonSerializer.Deserialize<TestStruct?>("1", options);
                Assert.Equal(1, myStruct.Value.InnerValue);
            }
        }

        [Fact]
        public static void NullableCustomValueTypeUsingAttributes()
        {
            {
                TestStructClass myStructClass = JsonSerializer.Deserialize<TestStructClass>(@"{""MyStruct"":null}");
                Assert.False(myStructClass.MyStruct.HasValue);
            }

            {
                TestStructClass myStructClass = JsonSerializer.Deserialize<TestStructClass>(@"{""MyStruct"":1}");
                Assert.True(myStructClass.MyStruct.HasValue);
                Assert.Equal(1, myStructClass.MyStruct.Value.InnerValue);
            }
        }

        [Fact]
        public static void NullableCustomValueTypeChoosesAttributeOverOptions()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonTestStructThrowingConverter());

            // Chooses JsonTestStructThrowingConverter on options, which will throw.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<TestStruct?>("1", options));

            // Chooses JsonTestStructConverter on attribute, which will not throw.
            TestStructClass myStructClass = JsonSerializer.Deserialize<TestStructClass>(@"{""MyStruct"":null}", options);
            Assert.False(myStructClass.MyStruct.HasValue);
        }

        [Fact]
        public static void NullableCustomValueTypeNegativeTest()
        {
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<TestStructInvalidClass>(@"{""MyInt"":null}"));
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize<TestStructInvalidClass>(@"{""MyInt"":1}"));
        }

        [Fact]
        public static void NullableStandardValueTypeTest()
        {
            {
                int? myInt = JsonSerializer.Deserialize<int?>("null");
                Assert.False(myInt.HasValue);
            }

            {
                int? myInt = JsonSerializer.Deserialize<int?>("1");
                Assert.Equal(1, myInt.Value);
            }
        }

        /// <summary>
        /// Used to verify a converter for Nullable is called for null values. Converters are passed
        /// null when {T} is a value type including when {T}=Nullable{int?}.
        /// </summary>
        private class NullIntTo42Converter : JsonConverter<int?>
        {
            public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    // Use literal value to differ from the default converter's behavior.
                    return 42;
                }

                Debug.Assert(false);
                throw new Exception("not expected");
            }

            public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    // Use literal value to differ from the default converter's behavior.
                    writer.WriteNumberValue(42);
                }
                else
                {
                    Debug.Assert(false);
                    throw new Exception("not expected");
                }
            }
        }

        [Fact]
        public static void NullableConverterIsNotPassedNull()
        {
            // For compat, deserialize does not call converter for null token unless the type doesn't support
            // null or HandleNull is overridden and returns 'true'.
            // For compat, serialize does not call converter for null unless null is a valid value and HandleNull is true.

            var options = new JsonSerializerOptions();
            options.Converters.Add(new NullIntTo42Converter());

            {
                int? myInt = JsonSerializer.Deserialize<int?>("null", options);
                Assert.Null(myInt);
            }

            {
                string json = JsonSerializer.Serialize<int?>(null, options);
                Assert.Equal("null", json);
            }

            {
                int?[] ints = JsonSerializer.Deserialize<int?[]>("[null, null]", options);
                Assert.Equal(2, ints.Length);
                Assert.Null(ints[0]);
                Assert.Null(ints[1]);
            }

            {
                string json = JsonSerializer.Serialize<int?[]>(new int?[] { null, null }, options);
                Assert.Equal("[null,null]", json);
            }
        }

        private class PocoSingleInt
        {
            public int MyInt;
        }

        /// <summary>
        /// Used to verify a converter for a reference type is not called for null values.
        /// </summary>
        private class PocoFailOnNullConverter : JsonConverter<PocoSingleInt>
        {
            public override PocoSingleInt Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    Debug.Assert(false);
                    throw new Exception();
                }

                reader.Skip();

                return new PocoSingleInt()
                {
                    // Use literal value to differ from the default converter's behavior.
                    MyInt = 42
                };
            }

            public override void Write(Utf8JsonWriter writer, PocoSingleInt value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    Debug.Assert(false);
                    throw new Exception();
                }

                writer.WriteStartObject();

                // Use literal value to differ from the default converter's behavior.
                writer.WriteNumber("MyInt", 42);

                writer.WriteEndObject();
            }
        }

        [Fact]
        public static void ReferenceTypeConverterDoesntGetPassedNull()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new PocoFailOnNullConverter());

            {
                PocoSingleInt poco = JsonSerializer.Deserialize<PocoSingleInt>("null", options);
                Assert.Null(poco);

                poco = JsonSerializer.Deserialize<PocoSingleInt>("{}", options);
                Assert.Equal(42, poco.MyInt);
            }

            {
                PocoSingleInt[] pocos = JsonSerializer.Deserialize<PocoSingleInt[]>("[null, null]", options);
                Assert.Equal(2, pocos.Length);
                Assert.Null(pocos[0]);
                Assert.Null(pocos[1]);

                pocos = JsonSerializer.Deserialize<PocoSingleInt[]>("[{}, {}]", options);
                Assert.Equal(2, pocos.Length);
                Assert.Equal(42, pocos[0].MyInt);
                Assert.Equal(42, pocos[1].MyInt);
            }

            {
                string json = JsonSerializer.Serialize<PocoSingleInt>(null, options);
                Assert.Equal(@"null", json);

                PocoSingleInt poco = new PocoSingleInt();
                json = JsonSerializer.Serialize<PocoSingleInt>(poco, options);
                Assert.Equal(@"{""MyInt"":42}", json);
            }

            {
                string json = JsonSerializer.Serialize<PocoSingleInt[]>(new PocoSingleInt[] { null, null }, options);
                Assert.Equal(@"[null,null]", json);

                PocoSingleInt poco = new PocoSingleInt();
                json = JsonSerializer.Serialize<PocoSingleInt[]>(new PocoSingleInt[] { poco, poco }, options);
                Assert.Equal(@"[{""MyInt"":42},{""MyInt"":42}]", json);
            }
        }

        [Fact]
        public static void StructConverter_SaysCanConvertNullableStruct_ConverterOnProperty()
        {
            string converterTypeAsStr = typeof(JsonTestStructValueChangingConverter).ToString();
            string structTypeAsStr = typeof(TestStruct).ToString();
            string nullableStructTypeAsStr = typeof(TestStruct?).ToString();

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(new ClassWithNullableStruct_ConverterOnProperty { MyStruct = new TestStruct() }));
            string exAsStr = ex.ToString();
            Assert.Contains(converterTypeAsStr, exAsStr);
            Assert.Contains(structTypeAsStr, exAsStr);
            Assert.Contains(nullableStructTypeAsStr, exAsStr);

            ex = Assert.Throws<InvalidOperationException>(() =>
                JsonSerializer.Deserialize<ClassWithNullableStruct_ConverterOnProperty>(""));
            exAsStr = ex.ToString();
            Assert.Contains(converterTypeAsStr, exAsStr);
            Assert.Contains(structTypeAsStr, exAsStr);
            Assert.Contains(nullableStructTypeAsStr, exAsStr);
        }

        [Fact]
        public static void StructConverter_SaysCanConvertNullableStruct_ConverterOnType()
        {
            // Converter cannot be applied directly to nullable type, so the serializer wraps the converter it with NullableConverter<T> as expected.

            string serialized = JsonSerializer.Serialize(new ClassWithNullableStruct_ConverterOnType { MyStruct = new TestStructWithConverter { InnerValue = 5 } });
            Assert.Equal(@"{""MyStruct"":{""InnerValue"":10}}", serialized);

            ClassWithNullableStruct_ConverterOnType obj = JsonSerializer.Deserialize<ClassWithNullableStruct_ConverterOnType>(serialized);
            Assert.Equal(15, obj.MyStruct?.InnerValue);
        }

        [Fact]
        public static void StructConverter_SaysCanConvertNullableStruct_ConverterOnOptions()
        {
            var options = new JsonSerializerOptions { Converters = { new JsonTestStructValueChangingConverter() } };

            Assert.Throws<InvalidOperationException>(
                () => JsonSerializer.Serialize(new ClassWithNullableStruct { MyStruct = new TestStruct() }, options));

            Assert.Throws<InvalidOperationException>(() =>
                JsonSerializer.Deserialize<ClassWithNullableStruct>("", options));
        }

        [Fact]
        public static void StructConverter_SaysCanConvertNullableStruct_StructAsRootType_ConverterOnType()
        {
            // Converter cannot be applied directly to nullable type, so the serializer wraps the converter it with NullableConverter<T> as expected.

            TestStructWithConverter? obj = new TestStructWithConverter { InnerValue = 5 };
            string serialized = JsonSerializer.Serialize(obj);
            Assert.Equal(@"{""InnerValue"":10}", serialized);

            obj = JsonSerializer.Deserialize<TestStructWithConverter?>(serialized);
            Assert.Equal(15, obj?.InnerValue);
        }

        [Fact]
        public static void StructConverter_SaysCanConvertNullableStruct_StructAsRootType_ConverterOnOptions()
        {
            var options = new JsonSerializerOptions { Converters = { new JsonTestStructValueChangingConverter() } };

            TestStruct? obj = new TestStruct { InnerValue = 5 };

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(obj, options));

            Assert.Throws<InvalidOperationException>(() =>
                JsonSerializer.Deserialize<TestStruct?>("", options));
        }

        private class ClassWithNullableStruct_ConverterOnProperty
        {
            [JsonConverter(typeof(JsonTestStructValueChangingConverter))]
            public TestStruct? MyStruct { get; set; }
        }

        private class ClassWithNullableStruct_ConverterOnType
        {
            public TestStructWithConverter? MyStruct { get; set; }
        }

        private class ClassWithNullableStruct
        {
            public TestStruct? MyStruct { get; set; }
        }

        [JsonConverter(typeof(JsonTestStructWithConverterValueChangingConverter))]
        public struct TestStructWithConverter
        {
            public int InnerValue { get; set; }
        }

        private class JsonTestStructValueChangingConverter : JsonConverter<TestStruct>
        {
            public override bool CanConvert(Type typeToConvert) =>
                typeToConvert == typeof(TestStruct) || typeToConvert == typeof(TestStruct?);

            public override TestStruct Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
                throw new NotImplementedException();

            public override void Write(Utf8JsonWriter writer, TestStruct value, JsonSerializerOptions options) =>
                throw new NotImplementedException();
        }

        private class JsonTestStructWithConverterValueChangingConverter : JsonConverter<TestStructWithConverter>
        {
            public override bool CanConvert(Type typeToConvert) =>
                typeToConvert == typeof(TestStructWithConverter) || typeToConvert == typeof(TestStructWithConverter?);

            public override TestStructWithConverter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                reader.Read();

                if (reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != "InnerValue")
                {
                    throw new JsonException();
                }

                reader.Read();

                var obj = new TestStructWithConverter
                {
                    InnerValue = reader.GetInt32() + 5
                };

                reader.Read();

                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    throw new JsonException();
                }

                return obj;
            }

            public override void Write(Utf8JsonWriter writer, TestStructWithConverter value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteNumber("InnerValue", value.InnerValue + 5);
                writer.WriteEndObject();
            }
        }

        [Fact]
        public static void NonNullableConverter_ReturnedByJsonConverterFactory_CanBeUsedAsFallback_ForNullableProperty()
        {
            string json = @"{
""Property"": {
""Item1"":1,
""Item2"":2
}}";
            // Verify that below converters will be called -
            // serializer doesn't support ValueTuple unless field support is active.
            ClassWithValueTuple obj0 = JsonSerializer.Deserialize<ClassWithValueTuple>(json);
            Assert.Equal(0, obj0.Property.Item1);
            Assert.Equal(0, obj0.Property.Item2);

            obj0 = JsonSerializer.Deserialize<ClassWithValueTuple>(json, new JsonSerializerOptions { IncludeFields = true });
            Assert.Equal(1, obj0.Property.Item1);
            Assert.Equal(2, obj0.Property.Item2);

            // Baseline: converter returned from factory can be used for non-nullable property.
            ClassWithFactoryOn_NonNullableProperty obj1 = JsonSerializer.Deserialize<ClassWithFactoryOn_NonNullableProperty>(json);
            Assert.Equal(1, obj1.Property.Item1);
            Assert.Equal(2, obj1.Property.Item2);

            JsonTestHelper.AssertJsonEqual(json, JsonSerializer.Serialize(obj1));

            // Test: converter returned from factory can be used for nullable property.
            ClassWithFactoryOn_NullableProperty obj2 = JsonSerializer.Deserialize<ClassWithFactoryOn_NullableProperty>(json);
            Assert.Equal(1, obj2.Property?.Item1);
            Assert.Equal(2, obj2.Property?.Item2);

            JsonTestHelper.AssertJsonEqual(json, JsonSerializer.Serialize(obj2));
        }

        private class ClassWithValueTuple
        {
            public (int, int) Property { get; set; }
        }

        private class ClassWithFactoryOn_NonNullableProperty
        {
            [JsonConverter(typeof(MyValueTupleConverterFactory))]
            public (int, int) Property { get; set; }
        }

        private class ClassWithFactoryOn_NullableProperty
        {
            [JsonConverter(typeof(MyValueTupleConverterFactory))]
            public (int, int)? Property { get; set; }
        }

        public class MyValueTupleConverterFactory : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert) => IsValueTupleType(typeToConvert);

            public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                Debug.Assert(CanConvert(typeToConvert));

                Type[] genericArgs = typeToConvert.GetGenericArguments();
                Type item1Type = genericArgs[0];
                Type item2Type = genericArgs[1];

                JsonConverter converter = (JsonConverter)Activator.CreateInstance(
                    typeof(ValueTupleConverter<,>).MakeGenericType(new Type[] { item1Type, item2Type }),
                    BindingFlags.Instance | BindingFlags.Public,
                    binder: null,
                    args: new object[] { },
                    culture: null);

                return converter;
            }

            protected static bool IsValueTupleType(Type type) =>
                type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ValueTuple<,>);

            protected class ValueTupleConverter<TItem1, TItem2> : JsonConverter<ValueTuple<TItem1, TItem2>>
            {
                public override (TItem1, TItem2) Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                {
                    Debug.Assert(reader.TokenType == JsonTokenType.StartObject);

                    reader.Read();
                    JsonTokenType tokenType = reader.TokenType;

                    (TItem1, TItem2) value = default;

                    while (true)
                    {
                        if (tokenType == JsonTokenType.PropertyName)
                        {
                            string propertyName = reader.GetString();
                            if (propertyName == "Item1")
                            {
                                value.Item1 = JsonSerializer.Deserialize<TItem1>(ref reader);
                            }
                            else if (propertyName == "Item2")
                            {
                                value.Item2 = JsonSerializer.Deserialize<TItem2>(ref reader);
                            }
                            else
                            {
                                throw new JsonException();
                            }

                            reader.Read();
                            tokenType = reader.TokenType;
                            continue;
                        }
                        else if (tokenType == JsonTokenType.EndObject)
                        {
                            break;
                        }

                        throw new JsonException();
                    }

                    return value;
                }

                public override void Write(Utf8JsonWriter writer, (TItem1, TItem2) value, JsonSerializerOptions options)
                {
                    writer.WriteStartObject();

                    writer.WritePropertyName("Item1");
                    JsonSerializer.Serialize(writer, value.Item1);

                    writer.WritePropertyName("Item2");
                    JsonSerializer.Serialize(writer, value.Item2);

                    writer.WriteEndObject();
                }
            }
        }
    }
}

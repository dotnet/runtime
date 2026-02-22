// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        [Fact]
        public static void CustomConverter_OnProperty_WithInvalidElementType_Succeeds()
        {
            // Type with ref property (invalid for serialization)
            var collection = new CollectionWithInvalidElementType
            {
                Items = new List<TypeWithRefProperty>
                {
                    new TypeWithRefProperty { Value1 = 42 }
                }
            };

            string json = JsonSerializer.Serialize(collection);
            Assert.Equal(@"{""Items"":[]}", json);

            var deserialized = JsonSerializer.Deserialize<CollectionWithInvalidElementType>(json);
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.Items);
            Assert.Empty(deserialized.Items);
        }

        [Fact]
        public static void CustomConverter_OnProperty_WithInvalidType_Succeeds()
        {
            var obj = new ClassWithInvalidTypeProperty
            {
                InvalidProperty = new TypeWithRefProperty { Value1 = 100 }
            };

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(@"{""InvalidProperty"":""custom""}", json);

            var deserialized = JsonSerializer.Deserialize<ClassWithInvalidTypeProperty>(json);
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.InvalidProperty);
            Assert.Equal(999, deserialized.InvalidProperty.Value1);
        }

        [Fact]
        public static void CustomConverter_OnType_WithRefProperty_Succeeds()
        {
            var obj = new ClassWithTypeConverterAttribute
            {
                Item = new TypeWithRefPropertyAndTypeConverter { Value1 = 50 }
            };

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(@"{""Item"":""type-converter""}", json);

            var deserialized = JsonSerializer.Deserialize<ClassWithTypeConverterAttribute>(json);
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.Item);
            Assert.Equal(888, deserialized.Item.Value1);
        }

        [Fact]
        public static void WithoutCustomConverter_InvalidType_ThrowsInvalidOperationException()
        {
            var obj = new ClassWithInvalidTypePropertyNoConverter
            {
                InvalidProperty = new TypeWithRefProperty { Value1 = 42 }
            };

            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(obj));
        }

        [Fact]
        public static void CustomConverter_OnDictionary_WithInvalidValueType_Succeeds()
        {
            var obj = new ClassWithDictionaryOfInvalidType
            {
                Items = new Dictionary<string, TypeWithRefProperty>
                {
                    ["key1"] = new TypeWithRefProperty { Value1 = 1 }
                }
            };

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(@"{""Items"":{}}", json);

            var deserialized = JsonSerializer.Deserialize<ClassWithDictionaryOfInvalidType>(json);
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.Items);
            Assert.Empty(deserialized.Items);
        }

        // Test classes and converters

        private class CollectionWithInvalidElementType
        {
            [JsonConverter(typeof(ListOfInvalidTypeConverter))]
            public IList<TypeWithRefProperty> Items { get; set; }
        }

        private class ClassWithInvalidTypeProperty
        {
            [JsonConverter(typeof(InvalidTypeConverter))]
            public TypeWithRefProperty InvalidProperty { get; set; }
        }

        private class ClassWithInvalidTypePropertyNoConverter
        {
            public TypeWithRefProperty InvalidProperty { get; set; }
        }

        private class ClassWithTypeConverterAttribute
        {
            public TypeWithRefPropertyAndTypeConverter Item { get; set; }
        }

        private class ClassWithDictionaryOfInvalidType
        {
            [JsonConverter(typeof(DictionaryOfInvalidTypeConverter))]
            public IDictionary<string, TypeWithRefProperty> Items { get; set; }
        }

        // Type with ref property - invalid for serialization
        private class TypeWithRefProperty
        {
            public int Value1 { get; set; }

            private int _value2;
            public ref int Value2 => ref _value2;
        }

        // Type with ref property and [JsonConverter] attribute on the type itself
        [JsonConverter(typeof(TypeWithRefPropertyConverter))]
        private class TypeWithRefPropertyAndTypeConverter
        {
            public int Value1 { get; set; }

            private int _value2;
            public ref int Value2 => ref _value2;
        }

        // Custom converter for IList<TypeWithRefProperty>
        private class ListOfInvalidTypeConverter : JsonConverter<IList<TypeWithRefProperty>>
        {
            public override IList<TypeWithRefProperty> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                reader.Skip();
                return new List<TypeWithRefProperty>();
            }

            public override void Write(Utf8JsonWriter writer, IList<TypeWithRefProperty> value, JsonSerializerOptions options)
            {
                writer.WriteStartArray();
                writer.WriteEndArray();
            }
        }

        // Custom converter for TypeWithRefProperty
        private class InvalidTypeConverter : JsonConverter<TypeWithRefProperty>
        {
            public override TypeWithRefProperty Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                reader.Skip();
                return new TypeWithRefProperty { Value1 = 999 };
            }

            public override void Write(Utf8JsonWriter writer, TypeWithRefProperty value, JsonSerializerOptions options)
            {
                writer.WriteStringValue("custom");
            }
        }

        // Custom converter for TypeWithRefPropertyAndTypeConverter (used via [JsonConverter] attribute on type)
        private class TypeWithRefPropertyConverter : JsonConverter<TypeWithRefPropertyAndTypeConverter>
        {
            public override TypeWithRefPropertyAndTypeConverter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                reader.Skip();
                return new TypeWithRefPropertyAndTypeConverter { Value1 = 888 };
            }

            public override void Write(Utf8JsonWriter writer, TypeWithRefPropertyAndTypeConverter value, JsonSerializerOptions options)
            {
                writer.WriteStringValue("type-converter");
            }
        }

        // Custom converter for IDictionary<string, TypeWithRefProperty>
        private class DictionaryOfInvalidTypeConverter : JsonConverter<IDictionary<string, TypeWithRefProperty>>
        {
            public override IDictionary<string, TypeWithRefProperty> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                reader.Skip();
                return new Dictionary<string, TypeWithRefProperty>();
            }

            public override void Write(Utf8JsonWriter writer, IDictionary<string, TypeWithRefProperty> value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteEndObject();
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public static class InvalidTypesWithConvertersTests
    {
        [Fact]
        public static void SourceGen_CustomConverter_OnProperty_WithInvalidElementType_Succeeds()
        {
            var collection = new SourceGenCollectionWithInvalidElementType
            {
                Items = new List<SourceGenTypeWithRefProperty>
                {
                    new SourceGenTypeWithRefProperty { Value1 = 42 }
                }
            };

            string json = JsonSerializer.Serialize(collection, InvalidTypesContext.Default.SourceGenCollectionWithInvalidElementType);
            Assert.Equal(@"{""Items"":[]}", json);

            var deserialized = JsonSerializer.Deserialize(json, InvalidTypesContext.Default.SourceGenCollectionWithInvalidElementType);
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.Items);
            Assert.Empty(deserialized.Items);
        }

        [Fact]
        public static void SourceGen_CustomConverter_OnProperty_WithInvalidType_Succeeds()
        {
            var obj = new SourceGenClassWithInvalidTypeProperty
            {
                InvalidProperty = new SourceGenTypeWithRefProperty { Value1 = 100 }
            };

            string json = JsonSerializer.Serialize(obj, InvalidTypesContext.Default.SourceGenClassWithInvalidTypeProperty);
            Assert.Equal(@"{""InvalidProperty"":""custom""}", json);

            var deserialized = JsonSerializer.Deserialize(json, InvalidTypesContext.Default.SourceGenClassWithInvalidTypeProperty);
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.InvalidProperty);
            Assert.Equal(999, deserialized.InvalidProperty.Value1);
        }

        [Fact]
        public static void SourceGen_CustomConverter_OnType_WithRefProperty_Succeeds()
        {
            var obj = new SourceGenClassWithTypeConverterAttribute
            {
                Item = new SourceGenTypeWithRefPropertyAndTypeConverter { Value1 = 50 }
            };

            string json = JsonSerializer.Serialize(obj, InvalidTypesContext.Default.SourceGenClassWithTypeConverterAttribute);
            Assert.Equal(@"{""Item"":""type-converter""}", json);

            var deserialized = JsonSerializer.Deserialize(json, InvalidTypesContext.Default.SourceGenClassWithTypeConverterAttribute);
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.Item);
            Assert.Equal(888, deserialized.Item.Value1);
        }

        [Fact]
        public static void SourceGen_CustomConverter_OnDictionary_WithInvalidValueType_Succeeds()
        {
            var obj = new SourceGenClassWithDictionaryOfInvalidType
            {
                Items = new Dictionary<string, SourceGenTypeWithRefProperty>
                {
                    ["key1"] = new SourceGenTypeWithRefProperty { Value1 = 1 }
                }
            };

            string json = JsonSerializer.Serialize(obj, InvalidTypesContext.Default.SourceGenClassWithDictionaryOfInvalidType);
            Assert.Equal(@"{""Items"":{}}", json);

            var deserialized = JsonSerializer.Deserialize(json, InvalidTypesContext.Default.SourceGenClassWithDictionaryOfInvalidType);
            Assert.NotNull(deserialized);
            Assert.NotNull(deserialized.Items);
            Assert.Empty(deserialized.Items);
        }
    }

    // Test classes for source generation

    public class SourceGenCollectionWithInvalidElementType
    {
        [JsonConverter(typeof(SourceGenListOfInvalidTypeConverter))]
        public IList<SourceGenTypeWithRefProperty> Items { get; set; }
    }

    public class SourceGenClassWithInvalidTypeProperty
    {
        [JsonConverter(typeof(SourceGenInvalidTypeConverter))]
        public SourceGenTypeWithRefProperty InvalidProperty { get; set; }
    }

    public class SourceGenClassWithTypeConverterAttribute
    {
        public SourceGenTypeWithRefPropertyAndTypeConverter Item { get; set; }
    }

    public class SourceGenClassWithDictionaryOfInvalidType
    {
        [JsonConverter(typeof(SourceGenDictionaryOfInvalidTypeConverter))]
        public IDictionary<string, SourceGenTypeWithRefProperty> Items { get; set; }
    }

    // Type with ref property - invalid for serialization
    public class SourceGenTypeWithRefProperty
    {
        public int Value1 { get; set; }

        private int _value2;
        public ref int Value2 => ref _value2;
    }

    // Type with ref property and [JsonConverter] attribute on the type itself
    [JsonConverter(typeof(SourceGenTypeWithRefPropertyConverter))]
    public class SourceGenTypeWithRefPropertyAndTypeConverter
    {
        public int Value1 { get; set; }

        private int _value2;
        public ref int Value2 => ref _value2;
    }

    // Custom converter for IList<SourceGenTypeWithRefProperty>
    public class SourceGenListOfInvalidTypeConverter : JsonConverter<IList<SourceGenTypeWithRefProperty>>
    {
        public override IList<SourceGenTypeWithRefProperty> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return new List<SourceGenTypeWithRefProperty>();
        }

        public override void Write(Utf8JsonWriter writer, IList<SourceGenTypeWithRefProperty> value, JsonSerializerOptions options)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
        }
    }

    // Custom converter for SourceGenTypeWithRefProperty
    public class SourceGenInvalidTypeConverter : JsonConverter<SourceGenTypeWithRefProperty>
    {
        public override SourceGenTypeWithRefProperty Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return new SourceGenTypeWithRefProperty { Value1 = 999 };
        }

        public override void Write(Utf8JsonWriter writer, SourceGenTypeWithRefProperty value, JsonSerializerOptions options)
        {
            writer.WriteStringValue("custom");
        }
    }

    // Custom converter for SourceGenTypeWithRefPropertyAndTypeConverter
    public class SourceGenTypeWithRefPropertyConverter : JsonConverter<SourceGenTypeWithRefPropertyAndTypeConverter>
    {
        public override SourceGenTypeWithRefPropertyAndTypeConverter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return new SourceGenTypeWithRefPropertyAndTypeConverter { Value1 = 888 };
        }

        public override void Write(Utf8JsonWriter writer, SourceGenTypeWithRefPropertyAndTypeConverter value, JsonSerializerOptions options)
        {
            writer.WriteStringValue("type-converter");
        }
    }

    // Custom converter for IDictionary<string, SourceGenTypeWithRefProperty>
    public class SourceGenDictionaryOfInvalidTypeConverter : JsonConverter<IDictionary<string, SourceGenTypeWithRefProperty>>
    {
        public override IDictionary<string, SourceGenTypeWithRefProperty> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            reader.Skip();
            return new Dictionary<string, SourceGenTypeWithRefProperty>();
        }

        public override void Write(Utf8JsonWriter writer, IDictionary<string, SourceGenTypeWithRefProperty> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteEndObject();
        }
    }

    // Source generation context
    [JsonSerializable(typeof(SourceGenCollectionWithInvalidElementType))]
    [JsonSerializable(typeof(SourceGenClassWithInvalidTypeProperty))]
    [JsonSerializable(typeof(SourceGenClassWithTypeConverterAttribute))]
    [JsonSerializable(typeof(SourceGenClassWithDictionaryOfInvalidType))]
    internal partial class InvalidTypesContext : JsonSerializerContext
    {
    }
}

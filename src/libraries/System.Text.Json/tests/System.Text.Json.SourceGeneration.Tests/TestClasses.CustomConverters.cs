// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;

namespace System.Text.Json.SourceGeneration.Tests
{
    /// <summary>
    /// Custom converter that adds\subtract 100 from MyIntProperty.
    /// </summary>
    public class CustomConverter_ClassWithCustomConverter : JsonConverter<ClassWithCustomConverter>
    {
        public override ClassWithCustomConverter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("No StartObject");
            }

            ClassWithCustomConverter obj = new();

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName &&
                reader.GetString() != "MyInt")
            {
                throw new JsonException("Wrong property name");
            }

            reader.Read();
            obj.MyInt = reader.GetInt32() - 100;

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException("No EndObject");
            }

            return obj;
        }

        public override void Write(Utf8JsonWriter writer, ClassWithCustomConverter value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(ClassWithCustomConverter.MyInt), value.MyInt + 100);
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Custom converter that adds\subtract 100 from MyIntProperty.
    /// </summary>
    public class CustomConverter_ClassWithCustomConverterFactory : JsonConverter<ClassWithCustomConverterFactory>
    {
        public override ClassWithCustomConverterFactory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("No StartObject");
            }

            ClassWithCustomConverterFactory obj = new();

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName &&
                reader.GetString() != "MyInt")
            {
                throw new JsonException("Wrong property name");
            }

            reader.Read();
            obj.MyInt = reader.GetInt32() - 100;

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException("No EndObject");
            }

            return obj;
        }

        public override void Write(Utf8JsonWriter writer, ClassWithCustomConverterFactory value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(ClassWithCustomConverterFactory.MyInt), value.MyInt + 100);
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Custom converter that adds\subtract 100 from MyIntProperty.
    /// </summary>
    public class CustomConverter_StructWithCustomConverter : JsonConverter<StructWithCustomConverter>
    {
        public override StructWithCustomConverter Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("No StartObject");
            }

            StructWithCustomConverter obj = new();

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName &&
                reader.GetString() != "MyInt")
            {
                throw new JsonException("Wrong property name");
            }

            reader.Read();
            obj.MyInt = reader.GetInt32() - 100;

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException("No EndObject");
            }

            return obj;
        }

        public override void Write(Utf8JsonWriter writer, StructWithCustomConverter value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(StructWithCustomConverter.MyInt), value.MyInt + 100);
            writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Custom converter that adds\subtract 100 from MyIntProperty.
    /// </summary>
    public class CustomConverter_StructWithCustomConverterFactory : JsonConverter<StructWithCustomConverterFactory>
    {
        public override StructWithCustomConverterFactory Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
            {
                throw new JsonException("No StartObject");
            }

            StructWithCustomConverterFactory obj = new();

            reader.Read();
            if (reader.TokenType != JsonTokenType.PropertyName &&
                reader.GetString() != "MyInt")
            {
                throw new JsonException("Wrong property name");
            }

            reader.Read();
            obj.MyInt = reader.GetInt32() - 100;

            reader.Read();
            if (reader.TokenType != JsonTokenType.EndObject)
            {
                throw new JsonException("No EndObject");
            }

            return obj;
        }

        public override void Write(Utf8JsonWriter writer, StructWithCustomConverterFactory value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WriteNumber(nameof(StructWithCustomConverterFactory.MyInt), value.MyInt + 100);
            writer.WriteEndObject();
        }
    }

    public class CustomConverterFactory : JsonConverterFactory
    {
        public CustomConverterFactory()
        {
        }

        public override bool CanConvert(Type typeToConvert)
        {
            return (
                typeToConvert == typeof(StructWithCustomConverterFactory) ||
                typeToConvert == typeof(ClassWithCustomConverterFactory));
        }

        public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
        {
            if (typeToConvert == typeof(StructWithCustomConverterFactory))
            {
                return new CustomConverter_StructWithCustomConverterFactory();
            }

            if (typeToConvert == typeof(ClassWithCustomConverterFactory))
            {
                return new CustomConverter_ClassWithCustomConverterFactory();
            }

            throw new InvalidOperationException("Not expected.");
        }
    }

    [JsonConverter(typeof(CustomConverter_ClassWithCustomConverter))]
    public class ClassWithCustomConverter
    {
        public int MyInt { get; set; }
    }

    [JsonConverter(typeof(CustomConverter_StructWithCustomConverter))]
    public struct StructWithCustomConverter
    {
        public int MyInt { get; set; }
    }

    [JsonConverter(typeof(CustomConverterFactory))]
    public class ClassWithCustomConverterFactory
    {
        public int MyInt { get; set; }
    }

    [JsonConverter(typeof(CustomConverterFactory))]
    public struct StructWithCustomConverterFactory
    {
        public int MyInt { get; set; }
    }

    public class ClassWithCustomConverterProperty
    {
        [JsonConverter(typeof(NestedPocoCustomConverter))]
        public NestedPoco Property { get; set; }

        public class NestedPoco
        {
            public int Value { get; set; }
        }

        public class NestedPocoCustomConverter : JsonConverter<NestedPoco>
        {
            public override NestedPoco? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => new NestedPoco { Value = reader.GetInt32() };
            public override void Write(Utf8JsonWriter writer, NestedPoco value, JsonSerializerOptions options) => writer.WriteNumberValue(value.Value);
        }
    }

    public struct StructWithCustomConverterProperty
    {
        [JsonConverter(typeof(ClassWithCustomConverterProperty.NestedPocoCustomConverter))]
        public ClassWithCustomConverterProperty.NestedPoco Property { get; set; }
    }

    public class ClassWithCustomConverterFactoryProperty
    {
        [JsonConverter(typeof(JsonStringEnumConverter))] // This converter is a JsonConverterFactory
        public Serialization.Tests.SampleEnum MyEnum { get; set; }
    }

    public struct StructWithCustomConverterFactoryProperty
    {
        [JsonConverter(typeof(JsonStringEnumConverter))] // This converter is a JsonConverterFactory
        public Serialization.Tests.SampleEnum MyEnum { get; set; }
    }

    [JsonConverter(typeof(CustomConverter_StructWithCustomConverter))] // Invalid
    public class ClassWithBadCustomConverter
    {
        public int MyInt { get; set; }
    }

    [JsonConverter(typeof(CustomConverter_StructWithCustomConverter))] // Invalid
    public struct StructWithBadCustomConverter
    {
        public int MyInt { get; set; }
    }
}

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
        [JsonConverter(typeof(JsonStringEnumConverter<SourceGenSampleEnum>))] // This converter is a JsonConverterFactory
        public SourceGenSampleEnum MyEnum { get; set; }
    }

    public struct StructWithCustomConverterFactoryProperty
    {
        [JsonConverter(typeof(JsonStringEnumConverter<SourceGenSampleEnum>))] // This converter is a JsonConverterFactory
        public SourceGenSampleEnum MyEnum { get; set; }
    }

    public class ClassWithCustomConverterFactoryNullableProperty
    {
        [JsonConverter(typeof(JsonStringEnumConverter<SourceGenSampleEnum>))] // This converter is a JsonConverterFactory
        public SourceGenSampleEnum? MyEnum { get; set; }
    }

    public class ClassWithCustomConverterNullableProperty
    {
        [JsonConverter(typeof(TimeSpanSecondsConverter))]
        public TimeSpan? TimeSpan { get; set; }
    }

    public class TimeSpanSecondsConverter : JsonConverter<TimeSpan>
    {
        public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return TimeSpan.FromSeconds(reader.GetDouble());
        }

        public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        {
            writer.WriteNumberValue(value.TotalSeconds);
        }
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

    public enum SourceGenSampleEnum
    {
        MinZero = 0,
        One = 1,
        Two = 2
    }

    // Generic converter types for testing open generic converter support

    /// <summary>
    /// A generic option type that represents an optional value.
    /// Uses an open generic converter type.
    /// </summary>
    [JsonConverter(typeof(OptionConverter<>))]
    public readonly struct Option<T>
    {
        public bool HasValue { get; }
        public T Value { get; }

        public Option(T value)
        {
            HasValue = true;
            Value = value;
        }

        public static implicit operator Option<T>(T value) => new(value);
    }

    /// <summary>
    /// Generic converter for the Option type.
    /// </summary>
    public sealed class OptionConverter<T> : JsonConverter<Option<T>>
    {
        public override bool HandleNull => true;

        public override Option<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return default;
            }

            return new(JsonSerializer.Deserialize<T>(ref reader, options)!);
        }

        public override void Write(Utf8JsonWriter writer, Option<T> value, JsonSerializerOptions options)
        {
            if (!value.HasValue)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializer.Serialize(writer, value.Value, options);
        }
    }

    /// <summary>
    /// A class that contains an Option property for testing.
    /// </summary>
    public class ClassWithOptionProperty
    {
        public string Name { get; set; }
        public Option<int> OptionalValue { get; set; }
    }

    /// <summary>
    /// A wrapper type that uses an open generic converter on a property.
    /// </summary>
    public class GenericWrapper<T>
    {
        public T WrappedValue { get; }

        public GenericWrapper(T value)
        {
            WrappedValue = value;
        }
    }

    /// <summary>
    /// Generic converter for the GenericWrapper type.
    /// </summary>
    public sealed class GenericWrapperConverter<T> : JsonConverter<GenericWrapper<T>>
    {
        public override GenericWrapper<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            T value = JsonSerializer.Deserialize<T>(ref reader, options)!;
            return new GenericWrapper<T>(value);
        }

        public override void Write(Utf8JsonWriter writer, GenericWrapper<T> value, JsonSerializerOptions options)
        {
            JsonSerializer.Serialize(writer, value.WrappedValue, options);
        }
    }

    /// <summary>
    /// A class with a property that uses an open generic converter attribute.
    /// </summary>
    public class ClassWithGenericConverterOnProperty
    {
        [JsonConverter(typeof(GenericWrapperConverter<>))]
        public GenericWrapper<int> Value { get; set; }
    }

    // Tests for nested containing class with type parameters
    // The converter is nested in a generic container class.
    [JsonConverter(typeof(NestedConverterContainer<>.NestedConverter<>))]
    public class TypeWithNestedConverter<T1, T2>
    {
        public T1 Value1 { get; set; }
        public T2 Value2 { get; set; }
    }

    public class NestedConverterContainer<T>
    {
        public sealed class NestedConverter<U> : JsonConverter<TypeWithNestedConverter<T, U>>
        {
            public override TypeWithNestedConverter<T, U> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                    throw new JsonException();

                var result = new TypeWithNestedConverter<T, U>();
                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                        break;

                    if (reader.TokenType != JsonTokenType.PropertyName)
                        throw new JsonException();

                    string propertyName = reader.GetString()!;
                    reader.Read();

                    if (propertyName == "Value1")
                        result.Value1 = JsonSerializer.Deserialize<T>(ref reader, options)!;
                    else if (propertyName == "Value2")
                        result.Value2 = JsonSerializer.Deserialize<U>(ref reader, options)!;
                }

                return result;
            }

            public override void Write(Utf8JsonWriter writer, TypeWithNestedConverter<T, U> value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("Value1");
                JsonSerializer.Serialize(writer, value.Value1, options);
                writer.WritePropertyName("Value2");
                JsonSerializer.Serialize(writer, value.Value2, options);
                writer.WriteEndObject();
            }
        }
    }

    // Tests for type parameters with constraints that are satisfied
    [JsonConverter(typeof(ConverterWithClassConstraint<>))]
    public class TypeWithSatisfiedConstraint<T>
    {
        public T Value { get; set; }
    }

    public sealed class ConverterWithClassConstraint<T> : JsonConverter<TypeWithSatisfiedConstraint<T>> where T : class
    {
        public override TypeWithSatisfiedConstraint<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType != JsonTokenType.StartObject)
                throw new JsonException();

            var result = new TypeWithSatisfiedConstraint<T>();
            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                    break;

                if (reader.TokenType != JsonTokenType.PropertyName)
                    throw new JsonException();

                string propertyName = reader.GetString()!;
                reader.Read();

                if (propertyName == "Value")
                    result.Value = JsonSerializer.Deserialize<T>(ref reader, options)!;
            }

            return result;
        }

        public override void Write(Utf8JsonWriter writer, TypeWithSatisfiedConstraint<T> value, JsonSerializerOptions options)
        {
            writer.WriteStartObject();
            writer.WritePropertyName("Value");
            JsonSerializer.Serialize(writer, value.Value, options);
            writer.WriteEndObject();
        }
    }
}

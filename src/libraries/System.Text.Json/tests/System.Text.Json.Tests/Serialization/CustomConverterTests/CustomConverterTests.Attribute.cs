// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        /// <summary>
        /// Pass additional information to a converter through an attribute on a property.
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Class, AllowMultiple = false)]
        private class PointConverterAttribute : JsonConverterAttribute
        {
            public PointConverterAttribute(int coordinateOffset = 0)
            {
                CoordinateOffset = coordinateOffset;
            }

            public int CoordinateOffset { get; private set; }

            /// <summary>
            /// If overridden, allows a custom attribute to create the converter in order to pass additional state.
            /// </summary>
            /// <returns>The custom converter, or null if the serializer should create the custom converter.</returns>
            public override JsonConverter CreateConverter(Type typeToConvert)
            {
                return new PointConverter(CoordinateOffset);
            }
        }

        private class ClassWithPointConverterAttribute
        {
            [PointConverter(10)]
            public Point Point1 { get; set; }
        }

        [Fact]
        public static void CustomAttributeExtraInformation()
        {
            const string json = @"{""Point1"":""1,2""}";

            ClassWithPointConverterAttribute obj = JsonSerializer.Deserialize<ClassWithPointConverterAttribute>(json);
            Assert.Equal(11, obj.Point1.X);
            Assert.Equal(12, obj.Point1.Y);

            string jsonSerialized = JsonSerializer.Serialize(obj);
            Assert.Equal(json, jsonSerialized);
        }

        private class ClassWithJsonConverterAttribute
        {
            [JsonConverter(typeof(PointConverter))]
            public Point Point1 { get; set; }
        }

        [Fact]
        public static void CustomAttributeOnProperty()
        {
            const string json = @"{""Point1"":""1,2""}";

            ClassWithJsonConverterAttribute obj = JsonSerializer.Deserialize<ClassWithJsonConverterAttribute>(json);
            Assert.Equal(1, obj.Point1.X);
            Assert.Equal(2, obj.Point1.Y);

            string jsonSerialized = JsonSerializer.Serialize(obj);
            Assert.Equal(json, jsonSerialized);
        }

        // A custom data type representing a point where JSON is "XValue,Yvalue".
        // A struct is used here, but could be a class.
        [JsonConverter(typeof(AttributedPointConverter))]
        public struct AttributedPoint
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        /// <summary>
        /// Converter for a custom data type that has additional state (coordinateOffset).
        /// </summary>
        private class AttributedPointConverter : JsonConverter<AttributedPoint>
        {
            private int _offset;

            public AttributedPointConverter() { }

            public AttributedPointConverter(int offset)
            {
                _offset = offset;
            }

            public override AttributedPoint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.String)
                {
                    throw new JsonException();
                }

                string[] stringValues = reader.GetString().Split(',');
                if (stringValues.Length != 2)
                {
                    throw new JsonException();
                }

                AttributedPoint value = new AttributedPoint();
                if (!int.TryParse(stringValues[0], out int x) || !int.TryParse(stringValues[1], out int y))
                {
                    throw new JsonException();
                }

                value.X = x + _offset;
                value.Y = y + _offset;

                return value;
            }

            public override void Write(Utf8JsonWriter writer, AttributedPoint value, JsonSerializerOptions options)
            {
                string stringValue = $"{value.X - _offset},{value.Y - _offset}";
                writer.WriteStringValue(stringValue);
            }
        }

        [Fact]
        public static void CustomAttributeOnType()
        {
            const string json = @"""1,2""";

            AttributedPoint point = JsonSerializer.Deserialize<AttributedPoint>(json);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);

            string jsonSerialized = JsonSerializer.Serialize(point);
            Assert.Equal(json, jsonSerialized);
        }

        [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
        private class AttributedPointConverterAttribute : JsonConverterAttribute
        {
            public AttributedPointConverterAttribute(int offset = 0)
            {
                Offset = offset;
            }

            public int Offset { get; private set; }

            /// <summary>
            /// If overridden, allows a custom attribute to create the converter in order to pass additional state.
            /// </summary>
            /// <returns>The custom converter, or null if the serializer should create the custom converter.</returns>
            public override JsonConverter CreateConverter(Type typeToConvert)
            {
                return new AttributedPointConverter(Offset);
            }
        }

        private class ClassWithJsonConverterAttributeOverride
        {
            [AttributedPointConverter(100)] // overrides the type attribute on AttributedPoint
            public AttributedPoint Point1 { get; set; }
        }

        [Fact]
        public static void CustomAttributeOnTypeAndProperty()
        {
            const string json = @"{""Point1"":""1,2""}";

            ClassWithJsonConverterAttributeOverride point = JsonSerializer.Deserialize<ClassWithJsonConverterAttributeOverride>(json);

            // The property attribute overrides the type attribute.
            Assert.Equal(101, point.Point1.X);
            Assert.Equal(102, point.Point1.Y);

            string jsonSerialized = JsonSerializer.Serialize(point);
            Assert.Equal(json, jsonSerialized);
        }

        [Fact]
        public static void CustomAttributeOnPropertyAndRuntime()
        {
            const string json = @"{""Point1"":""1,2""}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new AttributedPointConverter(200));

            ClassWithJsonConverterAttributeOverride point = JsonSerializer.Deserialize<ClassWithJsonConverterAttributeOverride>(json);

            // The property attribute overrides the runtime.
            Assert.Equal(101, point.Point1.X);
            Assert.Equal(102, point.Point1.Y);

            string jsonSerialized = JsonSerializer.Serialize(point);
            Assert.Equal(json, jsonSerialized);
        }

        [Fact]
        public static void CustomAttributeOnTypeAndRuntime()
        {
            const string json = @"""1,2""";

            // Baseline
            AttributedPoint point = JsonSerializer.Deserialize<AttributedPoint>(json);
            Assert.Equal(1, point.X);
            Assert.Equal(2, point.Y);
            Assert.Equal(json, JsonSerializer.Serialize(point));

            // Now use options.
            var options = new JsonSerializerOptions();
            options.Converters.Add(new AttributedPointConverter(200));

            point = JsonSerializer.Deserialize<AttributedPoint>(json, options);

            // The runtime overrides the type attribute.
            Assert.Equal(201, point.X);
            Assert.Equal(202, point.Y);

            string jsonSerialized = JsonSerializer.Serialize(point, options);
            Assert.Equal(json, jsonSerialized);
        }

        // Tests for open generic converters on generic types

        /// <summary>
        /// A generic option type that represents an optional value.
        /// The converter type is an open generic, which will be constructed
        /// to match the type arguments of the Option type.
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
        /// Generic converter for the Option type. Serializes the value if present,
        /// or null if not.
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

        [Fact]
        public static void GenericConverterAttributeOnGenericType_Serialize()
        {
            // Test serialization with a value
            Option<int> option = new Option<int>(42);
            string json = JsonSerializer.Serialize(option);
            Assert.Equal("42", json);

            // Test serialization without a value
            Option<int> emptyOption = default;
            json = JsonSerializer.Serialize(emptyOption);
            Assert.Equal("null", json);
        }

        [Fact]
        public static void GenericConverterAttributeOnGenericType_Deserialize()
        {
            // Test deserialization with a value
            Option<int> option = JsonSerializer.Deserialize<Option<int>>("42");
            Assert.True(option.HasValue);
            Assert.Equal(42, option.Value);

            // Test deserialization of null
            option = JsonSerializer.Deserialize<Option<int>>("null");
            Assert.False(option.HasValue);
        }

        [Fact]
        public static void GenericConverterAttributeOnGenericType_ComplexType()
        {
            // Test with a complex type
            Option<string> option = new Option<string>("hello");
            string json = JsonSerializer.Serialize(option);
            Assert.Equal(@"""hello""", json);

            option = JsonSerializer.Deserialize<Option<string>>(json);
            Assert.True(option.HasValue);
            Assert.Equal("hello", option.Value);
        }

        [Fact]
        public static void GenericConverterAttributeOnGenericType_NestedInClass()
        {
            // Test Option type when used as a property
            var obj = new ClassWithOptionProperty { Name = "Test", OptionalValue = 42 };
            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(@"{""Name"":""Test"",""OptionalValue"":42}", json);

            var deserialized = JsonSerializer.Deserialize<ClassWithOptionProperty>(json);
            Assert.Equal("Test", deserialized.Name);
            Assert.True(deserialized.OptionalValue.HasValue);
            Assert.Equal(42, deserialized.OptionalValue.Value);
        }

        private class ClassWithOptionProperty
        {
            public string Name { get; set; }
            public Option<int> OptionalValue { get; set; }
        }

        /// <summary>
        /// A generic result type that represents either a success value or an error.
        /// Tests a generic converter with two type parameters.
        /// </summary>
        [JsonConverter(typeof(ResultConverter<,>))]
        public readonly struct Result<TValue, TError>
        {
            public bool IsSuccess { get; }
            public TValue Value { get; }
            public TError Error { get; }

            private Result(TValue value, TError error, bool isSuccess)
            {
                Value = value;
                Error = error;
                IsSuccess = isSuccess;
            }

            public static Result<TValue, TError> Success(TValue value) =>
                new(value, default!, true);

            public static Result<TValue, TError> Failure(TError error) =>
                new(default!, error, false);
        }

        /// <summary>
        /// Generic converter for the Result type with two type parameters.
        /// </summary>
        public sealed class ResultConverter<TValue, TError> : JsonConverter<Result<TValue, TError>>
        {
            public override Result<TValue, TError> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                bool? isSuccess = null;
                TValue value = default!;
                TError error = default!;

                while (reader.Read())
                {
                    if (reader.TokenType == JsonTokenType.EndObject)
                    {
                        break;
                    }

                    if (reader.TokenType != JsonTokenType.PropertyName)
                    {
                        throw new JsonException();
                    }

                    string propertyName = reader.GetString()!;
                    reader.Read();

                    switch (propertyName)
                    {
                        case "IsSuccess":
                            isSuccess = reader.GetBoolean();
                            break;
                        case "Value":
                            value = JsonSerializer.Deserialize<TValue>(ref reader, options)!;
                            break;
                        case "Error":
                            error = JsonSerializer.Deserialize<TError>(ref reader, options)!;
                            break;
                    }
                }

                if (isSuccess == true)
                {
                    return Result<TValue, TError>.Success(value);
                }

                return Result<TValue, TError>.Failure(error);
            }

            public override void Write(Utf8JsonWriter writer, Result<TValue, TError> value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WriteBoolean("IsSuccess", value.IsSuccess);

                if (value.IsSuccess)
                {
                    writer.WritePropertyName("Value");
                    JsonSerializer.Serialize(writer, value.Value, options);
                }
                else
                {
                    writer.WritePropertyName("Error");
                    JsonSerializer.Serialize(writer, value.Error, options);
                }

                writer.WriteEndObject();
            }
        }

        [Fact]
        public static void GenericConverterAttributeOnGenericType_TwoTypeParameters_Success()
        {
            var result = Result<int, string>.Success(42);
            string json = JsonSerializer.Serialize(result);
            Assert.Equal(@"{""IsSuccess"":true,""Value"":42}", json);

            var deserialized = JsonSerializer.Deserialize<Result<int, string>>(json);
            Assert.True(deserialized.IsSuccess);
            Assert.Equal(42, deserialized.Value);
        }

        [Fact]
        public static void GenericConverterAttributeOnGenericType_TwoTypeParameters_Failure()
        {
            var result = Result<int, string>.Failure("error message");
            string json = JsonSerializer.Serialize(result);
            Assert.Equal(@"{""IsSuccess"":false,""Error"":""error message""}", json);

            var deserialized = JsonSerializer.Deserialize<Result<int, string>>(json);
            Assert.False(deserialized.IsSuccess);
            Assert.Equal("error message", deserialized.Error);
        }

        /// <summary>
        /// Test that an open generic converter can be used on a property with [JsonConverter].
        /// </summary>
        [Fact]
        public static void GenericConverterAttributeOnProperty()
        {
            var obj = new ClassWithGenericConverterOnProperty { Value = new MyGenericWrapper<int>(42) };
            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(@"{""Value"":42}", json);

            var deserialized = JsonSerializer.Deserialize<ClassWithGenericConverterOnProperty>(json);
            Assert.Equal(42, deserialized.Value.WrappedValue);
        }

        private class ClassWithGenericConverterOnProperty
        {
            [JsonConverter(typeof(MyGenericWrapperConverter<>))]
            public MyGenericWrapper<int> Value { get; set; }
        }

        public class MyGenericWrapper<T>
        {
            public T WrappedValue { get; }

            public MyGenericWrapper(T value)
            {
                WrappedValue = value;
            }
        }

        public sealed class MyGenericWrapperConverter<T> : JsonConverter<MyGenericWrapper<T>>
        {
            public override MyGenericWrapper<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                T value = JsonSerializer.Deserialize<T>(ref reader, options)!;
                return new MyGenericWrapper<T>(value);
            }

            public override void Write(Utf8JsonWriter writer, MyGenericWrapper<T> value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize(writer, value.WrappedValue, options);
            }
        }

        // Tests for type parameter arity mismatch
        [Fact]
        public static void GenericConverterAttribute_ArityMismatch_ThrowsInvalidOperationException()
        {
            // The converter has two type parameters but the type has one
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new TypeWithArityMismatch<int>()));
        }

        [JsonConverter(typeof(ConverterWithTwoParams<,>))]
        public class TypeWithArityMismatch<T>
        {
            public T Value { get; set; }
        }

        public sealed class ConverterWithTwoParams<T1, T2> : JsonConverter<TypeWithArityMismatch<T1>>
        {
            public override TypeWithArityMismatch<T1> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => throw new NotImplementedException();

            public override void Write(Utf8JsonWriter writer, TypeWithArityMismatch<T1> value, JsonSerializerOptions options)
                => throw new NotImplementedException();
        }

        // Tests for type constraint violations
        [Fact]
        public static void GenericConverterAttribute_ConstraintViolation_ThrowsArgumentException()
        {
            // The converter has a class constraint but int is a value type
            Assert.Throws<ArgumentException>(() => JsonSerializer.Serialize(new TypeWithConstraintViolation<int>()));
        }

        [JsonConverter(typeof(ConverterWithClassConstraint<>))]
        public class TypeWithConstraintViolation<T>
        {
            public T Value { get; set; }
        }

        public sealed class ConverterWithClassConstraint<T> : JsonConverter<TypeWithConstraintViolation<T>> where T : class
        {
            public override TypeWithConstraintViolation<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => throw new NotImplementedException();

            public override void Write(Utf8JsonWriter writer, TypeWithConstraintViolation<T> value, JsonSerializerOptions options)
                => throw new NotImplementedException();
        }

        // Tests for converter type that doesn't match the target type
        [Fact]
        public static void GenericConverterAttribute_ConverterTypeMismatch_ThrowsInvalidOperationException()
        {
            // The converter converts DifferentType<T> but the type is TypeWithConverterMismatch<T>
            Assert.Throws<InvalidOperationException>(() => JsonSerializer.Serialize(new TypeWithConverterMismatch<int>()));
        }

        [JsonConverter(typeof(DifferentTypeConverter<>))]
        public class TypeWithConverterMismatch<T>
        {
            public T Value { get; set; }
        }

        public class DifferentType<T>
        {
            public T Value { get; set; }
        }

        public sealed class DifferentTypeConverter<T> : JsonConverter<DifferentType<T>>
        {
            public override DifferentType<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
                => throw new NotImplementedException();

            public override void Write(Utf8JsonWriter writer, DifferentType<T> value, JsonSerializerOptions options)
                => throw new NotImplementedException();
        }

        // Tests for nested containing class with type parameters
        [Fact]
        public static void GenericConverterAttribute_NestedConverter_Works()
        {
            // Converter is nested in a generic container class
            var value = new TypeWithNestedConverter<int, string> { Value1 = 42, Value2 = "hello" };
            string json = JsonSerializer.Serialize(value);
            Assert.Equal(@"{""Value1"":42,""Value2"":""hello""}", json);

            var deserialized = JsonSerializer.Deserialize<TypeWithNestedConverter<int, string>>(json);
            Assert.Equal(42, deserialized.Value1);
            Assert.Equal("hello", deserialized.Value2);
        }

        [JsonConverter(typeof(Container<>.NestedConverter<>))]
        public class TypeWithNestedConverter<T1, T2>
        {
            public T1 Value1 { get; set; }
            public T2 Value2 { get; set; }
        }

        public class Container<T>
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
        [Fact]
        public static void GenericConverterAttribute_ConstraintSatisfied_Works()
        {
            // The converter has a class constraint and string is a reference type
            var value = new TypeWithSatisfiedConstraint<string> { Value = "test" };
            string json = JsonSerializer.Serialize(value);
            Assert.Equal(@"{""Value"":""test""}", json);

            var deserialized = JsonSerializer.Deserialize<TypeWithSatisfiedConstraint<string>>(json);
            Assert.Equal("test", deserialized.Value);
        }

        [JsonConverter(typeof(ConverterWithSatisfiedConstraint<>))]
        public class TypeWithSatisfiedConstraint<T>
        {
            public T Value { get; set; }
        }

        public sealed class ConverterWithSatisfiedConstraint<T> : JsonConverter<TypeWithSatisfiedConstraint<T>> where T : class
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
}

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
    }
}

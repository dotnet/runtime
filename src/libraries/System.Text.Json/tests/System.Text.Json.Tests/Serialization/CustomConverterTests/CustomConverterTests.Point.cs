// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        // A custom data type representing a point where JSON is "XValue,YValue".
        public struct Point
        {
            public Point(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get;}
            public int Y { get;}
        }

        // Converter for a custom data type that has additional state (coordinateOffset).
        private class PointConverter : JsonConverter<Point>
        {
            private int _coordinateOffset;

            public PointConverter() { }

            public PointConverter(int coordinateOffset = 0)
            {
                _coordinateOffset = coordinateOffset;
            }

            public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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

                if (!int.TryParse(stringValues[0], out int x) || !int.TryParse(stringValues[1], out int y))
                {
                    throw new JsonException();
                }

                var value = new Point(x + _coordinateOffset, y + _coordinateOffset);
                return value;
            }

            public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
            {
                string stringValue = $"{value.X - _coordinateOffset},{value.Y - _coordinateOffset}";
                writer.WriteStringValue(stringValue);
            }
        }

        [Fact]
        public static void CustomValueConverterFromArray()
        {
            const string json = @"[""1,2"",""3,4""]";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new PointConverter());

            Point[] Points = JsonSerializer.Deserialize<Point[]>(json, options);
            Assert.Equal(2, Points.Length);
            Assert.Equal(1, Points[0].X);
            Assert.Equal(2, Points[0].Y);
            Assert.Equal(3, Points[1].X);
            Assert.Equal(4, Points[1].Y);

            string jsonSerialized = JsonSerializer.Serialize(Points, options);
            Assert.Equal(json, jsonSerialized);
        }

        [Fact]
        public static void CustomValueConverterFromRoot()
        {
            const string json = @"""1,2""";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new PointConverter());

            Point obj = JsonSerializer.Deserialize<Point>(json, options);
            Assert.Equal(1, obj.X);
            Assert.Equal(2, obj.Y);

            string jsonSerialized = JsonSerializer.Serialize(obj, options);
            Assert.Equal(json, jsonSerialized);
        }

        [Fact]
        public static void CustomValueConverterFromRootAndOptions()
        {
            const string json = @"""1,2""";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new PointConverter(100));

            Point obj = JsonSerializer.Deserialize<Point>(json, options);
            Assert.Equal(101, obj.X);
            Assert.Equal(102, obj.Y);

            string jsonSerialized = JsonSerializer.Serialize(obj, options);
            Assert.Equal(json, jsonSerialized);
        }

        [Fact]
        public static void CustomValueConverterFromRootWithNullable()
        {
            const string json = "null";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new PointConverter());

            Point? obj = JsonSerializer.Deserialize<Point?>(json, options);
            Assert.Null(obj);
        }

        [Fact]
        public static void CustomValueConverterFromRootFail()
        {
            // Invalid JSON according to the converter.
            const string json = @"""1""";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new PointConverter());

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Point>(json, options));
        }

        [Fact]
        public static void CustomValueConverterStructFromRoot()
        {
            const string json = @"""1,2""";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new PointConverter());

            Point obj = JsonSerializer.Deserialize<Point>(json, options);
            Assert.Equal(1, obj.X);
            Assert.Equal(2, obj.Y);

            string jsonSerialized = JsonSerializer.Serialize(obj, options);
            Assert.Equal(json, jsonSerialized);
        }

        /// <summary>
        /// Converter for Point but treats it as an Object in the JSON.
        /// </summary>
        private class PointObjectConverter : JsonConverter<Point>
        {
            public override Point Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    throw new JsonException();
                }

                reader.Read();
                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException();
                }

                if (reader.GetString() != "COORD")
                {
                    throw new JsonException();
                }

                reader.Read();
                string[] stringValues = reader.GetString().Split(',');
                if (stringValues.Length != 2)
                {
                    throw new JsonException();
                }

                if (!int.TryParse(stringValues[0], out int x) || !int.TryParse(stringValues[1], out int y))
                {
                    throw new JsonException();
                }

                var value = new Point(x, y);

                reader.Read();
                if (reader.TokenType != JsonTokenType.EndObject)
                {
                    throw new JsonException();
                }

                return value;
            }

            public override void Write(Utf8JsonWriter writer, Point value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                string stringValue = $"{value.X},{value.Y}";
                writer.WriteString("COORD", stringValue);

                writer.WriteEndObject();
            }
        }

        [Fact]
        public static void CustomObjectConverterInArray()
        {
            const string json = @"[{""COORD"":""1,2""},{""COORD"":""3,4""}]";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new PointObjectConverter());

            Point[] Points = JsonSerializer.Deserialize<Point[]>(json, options);
            Assert.Equal(2, Points.Length);
            Assert.Equal(1, Points[0].X);
            Assert.Equal(2, Points[0].Y);
            Assert.Equal(3, Points[1].X);
            Assert.Equal(4, Points[1].Y);

            string jsonSerialized = JsonSerializer.Serialize(Points, options);
            Assert.Equal(json, jsonSerialized);
        }

        [Fact]
        public static void CustomObjectConverterFromRoot()
        {
            const string json = @"{""COORD"":""1,2""}";

            var options = new JsonSerializerOptions();
            options.Converters.Add(new PointObjectConverter());

            Point obj = JsonSerializer.Deserialize<Point>(json, options);
            Assert.Equal(1, obj.X);
            Assert.Equal(2, obj.Y);

            string jsonSerialized = JsonSerializer.Serialize(obj, options);
            Assert.Equal(json, jsonSerialized);
        }
    }
}

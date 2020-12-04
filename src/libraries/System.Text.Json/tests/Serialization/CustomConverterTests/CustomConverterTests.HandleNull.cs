// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        [Fact]
        public static void ValueTypeConverter_NoOverride()
        {
            // Baseline
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>("null"));

            // Per null handling default value for value types (true), converter handles null.
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Int32NullConverter_SpecialCaseNull());

            Assert.Equal(-1, JsonSerializer.Deserialize<int>("null", options));
        }

        private class Int32NullConverter_SpecialCaseNull : JsonConverter<int>
        {
            public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return -1;
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public static void ValueTypeConverter_OptOut()
        {
            // Per null handling opt-out, serializer handles null.
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Int32NullConverter_OptOut());

            // Serializer throws JsonException if null is assigned to value that can't be null.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<int>("null", options));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithInt>(@"{""MyInt"":null}", options));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<int>>("[null]", options));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, int>>(@"{""MyInt"":null}", options));
        }

        private class Int32NullConverter_OptOut : Int32NullConverter_SpecialCaseNull
        {
            public override bool HandleNull => false;
        }

        private class ClassWithInt
        {
            public int MyInt { get; set; }
        }

        [Fact]
        public static void ValueTypeConverter_NullOptIn()
        {
            // Per null handling opt-in, converter handles null.
            var options = new JsonSerializerOptions();
            options.Converters.Add(new Int32NullConverter_NullOptIn());

            Assert.Equal(-1, JsonSerializer.Deserialize<int>("null", options));
        }

        private class Int32NullConverter_NullOptIn : Int32NullConverter_SpecialCaseNull
        {
            public override bool HandleNull => true;
        }

        [Fact]
        public static void ComplexValueTypeConverter_NoOverride()
        {
            // Baseline
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Point_2D_Struct>("null"));

            var options = new JsonSerializerOptions();
            options.Converters.Add(new PointStructConverter_SpecialCaseNull());

            // Per null handling default value for value types (true), converter handles null.
            var obj = JsonSerializer.Deserialize<Point_2D_Struct>("null", options);
            Assert.Equal(-1, obj.X);
            Assert.Equal(-1, obj.Y);
        }

        private class PointStructConverter_SpecialCaseNull : JsonConverter<Point_2D_Struct>
        {
            public override Point_2D_Struct Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return new Point_2D_Struct(-1, -1);
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, Point_2D_Struct value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public static void ComplexValueTypeConverter_OptOut()
        {
            // Per null handling opt-out, serializer handles null.
            var options = new JsonSerializerOptions();
            options.Converters.Add(new PointStructConverter_OptOut());

            // Serializer throws JsonException if null is assigned to value that can't be null.
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Point_2D_Struct>("null", options));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithPoint>(@"{""MyPoint"":null}", options));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ImmutableClassWithPoint>(@"{""MyPoint"":null}", options));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<List<Point_2D_Struct>>("[null]", options));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, Point_2D_Struct>>(@"{""MyPoint"":null}", options));
        }

        private class PointStructConverter_OptOut : PointStructConverter_SpecialCaseNull
        {
            public override bool HandleNull => false;
        }

        private class ClassWithPoint
        {
            public Point_2D_Struct MyPoint { get; set; }
        }

        private class ImmutableClassWithPoint
        {
            public Point_2D_Struct MyPoint { get; }

            public ImmutableClassWithPoint(Point_2D_Struct myPoint) => MyPoint = myPoint;
        }

        [Fact]
        public static void ComplexValueTypeConverter_NullOptIn()
        {
            // Baseline
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Point_2D_Struct>("null"));

            // Per null handling opt-in, converter handles null.
            var options = new JsonSerializerOptions();
            options.Converters.Add(new PointStructConverter_NullOptIn());

            var obj = JsonSerializer.Deserialize<Point_2D_Struct>("null", options);
            Assert.Equal(-1, obj.X);
            Assert.Equal(-1, obj.Y);
        }

        private class PointStructConverter_NullOptIn : PointStructConverter_SpecialCaseNull
        {
            public override bool HandleNull => true;
        }

        [Fact]
        public static void NullableValueTypeConverter_NoOverride()
        {
            // Baseline
            int? val = JsonSerializer.Deserialize<int?>("null");
            Assert.Null(val);
            Assert.Equal("null", JsonSerializer.Serialize(val));

            // For compat, deserialize does not call converter for null token unless the type doesn't support
            // null or HandleNull is overridden and returns 'true'.
            // For compat, serialize does not call converter for null unless null is a valid value and HandleNull is true.
            var options = new JsonSerializerOptions();
            options.Converters.Add(new NullableInt32NullConverter_SpecialCaseNull());

            val = JsonSerializer.Deserialize<int?>("null", options);
            Assert.Null(val);

            val = null;
            Assert.Equal("null", JsonSerializer.Serialize(val, options));
        }

        private class NullableInt32NullConverter_SpecialCaseNull : JsonConverter<int?>
        {
            public override int? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return -1;
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, int? value, JsonSerializerOptions options)
            {
                if (!value.HasValue)
                {
                    writer.WriteNumberValue(-1);
                    return;
                }

                throw new NotSupportedException();
            }
        }

        [Fact]
        public static void NullableValueTypeConverter_OptOut()
        {
            // Baseline
            int? val = JsonSerializer.Deserialize<int?>("null");
            Assert.Null(val);
            Assert.Equal("null", JsonSerializer.Serialize(val));

            // Per null handling opt-out, serializer handles null.
            var options = new JsonSerializerOptions();
            options.Converters.Add(new NullableInt32NullConverter_NullOptOut());

            val = JsonSerializer.Deserialize<int?>("null", options);
            Assert.Null(val);
            Assert.Equal("null", JsonSerializer.Serialize(val, options));
        }

        private class NullableInt32NullConverter_NullOptOut : NullableInt32NullConverter_SpecialCaseNull
        {
            public override bool HandleNull => false;
        }

        [Fact]
        public static void ReferenceTypeConverter_NoOverride()
        {
            // Baseline
            Uri val = JsonSerializer.Deserialize<Uri>("null");
            Assert.Null(val);
            Assert.Equal("null", JsonSerializer.Serialize(val));

            // Per null handling default value for reference types (false), serializer handles null.
            var options = new JsonSerializerOptions();
            options.Converters.Add(new UriNullConverter_SpecialCaseNull());

            // Serializer sets default value.
            val = JsonSerializer.Deserialize<Uri>("null", options);
            Assert.Null(val);

            // Serializer serializes null.
            Assert.Equal("null", JsonSerializer.Serialize(val, options));
        }

        private class UriNullConverter_SpecialCaseNull : JsonConverter<Uri>
        {
            public override Uri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return new Uri("https://default");
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    writer.WriteStringValue("https://default");
                    return;
                }

                throw new NotSupportedException();
            }
        }

        [Fact]
        public static void ReferenceTypeConverter_OptOut()
        {
            // Per null handling opt-out, serializer handles null.
            var options = new JsonSerializerOptions();
            options.Converters.Add(new UriNullConverter_OptOut());

            Uri val = JsonSerializer.Deserialize<Uri>("null", options);
            Assert.Null(val);
            Assert.Equal("null", JsonSerializer.Serialize(val, options));
        }

        private class UriNullConverter_OptOut : UriNullConverter_SpecialCaseNull
        {
            public override bool HandleNull => false;
        }

        [Fact]
        public static void ReferenceTypeConverter_NullOptIn()
        {
            // Per null handling opt-in, converter handles null.
            var options = new JsonSerializerOptions();
            options.Converters.Add(new UriNullConverter_NullOptIn());

            Uri val = JsonSerializer.Deserialize<Uri>("null", options);
            Assert.Equal(new Uri("https://default"), val);

            val = null;
            Assert.Equal(@"""https://default""", JsonSerializer.Serialize(val, options));
        }

        private class UriNullConverter_NullOptIn : UriNullConverter_SpecialCaseNull
        {
            public override bool HandleNull => true;
        }

        [Fact]
        public static void ComplexReferenceTypeConverter_NoOverride()
        {
            // Baseline
            Point_2D obj = JsonSerializer.Deserialize<Point_2D>("null");
            Assert.Null(obj);
            Assert.Equal("null", JsonSerializer.Serialize(obj));

            // Per null handling default value for reference types (false), serializer handles null.
            var options = new JsonSerializerOptions();
            options.Converters.Add(new PointClassConverter_SpecialCaseNull());

            obj = JsonSerializer.Deserialize<Point_2D>("null", options);
            Assert.Null(obj);
            Assert.Equal("null", JsonSerializer.Serialize(obj));
        }

        private class PointClassConverter_SpecialCaseNull : JsonConverter<Point_2D>
        {
            public override Point_2D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return new Point_2D(-1, -1);
                }

                throw new JsonException();
            }

            public override void Write(Utf8JsonWriter writer, Point_2D value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    writer.WriteStartObject();
                    writer.WriteNumber("X", -1);
                    writer.WriteNumber("Y", -1);
                    writer.WriteEndObject();
                    return;
                }

                throw new JsonException();
            }
        }

        [Fact]
        public static void ComplexReferenceTypeConverter_NullOptIn()
        {
            // Per null handling opt-in, converter handles null.
            var options = new JsonSerializerOptions();
            options.Converters.Add(new PointClassConverter_NullOptIn());

            Point_2D obj = JsonSerializer.Deserialize<Point_2D>("null", options);
            Assert.Equal(-1, obj.X);
            Assert.Equal(-1, obj.Y);

            obj = null;
            JsonTestHelper.AssertJsonEqual(@"{""X"":-1,""Y"":-1}", JsonSerializer.Serialize(obj, options));
        }

        private class PointClassConverter_NullOptIn : PointClassConverter_SpecialCaseNull
        {
            public override bool HandleNull => true;
        }

        [Fact]
        public static void ConverterNotCalled_IgnoreNullValues()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new UriNullConverter_NullOptIn());

            // Converter is called - JsonIgnoreCondition.WhenWritingDefault does not apply to deserialization.
            ClassWithIgnoredUri obj = JsonSerializer.Deserialize<ClassWithIgnoredUri>(@"{""MyUri"":null}", options);
            Assert.Equal(new Uri("https://default"), obj.MyUri);

            obj.MyUri = null;
            // Converter is not called - value is ignored on serialization.
            Assert.Equal("{}", JsonSerializer.Serialize(obj, options));
        }

        private class ClassWithIgnoredUri
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public Uri MyUri { get; set; } = new Uri("https://microsoft.com");
        }

        [Fact]
        public static void ConverterWritesBadAmount()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new BadUriConverter());
            options.Converters.Add(new BadObjectConverter());

            // Using serializer overload in Release mode uses a writer with SkipValidation = true.
            var writerOptions = new JsonWriterOptions { SkipValidation = false };
            using (Utf8JsonWriter writer = new Utf8JsonWriter(new ArrayBufferWriter<byte>(), writerOptions))
            {
                Assert.Throws<JsonException>(() => JsonSerializer.Serialize(writer, new ClassWithUri(), options));
            }

            using (Utf8JsonWriter writer = new Utf8JsonWriter(new ArrayBufferWriter<byte>(), writerOptions))
            {
                Assert.Throws<JsonException>(() => JsonSerializer.Serialize(new StructWithObject(), options));
            }
        }

        private class BadUriConverter : UriNullConverter_NullOptIn
        {
            public override void Write(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options) { }
        }

        private class BadObjectConverter : JsonConverter<object>
        {
            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("hello");
                writer.WriteNullValue();
            }

            public override bool HandleNull => true;
        }

        private class ClassWithUri
        {
            public Uri MyUri { get; set; }
        }


        private class StructWithObject
        {
            public object MyObj { get; set; }
        }

        [Fact]
        public static void ObjectAsRootValue()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ObjectConverter());

            object obj = null;
            Assert.Equal(@"""NullObject""", JsonSerializer.Serialize(obj, options));
            Assert.Equal("NullObject", JsonSerializer.Deserialize<object>("null", options));

            options = new JsonSerializerOptions();
            options.Converters.Add(new BadObjectConverter());
            Assert.Throws<JsonException>(() => JsonSerializer.Serialize(obj, options));
        }

        [Fact]
        public static void ObjectAsCollectionElement()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new ObjectConverter());

            List<object> list = new List<object> {  null };
            Assert.Equal(@"[""NullObject""]", JsonSerializer.Serialize(list, options));

            list = JsonSerializer.Deserialize<List<object>>("[null]", options);
            Assert.Equal("NullObject", list[0]);

            options = new JsonSerializerOptions();
            options.Converters.Add(new BadObjectConverter());

            list[0] = null;
            Assert.Throws<JsonException>(() => JsonSerializer.Serialize(list, options));
        }

        public class ObjectConverter : JsonConverter<object>
        {
            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return "NullObject";
                }

                throw new NotSupportedException();
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    writer.WriteStringValue("NullObject");
                    return;
                }

                throw new NotSupportedException();
            }

            public override bool HandleNull => true;
        }

        [Fact]
        public static void SetterCalledWhenConverterReturnsNull()
        {
            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true,
                Converters = { new UriToNullConverter() }
            };

            // Baseline - null values ignored, converter is not called.
            string json = @"{""MyUri"":null}";

            ClassWithInitializedUri obj = JsonSerializer.Deserialize<ClassWithInitializedUri>(json, options);
            Assert.Equal(new Uri("https://microsoft.com"), obj.MyUri);

            // Test - setter is called if payload is not null and converter returns null.
            json = @"{""MyUri"":""https://default""}";
            obj = JsonSerializer.Deserialize<ClassWithInitializedUri>(json, options);
            Assert.Null(obj.MyUri);
        }

        private class ClassWithInitializedUri
        {
            public Uri MyUri { get; set; } = new Uri("https://microsoft.com");
        }

        public class UriToNullConverter : JsonConverter<Uri>
        {
            public override Uri Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => null;

            public override void Write(Utf8JsonWriter writer, Uri value, JsonSerializerOptions options) => throw new NotImplementedException();

            public override bool HandleNull => true;
        }
    }
}

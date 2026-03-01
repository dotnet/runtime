// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class CustomConverterTests
    {
        [Fact]
        public static void InvalidCastRefTypedPropertyFails()
        {
            var obj = new ObjectWrapperWithProperty
            {
                Object = new WrittenObject
                {
                    Int = 123,
                    String = "Hello",
                }
            };

            var json = JsonSerializer.Serialize(obj);

            var ex = Assert.Throws<InvalidCastException>(() => JsonSerializer.Deserialize<ObjectWrapperWithProperty>(json));
        }

        [Fact]
        public static void InvalidCastRefTypedFieldFails()
        {
            var options = new JsonSerializerOptions { IncludeFields = true };
            var obj = new ObjectWrapperWithField
            {
                Object = new WrittenObject
                {
                    Int = 123,
                    String = "Hello",
                }
            };

            var json = JsonSerializer.Serialize(obj);

            var ex = Assert.Throws<InvalidCastException>(() => JsonSerializer.Deserialize<ObjectWrapperWithField>(json, options));
        }

        /// <summary>
        /// A converter that intentionally deserialize a completely unrelated typed object.
        /// </summary>
        public class InvalidCastConverter : JsonConverter<object>
        {
            public override bool CanConvert(Type typeToConvert)
                => true;

            public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                JsonSerializer.Deserialize<WrittenObject>(ref reader, options);
                return new ReadObject { Double = Math.PI };
            }

            public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
            {
                JsonSerializer.Serialize<WrittenObject>(writer, (WrittenObject)value, options);
            }
        }

        private class ObjectWrapperWithProperty
        {
            [JsonConverter(typeof(InvalidCastConverter))]
            public WrittenObject Object { get; set; }
        }

        private class ObjectWrapperWithField
        {
            [JsonConverter(typeof(InvalidCastConverter))]
            public WrittenObject Object { get; set; }
        }

        private class WrittenObject
        {
            public string String { get; set; }
            public int Int { get; set; }
        }

        private class ReadObject
        {
            public double Double { get; set; }
        }

        [Fact]
        public static void CastDerivedWorks()
        {
            var options = new JsonSerializerOptions { IncludeFields = true };
            var obj = JsonSerializer.Deserialize<ObjectWrapperDerived>(@"{""DerivedProperty"":"""",""DerivedField"":""""}", options);

            Assert.IsType<Derived>(obj.DerivedField);
            Assert.IsType<Derived>(obj.DerivedProperty);
        }

        [Fact]
        public static void CastBaseWorks()
        {
            var options = new JsonSerializerOptions { IncludeFields = true };
            var obj = JsonSerializer.Deserialize<ObjectWrapperBase>(@"{""BaseProperty"":"""",""BaseField"":""""}", options);

            Assert.IsType<Derived>(obj.BaseField);
            Assert.IsType<Derived>(obj.BaseProperty);
        }

        [Fact]
        public static void CastBasePropertyFails()
        {
            var options = new JsonSerializerOptions { IncludeFields = true };
            var ex = Assert.Throws<InvalidCastException>(() => JsonSerializer.Deserialize<ObjectWrapperDerivedWithProperty>(@"{""DerivedProperty"":""""}", options));
        }

        [Fact]
        public static void CastBaseFieldFails()
        {
            var options = new JsonSerializerOptions { IncludeFields = true };
            var ex = Assert.Throws<InvalidCastException>(() => JsonSerializer.Deserialize<ObjectWrapperDerivedWithField>(@"{""DerivedField"":""""}", options));
        }

        /// <summary>
        /// A converter that deserializes an object of an derived class.
        /// </summary>
        private class BaseConverter : JsonConverter<Base>
        {
            public override bool CanConvert(Type typeToConvert)
                => true;

            public override Base Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                reader.GetString();
                return new Derived() { String = "Hello", Double = Math.PI };
            }

            public override void Write(Utf8JsonWriter writer, Base value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// A converter that deserializes an object of an derived class.
        /// </summary>
        private class DerivedConverter : JsonConverter<Derived>
        {
            public override bool CanConvert(Type typeToConvert)
                => true;

            public override Derived Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                reader.GetString();
                return new Derived() { String = "Hello", Double = Math.PI };
            }

            public override void Write(Utf8JsonWriter writer, Derived value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// A converter that deserializes an object of the base class where the wrapper expects an derived object.
        /// </summary>
        private class InvalidBaseConverter : JsonConverter<Base>
        {
            public override bool CanConvert(Type typeToConvert)
                => true;

            public override Base Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                reader.GetString();
                return new Base() { String = "Hello" };
            }

            public override void Write(Utf8JsonWriter writer, Base value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        private class Base
        {
            public string String;
        }

        private class Derived : Base
        {
            public double Double;
        }

        private class ObjectWrapperDerived
        {
            [JsonConverter(typeof(BaseConverter))]
            public Derived DerivedProperty { get; set; }
            [JsonConverter(typeof(BaseConverter))]
#pragma warning disable 0649
            public Derived DerivedField;
#pragma warning restore
        }

        private class ObjectWrapperDerivedWithProperty
        {
            [JsonConverter(typeof(InvalidBaseConverter))]
            public Derived DerivedProperty { get; set; }
        }

        private class ObjectWrapperDerivedWithField
        {
            [JsonConverter(typeof(InvalidBaseConverter))]
#pragma warning disable 0649
            public Derived DerivedField;
#pragma warning restore
        }

        private class ObjectWrapperBase
        {
            [JsonConverter(typeof(DerivedConverter))]
            public Base BaseProperty { get; set; }
            [JsonConverter(typeof(DerivedConverter))]
#pragma warning disable 0649
            public Base BaseField;
#pragma warning restore
        }
    }
}

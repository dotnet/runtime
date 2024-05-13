// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract class NullableAnnotationsTests : SerializerTests
    {
        protected NullableAnnotationsTests(JsonSerializerWrapper serializerUnderTest)
            : base(serializerUnderTest) { }

        [Fact]
        public void IgnoreNullableAnnotationsIsDisabledByDefault()
            => Assert.False(new JsonSerializerOptions().IgnoreNullableAnnotations);

        #region Read into Not Nullable
        [Fact]
        public async Task ReadNullIntoNotNullablePropertyThrows()
        {
            string json = """{"Property":null}""";

            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<NotNullablePropertyClass>(json));
            Assert.Contains("Property", ex.Message);
            Assert.Contains(typeof(NotNullablePropertyClass).ToString(), ex.Message);
        }

        [Fact]
        public async Task ReadEmptyObjectIntoTypeWithNotNullableProperty()
        {
            string json = "{}";

            NotNullablePropertyClass result = await Serializer.DeserializeWrapper<NotNullablePropertyClass>(json);
            Assert.Null(result.Property);
        }

        [Fact]
        public async Task ReadNullIntoNotNullablePropertyWithIgnoreNullableAnnotations()
        {
            string json = """{"Property":null}""";

            JsonSerializerOptions options = new() { IgnoreNullableAnnotations = true };
            NotNullablePropertyClass result = await Serializer.DeserializeWrapper<NotNullablePropertyClass>(json, options);
            Assert.Null(result.Property);
        }

        [Fact]
        public async Task ReadNullIntoReadonlyPropertySkipped()
        {
            string json = """{"ReadonlyProperty":null}""";

            NotNullableReadonlyPropertyClass result = await Serializer.DeserializeWrapper<NotNullableReadonlyPropertyClass>(json);
            Assert.Null(result.ReadonlyProperty);
        }

        [Fact]
        public async Task ReadNullIntoNotNullableFieldThrows()
        {
            string json = """{"Field":null}""";

            JsonSerializerOptions options = new() { IncludeFields = true };
            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<NotNullableFieldClass>(json, options));
            Assert.Contains("Field", ex.Message);
            Assert.Contains(typeof(NotNullableFieldClass).ToString(), ex.Message);
        }

        [Fact]
        public async Task ReadNullIntoNotNullableSpecialTypeProperties()
        {
            string json = """
                {
                  "JsonDocument":null,
                  "MemoryByte":null,
                  "ReadOnlyMemoryByte":null,
                  "MemoryOfT":null,
                  "ReadOnlyMemoryOfT":null
                }
                """;

            NotNullableSpecialTypePropertiesClass result = await Serializer.DeserializeWrapper<NotNullableSpecialTypePropertiesClass>(json);
            Assert.Equal(JsonValueKind.Null, result.JsonDocument?.RootElement.ValueKind);
            Assert.Equal(default, result.MemoryByte);
            Assert.Equal(default, result.ReadOnlyMemoryByte);
            Assert.Equal(default, result.MemoryOfT);
            Assert.Equal(default, result.ReadOnlyMemoryOfT);
        }

        [Fact]
        public async Task ReadNullIntoNotNullablePropertyWithHandleNullConverter()
        {
            string json = """{"PropertyWithHandleNullConverter":null}""";

            NotNullablePropertyWithHandleNullConverterClass result = await Serializer.DeserializeWrapper<NotNullablePropertyWithHandleNullConverterClass>(json);
            Assert.NotNull(result.PropertyWithHandleNullConverter);
        }

        [Fact]
        public async Task ReadNullIntoNotNullablePropertyWithConverterThrows()
        {
            string json = """{"PropertyWithConverter":null}""";

            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<NotNullablePropertyWithConverterClass>(json));
            Assert.Contains("PropertyWithConverter", ex.Message);
            Assert.Contains(typeof(NotNullablePropertyWithConverterClass).ToString(), ex.Message);
        }

        [Fact]
        public async Task ReadNotNullValueIntoNotNullablePropertyWithAlwaysNullConverterThrows()
        {
            string json = """{"PropertyWithAlwaysNullConverter":"42"}""";

            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<NotNullablePropertyWithAlwaysNullConverterClass>(json));
            Assert.Contains("PropertyWithAlwaysNullConverter", ex.Message);
            Assert.Contains(typeof(NotNullablePropertyWithAlwaysNullConverterClass).ToString(), ex.Message);
        }

        [Fact]
        public async Task ReadNullIntoNotNullablePropertyWithParameterizedCtorThrows()
        {
            string json = """{"CtorProperty":null}""";

            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<NotNullablePropertyParameterizedCtorClass>(json));
            Assert.Contains("CtorProperty", ex.Message);
            Assert.Contains(typeof(NotNullablePropertyParameterizedCtorClass).ToString(), ex.Message);
        }

        [Fact]
        public async Task ReadEmptyObjectIntoTypeWithNotNullablePropertyWithParameterizedCtor()
        {
            string json = "{}";

            NotNullablePropertyParameterizedCtorClass result = await Serializer.DeserializeWrapper<NotNullablePropertyParameterizedCtorClass>(json);
            Assert.Null(result.CtorProperty);
        }

        [Fact]
        public async Task ReadNullIntoNotNullablePropertyWithHandleNullConverterWithParameterizedCtor()
        {
            string json = """{"CtorPropertyWithHandleNullConverter":null}""";

            var result = await Serializer.DeserializeWrapper<NotNullablePropertyWithHandleNullConverterParameterizedCtorClass>(json);
            Assert.NotNull(result.CtorPropertyWithHandleNullConverter);
        }

        [Fact]
        public async Task ReadNotNullValueIntoNotNullablePropertyWithAlwaysNullConverterWithParameterizedCtorThrows()
        {
            string json = """{"CtorPropertyWithAlwaysNullConverter":"42"}""";

            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<NotNullablePropertyWithAlwaysNullConverterParameterizedCtorClass>(json));
            Assert.Contains("CtorPropertyWithAlwaysNullConverter", ex.Message);
            Assert.Contains(typeof(NotNullablePropertyWithAlwaysNullConverterParameterizedCtorClass).ToString(), ex.Message);
        }

        [Fact]
        public async Task ReadNullIntoNotNullablePropertyWithLargeParameterizedCtorThrows()
        {
            string json = """{"CtorProperty2":null}""";

            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<NotNullablePropertiesLargeParameterizedCtorClass>(json));
            Assert.Contains("CtorProperty2", ex.Message);
            Assert.Contains(typeof(NotNullablePropertiesLargeParameterizedCtorClass).ToString(), ex.Message);
        }

        [Fact]
        public async Task ReadEmptyObjectIntoTypeWithNotNullablePropertyWithLargeParameterizedCtor()
        {
            string json = "{}";

            NotNullablePropertiesLargeParameterizedCtorClass result = await Serializer.DeserializeWrapper<NotNullablePropertiesLargeParameterizedCtorClass>(json);
            Assert.Null(result.CtorProperty0);
            Assert.Null(result.CtorProperty1);
            Assert.Null(result.CtorProperty2);
            Assert.Null(result.CtorProperty3);
            Assert.Null(result.CtorProperty4);
        }

        [Fact]
        public async Task ReadNullIntoNotNullablePropertyWithHandleNullConverterWithLargeParameterizedCtor()
        {
            string json = """{"LargeCtorPropertyWithHandleNullConverter":null}""";

            var result = await Serializer.DeserializeWrapper<NotNullablePropertyWithHandleNullConverterLargeParameterizedCtorClass>(json);
            Assert.NotNull(result.LargeCtorPropertyWithHandleNullConverter);
        }

        [Fact]
        public async Task ReadNotNullValueIntoNotNullablePropertyWithAlwaysNullConverterWithLargeParameterizedCtorThrows()
        {
            string json = """{"LargeCtorPropertyWithAlwaysNullConverter":"42"}""";

            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<NotNullablePropertyWithAlwaysNullConverterLargeParameterizedCtorClass>(json));
            Assert.Contains("LargeCtorPropertyWithAlwaysNullConverter", ex.Message);
            Assert.Contains(typeof(NotNullablePropertyWithAlwaysNullConverterLargeParameterizedCtorClass).ToString(), ex.Message);
        }

        [Fact]
        public async Task ReadNullIntoDisallowNullPropertyThrows()
        {
            string json = """{"Property":null}""";

            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<DisallowNullPropertyClass>(json));
            Assert.Contains("Property", ex.Message);
            Assert.Contains(typeof(DisallowNullPropertyClass).ToString(), ex.Message);
        }
        #endregion Read into Not Nullable

        #region Write from Not Nullable
        [Fact]
        public async Task WriteNullFromNotNullablePropertyThrows()
        {
            NotNullablePropertyClass obj = new() { Property = null! };

            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.SerializeWrapper(obj));
            Assert.Contains("Property", ex.Message);
            Assert.Contains(typeof(NotNullablePropertyClass).ToString(), ex.Message);
        }

        [Fact]
        public async Task WriteNullFromNotNullableFieldThrows()
        {
            NotNullableFieldClass obj = new() { Field = null! };

            JsonSerializerOptions options = new() { IncludeFields = true };
            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.SerializeWrapper(obj, options));
            Assert.Contains("Field", ex.Message);
            Assert.Contains(typeof(NotNullableFieldClass).ToString(), ex.Message);
        }

        [Fact]
        public async Task WriteNullFromNotNullableJsonDocumentPropertyThrows()
        {
            NotNullableSpecialTypePropertiesClass obj = new() { JsonDocument = null! };

            // Unlike Deserialize, JsonDocument should throw on Serialize because it would write null.
            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.SerializeWrapper(obj));
            Assert.Contains("JsonDocument", ex.Message);
            Assert.Contains(typeof(NotNullableSpecialTypePropertiesClass).ToString(), ex.Message);
        }

        //[Fact]

        #endregion Write from Not Nullable

        #region Read into Nullable
        [Fact]
        public async Task ReadNullIntoNullableProperty()
        {
            string json = """{"Property":null}""";

            NullablePropertyClass result = await Serializer.DeserializeWrapper<NullablePropertyClass>(json);
            Assert.Null(result.Property);
        }

        [Fact]
        public async Task ReadEmptyObjectIntoTypeWithNullableProperty()
        {
            string json = "{}";

            NullablePropertyClass result = await Serializer.DeserializeWrapper<NullablePropertyClass>(json);
            Assert.Null(result.Property);
        }

        public async Task ReadNullIntoNullableField()
        {
            string json = """{"Field":null}""";

            NullableFieldClass result = await Serializer.DeserializeWrapper<NullableFieldClass>(json);
            Assert.Null(result.Field);
        }
        #endregion

        // Need to use **public** types; otherwise, nullability would be trimmed out.
        #region Not Nullable classes
        public class NotNullablePropertyClass
        {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
            public string Property { get; set; }
#pragma warning restore CS8618
        }

        public class NotNullableReadonlyPropertyClass
        {
#pragma warning disable CS8618
            public string ReadonlyProperty { get; }
#pragma warning restore CS8618
        }

        public class NotNullableFieldClass
        {
#pragma warning disable CS8618
            public string Field;
#pragma warning restore CS8618
        }

        public class NotNullableSpecialTypePropertiesClass
        {
            // types with internal converter that handles null.
#pragma warning disable CS8618
            public JsonDocument JsonDocument { get; set; }
#pragma warning restore CS8618
            public Memory<byte> MemoryByte { get; set; }
            public ReadOnlyMemory<byte> ReadOnlyMemoryByte { get; set; }
            public Memory<int> MemoryOfT { get; set; }
            public ReadOnlyMemory<int> ReadOnlyMemoryOfT { get; set; }
        }

        public class NotNullablePropertyWithHandleNullConverterClass
        {
#pragma warning disable CS8618
            [JsonConverter(typeof(MyHandleNullConverter))]
            public MyClass PropertyWithHandleNullConverter { get; set; }
#pragma warning restore CS8618
        }

        public class NotNullablePropertyWithAlwaysNullConverterClass
        {
#pragma warning disable CS8618
            [JsonConverter(typeof(MyAlwaysNullConverter))]
            public MyClass PropertyWithAlwaysNullConverter { get; set; }
#pragma warning restore CS8618
        }

        public class NotNullablePropertyWithConverterClass
        {
#pragma warning disable CS8618
            [JsonConverter(typeof(MyConverter))]
            public MyClass PropertyWithConverter { get; set; }
#pragma warning restore CS8618
        }

        public class MyClass { }

        public class MyHandleNullConverter : MyConverter
        {
            public override bool HandleNull => true;
        }

        public class MyConverter : JsonConverter<MyClass>
        {
            public override MyClass Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new MyClass();
            }

            public override void Write(Utf8JsonWriter writer, MyClass value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        public class MyAlwaysNullConverter : JsonConverter<MyClass>
        {
            public override bool HandleNull => true;

            public override MyClass Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return null!;
            }

            public override void Write(Utf8JsonWriter writer, MyClass value, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }
        }

        public class NotNullablePropertyParameterizedCtorClass
        {
            public string CtorProperty { get; }

            [JsonConstructor]
            public NotNullablePropertyParameterizedCtorClass(string ctorProperty) => CtorProperty = ctorProperty;
        }

        public class NotNullablePropertyWithHandleNullConverterParameterizedCtorClass
        {
            [JsonConverter(typeof(MyHandleNullConverter))]
            public MyClass CtorPropertyWithHandleNullConverter { get; }

            [JsonConstructor]
            public NotNullablePropertyWithHandleNullConverterParameterizedCtorClass(MyClass ctorPropertyWithHandleNullConverter) => CtorPropertyWithHandleNullConverter = ctorPropertyWithHandleNullConverter;
        }

        public class NotNullablePropertyWithAlwaysNullConverterParameterizedCtorClass
        {
            [JsonConverter(typeof(MyAlwaysNullConverter))]
            public MyClass CtorPropertyWithAlwaysNullConverter { get; }

            [JsonConstructor]
            public NotNullablePropertyWithAlwaysNullConverterParameterizedCtorClass(MyClass ctorPropertyWithAlwaysNullConverter) => CtorPropertyWithAlwaysNullConverter = ctorPropertyWithAlwaysNullConverter;
        }

        public class NotNullablePropertiesLargeParameterizedCtorClass
        {
            public string CtorProperty0 { get; }
            public string CtorProperty1 { get; }
            public string CtorProperty2 { get; }
            public string CtorProperty3 { get; }
            public string CtorProperty4 { get; }

            [JsonConstructor]
            public NotNullablePropertiesLargeParameterizedCtorClass(string ctorProperty0, string ctorProperty1, string ctorProperty2, string ctorProperty3, string ctorProperty4)
            {
                CtorProperty0 = ctorProperty0;
                CtorProperty1 = ctorProperty1;
                CtorProperty2 = ctorProperty2;
                CtorProperty3 = ctorProperty3;
                CtorProperty4 = ctorProperty4;
            }
        }

        public class NotNullablePropertyWithHandleNullConverterLargeParameterizedCtorClass
        {
            public string CtorProperty0 { get; }
            public string CtorProperty1 { get; }
            [JsonConverter(typeof(MyHandleNullConverter))]
            public MyClass LargeCtorPropertyWithHandleNullConverter { get; }
            public string CtorProperty3 { get; }
            public string CtorProperty4 { get; }

            [JsonConstructor]
            public NotNullablePropertyWithHandleNullConverterLargeParameterizedCtorClass(
                string ctorProperty0, string ctorProperty1, MyClass largeCtorPropertyWithHandleNullConverter, string ctorProperty3, string ctorProperty4)
            {
                CtorProperty0 = ctorProperty0;
                CtorProperty1 = ctorProperty1;
                LargeCtorPropertyWithHandleNullConverter = largeCtorPropertyWithHandleNullConverter;
                CtorProperty3 = ctorProperty3;
                CtorProperty4 = ctorProperty4;
            }
        }

        public class NotNullablePropertyWithAlwaysNullConverterLargeParameterizedCtorClass
        {
            public string CtorProperty0 { get; }
            public string CtorProperty1 { get; }
            [JsonConverter(typeof(MyAlwaysNullConverter))]
            public MyClass LargeCtorPropertyWithAlwaysNullConverter { get; }
            public string CtorProperty3 { get; }
            public string CtorProperty4 { get; }

            [JsonConstructor]
            public NotNullablePropertyWithAlwaysNullConverterLargeParameterizedCtorClass(
                string ctorProperty0, string ctorProperty1, MyClass largeCtorPropertyWithAlwaysNullConverter, string ctorProperty3, string ctorProperty4)
            {
                CtorProperty0 = ctorProperty0;
                CtorProperty1 = ctorProperty1;
                LargeCtorPropertyWithAlwaysNullConverter = largeCtorPropertyWithAlwaysNullConverter;
                CtorProperty3 = ctorProperty3;
                CtorProperty4 = ctorProperty4;
            }
        }
        #endregion Not Nullable classes

        #region [DisallowNull] classes
        public class DisallowNullPropertyClass
        {
            [DisallowNull]
            public string? Property { get; set; }
        }
        #endregion [DisallowNull] classes

        #region Nullable classes
        public class NullablePropertyClass
        {
            public string? Property { get; set; }
        }

        public class NullableFieldClass
        {
            public string? Field;
        }
        #endregion Nullable classes
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

#nullable enable annotations

namespace System.Text.Json.Serialization.Tests
{
    public abstract class NullableAnnotationsTests : SerializerTests
    {
        private static readonly JsonSerializerOptions s_optionsWithIgnoredNullability = new JsonSerializerOptions { RespectNullableAnnotations = false };
        private static readonly JsonSerializerOptions s_optionsWithEnforcedNullability = new JsonSerializerOptions { RespectNullableAnnotations = true };

        protected NullableAnnotationsTests(JsonSerializerWrapper serializerUnderTest)
            : base(serializerUnderTest) { }

        [Theory]
        [MemberData(nameof(GetTypesWithNonNullablePropertyGetter))]
        public async Task WriteNullFromNotNullablePropertyGetter_EnforcedNullability_ThrowsJsonException(Type type, string propertyName)
        {
            object value = Activator.CreateInstance(type)!;

            JsonException ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.SerializeWrapper(value, type, s_optionsWithEnforcedNullability));

            Assert.Contains(propertyName, ex.Message);
            Assert.Contains(type.Name.Split('`')[0], ex.Message);
        }

        [Theory]
        [MemberData(nameof(GetTypesWithNonNullablePropertyGetter))]
        public async Task WriteNullFromNotNullablePropertyGetter_IgnoredNullability_Succeeds(Type type, string _)
        {
            object value = Activator.CreateInstance(type)!;
            string json = await Serializer.SerializeWrapper(value, type, s_optionsWithIgnoredNullability);
            Assert.NotNull(json);
        }

        [Theory]
        [MemberData(nameof(GetTypesWithNonNullablePropertyGetter))]
        public async Task WriteNullFromNotNullablePropertyGetter_EnforcedNullability_DisabledFlag_Succeeds(Type type, string propertyName)
        {
            object value = Activator.CreateInstance(type)!;
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type, s_optionsWithEnforcedNullability, mutable: true);
            JsonPropertyInfo propertyInfo = typeInfo.Properties.FirstOrDefault(p => p.Name == propertyName);

            Assert.NotNull(propertyInfo);
            Assert.False(propertyInfo.IsGetNullable);

            propertyInfo.IsGetNullable = true;
            Assert.True(propertyInfo.IsGetNullable);

            string json = await Serializer.SerializeWrapper(value, typeInfo);
            Assert.NotNull(json);
        }

        public static IEnumerable<object[]> GetTypesWithNonNullablePropertyGetter()
        {
            yield return Wrap(typeof(NotNullablePropertyClass), nameof(NotNullablePropertyClass.Property));
            yield return Wrap(typeof(NotNullableFieldClass), nameof(NotNullableFieldClass.Field));
            yield return Wrap(typeof(NotNullablePropertyWithConverterClass), nameof(NotNullablePropertyWithConverterClass.PropertyWithConverter));
            yield return Wrap(typeof(NotNullPropertyClass), nameof(NotNullPropertyClass.Property));
            yield return Wrap(typeof(NotNullStructPropertyClass), nameof(NotNullStructPropertyClass.Property));
            yield return Wrap(typeof(NotNullPropertyClass<string>), nameof(NotNullPropertyClass<string>.Property));
            yield return Wrap(typeof(NotNullablePropertyParameterizedCtorClass), nameof(NotNullablePropertyParameterizedCtorClass.CtorProperty));
            yield return Wrap(typeof(NotNullablePropertiesLargeParameterizedCtorClass), nameof(NotNullablePropertiesLargeParameterizedCtorClass.CtorProperty2));
            yield return Wrap(typeof(NotNullableSpecialTypePropertiesClass), nameof(NotNullableSpecialTypePropertiesClass.JsonDocument));
            yield return Wrap(typeof(NullableObliviousConstructorParameter), nameof(NullableObliviousConstructorParameter.Property));
            yield return Wrap(typeof(NotNullGenericPropertyClass<string>), nameof(NotNullGenericPropertyClass<string>.Property));

            static object[] Wrap(Type type, string propertyName) => [type, propertyName];
        }

        [Theory]
        [MemberData(nameof(GetTypesWithNullablePropertyGetter))]
        public async Task WriteNullFromNullablePropertyGetter_EnforcedNullability_Succeeds(Type type, string _)
        {
            object value = Activator.CreateInstance(type)!;
            string json = await Serializer.SerializeWrapper(value, type, s_optionsWithEnforcedNullability);
            Assert.NotNull(json);
        }

        [Theory]
        [MemberData(nameof(GetTypesWithNullablePropertyGetter))]
        public async Task WriteNullFromNullablePropertyGetter_IgnoredNullability_Succeeds(Type type, string _)
        {
            object value = Activator.CreateInstance(type)!;
            string json = await Serializer.SerializeWrapper(value, type, s_optionsWithIgnoredNullability);
            Assert.NotNull(json);
        }

        [Theory]
        [MemberData(nameof(GetTypesWithNullablePropertyGetter))]
        public async Task WriteNullFromNullablePropertyGetter_EnforcedNullability_EnabledFlag_ThrowsJsonException(Type type, string propertyName)
        {
            object value = Activator.CreateInstance(type)!;
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type, s_optionsWithEnforcedNullability, mutable: true);
            JsonPropertyInfo propertyInfo = typeInfo.Properties.FirstOrDefault(p => p.Name == propertyName);

            Assert.NotNull(propertyInfo);
            Assert.True(propertyInfo.IsGetNullable);

            propertyInfo.IsGetNullable = false;
            Assert.False(propertyInfo.IsGetNullable);

            JsonException ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.SerializeWrapper(value, typeInfo));

            Assert.Contains(propertyName, ex.Message);
            Assert.Contains(type.Name.Split('`')[0], ex.Message);
        }

        public static IEnumerable<object[]> GetTypesWithNullablePropertyGetter()
        {
            yield return Wrap(typeof(NullablePropertyClass), nameof(NullablePropertyClass.Property));
            yield return Wrap(typeof(NullableFieldClass), nameof(NullableFieldClass.Field));
            yield return Wrap(typeof(NullStructPropertyClass), nameof(NullStructPropertyClass.Property));
            yield return Wrap(typeof(NullStructConstructorParameterClass), nameof(NullStructConstructorParameterClass.Property));
            yield return Wrap(typeof(MaybeNullPropertyClass), nameof(MaybeNullPropertyClass.Property));
            yield return Wrap(typeof(MaybeNullPropertyClass<string>), nameof(MaybeNullPropertyClass<string>.Property));
            yield return Wrap(typeof(NullableObliviousPropertyClass), nameof(NullableObliviousPropertyClass.Property));
            yield return Wrap(typeof(GenericPropertyClass<string>), nameof(GenericPropertyClass<string>.Property));
            yield return Wrap(typeof(NullableGenericPropertyClass<string>), nameof(NullableGenericPropertyClass<string>.Property));

            static object[] Wrap(Type type, string propertyName) => [type, propertyName];
        }

        [Theory]
        [MemberData(nameof(GetTypesWithNonNullablePropertySetter))]
        public async Task ReadNullIntoNotNullablePropertySetter_EnforcedNullability_ThrowsJsonException(Type type, string propertyName)
        {
            string json = $$"""{"{{propertyName}}":null}""";

            JsonException ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper(json, type, s_optionsWithEnforcedNullability));

            Assert.Contains(propertyName, ex.Message);
            Assert.Contains(type.Name.Split('`')[0], ex.Message);
        }

        [Theory]
        [MemberData(nameof(GetTypesWithNonNullablePropertySetter))]
        public async Task ReadNullIntoNotNullablePropertySetter_IgnoredNullability_Succeeds(Type type, string propertyName)
        {
            string json = $$"""{"{{propertyName}}":null}""";
            object? result = await Serializer.DeserializeWrapper(json, type, s_optionsWithIgnoredNullability);
            Assert.IsType(type, result);
        }

        [Theory]
        [MemberData(nameof(GetTypesWithNonNullablePropertySetter))]
        public async Task ReadEmptyObjectIntoNotNullablePropertySetter_Succeeds(Type type, string _)
        {
            object result = await Serializer.DeserializeWrapper("{}", type, s_optionsWithEnforcedNullability);
            Assert.IsType(type, result);
        }

        [Theory]
        [MemberData(nameof(GetTypesWithNonNullablePropertySetter))]
        public async Task ReadNullIntoNotNullablePropertySetter_EnforcedNullability_DisabledFlag_Succeeds(Type type, string propertyName)
        {
            string json = $$"""{"{{propertyName}}":null}""";
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type, s_optionsWithEnforcedNullability, mutable: true);
            JsonPropertyInfo propertyInfo = typeInfo.Properties.FirstOrDefault(p => p.Name == propertyName);

            Assert.NotNull(propertyInfo);
            Assert.False(propertyInfo.IsSetNullable);

            propertyInfo.IsSetNullable = true;
            Assert.True(propertyInfo.IsSetNullable);

            object? result = await Serializer.DeserializeWrapper(json, typeInfo);
            Assert.IsType(type, result);
        }

        public static IEnumerable<object[]> GetTypesWithNonNullablePropertySetter()
        {
            yield return Wrap(typeof(NotNullablePropertyClass), nameof(NotNullablePropertyClass.Property));
            yield return Wrap(typeof(NotNullableFieldClass), nameof(NotNullableFieldClass.Field));
            yield return Wrap(typeof(NotNullablePropertyWithConverterClass), nameof(NotNullablePropertyWithConverterClass.PropertyWithConverter));
            yield return Wrap(typeof(DisallowNullPropertyClass), nameof(DisallowNullPropertyClass.Property));
            yield return Wrap(typeof(DisallowNullStructPropertyClass), nameof(DisallowNullStructPropertyClass.Property));
            yield return Wrap(typeof(DisallowNullStructConstructorParameter), nameof(DisallowNullStructConstructorParameter.Property));
            yield return Wrap(typeof(DisallowNullPropertyClass<string>), nameof(DisallowNullPropertyClass<string>.Property));
            yield return Wrap(typeof(NotNullablePropertyParameterizedCtorClass), nameof(NotNullablePropertyParameterizedCtorClass.CtorProperty));
            yield return Wrap(typeof(NotNullablePropertiesLargeParameterizedCtorClass), nameof(NotNullablePropertiesLargeParameterizedCtorClass.CtorProperty2));
            yield return Wrap(typeof(NotNullGenericPropertyClass<string>), nameof(NotNullGenericPropertyClass<string>.Property));
            yield return Wrap(typeof(DisallowNullConstructorParameter), nameof(DisallowNullConstructorParameter.Property));
            yield return Wrap(typeof(DisallowNullConstructorParameter<string>), nameof(DisallowNullConstructorParameter<string>.Property));
            yield return Wrap(typeof(NotNullGenericConstructorParameter<string>), nameof(NotNullGenericConstructorParameter<string>.Property));

            static object[] Wrap(Type type, string propertyName) => [type, propertyName];
        }

        [Theory]
        [MemberData(nameof(GetTypesWithNullablePropertySetter))]
        public async Task ReadNullIntoNullablePropertySetter_EnforcedNullability_Succeeds(Type type, string propertyName)
        {
            string json = $$"""{"{{propertyName}}":null}""";
            object? result = await Serializer.DeserializeWrapper(json, type, s_optionsWithEnforcedNullability);
            Assert.IsType(type, result);
        }

        [Theory]
        [MemberData(nameof(GetTypesWithNullablePropertySetter))]
        public async Task ReadNullIntoNullablePropertySetter_IgnoredNullability_Succeeds(Type type, string propertyName)
        {
            string json = $$"""{"{{propertyName}}":null}""";
            object? result = await Serializer.DeserializeWrapper(json, type, s_optionsWithIgnoredNullability);
            Assert.IsType(type, result);
        }

        [Theory]
        [MemberData(nameof(GetTypesWithNullablePropertySetter))]
        public async Task ReadNullIntoNullablePropertySetter_EnforcedNullability_EnabledFlag_ThrowsJsonException(Type type, string propertyName)
        {
            string json = $$"""{"{{propertyName}}":null}""";
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type, s_optionsWithEnforcedNullability, mutable: true);
            JsonPropertyInfo propertyInfo = typeInfo.Properties.FirstOrDefault(p => p.Name == propertyName);

            Assert.NotNull(propertyInfo);
            Assert.True(propertyInfo.IsSetNullable);

            propertyInfo.IsSetNullable = false;
            Assert.False(propertyInfo.IsSetNullable);

            JsonException ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper(json, typeInfo));

            Assert.Contains(propertyName, ex.Message);
            Assert.Contains(type.Name.Split('`')[0], ex.Message);
        }

        public static IEnumerable<object[]> GetTypesWithNullablePropertySetter()
        {
            yield return Wrap(typeof(NullablePropertyClass), nameof(NullablePropertyClass.Property));
            yield return Wrap(typeof(NullableFieldClass), nameof(NullableFieldClass.Field));
            yield return Wrap(typeof(NullStructPropertyClass), nameof(NullStructPropertyClass.Property));
            yield return Wrap(typeof(NullStructConstructorParameterClass), nameof(NullStructConstructorParameterClass.Property));
            yield return Wrap(typeof(AllowNullPropertyClass), nameof(AllowNullPropertyClass.Property));
            yield return Wrap(typeof(AllowNullPropertyClass<string>), nameof(AllowNullPropertyClass<string>.Property));
            yield return Wrap(typeof(NullableObliviousPropertyClass), nameof(NullableObliviousPropertyClass.Property));
            yield return Wrap(typeof(NullableObliviousConstructorParameter), nameof(NullableObliviousConstructorParameter.Property));
            yield return Wrap(typeof(GenericPropertyClass<string>), nameof(GenericPropertyClass<string>.Property));
            yield return Wrap(typeof(NullableGenericPropertyClass<string>), nameof(NullableGenericPropertyClass<string>.Property));
            yield return Wrap(typeof(AllowNullConstructorParameter), nameof(AllowNullConstructorParameter.Property));
            yield return Wrap(typeof(AllowNullConstructorParameter<string>), nameof(AllowNullConstructorParameter<string>.Property));
            yield return Wrap(typeof(GenericConstructorParameter<string>), nameof(GenericConstructorParameter<string>.Property));
            yield return Wrap(typeof(NullableGenericConstructorParameter<string>), nameof(NullableGenericConstructorParameter<string>.Property));

            static object[] Wrap(Type type, string propertyName) => [type, propertyName];
        }

        [Fact]
        public async Task ReadNullIntoReadonlyProperty_Succeeds()
        {
            string json = """{"ReadonlyProperty":null}""";
            NotNullableReadonlyPropertyClass result = await Serializer.DeserializeWrapper<NotNullableReadonlyPropertyClass>(json, s_optionsWithEnforcedNullability);
            Assert.Null(result.ReadonlyProperty);
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

            NotNullableSpecialTypePropertiesClass result = await Serializer.DeserializeWrapper<NotNullableSpecialTypePropertiesClass>(json, s_optionsWithEnforcedNullability);
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

            NotNullablePropertyWithHandleNullConverterClass result = await Serializer.DeserializeWrapper<NotNullablePropertyWithHandleNullConverterClass>(json, s_optionsWithEnforcedNullability);
            Assert.NotNull(result.PropertyWithHandleNullConverter);
        }

        [Fact]
        public async Task ReadNotNullValueIntoNotNullablePropertyWithAlwaysNullConverterThrows()
        {
            string json = """{"PropertyWithAlwaysNullConverter":"42"}""";

            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<NotNullablePropertyWithAlwaysNullConverterClass>(json, s_optionsWithEnforcedNullability));
            Assert.Contains("PropertyWithAlwaysNullConverter", ex.Message);
            Assert.Contains(nameof(NotNullablePropertyWithAlwaysNullConverterClass), ex.Message);
        }

        [Fact]
        public async Task ReadNullIntoNotNullablePropertyWithHandleNullConverterWithParameterizedCtor()
        {
            string json = """{"CtorPropertyWithHandleNullConverter":null}""";

            var result = await Serializer.DeserializeWrapper<NotNullablePropertyWithHandleNullConverterParameterizedCtorClass>(json, s_optionsWithEnforcedNullability);
            Assert.NotNull(result.CtorPropertyWithHandleNullConverter);
        }

        [Fact]
        public async Task ReadNotNullValueIntoNotNullablePropertyWithAlwaysNullConverterWithParameterizedCtorThrows()
        {
            string json = """{"CtorPropertyWithAlwaysNullConverter":"42"}""";

            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<NotNullablePropertyWithAlwaysNullConverterParameterizedCtorClass>(json, s_optionsWithEnforcedNullability));
            Assert.Contains("CtorPropertyWithAlwaysNullConverter", ex.Message);
            Assert.Contains(nameof(NotNullablePropertyWithAlwaysNullConverterParameterizedCtorClass), ex.Message);
        }

        [Fact]
        public async Task ReadNullIntoNotNullablePropertyWithHandleNullConverterWithLargeParameterizedCtor()
        {
            string json = """{"LargeCtorPropertyWithHandleNullConverter":null}""";

            var result = await Serializer.DeserializeWrapper<NotNullablePropertyWithHandleNullConverterLargeParameterizedCtorClass>(json, s_optionsWithEnforcedNullability);
            Assert.NotNull(result.LargeCtorPropertyWithHandleNullConverter);
        }

        [Fact]
        public async Task ReadNotNullValueIntoNotNullablePropertyWithAlwaysNullConverterWithLargeParameterizedCtorThrows()
        {
            string json = """{"LargeCtorPropertyWithAlwaysNullConverter":"42"}""";

            Exception ex = await Assert.ThrowsAsync<JsonException>(() => Serializer.DeserializeWrapper<NotNullablePropertyWithAlwaysNullConverterLargeParameterizedCtorClass>(json, s_optionsWithEnforcedNullability));
            Assert.Contains("LargeCtorPropertyWithAlwaysNullConverter", ex.Message);
            Assert.Contains(nameof(NotNullablePropertyWithAlwaysNullConverterLargeParameterizedCtorClass), ex.Message);
        }

        [Fact]
        public async Task WriteNotNullPropertiesWithNullIgnoreConditions_Succeeds()
        {
            // JsonIgnoreCondition.WhenWritingNull/Default takes precedence over nullability enforcement.
            var value = new NotNullablePropertyWithIgnoreConditions { WhenWritingNull = null!, WhenWritingDefault = null! };
            string json = await Serializer.SerializeWrapper(value, s_optionsWithEnforcedNullability);
            Assert.Equal("{}", json);
        }

        public class NotNullablePropertyClass
        {
            public string Property { get; set; }
        }

        public class NullableObliviousPropertyClass
        {
#nullable disable annotations
            public string Property { get; set; }
#nullable restore annotations
        }

        public class NullableObliviousConstructorParameter
        {
            public string Property { get; set; }

            public NullableObliviousConstructorParameter() { }

            [JsonConstructor]
#nullable disable annotations
            public NullableObliviousConstructorParameter(string property)
#nullable restore annotations
            {
                Property = property;
            }
        }

        public class NotNullableReadonlyPropertyClass
        {
            public string ReadonlyProperty { get; }
        }

        public class NotNullableFieldClass
        {
            [JsonInclude]
            public string Field;
        }

        public class NotNullableSpecialTypePropertiesClass
        {
            // types with internal converter that handles null.
            public JsonDocument JsonDocument { get; set; }
            public Memory<byte> MemoryByte { get; set; }
            public ReadOnlyMemory<byte> ReadOnlyMemoryByte { get; set; }
            public Memory<int> MemoryOfT { get; set; }
            public ReadOnlyMemory<int> ReadOnlyMemoryOfT { get; set; }
        }

        public class NotNullablePropertyWithHandleNullConverterClass
        {
            [JsonConverter(typeof(MyHandleNullConverter))]
            public MyClass PropertyWithHandleNullConverter { get; set; }
        }

        public class NotNullablePropertyWithAlwaysNullConverterClass
        {
            [JsonConverter(typeof(MyAlwaysNullConverter))]
            public MyClass PropertyWithAlwaysNullConverter { get; set; }
        }

        public class NotNullablePropertyWithConverterClass
        {
            [JsonConverter(typeof(MyConverter))]
            public MyClass PropertyWithConverter { get; set; }
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

            public NotNullablePropertyParameterizedCtorClass() { }

            [JsonConstructor]
            public NotNullablePropertyParameterizedCtorClass(string ctorProperty) => CtorProperty = ctorProperty;
        }

        public class NotNullablePropertyWithHandleNullConverterParameterizedCtorClass
        {
            [JsonConverter(typeof(MyHandleNullConverter))]
            public MyClass CtorPropertyWithHandleNullConverter { get; }

            public NotNullablePropertyWithHandleNullConverterParameterizedCtorClass() { }

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

            public NotNullablePropertiesLargeParameterizedCtorClass()
            {
                CtorProperty0 = "str";
                CtorProperty1 = "str";
                // CtorProperty2 intentionally left uninitialized.
                CtorProperty3 = "str";
                CtorProperty4 = "str";
            }

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

        public class NotNullPropertyClass
        {
            [NotNull]
            public string? Property { get; set; }
        }

        public class MaybeNullPropertyClass
        {
            [MaybeNull]
            public string Property { get; set; }
        }

        public class AllowNullPropertyClass
        {
            [AllowNull]
            public string Property { get; set; }
        }

        public class DisallowNullPropertyClass
        {
            [DisallowNull]
            public string? Property { get; set; }
        }

        public class AllowNullConstructorParameter
        {
            public string? Property { get; set; }

            public AllowNullConstructorParameter() { }

            [JsonConstructor]
            public AllowNullConstructorParameter([AllowNull] string property)
            {
                Property = property;
            }
        }

        public class DisallowNullConstructorParameter
        {
            public string Property { get; set; }

            public DisallowNullConstructorParameter() { }

            [JsonConstructor]
            public DisallowNullConstructorParameter([DisallowNull] string? property)
            {
                Property = property;
            }
        }

        public class NullStructPropertyClass
        {
            public int? Property { get; set; }
        }

        public class NullStructConstructorParameterClass
        {
            public int? Property { get; set; }

            public NullStructConstructorParameterClass() { }

            [JsonConstructor]
            public NullStructConstructorParameterClass(int? property)
            {
                Property = property;
            }
        }

        public class NotNullStructPropertyClass
        {
            [NotNull]
            public int? Property { get; set; }
        }

        public class DisallowNullStructPropertyClass
        {
            [DisallowNull]
            public int? Property { get; set; }
        }

        public class DisallowNullStructConstructorParameter
        {
            public int? Property { get; set; }

            public DisallowNullStructConstructorParameter() { }

            [JsonConstructor]
            public DisallowNullStructConstructorParameter([DisallowNull] int? property)
            {
                Property = property;
            }
        }

        public class NotNullPropertyClass<T>
        {
            [NotNull]
            public T? Property { get; set; }
        }

        public class MaybeNullPropertyClass<T>
        {
            [MaybeNull]
            public T Property { get; set; }
        }

        public class AllowNullPropertyClass<T>
        {
            [AllowNull]
            public T Property { get; set; }
        }

        public class DisallowNullPropertyClass<T>
        {
            [DisallowNull]
            public T? Property { get; set; }
        }

        public class AllowNullConstructorParameter<T>
        {
            public T? Property { get; set; }

            public AllowNullConstructorParameter() { }

            [JsonConstructor]
            public AllowNullConstructorParameter([AllowNull] T property)
            {
                Property = property;
            }
        }

        public class DisallowNullConstructorParameter<T>
        {
            public T Property { get; set; }

            public DisallowNullConstructorParameter() { }

            [JsonConstructor]
            public DisallowNullConstructorParameter([DisallowNull] T? property)
            {
                Property = property;
            }
        }

        public class GenericPropertyClass<T>
        {
            public T Property { get; set; }
        }

        public class NullableGenericPropertyClass<T>
        {
            public T? Property { get; set; }
        }

        public class NotNullGenericPropertyClass<T> where T : notnull
        {
            public T Property { get; set; }
        }

        public class GenericConstructorParameter<T>
        {
            public T Property { get; set; }

            public GenericConstructorParameter() { }

            [JsonConstructor]
            public GenericConstructorParameter(T property)
            {
                Property = property;
            }
        }

        public class NullableGenericConstructorParameter<T>
        {
            public T? Property { get; set; }

            public NullableGenericConstructorParameter() { }

            [JsonConstructor]
            public NullableGenericConstructorParameter(T? property)
            {
                Property = property;
            }
        }

        public class NotNullGenericConstructorParameter<T> where T : notnull
        {
            public T Property { get; set; }

            public NotNullGenericConstructorParameter() { }

            [JsonConstructor]
            public NotNullGenericConstructorParameter(T property)
            {
                Property = property;
            }
        }

        public class NotNullablePropertyWithIgnoreConditions
        {
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string WhenWritingNull { get; set; }

            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string WhenWritingDefault { get; set; }
        }

        public class NullablePropertyClass
        {
            public string? Property { get; set; }
        }

        public class NullableFieldClass
        {
            [JsonInclude]
            public string? Field;
        }
    }
}

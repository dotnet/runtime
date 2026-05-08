// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public static partial class JsonSourceGenerationOptionsTests
    {
        [Fact]
        public static void ContextWithGeneralSerializerDefaults_GeneratesExpectedOptions()
        {
            JsonSerializerOptions expected = new(JsonSerializerDefaults.General) { TypeInfoResolver = ContextWithGeneralSerializerDefaults.Default };
            JsonSerializerOptions options = ContextWithGeneralSerializerDefaults.Default.Options;

            JsonTestHelper.AssertOptionsEqual(expected, options);
        }

        [JsonSourceGenerationOptions(JsonSerializerDefaults.General)]
        [JsonSerializable(typeof(PersonStruct))]
        public partial class ContextWithGeneralSerializerDefaults : JsonSerializerContext
        { }

        [Fact]
        public static void ContextWithWebSerializerDefaults_GeneratesExpectedOptions()
        {
            JsonSerializerOptions expected = new(JsonSerializerDefaults.Web) { TypeInfoResolver = ContextWithWebSerializerDefaults.Default };
            JsonSerializerOptions options = ContextWithWebSerializerDefaults.Default.Options;

            JsonTestHelper.AssertOptionsEqual(expected, options);
        }

        [JsonSourceGenerationOptions(JsonSerializerDefaults.Web)]
        [JsonSerializable(typeof(PersonStruct))]
        public partial class ContextWithWebSerializerDefaults : JsonSerializerContext
        { }

        [Fact]
        public static void ContextWithStrictSerializerDefaults_GeneratesExpectedOptions()
        {
            JsonSerializerOptions expected = new(JsonSerializerDefaults.Strict) { TypeInfoResolver = ContextWithStrictSerializerDefaults.Default };
            JsonSerializerOptions options = ContextWithStrictSerializerDefaults.Default.Options;

            JsonTestHelper.AssertOptionsEqual(expected, options);
        }

        [JsonSourceGenerationOptions(JsonSerializerDefaults.Strict)]
        [JsonSerializable(typeof(PersonStruct))]
        public partial class ContextWithStrictSerializerDefaults : JsonSerializerContext
        { }

        [Fact]
        public static void ContextWithWebDefaultsAndOverriddenPropertyNamingPolicy_GeneratesExpectedOptions()
        {
            JsonSerializerOptions expected = new(JsonSerializerDefaults.Web)
            {
                PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower,
                TypeInfoResolver = ContextWithWebDefaultsAndOverriddenPropertyNamingPolicy.Default,
            };

            JsonSerializerOptions options = ContextWithWebDefaultsAndOverriddenPropertyNamingPolicy.Default.Options;

            JsonTestHelper.AssertOptionsEqual(expected, options);
        }

        [JsonSourceGenerationOptions(JsonSerializerDefaults.Web, PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseLower)]
        [JsonSerializable(typeof(PersonStruct))]
        public partial class ContextWithWebDefaultsAndOverriddenPropertyNamingPolicy : JsonSerializerContext
        { }

        [Fact]
        public static void ContextWithAllOptionsSet_GeneratesExpectedOptions()
        {
            JsonSerializerOptions expected = new(JsonSerializerDefaults.Web)
            {
                AllowOutOfOrderMetadataProperties = true,
                AllowTrailingCommas = true,
                Converters = { new JsonStringEnumConverter<BindingFlags>(), new JsonStringEnumConverter<JsonIgnoreCondition>() },
                DefaultBufferSize = 128,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
                DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseUpper,
                IgnoreReadOnlyFields = true,
                IgnoreReadOnlyProperties = true,
                IncludeFields = true,
                MaxDepth = 1024,
                NewLine = "\n",
                NumberHandling = JsonNumberHandling.WriteAsString,
                PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.KebabCaseUpper,
                ReadCommentHandling = JsonCommentHandling.Skip,
                ReferenceHandler = ReferenceHandler.Preserve,
                RespectNullableAnnotations = true,
                RespectRequiredConstructorParameters = true,
                UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
                UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
                WriteIndented = true,
                IndentCharacter = '\t',
                IndentSize = 1,
                AllowDuplicateProperties = false,

                TypeInfoResolver = ContextWithAllOptionsSet.Default,
            };

            JsonSerializerOptions options = ContextWithAllOptionsSet.Default.Options;

            JsonTestHelper.AssertOptionsEqual(expected, options);
        }

        [JsonSourceGenerationOptions(JsonSerializerDefaults.Web,
            AllowOutOfOrderMetadataProperties = true,
            AllowTrailingCommas = true,
            Converters = [typeof(JsonStringEnumConverter<BindingFlags>), typeof(JsonStringEnumConverter<JsonIgnoreCondition>)],
            DefaultBufferSize = 128,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            DictionaryKeyPolicy = JsonKnownNamingPolicy.SnakeCaseUpper,
            IgnoreReadOnlyFields = true,
            IgnoreReadOnlyProperties = true,
            IncludeFields = true,
            MaxDepth = 1024,
            NewLine = "\n",
            NumberHandling = JsonNumberHandling.WriteAsString,
            PreferredObjectCreationHandling = JsonObjectCreationHandling.Replace,
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonKnownNamingPolicy.KebabCaseUpper,
            ReadCommentHandling = JsonCommentHandling.Skip,
            ReferenceHandler = JsonKnownReferenceHandler.Preserve,
            RespectNullableAnnotations = true,
            RespectRequiredConstructorParameters = true,
            UnknownTypeHandling = JsonUnknownTypeHandling.JsonNode,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
            WriteIndented = true,
            IndentCharacter = '\t',
            IndentSize = 1,
            AllowDuplicateProperties = false)]
        [JsonSerializable(typeof(PersonStruct))]
        public partial class ContextWithAllOptionsSet : JsonSerializerContext
        { }

        [Fact]
        public static void ContextWithInvalidSerializerDefaults_ThrowsArgumentOutOfRangeException()
        {
            TypeInitializationException ex = Assert.Throws<TypeInitializationException>(() => ContextWithInvalidSerializerDefaults.Default);
            ArgumentOutOfRangeException inner = Assert.IsType<ArgumentOutOfRangeException>(ex.InnerException);
            Assert.Contains("defaults", inner.Message);
        }

        [JsonSourceGenerationOptions((JsonSerializerDefaults)(-1))]
        [JsonSerializable(typeof(PersonStruct))]
        public partial class ContextWithInvalidSerializerDefaults : JsonSerializerContext
        { }

        [Fact]
        public static void UseStringEnumConverter_EnablesDefaultStringEnumSerialization()
        {
            var value = new ClassWithEnumProperty { StringValue = MyEnum.A, NumberValue = MyEnum.A };
            string expectedJson = """{"StringValue":"A","NumberValue":0}""";

            string json = JsonSerializer.Serialize(value, ContextWithStringEnumConverterEnabled.Default.ClassWithEnumProperty);
            Assert.Equal(expectedJson, json);

            value = JsonSerializer.Deserialize(json, ContextWithStringEnumConverterEnabled.Default.ClassWithEnumProperty);
            Assert.Equal(MyEnum.A, value.StringValue);
            Assert.Equal(MyEnum.A, value.NumberValue);
        }

        public class ClassWithEnumProperty
        {
            public MyEnum StringValue { get; set; }

            [JsonConverter(typeof(JsonNumberEnumConverter<MyEnum>))]
            public MyEnum NumberValue { get; set; }
        }

        public enum MyEnum { A = 0, B = 1, C = 2 }

        [JsonSourceGenerationOptions(UseStringEnumConverter = true)]
        [JsonSerializable(typeof(ClassWithEnumProperty))]
        public partial class ContextWithStringEnumConverterEnabled : JsonSerializerContext
        { }

        [Fact]
        public static void AttributeWithGeneralSerializerDefaults_SetsPropertiesCorrectly()
        {
            var attr = new JsonSourceGenerationOptionsAttribute(JsonSerializerDefaults.General);
            
            // General uses default property values
            Assert.False(attr.PropertyNameCaseInsensitive);
            Assert.Equal(JsonKnownNamingPolicy.Unspecified, attr.PropertyNamingPolicy);
            Assert.Equal(JsonNumberHandling.Strict, attr.NumberHandling);
            Assert.Equal(JsonUnmappedMemberHandling.Skip, attr.UnmappedMemberHandling);
            Assert.False(attr.AllowDuplicateProperties);
            Assert.False(attr.RespectNullableAnnotations);
            Assert.False(attr.RespectRequiredConstructorParameters);
        }

        [Fact]
        public static void AttributeWithWebSerializerDefaults_SetsPropertiesCorrectly()
        {
            var attr = new JsonSourceGenerationOptionsAttribute(JsonSerializerDefaults.Web);
            
            // Web sets PropertyNameCaseInsensitive=true, PropertyNamingPolicy=CamelCase, and NumberHandling=AllowReadingFromString
            Assert.True(attr.PropertyNameCaseInsensitive);
            Assert.Equal(JsonKnownNamingPolicy.CamelCase, attr.PropertyNamingPolicy);
            Assert.Equal(JsonNumberHandling.AllowReadingFromString, attr.NumberHandling);
            
            // Other properties should be default
            Assert.Equal(JsonUnmappedMemberHandling.Skip, attr.UnmappedMemberHandling);
            Assert.False(attr.AllowDuplicateProperties);
            Assert.False(attr.RespectNullableAnnotations);
            Assert.False(attr.RespectRequiredConstructorParameters);
        }

        [Fact]
        public static void AttributeWithStrictSerializerDefaults_SetsPropertiesCorrectly()
        {
            var attr = new JsonSourceGenerationOptionsAttribute(JsonSerializerDefaults.Strict);
            
            // Strict sets UnmappedMemberHandling=Disallow, AllowDuplicateProperties=false, RespectNullableAnnotations=true, and RespectRequiredConstructorParameters=true
            Assert.Equal(JsonUnmappedMemberHandling.Disallow, attr.UnmappedMemberHandling);
            Assert.False(attr.AllowDuplicateProperties);
            Assert.True(attr.RespectNullableAnnotations);
            Assert.True(attr.RespectRequiredConstructorParameters);
            
            // Other properties should be default
            Assert.False(attr.PropertyNameCaseInsensitive);
            Assert.Equal(JsonKnownNamingPolicy.Unspecified, attr.PropertyNamingPolicy);
            Assert.Equal(JsonNumberHandling.Strict, attr.NumberHandling);
        }
    }
}

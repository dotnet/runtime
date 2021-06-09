// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public static class SerializationLogicTests
    {
        private static JsonSerializerOptions s_compatibleOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        [Theory]
        [MemberData(nameof(GetOptionsUsingDeserializationOnlyFeatures))]
        [MemberData(nameof(GetCompatibleOptions))]
        public static void SerializationFuncInvokedWhenSupported(JsonSerializerOptions options)
        {
            JsonMessage message = new();

            // Per context implementation, NotImplementedException thrown because the options are compatible, hence the serialization func is invoked.
            JsonContext context = new(options);
            Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(message, context.JsonMessage));
            Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(message, typeof(JsonMessage), context));
        }

        [Theory]
        [MemberData(nameof(GetOptionsUsingUnsupportedFeatures))]
        [MemberData(nameof(GetIncompatibleOptions))]
        public static void SerializationFuncNotInvokedWhenNotSupported(JsonSerializerOptions options)
        {
            JsonMessage message = new();

            // Per context implementation, NotImplementedException thrown because the options are compatible, hence the serialization func is invoked.
            Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(message, JsonContext.Default.JsonMessage));
            Assert.Throws<NotImplementedException>(() => JsonSerializer.Serialize(message, typeof(JsonMessage), JsonContext.Default));

            // NotSupportedException thrown because
            // - the options are not compatible, hence the serialization func is not invoked.
            // - the serializer correctly tries to serialize based on property metadata, but we have not provided it in our implementation.
            JsonContext context = new(options);
            JsonTestHelper.AssertThrows_PropMetadataInit(() => JsonSerializer.Serialize(message, context.JsonMessage), typeof(JsonMessage));
            JsonTestHelper.AssertThrows_PropMetadataInit(() => JsonSerializer.Serialize(message, typeof(JsonMessage), context), typeof(JsonMessage));
        }

        [Fact]
        public static void DictionaryFastPathPrimitiveValueSupported()
        {
            Assert.NotNull(DictionaryTypeContext.Default.DictionarySystemStringSystemString.Serialize);
            Assert.NotNull(DictionaryTypeContext.Default.DictionarySystemStringSystemTextJsonSourceGenerationTestsJsonMessage.Serialize);
            Assert.NotNull(DictionaryTypeContext.Default.JsonMessage.Serialize);
            Assert.Null(DictionaryTypeContext.Default.String.Serialize);
            Assert.Null(DictionaryTypeContext.Default.Int32.Serialize);
        }

        // Options with features that apply only to deserialization.
        public static IEnumerable<object[]> GetOptionsUsingDeserializationOnlyFeatures()
        {
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { AllowTrailingCommas = true } };
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { DefaultBufferSize = 8192 } };
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { PropertyNameCaseInsensitive = true } };
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { ReadCommentHandling = JsonCommentHandling.Skip } };
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { NumberHandling = JsonNumberHandling.AllowReadingFromString } };
        }

        /// <summary>
        /// Options compatible with <see cref="JsonContext.s_defaultOptions"/>.
        /// </summary>
        public static IEnumerable<object[]> GetCompatibleOptions()
        {
            yield return new object[] { s_compatibleOptions };
            yield return new object[] { new JsonSerializerOptions(JsonSerializerDefaults.Web) { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault } };
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { NumberHandling = JsonNumberHandling.Strict } };
        }

        // Options with features that aren't supported in the generated serialization funcs.
        public static IEnumerable<object[]> GetOptionsUsingUnsupportedFeatures()
        {
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { Converters = { new JsonStringEnumConverter() } } };
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping } };
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { NumberHandling = JsonNumberHandling.WriteAsString } };
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals } };
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { NumberHandling = JsonNumberHandling.WriteAsString | JsonNumberHandling.AllowNamedFloatingPointLiterals } };
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { ReferenceHandler = ReferenceHandler.Preserve } };
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { ReferenceHandler = ReferenceHandler.IgnoreCycles } };
#pragma warning disable SYSLIB0020 // Type or member is obsolete
            yield return new object[] { new JsonSerializerOptions { IgnoreNullValues = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase } };
#pragma warning restore SYSLIB0020 // Type or member is obsolete
        }

        /// <summary>
        /// Options incompatible with <see cref="JsonContext.s_defaultOptions"/>.
        /// </summary>
        public static IEnumerable<object[]> GetIncompatibleOptions()
        {
            yield return new object[] { new JsonSerializerOptions() };
            yield return new object[] { new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } } };
            yield return new object[] { new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping } };
            yield return new object[] { new JsonSerializerOptions { NumberHandling = JsonNumberHandling.WriteAsString } };
            yield return new object[] { new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve } };
            yield return new object[] { new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles } };
            yield return new object[] { new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.Never } };
            yield return new object[] { new JsonSerializerOptions { IgnoreReadOnlyFields = true } };
            yield return new object[] { new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault } };
            yield return new object[] { new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase } };
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { DefaultIgnoreCondition = JsonIgnoreCondition.Never } };
            yield return new object[] { new JsonSerializerOptions(s_compatibleOptions) { IgnoreReadOnlyFields = true } };
        }
    }
}

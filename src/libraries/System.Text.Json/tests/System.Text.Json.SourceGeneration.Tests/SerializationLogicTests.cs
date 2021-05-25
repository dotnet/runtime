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

        // Options with features that aren't supported in generated serialization funcs.
        public static IEnumerable<object[]> GetOptionsUsingUnsupportedFeatures()
        {
            yield return new object[] { new JsonSerializerOptions { Converters = { new JsonStringEnumConverter() } } };
            yield return new object[] { new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping } };
            yield return new object[] { new JsonSerializerOptions { NumberHandling = JsonNumberHandling.WriteAsString } };
            yield return new object[] { new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve } };
            yield return new object[] { new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles } };
            yield return new object[] { new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.IgnoreCycles } };
        }

        // Options incompatible with JsonContext.s_defaultOptions below.
        public static IEnumerable<object[]> GetIncompatibleOptions()
        {
            yield return new object[] { new JsonSerializerOptions() };
            yield return new object[] { new JsonSerializerOptions { DefaultIgnoreCondition = JsonIgnoreCondition.Never } };
            yield return new object[] { new JsonSerializerOptions { IgnoreReadOnlyFields = true } };
        }
    }
}

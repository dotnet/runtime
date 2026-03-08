// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task DictionaryKeyFilter_IgnoreMetadataNames_SkipsSchemaProperties(bool useIgnoreMetadataNames)
        {
            JsonDictionaryKeyFilter filter = useIgnoreMetadataNames
                ? JsonDictionaryKeyFilter.IgnoreMetadataNames
                : new CustomMetadataNamesFilter();

            var options = new JsonSerializerOptions { DictionaryKeyFilter = filter };

            const string json = """
                {
                    "$schema": "http://example.org/myschema.json",
                    "Key1": 1,
                    "Key2": 2
                }
                """;

            Dictionary<string, int> result = await Serializer.DeserializeWrapper<Dictionary<string, int>>(json, options);

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result["Key1"]);
            Assert.Equal(2, result["Key2"]);
            Assert.False(result.ContainsKey("$schema"));
        }

        [Fact]
        public async Task DictionaryKeyFilter_IgnoreMetadataNames_SkipsNestedObjectValues()
        {
            var options = new JsonSerializerOptions { DictionaryKeyFilter = JsonDictionaryKeyFilter.IgnoreMetadataNames };

            const string json = """
                {
                    "$schema": { "type": "object" },
                    "Key1": 1
                }
                """;

            Dictionary<string, int> result = await Serializer.DeserializeWrapper<Dictionary<string, int>>(json, options);

            Assert.Single(result);
            Assert.Equal(1, result["Key1"]);
        }

        [Fact]
        public async Task DictionaryKeyFilter_NullFilter_NoKeysFiltered()
        {
            var options = new JsonSerializerOptions { DictionaryKeyFilter = null };

            const string json = """{"Key1": 1, "Key2": 2}""";

            Dictionary<string, int> result = await Serializer.DeserializeWrapper<Dictionary<string, int>>(json, options);

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result["Key1"]);
            Assert.Equal(2, result["Key2"]);
        }

        [Fact]
        public async Task DictionaryKeyFilter_CustomFilter_SkipsMatchingKeys()
        {
            var options = new JsonSerializerOptions { DictionaryKeyFilter = new SkipKey1Filter() };

            const string json = """{"Key1": 1, "Key2": 2, "Key3": 3}""";

            Dictionary<string, int> result = await Serializer.DeserializeWrapper<Dictionary<string, int>>(json, options);

            Assert.Equal(2, result.Count);
            Assert.False(result.ContainsKey("Key1"));
            Assert.Equal(2, result["Key2"]);
            Assert.Equal(3, result["Key3"]);
        }

        [Fact]
        public async Task DictionaryKeyFilter_IReadOnlyDictionary_FiltersKeys()
        {
            var options = new JsonSerializerOptions { DictionaryKeyFilter = JsonDictionaryKeyFilter.IgnoreMetadataNames };

            const string json = """{"$id": "1", "Key1": "value1"}""";

            IReadOnlyDictionary<string, string> result = await Serializer.DeserializeWrapper<IReadOnlyDictionary<string, string>>(json, options);

            Assert.Single(result);
            Assert.Equal("value1", result["Key1"]);
        }

        [Fact]
        public async Task DictionaryKeyFilter_MutatesOptionsCopyIndependently()
        {
            var options1 = new JsonSerializerOptions { DictionaryKeyFilter = JsonDictionaryKeyFilter.IgnoreMetadataNames };
            var options2 = new JsonSerializerOptions(options1);

            // Verify both have the filter.
            Assert.Same(options1.DictionaryKeyFilter, options2.DictionaryKeyFilter);

            // Verify changing one doesn't affect the other.
            options2.DictionaryKeyFilter = null;
            Assert.NotNull(options1.DictionaryKeyFilter);
            Assert.Null(options2.DictionaryKeyFilter);
        }

        [Fact]
        public async Task DictionaryKeyFilter_AllKeysFiltered_EmptyDictionary()
        {
            var options = new JsonSerializerOptions { DictionaryKeyFilter = new FilterAllKeysFilter() };

            const string json = """{"Key1": 1, "Key2": 2}""";

            Dictionary<string, int> result = await Serializer.DeserializeWrapper<Dictionary<string, int>>(json, options);

            Assert.Empty(result);
        }

        [Fact]
        public async Task DictionaryKeyFilter_IgnoreMetadataNames_MultipleMetadataProperties()
        {
            var options = new JsonSerializerOptions { DictionaryKeyFilter = JsonDictionaryKeyFilter.IgnoreMetadataNames };

            const string json = """
                {
                    "$schema": "http://example.org/schema.json",
                    "$id": "uniqueId",
                    "$comment": "A test",
                    "Key1": 1,
                    "Key2": 2
                }
                """;

            Dictionary<string, int> result = await Serializer.DeserializeWrapper<Dictionary<string, int>>(json, options);

            Assert.Equal(2, result.Count);
            Assert.Equal(1, result["Key1"]);
            Assert.Equal(2, result["Key2"]);
        }

        [Fact]
        public void DictionaryKeyFilter_IgnoreMetadataNames_IgnoresDollarPrefixKeys()
        {
            Assert.True(JsonDictionaryKeyFilter.IgnoreMetadataNames.IgnoreKey("$schema"u8));
            Assert.True(JsonDictionaryKeyFilter.IgnoreMetadataNames.IgnoreKey("$id"u8));
            Assert.True(JsonDictionaryKeyFilter.IgnoreMetadataNames.IgnoreKey("$ref"u8));
            Assert.True(JsonDictionaryKeyFilter.IgnoreMetadataNames.IgnoreKey("$type"u8));
            Assert.True(JsonDictionaryKeyFilter.IgnoreMetadataNames.IgnoreKey("$"u8));
            Assert.False(JsonDictionaryKeyFilter.IgnoreMetadataNames.IgnoreKey("schema"u8));
            Assert.False(JsonDictionaryKeyFilter.IgnoreMetadataNames.IgnoreKey(""u8));
            Assert.False(JsonDictionaryKeyFilter.IgnoreMetadataNames.IgnoreKey("Key1"u8));
        }

        private sealed class CustomMetadataNamesFilter : JsonDictionaryKeyFilter
        {
            public override bool IgnoreKey(ReadOnlySpan<byte> utf8JsonPropertyName) =>
                utf8JsonPropertyName.Length > 0 && utf8JsonPropertyName[0] == (byte)'$';
        }

        private sealed class SkipKey1Filter : JsonDictionaryKeyFilter
        {
            public override bool IgnoreKey(ReadOnlySpan<byte> utf8JsonPropertyName) =>
                utf8JsonPropertyName.SequenceEqual("Key1"u8);
        }

        private sealed class FilterAllKeysFilter : JsonDictionaryKeyFilter
        {
            public override bool IgnoreKey(ReadOnlySpan<byte> utf8JsonPropertyName) => true;
        }
    }
}

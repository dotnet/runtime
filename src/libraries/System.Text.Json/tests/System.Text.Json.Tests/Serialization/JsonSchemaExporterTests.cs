// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;
using Xunit;

namespace System.Text.Json.Schema.Tests
{
    public sealed class JsonSchemaExporterTests_Reflection() : JsonSchemaExporterTests(JsonSerializerWrapper.StringSerializer)
    {
        [Theory]
        [InlineData(false, """{"type":["object","null"],"properties":{"Name":{"type":["string","null"]},"ReadOnlyProperty":{"type":"integer"},"ReadOnlyCollectionProperty":{"type":"array","items":{"type":"integer"}}}}""")]
        [InlineData(true, """{"type":["object","null"],"properties":{"Name":{"type":["string","null"]},"ReadOnlyCollectionProperty":{"type":"array","items":{"type":"integer"}}}}""")]
        public void IgnoreReadOnlyProperties_ReturnsExpectedSchema(bool ignoreReadOnlyProperties, string expectedJsonSchema)
        {
            var options = new JsonSerializerOptions(Serializer.DefaultOptions)
            {
                IgnoreReadOnlyProperties = ignoreReadOnlyProperties,
            };

            JsonNode schema = options.GetJsonSchemaAsNode(typeof(PocoWithReadOnlyProperties));
            AssertValidJsonSchema(typeof(PocoWithReadOnlyProperties), expectedJsonSchema, schema);
        }

        [Theory]
        [InlineData(false, """{"type":["object","null"],"properties":{"Value":{"type":"integer"},"ReadOnlyValue":{"type":"integer"}}}""")]
        [InlineData(true, """{"type":["object","null"],"properties":{"Value":{"type":"integer"}}}""")]
        public void IgnoreReadOnlyFields_ReturnsExpectedSchema(bool ignoreReadOnlyFields, string expectedJsonSchema)
        {
            var options = new JsonSerializerOptions(Serializer.DefaultOptions)
            {
                IncludeFields = true,
                IgnoreReadOnlyFields = ignoreReadOnlyFields,
            };

            JsonNode schema = options.GetJsonSchemaAsNode(typeof(PocoWithReadOnlyField));
            AssertValidJsonSchema(typeof(PocoWithReadOnlyField), expectedJsonSchema, schema);
        }

        [Fact]
        public void ContractCustomizerRemovingGetterAndSetter_PropertyExcludedFromSchema()
        {
            var options = new JsonSerializerOptions(Serializer.DefaultOptions)
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers =
                    {
                        static typeInfo =>
                        {
                            foreach (JsonPropertyInfo property in typeInfo.Properties)
                            {
                                if (property.Name == nameof(PocoWithCustomizedProperties.Name))
                                {
                                    property.Get = null;
                                    property.Set = null;
                                }
                            }
                        }
                    }
                }
            };

            JsonNode schema = options.GetJsonSchemaAsNode(typeof(PocoWithCustomizedProperties));
            AssertValidJsonSchema(typeof(PocoWithCustomizedProperties), """{"type":["object","null"],"properties":{"Age":{"type":"integer"}}}""", schema);

            // The schema should be consistent with the serializer, which also omits the property.
            string json = JsonSerializer.Serialize(new PocoWithCustomizedProperties { Name = "name", Age = 42 }, options);
            Assert.Equal("""{"Age":42}""", json);
        }

        [Theory]
        [InlineData(false, """{"type":["object","null"],"properties":{"Name":{"type":["string","null"]},"Age":{"type":"integer"}}}""")]
        [InlineData(true, """{"type":["object","null"],"properties":{"Age":{"type":"integer"}}}""")]
        public void ContractCustomizerRemovingSetter_RespectsIgnoreReadOnlyProperties(bool ignoreReadOnlyProperties, string expectedJsonSchema)
        {
            var options = new JsonSerializerOptions(Serializer.DefaultOptions)
            {
                IgnoreReadOnlyProperties = ignoreReadOnlyProperties,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers =
                    {
                        static typeInfo =>
                        {
                            foreach (JsonPropertyInfo property in typeInfo.Properties)
                            {
                                if (property.Name == nameof(PocoWithCustomizedProperties.Name))
                                {
                                    property.Set = null;
                                }
                            }
                        }
                    }
                }
            };

            JsonNode schema = options.GetJsonSchemaAsNode(typeof(PocoWithCustomizedProperties));
            AssertValidJsonSchema(typeof(PocoWithCustomizedProperties), expectedJsonSchema, schema);
        }

        public class PocoWithCustomizedProperties
        {
            public string? Name { get; set; }
            public int Age { get; set; }
        }

        public class PocoWithReadOnlyProperties
        {
            public string? Name { get; set; }
            public int ReadOnlyProperty => 42;
            public List<int> ReadOnlyCollectionProperty { get; } = new();
        }

        public class PocoWithReadOnlyField
        {
            public int Value;
            public readonly int ReadOnlyValue = 42;
        }
    }
}

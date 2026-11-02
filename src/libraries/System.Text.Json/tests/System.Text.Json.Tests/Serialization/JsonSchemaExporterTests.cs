// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Nodes;
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

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;
using Json.Schema;
using Xunit;
using Xunit.Sdk;

namespace System.Text.Json.Schema.Tests
{
    public abstract partial class JsonSchemaExporterTests : SerializerTests
    {
        private readonly JsonSerializerOptions _indentedOptions;

        protected JsonSchemaExporterTests(JsonSerializerWrapper serializer) : base(serializer)
        {
            _indentedOptions = new(serializer.DefaultOptions) { WriteIndented = true };
        }

        [Theory]
        [MemberData(nameof(GetTestData))]
        public void TestTypes_GeneratesExpectedJsonSchema(ITestData testData)
        {
            JsonNode schema = Serializer.DefaultOptions.GetJsonSchemaAsNode(testData.Type, testData.Options);
            AssertValidJsonSchema(testData.Type, testData.ExpectedJsonSchema, schema);
        }

        [Theory]
        [MemberData(nameof(GetTestDataUsingAllValues))]
        public void TestTypes_SerializedValueMatchesGeneratedSchema(ITestData testData)
        {
            JsonNode schema = Serializer.DefaultOptions.GetJsonSchemaAsNode(testData.Type, testData.Options);
            JsonNode? instance = JsonSerializer.SerializeToNode(testData.Value, testData.Type, Serializer.DefaultOptions);
            AssertDocumentMatchesSchema(schema, instance);
        }

        [Theory]
        [InlineData(typeof(string), "string")]
        [InlineData(typeof(int[]), "array")]
        [InlineData(typeof(Dictionary<string, int>), "object")]
        [InlineData(typeof(SimplePoco), "object")]
        public void TreatNullObliviousAsNonNullable_False_MarksReferenceTypesAsNullable(Type referenceType, string expectedType)
        {
            Assert.True(!referenceType.IsValueType);
            var config = new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = false };
            JsonNode schema = Serializer.DefaultOptions.GetJsonSchemaAsNode(referenceType, config);
            JsonArray arr = Assert.IsType<JsonArray>(schema["type"]);
            Assert.Equal([expectedType, "null"], arr.Select(e => (string)e!));
        }

        [Theory]
        [InlineData(typeof(string), "string")]
        [InlineData(typeof(int[]), "array")]
        [InlineData(typeof(Dictionary<string, int>), "object")]
        [InlineData(typeof(SimplePoco), "object")]
        public void TreatNullObliviousAsNonNullable_True_MarksReferenceTypesAsNonNullable(Type referenceType, string expectedType)
        {
            Assert.True(!referenceType.IsValueType);
            var config = new JsonSchemaExporterOptions { TreatNullObliviousAsNonNullable = true };
            JsonNode schema = Serializer.DefaultOptions.GetJsonSchemaAsNode(referenceType, config);
            Assert.Equal(expectedType, (string)schema["type"]!);
        }

        [Theory]
        [InlineData(typeof(Type))]
        [InlineData(typeof(MethodInfo))]
        [InlineData(typeof(UIntPtr))]
        [InlineData(typeof(MemberInfo))]
        [InlineData(typeof(SerializationInfo))]
        [InlineData(typeof(Func<int, int>))]
        public void UnsupportedType_ReturnsExpectedSchema(Type type)
        {
            JsonNode schema = Serializer.DefaultOptions.GetJsonSchemaAsNode(type);
            Assert.Equal(""""{"$comment":"Unsupported .NET type","not":true}"""", schema.ToJsonString());
        }

        [Fact]
        public void TypeWithDisallowUnmappedMembers_AdditionalPropertiesFailValidation()
        {
            JsonNode schema = Serializer.DefaultOptions.GetJsonSchemaAsNode(typeof(PocoDisallowingUnmappedMembers));
            JsonNode? jsonWithUnmappedProperties = JsonNode.Parse("""{ "UnmappedProperty" : {} }""");
            AssertDoesNotMatchSchema(schema, jsonWithUnmappedProperties);
        }

        [Fact]
        public void GetJsonSchemaAsNode_NullInputs_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => ((JsonSerializerOptions)null!).GetJsonSchemaAsNode(typeof(int)));
            Assert.Throws<ArgumentNullException>(() => Serializer.DefaultOptions.GetJsonSchemaAsNode((Type)null!));
            Assert.Throws<ArgumentNullException>(() => ((JsonTypeInfo)null!).GetJsonSchemaAsNode());
        }

        [Fact]
        public void GetJsonSchemaAsNode_NoResolver_ThrowInvalidOperationException()
        {
            var options = new JsonSerializerOptions();
            Assert.Throws<InvalidOperationException>(() => options.GetJsonSchemaAsNode(typeof(int)));
        }

        [Fact]
        public void JsonSerializerOptions_SmallMaxDepth_ThrowsInvalidOperationException()
        {
            var options = new JsonSerializerOptions(Serializer.DefaultOptions) { MaxDepth = 1 };
            var ex = Assert.Throws<InvalidOperationException>(() => options.GetJsonSchemaAsNode(typeof(PocoWithRecursiveMembers)));
            Assert.Contains("depth", ex.Message);
        }

        [Fact]
        public void ReferenceHandlePreserve_Enabled_ThrowsNotSupportedException()
        {
            var options = new JsonSerializerOptions(Serializer.DefaultOptions) { ReferenceHandler = ReferenceHandler.Preserve };
            options.MakeReadOnly();

            var ex = Assert.Throws<NotSupportedException>(() => options.GetJsonSchemaAsNode(typeof(SimplePoco)));
            Assert.Contains("ReferenceHandler.Preserve", ex.Message);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void JsonSchemaExporterOptions_DefaultSettings(bool useSingleton)
        {
            JsonSchemaExporterOptions options = useSingleton ? JsonSchemaExporterOptions.Default : new();

            Assert.False(options.TreatNullObliviousAsNonNullable);
            Assert.Null(options.TransformSchemaNode);
        }

        [Fact]
        public void JsonSchemaExporterOptions_Default_IsSame()
        {
            Assert.Same(JsonSchemaExporterOptions.Default, JsonSchemaExporterOptions.Default);
        }

        protected void AssertValidJsonSchema(Type type, string expectedJsonSchema, JsonNode actualJsonSchema)
        {
            JsonNode? expectedJsonSchemaNode = JsonNode.Parse(expectedJsonSchema, documentOptions: new() { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });

            if (!JsonNode.DeepEquals(expectedJsonSchemaNode, actualJsonSchema))
            {
                throw new XunitException($"""
                Generated schema does not match the expected specification.
                Expected:
                {FormatJson(expectedJsonSchemaNode)}
                Actual:
                {FormatJson(actualJsonSchema)}
                """);
            }
        }

        protected void AssertDocumentMatchesSchema(JsonNode schema, JsonNode? instance)
        {
            EvaluationResults results = EvaluateSchemaCore(schema, instance);
            if (!results.IsValid)
            {
                IEnumerable<string> errors = results.Details
                    .Where(d => d.HasErrors)
                    .SelectMany(d => d.Errors!.Select(error => $"Path:${d.InstanceLocation} {error.Key}:{error.Value}"));

                throw new XunitException($"""
                Instance JSON document does not match the specified schema.
                Schema:
                {FormatJson(schema)}
                Instance:
                {FormatJson(instance)}
                Errors:
                {string.Join(Environment.NewLine, errors)}
                """);
            }
        }

        protected void AssertDoesNotMatchSchema(JsonNode schema, JsonNode? instance)
        {
            EvaluationResults results = EvaluateSchemaCore(schema, instance);
            if (results.IsValid)
            {
                throw new XunitException($"""
                Instance JSON document matches the specified schema.
                Schema:
                {FormatJson(schema)}
                Instance:
                {FormatJson(instance)}
                """);
            }
        }

        private EvaluationResults EvaluateSchemaCore(JsonNode schema, JsonNode? instance)
        {
            JsonSchema jsonSchema = JsonSchema.FromText(schema.ToJsonString());
            return jsonSchema.Evaluate(instance, s_evaluationOptions);
        }

        private static readonly EvaluationOptions s_evaluationOptions = new()
        {
            OutputFormat = OutputFormat.List,
            RequireFormatValidation = true,
        };

        private string FormatJson(JsonNode? node) =>
            JsonSerializer.Serialize(node, _indentedOptions);
    }
}

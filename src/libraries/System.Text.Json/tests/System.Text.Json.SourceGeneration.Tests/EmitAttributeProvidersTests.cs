// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;
using System.Text.Json.Schema;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public static partial class EmitAttributeProvidersTests
    {
        public class Poco
        {
            public int Id { get; set; }

            public string? Name { get; set; }

            [Obsolete("Legacy")]
            public string? Legacy { get; set; }

            public double Score;
        }

        public class PocoWithConstructor
        {
            public PocoWithConstructor(int x, string y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }

            public string Y { get; }
        }

        [JsonSerializable(typeof(Poco))]
        [JsonSerializable(typeof(PocoWithConstructor))]
        [JsonSourceGenerationOptions(IncludeFields = true)]
        public partial class DefaultContext : JsonSerializerContext;

        [JsonSerializable(typeof(Poco))]
        [JsonSerializable(typeof(PocoWithConstructor))]
        [JsonSourceGenerationOptions(IncludeFields = true, EmitAttributeProviders = false)]
        public partial class OmitContext : JsonSerializerContext;

        // Same options as the context above, but with the option explicitly set to its default value.
        [JsonSerializable(typeof(Poco))]
        [JsonSourceGenerationOptions(IncludeFields = true, EmitAttributeProviders = true)]
        public partial class ExplicitlyIncludedContext : JsonSerializerContext;

        [Fact]
        public static void DefaultContext_PopulatesAttributeProviders()
        {
            AssertAllPropertiesHaveProviders<Poco>(DefaultContext.Default.Poco, expectedCount: 4);
            AssertAllPropertiesHaveProviders<Poco>(ExplicitlyIncludedContext.Default.Poco, expectedCount: 4);

            JsonTypeInfo<PocoWithConstructor> ctorInfo = DefaultContext.Default.PocoWithConstructor;
            Assert.IsAssignableFrom<ConstructorInfo>(ctorInfo.ConstructorAttributeProvider);

            Assert.Equal(2, ctorInfo.Properties.Count);
            foreach (JsonPropertyInfo property in ctorInfo.Properties)
            {
                Assert.NotNull(property.AttributeProvider);
                Assert.IsAssignableFrom<ParameterInfo>(property.AssociatedParameter?.AttributeProvider);
            }
        }

        [Fact]
        public static void OmitContext_AttributeProvidersAreNull()
        {
            JsonTypeInfo<Poco> pocoInfo = OmitContext.Default.Poco;
            Assert.Equal(4, pocoInfo.Properties.Count);
            Assert.All(pocoInfo.Properties, property => Assert.Null(property.AttributeProvider));

            JsonTypeInfo<PocoWithConstructor> ctorInfo = OmitContext.Default.PocoWithConstructor;
            Assert.Null(ctorInfo.ConstructorAttributeProvider);

            Assert.Equal(2, ctorInfo.Properties.Count);
            foreach (JsonPropertyInfo property in ctorInfo.Properties)
            {
                Assert.Null(property.AttributeProvider);
                Assert.NotNull(property.AssociatedParameter);
                Assert.Null(property.AssociatedParameter.AttributeProvider);
            }
        }

        [Fact]
        public static void OmitContext_RoundtripsLikeDefaultContext()
        {
            var value = new Poco { Id = 42, Name = "name", Score = 1.5 };

            string expected = JsonSerializer.Serialize(value, DefaultContext.Default.Poco);
            string actual = JsonSerializer.Serialize(value, OmitContext.Default.Poco);
            Assert.Equal(expected, actual);

            Poco? result = JsonSerializer.Deserialize(actual, OmitContext.Default.Poco);
            Assert.NotNull(result);
            Assert.Equal(42, result.Id);
            Assert.Equal("name", result.Name);
            Assert.Equal(1.5, result.Score);

            const string ctorJson = """{"X":7,"Y":"seven"}""";
            PocoWithConstructor? ctorResult = JsonSerializer.Deserialize(ctorJson, OmitContext.Default.PocoWithConstructor);
            Assert.NotNull(ctorResult);
            Assert.Equal(7, ctorResult.X);
            Assert.Equal("seven", ctorResult.Y);
            Assert.Equal(ctorJson, JsonSerializer.Serialize(ctorResult, OmitContext.Default.PocoWithConstructor));
        }

        [Fact]
        public static void OmitContext_ContractModifierCanAssignPropertyAttributeProvider()
        {
            JsonSerializerOptions options = new()
            {
                TypeInfoResolver = OmitContext.Default.WithAddedModifier(static typeInfo =>
                {
                    foreach (JsonPropertyInfo property in typeInfo.Properties)
                    {
                        property.AttributeProvider = typeof(PocoWithConstructor).GetProperty(property.Name);
                    }
                }),
            };

            JsonTypeInfo<PocoWithConstructor> typeInfo = (JsonTypeInfo<PocoWithConstructor>)options.GetTypeInfo(typeof(PocoWithConstructor));
            Assert.All(typeInfo.Properties, property => Assert.NotNull(property.AttributeProvider));

            // Constructor and parameter attribute providers cannot be restored.
            Assert.Null(typeInfo.ConstructorAttributeProvider);
            Assert.All(typeInfo.Properties, property => Assert.Null(property.AssociatedParameter?.AttributeProvider));
        }

        [Fact]
        public static void JsonSchemaExporter_ObsoleteMemberIsDeprecatedOnlyWhenProvidersArePresent()
        {
            JsonNode defaultSchema = JsonSchemaExporter.GetJsonSchemaAsNode(DefaultContext.Default.Poco);
            JsonNode omitSchema = JsonSchemaExporter.GetJsonSchemaAsNode(OmitContext.Default.Poco);

            Assert.True((bool?)defaultSchema["properties"]!["Legacy"]!["deprecated"]);
            Assert.Null(omitSchema["properties"]!["Legacy"]!["deprecated"]);

            // Every other aspect of the schema is unchanged.
            ((JsonObject)defaultSchema["properties"]!["Legacy"]!).Remove("deprecated");
            Assert.True(JsonNode.DeepEquals(defaultSchema, omitSchema));
        }

        private static void AssertAllPropertiesHaveProviders<T>(JsonTypeInfo<T> typeInfo, int expectedCount)
        {
            Assert.Equal(expectedCount, typeInfo.Properties.Count);

            foreach (JsonPropertyInfo property in typeInfo.Properties)
            {
                MemberInfo member = Assert.IsAssignableFrom<MemberInfo>(property.AttributeProvider);
                Assert.Equal(typeof(T), member.DeclaringType);
                Assert.Equal(property.Name, member.Name);
            }

            Assert.Contains(typeInfo.Properties, property => property.AttributeProvider is FieldInfo);
            Assert.Contains(typeInfo.Properties, property => property.AttributeProvider is PropertyInfo);
        }
    }
}

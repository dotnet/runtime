// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public static partial class PolymorphismTests
    {
        [Fact]
        public static void PolymorphismOptions_AreGenerated()
        {
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenPolymorphicBase;
            JsonPolymorphismOptions options = Assert.IsType<JsonPolymorphismOptions>(typeInfo.PolymorphismOptions);

            Assert.True(options.IgnoreUnrecognizedTypeDiscriminators);
            Assert.Equal(JsonUnknownDerivedTypeHandling.FallBackToBaseType, options.UnknownDerivedTypeHandling);
            Assert.Equal("$kind", options.TypeDiscriminatorPropertyName);
            Assert.Collection(
                options.DerivedTypes,
                derivedType =>
                {
                    Assert.Equal(typeof(SourceGenStringDiscriminatorDerived), derivedType.DerivedType);
                    Assert.Equal("string-derived", derivedType.TypeDiscriminator);
                },
                derivedType =>
                {
                    Assert.Equal(typeof(SourceGenIntDiscriminatorDerived), derivedType.DerivedType);
                    Assert.Equal(42, derivedType.TypeDiscriminator);
                });
        }

        [Fact]
        public static void PolymorphicTypeClassifier_IsGeneratedAndVisibleToModifier()
        {
            bool modifierObservedClassifier = false;
            IJsonTypeInfoResolver resolver = PolymorphismTestsContext.Default.WithAddedModifier(typeInfo =>
            {
                if (typeInfo.Type == typeof(SourceGenClassifiedAnimal))
                {
                    Assert.NotNull(typeInfo.PolymorphismOptions);
                    Assert.NotNull(typeInfo.TypeClassifier);
                    modifierObservedClassifier = true;
                }
            });

            var options = new JsonSerializerOptions
            {
                TypeInfoResolver = resolver,
            };

            SourceGenClassifiedAnimal? result = JsonSerializer.Deserialize<SourceGenClassifiedAnimal>(
                """{"Name":"Rex","Breed":"Labrador"}""",
                options);

            SourceGenClassifiedDog dog = Assert.IsType<SourceGenClassifiedDog>(result);
            Assert.Equal("Rex", dog.Name);
            Assert.Equal("Labrador", dog.Breed);
            Assert.True(modifierObservedClassifier);
        }

        [Fact]
        public static void CollectionPolymorphismOptions_AreGenerated()
        {
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenPolymorphicIntList;
            JsonPolymorphismOptions options = Assert.IsType<JsonPolymorphismOptions>(typeInfo.PolymorphismOptions);

            Assert.Equal("$kind", options.TypeDiscriminatorPropertyName);
            JsonDerivedType derivedType = Assert.Single(options.DerivedTypes);
            Assert.Equal(typeof(SourceGenPolymorphicIntListDerived), derivedType.DerivedType);
            Assert.Equal("derived-list", derivedType.TypeDiscriminator);
        }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
        [JsonSerializable(typeof(SourceGenPolymorphicBase))]
        [JsonSerializable(typeof(SourceGenClassifiedAnimal))]
        [JsonSerializable(typeof(SourceGenPolymorphicIntList))]
        internal sealed partial class PolymorphismTestsContext : JsonSerializerContext
        {
        }
    }

    [JsonPolymorphic(
        TypeDiscriminatorPropertyName = "$kind",
        IgnoreUnrecognizedTypeDiscriminators = true,
        UnknownDerivedTypeHandling = JsonUnknownDerivedTypeHandling.FallBackToBaseType)]
    [JsonDerivedType(typeof(SourceGenStringDiscriminatorDerived), "string-derived")]
    [JsonDerivedType(typeof(SourceGenIntDiscriminatorDerived), 42)]
    public class SourceGenPolymorphicBase
    {
        public string? Value { get; set; }
    }

    public sealed class SourceGenStringDiscriminatorDerived : SourceGenPolymorphicBase
    {
        public string? StringValue { get; set; }
    }

    public sealed class SourceGenIntDiscriminatorDerived : SourceGenPolymorphicBase
    {
        public int IntValue { get; set; }
    }

    [JsonPolymorphic(TypeClassifier = typeof(SourceGenAnimalClassifierFactory))]
    [JsonDerivedType(typeof(SourceGenClassifiedDog), "dog")]
    [JsonDerivedType(typeof(SourceGenClassifiedCat), "cat")]
    public class SourceGenClassifiedAnimal
    {
        public string? Name { get; set; }
    }

    public sealed class SourceGenClassifiedDog : SourceGenClassifiedAnimal
    {
        public string? Breed { get; set; }
    }

    public sealed class SourceGenClassifiedCat : SourceGenClassifiedAnimal
    {
        public int Lives { get; set; }
    }

    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$kind")]
    [JsonDerivedType(typeof(SourceGenPolymorphicIntListDerived), "derived-list")]
    public class SourceGenPolymorphicIntList : List<int>
    {
    }

    public sealed class SourceGenPolymorphicIntListDerived : SourceGenPolymorphicIntList
    {
    }

    public sealed class SourceGenAnimalClassifierFactory : JsonTypeClassifierFactory<SourceGenClassifiedAnimal>
    {
        public override JsonTypeClassifier CreateJsonClassifier(JsonTypeClassifierContext context, JsonSerializerOptions options)
        {
            Assert.Equal(JsonTypeClassifierKind.PolymorphicType, context.Kind);
            Assert.Equal(typeof(SourceGenClassifiedAnimal), context.DeclaringType);
            Assert.Equal("$type", context.TypeDiscriminatorPropertyName);
            Assert.Collection(
                context.DerivedTypes,
                derivedType =>
                {
                    Assert.Equal(typeof(SourceGenClassifiedDog), derivedType.DerivedType);
                    Assert.Equal("dog", derivedType.TypeDiscriminator);
                },
                derivedType =>
                {
                    Assert.Equal(typeof(SourceGenClassifiedCat), derivedType.DerivedType);
                    Assert.Equal("cat", derivedType.TypeDiscriminator);
                });

            return static (ref Utf8JsonReader reader) =>
            {
                using JsonDocument document = JsonDocument.ParseValue(ref reader);
                JsonElement root = document.RootElement;

                if (root.TryGetProperty("Breed", out _))
                {
                    return typeof(SourceGenClassifiedDog);
                }

                if (root.TryGetProperty("Lives", out _))
                {
                    return typeof(SourceGenClassifiedCat);
                }

                return null;
            };
        }
    }
}

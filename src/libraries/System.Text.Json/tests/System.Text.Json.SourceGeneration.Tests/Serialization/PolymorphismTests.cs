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

        [Fact]
        public static void OpenGenericDerivedType_PartiallyConcrete_RoundTrips()
        {
            // SourceGenOpenGenericDerived<T> : SourceGenOpenGenericBase<T, int> registered on
            // SourceGenOpenGenericBase<string, int>. Position 0 (T) unifies to string;
            // position 1 (concrete int) matches. The generated metadata for the closed base
            // must contain SourceGenOpenGenericDerived<string> as the resolved derived type.
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenOpenGenericBaseStringInt32;
            JsonPolymorphismOptions options = Assert.IsType<JsonPolymorphismOptions>(typeInfo.PolymorphismOptions);
            JsonDerivedType derivedType = Assert.Single(options.DerivedTypes);
            Assert.Equal(typeof(SourceGenOpenGenericDerived<string>), derivedType.DerivedType);
            Assert.Equal("derived", derivedType.TypeDiscriminator);

            SourceGenOpenGenericBase<string, int> value = new SourceGenOpenGenericDerived<string> { Extra = "hello" };
            string json = JsonSerializer.Serialize(value, typeInfo);
            Assert.Contains("\"$type\":\"derived\"", json);

            var result = JsonSerializer.Deserialize(json, typeInfo);
            var d = Assert.IsType<SourceGenOpenGenericDerived<string>>(result);
            Assert.Equal("hello", d.Extra);
        }

        [Fact]
        public static void ClosedDerivedTypes_MismatchedBaseSpecialization_AreFilteredOnIntSpec()
        {
            // For SourceGenSpecAnimal<int>, only SourceGenSpecAnimal_Cat applies; the closed
            // Dog : SourceGenSpecAnimal<string> registration is silently filtered.
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenSpecAnimalInt;
            JsonPolymorphismOptions options = Assert.IsType<JsonPolymorphismOptions>(typeInfo.PolymorphismOptions);
            JsonDerivedType derivedType = Assert.Single(options.DerivedTypes);
            Assert.Equal(typeof(SourceGenSpecAnimal_Cat), derivedType.DerivedType);

            SourceGenSpecAnimal<int> cat = new SourceGenSpecAnimal_Cat { Name = "Felix", Lives = 9 };
            string json = JsonSerializer.Serialize(cat, typeInfo);
            Assert.Contains("\"$type\":\"cat\"", json);
        }

        [Fact]
        public static void ClosedDerivedTypes_MismatchedBaseSpecialization_AreFilteredOnStringSpec()
        {
            // For SourceGenSpecAnimal<string>, only SourceGenSpecAnimal_Dog applies.
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenSpecAnimalString;
            JsonPolymorphismOptions options = Assert.IsType<JsonPolymorphismOptions>(typeInfo.PolymorphismOptions);
            JsonDerivedType derivedType = Assert.Single(options.DerivedTypes);
            Assert.Equal(typeof(SourceGenSpecAnimal_Dog), derivedType.DerivedType);

            SourceGenSpecAnimal<string> dog = new SourceGenSpecAnimal_Dog { Name = "Rex", Breed = "Husky" };
            string json = JsonSerializer.Serialize(dog, typeInfo);
            Assert.Contains("\"$type\":\"dog\"", json);
        }

        [Fact]
        public static void ClosedDerivedTypes_AllFilteredForSpec_BecomesNonPolymorphic()
        {
            // For SourceGenSpecAnimal<bool>, NEITHER Cat nor Dog applies; filtering empties
            // the derived-type list, so the source-gen-emitted type info must omit
            // polymorphism options entirely (otherwise we would throw at runtime).
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenSpecAnimalBool;
            Assert.Null(typeInfo.PolymorphismOptions);

            string json = JsonSerializer.Serialize(new SourceGenSpecAnimal<bool> { Name = "Cookie" }, typeInfo);
            Assert.DoesNotContain("$type", json);
            Assert.Contains("\"Name\":\"Cookie\"", json);
        }

        [Fact]
        public static void OpenDerivedWithConstraint_FailingConstraintIsDroppedFromEmittedMetadata()
        {
            // SourceGenConstrainedDerived<T> where T : struct, registered on
            // SourceGenConstrainedBase<string>. string is not a struct, so the source
            // generator emits SYSLIB1229 (suppressed below) and drops the registration
            // from generated metadata. The emitted typeInfo has no derived types and
            // serializes the closed base as non-polymorphic.
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenConstrainedBaseString;
            Assert.Null(typeInfo.PolymorphismOptions);

            string json = JsonSerializer.Serialize(new SourceGenConstrainedBase<string> { Value = "hi" }, typeInfo);
            Assert.DoesNotContain("$type", json);
        }

        [Fact]
        public static void OpenDerivedWithConstraint_AppliesWhenConstraintIsSatisfied()
        {
            // SourceGenConstrainedDerived<T> where T : struct, registered on
            // SourceGenConstrainedBase<int>. int satisfies struct -- the open derived
            // resolves to SourceGenConstrainedDerived<int>.
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenConstrainedBaseInt;
            JsonPolymorphismOptions options = Assert.IsType<JsonPolymorphismOptions>(typeInfo.PolymorphismOptions);
            JsonDerivedType derivedType = Assert.Single(options.DerivedTypes);
            Assert.Equal(typeof(SourceGenConstrainedDerived<int>), derivedType.DerivedType);

            SourceGenConstrainedBase<int> derived = new SourceGenConstrainedDerived<int> { Value = 1, Extra = 2 };
            string json = JsonSerializer.Serialize(derived, typeInfo);
            Assert.Contains("\"$type\":\"derived\"", json);
        }

        [Fact]
        public static void OpenDerivedWithExtraUnboundParameter_BadArmIsDroppedFromEmittedMetadata()
        {
            // Two registrations on the same base: SourceGenExtraParam_Cat<T> resolves OK to
            // SourceGenExtraParam_Cat<int>; SourceGenExtraParam_Cat<T, T2> has an unbound
            // T2 that the base does not pin down, so the source generator emits SYSLIB1229
            // (suppressed below) and drops it from the generated metadata. Only the well-
            // formed "cat" arm survives in the emitted typeInfo.
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenExtraParamAnimalInt;
            JsonPolymorphismOptions options = Assert.IsType<JsonPolymorphismOptions>(typeInfo.PolymorphismOptions);
            JsonDerivedType derivedType = Assert.Single(options.DerivedTypes);
            Assert.Equal(typeof(SourceGenExtraParam_Cat<int>), derivedType.DerivedType);
            Assert.Equal("cat", derivedType.TypeDiscriminator);

            SourceGenExtraParamAnimal<int> value = new SourceGenExtraParam_Cat<int> { Name = "Felix", Tag = 7 };
            string json = JsonSerializer.Serialize(value, typeInfo);
            Assert.Contains("\"$type\":\"cat\"", json);
        }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
        [JsonSerializable(typeof(SourceGenPolymorphicBase))]
        [JsonSerializable(typeof(SourceGenClassifiedAnimal))]
        [JsonSerializable(typeof(SourceGenPolymorphicIntList))]
        [JsonSerializable(typeof(SourceGenOpenGenericBase<string, int>))]
        [JsonSerializable(typeof(SourceGenSpecAnimal<int>), TypeInfoPropertyName = "SourceGenSpecAnimalInt")]
        [JsonSerializable(typeof(SourceGenSpecAnimal<string>), TypeInfoPropertyName = "SourceGenSpecAnimalString")]
        [JsonSerializable(typeof(SourceGenSpecAnimal<bool>), TypeInfoPropertyName = "SourceGenSpecAnimalBool")]
        [JsonSerializable(typeof(SourceGenConstrainedBase<string>), TypeInfoPropertyName = "SourceGenConstrainedBaseString")]
        [JsonSerializable(typeof(SourceGenConstrainedBase<int>), TypeInfoPropertyName = "SourceGenConstrainedBaseInt")]
        [JsonSerializable(typeof(SourceGenExtraParamAnimal<int>), TypeInfoPropertyName = "SourceGenExtraParamAnimalInt")]
        internal sealed partial class PolymorphismTestsContext : JsonSerializerContext
        {
        }
    }

    [JsonDerivedType(typeof(SourceGenOpenGenericDerived<>), "derived")]
    public class SourceGenOpenGenericBase<T1, T2>
    {
        public T1? Value1 { get; set; }
        public T2? Value2 { get; set; }
    }

    public sealed class SourceGenOpenGenericDerived<T> : SourceGenOpenGenericBase<T, int>
    {
        public T? Extra { get; set; }
    }

    // Specialization-filtering fixtures (PR #127318 follow-up).

    [JsonDerivedType(typeof(SourceGenSpecAnimal_Cat), "cat")]
    [JsonDerivedType(typeof(SourceGenSpecAnimal_Dog), "dog")]
    public class SourceGenSpecAnimal<T>
    {
        public string? Name { get; set; }
    }

    public sealed class SourceGenSpecAnimal_Cat : SourceGenSpecAnimal<int> { public int Lives { get; set; } }
    public sealed class SourceGenSpecAnimal_Dog : SourceGenSpecAnimal<string> { public string? Breed { get; set; } }

    // Constraint-on-derived: the source generator emits SYSLIB1229 for the
    // SourceGenConstrainedBase<string> specialization because string does not satisfy
    // the `where T : struct` constraint. The warning is benign here -- the bad
    // registration is dropped from generated metadata and the closed base serializes
    // as a plain (non-polymorphic) type.
#pragma warning disable SYSLIB1229
    [JsonDerivedType(typeof(SourceGenConstrainedDerived<>), "derived")]
#pragma warning restore SYSLIB1229
    public class SourceGenConstrainedBase<T>
    {
        public T? Value { get; set; }
    }

    public sealed class SourceGenConstrainedDerived<T> : SourceGenConstrainedBase<T> where T : struct
    {
        public int Extra { get; set; }
    }

    [JsonDerivedType(typeof(SourceGenExtraParam_Cat<>), "cat")]
    // SourceGenExtraParam_Cat<T, T2> has an extra unbound T2 that the single-parameter
    // base cannot pin down. The source generator emits SYSLIB1229; suppress it here so
    // we can verify the runtime drops the bad arm and keeps the well-formed "cat" arm.
#pragma warning disable SYSLIB1229
    [JsonDerivedType(typeof(SourceGenExtraParam_Cat<,>), "cat2")]
#pragma warning restore SYSLIB1229
    public class SourceGenExtraParamAnimal<T>
    {
        public T? Tag { get; set; }
    }

    public class SourceGenExtraParam_Cat<T> : SourceGenExtraParamAnimal<T>
    {
        public string? Name { get; set; }
    }

    public class SourceGenExtraParam_Cat<T, T2> : SourceGenExtraParamAnimal<T>
    {
        public T2? Extra { get; set; }
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

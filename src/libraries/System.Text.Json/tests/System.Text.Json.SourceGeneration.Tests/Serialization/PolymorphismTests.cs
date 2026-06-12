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
        public static void OpenGenericDerivedType_NonUniformPinning_IsDroppedFromMetadata()
        {
            // SourceGenOpenGenericDerived<T> : SourceGenOpenGenericBase<T, int> pins T2 to
            // int -- non-uniform w.r.t. SourceGenOpenGenericBase<T1, T2>. Source-gen
            // emits SYSLIB1229 (suppressed on the fixture decoration) and drops the
            // registration from the generated metadata; the closed base has no
            // PolymorphismOptions and serializes plainly.
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenOpenGenericBaseStringInt32;
            Assert.Null(typeInfo.PolymorphismOptions);

            SourceGenOpenGenericBase<string, int> value = new SourceGenOpenGenericDerived<string> { Extra = "hello" };
            string json = JsonSerializer.Serialize(value, typeInfo);
            Assert.DoesNotContain("$type", json);
        }

        [Fact]
        public static void ClosedDerivedOnGenericBase_IsDroppedOnIntSpec()
        {
            // Under the uniform-applicability rule, closed-derived registrations on an
            // OPEN generic base (here SourceGenSpecAnimal_Cat : SourceGenSpecAnimal<int>
            // and Dog : SourceGenSpecAnimal<string>) are non-uniform -- they can never
            // apply to every closure of the open base. Source-gen drops them from emitted
            // metadata (with SYSLIB1229 suppressed on the fixture) so the closed base has
            // no PolymorphismOptions and serializes plainly.
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenSpecAnimalInt;
            Assert.Null(typeInfo.PolymorphismOptions);

            SourceGenSpecAnimal<int> cat = new SourceGenSpecAnimal_Cat { Name = "Felix", Lives = 9 };
            string json = JsonSerializer.Serialize(cat, typeInfo);
            Assert.DoesNotContain("$type", json);
        }

        [Fact]
        public static void ClosedDerivedOnGenericBase_IsDroppedOnStringSpec()
        {
            // Companion to ClosedDerivedOnGenericBase_IsDroppedOnIntSpec for the string
            // closure. Same rejection regardless of which closure is constructed.
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenSpecAnimalString;
            Assert.Null(typeInfo.PolymorphismOptions);

            SourceGenSpecAnimal<string> dog = new SourceGenSpecAnimal_Dog { Name = "Rex", Breed = "Husky" };
            string json = JsonSerializer.Serialize(dog, typeInfo);
            Assert.DoesNotContain("$type", json);
        }

        [Fact]
        public static void ClosedDerivedOnGenericBase_IsDroppedOnUnrelatedSpec()
        {
            // The bool closure of SourceGenSpecAnimal<T> -- no closed-derived registration
            // would have matched even under per-closure filtering. The emitted typeInfo has
            // no PolymorphismOptions and serializes plainly.
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenSpecAnimalBool;
            Assert.Null(typeInfo.PolymorphismOptions);

            string json = JsonSerializer.Serialize(new SourceGenSpecAnimal<bool> { Name = "Cookie" }, typeInfo);
            Assert.DoesNotContain("$type", json);
            Assert.Contains("\"Name\":\"Cookie\"", json);
        }

        [Fact]
        public static void OpenDerivedWithNarrowerConstraint_IsDroppedOnUnsatisfyingSpec()
        {
            // SourceGenConstrainedDerived<T> where T : struct, registered on
            // SourceGenConstrainedBase<T>. The derived's struct constraint is narrower
            // than the base's (none), so the registration is non-uniform under B-strict.
            // Source-gen emits SYSLIB1229 (suppressed on the fixture) and drops the
            // registration; every closure of the base has null PolymorphismOptions.
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenConstrainedBaseString;
            Assert.Null(typeInfo.PolymorphismOptions);

            string json = JsonSerializer.Serialize(new SourceGenConstrainedBase<string> { Value = "hi" }, typeInfo);
            Assert.DoesNotContain("$type", json);
        }

        [Fact]
        public static void OpenDerivedWithNarrowerConstraint_IsDroppedOnSatisfyingSpec()
        {
            // Same fixture as above, applied to a closure (int) where the derived's
            // struct constraint happens to hold. Under per-closure filtering this would
            // have been accepted; under B-strict uniform applicability it is rejected
            // uniformly so that introducing a new closure (e.g. <string>) cannot break a
            // working serialization site.
            JsonTypeInfo typeInfo = PolymorphismTestsContext.Default.SourceGenConstrainedBaseInt;
            Assert.Null(typeInfo.PolymorphismOptions);

            string json = JsonSerializer.Serialize(new SourceGenConstrainedBase<int> { Value = 1 }, typeInfo);
            Assert.DoesNotContain("$type", json);
        }

        [Fact]
        public static void OpenDerivedWithExtraUnboundParameter_BadArmIsDroppedFromEmittedMetadata()
        {
            // Two registrations on the same base: SourceGenExtraParam_Cat<T> is uniform
            // and resolves to SourceGenExtraParam_Cat<int>; SourceGenExtraParam_Cat<T, T2>
            // has an extra unbound T2 that the single-parameter base cannot pin down, so
            // source-gen emits SYSLIB1229 (suppressed below) and drops it from the
            // generated metadata. Only the well-formed "cat" arm survives in the emitted
            // typeInfo.
            //
            // This is the documented source-gen-vs-reflection asymmetry: source-gen drops
            // bad arms per-attribute, reflection throws on the first bad arm.
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

    // SourceGenOpenGenericDerived<T> : SourceGenOpenGenericBase<T, int> pins the second
    // base parameter to a concrete type -- non-uniform under B-strict. Source-gen emits
    // SYSLIB1229 at compile time; suppress it here so we can verify the runtime drops the
    // registration and the closed base serializes plainly.
#pragma warning disable SYSLIB1229
    [JsonDerivedType(typeof(SourceGenOpenGenericDerived<>), "derived")]
#pragma warning restore SYSLIB1229
    public class SourceGenOpenGenericBase<T1, T2>
    {
        public T1? Value1 { get; set; }
        public T2? Value2 { get; set; }
    }

    public sealed class SourceGenOpenGenericDerived<T> : SourceGenOpenGenericBase<T, int>
    {
        public T? Extra { get; set; }
    }

    // Specialization-pinning fixtures (PR #129294, B-strict uniform-applicability rule).
    // Each closed-derived registration applies to a single specialization of the open base,
    // which the shared attribute declaration cannot enforce uniformly. Source-gen emits
    // SYSLIB1229; suppress here so we can verify the runtime drops the registration and the
    // closed base serializes plainly.

#pragma warning disable SYSLIB1229
    [JsonDerivedType(typeof(SourceGenSpecAnimal_Cat), "cat")]
    [JsonDerivedType(typeof(SourceGenSpecAnimal_Dog), "dog")]
#pragma warning restore SYSLIB1229
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

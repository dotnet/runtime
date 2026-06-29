// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET11_0_OR_GREATER
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class PolymorphicTests
    {
        // The closed-type hierarchies exercised below live in the IL fixture assembly
        // System.Text.Json.ClosedTypeTestFixtures. They are annotated with [IsClosedType],
        // which the C# compiler reserves for its own use, so the metadata cannot be authored
        // in C#. Closed-type polymorphism inference is a reflection-only feature, so these
        // tests rely on the default reflection-based resolver and are skipped when reflection
        // is disabled (for example under source-gen subclasses or NativeAOT).
        private static JsonSerializerOptions CreateClosedTypeInferenceOptions(bool infer = true) =>
            new JsonSerializerOptions
            {
                InferClosedTypePolymorphism = infer,
            };

        [Theory]
        [InlineData(typeof(ClosedCircle), "ClosedCircle")]
        [InlineData(typeof(ClosedSquare), "ClosedSquare")]
        public async Task ClosedTypeInference_BasicHierarchy_EmitsAndReadsTypeDiscriminator(Type derivedType, string expectedDiscriminator)
        {
            if (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                return;
            }

            JsonSerializerOptions options = CreateClosedTypeInferenceOptions();

            ClosedShape value = (ClosedShape)Activator.CreateInstance(derivedType)!;
            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual($$"""{"$type":"{{expectedDiscriminator}}"}""", json);

            ClosedShape roundtripped = await Serializer.DeserializeWrapper<ClosedShape>(json, options);
            Assert.IsType(derivedType, roundtripped);
        }

        [Fact]
        public async Task ClosedTypeInference_CollectionOfClosedBase_InfersEachElement()
        {
            if (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                return;
            }

            JsonSerializerOptions options = CreateClosedTypeInferenceOptions();

            List<ClosedShape> value = new() { new ClosedCircle(), new ClosedSquare() };
            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual("""[{"$type":"ClosedCircle"},{"$type":"ClosedSquare"}]""", json);

            List<ClosedShape> roundtripped = await Serializer.DeserializeWrapper<List<ClosedShape>>(json, options);
            Assert.Collection(
                roundtripped,
                element => Assert.IsType<ClosedCircle>(element),
                element => Assert.IsType<ClosedSquare>(element));
        }

        [Fact]
        public async Task ClosedTypeInference_NestedClosedProperty_InfersAlongsideRegularProperties()
        {
            if (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                return;
            }

            JsonSerializerOptions options = CreateClosedTypeInferenceOptions();

            ClosedShapeHolder value = new() { Name = "holder", Shape = new ClosedSquare() };
            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual("""{"Name":"holder","Shape":{"$type":"ClosedSquare"}}""", json);

            ClosedShapeHolder roundtripped = await Serializer.DeserializeWrapper<ClosedShapeHolder>(json, options);
            Assert.Equal("holder", roundtripped.Name);
            Assert.IsType<ClosedSquare>(roundtripped.Shape);
        }

        [Fact]
        public async Task ClosedTypeInference_DeserializeUnknownDiscriminator_Throws()
        {
            if (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                return;
            }

            JsonSerializerOptions options = CreateClosedTypeInferenceOptions();

            // "Triangle" is not part of the closed ClosedShape hierarchy, so resolving it must fail.
            await Assert.ThrowsAsync<JsonException>(
                () => Serializer.DeserializeWrapper<ClosedShape>("""{"$type":"Triangle"}""", options));
        }

        [Fact]
        public async Task ClosedTypeInference_FlagDisabled_DoesNotInferPolymorphism()
        {
            if (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                return;
            }

            JsonSerializerOptions options = CreateClosedTypeInferenceOptions(infer: false);

            ClosedShape value = new ClosedCircle();
            string json = await Serializer.SerializeWrapper(value, options);

            Assert.DoesNotContain("$type", json);
        }

        [Fact]
        public async Task ClosedTypeInference_EmptyDerivedTypes_IsInert()
        {
            if (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                return;
            }

            JsonSerializerOptions options = CreateClosedTypeInferenceOptions();

            EmptyClosedBase value = new EmptyClosedDerived();
            string json = await Serializer.SerializeWrapper(value, options);

            Assert.DoesNotContain("$type", json);
        }

        [Fact]
        public async Task ClosedTypeInference_OpenGenericDerived_IsInferredAndResolved()
        {
            if (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                return;
            }

            JsonSerializerOptions options = CreateClosedTypeInferenceOptions();

            ClosedContainer<string> value = new ClosedBox<string>();
            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual("""{"$type":"ClosedBox`1"}""", json);

            ClosedContainer<string> roundtripped = await Serializer.DeserializeWrapper<ClosedContainer<string>>(json, options);
            Assert.IsType<ClosedBox<string>>(roundtripped);
        }

        [Fact]
        public async Task ClosedTypeInference_DuplicateDiscriminator_ThrowsInvalidOperationException()
        {
            if (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                return;
            }

            JsonSerializerOptions options = CreateClosedTypeInferenceOptions();

            ClosedCollisionBase value = new ClosedCollisionHolderA.Node();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value, options));
        }

        [Fact]
        public async Task ClosedTypeInference_InaccessibleDerivedType_ThrowsInvalidOperationException()
        {
            if (!JsonSerializer.IsReflectionEnabledByDefault)
            {
                return;
            }

            JsonSerializerOptions options = CreateClosedTypeInferenceOptions();

            // The closed hierarchy mixes a public and an internal derived type. The internal
            // type is less accessible than the public base, so inference must reject it.
            PublicClosedBase value = new PublicClosedDerived();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value, options));
        }
    }

    // Regular POCO that holds a closed-type property; used to verify inference applies to
    // nested closed values alongside ordinary properties.
    public sealed class ClosedShapeHolder
    {
        public string? Name { get; set; }
        public ClosedShape? Shape { get; set; }
    }
}
#endif

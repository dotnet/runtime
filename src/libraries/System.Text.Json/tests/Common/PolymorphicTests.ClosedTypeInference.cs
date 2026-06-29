// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if NET11_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class PolymorphicTests
    {
        // The closed-type hierarchies exercised below live in the IL fixture assembly
        // System.Text.Json.ClosedTypeTestFixtures. They are annotated with
        // [IsClosedType], which the C# compiler reserves for its own use, so the metadata
        // cannot be authored in C#. These tests always run against the reflection-based
        // resolver (even under source-gen subclasses) by supplying an explicit
        // DefaultJsonTypeInfoResolver, so they validate the reflection inference path.
        private static JsonSerializerOptions CreateClosedTypeInferenceOptions(bool infer = true) =>
            new JsonSerializerOptions
            {
                TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
                InferClosedTypePolymorphism = infer,
            };

        [Fact]
        public async Task ClosedTypeInference_BasicHierarchy_EmitsAndReadsTypeDiscriminator()
        {
            JsonSerializerOptions options = CreateClosedTypeInferenceOptions();

            ClosedShape value = new ClosedCircle();
            string json = await Serializer.SerializeWrapper(value, options);
            JsonTestHelper.AssertJsonEqual("""{"$type":"ClosedCircle"}""", json);

            ClosedShape roundtripped = await Serializer.DeserializeWrapper<ClosedShape>(json, options);
            Assert.IsType<ClosedCircle>(roundtripped);
        }

        [Fact]
        public async Task ClosedTypeInference_FlagDisabled_DoesNotInferPolymorphism()
        {
            JsonSerializerOptions options = CreateClosedTypeInferenceOptions(infer: false);

            ClosedShape value = new ClosedCircle();
            string json = await Serializer.SerializeWrapper(value, options);

            Assert.DoesNotContain("$type", json);
        }

        [Fact]
        public async Task ClosedTypeInference_EmptyDerivedTypes_IsInert()
        {
            JsonSerializerOptions options = CreateClosedTypeInferenceOptions();

            EmptyClosedBase value = new EmptyClosedDerived();
            string json = await Serializer.SerializeWrapper(value, options);

            Assert.DoesNotContain("$type", json);
        }

        [Fact]
        public async Task ClosedTypeInference_OpenGenericDerived_IsInferredAndResolved()
        {
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
            JsonSerializerOptions options = CreateClosedTypeInferenceOptions();

            ClosedCollisionBase value = new ClosedCollisionHolderA.Node();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value, options));
        }

        [Fact]
        public async Task ClosedTypeInference_InaccessibleDerivedType_ThrowsInvalidOperationException()
        {
            JsonSerializerOptions options = CreateClosedTypeInferenceOptions();

            // The closed hierarchy mixes a public and an internal derived type. The internal
            // type is less accessible than the public base, so inference must reject it.
            PublicClosedBase value = new PublicClosedDerived();
            await Assert.ThrowsAsync<InvalidOperationException>(() => Serializer.SerializeWrapper(value, options));
        }
    }
}
#endif

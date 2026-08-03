// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;
using System.Text.Json.SourceGeneration.Tests.NETStandard;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed partial class PolymorphicTests_Metadata
    {
        protected override JsonSerializerOptions ClosedTypeInferenceOptions =>
            ClosedInferenceContext_Metadata.Default.Options;

        [JsonSerializable(typeof(ClosedEmptyBase))]
        [JsonSerializable(typeof(ClosedNumberList))]
        [JsonSerializable(typeof(ClosedShape))]
        internal sealed partial class PolymorphicTestsContext_Metadata;

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RuntimeInferClosedTypePolymorphism_EmptyHierarchy_IsInert(bool defaultMode)
        {
            var options = new JsonSerializerOptions
            {
                InferClosedTypePolymorphism = true,
            };

            JsonSerializerContext context = defaultMode
                ? new PolymorphicTests_Default.PolymorphicTestsContext_Default(options)
                : new PolymorphicTestsContext_Metadata(options);

            Assert.Same(options, context.Options);
            Assert.Null(options.GetTypeInfo(typeof(ClosedEmptyBase)).PolymorphismOptions);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void ClosedTypeInference_FromReferencedAssembly(bool defaultMode)
        {
            JsonSerializerContext context = defaultMode
                ? ClosedInferenceContext_Default.Default
                : ClosedInferenceContext_Metadata.Default;

            JsonTypeInfo? typeInfo = context.GetTypeInfo(typeof(ReferencedClosedShape));
            Assert.NotNull(typeInfo);
            JsonPolymorphismOptions? polymorphismOptions = typeInfo.PolymorphismOptions;
            Assert.NotNull(polymorphismOptions);

            Assert.Collection(
                polymorphismOptions.DerivedTypes,
                derivedType =>
                {
                    Assert.Equal(typeof(ReferencedClosedCircle), derivedType.DerivedType);
                    Assert.Equal(nameof(ReferencedClosedCircle), derivedType.TypeDiscriminator);
                },
                derivedType =>
                {
                    Assert.Equal(typeof(ReferencedClosedSquare), derivedType.DerivedType);
                    Assert.Equal(nameof(ReferencedClosedSquare), derivedType.TypeDiscriminator);
                });
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void RuntimeInferClosedTypePolymorphism_WithoutGeneratedMetadata_Throws(
            bool defaultMode,
            bool closedCollection)
        {
            var options = new JsonSerializerOptions
            {
                InferClosedTypePolymorphism = true,
            };

            JsonSerializerContext context = defaultMode
                ? new PolymorphicTests_Default.PolymorphicTestsContext_Default(options)
                : new PolymorphicTestsContext_Metadata(options);

            Assert.Same(options, context.Options);

            Type baseType = closedCollection ? typeof(ClosedNumberList) : typeof(ClosedShape);
            InvalidOperationException exception =
                Assert.Throws<InvalidOperationException>(() => options.GetTypeInfo(baseType));

            Assert.Contains(baseType.ToString(), exception.Message);
            Assert.Contains(
                $"{nameof(JsonSerializerOptions)}.{nameof(JsonSerializerOptions.InferClosedTypePolymorphism)}",
                exception.Message);
            Assert.Contains(
                $"{nameof(JsonSourceGenerationOptionsAttribute)}.{nameof(JsonSourceGenerationOptionsAttribute.InferClosedTypePolymorphism)}",
                exception.Message);
        }
    }

    public sealed partial class PolymorphicTests_Metadata_AsyncStream
    {
        protected override JsonSerializerOptions ClosedTypeInferenceOptions =>
            ClosedInferenceContext_Metadata.Default.Options;
    }

    public sealed partial class PolymorphicTests_Default
    {
        protected override JsonSerializerOptions ClosedTypeInferenceOptions =>
            ClosedInferenceContext_Default.Default.Options;

        [JsonSerializable(typeof(ClosedEmptyBase))]
        [JsonSerializable(typeof(ClosedNumberList))]
        [JsonSerializable(typeof(ClosedShape))]
        internal sealed partial class PolymorphicTestsContext_Default;
    }

    public sealed partial class PolymorphicTests_Default_AsyncStream
    {
        protected override JsonSerializerOptions ClosedTypeInferenceOptions =>
            ClosedInferenceContext_Default.Default.Options;
    }

    public closed class ClosedNumberList : List<int>;
    public sealed class AscendingNumberList : ClosedNumberList;
    public sealed class DescendingNumberList : ClosedNumberList;

    // These contexts intentionally include invalid inferred registrations whose diagnostics are
    // validated by the corresponding runtime-behavior tests.
#pragma warning disable SYSLIB1229, SYSLIB1240, SYSLIB1241, SYSLIB1242
    [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata, InferClosedTypePolymorphism = true)]
    [JsonSerializable(typeof(ClosedAccessBase))]
    [JsonSerializable(typeof(ClosedArrayBase<int[]>))]
    [JsonSerializable(typeof(ClosedCollisionBase))]
    // Both inferred types are named Node, so give their generated metadata properties unique names.
    [JsonSerializable(typeof(ClosedCollisionHolderA.Node), TypeInfoPropertyName = "ClosedCollisionNodeA")]
    [JsonSerializable(typeof(ClosedCollisionHolderB.Node), TypeInfoPropertyName = "ClosedCollisionNodeB")]
    [JsonSerializable(typeof(ClosedConcreteMismatchBase<string>))]
    [JsonSerializable(typeof(ClosedConstrainedBase<List<string>>))]
    [JsonSerializable(typeof(ClosedConstraintViolationBase<string>))]
    [JsonSerializable(typeof(ClosedContainer<int>))]
    [JsonSerializable(typeof(ClosedContainer<List<int>>))]
    [JsonSerializable(typeof(ClosedContainer<string>))]
    [JsonSerializable(typeof(ClosedCustomDiscriminatorBase))]
    [JsonSerializable(typeof(ClosedDeepJaggedBase<List<int[][][]>>))]
    [JsonSerializable(typeof(ClosedDeepJaggedMismatchBase<List<int[][]>>))]
    [JsonSerializable(typeof(ClosedDuplicateArityBase<int, int>))]
    [JsonSerializable(typeof(ClosedDuplicateArityBase<int, string>))]
    [JsonSerializable(typeof(ClosedDuplicateArityDerived<int>), TypeInfoPropertyName = "ClosedDuplicateArityDerivedOne")]
    [JsonSerializable(typeof(ClosedDuplicateArityDerived<int, int>), TypeInfoPropertyName = "ClosedDuplicateArityDerivedTwoIntInt")]
    [JsonSerializable(typeof(ClosedDuplicateArityDerived<int, string>), TypeInfoPropertyName = "ClosedDuplicateArityDerivedTwoIntString")]
    [JsonSerializable(typeof(ClosedEmptyBase))]
    [JsonSerializable(typeof(ClosedExplicitBase))]
    [JsonSerializable(typeof(ClosedGroundMismatchBase<int, string>))]
    [JsonSerializable(typeof(ClosedKvpBase<KeyValuePair<string, int>>))]
    [JsonSerializable(typeof(ClosedMixedBase<int>))]
    [JsonSerializable(typeof(ClosedNestedAccessContainer.Base))]
    [JsonSerializable(typeof(ClosedNestedArgBase<ClosedNestedOuter<string>.NestedBox<int>>))]
    [JsonSerializable(typeof(ClosedNestedDerivedBase<int>))]
    [JsonSerializable(typeof(ClosedNestedMismatchBase<ClosedNestedOuter<string>.NestedBox<int>>))]
    [JsonSerializable(typeof(ClosedPair<string, int>))]
    [JsonSerializable(typeof(ClosedPartialBase<string, int>))]
    [JsonSerializable(typeof(ClosedPayload))]
    [JsonSerializable(typeof(ClosedProtectedAccessBase))]
    [JsonSerializable(typeof(ClosedRepeatedBase<int, int>))]
    [JsonSerializable(typeof(ClosedRepeatedMismatchBase<int, string>))]
    [JsonSerializable(typeof(ClosedReorderedBase<int, string>))]
    [JsonSerializable(typeof(ClosedShape))]
    [JsonSerializable(typeof(ClosedShapeHolder))]
    [JsonSerializable(typeof(ClosedTupleBase<(int, string)>))]
    [JsonSerializable(typeof(ClosedUnspeakableBase<int[]>))]
    [JsonSerializable(typeof(ClosedUnspeakableBase<string>))]
    [JsonSerializable(typeof(ClosedWrappedBase<List<string>>))]
    [JsonSerializable(typeof(List<ClosedShape>))]
    [JsonSerializable(typeof(PlainAbstractBase))]
    [JsonSerializable(typeof(ReferencedClosedShape))]
    internal sealed partial class ClosedInferenceContext_Metadata : JsonSerializerContext;

    [JsonSourceGenerationOptions(InferClosedTypePolymorphism = true)]
    [JsonSerializable(typeof(ClosedAccessBase))]
    [JsonSerializable(typeof(ClosedArrayBase<int[]>))]
    [JsonSerializable(typeof(ClosedCollisionBase))]
    [JsonSerializable(typeof(ClosedCollisionHolderA.Node), TypeInfoPropertyName = "ClosedCollisionNodeA")]
    [JsonSerializable(typeof(ClosedCollisionHolderB.Node), TypeInfoPropertyName = "ClosedCollisionNodeB")]
    [JsonSerializable(typeof(ClosedConcreteMismatchBase<string>))]
    [JsonSerializable(typeof(ClosedConstrainedBase<List<string>>))]
    [JsonSerializable(typeof(ClosedConstraintViolationBase<string>))]
    [JsonSerializable(typeof(ClosedContainer<int>))]
    [JsonSerializable(typeof(ClosedContainer<List<int>>))]
    [JsonSerializable(typeof(ClosedContainer<string>))]
    [JsonSerializable(typeof(ClosedCustomDiscriminatorBase))]
    [JsonSerializable(typeof(ClosedDeepJaggedBase<List<int[][][]>>))]
    [JsonSerializable(typeof(ClosedDeepJaggedMismatchBase<List<int[][]>>))]
    [JsonSerializable(typeof(ClosedDuplicateArityBase<int, int>))]
    [JsonSerializable(typeof(ClosedDuplicateArityBase<int, string>))]
    [JsonSerializable(typeof(ClosedDuplicateArityDerived<int>), TypeInfoPropertyName = "ClosedDuplicateArityDerivedOne")]
    [JsonSerializable(typeof(ClosedDuplicateArityDerived<int, int>), TypeInfoPropertyName = "ClosedDuplicateArityDerivedTwoIntInt")]
    [JsonSerializable(typeof(ClosedDuplicateArityDerived<int, string>), TypeInfoPropertyName = "ClosedDuplicateArityDerivedTwoIntString")]
    [JsonSerializable(typeof(ClosedEmptyBase))]
    [JsonSerializable(typeof(ClosedExplicitBase))]
    [JsonSerializable(typeof(ClosedGroundMismatchBase<int, string>))]
    [JsonSerializable(typeof(ClosedKvpBase<KeyValuePair<string, int>>))]
    [JsonSerializable(typeof(ClosedMixedBase<int>))]
    [JsonSerializable(typeof(ClosedNestedAccessContainer.Base))]
    [JsonSerializable(typeof(ClosedNestedArgBase<ClosedNestedOuter<string>.NestedBox<int>>))]
    [JsonSerializable(typeof(ClosedNestedDerivedBase<int>))]
    [JsonSerializable(typeof(ClosedNestedMismatchBase<ClosedNestedOuter<string>.NestedBox<int>>))]
    [JsonSerializable(typeof(ClosedPair<string, int>))]
    [JsonSerializable(typeof(ClosedPartialBase<string, int>))]
    [JsonSerializable(typeof(ClosedPayload))]
    [JsonSerializable(typeof(ClosedProtectedAccessBase))]
    [JsonSerializable(typeof(ClosedRepeatedBase<int, int>))]
    [JsonSerializable(typeof(ClosedRepeatedMismatchBase<int, string>))]
    [JsonSerializable(typeof(ClosedReorderedBase<int, string>))]
    [JsonSerializable(typeof(ClosedShape))]
    [JsonSerializable(typeof(ClosedShapeHolder))]
    [JsonSerializable(typeof(ClosedTupleBase<(int, string)>))]
    [JsonSerializable(typeof(ClosedUnspeakableBase<int[]>))]
    [JsonSerializable(typeof(ClosedUnspeakableBase<string>))]
    [JsonSerializable(typeof(ClosedWrappedBase<List<string>>))]
    [JsonSerializable(typeof(List<ClosedShape>))]
    [JsonSerializable(typeof(PlainAbstractBase))]
    [JsonSerializable(typeof(ReferencedClosedShape))]
    internal sealed partial class ClosedInferenceContext_Default : JsonSerializerContext;
#pragma warning restore SYSLIB1229, SYSLIB1240, SYSLIB1241, SYSLIB1242

}

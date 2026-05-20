// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed partial class UnionTests_Metadata : UnionTests
    {
        public UnionTests_Metadata()
            : base(new StringSerializerWrapper(UnionTestsContext_Metadata.Default))
        {
        }

#pragma warning disable SYSLIB1227
        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
        [JsonSerializable(typeof(MixedUnion))]
        [JsonSerializable(typeof(ObjectUnion))]
        [JsonSerializable(typeof(Payload))]
        [JsonSerializable(typeof(HierarchyUnion))]
        [JsonSerializable(typeof(NullableCaseUnion))]
        [JsonSerializable(typeof(NoNullableCaseUnion))]
        [JsonSerializable(typeof(ValueTypeNullablePairUnion))]
        [JsonSerializable(typeof(SingleObjectUnion))]
        [JsonSerializable(typeof(ClassifiedPayloadUnion))]
        [JsonSerializable(typeof(UnionWithCustomClassifier))]
        [JsonSerializable(typeof(UnionWithCustomConverterCase))]
        [JsonSerializable(typeof(UnionWithIntAndLongCase))]
        [JsonSerializable(typeof(UnionWithMixedAmbiguity))]
        [JsonSerializable(typeof(AsyncEnumerableUnion))]
        [JsonSerializable(typeof(CustomCase))]
        [JsonSerializable(typeof(OtherCase))]
        [JsonSerializable(typeof(PayloadCase))]
        [JsonSerializable(typeof(UnionContainer))]
        [JsonSerializable(typeof(NullableScalarUnionContainer))]
        [JsonSerializable(typeof(UserDefinedAttributeOnlyUnion))]
        [JsonSerializable(typeof(UserDefinedNullableAttributeOnlyUnion))]
        [JsonSerializable(typeof(UserDefinedCtorVsImplicitOpUnion))]
        [JsonSerializable(typeof(UserDefinedUnconventionalUnion))]
        [JsonSerializable(typeof(UserDefinedUnionWithoutValueProperty))]
        [JsonSerializable(typeof(UserDefinedJsonUnionOnPlainObject))]
        [JsonSerializable(typeof(UserDefinedUnionViaIUnion))]
        [JsonSerializable(typeof(UserDefinedValueTypeUnion))]
        [JsonSerializable(typeof(CustomDiscriminatedAnimalUnion))]
        [JsonSerializable(typeof(CustomDiscriminatedAnimalUnion_NoConvention))]
        [JsonSerializable(typeof(CustomDiscriminatedScalarUnion))]
        [JsonSerializable(typeof(CustomDiscriminatedFlora))]
        [JsonSerializable(typeof(NullableEnumUnion))]
        [JsonSerializable(typeof(RecursiveNat))]
        [JsonSerializable(typeof(SelfReferentialUnion))]
        internal sealed partial class UnionTestsContext_Metadata : JsonSerializerContext
        {
        }
#pragma warning restore SYSLIB1227
    }

    public sealed partial class UnionTests_Metadata_AsyncStream : UnionTests
    {
        public UnionTests_Metadata_AsyncStream()
            : base(new AsyncStreamSerializerWrapper(UnionTests_Metadata.UnionTestsContext_Metadata.Default))
        {
        }
    }

    public sealed partial class UnionTests_Default : UnionTests
    {
        public UnionTests_Default()
            : base(new StringSerializerWrapper(UnionTestsContext_Default.Default))
        {
        }

#pragma warning disable SYSLIB1227
        [JsonSerializable(typeof(MixedUnion))]
        [JsonSerializable(typeof(ObjectUnion))]
        [JsonSerializable(typeof(Payload))]
        [JsonSerializable(typeof(HierarchyUnion))]
        [JsonSerializable(typeof(NullableCaseUnion))]
        [JsonSerializable(typeof(NoNullableCaseUnion))]
        [JsonSerializable(typeof(ValueTypeNullablePairUnion))]
        [JsonSerializable(typeof(SingleObjectUnion))]
        [JsonSerializable(typeof(ClassifiedPayloadUnion))]
        [JsonSerializable(typeof(UnionWithCustomClassifier))]
        [JsonSerializable(typeof(UnionWithCustomConverterCase))]
        [JsonSerializable(typeof(UnionWithIntAndLongCase))]
        [JsonSerializable(typeof(UnionWithMixedAmbiguity))]
        [JsonSerializable(typeof(AsyncEnumerableUnion))]
        [JsonSerializable(typeof(CustomCase))]
        [JsonSerializable(typeof(OtherCase))]
        [JsonSerializable(typeof(PayloadCase))]
        [JsonSerializable(typeof(UnionContainer))]
        [JsonSerializable(typeof(NullableScalarUnionContainer))]
        [JsonSerializable(typeof(UserDefinedAttributeOnlyUnion))]
        [JsonSerializable(typeof(UserDefinedNullableAttributeOnlyUnion))]
        [JsonSerializable(typeof(UserDefinedCtorVsImplicitOpUnion))]
        [JsonSerializable(typeof(UserDefinedUnconventionalUnion))]
        [JsonSerializable(typeof(UserDefinedUnionWithoutValueProperty))]
        [JsonSerializable(typeof(UserDefinedJsonUnionOnPlainObject))]
        [JsonSerializable(typeof(UserDefinedUnionViaIUnion))]
        [JsonSerializable(typeof(UserDefinedValueTypeUnion))]
        [JsonSerializable(typeof(CustomDiscriminatedAnimalUnion))]
        [JsonSerializable(typeof(CustomDiscriminatedAnimalUnion_NoConvention))]
        [JsonSerializable(typeof(CustomDiscriminatedScalarUnion))]
        [JsonSerializable(typeof(CustomDiscriminatedFlora))]
        [JsonSerializable(typeof(NullableEnumUnion))]
        [JsonSerializable(typeof(RecursiveNat))]
        [JsonSerializable(typeof(SelfReferentialUnion))]
        internal sealed partial class UnionTestsContext_Default : JsonSerializerContext
        {
        }
#pragma warning restore SYSLIB1227
    }

    public sealed partial class UnionTests_Default_AsyncStream : UnionTests
    {
        public UnionTests_Default_AsyncStream()
            : base(new AsyncStreamSerializerWrapper(UnionTests_Default.UnionTestsContext_Default.Default))
        {
        }
    }
}

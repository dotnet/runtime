// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed class NullableAnnotationsTests_Metadata_String : NullableAnnotationsTests_Metadata
    {
        public NullableAnnotationsTests_Metadata_String()
            : base(new StringSerializerWrapper(NullableAnnotationsTestsContext_Metadata.Default)) { }
    }

    public sealed class NullableAnnotationsTests_Metadata_AsyncStream : NullableAnnotationsTests_Metadata
    {
        public NullableAnnotationsTests_Metadata_AsyncStream()
            : base(new AsyncStreamSerializerWrapper(NullableAnnotationsTestsContext_Metadata.Default)) { }
    }

    public abstract partial class NullableAnnotationsTests_Metadata : NullableAnnotationsTests
    {
        protected NullableAnnotationsTests_Metadata(JsonSerializerWrapper serializer)
            : base(serializer) { }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata, RespectNullableAnnotations = false)]
        [JsonSerializable(typeof(NotNullablePropertyClass))]
        [JsonSerializable(typeof(NotNullableReadonlyPropertyClass))]
        [JsonSerializable(typeof(NotNullableFieldClass))]
        [JsonSerializable(typeof(NotNullableSpecialTypePropertiesClass))]
        [JsonSerializable(typeof(NotNullablePropertyWithHandleNullConverterClass))]
        [JsonSerializable(typeof(NotNullablePropertyWithConverterClass))]
        [JsonSerializable(typeof(NotNullablePropertyWithAlwaysNullConverterClass))]
        [JsonSerializable(typeof(NotNullablePropertyParameterizedCtorClass))]
        [JsonSerializable(typeof(NotNullablePropertyWithHandleNullConverterParameterizedCtorClass))]
        [JsonSerializable(typeof(NotNullablePropertyWithAlwaysNullConverterParameterizedCtorClass))]
        [JsonSerializable(typeof(NotNullablePropertiesLargeParameterizedCtorClass))]
        [JsonSerializable(typeof(NotNullablePropertyWithHandleNullConverterLargeParameterizedCtorClass))]
        [JsonSerializable(typeof(NotNullablePropertyWithAlwaysNullConverterLargeParameterizedCtorClass))]
        [JsonSerializable(typeof(NotNullPropertyClass))]
        [JsonSerializable(typeof(MaybeNullPropertyClass))]
        [JsonSerializable(typeof(AllowNullPropertyClass))]
        [JsonSerializable(typeof(DisallowNullPropertyClass))]
        [JsonSerializable(typeof(AllowNullConstructorParameter))]
        [JsonSerializable(typeof(DisallowNullConstructorParameter))]
        [JsonSerializable(typeof(NullStructPropertyClass))]
        [JsonSerializable(typeof(NullStructConstructorParameterClass))]
        [JsonSerializable(typeof(NotNullStructPropertyClass))]
        [JsonSerializable(typeof(DisallowNullStructPropertyClass))]
        [JsonSerializable(typeof(DisallowNullStructConstructorParameter))]
        [JsonSerializable(typeof(NotNullPropertyClass<string>))]
        [JsonSerializable(typeof(MaybeNullPropertyClass<string>))]
        [JsonSerializable(typeof(AllowNullPropertyClass<string>))]
        [JsonSerializable(typeof(DisallowNullPropertyClass<string>))]
        [JsonSerializable(typeof(AllowNullConstructorParameter<string>))]
        [JsonSerializable(typeof(DisallowNullConstructorParameter<string>))]
        [JsonSerializable(typeof(NullablePropertyClass))]
        [JsonSerializable(typeof(NullableFieldClass))]
        [JsonSerializable(typeof(NullableObliviousPropertyClass))]
        [JsonSerializable(typeof(NullableObliviousConstructorParameter))]
        [JsonSerializable(typeof(GenericPropertyClass<string>))]
        [JsonSerializable(typeof(NullableGenericPropertyClass<string>))]
        [JsonSerializable(typeof(NotNullGenericPropertyClass<string>))]
        [JsonSerializable(typeof(GenericConstructorParameter<string>))]
        [JsonSerializable(typeof(NullableGenericConstructorParameter<string>))]
        [JsonSerializable(typeof(NotNullGenericConstructorParameter<string>))]
        [JsonSerializable(typeof(NotNullablePropertyWithIgnoreConditions))]
        internal sealed partial class NullableAnnotationsTestsContext_Metadata
            : JsonSerializerContext { }
    }

    public sealed class NullableAnnotationsTests_Default_String : NullableAnnotationsTests_Default
    {
        public NullableAnnotationsTests_Default_String()
            : base(new StringSerializerWrapper(NullableAnnotationsTestsContext_Default.Default)) { }
    }

    public sealed class NullableAnnotationsTests_Default_AsyncStream : NullableAnnotationsTests_Default
    {
        public NullableAnnotationsTests_Default_AsyncStream()
            : base(new AsyncStreamSerializerWrapper(NullableAnnotationsTestsContext_Default.Default)) { }
    }

    public abstract partial class NullableAnnotationsTests_Default : NullableAnnotationsTests
    {
        protected NullableAnnotationsTests_Default(JsonSerializerWrapper serializer)
            : base(serializer) { }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Default, RespectNullableAnnotations = false)]
        [JsonSerializable(typeof(NotNullablePropertyClass))]
        [JsonSerializable(typeof(NotNullableReadonlyPropertyClass))]
        [JsonSerializable(typeof(NotNullableFieldClass))]
        [JsonSerializable(typeof(NotNullableSpecialTypePropertiesClass))]
        [JsonSerializable(typeof(NotNullablePropertyWithHandleNullConverterClass))]
        [JsonSerializable(typeof(NotNullablePropertyWithConverterClass))]
        [JsonSerializable(typeof(NotNullablePropertyWithAlwaysNullConverterClass))]
        [JsonSerializable(typeof(NotNullablePropertyParameterizedCtorClass))]
        [JsonSerializable(typeof(NotNullablePropertyWithHandleNullConverterParameterizedCtorClass))]
        [JsonSerializable(typeof(NotNullablePropertyWithAlwaysNullConverterParameterizedCtorClass))]
        [JsonSerializable(typeof(NotNullablePropertiesLargeParameterizedCtorClass))]
        [JsonSerializable(typeof(NotNullablePropertyWithHandleNullConverterLargeParameterizedCtorClass))]
        [JsonSerializable(typeof(NotNullablePropertyWithAlwaysNullConverterLargeParameterizedCtorClass))]
        [JsonSerializable(typeof(NotNullPropertyClass))]
        [JsonSerializable(typeof(MaybeNullPropertyClass))]
        [JsonSerializable(typeof(AllowNullPropertyClass))]
        [JsonSerializable(typeof(DisallowNullPropertyClass))]
        [JsonSerializable(typeof(AllowNullConstructorParameter))]
        [JsonSerializable(typeof(DisallowNullConstructorParameter))]
        [JsonSerializable(typeof(NullStructPropertyClass))]
        [JsonSerializable(typeof(NullStructConstructorParameterClass))]
        [JsonSerializable(typeof(NotNullStructPropertyClass))]
        [JsonSerializable(typeof(DisallowNullStructPropertyClass))]
        [JsonSerializable(typeof(DisallowNullStructConstructorParameter))]
        [JsonSerializable(typeof(NotNullPropertyClass<string>))]
        [JsonSerializable(typeof(MaybeNullPropertyClass<string>))]
        [JsonSerializable(typeof(AllowNullPropertyClass<string>))]
        [JsonSerializable(typeof(DisallowNullPropertyClass<string>))]
        [JsonSerializable(typeof(AllowNullConstructorParameter<string>))]
        [JsonSerializable(typeof(DisallowNullConstructorParameter<string>))]
        [JsonSerializable(typeof(NullablePropertyClass))]
        [JsonSerializable(typeof(NullableFieldClass))]
        [JsonSerializable(typeof(NullableObliviousPropertyClass))]
        [JsonSerializable(typeof(NullableObliviousConstructorParameter))]
        [JsonSerializable(typeof(GenericPropertyClass<string>))]
        [JsonSerializable(typeof(NullableGenericPropertyClass<string>))]
        [JsonSerializable(typeof(NotNullGenericPropertyClass<string>))]
        [JsonSerializable(typeof(GenericConstructorParameter<string>))]
        [JsonSerializable(typeof(NullableGenericConstructorParameter<string>))]
        [JsonSerializable(typeof(NotNullGenericConstructorParameter<string>))]
        [JsonSerializable(typeof(NotNullablePropertyWithIgnoreConditions))]
        internal sealed partial class NullableAnnotationsTestsContext_Default
            : JsonSerializerContext
        { }
    }
}

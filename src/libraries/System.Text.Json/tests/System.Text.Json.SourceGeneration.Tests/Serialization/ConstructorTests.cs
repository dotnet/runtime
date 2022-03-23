// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;

namespace System.Text.Json.SourceGeneration.Tests
{
    public sealed class ConstructorTests_Metadata_String : ConstructorTests_Metadata
    {
        public ConstructorTests_Metadata_String()
            : base(new StringSerializerWrapper(ConstructorTestsContext_Metadata.Default, (options) => new ConstructorTestsContext_Metadata(options)))
        {
        }
    }

    public sealed class ConstructorTests_Metadata_AsyncStream : ConstructorTests_Metadata
    {
        public ConstructorTests_Metadata_AsyncStream()
            : base(new AsyncStreamSerializerWrapper(ConstructorTestsContext_Metadata.Default, (options) => new ConstructorTestsContext_Metadata(options)))
        {
        }
    }

    public abstract partial class ConstructorTests_Metadata : ConstructorTests
    {
        protected ConstructorTests_Metadata(JsonSerializerWrapper stringWrapper)
            : base(stringWrapper)
        {
        }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
        [JsonSerializable(typeof(PrivateParameterlessCtor))]
        [JsonSerializable(typeof(InternalParameterlessCtor))]
        [JsonSerializable(typeof(ProtectedParameterlessCtor))]
        [JsonSerializable(typeof(PrivateParameterizedCtor))]
        [JsonSerializable(typeof(InternalParameterizedCtor))]
        [JsonSerializable(typeof(ProtectedParameterizedCtor))]
        [JsonSerializable(typeof(PrivateParameterizedCtor_WithAttribute))]
        [JsonSerializable(typeof(InternalParameterizedCtor_WithAttribute))]
        [JsonSerializable(typeof(ProtectedParameterizedCtor_WithAttribute))]
        [JsonSerializable(typeof(SinglePublicParameterizedCtor))]
        [JsonSerializable(typeof(SingleParameterlessCtor_MultiplePublicParameterizedCtor))]
        [JsonSerializable(typeof(SingleParameterlessCtor_MultiplePublicParameterizedCtor_Struct))]
        [JsonSerializable(typeof(PublicParameterizedCtor))]
        [JsonSerializable(typeof(PrivateParameterlessConstructor_PublicParameterizedCtor))]
        [JsonSerializable(typeof(PublicParameterizedCtor_WithAttribute))]
        [JsonSerializable(typeof(Struct_PublicParameterizedConstructor_WithAttribute))]
        [JsonSerializable(typeof(PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute))]
        [JsonSerializable(typeof(MultiplePublicParameterizedCtor))]
        [JsonSerializable(typeof(MultiplePublicParameterizedCtor_Struct))]
        [JsonSerializable(typeof(MultiplePublicParameterizedCtor_WithAttribute))]
        [JsonSerializable(typeof(MultiplePublicParameterizedCtor_WithAttribute_Struct))]
        [JsonSerializable(typeof(ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute))]
        [JsonSerializable(typeof(Parameterized_StackWrapper))]
        [JsonSerializable(typeof(Parameterized_WrapperForICollection))]
        [JsonSerializable(typeof(Point_2D_Struct))]
        [JsonSerializable(typeof(Point_2D_Struct_WithAttribute))]
        [JsonSerializable(typeof(ObjWCtorMixedParams))]
        [JsonSerializable(typeof(Person_Class))]
        [JsonSerializable(typeof(Point_2D))]
        [JsonSerializable(typeof(Point_MultipleMembers_BindTo_OneConstructorParameter))]
        [JsonSerializable(typeof(Point_MultipleMembers_BindTo_OneConstructorParameter_Variant))]
        [JsonSerializable(typeof(Url_BindTo_OneConstructorParameter))]
        [JsonSerializable(typeof(Point_Without_Members))]
        [JsonSerializable(typeof(Point_With_MismatchedMembers))]
        [JsonSerializable(typeof(WrapperFor_Point_With_MismatchedMembers))]
        [JsonSerializable(typeof(Employee))]
        [JsonSerializable(typeof(Class_ExtData_CtorParam))]
        [JsonSerializable(typeof(ClassWithUnicodePropertyName))]
        [JsonSerializable(typeof(RootClass))]
        [JsonSerializable(typeof(Parameterized_ClassWithUnicodeProperty))]
        [JsonSerializable(typeof(Parameterized_ClassWithExtensionProperty))]
        [JsonSerializable(typeof(Point_3D))]
        [JsonSerializable(typeof(Point_2D_With_ExtData))]
        [JsonSerializable(typeof(List<Point_3D>))]
        [JsonSerializable(typeof(Dictionary<string, Point_3D>))]
        [JsonSerializable(typeof(WrapperForPoint_3D))]
        [JsonSerializable(typeof(ClassWrapperForPoint_3D))]
        [JsonSerializable(typeof(ClassWrapper_For_Int_String))]
        [JsonSerializable(typeof(ClassWrapper_For_Int_Point_3D_String))]
        [JsonSerializable(typeof(Person_Class))]
        [JsonSerializable(typeof(Person_Struct))]
        [JsonSerializable(typeof(Point_CtorsIgnoreJson))]
        [JsonSerializable(typeof(NullArgTester))]
        [JsonSerializable(typeof(NullArgTester_Mutable))]
        [JsonSerializable(typeof(Parameterless_ClassWithPrimitives))]
        [JsonSerializable(typeof(Parameterized_ClassWithPrimitives_3Args))]
        [JsonSerializable(typeof(Tuple<string, double>))]
        [JsonSerializable(typeof(TupleWrapper))]
        [JsonSerializable(typeof(List<Tuple<string, double>>))]
        [JsonSerializable(typeof(Tuple<int, int, int, int, int, int, int>))]
        [JsonSerializable(typeof(Tuple<int, int, int, int, int, int, int, int>))]
        [JsonSerializable(typeof(Tuple<int, string, int, string, string, int, Point_3D_Struct>))]
        [JsonSerializable(typeof(Tuple<int, string, int, string, string, int, Point_3D_Struct, int>))]
        [JsonSerializable(typeof(Point_3D[]))]
        [JsonSerializable(typeof(Struct_With_Ctor_With_64_Params))]
        [JsonSerializable(typeof(Class_With_Ctor_With_64_Params))]
        [JsonSerializable(typeof(Class_With_Ctor_With_65_Params))]
        [JsonSerializable(typeof(Struct_With_Ctor_With_65_Params))]
        [JsonSerializable(typeof(Parameterized_Person))]
        [JsonSerializable(typeof(BitVector32))]
        [JsonSerializable(typeof(SimpleClassWithParameterizedCtor_GenericDictionary_JsonElementExt))]
        [JsonSerializable(typeof(SimpleClassWithParameterizedCtor_GenericDictionary_ObjectExt))]
        [JsonSerializable(typeof(SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_JsonElementExt))]
        [JsonSerializable(typeof(SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_ObjectExt))]
        [JsonSerializable(typeof(Point_MembersHave_JsonInclude))]
        [JsonSerializable(typeof(ClassWithFiveArgs_MembersHave_JsonNumberHandlingAttributes))]
        [JsonSerializable(typeof(Point_MembersHave_JsonPropertyName))]
        [JsonSerializable(typeof(Point_MembersHave_JsonConverter))]
        [JsonSerializable(typeof(Point_MembersHave_JsonIgnore))]
        [JsonSerializable(typeof(Point_ExtendedPropNames))]
        [JsonSerializable(typeof(Point_With_Array))]
        [JsonSerializable(typeof(Point_With_Dictionary))]
        [JsonSerializable(typeof(Point_With_Object))]
        [JsonSerializable(typeof(Point_With_Property))]
        [JsonSerializable(typeof(ClassWithNestedClass))]
        [JsonSerializable(typeof(StructWithFourArgs))]
        [JsonSerializable(typeof(TypeWithGuid))]
        [JsonSerializable(typeof(TypeWithNullableGuid))]
        [JsonSerializable(typeof(TypeWithUri))]
        [JsonSerializable(typeof(Parameterized_IndexViewModel_Immutable))]
        [JsonSerializable(typeof(Parameterized_Person_ObjExtData))]
        [JsonSerializable(typeof(ClassWithStrings))]
        [JsonSerializable(typeof(Point_3D_Struct))]
        [JsonSerializable(typeof(Tuple<SimpleTestClass, SimpleTestClass, SimpleTestClass, SimpleTestClass, SimpleTestClass, SimpleTestClass, SimpleTestClass>))]
        [JsonSerializable(typeof(Employee))]
        [JsonSerializable(typeof(AgePoco))]
        [JsonSerializable(typeof(MyRecordWithUnboundCtorProperty))]
        [JsonSerializable(typeof(MyRecord))]
        [JsonSerializable(typeof(AgeRecord))]
        [JsonSerializable(typeof(JsonElement))]
        [JsonSerializable(typeof(Parameterized_Class_With_ComplexTuple))]
        [JsonSerializable(typeof(Parameterized_Person_Simple))]
        [JsonSerializable(typeof(SmallType_IgnoredProp_Bind_ParamWithDefaultValue))]
        [JsonSerializable(typeof(SmallType_IgnoredProp_Bind_Param))]
        [JsonSerializable(typeof(LargeType_IgnoredProp_Bind_ParamWithDefaultValue))]
        [JsonSerializable(typeof(LargeType_IgnoredProp_Bind_Param))]
        [JsonSerializable(typeof(ClassWithIgnoredSameType))]
        [JsonSerializable(typeof(ClassWithDefaultCtorParams))]
        internal sealed partial class ConstructorTestsContext_Metadata : JsonSerializerContext
        {
        }
    }

    public sealed class ConstructorTests_Default_String : ConstructorTests_Default
    {
        public ConstructorTests_Default_String()
            : base(new StringSerializerWrapper(ConstructorTestsContext_Default.Default, (options) => new ConstructorTestsContext_Default(options)))
        {
        }
    }

    public sealed class ConstructorTests_Default_AsyncStream : ConstructorTests_Default
    {
        public ConstructorTests_Default_AsyncStream()
            : base(new AsyncStreamSerializerWrapper(ConstructorTestsContext_Default.Default, (options) => new ConstructorTestsContext_Default(options)))
        {
        }
    }

    public abstract partial class ConstructorTests_Default : ConstructorTests_Metadata
    {
        public ConstructorTests_Default(JsonSerializerWrapper jsonSerializer) : base(jsonSerializer)
        {
        }

        [JsonSerializable(typeof(PrivateParameterlessCtor))]
        [JsonSerializable(typeof(InternalParameterlessCtor))]
        [JsonSerializable(typeof(ProtectedParameterlessCtor))]
        [JsonSerializable(typeof(PrivateParameterizedCtor))]
        [JsonSerializable(typeof(InternalParameterizedCtor))]
        [JsonSerializable(typeof(ProtectedParameterizedCtor))]
        [JsonSerializable(typeof(PrivateParameterizedCtor_WithAttribute))]
        [JsonSerializable(typeof(InternalParameterizedCtor_WithAttribute))]
        [JsonSerializable(typeof(ProtectedParameterizedCtor_WithAttribute))]
        [JsonSerializable(typeof(SinglePublicParameterizedCtor))]
        [JsonSerializable(typeof(SingleParameterlessCtor_MultiplePublicParameterizedCtor))]
        [JsonSerializable(typeof(SingleParameterlessCtor_MultiplePublicParameterizedCtor_Struct))]
        [JsonSerializable(typeof(PublicParameterizedCtor))]
        [JsonSerializable(typeof(PrivateParameterlessConstructor_PublicParameterizedCtor))]
        [JsonSerializable(typeof(PublicParameterizedCtor_WithAttribute))]
        [JsonSerializable(typeof(Struct_PublicParameterizedConstructor_WithAttribute))]
        [JsonSerializable(typeof(PrivateParameterlessConstructor_PublicParameterizedCtor_WithAttribute))]
        [JsonSerializable(typeof(MultiplePublicParameterizedCtor))]
        [JsonSerializable(typeof(MultiplePublicParameterizedCtor_Struct))]
        [JsonSerializable(typeof(MultiplePublicParameterizedCtor_WithAttribute))]
        [JsonSerializable(typeof(MultiplePublicParameterizedCtor_WithAttribute_Struct))]
        [JsonSerializable(typeof(ParameterlessCtor_MultiplePublicParameterizedCtor_WithAttribute))]
        [JsonSerializable(typeof(Parameterized_StackWrapper))]
        [JsonSerializable(typeof(Parameterized_WrapperForICollection))]
        [JsonSerializable(typeof(Point_2D_Struct))]
        [JsonSerializable(typeof(Point_2D_Struct_WithAttribute))]
        [JsonSerializable(typeof(ObjWCtorMixedParams))]
        [JsonSerializable(typeof(Person_Class))]
        [JsonSerializable(typeof(Point_2D))]
        [JsonSerializable(typeof(Point_MultipleMembers_BindTo_OneConstructorParameter))]
        [JsonSerializable(typeof(Point_MultipleMembers_BindTo_OneConstructorParameter_Variant))]
        [JsonSerializable(typeof(Url_BindTo_OneConstructorParameter))]
        [JsonSerializable(typeof(Point_Without_Members))]
        [JsonSerializable(typeof(Point_With_MismatchedMembers))]
        [JsonSerializable(typeof(WrapperFor_Point_With_MismatchedMembers))]
        [JsonSerializable(typeof(Employee))]
        [JsonSerializable(typeof(Class_ExtData_CtorParam))]
        [JsonSerializable(typeof(ClassWithUnicodePropertyName))]
        [JsonSerializable(typeof(RootClass))]
        [JsonSerializable(typeof(Parameterized_ClassWithUnicodeProperty))]
        [JsonSerializable(typeof(Parameterized_ClassWithExtensionProperty))]
        [JsonSerializable(typeof(Point_3D))]
        [JsonSerializable(typeof(Point_2D_With_ExtData))]
        [JsonSerializable(typeof(List<Point_3D>))]
        [JsonSerializable(typeof(Dictionary<string, Point_3D>))]
        [JsonSerializable(typeof(WrapperForPoint_3D))]
        [JsonSerializable(typeof(ClassWrapperForPoint_3D))]
        [JsonSerializable(typeof(ClassWrapper_For_Int_String))]
        [JsonSerializable(typeof(ClassWrapper_For_Int_Point_3D_String))]
        [JsonSerializable(typeof(Person_Class))]
        [JsonSerializable(typeof(Person_Struct))]
        [JsonSerializable(typeof(Point_CtorsIgnoreJson))]
        [JsonSerializable(typeof(NullArgTester))]
        [JsonSerializable(typeof(NullArgTester_Mutable))]
        [JsonSerializable(typeof(Parameterless_ClassWithPrimitives))]
        [JsonSerializable(typeof(Parameterized_ClassWithPrimitives_3Args))]
        [JsonSerializable(typeof(Tuple<string, double>))]
        [JsonSerializable(typeof(TupleWrapper))]
        [JsonSerializable(typeof(List<Tuple<string, double>>))]
        [JsonSerializable(typeof(Tuple<int, int, int, int, int, int, int>))]
        [JsonSerializable(typeof(Tuple<int, int, int, int, int, int, int, int>))]
        [JsonSerializable(typeof(Tuple<int, string, int, string, string, int, Point_3D_Struct>))]
        [JsonSerializable(typeof(Tuple<int, string, int, string, string, int, Point_3D_Struct, int>))]
        [JsonSerializable(typeof(Point_3D[]))]
        [JsonSerializable(typeof(Struct_With_Ctor_With_64_Params))]
        [JsonSerializable(typeof(Class_With_Ctor_With_64_Params))]
        [JsonSerializable(typeof(Class_With_Ctor_With_65_Params))]
        [JsonSerializable(typeof(Struct_With_Ctor_With_65_Params))]
        [JsonSerializable(typeof(Parameterized_Person))]
        [JsonSerializable(typeof(BitVector32))]
        [JsonSerializable(typeof(SimpleClassWithParameterizedCtor_GenericDictionary_JsonElementExt))]
        [JsonSerializable(typeof(SimpleClassWithParameterizedCtor_GenericDictionary_ObjectExt))]
        [JsonSerializable(typeof(SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_JsonElementExt))]
        [JsonSerializable(typeof(SimpleClassWithParameterizedCtor_Derived_GenericIDictionary_ObjectExt))]
        [JsonSerializable(typeof(Point_MembersHave_JsonInclude))]
        [JsonSerializable(typeof(ClassWithFiveArgs_MembersHave_JsonNumberHandlingAttributes))]
        [JsonSerializable(typeof(Point_MembersHave_JsonPropertyName))]
        [JsonSerializable(typeof(Point_MembersHave_JsonConverter))]
        [JsonSerializable(typeof(Point_MembersHave_JsonIgnore))]
        [JsonSerializable(typeof(Point_ExtendedPropNames))]
        [JsonSerializable(typeof(Point_With_Array))]
        [JsonSerializable(typeof(Point_With_Dictionary))]
        [JsonSerializable(typeof(Point_With_Object))]
        [JsonSerializable(typeof(Point_With_Property))]
        [JsonSerializable(typeof(ClassWithNestedClass))]
        [JsonSerializable(typeof(StructWithFourArgs))]
        [JsonSerializable(typeof(TypeWithGuid))]
        [JsonSerializable(typeof(TypeWithNullableGuid))]
        [JsonSerializable(typeof(TypeWithUri))]
        [JsonSerializable(typeof(Parameterized_IndexViewModel_Immutable))]
        [JsonSerializable(typeof(Parameterized_Person_ObjExtData))]
        [JsonSerializable(typeof(ClassWithStrings))]
        [JsonSerializable(typeof(Point_3D_Struct))]
        [JsonSerializable(typeof(Tuple<SimpleTestClass, SimpleTestClass, SimpleTestClass, SimpleTestClass, SimpleTestClass, SimpleTestClass, SimpleTestClass>))]
        [JsonSerializable(typeof(Employee))]
        [JsonSerializable(typeof(AgePoco))]
        [JsonSerializable(typeof(MyRecordWithUnboundCtorProperty))]
        [JsonSerializable(typeof(MyRecord))]
        [JsonSerializable(typeof(AgeRecord))]
        [JsonSerializable(typeof(JsonElement))]
        [JsonSerializable(typeof(Parameterized_Class_With_ComplexTuple))]
        [JsonSerializable(typeof(Parameterized_Person_Simple))]
        [JsonSerializable(typeof(SmallType_IgnoredProp_Bind_ParamWithDefaultValue))]
        [JsonSerializable(typeof(SmallType_IgnoredProp_Bind_Param))]
        [JsonSerializable(typeof(LargeType_IgnoredProp_Bind_ParamWithDefaultValue))]
        [JsonSerializable(typeof(LargeType_IgnoredProp_Bind_Param))]
        [JsonSerializable(typeof(ClassWithIgnoredSameType))]
        [JsonSerializable(typeof(ClassWithDefaultCtorParams))]
        internal sealed partial class ConstructorTestsContext_Default : JsonSerializerContext
        {
        }
    }
}

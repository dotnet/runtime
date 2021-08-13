// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public partial class PropertyVisibilityTests_Metadata : PropertyVisibilityTests
    {
        public PropertyVisibilityTests_Metadata()
            : this(new StringSerializerWrapper(PropertyVisibilityTestsContext_Metadata.Default, (options) => new PropertyVisibilityTestsContext_Metadata(options)))
        {
        }

        protected PropertyVisibilityTests_Metadata(Serialization.Tests.JsonSerializerWrapperForString serializerWrapper)
            : base(serializerWrapper)
        {
        }

        [Theory]
        [InlineData(typeof(ClassWithBadIgnoreAttribute))]
        [InlineData(typeof(StructWithBadIgnoreAttribute))]
        public override async Task JsonIgnoreCondition_WhenWritingNull_OnValueType_Fail_EmptyJson(Type type)
        {
            await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper("", type));

            InvalidOperationException ioe = await Assert.ThrowsAsync<InvalidOperationException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(Activator.CreateInstance(type), type));
            string exAsStr = ioe.ToString();
            Assert.Contains("JsonIgnoreCondition.WhenWritingNull", exAsStr);
            Assert.Contains("MyBadMember", exAsStr);
            Assert.Contains(type.ToString(), exAsStr);
            Assert.Contains("JsonIgnoreCondition.WhenWritingDefault", exAsStr);
        }

        [Fact]
        public override async Task Honor_JsonSerializablePropertyAttribute_OnProperties()
        {
            string json = @"{
                ""MyInt"":1,
                ""MyString"":""Hello"",
                ""MyFloat"":2,
                ""MyUri"":""https://microsoft.com""
            }";

            var obj = await JsonSerializerWrapperForString.DeserializeWrapper<MyClass_WithNonPublicAccessors_WithPropertyAttributes>(json);
            Assert.Equal(0, obj.MyInt); // Source gen can't use private setter
            Assert.Equal("Hello", obj.MyString);
            Assert.Equal(2f, obj.GetMyFloat);
            Assert.Equal(new Uri("https://microsoft.com"), obj.MyUri);

            json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            Assert.Contains(@"""MyInt"":0", json);
            Assert.Contains(@"""MyString"":""Hello""", json);
            Assert.DoesNotContain(@"""MyFloat"":", json); // Source gen can't use private setter
            Assert.Contains(@"""MyUri"":""https://microsoft.com""", json);
        }

        [Theory]
        [InlineData(typeof(ClassWithInitOnlyProperty))]
        [InlineData(typeof(StructWithInitOnlyProperty))]
        public override async Task InitOnlyProperties(Type type)
        {
            // Init-only setters cannot be referenced as get/set helpers in generated code.
            object obj = await JsonSerializerWrapperForString.DeserializeWrapper(@"{""MyInt"":1}", type);
            Assert.Equal(0, (int)type.GetProperty("MyInt").GetValue(obj));

            // Init-only properties can be serialized.
            Assert.Equal(@"{""MyInt"":0}", await JsonSerializerWrapperForString.SerializeWrapper(obj, type));
        }

        [Theory]
        [InlineData(typeof(Class_PropertyWith_PrivateInitOnlySetter_WithAttribute))]
        [InlineData(typeof(Class_PropertyWith_InternalInitOnlySetter_WithAttribute))]
        [InlineData(typeof(Class_PropertyWith_ProtectedInitOnlySetter_WithAttribute))]
        public override async Task NonPublicInitOnlySetter_With_JsonInclude(Type type)
        {
            // Init-only setters cannot be referenced as get/set helpers in generated code.
            object obj = await JsonSerializerWrapperForString.DeserializeWrapper(@"{""MyInt"":1}", type);
            Assert.Equal(0, (int)type.GetProperty("MyInt").GetValue(obj));

            // Init-only properties can be serialized.
            Assert.Equal(@"{""MyInt"":0}", await JsonSerializerWrapperForString.SerializeWrapper(obj, type));
        }

        [Fact]
        public override async Task HonorCustomConverter_UsingPrivateSetter()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());

            string json = @"{""MyEnum"":""AnotherValue"",""MyInt"":2}";

            // Deserialization baseline, without enum converter, we get JsonException.
            await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StructWithPropertiesWithConverter>(json));

            var obj = await JsonSerializerWrapperForString.DeserializeWrapper<StructWithPropertiesWithConverter>(json, options);
            Assert.Equal(MySmallEnum.AnotherValue, obj.GetMyEnum);
            Assert.Equal(0, obj.MyInt); // Private setter can't be used with source-gen.

            // ConverterForInt32 throws this exception.
            await Assert.ThrowsAsync<NotImplementedException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(obj, options));
        }

        [Fact]
        public override async Task Public_And_NonPublicPropertyAccessors_PropertyAttributes()
        {
            string json = @"{""W"":1,""X"":2,""Y"":3,""Z"":4}";

            var obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithMixedPropertyAccessors_PropertyAttributes>(json);
            Assert.Equal(1, obj.W);
            Assert.Equal(2, obj.X);
            Assert.Equal(3, obj.Y);
            Assert.Equal(4, obj.GetZ);

            json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            Assert.Contains(@"""W"":1", json);
            Assert.Contains(@"""X"":2", json);
            Assert.Contains(@"""Y"":3", json);
            Assert.DoesNotContain(@"""Z"":", json); // Private setter cannot be used with source gen.
        }

        [Fact]
        public override async Task HonorJsonPropertyName()
        {
            string json = @"{""prop1"":1,""prop2"":2}";

            var obj = await JsonSerializerWrapperForString.DeserializeWrapper<StructWithPropertiesWithJsonPropertyName>(json);
            Assert.Equal(MySmallEnum.AnotherValue, obj.GetMyEnum);
            Assert.Equal(0, obj.MyInt); // Private setter cannot be used with source gen.

            json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            Assert.DoesNotContain(@"""prop1"":", json); // Private getter cannot be used with source gen.
            Assert.Contains(@"""prop2"":0", json);
        }

        [JsonSourceGenerationOptions(GenerationMode = JsonSourceGenerationMode.Metadata)]
        [JsonSerializable(typeof(ClassWithNewSlotField))]
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(object))]
        [JsonSerializable(typeof(ClassWithInternalField))]
        [JsonSerializable(typeof(ClassWithNewSlotDecimalField))]
        [JsonSerializable(typeof(ClassWithNewSlotAttributedDecimalField))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyPolicyConflictPrivate))]
        [JsonSerializable(typeof(ClassWithMissingCollectionProperty))]
        [JsonSerializable(typeof(ClassWithClassProperty_IgnoreConditionWhenWritingDefault))]
        [JsonSerializable(typeof(ClassWithNoSetter))]
        [JsonSerializable(typeof(ClassWithInternalProperty))]
        [JsonSerializable(typeof(ClassWithPropertyNamingConflict))]
        [JsonSerializable(typeof(ClassWithStructProperty_IgnoreConditionWhenWritingDefault))]
        [JsonSerializable(typeof(ClassWithMissingObjectProperty))]
        [JsonSerializable(typeof(ClassWithInitOnlyProperty))]
        [JsonSerializable(typeof(StructWithInitOnlyProperty))]
        [JsonSerializable(typeof(MyClassWithValueTypeInterfaceProperty))]
        [JsonSerializable(typeof(ClassWithNonPublicProperties))]
        [JsonSerializable(typeof(ClassWithProperty_IgnoreConditionAlways))]
        [JsonSerializable(typeof(ClassWithBadIgnoreAttribute))]
        [JsonSerializable(typeof(StructWithBadIgnoreAttribute))]
        [JsonSerializable(typeof(Class_PropertyWith_InternalInitOnlySetter))]
        [JsonSerializable(typeof(Class_PropertyWith_ProtectedInitOnlySetter))]
        [JsonSerializable(typeof(ClassWithIgnoredPublicPropertyAndNewSlotPrivate))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyPolicyConflictPublic))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyNamingConflictPrivate))]
        [JsonSerializable(typeof(ClassWithIgnoredNewSlotProperty))]
        [JsonSerializable(typeof(ClassWithPublicGetterAndPrivateSetter))]
        [JsonSerializable(typeof(ClassWithInitializedProps))]
        [JsonSerializable(typeof(ClassWithNewSlotInternalProperty))]
        [JsonSerializable(typeof(ClassWithPropertyPolicyConflict))]
        [JsonSerializable(typeof(ClassWithPrivateSetterAndGetter))]
        [JsonSerializable(typeof(ClassWithIgnoreAttributeProperty))]
        [JsonSerializable(typeof(ClassWithIgnoredNewSlotField))]
        [JsonSerializable(typeof(MyStruct_WithNonPublicAccessors_WithTypeAttribute))]
        [JsonSerializable(typeof(ClassWithReadOnlyFields))]
        [JsonSerializable(typeof(MyValueTypeWithBoxedPrimitive))]
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(ClassWithNoGetter))]
        [JsonSerializable(typeof(ClassWithPropsAndIgnoreAttributes))]
        [JsonSerializable(typeof(List<bool>))]
        [JsonSerializable(typeof(MyValueTypeWithProperties))]
        [JsonSerializable(typeof(ClassInheritedWithPropertyPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(ClassInheritedWithPropertyFieldPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(ClassWithPropertyFieldPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(ClassWithOverrideReversed))]
        [JsonSerializable(typeof(ClassWithReadOnlyStringProperty))]
        [JsonSerializable(typeof(ClassWithReadOnlyStringProperty_IgnoreNever))]
        [JsonSerializable(typeof(ClassWithProps))]
        [JsonSerializable(typeof(ClassWithStructProperty_IgnoreConditionNever))]
        [JsonSerializable(typeof(ClassWithStructProperty_IgnoreConditionNever_Ctor))]
        [JsonSerializable(typeof(ClassWithPropertyFieldNamingConflictWhichThrows))]
        [JsonSerializable(typeof(ClassWithBadIgnoreAttribute))]
        [JsonSerializable(typeof(StructWithBadIgnoreAttribute))]
        [JsonSerializable(typeof(ClassWithPropertyNamingConflictWhichThrows))]
        [JsonSerializable(typeof(ClassWithReadOnlyStringField))]
        [JsonSerializable(typeof(ClassWithReadOnlyStringField_IgnoreWhenWritingDefault))]
        [JsonSerializable(typeof(ClassWithReadOnlyStringField_IgnoreNever))]
        [JsonSerializable(typeof(ClassInheritedWithPropertyFieldNamingConflictWhichThrows))]
        [JsonSerializable(typeof(ClassTwiceInheritedWithPropertyFieldNamingConflictWhichThrows))]
        [JsonSerializable(typeof(ClassWithPrivateProperty_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithInternalProperty_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithProtectedProperty_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithPrivateField_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithInternalField_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithProtectedField_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithPrivate_InitOnlyProperty_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithInternal_InitOnlyProperty_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithProtected_InitOnlyProperty_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithPublicProperty))]
        [JsonSerializable(typeof(ClassInheritedWithPropertyNamingConflictWhichThrows))]
        [JsonSerializable(typeof(StructWithOverride))]
        [JsonSerializable(typeof(ClassTwiceInheritedWithPropertyFieldPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(ClassTwiceInheritedWithPropertyNamingConflictWhichThrows))]
        [JsonSerializable(typeof(MyClass_WithNonPublicAccessors_WithPropertyAttributes))]
        [JsonSerializable(typeof(Class_PropertyWith_PrivateInitOnlySetter))]
        [JsonSerializable(typeof(Class_PropertyWith_InternalInitOnlySetter))]
        [JsonSerializable(typeof(Class_PropertyWith_ProtectedInitOnlySetter))]
        [JsonSerializable(typeof(Class_PropertyWith_PrivateInitOnlySetter_WithAttribute))]
        [JsonSerializable(typeof(Class_PropertyWith_InternalInitOnlySetter_WithAttribute))]
        [JsonSerializable(typeof(Class_PropertyWith_ProtectedInitOnlySetter_WithAttribute))]
        [JsonSerializable(typeof(DerivedClass_With_IgnoredOverride))]
        [JsonSerializable(typeof(DerivedClass_WithVisibleProperty_Of_DerivedClass_With_IgnoredOverride))]
        [JsonSerializable(typeof(DerivedClass_With_IgnoredOverride_And_ConflictingPropertyName))]
        [JsonSerializable(typeof(DerivedClass_With_Ignored_NewProperty))]
        [JsonSerializable(typeof(DerivedClass_WithConflictingNewMember))]
        [JsonSerializable(typeof(DerivedClass_WithConflictingNewMember_Of_DifferentType))]
        [JsonSerializable(typeof(DerivedClass_With_Ignored_ConflictingNewMember))]
        [JsonSerializable(typeof(DerivedClass_With_Ignored_ConflictingNewMember_Of_DifferentType))]
        [JsonSerializable(typeof(DerivedClass_With_NewProperty_And_ConflictingPropertyName))]
        [JsonSerializable(typeof(DerivedClass_With_Ignored_NewProperty_Of_DifferentType))]
        [JsonSerializable(typeof(DerivedClass_With_Ignored_NewProperty_Of_DifferentType_And_ConflictingPropertyName))]
        [JsonSerializable(typeof(FurtherDerivedClass_With_ConflictingPropertyName))]
        [JsonSerializable(typeof(DerivedClass_WithConflictingPropertyName))]
        [JsonSerializable(typeof(FurtherDerivedClass_With_IgnoredOverride))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyNamingConflictPublic))]
        [JsonSerializable(typeof(MyClassWithValueType))]
        [JsonSerializable(typeof(StructWithPropertiesWithConverter))]
        [JsonSerializable(typeof(ClassWithNewSlotProperty))]
        [JsonSerializable(typeof(ClassWithNewSlotAttributedDecimalProperty))]
        [JsonSerializable(typeof(ClassWithNewSlotDecimalProperty))]
        [JsonSerializable(typeof(LargeStructWithValueAndReferenceTypes))]
        [JsonSerializable(typeof(ClassWithUnsupportedBigInteger))]
        [JsonSerializable(typeof(WrapperForClassWithUnsupportedBigInteger))]
        [JsonSerializable(typeof(ClassWithIgnoredUnsupportedBigInteger))]
        [JsonSerializable(typeof(WrapperForClassWithIgnoredUnsupportedBigInteger))]
        [JsonSerializable(typeof(ClassWithThingsToIgnore))]
        [JsonSerializable(typeof(ClassWithMixedPropertyAccessors_PropertyAttributes))]
        [JsonSerializable(typeof(ClassWithPropertyPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(ClassTwiceInheritedWithPropertyPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(MyClass_WithNonPublicAccessors))]
        [JsonSerializable(typeof(ClassWithThingsToIgnore_PerProperty))]
        [JsonSerializable(typeof(StructWithPropertiesWithJsonPropertyName))]
        [JsonSerializable(typeof(ClassWithValueAndReferenceTypes))]
        [JsonSerializable(typeof(ClassWithReadOnlyStringProperty_IgnoreWhenWritingDefault))]
        internal sealed partial class PropertyVisibilityTestsContext_Metadata : JsonSerializerContext
        {
        }
    }

    public partial class PropertyVisibilityTests_Default : PropertyVisibilityTests_Metadata
    {
        public PropertyVisibilityTests_Default()
            : base(new StringSerializerWrapper(PropertyVisibilityTestsContext_Default.Default, (options) => new PropertyVisibilityTestsContext_Default(options)))
        {
        }

        [Theory]
        [InlineData(typeof(ClassWithPrivateProperty_WithJsonIncludeProperty))]
        [InlineData(typeof(ClassWithInternalProperty_WithJsonIncludeProperty))]
        [InlineData(typeof(ClassWithProtectedProperty_WithJsonIncludeProperty))]
        [InlineData(typeof(ClassWithPrivateField_WithJsonIncludeProperty))]
        [InlineData(typeof(ClassWithInternalField_WithJsonIncludeProperty))]
        [InlineData(typeof(ClassWithProtectedField_WithJsonIncludeProperty))]
        [InlineData(typeof(ClassWithPrivate_InitOnlyProperty_WithJsonIncludeProperty))]
        [InlineData(typeof(ClassWithInternal_InitOnlyProperty_WithJsonIncludeProperty))]
        [InlineData(typeof(ClassWithProtected_InitOnlyProperty_WithJsonIncludeProperty))]
        public override async Task NonPublicProperty_WithJsonInclude_Invalid(Type type)
        {
            // Exception messages direct users to use JsonSourceGenerationMode.Metadata to see a more detailed error.
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper("{}", type));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(Activator.CreateInstance(type), type));
        }

        [Theory]
        [InlineData(typeof(ClassWithBadIgnoreAttribute))]
        [InlineData(typeof(StructWithBadIgnoreAttribute))]
        public override async Task JsonIgnoreCondition_WhenWritingNull_OnValueType_Fail(Type type)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper("{}", type));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(Activator.CreateInstance(type), type));
        }

        [Theory]
        [InlineData(typeof(ClassWithBadIgnoreAttribute))]
        [InlineData(typeof(StructWithBadIgnoreAttribute))]
        public override async Task JsonIgnoreCondition_WhenWritingNull_OnValueType_Fail_EmptyJson(Type type)
        {
            // Since this code goes down fast-path, there's no warm up and we hit the reader exception about having no tokens.
            await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper("", type));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(Activator.CreateInstance(type), type));
        }

        [JsonSerializable(typeof(ClassWithNewSlotField))]
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(object))]
        [JsonSerializable(typeof(ClassWithInternalField))]
        [JsonSerializable(typeof(ClassWithNewSlotDecimalField))]
        [JsonSerializable(typeof(ClassWithNewSlotAttributedDecimalField))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyPolicyConflictPrivate))]
        [JsonSerializable(typeof(ClassWithMissingCollectionProperty))]
        [JsonSerializable(typeof(ClassWithClassProperty_IgnoreConditionWhenWritingDefault))]
        [JsonSerializable(typeof(ClassWithNoSetter))]
        [JsonSerializable(typeof(ClassWithInternalProperty))]
        [JsonSerializable(typeof(ClassWithPropertyNamingConflict))]
        [JsonSerializable(typeof(ClassWithStructProperty_IgnoreConditionWhenWritingDefault))]
        [JsonSerializable(typeof(ClassWithMissingObjectProperty))]
        [JsonSerializable(typeof(ClassWithInitOnlyProperty))]
        [JsonSerializable(typeof(StructWithInitOnlyProperty))]
        [JsonSerializable(typeof(MyClassWithValueTypeInterfaceProperty))]
        [JsonSerializable(typeof(ClassWithNonPublicProperties))]
        [JsonSerializable(typeof(ClassWithProperty_IgnoreConditionAlways))]
        [JsonSerializable(typeof(ClassWithBadIgnoreAttribute))]
        [JsonSerializable(typeof(StructWithBadIgnoreAttribute))]
        [JsonSerializable(typeof(Class_PropertyWith_InternalInitOnlySetter))]
        [JsonSerializable(typeof(Class_PropertyWith_ProtectedInitOnlySetter))]
        [JsonSerializable(typeof(ClassWithIgnoredPublicPropertyAndNewSlotPrivate))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyPolicyConflictPublic))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyNamingConflictPrivate))]
        [JsonSerializable(typeof(ClassWithIgnoredNewSlotProperty))]
        [JsonSerializable(typeof(ClassWithPublicGetterAndPrivateSetter))]
        [JsonSerializable(typeof(ClassWithInitializedProps))]
        [JsonSerializable(typeof(ClassWithNewSlotInternalProperty))]
        [JsonSerializable(typeof(ClassWithPropertyPolicyConflict))]
        [JsonSerializable(typeof(ClassWithPrivateSetterAndGetter))]
        [JsonSerializable(typeof(ClassWithIgnoreAttributeProperty))]
        [JsonSerializable(typeof(ClassWithIgnoredNewSlotField))]
        [JsonSerializable(typeof(MyStruct_WithNonPublicAccessors_WithTypeAttribute))]
        [JsonSerializable(typeof(ClassWithReadOnlyFields))]
        [JsonSerializable(typeof(MyValueTypeWithBoxedPrimitive))]
        [JsonSerializable(typeof(int))]
        [JsonSerializable(typeof(ClassWithNoGetter))]
        [JsonSerializable(typeof(ClassWithPropsAndIgnoreAttributes))]
        [JsonSerializable(typeof(List<bool>))]
        [JsonSerializable(typeof(MyValueTypeWithProperties))]
        [JsonSerializable(typeof(ClassInheritedWithPropertyPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(ClassInheritedWithPropertyFieldPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(ClassWithPropertyFieldPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(ClassWithOverrideReversed))]
        [JsonSerializable(typeof(ClassWithReadOnlyStringProperty))]
        [JsonSerializable(typeof(ClassWithReadOnlyStringProperty_IgnoreNever))]
        [JsonSerializable(typeof(ClassWithProps))]
        [JsonSerializable(typeof(ClassWithStructProperty_IgnoreConditionNever))]
        [JsonSerializable(typeof(ClassWithStructProperty_IgnoreConditionNever_Ctor))]
        [JsonSerializable(typeof(ClassWithPropertyFieldNamingConflictWhichThrows))]
        [JsonSerializable(typeof(ClassWithBadIgnoreAttribute))]
        [JsonSerializable(typeof(StructWithBadIgnoreAttribute))]
        [JsonSerializable(typeof(ClassWithPropertyNamingConflictWhichThrows))]
        [JsonSerializable(typeof(ClassWithReadOnlyStringField))]
        [JsonSerializable(typeof(ClassWithReadOnlyStringField_IgnoreWhenWritingDefault))]
        [JsonSerializable(typeof(ClassWithReadOnlyStringField_IgnoreNever))]
        [JsonSerializable(typeof(ClassInheritedWithPropertyFieldNamingConflictWhichThrows))]
        [JsonSerializable(typeof(ClassTwiceInheritedWithPropertyFieldNamingConflictWhichThrows))]
        [JsonSerializable(typeof(ClassWithPrivateProperty_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithInternalProperty_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithProtectedProperty_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithPrivateField_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithInternalField_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithProtectedField_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithPrivate_InitOnlyProperty_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithInternal_InitOnlyProperty_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithProtected_InitOnlyProperty_WithJsonIncludeProperty))]
        [JsonSerializable(typeof(ClassWithPublicProperty))]
        [JsonSerializable(typeof(ClassInheritedWithPropertyNamingConflictWhichThrows))]
        [JsonSerializable(typeof(StructWithOverride))]
        [JsonSerializable(typeof(ClassTwiceInheritedWithPropertyFieldPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(ClassTwiceInheritedWithPropertyNamingConflictWhichThrows))]
        [JsonSerializable(typeof(MyClass_WithNonPublicAccessors_WithPropertyAttributes))]
        [JsonSerializable(typeof(Class_PropertyWith_PrivateInitOnlySetter))]
        [JsonSerializable(typeof(Class_PropertyWith_InternalInitOnlySetter))]
        [JsonSerializable(typeof(Class_PropertyWith_ProtectedInitOnlySetter))]
        [JsonSerializable(typeof(Class_PropertyWith_PrivateInitOnlySetter_WithAttribute))]
        [JsonSerializable(typeof(Class_PropertyWith_InternalInitOnlySetter_WithAttribute))]
        [JsonSerializable(typeof(Class_PropertyWith_ProtectedInitOnlySetter_WithAttribute))]
        [JsonSerializable(typeof(DerivedClass_With_IgnoredOverride))]
        [JsonSerializable(typeof(DerivedClass_WithVisibleProperty_Of_DerivedClass_With_IgnoredOverride))]
        [JsonSerializable(typeof(DerivedClass_With_IgnoredOverride_And_ConflictingPropertyName))]
        [JsonSerializable(typeof(DerivedClass_With_Ignored_NewProperty))]
        [JsonSerializable(typeof(DerivedClass_WithConflictingNewMember))]
        [JsonSerializable(typeof(DerivedClass_WithConflictingNewMember_Of_DifferentType))]
        [JsonSerializable(typeof(DerivedClass_With_Ignored_ConflictingNewMember))]
        [JsonSerializable(typeof(DerivedClass_With_Ignored_ConflictingNewMember_Of_DifferentType))]
        [JsonSerializable(typeof(DerivedClass_With_NewProperty_And_ConflictingPropertyName))]
        [JsonSerializable(typeof(DerivedClass_With_Ignored_NewProperty_Of_DifferentType))]
        [JsonSerializable(typeof(DerivedClass_With_Ignored_NewProperty_Of_DifferentType_And_ConflictingPropertyName))]
        [JsonSerializable(typeof(FurtherDerivedClass_With_ConflictingPropertyName))]
        [JsonSerializable(typeof(DerivedClass_WithConflictingPropertyName))]
        [JsonSerializable(typeof(FurtherDerivedClass_With_IgnoredOverride))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyNamingConflictPublic))]
        [JsonSerializable(typeof(MyClassWithValueType))]
        [JsonSerializable(typeof(StructWithPropertiesWithConverter))]
        [JsonSerializable(typeof(ClassWithNewSlotProperty))]
        [JsonSerializable(typeof(ClassWithNewSlotAttributedDecimalProperty))]
        [JsonSerializable(typeof(ClassWithNewSlotDecimalProperty))]
        [JsonSerializable(typeof(LargeStructWithValueAndReferenceTypes))]
        [JsonSerializable(typeof(ClassWithUnsupportedBigInteger))]
        [JsonSerializable(typeof(WrapperForClassWithUnsupportedBigInteger))]
        [JsonSerializable(typeof(ClassWithIgnoredUnsupportedBigInteger))]
        [JsonSerializable(typeof(WrapperForClassWithIgnoredUnsupportedBigInteger))]
        [JsonSerializable(typeof(ClassWithThingsToIgnore))]
        [JsonSerializable(typeof(ClassWithMixedPropertyAccessors_PropertyAttributes))]
        [JsonSerializable(typeof(ClassWithPropertyPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(ClassTwiceInheritedWithPropertyPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(MyClass_WithNonPublicAccessors))]
        [JsonSerializable(typeof(ClassWithThingsToIgnore_PerProperty))]
        [JsonSerializable(typeof(StructWithPropertiesWithJsonPropertyName))]
        [JsonSerializable(typeof(ClassWithValueAndReferenceTypes))]
        [JsonSerializable(typeof(ClassWithReadOnlyStringProperty_IgnoreWhenWritingDefault))]
        internal sealed partial class PropertyVisibilityTestsContext_Default : JsonSerializerContext
        {
        }
    }
}

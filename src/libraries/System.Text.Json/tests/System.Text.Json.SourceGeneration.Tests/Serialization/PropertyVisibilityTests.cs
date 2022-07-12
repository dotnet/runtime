// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
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

        protected PropertyVisibilityTests_Metadata(Serialization.Tests.JsonSerializerWrapper serializerWrapper)
            : base(serializerWrapper)
        {
        }

        [Theory]
        [InlineData(typeof(ClassWithBadIgnoreAttribute))]
        [InlineData(typeof(StructWithBadIgnoreAttribute))]
        public override async Task JsonIgnoreCondition_WhenWritingNull_OnValueType_Fail_EmptyJson(Type type)
        {
            InvalidOperationException ioe = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper("", type));
            ValidateInvalidOperationException();

            ioe = await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(Activator.CreateInstance(type), type));
            ValidateInvalidOperationException();

            void ValidateInvalidOperationException()
            {
                string exAsStr = ioe.ToString();
                Assert.Contains("JsonIgnoreCondition.WhenWritingNull", exAsStr);
                Assert.Contains("MyBadMember", exAsStr);
                Assert.Contains(type.ToString(), exAsStr);
                Assert.Contains("JsonIgnoreCondition.WhenWritingDefault", exAsStr);
            }
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

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<MyClass_WithNonPublicAccessors_WithPropertyAttributes>(json));

            var obj = new MyClass_WithNonPublicAccessors_WithPropertyAttributes();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(obj));
        }

        [Theory]
        [InlineData(typeof(ClassWithInitOnlyProperty))]
        [InlineData(typeof(StructWithInitOnlyProperty))]
        public override async Task InitOnlyProperties(Type type)
        {
            PropertyInfo property = type.GetProperty("MyInt");

            // Init-only properties can be serialized.
            object obj = Activator.CreateInstance(type);
            property.SetValue(obj, 1);
            Assert.Equal(@"{""MyInt"":1}", await Serializer.SerializeWrapper(obj, type));

            // Deserializing init-only properties is not supported.
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper(@"{""MyInt"":1}", type));
        }

        [Theory]
        [InlineData(typeof(Class_PropertyWith_PrivateInitOnlySetter_WithAttribute))]
        [InlineData(typeof(Class_PropertyWith_InternalInitOnlySetter_WithAttribute))]
        [InlineData(typeof(Class_PropertyWith_ProtectedInitOnlySetter_WithAttribute))]
        public override async Task NonPublicInitOnlySetter_With_JsonInclude(Type type)
        {
            PropertyInfo property = type.GetProperty("MyInt");

            // Init-only properties can be serialized.
            object obj = Activator.CreateInstance(type);
            property.SetValue(obj, 1);
            Assert.Equal(@"{""MyInt"":1}", await Serializer.SerializeWrapper(obj, type));

            // Deserializing init-only properties is not supported.
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper(@"{""MyInt"":1}", type));
        }

        [Fact]
        public override async Task HonorCustomConverter_UsingPrivateSetter()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());

            string json = @"{""MyEnum"":""AnotherValue"",""MyInt"":2}";

            // Deserialization baseline, without enum converter, we get JsonException. NB order of members in deserialized type is significant for this assertion to succeed.
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<StructWithPropertiesWithConverter>(json));

            // JsonInclude not supported in source gen.
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<StructWithPropertiesWithConverter>(json, options));

            // JsonInclude on private getters not supported.
            var obj = new StructWithPropertiesWithConverter();
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(obj, options));
        }

        [Fact]
        public override async Task Public_And_NonPublicPropertyAccessors_PropertyAttributes()
        {
            string json = @"{""W"":1,""X"":2,""Y"":3,""Z"":4}";

            var obj = await Serializer.DeserializeWrapper<ClassWithMixedPropertyAccessors_PropertyAttributes>(json);
            Assert.Equal(1, obj.W);
            Assert.Equal(2, obj.X);
            Assert.Equal(3, obj.Y);
            Assert.Equal(4, obj.GetZ);

            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(obj));
        }

        [Fact]
        public override async Task HonorJsonPropertyName_PrivateGetter()
        {
            string json = @"{""prop1"":1}";

            var obj = await Serializer.DeserializeWrapper<StructWithPropertiesWithJsonPropertyName_PrivateGetter>(json);
            Assert.Equal(MySmallEnum.AnotherValue, obj.GetProxy());

            // JsonInclude for private members not supported in source gen
            await Assert.ThrowsAsync<InvalidOperationException>(async() => await Serializer.SerializeWrapper(obj));
        }

        [Fact]
        public override async Task HonorJsonPropertyName_PrivateSetter()
        {
            string json = @"{""prop2"":2}";

            // JsonInclude for private members not supported in source gen
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper<StructWithPropertiesWithJsonPropertyName_PrivateSetter>(json));

            var obj = new StructWithPropertiesWithJsonPropertyName_PrivateSetter();
            obj.SetProxy(2);
            Assert.Equal(json, await Serializer.SerializeWrapper(obj));
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
        [JsonSerializable(typeof(ClassWithIgnoredPublicProperty))]
        [JsonSerializable(typeof(ClassWithIgnoredPublicPropertyAndNewSlotPrivate))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyPolicyConflictPublic))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyNamingConflictPrivate))]
        [JsonSerializable(typeof(ClassWithIgnoredNewSlotProperty))]
        [JsonSerializable(typeof(ClassWithObsoleteAndIgnoredProperty))]
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
#if !NETFRAMEWORK
        [JsonSerializable(typeof(ClassWithUnsupportedBigInteger))]
        [JsonSerializable(typeof(WrapperForClassWithUnsupportedBigInteger))]
        [JsonSerializable(typeof(ClassWithIgnoredUnsupportedBigInteger))]
        [JsonSerializable(typeof(WrapperForClassWithIgnoredUnsupportedBigInteger))]
#endif
        [JsonSerializable(typeof(ClassWithThingsToIgnore))]
        [JsonSerializable(typeof(ClassWithMixedPropertyAccessors_PropertyAttributes))]
        [JsonSerializable(typeof(ClassWithPropertyPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(ClassTwiceInheritedWithPropertyPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(MyClass_WithNonPublicAccessors))]
        [JsonSerializable(typeof(ClassWithThingsToIgnore_PerProperty))]
        [JsonSerializable(typeof(StructWithPropertiesWithJsonPropertyName_PrivateGetter))]
        [JsonSerializable(typeof(StructWithPropertiesWithJsonPropertyName_PrivateSetter))]
        [JsonSerializable(typeof(ClassWithValueAndReferenceTypes))]
        [JsonSerializable(typeof(ClassWithReadOnlyStringProperty_IgnoreWhenWritingDefault))]
        [JsonSerializable(typeof(ConcreteDerivedClass))]
        [JsonSerializable(typeof(TypeWith_RefStringProp))]
        [JsonSerializable(typeof(TypeWith_IgnoredRefStringProp))]
        [JsonSerializable(typeof(TypeWith_PropWith_BadConverter))]
        [JsonSerializable(typeof(TypeWith_IgnoredPropWith_BadConverter))]
        [JsonSerializable(typeof(ClassWithIgnoredCallbacks))]
        [JsonSerializable(typeof(ClassWithCallbacks))]
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
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper("{}", type));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(Activator.CreateInstance(type), type));
        }

        [Theory]
        [InlineData(typeof(ClassWithBadIgnoreAttribute))]
        [InlineData(typeof(StructWithBadIgnoreAttribute))]
        public override async Task JsonIgnoreCondition_WhenWritingNull_OnValueType_Fail(Type type)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper("{}", type));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(Activator.CreateInstance(type), type));
        }

        [Theory]
        [InlineData(typeof(ClassWithBadIgnoreAttribute))]
        [InlineData(typeof(StructWithBadIgnoreAttribute))]
        public override async Task JsonIgnoreCondition_WhenWritingNull_OnValueType_Fail_EmptyJson(Type type)
        {
            // Since this code goes down fast-path, there's no warm up and we hit the reader exception about having no tokens.
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper("", type));
            await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.SerializeWrapper(Activator.CreateInstance(type), type));
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
        [JsonSerializable(typeof(ClassWithIgnoredPublicProperty))]
        [JsonSerializable(typeof(ClassWithIgnoredPublicPropertyAndNewSlotPrivate))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyPolicyConflictPublic))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyNamingConflictPrivate))]
        [JsonSerializable(typeof(ClassWithIgnoredNewSlotProperty))]
        [JsonSerializable(typeof(ClassWithObsoleteAndIgnoredProperty))]
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
#if !NETFRAMEWORK
        [JsonSerializable(typeof(ClassWithUnsupportedBigInteger))]
        [JsonSerializable(typeof(WrapperForClassWithUnsupportedBigInteger))]
        [JsonSerializable(typeof(ClassWithIgnoredUnsupportedBigInteger))]
        [JsonSerializable(typeof(WrapperForClassWithIgnoredUnsupportedBigInteger))]
#endif
        [JsonSerializable(typeof(ClassWithThingsToIgnore))]
        [JsonSerializable(typeof(ClassWithMixedPropertyAccessors_PropertyAttributes))]
        [JsonSerializable(typeof(ClassWithPropertyPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(ClassTwiceInheritedWithPropertyPolicyConflictWhichThrows))]
        [JsonSerializable(typeof(MyClass_WithNonPublicAccessors))]
        [JsonSerializable(typeof(ClassWithThingsToIgnore_PerProperty))]
        [JsonSerializable(typeof(StructWithPropertiesWithJsonPropertyName_PrivateGetter))]
        [JsonSerializable(typeof(StructWithPropertiesWithJsonPropertyName_PrivateSetter))]
        [JsonSerializable(typeof(ClassWithValueAndReferenceTypes))]
        [JsonSerializable(typeof(ClassWithReadOnlyStringProperty_IgnoreWhenWritingDefault))]
        [JsonSerializable(typeof(ConcreteDerivedClass))]
        [JsonSerializable(typeof(TypeWith_RefStringProp))]
        [JsonSerializable(typeof(TypeWith_IgnoredRefStringProp))]
        [JsonSerializable(typeof(TypeWith_PropWith_BadConverter))]
        [JsonSerializable(typeof(TypeWith_IgnoredPropWith_BadConverter))]
        [JsonSerializable(typeof(ClassWithIgnoredCallbacks))]
        [JsonSerializable(typeof(ClassWithCallbacks))]
        internal sealed partial class PropertyVisibilityTestsContext_Default : JsonSerializerContext
        {
        }
    }
}

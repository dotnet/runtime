// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using System.Text.Json.Serialization.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.SourceGeneration.Tests
{
    public partial class PropertyVisibilityTests_Metadata : PropertyVisibilityTests
    {
        public PropertyVisibilityTests_Metadata()
            : this(new StringSerializerWrapper(PropertyVisibilityTestsContext_Metadata.Default))
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
        [InlineData(typeof(Class_PropertyWith_PrivateInitOnlySetter_WithAttribute))]
        [InlineData(typeof(Class_PropertyWith_InternalInitOnlySetter_WithAttribute))]
        [InlineData(typeof(Class_PropertyWith_ProtectedInitOnlySetter_WithAttribute))]
        public override async Task NonPublicInitOnlySetter_With_JsonInclude(Type type)
        {
            bool isDeserializationSupported = type == typeof(Class_PropertyWith_InternalInitOnlySetter_WithAttribute);

            PropertyInfo property = type.GetProperty("MyInt");

            // Init-only properties can be serialized.
            object obj = Activator.CreateInstance(type);
            property.SetValue(obj, 1);
            Assert.Equal(@"{""MyInt"":1}", await Serializer.SerializeWrapper(obj, type));

            // Deserializing JsonInclude is only supported for internal properties
            if (isDeserializationSupported)
            {
                obj = await Serializer.DeserializeWrapper(@"{""MyInt"":1}", type);
                Assert.Equal(1, (int)type.GetProperty("MyInt").GetValue(obj));
            }
            else
            {
                await Assert.ThrowsAsync<InvalidOperationException>(async () => await Serializer.DeserializeWrapper(@"{""MyInt"":1}", type));
            }
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

        [Fact]
        public override async Task TestCollectionWithPrivateElementType()
        {
            // The source generator cannot support enumerables whose element type is private.
            CollectionWithPrivateElementType collection = CollectionWithPrivateElementType.CreatePopulatedInstance();
            string json = collection.GetExpectedJson();

            Assert.True(Serializer.DefaultOptions.TryGetTypeInfo(typeof(CollectionWithPrivateElementType), out _));

            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.SerializeWrapper(collection));
            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<CollectionWithPrivateElementType>(json));
        }

        [Fact]
        public override async Task TestDictionaryWithPrivateKeyAndValueType()
        {
            // The source generator cannot support dictionaries whose key/value types are private.
            DictionaryWithPrivateKeyAndValueType dictionary = DictionaryWithPrivateKeyAndValueType.CreatePopulatedInstance();
            string json = dictionary.GetExpectedJson();

            Assert.True(Serializer.DefaultOptions.TryGetTypeInfo(typeof(DictionaryWithPrivateKeyAndValueType), out _));

            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.SerializeWrapper(dictionary));
            await Assert.ThrowsAsync<NotSupportedException>(() => Serializer.DeserializeWrapper<DictionaryWithPrivateKeyAndValueType>(json));
        }

        [Fact]
        public override async Task ClassWithIgnoredAndPrivateMembers_DoesNotIncludeIgnoredMetadata()
        {
            // The type referenced by the ignored/private properties should
            // not be included in the supported types by the generator.
            JsonSerializerOptions options = Serializer.DefaultOptions;
            Assert.Null(options.TypeInfoResolver.GetTypeInfo(typeof(TypeThatShouldNotBeGenerated), options));

            await base.ClassWithIgnoredAndPrivateMembers_DoesNotIncludeIgnoredMetadata();
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
        [JsonSerializable(typeof(StructWithInitOnlyProperty?))]
        [JsonSerializable(typeof(ClassWithCustomNamedInitOnlyProperty))]
        [JsonSerializable(typeof(StructWithCustomNamedInitOnlyProperty))]
        [JsonSerializable(typeof(MyClassWithValueTypeInterfaceProperty))]
        [JsonSerializable(typeof(ClassWithNonPublicProperties))]
        [JsonSerializable(typeof(ClassWithProperty_IgnoreConditionAlways))]
        [JsonSerializable(typeof(ClassWithBadIgnoreAttribute))]
        [JsonSerializable(typeof(StructWithBadIgnoreAttribute))]
        [JsonSerializable(typeof(Class_PropertyWith_InternalInitOnlySetter))]
        [JsonSerializable(typeof(Class_PropertyWith_ProtectedInitOnlySetter))]
        [JsonSerializable(typeof(ClassWithIgnoredPublicProperty))]
        [JsonSerializable(typeof(ClassWithIgnoredPublicPropertyAndNewSlotPrivate))]
        [JsonSerializable(typeof(ClassWithIgnoredPublicPropertyAndNewSlotPublicAndIgnoredToo))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyPolicyConflictPublic))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyNamingConflictPrivate))]
        [JsonSerializable(typeof(ClassWithIgnoredNewSlotProperty))]
        [JsonSerializable(typeof(ClassWithObsoleteAndIgnoredProperty))]
        [JsonSerializable(typeof(ClassWithPublicGetterAndPrivateSetter))]
        [JsonSerializable(typeof(ClassWithInitializedProps))]
        [JsonSerializable(typeof(ClassWithNewSlotInternalProperty))]
        [JsonSerializable(typeof(ClassWithPropertyPolicyConflict))]
        [JsonSerializable(typeof(ClassWithPrivateSetterAndGetter))]
        [JsonSerializable(typeof(ClassWithProtectedMembers))]
        [JsonSerializable(typeof(ClassWithProtectedGetter))]
        [JsonSerializable(typeof(ClassWithProtectedSetter))]
        [JsonSerializable(typeof(ClassWithPrivateProtectedMembers))]
        [JsonSerializable(typeof(ClassWithPrivateProtectedGetter))]
        [JsonSerializable(typeof(ClassWithPrivateProtectedSetter))]
        [JsonSerializable(typeof(ClassWithInternalProtectedMembers))]
        [JsonSerializable(typeof(ClassWithInternalProtectedGetter))]
        [JsonSerializable(typeof(ClassWithInternalProtectedSetter))]
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
        [JsonSerializable(typeof(ISimpleInterfaceHierarchy.IDerivedInterface1))]
        [JsonSerializable(typeof(ISimpleInterfaceHierarchy.IDerivedInterface2))]
        [JsonSerializable(typeof(IDiamondInterfaceHierarchy.IDerivedInterface1), TypeInfoPropertyName = "IDiamondInterfaceHierarchyIDerivedInterface1")]
        [JsonSerializable(typeof(IDiamondInterfaceHierarchy.IDerivedInterface2), TypeInfoPropertyName = "IDiamondInterfaceHierarchyIDerivedInterface2")]
        [JsonSerializable(typeof(IDiamondInterfaceHierarchy.IJoinInterface))]
        [JsonSerializable(typeof(ISimpleInterfaceHierarchyWithNamingConflict))]
        [JsonSerializable(typeof(ISimpleInterfaceHierarchyWithNamingConflict.IDerivedInterface))]
        [JsonSerializable(typeof(IDiamondInterfaceHierarchyWithNamingConflict.IJoinInterface), TypeInfoPropertyName = "IDiamondInterfaceHierarchyWithNamingConflictIJoinInterface")]
        [JsonSerializable(typeof(IDiamondInterfaceHierarchyWithNamingConflictUsingAttribute.IJoinInterface), TypeInfoPropertyName = "IDiamondInterfaceHierarchyWithNamingConflictUsingAttributeIJoinInterface")]
        [JsonSerializable(typeof(CollectionWithPrivateElementType))]
        [JsonSerializable(typeof(DictionaryWithPrivateKeyAndValueType))][JsonSerializable(typeof(ClassWithIgnoredAndPrivateMembers))]
        [JsonSerializable(typeof(ClassWithInternalJsonIncludeProperties))]
        [JsonSerializable(typeof(ClassWithIgnoredAndPrivateMembers))]
        [JsonSerializable(typeof(ClassUsingIgnoreWhenWritingDefaultAttribute))]
        [JsonSerializable(typeof(ClassUsingIgnoreNeverAttribute))]
        [JsonSerializable(typeof(ClassWithIgnoredUnsupportedDictionary))]
        [JsonSerializable(typeof(ClassWithProperty_IgnoreConditionAlways_Ctor))]
        [JsonSerializable(typeof(ClassWithClassProperty_IgnoreConditionWhenWritingDefault_Ctor))]
        [JsonSerializable(typeof(StructWithStructProperty_IgnoreConditionWhenWritingDefault_Ctor))]
        [JsonSerializable(typeof(SmallStructWithValueAndReferenceTypes))]
        [JsonSerializable(typeof(WrapperForClassWithIgnoredUnsupportedDictionary))]
        [JsonSerializable(typeof(Class1))]
        [JsonSerializable(typeof(Class2))]
        [JsonSerializable(typeof(NamespaceBase.Class1), TypeInfoPropertyName = "Class1FromNamespaceBase")]
        [JsonSerializable(typeof(NamespaceBase.Class2), TypeInfoPropertyName = "Class2FromNamespaceBase")]
        internal sealed partial class PropertyVisibilityTestsContext_Metadata : JsonSerializerContext
        {
        }
    }

    public partial class PropertyVisibilityTests_Default : PropertyVisibilityTests_Metadata
    {
        public PropertyVisibilityTests_Default()
            : base(new StringSerializerWrapper(PropertyVisibilityTestsContext_Default.Default))
        {
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

        [Fact]
        public void PublicContextAndTestClassWithPropertiesWithDifferentAccessibilities()
        {
            JsonSerializerOptions options = new()
            {
                IncludeFields = true,
            };

            options.AddContext<PublicContext>();

            PublicClassWithDifferentAccessibilitiesProperties obj = new()
            {
                PublicProperty = new(),
                PublicField = new(),
            };

            string json = JsonSerializer.Serialize(obj, options);
            Assert.Equal("""{"PublicProperty":{},"PublicField":{}}""", json);

            var deserialized = JsonSerializer.Deserialize<PublicClassWithDifferentAccessibilitiesProperties>(json, options);
            Assert.NotNull(deserialized.PublicProperty);
            Assert.NotNull(deserialized.PublicField);

            json = "{}";
            deserialized = JsonSerializer.Deserialize<PublicClassWithDifferentAccessibilitiesProperties>(json, options);
            Assert.Null(deserialized.PublicProperty);
            Assert.Null(deserialized.PublicField);
        }

        [Fact]
        public void PublicContextAndJsonConverter()
        {
            JsonConverter obj = JsonMetadataServices.BooleanConverter;

            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(obj, PublicContext.Default.Options));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<JsonConverter>("{}", PublicContext.Default.Options));
        }

        [Fact]
        public void PublicContextAndJsonSerializerOptions()
        {
            JsonSerializerOptions obj = new()
            {
                DefaultBufferSize = 123,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                IncludeFields = true,
            };

            string json = JsonSerializer.Serialize(obj, PublicContext.Default.Options);

            JsonSerializerOptions deserialized = JsonSerializer.Deserialize<JsonSerializerOptions>(json, PublicContext.Default.Options);
            Assert.Equal(obj.DefaultBufferSize, deserialized.DefaultBufferSize);
            Assert.Equal(obj.DefaultIgnoreCondition, deserialized.DefaultIgnoreCondition);
            Assert.Equal(obj.IncludeFields, deserialized.IncludeFields);
            Assert.Equal(obj.IgnoreReadOnlyFields, deserialized.IgnoreReadOnlyFields);
            Assert.Equal(obj.MaxDepth, deserialized.MaxDepth);
        }

        [Fact]
        public void PocoWithNullableProperties_IgnoresNullValuesWithGlobalSetting()
        {
            // Regression test for https://github.com/dotnet/runtime/issues/96404
            var value = new PocoWithNullableProperties();
            string json = JsonSerializer.Serialize(value, DefaultContextWithGlobalIgnoreSetting.Default.PocoWithNullableProperties);
            Assert.Equal("{}", json);
        }

        class PocoWithNullableProperties
        {
            public string? NullableRefType { get; set; }
            public int? NullableValueType { get; set; }
        }

        [JsonSourceGenerationOptions(
            GenerationMode = JsonSourceGenerationMode.Default,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
        [JsonSerializable(typeof(PocoWithNullableProperties))]
        partial class DefaultContextWithGlobalIgnoreSetting : JsonSerializerContext;

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
        [JsonSerializable(typeof(StructWithInitOnlyProperty?))]
        [JsonSerializable(typeof(ClassWithCustomNamedInitOnlyProperty))]
        [JsonSerializable(typeof(StructWithCustomNamedInitOnlyProperty))]
        [JsonSerializable(typeof(MyClassWithValueTypeInterfaceProperty))]
        [JsonSerializable(typeof(ClassWithNonPublicProperties))]
        [JsonSerializable(typeof(ClassWithProperty_IgnoreConditionAlways))]
        [JsonSerializable(typeof(ClassWithBadIgnoreAttribute))]
        [JsonSerializable(typeof(StructWithBadIgnoreAttribute))]
        [JsonSerializable(typeof(Class_PropertyWith_InternalInitOnlySetter))]
        [JsonSerializable(typeof(Class_PropertyWith_ProtectedInitOnlySetter))]
        [JsonSerializable(typeof(ClassWithIgnoredPublicProperty))]
        [JsonSerializable(typeof(ClassWithIgnoredPublicPropertyAndNewSlotPrivate))]
        [JsonSerializable(typeof(ClassWithIgnoredPublicPropertyAndNewSlotPublicAndIgnoredToo))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyPolicyConflictPublic))]
        [JsonSerializable(typeof(ClassWithIgnoredPropertyNamingConflictPrivate))]
        [JsonSerializable(typeof(ClassWithIgnoredNewSlotProperty))]
        [JsonSerializable(typeof(ClassWithObsoleteAndIgnoredProperty))]
        [JsonSerializable(typeof(ClassWithPublicGetterAndPrivateSetter))]
        [JsonSerializable(typeof(ClassWithInitializedProps))]
        [JsonSerializable(typeof(ClassWithNewSlotInternalProperty))]
        [JsonSerializable(typeof(ClassWithPropertyPolicyConflict))]
        [JsonSerializable(typeof(ClassWithPrivateSetterAndGetter))]
        [JsonSerializable(typeof(ClassWithProtectedMembers))]
        [JsonSerializable(typeof(ClassWithProtectedGetter))]
        [JsonSerializable(typeof(ClassWithProtectedSetter))]
        [JsonSerializable(typeof(ClassWithPrivateProtectedMembers))]
        [JsonSerializable(typeof(ClassWithPrivateProtectedGetter))]
        [JsonSerializable(typeof(ClassWithPrivateProtectedSetter))]
        [JsonSerializable(typeof(ClassWithInternalProtectedMembers))]
        [JsonSerializable(typeof(ClassWithInternalProtectedGetter))]
        [JsonSerializable(typeof(ClassWithInternalProtectedSetter))]
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
        [JsonSerializable(typeof(ISimpleInterfaceHierarchy.IDerivedInterface1))]
        [JsonSerializable(typeof(ISimpleInterfaceHierarchy.IDerivedInterface2))]
        [JsonSerializable(typeof(IDiamondInterfaceHierarchy.IDerivedInterface1), TypeInfoPropertyName = "IDiamondInterfaceHierarchyIDerivedInterface1")]
        [JsonSerializable(typeof(IDiamondInterfaceHierarchy.IDerivedInterface2), TypeInfoPropertyName = "IDiamondInterfaceHierarchyIDerivedInterface2")]
        [JsonSerializable(typeof(IDiamondInterfaceHierarchy.IJoinInterface))]
        [JsonSerializable(typeof(ISimpleInterfaceHierarchyWithNamingConflict))]
        [JsonSerializable(typeof(ISimpleInterfaceHierarchyWithNamingConflict.IDerivedInterface))]
        [JsonSerializable(typeof(IDiamondInterfaceHierarchyWithNamingConflict.IJoinInterface), TypeInfoPropertyName = "IDiamondInterfaceHierarchyWithNamingConflictIJoinInterface")]
        [JsonSerializable(typeof(IDiamondInterfaceHierarchyWithNamingConflictUsingAttribute.IJoinInterface), TypeInfoPropertyName = "IDiamondInterfaceHierarchyWithNamingConflictUsingAttributeIJoinInterface")]
        [JsonSerializable(typeof(CollectionWithPrivateElementType))]
        [JsonSerializable(typeof(DictionaryWithPrivateKeyAndValueType))]
        [JsonSerializable(typeof(ClassWithInternalJsonIncludeProperties))]
        [JsonSerializable(typeof(ClassWithIgnoredAndPrivateMembers))]
        [JsonSerializable(typeof(ClassUsingIgnoreWhenWritingDefaultAttribute))]
        [JsonSerializable(typeof(ClassUsingIgnoreNeverAttribute))]
        [JsonSerializable(typeof(ClassWithIgnoredUnsupportedDictionary))]
        [JsonSerializable(typeof(ClassWithProperty_IgnoreConditionAlways_Ctor))]
        [JsonSerializable(typeof(ClassWithClassProperty_IgnoreConditionWhenWritingDefault_Ctor))]
        [JsonSerializable(typeof(StructWithStructProperty_IgnoreConditionWhenWritingDefault_Ctor))]
        [JsonSerializable(typeof(SmallStructWithValueAndReferenceTypes))]
        [JsonSerializable(typeof(WrapperForClassWithIgnoredUnsupportedDictionary))]
        [JsonSerializable(typeof(Class1))]
        [JsonSerializable(typeof(Class2))]
        [JsonSerializable(typeof(NamespaceBase.Class1), TypeInfoPropertyName = "Class1FromNamespaceBase")]
        [JsonSerializable(typeof(NamespaceBase.Class2), TypeInfoPropertyName = "Class2FromNamespaceBase")]
        internal sealed partial class PropertyVisibilityTestsContext_Default : JsonSerializerContext
        {
        }
    }
}

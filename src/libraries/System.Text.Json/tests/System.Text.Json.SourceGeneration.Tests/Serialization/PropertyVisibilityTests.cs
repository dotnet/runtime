// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
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
            // Inaccessible [JsonInclude] members are ignored (https://github.com/dotnet/runtime/issues/124889).
            // MyInt (public get, private set) and MyString (public get, internal set) serialize normally.
            // MyFloat (private get, public set) and MyUri (internal get, public set) lack getter delegates.
            string json = """{"MyInt":1,"MyString":"Hello","MyFloat":2,"MyUri":"https://microsoft.com"}""";
            var obj = await Serializer.DeserializeWrapper<MyClass_WithNonPublicAccessors_WithPropertyAttributes>(json);
            Assert.Equal(0, obj.MyInt);
            Assert.Equal("Hello", obj.MyString);

            string serialized = await Serializer.SerializeWrapper(obj);
            Assert.Contains(@"""MyString"":""Hello""", serialized);
            Assert.Contains(@"""MyUri"":""https://microsoft.com""", serialized);
        }

        [Theory]
        [InlineData(typeof(Class_PropertyWith_PrivateInitOnlySetter_WithAttribute))]
        [InlineData(typeof(Class_PropertyWith_InternalInitOnlySetter_WithAttribute))]
        [InlineData(typeof(Class_PropertyWith_ProtectedInitOnlySetter_WithAttribute))]
        public override async Task NonPublicInitOnlySetter_With_JsonInclude(Type type)
        {
            // Inaccessible [JsonInclude] members are ignored (https://github.com/dotnet/runtime/issues/124889).
            // All types have public getter so serialization works; internal setter type can also deserialize.
            bool isDeserializationSupported = type == typeof(Class_PropertyWith_InternalInitOnlySetter_WithAttribute);

            object obj = Activator.CreateInstance(type);
            type.GetProperty("MyInt").SetValue(obj, 1);
            Assert.Equal("""{"MyInt":1}""", await Serializer.SerializeWrapper(obj, type));

            if (isDeserializationSupported)
            {
                obj = await Serializer.DeserializeWrapper("""{"MyInt":1}""", type);
                Assert.Equal(1, (int)type.GetProperty("MyInt").GetValue(obj));
            }
            else
            {
                obj = await Serializer.DeserializeWrapper("""{"MyInt":1}""", type);
                Assert.Equal(0, (int)type.GetProperty("MyInt").GetValue(obj));
            }
        }

        [Fact]
        public override async Task HonorCustomConverter_UsingPrivateSetter()
        {
            // Inaccessible [JsonInclude] members are ignored (https://github.com/dotnet/runtime/issues/124889).
            // MyEnum (private get, public set): setter works, getter excluded.
            // MyInt (public get, private set): getter works (with converter), setter excluded.
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter());

            string json = """{"MyEnum":"AnotherValue","MyInt":2}""";
            var obj = await Serializer.DeserializeWrapper<StructWithPropertiesWithConverter>(json, options);
            Assert.Equal(MySmallEnum.AnotherValue, obj.GetMyEnum);
            Assert.Equal(0, obj.MyInt);
        }

        [Fact]
        public override async Task Public_And_NonPublicPropertyAccessors_PropertyAttributes()
        {
            // Inaccessible [JsonInclude] members are ignored (https://github.com/dotnet/runtime/issues/124889).
            // Z (private get, public set) has no getter delegate so won't appear in serialization output.
            string json = """{"W":1,"X":2,"Y":3,"Z":4}""";
            var obj = await Serializer.DeserializeWrapper<ClassWithMixedPropertyAccessors_PropertyAttributes>(json);
            Assert.Equal(1, obj.W);
            Assert.Equal(2, obj.X);
            Assert.Equal(3, obj.Y);
            Assert.Equal(4, obj.GetZ);

            string serialized = await Serializer.SerializeWrapper(obj);
            Assert.Contains(@"""W"":1", serialized);
            Assert.Contains(@"""X"":2", serialized);
            Assert.Contains(@"""Y"":3", serialized);
            Assert.DoesNotContain(@"""Z"":", serialized);
        }

        [Fact]
        public override async Task HonorJsonPropertyName_PrivateGetter()
        {
            // Inaccessible [JsonInclude] members are ignored (https://github.com/dotnet/runtime/issues/124889).
            // MyEnum (private get, public set) won't serialize (no getter delegate).
            string json = """{"prop1":1}""";
            var obj = await Serializer.DeserializeWrapper<StructWithPropertiesWithJsonPropertyName_PrivateGetter>(json);
            Assert.Equal(MySmallEnum.AnotherValue, obj.GetProxy());

            string serialized = await Serializer.SerializeWrapper(obj);
            Assert.DoesNotContain("prop1", serialized);
        }

        [Fact]
        public override async Task HonorJsonPropertyName_PrivateSetter()
        {
            // Inaccessible [JsonInclude] members are ignored (https://github.com/dotnet/runtime/issues/124889).
            // MyInt (public get, private set) serializes but can't be deserialized.
            var obj = new StructWithPropertiesWithJsonPropertyName_PrivateSetter();
            obj.SetProxy(2);
            Assert.Equal("""{"prop2":2}""", await Serializer.SerializeWrapper(obj));

            obj = await Serializer.DeserializeWrapper<StructWithPropertiesWithJsonPropertyName_PrivateSetter>("""{"prop2":2}""");
            Assert.Equal(0, obj.MyInt);
        }

        public override async Task NonPublicProperty_JsonInclude_WorksAsExpected(Type type, bool isAccessibleBySourceGen)
        {
            // Inaccessible [JsonInclude] members are ignored (https://github.com/dotnet/runtime/issues/124889).
            if (isAccessibleBySourceGen)
            {
                await base.NonPublicProperty_JsonInclude_WorksAsExpected(type, isAccessibleBySourceGen);
            }
            else
            {
                object result = await Serializer.DeserializeWrapper("""{"MyString":"value"}""", type);
                Assert.IsType(type, result);

                string json = await Serializer.SerializeWrapper(result, type);
                Assert.Equal("{}", json);
            }
        }

        // The following tests validate that inaccessible [JsonInclude] members are ignored
        // until https://github.com/dotnet/runtime/issues/124889 is complete (tracking: https://github.com/dotnet/runtime/issues/88519).

        [Fact]
        public override async Task JsonInclude_PrivateProperties_CanRoundtrip()
        {
            var obj = ClassWithPrivateJsonIncludeProperties_Roundtrip.Create("Test", 25);
            string json = await Serializer.SerializeWrapper(obj);
            Assert.Equal("{}", json);
        }

        [Fact]
        public override async Task JsonInclude_ProtectedProperties_CanRoundtrip()
        {
            var obj = ClassWithProtectedJsonIncludeProperties_Roundtrip.Create("Test", 25);
            string json = await Serializer.SerializeWrapper(obj);
            Assert.Equal("{}", json);
        }

        [Fact]
        public override async Task JsonInclude_MixedAccessibility_AllPropertiesRoundtrip()
        {
            string json = """{"PublicProp":1,"InternalProp":2,"PrivateProp":3,"ProtectedProp":4}""";
            var deserialized = await Serializer.DeserializeWrapper<ClassWithMixedAccessibilityJsonIncludeProperties>(json);
            Assert.Equal(1, deserialized.PublicProp);
            Assert.Equal(2, deserialized.InternalProp);
            Assert.Equal(0, deserialized.GetPrivateProp());
            Assert.Equal(0, deserialized.GetProtectedProp());

            string actualJson = await Serializer.SerializeWrapper(deserialized);
            Assert.Contains(@"""PublicProp"":1", actualJson);
            Assert.Contains(@"""InternalProp"":2", actualJson);
            Assert.DoesNotContain("PrivateProp", actualJson);
            Assert.DoesNotContain("ProtectedProp", actualJson);
        }

        [Fact]
        public override async Task JsonInclude_PrivateInitOnlyProperties_PreservesDefaults()
        {
            var deserialized = await Serializer.DeserializeWrapper<ClassWithJsonIncludePrivateInitOnlyProperties>("{}");
            Assert.Equal("DefaultName", deserialized.Name);
            Assert.Equal(42, deserialized.Number);

            deserialized = await Serializer.DeserializeWrapper<ClassWithJsonIncludePrivateInitOnlyProperties>("""{"Name":"Override","Number":100}""");
            Assert.Equal("DefaultName", deserialized.Name);
            Assert.Equal(42, deserialized.Number);
        }

        [Fact]
        public override async Task JsonInclude_PrivateGetterProperties_CanSerialize()
        {
            var obj = new ClassWithJsonIncludePrivateGetterProperties { Name = "Test", Number = 99 };
            string json = await Serializer.SerializeWrapper(obj);
            Assert.DoesNotContain("Name", json);
            Assert.DoesNotContain("Number", json);
        }

        [Fact]
        public override async Task JsonInclude_PrivateProperties_EmptyJson_DeserializesToDefault()
        {
            var deserialized = await Serializer.DeserializeWrapper<ClassWithPrivateJsonIncludeProperties_Roundtrip>("{}");
            Assert.Equal("default", deserialized.GetName());
            Assert.Equal(0, deserialized.GetAge());
        }

        [Fact]
        public override async Task JsonInclude_StructWithPrivateProperties_CanRoundtrip()
        {
            var obj = StructWithJsonIncludePrivateProperties.Create("Hello", 42);
            string json = await Serializer.SerializeWrapper(obj);
            Assert.Equal("{}", json);
        }

        [Fact]
        public override async Task JsonInclude_GenericType_PrivateProperties_CanRoundtrip()
        {
            var obj = GenericClassWithPrivateJsonIncludeProperties<int>.Create(42, "test");
            string json = await Serializer.SerializeWrapper(obj);
            Assert.Equal("{}", json);
        }

        [Fact]
        public override void InitOnlyProperties_ExposesSetterDelegate()
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeof(ClassWithJsonIncludePrivateInitOnlyProperties));

            JsonPropertyInfo nameProp = typeInfo.Properties.Single(p => p.Name == "Name");
            Assert.NotNull(nameProp.Get);
            Assert.Null(nameProp.Set);

            JsonPropertyInfo numberProp = typeInfo.Properties.Single(p => p.Name == "Number");
            Assert.NotNull(numberProp.Get);
            Assert.Null(numberProp.Set);
        }

        public override void NonPublicInitOnlyJsonIncludeProperties_HaveNoAssociatedParameterInfo(Type type)
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type);
            bool isAccessible = type.Name.Contains("Internal");
            if (isAccessible)
            {
                JsonPropertyInfo prop = typeInfo.Properties.Single(p => p.Name == "MyString");
                Assert.NotNull(prop.Get);
                Assert.NotNull(prop.Set);
                Assert.Null(prop.AssociatedParameter);
            }
            else
            {
                Assert.DoesNotContain(typeInfo.Properties, p => p.Name == "MyString");
            }
        }

        [Fact]
        public override void PrivateJsonIncludeProperties_ExposesGetterAndSetterDelegates()
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeof(ClassWithPrivateJsonIncludeProperties_Roundtrip));
            Assert.Empty(typeInfo.Properties);
        }

        [Fact]
        public override void PrivateJsonIncludeGetterOnly_ExposesGetterDelegate()
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeof(ClassWithJsonIncludePrivateGetterProperties));

            JsonPropertyInfo nameProp = typeInfo.Properties.Single(p => p.Name == "Name");
            Assert.Null(nameProp.Get);
            Assert.NotNull(nameProp.Set);
        }

        public override void NonPublicJsonIncludeMembers_ExposeGetterAndSetterDelegates(Type type)
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type);
            bool isAccessible = type.Name.Contains("Internal");
            if (isAccessible)
            {
                JsonPropertyInfo prop = typeInfo.Properties.Single(p => p.Name == "MyString");
                Assert.NotNull(prop.Get);
                Assert.NotNull(prop.Set);
            }
            else
            {
                Assert.DoesNotContain(typeInfo.Properties, p => p.Name == "MyString");
            }
        }

        [Fact]
        public override void MixedAccessibilityJsonIncludeProperties_AllExposeGetterAndSetterDelegates()
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeof(ClassWithMixedAccessibilityJsonIncludeProperties));
            Assert.Equal(2, typeInfo.Properties.Count);
            Assert.NotNull(typeInfo.Properties.Single(p => p.Name == "PublicProp").Get);
            Assert.NotNull(typeInfo.Properties.Single(p => p.Name == "InternalProp").Get);
        }

        [Fact]
        public override void StructWithPrivateJsonIncludeProperties_ExposesGetterAndSetterDelegates()
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeof(StructWithJsonIncludePrivateProperties));
            Assert.Empty(typeInfo.Properties);
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

        [Fact]
        public override async Task JsonIgnoreCondition_TypeLevel_Always_ThrowsInvalidOperation()
        {
            // In the source generator path, 'JsonIgnoreCondition.Always' on a type emits a diagnostic warning
            // and the attribute is ignored, so all properties are serialized normally.
            var obj = new ClassWithTypeLevelIgnore_Always
            {
                MyString = "value",
                MyInt = 42
            };

            string json = await Serializer.SerializeWrapper(obj);
            Assert.Contains(@"""MyString"":""value""", json);
            Assert.Contains(@"""MyInt"":42", json);
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
        [JsonSerializable(typeof(Class_WithIgnoredInitOnlyProperty))]
        [JsonSerializable(typeof(Record_WithIgnoredPropertyInCtor))]
        [JsonSerializable(typeof(Class_WithIgnoredRequiredProperty))]
        [JsonSerializable(typeof(RecordWithIgnoredNestedInitOnlyProperty))]
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
        [JsonSerializable(typeof(ClassWithIgnoredAndPrivateMembers))]
        [JsonSerializable(typeof(ClassWithInternalJsonIncludeProperties))]
        [JsonSerializable(typeof(ClassWithIgnoredAndPrivateMembers))]
        [JsonSerializable(typeof(ClassWithPrivateJsonIncludeProperties_Roundtrip))]
        [JsonSerializable(typeof(ClassWithProtectedJsonIncludeProperties_Roundtrip))]
        [JsonSerializable(typeof(ClassWithMixedAccessibilityJsonIncludeProperties))]
        [JsonSerializable(typeof(ClassWithJsonIncludePrivateInitOnlyProperties))]
        [JsonSerializable(typeof(ClassWithJsonIncludePrivateGetterProperties))]
        [JsonSerializable(typeof(StructWithJsonIncludePrivateProperties))]
        [JsonSerializable(typeof(GenericClassWithPrivateJsonIncludeProperties<int>))]
        [JsonSerializable(typeof(ConstrainedGenericClassWithInitOnlyProperties<ConstraintDerived>))]
        [JsonSerializable(typeof(ClassWithInitOnlyPropertyDefaults))]
        [JsonSerializable(typeof(StructWithInitOnlyPropertyDefaults))]
        [JsonSerializable(typeof(ClassUsingIgnoreWhenWritingDefaultAttribute))]
        [JsonSerializable(typeof(ClassUsingIgnoreNeverAttribute))]
        [JsonSerializable(typeof(ClassWithIgnoredUnsupportedDictionary))]
        [JsonSerializable(typeof(ClassWithProperty_IgnoreConditionAlways_Ctor))]
        [JsonSerializable(typeof(ClassWithClassProperty_IgnoreConditionWhenWritingDefault_Ctor))]
        [JsonSerializable(typeof(StructWithStructProperty_IgnoreConditionWhenWritingDefault_Ctor))]
        [JsonSerializable(typeof(JsonIgnoreCondition_WhenReadingWritingTestModel))]
        [JsonSerializable(typeof(ClassWithTypeLevelIgnore_WhenWritingNull))]
        [JsonSerializable(typeof(ClassWithTypeLevelIgnore_WhenWritingDefault))]
        [JsonSerializable(typeof(ClassWithTypeLevelIgnore_Always))]
        [JsonSerializable(typeof(ClassWithTypeLevelIgnore_PropertyOverride))]
        [JsonSerializable(typeof(StructWithTypeLevelIgnore_WhenWritingNull))]
        [JsonSerializable(typeof(BaseClassWithProperties))]
        [JsonSerializable(typeof(DerivedClassWithTypeLevelIgnore))]
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
        [JsonSerializable(typeof(Class_WithIgnoredInitOnlyProperty))]
        [JsonSerializable(typeof(Record_WithIgnoredPropertyInCtor))]
        [JsonSerializable(typeof(Class_WithIgnoredRequiredProperty))]
        [JsonSerializable(typeof(RecordWithIgnoredNestedInitOnlyProperty))]
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
        [JsonSerializable(typeof(ClassWithPrivateJsonIncludeProperties_Roundtrip))]
        [JsonSerializable(typeof(ClassWithProtectedJsonIncludeProperties_Roundtrip))]
        [JsonSerializable(typeof(ClassWithMixedAccessibilityJsonIncludeProperties))]
        [JsonSerializable(typeof(ClassWithJsonIncludePrivateInitOnlyProperties))]
        [JsonSerializable(typeof(ClassWithJsonIncludePrivateGetterProperties))]
        [JsonSerializable(typeof(StructWithJsonIncludePrivateProperties))]
        [JsonSerializable(typeof(GenericClassWithPrivateJsonIncludeProperties<int>))]
        [JsonSerializable(typeof(ConstrainedGenericClassWithInitOnlyProperties<ConstraintDerived>))]
        [JsonSerializable(typeof(ClassWithInitOnlyPropertyDefaults))]
        [JsonSerializable(typeof(StructWithInitOnlyPropertyDefaults))]
        [JsonSerializable(typeof(ClassUsingIgnoreWhenWritingDefaultAttribute))]
        [JsonSerializable(typeof(ClassUsingIgnoreNeverAttribute))]
        [JsonSerializable(typeof(ClassWithIgnoredUnsupportedDictionary))]
        [JsonSerializable(typeof(ClassWithProperty_IgnoreConditionAlways_Ctor))]
        [JsonSerializable(typeof(ClassWithClassProperty_IgnoreConditionWhenWritingDefault_Ctor))]
        [JsonSerializable(typeof(StructWithStructProperty_IgnoreConditionWhenWritingDefault_Ctor))]
        [JsonSerializable(typeof(JsonIgnoreCondition_WhenReadingWritingTestModel))]
        [JsonSerializable(typeof(ClassWithTypeLevelIgnore_WhenWritingNull))]
        [JsonSerializable(typeof(ClassWithTypeLevelIgnore_WhenWritingDefault))]
        [JsonSerializable(typeof(ClassWithTypeLevelIgnore_Always))]
        [JsonSerializable(typeof(ClassWithTypeLevelIgnore_PropertyOverride))]
        [JsonSerializable(typeof(StructWithTypeLevelIgnore_WhenWritingNull))]
        [JsonSerializable(typeof(BaseClassWithProperties))]
        [JsonSerializable(typeof(DerivedClassWithTypeLevelIgnore))]
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

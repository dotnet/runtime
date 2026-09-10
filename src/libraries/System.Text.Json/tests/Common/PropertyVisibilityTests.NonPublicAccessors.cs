// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class PropertyVisibilityTests
    {
        [Fact]
        public async Task NonPublic_AccessorsNotSupported_WithoutAttribute()
        {
            string json = """
                    {
                                    "MyInt":1,
                                    "MyString":"Hello",
                                    "MyFloat":2,
                                    "MyUri":"https://microsoft.com"
                                }
                """;

            var obj = await Serializer.DeserializeWrapper<MyClass_WithNonPublicAccessors>(json);
            Assert.Equal(0, obj.MyInt);
            Assert.Null(obj.MyString);
            Assert.Equal(2f, obj.GetMyFloat);
            Assert.Equal(new Uri("https://microsoft.com"), obj.MyUri);

            json = await Serializer.SerializeWrapper(obj);
            Assert.Contains("""
                "MyInt":0
                """, json);
            Assert.Contains("""
                "MyString":null
                """, json);
            Assert.DoesNotContain("""
                "MyFloat":
                """, json);
            Assert.DoesNotContain("""
                "MyUri":
                """, json);
        }

        public class MyClass_WithNonPublicAccessors
        {
            public int MyInt { get; private set; }
            public string? MyString { get; internal set; }
            public float MyFloat { private get; set; }
            public Uri MyUri { internal get; set; }

            // For test validation.
            internal float GetMyFloat => MyFloat;
        }

        [Fact]
        public virtual async Task Honor_JsonSerializablePropertyAttribute_OnProperties()
        {
            string json = """
                    {
                                    "MyInt":1,
                                    "MyString":"Hello",
                                    "MyFloat":2,
                                    "MyUri":"https://microsoft.com"
                                }
                """;

            var obj = await Serializer.DeserializeWrapper<MyClass_WithNonPublicAccessors_WithPropertyAttributes>(json);
            Assert.Equal(1, obj.MyInt);
            Assert.Equal("Hello", obj.MyString);
            Assert.Equal(2f, obj.GetMyFloat);
            Assert.Equal(new Uri("https://microsoft.com"), obj.MyUri);

            json = await Serializer.SerializeWrapper(obj);
            Assert.Contains("""
                "MyInt":1
                """, json);
            Assert.Contains("""
                "MyString":"Hello"
                """, json);
            Assert.Contains("""
                "MyFloat":2
                """, json);
            Assert.Contains("""
                "MyUri":"https://microsoft.com"
                """, json);
        }

        public class MyClass_WithNonPublicAccessors_WithPropertyAttributes
        {
            [JsonInclude]
            public int MyInt { get; private set; }
            [JsonInclude]
            public string? MyString { get; internal set; }
            [JsonInclude]
            public float MyFloat { private get; set; }
            [JsonInclude]
            public Uri? MyUri { internal get; set; }

            // For test validation.
            internal float GetMyFloat => MyFloat;
        }

        private class MyClass_WithNonPublicAccessors_WithPropertyAttributes_And_PropertyIgnore
        {
            [JsonInclude]
            [JsonIgnore]
            public int MyInt { get; private set; }

            [JsonInclude]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
            public string MyString { get; internal set; } = "DefaultString";

            [JsonInclude]
            [JsonIgnore(Condition = JsonIgnoreCondition.Always)]
            public float MyFloat { private get; set; }

            [JsonInclude]
            [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
            public Uri MyUri { internal get; set; }

            // For test validation.
            internal float GetMyFloat => MyFloat;
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        // Need support for extension data.
        [ActiveIssue("https://github.com/dotnet/runtime/issues/45448")]
#endif
        public async Task ExtensionDataCanHaveNonPublicSetter()
        {
            string json = """{"Key":"Value"}""";

            // Baseline
            var obj1 = await Serializer.DeserializeWrapper<ClassWithExtensionData_NonPublicSetter>(json);
            Assert.Null(obj1.ExtensionData);
            Assert.Equal("{}", await Serializer.SerializeWrapper(obj1));

            // With attribute
            var obj2 = await Serializer.DeserializeWrapper<ClassWithExtensionData_NonPublicSetter_WithAttribute>(json);
            Assert.Equal("Value", obj2.ExtensionData["Key"].GetString());
            Assert.Equal(json, await Serializer.SerializeWrapper(obj2));
        }

        private class ClassWithExtensionData_NonPublicSetter
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; private set; }
        }

        private class ClassWithExtensionData_NonPublicSetter_WithAttribute
        {
            [JsonExtensionData]
            [JsonInclude]
            public Dictionary<string, JsonElement> ExtensionData { get; private set; }
        }

        private class ClassWithExtensionData_NonPublicGetter
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { internal get; set; }
        }

        [Fact]
        public virtual async Task HonorCustomConverter_UsingPrivateSetter()
        {
            var options = new JsonSerializerOptions();
            options.Converters.Add(new JsonStringEnumConverter<MySmallEnum>());

            string json = """{"MyEnum":"AnotherValue","MyInt":2}""";

            // Deserialization baseline, without enum converter, we get JsonException.
            await Assert.ThrowsAsync<JsonException>(async () => await Serializer.DeserializeWrapper<StructWithPropertiesWithConverter>(json));

            var obj = await Serializer.DeserializeWrapper<StructWithPropertiesWithConverter>(json, options);
            Assert.Equal(MySmallEnum.AnotherValue, obj.GetMyEnum);
            Assert.Equal(25, obj.MyInt);

            // ConverterForInt32 throws this exception.
            await Assert.ThrowsAsync<NotImplementedException>(async () => await Serializer.SerializeWrapper(obj, options));
        }

        public struct StructWithPropertiesWithConverter
        {
            [JsonInclude]
            public MySmallEnum MyEnum { private get; set; }

            [JsonInclude]
            [JsonConverter(typeof(ConverterForInt32))]
            public int MyInt { get; private set; }

            // For test validation.
            internal MySmallEnum GetMyEnum => MyEnum;
        }

        public enum MySmallEnum
        {
            DefaultValue = 0,
            AnotherValue = 1
        }

        [Fact]
        public async Task HonorCaseInsensitivity()
        {
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            string json = """{"MYSTRING":"Hello"}""";
            Assert.Null((await Serializer.DeserializeWrapper<MyStruct_WithNonPublicAccessors_WithTypeAttribute>(json)).MyString);
            Assert.Equal("Hello", (await Serializer.DeserializeWrapper<MyStruct_WithNonPublicAccessors_WithTypeAttribute>(json, options)).MyString);
        }

        public struct MyStruct_WithNonPublicAccessors_WithTypeAttribute
        {
            [JsonInclude]
            public int MyInt { get; private set; }
            [JsonInclude]
            public string MyString { get; internal set; }
            [JsonInclude]
            public float MyFloat { private get; set; }
            [JsonInclude]
            public Uri MyUri { internal get; set; }

            // For test validation.
            internal float GetMyFloat => MyFloat;
        }

        [Fact]
        public async Task HonorNamingPolicy()
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower };

            string json = """{"my_string":"Hello"}""";
            Assert.Null((await Serializer.DeserializeWrapper<MyStruct_WithNonPublicAccessors_WithTypeAttribute>(json)).MyString);
            Assert.Equal("Hello", (await Serializer.DeserializeWrapper<MyStruct_WithNonPublicAccessors_WithTypeAttribute>(json, options)).MyString);
        }

        [Fact]
        public virtual async Task HonorJsonPropertyName_PrivateGetter()
        {
            string json = """{"prop1":1}""";

            var obj = await Serializer.DeserializeWrapper<StructWithPropertiesWithJsonPropertyName_PrivateGetter>(json);
            Assert.Equal(MySmallEnum.AnotherValue, obj.GetProxy());

            json = await Serializer.SerializeWrapper(obj);
            Assert.Contains("""
                "prop1":1
                """, json);
        }

        [Fact]
        public virtual async Task HonorJsonPropertyName_PrivateSetter()
        {
            string json = """{"prop2":2}""";

            var obj = await Serializer.DeserializeWrapper<StructWithPropertiesWithJsonPropertyName_PrivateSetter>(json);
            Assert.Equal(2, obj.MyInt);

            json = await Serializer.SerializeWrapper(obj);
            Assert.Contains("""
                "prop2":2
                """, json);
        }

        public struct StructWithPropertiesWithJsonPropertyName_PrivateGetter
        {
            [JsonInclude]
            [JsonPropertyName("prop1")]
            public MySmallEnum MyEnum { private get; set; }

            // For test validation.
            internal MySmallEnum GetProxy() => MyEnum;
        }

        public struct StructWithPropertiesWithJsonPropertyName_PrivateSetter
        {
            [JsonInclude]
            [JsonPropertyName("prop2")]
            public int MyInt { get; private set; }

            internal void SetProxy(int myInt) => MyInt = myInt;
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        // Needs support for parameterized ctors.
        [ActiveIssue("https://github.com/dotnet/runtime/issues/45448")]
#endif
        public async Task Map_JsonSerializableProperties_ToCtorArgs()
        {
            var obj = await Serializer.DeserializeWrapper<PointWith_JsonSerializableProperties>("""{"X":1,"Y":2}""");
            Assert.Equal(1, obj.X);
            Assert.Equal(2, obj.GetY);
        }

        private struct PointWith_JsonSerializableProperties
        {
            [JsonInclude]
            public int X { get; internal set; }
            [JsonInclude]
            public int Y { internal get; set;  }

            internal int GetY => Y;

            [JsonConstructor]
            public PointWith_JsonSerializableProperties(int x, int y) => (X, Y) = (x, y);
        }

        [Fact]
        public virtual async Task Public_And_NonPublicPropertyAccessors_PropertyAttributes()
        {
            string json = """{"W":1,"X":2,"Y":3,"Z":4}""";

            var obj = await Serializer.DeserializeWrapper<ClassWithMixedPropertyAccessors_PropertyAttributes>(json);
            Assert.Equal(1, obj.W);
            Assert.Equal(2, obj.X);
            Assert.Equal(3, obj.Y);
            Assert.Equal(4, obj.GetZ);

            json = await Serializer.SerializeWrapper(obj);
            Assert.Contains("""
                "W":1
                """, json);
            Assert.Contains("""
                "X":2
                """, json);
            Assert.Contains("""
                "Y":3
                """, json);
            Assert.Contains("""
                "Z":4
                """, json);
        }

        public class ClassWithMixedPropertyAccessors_PropertyAttributes
        {
            [JsonInclude]
            public int W { get; set; }
            [JsonInclude]
            public int X { get; internal set; }
            [JsonInclude]
            public int Y { get; set; }
            [JsonInclude]
            public int Z { private get; set; }

            internal int GetZ => Z;
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
        public virtual async Task NonPublicProperty_JsonInclude_WorksAsExpected([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
        {
            string json = """{"MyString":"value"}""";
            MemberInfo memberInfo = type.GetMember("MyString", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)[0];

            object result = await Serializer.DeserializeWrapper(json, type);
            Assert.IsType(type, result);
            Assert.Equal("value", memberInfo is PropertyInfo p ? p.GetValue(result) : ((FieldInfo)memberInfo).GetValue(result));

            string actualJson = await Serializer.SerializeWrapper(result, type);
            Assert.Equal(json, actualJson);
        }

        public class ClassWithPrivateProperty_WithJsonIncludeProperty
        {
            [JsonInclude]
            private string MyString { get; set; }
        }

        public class ClassWithInternalProperty_WithJsonIncludeProperty
        {
            [JsonInclude]
            internal string MyString { get; set; }
        }

        public class ClassWithProtectedProperty_WithJsonIncludeProperty
        {
            [JsonInclude]
            protected string MyString { get; private set; }
        }

        public class ClassWithPrivateField_WithJsonIncludeProperty
        {
            [JsonInclude]
            private string MyString = null;

            public override string ToString() => MyString;
        }

        public class ClassWithInternalField_WithJsonIncludeProperty
        {
            [JsonInclude]
            internal string MyString = null;
        }

        public class ClassWithProtectedField_WithJsonIncludeProperty
        {
            [JsonInclude]
            protected string MyString = null;
        }

        public class ClassWithPrivate_InitOnlyProperty_WithJsonIncludeProperty
        {
            [JsonInclude]
            private string MyString { get; init; }
        }

        public class ClassWithInternal_InitOnlyProperty_WithJsonIncludeProperty
        {
            [JsonInclude]
            internal string MyString { get; init; }
        }

        public class ClassWithProtected_InitOnlyProperty_WithJsonIncludeProperty
        {
            [JsonInclude]
            protected string MyString { get; init; }
        }

        [Fact]
        public async Task CanAlwaysRoundtripInternalJsonIncludeProperties()
        {
            // Internal JsonInclude properties should be honored
            // by both reflection and the source generator.

            var value = new ClassWithInternalJsonIncludeProperties { X = 1, Y = 2 };
            string json = await Serializer.SerializeWrapper(value);
            Assert.Equal("""{"X":1,"Y":2}""", json);

            value = await Serializer.DeserializeWrapper<ClassWithInternalJsonIncludeProperties>(json);
            Assert.Equal(1, value.X);
            Assert.Equal(2, value.Y);
        }

        [Fact]
        public virtual void InitOnlyProperties_ExposesSetterDelegate()
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeof(ClassWithJsonIncludePrivateInitOnlyProperties));

            JsonPropertyInfo nameProp = typeInfo.Properties.Single(p => p.Name == "Name");
            Assert.NotNull(nameProp.Get);
            Assert.NotNull(nameProp.Set);
            Assert.Null(nameProp.AssociatedParameter);

            JsonPropertyInfo numberProp = typeInfo.Properties.Single(p => p.Name == "Number");
            Assert.NotNull(numberProp.Get);
            Assert.NotNull(numberProp.Set);
            Assert.Null(numberProp.AssociatedParameter);
        }

        [Theory]
        [InlineData(typeof(ClassWithPrivate_InitOnlyProperty_WithJsonIncludeProperty))]
        [InlineData(typeof(ClassWithInternal_InitOnlyProperty_WithJsonIncludeProperty))]
        [InlineData(typeof(ClassWithProtected_InitOnlyProperty_WithJsonIncludeProperty))]
        public virtual void NonPublicInitOnlyJsonIncludeProperties_HaveNoAssociatedParameterInfo(Type type)
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type);

            JsonPropertyInfo prop = typeInfo.Properties.Single(p => p.Name == "MyString");
            Assert.NotNull(prop.Get);
            Assert.NotNull(prop.Set);
            Assert.Null(prop.AssociatedParameter);
        }

        [Fact]
        public virtual void PrivateJsonIncludeProperties_ExposesGetterAndSetterDelegates()
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeof(ClassWithPrivateJsonIncludeProperties_Roundtrip));

            JsonPropertyInfo nameProp = typeInfo.Properties.Single(p => p.Name == "Name");
            Assert.NotNull(nameProp.Get);
            Assert.NotNull(nameProp.Set);

            JsonPropertyInfo ageProp = typeInfo.Properties.Single(p => p.Name == "Age");
            Assert.NotNull(ageProp.Get);
            Assert.NotNull(ageProp.Set);
        }

        [Fact]
        public virtual void PrivateJsonIncludeGetterOnly_ExposesGetterDelegate()
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeof(ClassWithJsonIncludePrivateGetterProperties));

            JsonPropertyInfo nameProp = typeInfo.Properties.Single(p => p.Name == "Name");
            Assert.NotNull(nameProp.Get);
            Assert.NotNull(nameProp.Set);

            JsonPropertyInfo numberProp = typeInfo.Properties.Single(p => p.Name == "Number");
            Assert.NotNull(numberProp.Get);
            Assert.NotNull(numberProp.Set);
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
        public virtual void NonPublicJsonIncludeMembers_ExposeGetterAndSetterDelegates(Type type)
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(type);

            JsonPropertyInfo prop = typeInfo.Properties.Single(p => p.Name == "MyString");
            Assert.NotNull(prop.Get);
            Assert.NotNull(prop.Set);
        }

        [Fact]
        public virtual void MixedAccessibilityJsonIncludeProperties_AllExposeGetterAndSetterDelegates()
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeof(ClassWithMixedAccessibilityJsonIncludeProperties));
            Assert.Equal(4, typeInfo.Properties.Count);

            foreach (JsonPropertyInfo prop in typeInfo.Properties)
            {
                Assert.NotNull(prop.Get);
                Assert.NotNull(prop.Set);
            }
        }

        [Fact]
        public virtual void StructWithPrivateJsonIncludeProperties_ExposesGetterAndSetterDelegates()
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeof(StructWithJsonIncludePrivateProperties));

            JsonPropertyInfo nameProp = typeInfo.Properties.Single(p => p.Name == "Name");
            Assert.NotNull(nameProp.Get);
            Assert.NotNull(nameProp.Set);

            JsonPropertyInfo numberProp = typeInfo.Properties.Single(p => p.Name == "Number");
            Assert.NotNull(numberProp.Get);
            Assert.NotNull(numberProp.Set);
        }

        [Fact]
        public void PublicPropertyWithNonPublicAccessors_WithoutJsonInclude_ExposesOnlyPublicAccessors()
        {
            JsonTypeInfo typeInfo = Serializer.GetTypeInfo(typeof(ClassWithInternalJsonIncludeProperties));

            // X has public get, internal set (with [JsonInclude]) - both should be exposed
            JsonPropertyInfo xProp = typeInfo.Properties.Single(p => p.Name == "X");
            Assert.NotNull(xProp.Get);
            Assert.NotNull(xProp.Set);

            // Y has internal get (with [JsonInclude]), public set - both should be exposed
            JsonPropertyInfo yProp = typeInfo.Properties.Single(p => p.Name == "Y");
            Assert.NotNull(yProp.Get);
            Assert.NotNull(yProp.Set);
        }

        public class ClassWithInternalJsonIncludeProperties
        {
            [JsonInclude]
            public int X { get; internal set; }
            [JsonInclude]
            public int Y { internal get; set; }
        }

        [Fact]
        public virtual async Task ClassWithIgnoredAndPrivateMembers_DoesNotIncludeIgnoredMetadata()
        {
            JsonSerializerOptions options = Serializer.CreateOptions(includeFields: true);

            JsonTypeInfo typeInfo = options.GetTypeInfo(typeof(ClassWithIgnoredAndPrivateMembers));

            // The contract surfaces the ignored properties but not the private ones
            Assert.Equal(2, typeInfo.Properties.Count);
            Assert.Contains(typeInfo.Properties, prop => prop.Name == "PublicIgnoredField");
            Assert.Contains(typeInfo.Properties, prop => prop.Name == "PublicIgnoredProperty");

            // The ignored properties included in the contract do not specify any accessor delegates.
            Assert.All(typeInfo.Properties, prop => Assert.True(prop.Get is null));
            Assert.All(typeInfo.Properties, prop => Assert.True(prop.Set is null));

            string json = await Serializer.SerializeWrapper(new ClassWithIgnoredAndPrivateMembers(), options);
            Assert.Equal("{}", json);
        }

        public class ClassWithIgnoredAndPrivateMembers
        {
            [JsonIgnore]
            public TypeThatShouldNotBeGenerated PublicIgnoredField = new();

            private TypeThatShouldNotBeGenerated PrivateField = new();

            [JsonIgnore]
            public TypeThatShouldNotBeGenerated PublicIgnoredProperty { get; set; } = new();

            private TypeThatShouldNotBeGenerated PrivateProperty { get; set; } = new();
        }

        public class TypeThatShouldNotBeGenerated
        {
            private protected object _thisLock = new object();
        }

        // ---- Extensive test types for [JsonInclude] with inaccessible members ----

        public class ClassWithPrivateJsonIncludeProperties_Roundtrip
        {
            [JsonInclude]
            private string Name { get; set; } = "default";
            [JsonInclude]
            private int Age { get; set; }

            public static ClassWithPrivateJsonIncludeProperties_Roundtrip Create(string name, int age)
            {
                var obj = new ClassWithPrivateJsonIncludeProperties_Roundtrip();
                obj.Name = name;
                obj.Age = age;
                return obj;
            }

            // For test validation.
            internal string GetName() => Name;
            internal int GetAge() => Age;
        }

        public class ClassWithProtectedJsonIncludeProperties_Roundtrip
        {
            [JsonInclude]
            protected string Name { get; set; } = "default";
            [JsonInclude]
            protected int Age { get; set; }

            public static ClassWithProtectedJsonIncludeProperties_Roundtrip Create(string name, int age)
            {
                var obj = new ClassWithProtectedJsonIncludeProperties_Roundtrip();
                obj.Name = name;
                obj.Age = age;
                return obj;
            }

            // For test validation.
            internal string GetName() => Name;
            internal int GetAge() => Age;
        }

        public class ClassWithMixedAccessibilityJsonIncludeProperties
        {
            [JsonInclude]
            public int PublicProp { get; set; }
            [JsonInclude]
            internal int InternalProp { get; set; }
            [JsonInclude]
            private int PrivateProp { get; set; }
            [JsonInclude]
            protected int ProtectedProp { get; set; }

            internal int GetPrivateProp() => PrivateProp;
            internal int GetProtectedProp() => ProtectedProp;
        }

        public class ClassWithJsonIncludePrivateInitOnlyProperties
        {
            [JsonInclude]
            public string Name { get; private init; } = "DefaultName";
            [JsonInclude]
            public int Number { get; private init; } = 42;
        }

        public class ClassWithJsonIncludePrivateGetterProperties
        {
            [JsonInclude]
            public string Name { private get; set; } = "DefaultName";
            [JsonInclude]
            public int Number { private get; set; } = 42;

            internal string GetName() => Name;
            internal int GetNumber() => Number;
        }

        public struct StructWithJsonIncludePrivateProperties
        {
            [JsonInclude]
            private string Name { get; set; }
            [JsonInclude]
            private int Number { get; set; }

            public static StructWithJsonIncludePrivateProperties Create(string name, int number) =>
                new StructWithJsonIncludePrivateProperties { Name = name, Number = number };

            internal readonly string GetName() => Name;
            internal readonly int GetNumber() => Number;
        }

        public class GenericClassWithPrivateJsonIncludeProperties<T>
        {
            [JsonInclude]
            private T Value { get; set; }

            [JsonInclude]
            private string Label { get; set; } = "default";

            public static GenericClassWithPrivateJsonIncludeProperties<T> Create(T value, string label)
            {
                var obj = new GenericClassWithPrivateJsonIncludeProperties<T>();
                obj.Value = value;
                obj.Label = label;
                return obj;
            }

            public T GetValue() => Value;
            public string GetLabel() => Label;
        }

        [Fact]
        public virtual async Task JsonInclude_PrivateProperties_CanRoundtrip()
        {
            var obj = ClassWithPrivateJsonIncludeProperties_Roundtrip.Create("Test", 25);
            string json = await Serializer.SerializeWrapper(obj);
            Assert.Contains(@"""Name"":""Test""", json);
            Assert.Contains(@"""Age"":25", json);

            var deserialized = await Serializer.DeserializeWrapper<ClassWithPrivateJsonIncludeProperties_Roundtrip>(json);
            Assert.Equal("Test", deserialized.GetName());
            Assert.Equal(25, deserialized.GetAge());
        }

        [Fact]
        public virtual async Task JsonInclude_ProtectedProperties_CanRoundtrip()
        {
            var obj = ClassWithProtectedJsonIncludeProperties_Roundtrip.Create("Test", 25);
            string json = await Serializer.SerializeWrapper(obj);
            Assert.Contains(@"""Name"":""Test""", json);
            Assert.Contains(@"""Age"":25", json);

            var deserialized = await Serializer.DeserializeWrapper<ClassWithProtectedJsonIncludeProperties_Roundtrip>(json);
            Assert.Equal("Test", deserialized.GetName());
            Assert.Equal(25, deserialized.GetAge());
        }

        [Fact]
        public virtual async Task JsonInclude_MixedAccessibility_AllPropertiesRoundtrip()
        {
            string json = """{"PublicProp":1,"InternalProp":2,"PrivateProp":3,"ProtectedProp":4}""";
            var deserialized = await Serializer.DeserializeWrapper<ClassWithMixedAccessibilityJsonIncludeProperties>(json);
            Assert.Equal(1, deserialized.PublicProp);
            Assert.Equal(2, deserialized.InternalProp);
            Assert.Equal(3, deserialized.GetPrivateProp());
            Assert.Equal(4, deserialized.GetProtectedProp());

            string actualJson = await Serializer.SerializeWrapper(deserialized);
            Assert.Contains(@"""PublicProp"":1", actualJson);
            Assert.Contains(@"""InternalProp"":2", actualJson);
            Assert.Contains(@"""PrivateProp"":3", actualJson);
            Assert.Contains(@"""ProtectedProp"":4", actualJson);
        }

        [Fact]
        public virtual async Task JsonInclude_PrivateInitOnlyProperties_PreservesDefaults()
        {
            // Deserializing empty JSON should preserve default values.
            var deserialized = await Serializer.DeserializeWrapper<ClassWithJsonIncludePrivateInitOnlyProperties>("{}");
            Assert.Equal("DefaultName", deserialized.Name);
            Assert.Equal(42, deserialized.Number);

            // Deserializing with values should override defaults.
            deserialized = await Serializer.DeserializeWrapper<ClassWithJsonIncludePrivateInitOnlyProperties>("""{"Name":"Override","Number":100}""");
            Assert.Equal("Override", deserialized.Name);
            Assert.Equal(100, deserialized.Number);

            // Serialization should work.
            string json = await Serializer.SerializeWrapper(deserialized);
            Assert.Contains(@"""Name"":""Override""", json);
            Assert.Contains(@"""Number"":100", json);
        }

        [Fact]
        public virtual async Task JsonInclude_PrivateGetterProperties_CanSerialize()
        {
            var obj = new ClassWithJsonIncludePrivateGetterProperties { Name = "Test", Number = 99 };
            string json = await Serializer.SerializeWrapper(obj);
            Assert.Contains(@"""Name"":""Test""", json);
            Assert.Contains(@"""Number"":99", json);

            var deserialized = await Serializer.DeserializeWrapper<ClassWithJsonIncludePrivateGetterProperties>(json);
            Assert.Equal("Test", deserialized.GetName());
            Assert.Equal(99, deserialized.GetNumber());
        }

        [Fact]
        public virtual async Task JsonInclude_PrivateProperties_EmptyJson_DeserializesToDefault()
        {
            var deserialized = await Serializer.DeserializeWrapper<ClassWithPrivateJsonIncludeProperties_Roundtrip>("{}");
            Assert.Equal("default", deserialized.GetName());
            Assert.Equal(0, deserialized.GetAge());
        }

        [Fact]
        public virtual async Task JsonInclude_StructWithPrivateProperties_CanRoundtrip()
        {
            var obj = StructWithJsonIncludePrivateProperties.Create("Hello", 42);
            string json = await Serializer.SerializeWrapper(obj);
            Assert.Contains(@"""Name"":""Hello""", json);
            Assert.Contains(@"""Number"":42", json);

            var deserialized = await Serializer.DeserializeWrapper<StructWithJsonIncludePrivateProperties>(json);
            Assert.Equal("Hello", deserialized.GetName());
            Assert.Equal(42, deserialized.GetNumber());
        }

        [Fact]
        public virtual async Task JsonInclude_GenericType_PrivateProperties_CanRoundtrip()
        {
            var obj = GenericClassWithPrivateJsonIncludeProperties<int>.Create(42, "test");
            string json = await Serializer.SerializeWrapper(obj);
            Assert.Contains(@"""Value"":42", json);
            Assert.Contains(@"""Label"":""test""", json);

            var deserialized = await Serializer.DeserializeWrapper<GenericClassWithPrivateJsonIncludeProperties<int>>(json);
            Assert.Equal(42, deserialized.GetValue());
            Assert.Equal("test", deserialized.GetLabel());
        }

        public struct GenericStructWithPrivateJsonIncludeProperties<T>
        {
            [JsonInclude]
            private T Value { get; set; }

            [JsonInclude]
            private string Label { get; set; }
        }

        [Theory]
        [InlineData(typeof(GenericStructWithPrivateJsonIncludeProperties<int>), """{"Value":42,"Label":"test"}""")]
        [InlineData(typeof(GenericStructWithPrivateJsonIncludeProperties<string>), """{"Value":"hello","Label":"test"}""")]
        public async Task JsonInclude_GenericStruct_PrivateProperties_CanRoundtrip(Type type, string json)
        {
            object result = await Serializer.DeserializeWrapper(json, type);
            JsonTestHelper.AssertJsonEqual(json, await Serializer.SerializeWrapper(result, type));
        }

        public struct GenericStructWithThrowingAccessors<T>
        {
            [JsonInclude]
            private T Value
            {
                [MethodImpl(MethodImplOptions.NoInlining)]
                get => throw new InvalidOperationException("Getter failure.");

                [MethodImpl(MethodImplOptions.NoInlining)]
                set => throw new InvalidOperationException("Setter failure.");
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task JsonInclude_GenericStruct_ThrowingAccessors_PropagateOriginalException(bool serialize)
        {
            Func<Task> action = serialize
                ? () => Serializer.SerializeWrapper(default(GenericStructWithThrowingAccessors<int>))
                : () => Serializer.DeserializeWrapper<GenericStructWithThrowingAccessors<int>>("""{"Value":42}""");

            InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(action);
            Assert.Equal(serialize ? "Getter failure." : "Setter failure.", exception.Message);
            Assert.Contains(nameof(GenericStructWithThrowingAccessors<int>), exception.StackTrace);
        }

        public class GenericClassWithReadOnlyJsonIncludeMembers<T>
        {
            [JsonInclude]
            [JsonPropertyName("Value")]
            private readonly T _value;

            [JsonInclude]
            private string Label { get; } = "initial";

            public GenericClassWithReadOnlyJsonIncludeMembers() : this(default!) { }

            public GenericClassWithReadOnlyJsonIncludeMembers(T value) => _value = value;

            public T GetValue() => _value;
        }

        [Theory]
        [InlineData("""{"Value":42}""")]
        [InlineData("""{"Label":"changed"}""")]
        public async Task JsonInclude_GenericReadOnlyMembers_AreNotDeserialized(string json)
        {
            var result = await Serializer.DeserializeWrapper<GenericClassWithReadOnlyJsonIncludeMembers<int>>(json);
            Assert.Equal(0, result.GetValue());
            JsonTestHelper.AssertJsonEqual("""{"Value":0,"Label":"initial"}""", await Serializer.SerializeWrapper(result));
        }

        public class GenericClassWithPrivateJsonIncludeFields<T>
        {
            [JsonInclude]
            [JsonPropertyName("Value")]
            private T _value;

            [JsonInclude]
            [JsonPropertyName("Label")]
            private string _label;

            public GenericClassWithPrivateJsonIncludeFields() : this(default!, "initial") { }

            public GenericClassWithPrivateJsonIncludeFields(T value, string label)
            {
                _value = value;
                _label = label;
            }

            public T GetValue() => _value;
            public string GetLabel() => _label;
        }

        public struct GenericStructWithPrivateJsonIncludeFields<T>
        {
            [JsonInclude]
            [JsonPropertyName("Value")]
            private T _value;

            [JsonInclude]
            [JsonPropertyName("Label")]
            private string _label;

            public GenericStructWithPrivateJsonIncludeFields(T value, string label)
            {
                _value = value;
                _label = label;
            }

            public readonly T GetValue() => _value;
            public readonly string GetLabel() => _label;
        }

        [Theory]
        [InlineData(typeof(GenericClassWithPrivateJsonIncludeFields<int>), """{"Value":42,"Label":"test"}""")]
        [InlineData(typeof(GenericClassWithPrivateJsonIncludeFields<string>), """{"Value":"hello","Label":"test"}""")]
        [InlineData(typeof(GenericStructWithPrivateJsonIncludeFields<int>), """{"Value":42,"Label":"test"}""")]
        [InlineData(typeof(GenericStructWithPrivateJsonIncludeFields<string>), """{"Value":"hello","Label":"test"}""")]
        public async Task JsonInclude_GenericFields_CanRoundtrip(Type type, string json)
        {
            object result = await Serializer.DeserializeWrapper(json, type);
            JsonTestHelper.AssertJsonEqual(json, await Serializer.SerializeWrapper(result, type));
        }

        public struct StructWithPrivateConstructorAndMembers
        {
            public int Value { get; }

            [JsonInclude, JsonPropertyName("Field")]
            private int _field;

            [JsonInclude, JsonPropertyName("ReadOnly")]
            private readonly int _readOnly;

            [JsonInclude]
            private string Property { get; set; }

            [JsonInclude]
            private string InitOnly { get; init; }

            [JsonConstructor]
            private StructWithPrivateConstructorAndMembers(int value)
            {
                Value = value;
                _field = -1;
                _readOnly = 23;
                Property = "initial";
                InitOnly = "initial";
            }

            public readonly int GetField() => _field;
            public readonly int GetReadOnly() => _readOnly;
        }

        public struct GenericStructWithPrivateInitOnlyMembers<T>
        {
            public T Value { get; }

            [JsonInclude, JsonPropertyName("ReadOnly")]
            private readonly T _readOnly;

            [JsonInclude]
            private T InitOnly { get; init; }

            [JsonConstructor]
            private GenericStructWithPrivateInitOnlyMembers(T value)
            {
                Value = value;
                _readOnly = value;
                InitOnly = default!;
            }
        }

        [Theory]
        [InlineData(typeof(StructWithPrivateConstructorAndMembers),
            """{"Value":42}""",
            """{"Value":42,"Field":-1,"ReadOnly":23,"Property":"initial","InitOnly":"initial"}""")]
        [InlineData(typeof(StructWithPrivateConstructorAndMembers),
            """{"Value":42,"Field":17,"ReadOnly":99,"Property":"updated","InitOnly":"initialized"}""",
            """{"Value":42,"Field":17,"ReadOnly":23,"Property":"updated","InitOnly":"initialized"}""")]
        [InlineData(typeof(GenericStructWithPrivateInitOnlyMembers<int>),
            """{"Value":42,"ReadOnly":99,"InitOnly":17}""",
            """{"Value":42,"ReadOnly":42,"InitOnly":17}""")]
        [InlineData(typeof(GenericStructWithPrivateInitOnlyMembers<string>),
            """{"Value":"constructor","ReadOnly":"ignored","InitOnly":"initialized"}""",
            """{"Value":"constructor","ReadOnly":"constructor","InitOnly":"initialized"}""")]
        public async Task JsonInclude_Struct_PrivateConstructorAndMembers_CanRoundtrip(Type type, string json, string expectedJson)
        {
            object result = await Serializer.DeserializeWrapper(json, type);
            Assert.IsType(type, result);
            JsonTestHelper.AssertJsonEqual(expectedJson, await Serializer.SerializeWrapper(result, type));
        }

        public class GenericMemberOuter<TOuter>
        {
            public class Nested<TInner>
            {
                [JsonInclude]
                private TOuter OuterValue { get; set; }

                public TInner InnerValue { get; init; }
            }

            public class Nested
            {
                [JsonInclude]
                private TOuter Value { get; set; }
            }
        }

        public class GenericAccessorBase<T>
        {
            [JsonInclude]
            [JsonPropertyName("BaseValue")]
            private T Value { get; set; }
        }

        public class GenericAccessorDerived<T> : GenericAccessorBase<T>
        {
            [JsonInclude]
            [JsonPropertyName("DerivedValue")]
            private T Value { get; set; }
        }

        public class NonGenericAccessorDerived : GenericAccessorBase<int> { }

        public class GenericAccessorGrandBase<TGrand> where TGrand : class
        {
            [JsonInclude]
            private TGrand GrandSecret { get; set; } = default!;
        }

        public class GenericAccessorIntermediate<TBase> : GenericAccessorGrandBase<string> where TBase : struct
        {
            [JsonInclude]
            private TBase BaseSecret { get; set; }
        }

        public class NonGenericAccessorLeaf : GenericAccessorIntermediate<int> { }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task JsonInclude_MultipleGenericBaseTypes_CanRoundtrip(bool deserialize)
        {
            const string PopulatedJson = """{"BaseSecret":42,"GrandSecret":"secret"}""";
            const string DefaultJson = """{"BaseSecret":0,"GrandSecret":null}""";

            NonGenericAccessorLeaf value = deserialize
                ? await Serializer.DeserializeWrapper<NonGenericAccessorLeaf>(PopulatedJson)
                : new NonGenericAccessorLeaf();

            JsonTestHelper.AssertJsonEqual(
                deserialize ? PopulatedJson : DefaultJson,
                await Serializer.SerializeWrapper(value));
        }

        public class GenericAccessorDerivedWithPublicProperty<T> : GenericAccessorBase<T>
        {
            public T DerivedValue { get; set; }
        }

        public class GenericAccessorDerivedWithIgnoredProperty<T> : GenericAccessorBase<T>
        {
            [JsonIgnore]
            public T IgnoredValue { get; init; }
        }

        public class GenericAccessorDerived<TFirst, TSecond> : GenericAccessorBase<TFirst>
        {
            [JsonInclude]
            [JsonPropertyName("DerivedValue")]
            private TSecond Value { get; set; }
        }

        public class GenericMembersWithKeywordParameter<@class> where @class : struct
        {
            [JsonInclude]
            private @class Value { get; set; }

            public @class Other { get; init; }
        }

        public class ConstrainedGenericAccessor<TValue, TCollection>
            where TValue : struct
            where TCollection : ICollection<TValue>, new()
        {
            [JsonInclude]
            private TValue Value { get; set; }

            public TCollection Values { get; init; } = new();
        }

        [Theory]
        [InlineData(typeof(GenericMemberOuter<int>.Nested<string>), """{"OuterValue":42,"InnerValue":"test"}""")]
        [InlineData(typeof(GenericMemberOuter<int>.Nested), """{"Value":42}""")]
        [InlineData(typeof(GenericAccessorDerived<int>), """{"BaseValue":1,"DerivedValue":2}""")]
        [InlineData(typeof(NonGenericAccessorDerived), """{"BaseValue":42}""")]
        [InlineData(typeof(GenericAccessorDerivedWithPublicProperty<int>), """{"BaseValue":1,"DerivedValue":2}""")]
        [InlineData(typeof(GenericAccessorDerivedWithIgnoredProperty<int>), """{"BaseValue":42}""")]
        [InlineData(typeof(GenericAccessorDerived<int, string>), """{"BaseValue":42,"DerivedValue":"test"}""")]
        [InlineData(typeof(GenericMembersWithKeywordParameter<int>), """{"Value":42,"Other":7}""")]
        [InlineData(typeof(ConstrainedGenericAccessor<int, List<int>>), """{"Value":42,"Values":[1,2]}""")]
        public async Task JsonInclude_GenericNestedAndInheritedMembers_CanRoundtrip(Type type, string json)
        {
            object result = await Serializer.DeserializeWrapper(json, type);
            JsonTestHelper.AssertJsonEqual(json, await Serializer.SerializeWrapper(result, type));
        }

        public class ConstraintBase
        {
            public int Id { get; set; }
        }

        public class ConstraintDerived : ConstraintBase
        {
            public string Extra { get; set; } = "";
        }

        public class ConstrainedGenericClassWithInitOnlyProperties<T> where T : notnull, ConstraintBase
        {
            public required T Response { get; init; }
            public required string Name { get; init; }
        }

        [Fact]
        public virtual async Task InitOnlyProperties_GenericTypeWithConstraints_CanRoundtrip()
        {
            var derived = new ConstraintDerived { Id = 1, Extra = "extra" };
            string json = await Serializer.SerializeWrapper(
                new ConstrainedGenericClassWithInitOnlyProperties<ConstraintDerived> { Response = derived, Name = "test" });
            Assert.Contains(@"""Name"":""test""", json);
            Assert.Contains(@"""Id"":1", json);

            var deserialized = await Serializer.DeserializeWrapper<ConstrainedGenericClassWithInitOnlyProperties<ConstraintDerived>>(json);
            Assert.Equal("test", deserialized.Name);
            Assert.Equal(1, deserialized.Response.Id);
            Assert.Equal("extra", deserialized.Response.Extra);
        }

        public class ConstrainedGenericClassWithOptionalInitOnlyProperties<T> where T : notnull, ConstraintBase
        {
            public T? Response { get; init; }
            public string Name { get; init; } = "default";
        }

        [Fact]
        public async Task InitOnlyProperties_ConstrainedGenericType_PreservesDefaults()
        {
            var value = new ConstrainedGenericClassWithOptionalInitOnlyProperties<ConstraintDerived>
            {
                Response = new ConstraintDerived { Id = 2, Extra = "optional" },
                Name = "updated"
            };

            string json = await Serializer.SerializeWrapper(value);
            var deserialized = await Serializer.DeserializeWrapper<ConstrainedGenericClassWithOptionalInitOnlyProperties<ConstraintDerived>>(json);
            Assert.Equal(2, deserialized.Response.Id);
            Assert.Equal("optional", deserialized.Response.Extra);
            Assert.Equal("updated", deserialized.Name);

            deserialized = await Serializer.DeserializeWrapper<ConstrainedGenericClassWithOptionalInitOnlyProperties<ConstraintDerived>>("{}");
            Assert.Null(deserialized.Response);
            Assert.Equal("default", deserialized.Name);
        }
    }
}

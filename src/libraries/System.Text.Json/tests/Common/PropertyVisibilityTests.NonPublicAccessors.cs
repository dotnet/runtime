// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class PropertyVisibilityTests
    {
        [Fact]
        public async Task NonPublic_AccessorsNotSupported_WithoutAttribute()
        {
            string json = @"{
                ""MyInt"":1,
                ""MyString"":""Hello"",
                ""MyFloat"":2,
                ""MyUri"":""https://microsoft.com""
            }";

            var obj = await JsonSerializerWrapperForString.DeserializeWrapper<MyClass_WithNonPublicAccessors>(json);
            Assert.Equal(0, obj.MyInt);
            Assert.Null(obj.MyString);
            Assert.Equal(2f, obj.GetMyFloat);
            Assert.Equal(new Uri("https://microsoft.com"), obj.MyUri);

            json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            Assert.Contains(@"""MyInt"":0", json);
            Assert.Contains(@"""MyString"":null", json);
            Assert.DoesNotContain(@"""MyFloat"":", json);
            Assert.DoesNotContain(@"""MyUri"":", json);
        }

        public class MyClass_WithNonPublicAccessors
        {
            public int MyInt { get; private set; }
            public string MyString { get; internal set; }
            public float MyFloat { private get; set; }
            public Uri MyUri { internal get; set; }

            // For test validation.
            internal float GetMyFloat => MyFloat;
        }

        [Fact]
        public virtual async Task Honor_JsonSerializablePropertyAttribute_OnProperties()
        {
            string json = @"{
                ""MyInt"":1,
                ""MyString"":""Hello"",
                ""MyFloat"":2,
                ""MyUri"":""https://microsoft.com""
            }";

            var obj = await JsonSerializerWrapperForString.DeserializeWrapper<MyClass_WithNonPublicAccessors_WithPropertyAttributes>(json);
            Assert.Equal(1, obj.MyInt);
            Assert.Equal("Hello", obj.MyString);
            Assert.Equal(2f, obj.GetMyFloat);
            Assert.Equal(new Uri("https://microsoft.com"), obj.MyUri);

            json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            Assert.Contains(@"""MyInt"":1", json);
            Assert.Contains(@"""MyString"":""Hello""", json);
            Assert.Contains(@"""MyFloat"":2", json);
            Assert.Contains(@"""MyUri"":""https://microsoft.com""", json);
        }

        public class MyClass_WithNonPublicAccessors_WithPropertyAttributes
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
            string json = @"{""Key"":""Value""}";

            // Baseline
            var obj1 = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithExtensionData_NonPublicSetter>(json);
            Assert.Null(obj1.ExtensionData);
            Assert.Equal("{}", await JsonSerializerWrapperForString.SerializeWrapper(obj1));

            // With attribute
            var obj2 = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithExtensionData_NonPublicSetter_WithAttribute>(json);
            Assert.Equal("Value", obj2.ExtensionData["Key"].GetString());
            Assert.Equal(json, await JsonSerializerWrapperForString.SerializeWrapper(obj2));
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
            options.Converters.Add(new JsonStringEnumConverter());

            string json = @"{""MyEnum"":""AnotherValue"",""MyInt"":2}";

            // Deserialization baseline, without enum converter, we get JsonException.
            await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StructWithPropertiesWithConverter>(json));

            var obj = await JsonSerializerWrapperForString.DeserializeWrapper<StructWithPropertiesWithConverter>(json, options);
            Assert.Equal(MySmallEnum.AnotherValue, obj.GetMyEnum);
            Assert.Equal(25, obj.MyInt);

            // ConverterForInt32 throws this exception.
            await Assert.ThrowsAsync<NotImplementedException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(obj, options));
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

            string json = @"{""MYSTRING"":""Hello""}";
            Assert.Null((await JsonSerializerWrapperForString.DeserializeWrapper<MyStruct_WithNonPublicAccessors_WithTypeAttribute>(json)).MyString);
            Assert.Equal("Hello", (await JsonSerializerWrapperForString.DeserializeWrapper<MyStruct_WithNonPublicAccessors_WithTypeAttribute>(json, options)).MyString);
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
            var options = new JsonSerializerOptions { PropertyNamingPolicy = new SimpleSnakeCasePolicy() };

            string json = @"{""my_string"":""Hello""}";
            Assert.Null((await JsonSerializerWrapperForString.DeserializeWrapper<MyStruct_WithNonPublicAccessors_WithTypeAttribute>(json)).MyString);
            Assert.Equal("Hello", (await JsonSerializerWrapperForString.DeserializeWrapper<MyStruct_WithNonPublicAccessors_WithTypeAttribute>(json, options)).MyString);
        }

        [Fact]
        public virtual async Task HonorJsonPropertyName()
        {
            string json = @"{""prop1"":1,""prop2"":2}";

            var obj = await JsonSerializerWrapperForString.DeserializeWrapper<StructWithPropertiesWithJsonPropertyName>(json);
            Assert.Equal(MySmallEnum.AnotherValue, obj.GetMyEnum);
            Assert.Equal(2, obj.MyInt);

            json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            Assert.Contains(@"""prop1"":1", json);
            Assert.Contains(@"""prop2"":2", json);
        }

        public struct StructWithPropertiesWithJsonPropertyName
        {
            [JsonInclude]
            [JsonPropertyName("prop1")]
            public MySmallEnum MyEnum { private get; set; }

            [JsonInclude]
            [JsonPropertyName("prop2")]
            public int MyInt { get; private set; }

            // For test validation.
            internal MySmallEnum GetMyEnum => MyEnum;
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        // Needs support for parameterized ctors.
        [ActiveIssue("https://github.com/dotnet/runtime/issues/45448")]
#endif
        public async Task Map_JsonSerializableProperties_ToCtorArgs()
        {
            var obj = await JsonSerializerWrapperForString.DeserializeWrapper<PointWith_JsonSerializableProperties>(@"{""X"":1,""Y"":2}");
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
            Assert.Contains(@"""Z"":4", json);
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
        public virtual async Task NonPublicProperty_WithJsonInclude_Invalid(Type type)
        {
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper("{}", type));
            string exAsStr = ex.ToString();
            Assert.Contains("MyString", exAsStr);
            Assert.Contains(type.ToString(), exAsStr);
            Assert.Contains("JsonIncludeAttribute", exAsStr);

            ex = await Assert.ThrowsAsync<InvalidOperationException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(Activator.CreateInstance(type), type));
            exAsStr = ex.ToString();
            Assert.Contains("MyString", exAsStr);
            Assert.Contains(type.ToString(), exAsStr);
            Assert.Contains("JsonIncludeAttribute", exAsStr);
        }

        public class ClassWithPrivateProperty_WithJsonIncludeProperty
        {
            [JsonInclude]
            private string MyString { get; set; }
        }

        public class ClassWithInternalProperty_WithJsonIncludeProperty
        {
            [JsonInclude]
            internal string MyString { get; }
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
    }
}

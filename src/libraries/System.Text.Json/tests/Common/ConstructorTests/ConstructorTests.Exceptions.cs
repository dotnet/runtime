// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class ConstructorTests
    {
        [Fact]
        public async Task MultipleProperties_Cannot_BindTo_TheSame_ConstructorParameter()
        {
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => JsonSerializerWrapperForString.DeserializeWrapper<Point_MultipleMembers_BindTo_OneConstructorParameter>("{}"));

            string exStr = ex.ToString();
            Assert.Contains("'X'", exStr);
            Assert.Contains("'x'", exStr);
            Assert.Contains("System.Text.Json.Serialization.Tests.Point_MultipleMembers_BindTo_OneConstructorParameter", exStr);

            ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => JsonSerializerWrapperForString.DeserializeWrapper<Point_MultipleMembers_BindTo_OneConstructorParameter_Variant>("{}"));

            exStr = ex.ToString();
            Assert.Contains("'X'", exStr);
            Assert.Contains("'x'", exStr);
            Assert.Contains("Point_MultipleMembers_BindTo_OneConstructorParameter_Variant", exStr);

            ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => JsonSerializerWrapperForString.DeserializeWrapper<Url_BindTo_OneConstructorParameter>("{}"));

            exStr = ex.ToString();
            Assert.Contains("'URL'", exStr);
            Assert.Contains("'Url'", exStr);
            Assert.Contains("Url_BindTo_OneConstructorParameter", exStr);
        }

        [Fact]
        public async Task All_ConstructorParameters_MustBindTo_ObjectMembers()
        {
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => JsonSerializerWrapperForString.DeserializeWrapper<Point_Without_Members>("{}"));

            string exStr = ex.ToString();
            Assert.Contains("System.Text.Json.Serialization.Tests.Point_Without_Members", exStr);

            ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => JsonSerializerWrapperForString.DeserializeWrapper<Point_With_MismatchedMembers>("{}"));
            exStr = ex.ToString();
            Assert.Contains("System.Text.Json.Serialization.Tests.Point_With_MismatchedMembers", exStr);

            ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => JsonSerializerWrapperForString.DeserializeWrapper<WrapperFor_Point_With_MismatchedMembers>(@"{""MyInt"":1,""MyPoint"":{}}"));
            exStr = ex.ToString();
            Assert.Contains("System.Text.Json.Serialization.Tests.Point_With_MismatchedMembers", exStr);
        }

        [Fact]
        public async Task LeadingReferenceMetadataNotSupported()
        {
            string json = @"{""$id"":""1"",""Name"":""Jet"",""Manager"":{""$ref"":""1""}}";

            // Metadata ignored by default.
            var employee = await JsonSerializerWrapperForString.DeserializeWrapper<Employee>(json);

            Assert.Equal("Jet", employee.Name);
            Assert.Null(employee.Manager.Name);
            Assert.Null(employee.Manager.Manager);

            // Metadata not supported with preserve ref feature on.

            var options = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve };

            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(
                () => JsonSerializerWrapperForString.DeserializeWrapper<Employee>(json, options));

            string exStr = ex.ToString();
            Assert.Contains("System.Text.Json.Serialization.Tests.ConstructorTests+Employee", exStr);
            Assert.Contains("$.$id", exStr);
        }

        public class Employee
        {
            public string Name { get; }
            public Employee Manager { get; set; }

            public Employee(string name)
            {
                Name = name;
            }
        }

        [Fact]
        public async Task RandomReferenceMetadataNotSupported()
        {
            string json = @"{""Name"":""Jet"",""$random"":10}";

            // Baseline, preserve ref feature off.

            var employee = await JsonSerializerWrapperForString.DeserializeWrapper<Employee>(json);

            Assert.Equal("Jet", employee.Name);

            // Metadata not supported with preserve ref feature on.

            var options = new JsonSerializerOptions { ReferenceHandler = ReferenceHandler.Preserve };

            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Employee>(json, options));
            string exStr = ex.ToString();
            Assert.Contains("System.Text.Json.Serialization.Tests.ConstructorTests+Employee", exStr);
            Assert.Contains("$.$random", exStr);
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Needs JsonExtensionData support.")]
#endif
        public async Task ExtensionDataProperty_CannotBindTo_CtorParam()
        {
            InvalidOperationException ex = await Assert.ThrowsAsync<InvalidOperationException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Class_ExtData_CtorParam>("{}"));
            string exStr = ex.ToString(); // System.InvalidOperationException: 'The extension data property 'System.Collections.Generic.Dictionary`2[System.String,System.Text.Json.JsonElement] ExtensionData' on type 'System.Text.Json.Serialization.Tests.ConstructorTests+Class_ExtData_CtorParam' cannot bind with a parameter in constructor 'Void .ctor(System.Collections.Generic.Dictionary`2[System.String,System.Text.Json.JsonElement])'.'
            Assert.Contains("System.Collections.Generic.Dictionary`2[System.String,System.Text.Json.JsonElement] ExtensionData", exStr);
            Assert.Contains("System.Text.Json.Serialization.Tests.ConstructorTests+Class_ExtData_CtorParam", exStr);
        }

        public class Class_ExtData_CtorParam
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }

            public Class_ExtData_CtorParam(Dictionary<string, JsonElement> extensionData) { }
        }

        [Fact]
        public async Task DeserializePathForObjectFails()
        {
            const string GoodJson = "{\"Property\u04671\":1}";
            const string GoodJsonEscaped = "{\"Property\\u04671\":1}";
            const string BadJson = "{\"Property\u04671\":bad}";
            const string BadJsonEscaped = "{\"Property\\u04671\":bad}";
            const string Expected = "$.Property\u04671";

            ClassWithUnicodePropertyName obj;

            // Baseline.
            obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithUnicodePropertyName>(GoodJson);
            Assert.Equal(1, obj.Property\u04671);

            obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithUnicodePropertyName>(GoodJsonEscaped);
            Assert.Equal(1, obj.Property\u04671);

            JsonException e;

            // Exception.
            e = await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<ClassWithUnicodePropertyName>(BadJson));
            Assert.Equal(Expected, e.Path);

            e = await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<ClassWithUnicodePropertyName>(BadJsonEscaped));
            Assert.Equal(Expected, e.Path);
        }

        public class ClassWithUnicodePropertyName
        {
            public int Property\u04671 { get; } // contains a trailing "1"

            public ClassWithUnicodePropertyName(int property\u04671)
            {
                Property\u04671 = property\u04671;
            }
        }

        [Fact]
        public async Task PathForChildPropertyFails()
        {
            JsonException e = await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<RootClass>(@"{""Child"":{""MyInt"":bad]}"));
            Assert.Equal("$.Child.MyInt", e.Path);
        }

        public class RootClass
        {
            public ChildClass Child { get; }

            public RootClass(ChildClass child)
            {
                Child = child;
            }
        }

        public class ChildClass
        {
            public int MyInt { get; set; }
            public int[] MyIntArray { get; set; }
            public Dictionary<string, ChildClass> MyDictionary { get; set; }
            public ChildClass[] Children { get; set; }
        }

        [Fact]
        public async Task PathForChildListFails()
        {
            JsonException e = await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<RootClass>(@"{""Child"":{""MyIntArray"":[1, bad]}"));
            Assert.Contains("$.Child.MyIntArray", e.Path);
        }

        [Fact]
        public async Task PathForChildDictionaryFails()
        {
            JsonException e = await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<RootClass>(@"{""Child"":{""MyDictionary"":{""Key"": bad]"));
            Assert.Equal("$.Child.MyDictionary.Key", e.Path);
        }

        [Fact]
        public async Task PathForSpecialCharacterFails()
        {
            JsonException e = await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<RootClass>(@"{""Child"":{""MyDictionary"":{""Key1"":{""Children"":[{""MyDictionary"":{""K.e.y"":"""));
            Assert.Equal("$.Child.MyDictionary.Key1.Children[0].MyDictionary['K.e.y']", e.Path);
        }

        [Fact]
        public async Task PathForSpecialCharacterNestedFails()
        {
            JsonException e = await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<RootClass>(@"{""Child"":{""Children"":[{}, {""MyDictionary"":{""K.e.y"": {""MyInt"":bad"));
            Assert.Equal("$.Child.Children[1].MyDictionary['K.e.y'].MyInt", e.Path);
        }

        [Fact]
        public async Task EscapingFails()
        {
            JsonException e = await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<Parameterized_ClassWithUnicodeProperty>("{\"A\u0467\":bad}"));
            Assert.Equal("$.A\u0467", e.Path);
        }

        public class Parameterized_ClassWithUnicodeProperty
        {
            public int A\u0467 { get; }

            public Parameterized_ClassWithUnicodeProperty(int a\u0467)
            {
                A\u0467 = a\u0467;
            }
        }

        [Fact]
        public async Task ExtensionPropertyRoundTripFails()
        {
            JsonException e = await Assert.ThrowsAsync<JsonException>(() =>
                JsonSerializerWrapperForString.DeserializeWrapper<Parameterized_ClassWithExtensionProperty>(@"{""MyNestedClass"":{""UnknownProperty"":bad}}"));

            Assert.Equal("$.MyNestedClass.UnknownProperty", e.Path);
        }

        public class Parameterized_ClassWithExtensionProperty
        {
            public SimpleTestClass MyNestedClass { get; }
            public int MyInt { get; }

            [JsonExtensionData]
            public IDictionary<string, JsonElement> MyOverflow { get; set; }

            public Parameterized_ClassWithExtensionProperty(SimpleTestClass myNestedClass, int myInt)
            {
                MyNestedClass = myNestedClass;
                MyInt = myInt;
            }
        }

        [Fact]
        public async Task CaseInsensitiveFails()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;

            // Baseline (no exception)
            {
                var obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithConstructor_SimpleAndComplexParameters>(@"{""mydecimal"":1}", options);
                Assert.Equal(1, obj.MyDecimal);
            }

            {
                var obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithConstructor_SimpleAndComplexParameters>(@"{""MYDECIMAL"":1}", options);
                Assert.Equal(1, obj.MyDecimal);
            }

            JsonException e;

            e = await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<ClassWithConstructor_SimpleAndComplexParameters>(@"{""mydecimal"":bad}", options));
            Assert.Equal("$.mydecimal", e.Path);

            e = await Assert.ThrowsAsync<JsonException>(() => JsonSerializerWrapperForString.DeserializeWrapper<ClassWithConstructor_SimpleAndComplexParameters>(@"{""MYDECIMAL"":bad}", options));
            Assert.Equal("$.MYDECIMAL", e.Path);
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Multi-dim arrays not supported.")]
#endif
        public async Task ClassWithUnsupportedCollectionTypes()
        {
            Exception e;

            e = await Assert.ThrowsAsync<NotSupportedException>(() => JsonSerializerWrapperForString.DeserializeWrapper<ClassWithInvalidArray>(@"{""UnsupportedArray"":[]}"));
            Assert.Contains("System.Int32[,]", e.ToString());
            // The exception for element types do not contain the parent type and the property name
            // since the verification occurs later and is no longer bound to the parent type.
            Assert.DoesNotContain("ClassWithInvalidArray.UnsupportedArray", e.ToString());

            e = await Assert.ThrowsAsync<NotSupportedException>(() => JsonSerializerWrapperForString.DeserializeWrapper<ClassWithInvalidDictionary>(@"{""UnsupportedDictionary"":{}}"));
            Assert.Contains("System.Int32[,]", e.ToString());
            Assert.DoesNotContain("ClassWithInvalidDictionary.UnsupportedDictionary", e.ToString());
        }

        private class ClassWithInvalidArray
        {
            public int[,] UnsupportedArray { get; set; }

            public ClassWithInvalidArray(int[,] unsupportedArray)
            {
                UnsupportedArray = unsupportedArray;
            }
        }

        private class ClassWithInvalidDictionary
        {
            public Dictionary<string, int[,]> UnsupportedDictionary { get; set; }

            public ClassWithInvalidDictionary(Dictionary<string, int[,]> unsupportedDictionary)
            {
                UnsupportedDictionary = unsupportedDictionary;
            }
        }
    }
}

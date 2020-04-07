// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class ConstructorTests
    {
        [Fact]
        public void MultipleProperties_Cannot_BindTo_TheSame_ConstructorParameter()
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => Serializer.Deserialize<Point_MultipleMembers_BindTo_OneConstructorParameter>("{}"));

            string exStr = ex.ToString();
            Assert.Contains("'X'", exStr);
            Assert.Contains("'x'", exStr);
            Assert.Contains("(Int32, Int32)", exStr);
            Assert.Contains("System.Text.Json.Serialization.Tests.Point_MultipleMembers_BindTo_OneConstructorParameter", exStr);

            ex = Assert.Throws<InvalidOperationException>(
                () => Serializer.Deserialize<Point_MultipleMembers_BindTo_OneConstructorParameter_Variant>("{}"));

            exStr = ex.ToString();
            Assert.Contains("'X'", exStr);
            Assert.Contains("'x'", exStr);
            Assert.Contains("(Int32)", exStr);
            Assert.Contains("System.Text.Json.Serialization.Tests.Point_MultipleMembers_BindTo_OneConstructorParameter_Variant", exStr);
        }

        [Fact]
        public void All_ConstructorParameters_MustBindTo_ObjectMembers()
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => Serializer.Deserialize<Point_Without_Members>("{}"));

            string exStr = ex.ToString();
            Assert.Contains("(Int32, Int32)", exStr);
            Assert.Contains("System.Text.Json.Serialization.Tests.Point_Without_Members", exStr);

            ex = Assert.Throws<InvalidOperationException>(
                () => Serializer.Deserialize<Point_With_MismatchedMembers>("{}"));
            exStr = ex.ToString();
            Assert.Contains("(Int32, Int32)", exStr);
            Assert.Contains("System.Text.Json.Serialization.Tests.Point_With_MismatchedMembers", exStr);

            ex = Assert.Throws<InvalidOperationException>(
                () => Serializer.Deserialize<WrapperFor_Point_With_MismatchedMembers>(@"{""MyInt"":1,""MyPoint"":{}}"));
            exStr = ex.ToString();
            Assert.Contains("(Int32, Int32)", exStr);
            Assert.Contains("System.Text.Json.Serialization.Tests.Point_With_MismatchedMembers", exStr);
        }

        [Fact]
        public void LeadingReferenceMetadataNotSupported()
        {
            string json = @"{""$id"":""1"",""Name"":""Jet"",""Manager"":{""$ref"":""1""}}";

            // Metadata ignored by default.
            var employee = Serializer.Deserialize<Employee>(json);

            Assert.Equal("Jet", employee.Name);
            Assert.Null(employee.Manager.Name); ;
            Assert.Null(employee.Manager.Manager);

            // Metadata not supported with preserve ref feature on.

            var options = new JsonSerializerOptions { ReferenceHandling = ReferenceHandling.Preserve };

            NotSupportedException ex = Assert.Throws<NotSupportedException>(
                () => Serializer.Deserialize<Employee>(json, options));

            string exStr = ex.ToString();
            Assert.Contains("System.Text.Json.Serialization.Tests.ConstructorTests+Employee", exStr);
            Assert.Contains("$.$id", exStr);
        }

        private class Employee
        {
            public string Name { get; }
            public Employee Manager { get; set; }

            public Employee(string name)
            {
                Name = name;
            }
        }

        [Fact]
        public void RandomReferenceMetadataNotSupported()
        {
            string json = @"{""Name"":""Jet"",""$random"":10}";

            // Baseline, preserve ref feature off.

            var employee = JsonSerializer.Deserialize<Employee>(json);

            Assert.Equal("Jet", employee.Name);

            // Metadata not supported with preserve ref feature on.

            var options = new JsonSerializerOptions { ReferenceHandling = ReferenceHandling.Preserve };

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => Serializer.Deserialize<Employee>(json, options));
            string exStr = ex.ToString();
            Assert.Contains("System.Text.Json.Serialization.Tests.ConstructorTests+Employee", exStr);
            Assert.Contains("$.$random", exStr);
        }

        [Fact]
        public void ExtensionDataProperty_CannotBindTo_CtorParam()
        {
            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => Serializer.Deserialize<Class_ExtData_CtorParam>("{}"));
            string exStr = ex.ToString();
            Assert.Contains("System.Collections.Generic.Dictionary`2[System.String,System.Text.Json.JsonElement] ExtensionData", exStr);
            Assert.Contains("System.Text.Json.Serialization.Tests.ConstructorTests+Class_ExtData_CtorParam", exStr);
            Assert.Contains("(System.Collections.Generic.Dictionary`2[System.String,System.Text.Json.JsonElement])", exStr);
        }

        public class Class_ExtData_CtorParam
        {
            [JsonExtensionData]
            public Dictionary<string, JsonElement> ExtensionData { get; set; }

            public Class_ExtData_CtorParam(Dictionary<string, JsonElement> extensionData) { }
        }

        [Fact]
        public void AnonymousObject_InvalidOperationException()
        {
            var obj = new { Prop = 5 };

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(() => JsonSerializer.Deserialize("{}", obj.GetType()));

            // We expect property 'Prop' to bind with a ctor arg called 'prop', but the ctor arg is called 'Prop'.
            string exStr = ex.ToString();
            Assert.Contains("AnonymousType", exStr);
            Assert.Contains("(Int32)", exStr);
            Assert.Contains("[System.Int32]", exStr);
        }

        [Fact]
        public void DeserializePathForObjectFails()
        {
            const string GoodJson = "{\"Property\u04671\":1}";
            const string GoodJsonEscaped = "{\"Property\\u04671\":1}";
            const string BadJson = "{\"Property\u04671\":bad}";
            const string BadJsonEscaped = "{\"Property\\u04671\":bad}";
            const string Expected = "$.Property\u04671";

            ClassWithUnicodePropertyName obj;

            // Baseline.
            obj = Serializer.Deserialize<ClassWithUnicodePropertyName>(GoodJson);
            Assert.Equal(1, obj.Property\u04671);

            obj = Serializer.Deserialize<ClassWithUnicodePropertyName>(GoodJsonEscaped);
            Assert.Equal(1, obj.Property\u04671);

            JsonException e;

            // Exception.
            e = Assert.Throws<JsonException>(() => Serializer.Deserialize<ClassWithUnicodePropertyName>(BadJson));
            Assert.Equal(Expected, e.Path);

            e = Assert.Throws<JsonException>(() => Serializer.Deserialize<ClassWithUnicodePropertyName>(BadJsonEscaped));
            Assert.Equal(Expected, e.Path);
        }

        private class ClassWithUnicodePropertyName
        {
            public int Property\u04671 { get; } // contains a trailing "1"

            public ClassWithUnicodePropertyName(int property\u04671)
            {
                Property\u04671 = property\u04671;
            }
        }

        [Fact]
        public void PathForChildPropertyFails()
        {
            JsonException e = Assert.Throws<JsonException>(() => Serializer.Deserialize<RootClass>(@"{""Child"":{""MyInt"":bad]}"));
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
        public void PathForChildListFails()
        {
            JsonException e = Assert.Throws<JsonException>(() => Serializer.Deserialize<RootClass>(@"{""Child"":{""MyIntArray"":[1, bad]}"));
            Assert.Contains("$.Child.MyIntArray", e.Path);
        }

        [Fact]
        public void PathForChildDictionaryFails()
        {
            JsonException e = Assert.Throws<JsonException>(() => Serializer.Deserialize<RootClass>(@"{""Child"":{""MyDictionary"":{""Key"": bad]"));
            Assert.Equal("$.Child.MyDictionary.Key", e.Path);
        }

        [Fact]
        public void PathForSpecialCharacterFails()
        {
            JsonException e = Assert.Throws<JsonException>(() => Serializer.Deserialize<RootClass>(@"{""Child"":{""MyDictionary"":{""Key1"":{""Children"":[{""MyDictionary"":{""K.e.y"":"""));
            Assert.Equal("$.Child.MyDictionary.Key1.Children[0].MyDictionary['K.e.y']", e.Path);
        }

        [Fact]
        public void PathForSpecialCharacterNestedFails()
        {
            JsonException e = Assert.Throws<JsonException>(() => Serializer.Deserialize<RootClass>(@"{""Child"":{""Children"":[{}, {""MyDictionary"":{""K.e.y"": {""MyInt"":bad"));
            Assert.Equal("$.Child.Children[1].MyDictionary['K.e.y'].MyInt", e.Path);
        }

        [Fact]
        public void EscapingFails()
        {
            JsonException e = Assert.Throws<JsonException>(() => Serializer.Deserialize<Parameterized_ClassWithUnicodeProperty>("{\"A\u0467\":bad}"));
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
        [ActiveIssue("JsonElement needs to support Path")]
        public void ExtensionPropertyRoundTripFails()
        {
            JsonException e = Assert.Throws<JsonException>(() => Serializer.Deserialize<Parameterized_ClassWithExtensionProperty>(@"{""MyNestedClass"":{""UnknownProperty"":bad}}"));
            Assert.Equal("$.MyNestedClass.UnknownProperty", e.Path);
        }

        private class Parameterized_ClassWithExtensionProperty
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
        public void CaseInsensitiveFails()
        {
            var options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;

            // Baseline (no exception)
            {
                var obj = Serializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(@"{""mydecimal"":1}", options);
                Assert.Equal(1, obj.MyDecimal);
            }

            {
                var obj = Serializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(@"{""MYDECIMAL"":1}", options);
                Assert.Equal(1, obj.MyDecimal);
            }

            JsonException e;

            e = Assert.Throws<JsonException>(() => Serializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(@"{""mydecimal"":bad}", options));
            Assert.Equal("$.mydecimal", e.Path);

            e = Assert.Throws<JsonException>(() => Serializer.Deserialize<ClassWithConstructor_SimpleAndComplexParameters>(@"{""MYDECIMAL"":bad}", options));
            Assert.Equal("$.MYDECIMAL", e.Path);
        }

        [Fact]
        public void ClassWithUnsupportedCollectionTypes()
        {
            Exception e;

            e = Assert.Throws<NotSupportedException>(() => Serializer.Deserialize<ClassWithInvalidArray>(@"{""UnsupportedArray"":[]}"));
            Assert.Contains("System.Int32[,]", e.ToString());
            // The exception for element types do not contain the parent type and the property name
            // since the verification occurs later and is no longer bound to the parent type.
            Assert.DoesNotContain("ClassWithInvalidArray.UnsupportedArray", e.ToString());

            e = Assert.Throws<NotSupportedException>(() => Serializer.Deserialize<ClassWithInvalidDictionary>(@"{""UnsupportedDictionary"":{}}"));
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

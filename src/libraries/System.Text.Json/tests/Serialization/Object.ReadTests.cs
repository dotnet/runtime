// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ObjectTests
    {
        [Fact]
        public static void ReadSimpleStruct()
        {
            SimpleTestStruct obj = JsonSerializer.Deserialize<SimpleTestStruct>(SimpleTestStruct.s_json);
            obj.Verify();
        }

        [Fact]
        public static void ReadSimpleClass()
        {
            SimpleTestClass obj = JsonSerializer.Deserialize<SimpleTestClass>(SimpleTestClass.s_json);
            obj.Verify();
        }

        [Theory]
        [InlineData("", " ")]
        [InlineData("", "\t ")]
        [InlineData("", "//Single Line Comment\r\n")]
        [InlineData("", "/* Multi\nLine Comment */")]
        [InlineData("", "\t\t\t\n// Both\n/* Comments */")]
        [InlineData(" ", "")]
        [InlineData("\t ", "")]
        [InlineData(" \t", " \n")]
        [InlineData("// Leading Comment\n", "")]
        [InlineData("/* Multi\nLine\nComment */ ", "")]
        [InlineData("/* Multi\nLine\nComment */ ", "\t// trailing comment\n ")]
        public static void ReadSimpleClassIgnoresLeadingOrTrailingTrivia(string leadingTrivia, string trailingTrivia)
        {
            var options = new JsonSerializerOptions();
            options.ReadCommentHandling = JsonCommentHandling.Skip;

            SimpleTestClass obj = JsonSerializer.Deserialize<SimpleTestClass>(leadingTrivia + SimpleTestClass.s_json + trailingTrivia, options);
            obj.Verify();
        }

        [Fact]
        public static void ReadSimpleClassWithObject()
        {
            SimpleTestClassWithSimpleObject obj = JsonSerializer.Deserialize<SimpleTestClassWithSimpleObject>(SimpleTestClassWithSimpleObject.s_json);
            obj.Verify();
            string reserialized = JsonSerializer.Serialize(obj);

            // Properties in the exported json will be in the order that they were reflected, doing a quick check to see that
            // we end up with the same length (i.e. same amount of data) to start.
            Assert.Equal(SimpleTestClassWithSimpleObject.s_json.StripWhitespace().Length, reserialized.Length);

            // Shoving it back through the parser should validate round tripping.
            obj = JsonSerializer.Deserialize<SimpleTestClassWithSimpleObject>(reserialized);
            obj.Verify();
        }

        [Theory]
        [InlineData("", "//Single Line Comment\r\n")]
        [InlineData("", "/* Multi\nLine Comment */")]
        [InlineData("", "\t\t\t\n// Both\n/* Comments */")]
        [InlineData("// Leading Comment\n", "")]
        [InlineData("/* Multi\nLine\nComment */ ", "")]
        [InlineData("/* Multi\nLine\nComment */ ", "\t// trailing comment\n ")]
        public static void ReadClassWithCommentsThrowsIfDisallowed(string leadingTrivia, string trailingTrivia)
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<ClassWithComplexObjects>(leadingTrivia + ClassWithComplexObjects.s_json + trailingTrivia));
        }

        [Fact]
        public static void ReadSimpleClassWithObjectArray()
        {
            SimpleTestClassWithObjectArrays obj = JsonSerializer.Deserialize<SimpleTestClassWithObjectArrays>(SimpleTestClassWithObjectArrays.s_json);
            obj.Verify();
            string reserialized = JsonSerializer.Serialize(obj);

            // Properties in the exported json will be in the order that they were reflected, doing a quick check to see that
            // we end up with the same length (i.e. same amount of data) to start.
            string json = SimpleTestClassWithObjectArrays.s_json.StripWhitespace();
            Assert.Equal(json.Length, reserialized.Length);

            // Shoving it back through the parser should validate round tripping.
            obj = JsonSerializer.Deserialize<SimpleTestClassWithObjectArrays>(reserialized);
            obj.Verify();
        }

        [Fact]
        public static void ReadArrayInObjectArray()
        {
            object[] array = JsonSerializer.Deserialize<object[]>(@"[[]]");
            Assert.Equal(1, array.Length);
            Assert.IsType<JsonElement>(array[0]);
            Assert.Equal(JsonValueKind.Array, ((JsonElement)array[0]).ValueKind);
        }

        [Fact]
        public static void ReadObjectInObjectArray()
        {
            object[] array = JsonSerializer.Deserialize<object[]>(@"[{}]");
            Assert.Equal(1, array.Length);
            Assert.IsType<JsonElement>(array[0]);
            Assert.Equal(JsonValueKind.Object, ((JsonElement)array[0]).ValueKind);

            // Scenario described in https://github.com/dotnet/corefx/issues/36169
            array = JsonSerializer.Deserialize<object[]>("[1, false]");
            Assert.Equal(2, array.Length);
            Assert.IsType<JsonElement>(array[0]);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)array[0]).ValueKind);
            Assert.Equal(1, ((JsonElement)array[0]).GetInt32());
            Assert.IsType<JsonElement>(array[1]);
            Assert.Equal(JsonValueKind.False, ((JsonElement)array[1]).ValueKind);

            array = JsonSerializer.Deserialize<object[]>(@"[1, false, { ""name"" : ""Person"" }]");
            Assert.Equal(3, array.Length);
            Assert.IsType<JsonElement>(array[0]);
            Assert.Equal(JsonValueKind.Number, ((JsonElement)array[0]).ValueKind);
            Assert.Equal(1, ((JsonElement)array[0]).GetInt32());
            Assert.IsType<JsonElement>(array[1]);
            Assert.Equal(JsonValueKind.False, ((JsonElement)array[1]).ValueKind);
            Assert.IsType<JsonElement>(array[2]);
            Assert.Equal(JsonValueKind.Object, ((JsonElement)array[2]).ValueKind);
        }

        [Fact]
        public static void ReadClassWithComplexObjects()
        {
            ClassWithComplexObjects obj = JsonSerializer.Deserialize<ClassWithComplexObjects>(ClassWithComplexObjects.s_json);
            obj.Verify();
            string reserialized = JsonSerializer.Serialize(obj);

            // Properties in the exported json will be in the order that they were reflected, doing a quick check to see that
            // we end up with the same length (i.e. same amount of data) to start.
            Assert.Equal(ClassWithComplexObjects.s_json.StripWhitespace().Length, reserialized.Length);

            // Shoving it back through the parser should validate round tripping.
            obj = JsonSerializer.Deserialize<ClassWithComplexObjects>(reserialized);
            obj.Verify();
        }

        [Theory]
        [InlineData("", " ")]
        [InlineData("", "\t ")]
        [InlineData("", "//Single Line Comment\r\n")]
        [InlineData("", "/* Multi\nLine Comment */")]
        [InlineData("", "\t\t\t\n// Both\n/* Comments */")]
        [InlineData(" ", "")]
        [InlineData("\t ", "")]
        [InlineData(" \t", " \n")]
        [InlineData("// Leading Comment\n", "")]
        [InlineData("/* Multi\nLine\nComment */ ", "")]
        [InlineData("/* Multi\nLine\nComment */ ", "\t// trailing comment\n ")]
        public static void ReadComplexClassIgnoresLeadingOrTrailingTrivia(string leadingTrivia, string trailingTrivia)
        {
            var options = new JsonSerializerOptions();
            options.ReadCommentHandling = JsonCommentHandling.Skip;

            ClassWithComplexObjects obj = JsonSerializer.Deserialize<ClassWithComplexObjects>(leadingTrivia + ClassWithComplexObjects.s_json + trailingTrivia, options);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithNestedComments()
        {
            var options = new JsonSerializerOptions();
            options.ReadCommentHandling = JsonCommentHandling.Skip;

            TestClassWithNestedObjectCommentsOuter obj = JsonSerializer.Deserialize<TestClassWithNestedObjectCommentsOuter>(TestClassWithNestedObjectCommentsOuter.s_data, options);
            obj.Verify();
        }

        [Fact]
        public static void ReadEmpty()
        {
            SimpleTestClass obj = JsonSerializer.Deserialize<SimpleTestClass>("{}");
            Assert.NotNull(obj);
        }

        [Fact]
        public static void EmptyClassWithRandomData()
        {
            JsonSerializer.Deserialize<EmptyClass>(SimpleTestClass.s_json);
            JsonSerializer.Deserialize<EmptyClass>(SimpleTestClassWithNulls.s_json);
        }

        [Theory]
        [InlineData("blah", true)]
        [InlineData("null.", true)]
        [InlineData("{{}", true)]
        [InlineData("{", true)]
        [InlineData("}", true)]
        [InlineData("", true)]
        [InlineData("true", false)]
        [InlineData("[]", false)]
        [InlineData("[{}]", false)]
        public static void ReadObjectFail(string json, bool failsOnObject)
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<SimpleTestClass>(json));

            if (failsOnObject)
            {
                Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<object>(json));
            }
            else
            {
                Assert.IsType<JsonElement>(JsonSerializer.Deserialize<object>(json));
            }
        }

        [Fact]
        public static void ReadObjectFail_ReferenceTypeMissingParameterlessConstructor()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<PublicParameterizedConstructorTestClass>(@"{""Name"":""Name!""}"));
        }

        class PublicParameterizedConstructorTestClass
        {
            private readonly string _name;
            public PublicParameterizedConstructorTestClass(string name)
            {
                _name = name;
            }
            public string Name
            {
                get { return _name; }
            }
        }

        [Fact]
        public static void ReadClassWithStringToPrimitiveDictionary()
        {
            TestClassWithStringToPrimitiveDictionary obj = JsonSerializer.Deserialize<TestClassWithStringToPrimitiveDictionary>(TestClassWithStringToPrimitiveDictionary.s_data);
            obj.Verify();
        }

        public class TestClassWithBadData
        {
            public TestChildClassWithBadData[] Children { get; set; }
        }

        public class TestChildClassWithBadData
        {
            public int MyProperty { get; set; }
        }

        [Fact]
        public static void ReadConversionFails()
        {
            byte[] data = Encoding.UTF8.GetBytes(
                @"{" +
                    @"""Children"":[" +
                        @"{""MyProperty"":""StringButShouldBeInt""}" +
                    @"]" +
                @"}");

            bool exceptionThrown = false;

            try
            {
                JsonSerializer.Deserialize<TestClassWithBadData>(data);
            }
            catch (JsonException exception)
            {
                exceptionThrown = true;

                // Exception should contain path.
                Assert.True(exception.ToString().Contains("Path: $.Children[0].MyProperty"));
            }

            Assert.True(exceptionThrown);
        }

        [Fact]
        public static void ReadObject_PublicIndexer()
        {
            Indexer indexer = JsonSerializer.Deserialize<Indexer>(@"{""NonIndexerProp"":""Value""}");
            Assert.Equal("Value", indexer.NonIndexerProp);
            Assert.Equal(-1, indexer[0]);
        }

        [Fact]
        public static void ReadSimpleStructWithSimpleClass()
        {
            SimpleStructWithSimpleClass testObject = new SimpleStructWithSimpleClass();
            testObject.Initialize();

            string json = JsonSerializer.Serialize(testObject, testObject.GetType());
            SimpleStructWithSimpleClass obj = JsonSerializer.Deserialize<SimpleStructWithSimpleClass>(json);
            obj.Verify();
        }

        [Fact]
        public static void ReadSimpleTestStructWithSimpleTestClass()
        {
            SimpleTestStruct testObject = new SimpleTestStruct();
            testObject.Initialize();
            testObject.MySimpleTestClass = new SimpleTestClass { MyString = "Hello", MyDouble = 3.14 };

            string json = JsonSerializer.Serialize(testObject);
            SimpleTestStruct parsedObject = JsonSerializer.Deserialize<SimpleTestStruct>(json);
            parsedObject.Verify();
            Assert.Equal(3.14, parsedObject.MySimpleTestClass.MyDouble);
            Assert.Equal("Hello", parsedObject.MySimpleTestClass.MyString);
        }

        [Fact]
        public static void ReadSimpleTestClassWithSimpleTestStruct()
        {
            SimpleTestClass testObject = new SimpleTestClass();
            testObject.Initialize();
            testObject.MySimpleTestStruct = new SimpleTestStruct { MyInt64 = 64, MyString = "Hello", MyInt32Array = new int[] { 32 } };

            string json = JsonSerializer.Serialize(testObject);
            SimpleTestClass parsedObject = JsonSerializer.Deserialize<SimpleTestClass>(json);
            parsedObject.Verify();
            Assert.Equal(64, parsedObject.MySimpleTestStruct.MyInt64);
            Assert.Equal("Hello", parsedObject.MySimpleTestStruct.MyString);
            Assert.Equal(32, parsedObject.MySimpleTestStruct.MyInt32Array[0]);
        }

        [Fact]
        public static void OuterClassHavingPropertiesDefinedAfterClassWithDictionaryTest()
        {
            OuterClassHavingPropertiesDefinedAfterClassWithDictionary testObject = new OuterClassHavingPropertiesDefinedAfterClassWithDictionary
            {
                MyInt = 10,
                MyIntArray = new int[] { 3 },
                MyDouble = 3.14,
                MyList = new List<string> { "Hello" },
                MyString = "World",
                MyInnerTestClass = new SimpleClassWithDictionary { MyInt = 2 }
            };
            string json = JsonSerializer.Serialize(testObject);

            OuterClassHavingPropertiesDefinedAfterClassWithDictionary parsedObject = JsonSerializer.Deserialize<OuterClassHavingPropertiesDefinedAfterClassWithDictionary>(json);

            Assert.Equal(3.14, parsedObject.MyDouble);
            Assert.Equal(10, parsedObject.MyInt);
            Assert.Equal("World", parsedObject.MyString);
            Assert.Equal("Hello", parsedObject.MyList[0]);
            Assert.Equal(3, parsedObject.MyIntArray[0]);
            Assert.Equal(2, parsedObject.MyInnerTestClass.MyInt);
        }

        [Fact]
        public static void ReadStructWithSimpleClassArrayValueTest()
        {
            string json = "{\"MySimpleTestClass\":{\"MyInt32Array\":[1],\"MyStringToStringDict\":null,\"MyStringToStringIDict\":null},\"MyInt32Array\":[2]}";
            SimpleTestStruct parsedObject = JsonSerializer.Deserialize<SimpleTestStruct>(json);

            Assert.Equal(1, parsedObject.MySimpleTestClass.MyInt32Array[0]);
            Assert.Equal(2, parsedObject.MyInt32Array[0]);
        }
    }
}

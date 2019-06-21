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
            SimpleTestStruct obj = JsonSerializer.Parse<SimpleTestStruct>(SimpleTestStruct.s_json);
            obj.Verify();
        }

        [Fact]
        public static void ReadSimpleClass()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(SimpleTestClass.s_json);
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

            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(leadingTrivia + SimpleTestClass.s_json + trailingTrivia, options);
            obj.Verify();
        }

        [Fact]
        public static void ReadSimpleClassWithObject()
        {
            SimpleTestClassWithSimpleObject obj = JsonSerializer.Parse<SimpleTestClassWithSimpleObject>(SimpleTestClassWithSimpleObject.s_json);
            obj.Verify();
            string reserialized = JsonSerializer.ToString(obj);

            // Properties in the exported json will be in the order that they were reflected, doing a quick check to see that
            // we end up with the same length (i.e. same amount of data) to start.
            Assert.Equal(SimpleTestClassWithSimpleObject.s_json.StripWhitespace().Length, reserialized.Length);

            // Shoving it back through the parser should validate round tripping.
            obj = JsonSerializer.Parse<SimpleTestClassWithSimpleObject>(reserialized);
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
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<ClassWithComplexObjects>(leadingTrivia + ClassWithComplexObjects.s_json + trailingTrivia));
        }

        [Fact]
        public static void ReadSimpleClassWithObjectArray()
        {
            SimpleTestClassWithObjectArrays obj = JsonSerializer.Parse<SimpleTestClassWithObjectArrays>(SimpleTestClassWithObjectArrays.s_json);
            obj.Verify();
            string reserialized = JsonSerializer.ToString(obj);

            // Properties in the exported json will be in the order that they were reflected, doing a quick check to see that
            // we end up with the same length (i.e. same amount of data) to start.
            Assert.Equal(SimpleTestClassWithObjectArrays.s_json.StripWhitespace().Length, reserialized.Length);

            // Shoving it back through the parser should validate round tripping.
            obj = JsonSerializer.Parse<SimpleTestClassWithObjectArrays>(reserialized);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithComplexObjects()
        {
            ClassWithComplexObjects obj = JsonSerializer.Parse<ClassWithComplexObjects>(ClassWithComplexObjects.s_json);
            obj.Verify();
            string reserialized = JsonSerializer.ToString(obj);

            // Properties in the exported json will be in the order that they were reflected, doing a quick check to see that
            // we end up with the same length (i.e. same amount of data) to start.
            Assert.Equal(ClassWithComplexObjects.s_json.StripWhitespace().Length, reserialized.Length);

            // Shoving it back through the parser should validate round tripping.
            obj = JsonSerializer.Parse<ClassWithComplexObjects>(reserialized);
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

            ClassWithComplexObjects obj = JsonSerializer.Parse<ClassWithComplexObjects>(leadingTrivia + ClassWithComplexObjects.s_json + trailingTrivia, options);
            obj.Verify();
        }

        [Fact]
        public static void ReadClassWithNestedComments()
        {
            var options = new JsonSerializerOptions();
            options.ReadCommentHandling = JsonCommentHandling.Skip;

            TestClassWithNestedObjectCommentsOuter obj = JsonSerializer.Parse<TestClassWithNestedObjectCommentsOuter>(TestClassWithNestedObjectCommentsOuter.s_data, options);
            obj.Verify();
        }

        [Fact]
        public static void ReadEmpty()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>("{}");
            Assert.NotNull(obj);
        }

        [Fact]
        public static void EmptyClassWithRandomData()
        {
            JsonSerializer.Parse<EmptyClass>(SimpleTestClass.s_json);
            JsonSerializer.Parse<EmptyClass>(SimpleTestClassWithNulls.s_json);
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
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>(json));

            if (failsOnObject)
            {
                Assert.Throws<JsonException>(() => JsonSerializer.Parse<object>(json));
            }
            else
            {
                Assert.IsType<JsonElement>(JsonSerializer.Parse<object>(json));
            }
        }

        [Fact]
        public static void ReadObjectFail_ReferenceTypeMissingParameterlessConstructor()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Parse<PublicParameterizedConstructorTestClass>(@"{""Name"":""Name!""}"));
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
            TestClassWithStringToPrimitiveDictionary obj = JsonSerializer.Parse<TestClassWithStringToPrimitiveDictionary>(TestClassWithStringToPrimitiveDictionary.s_data);
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
                JsonSerializer.Parse<TestClassWithBadData>(data);
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
            Indexer indexer = JsonSerializer.Parse<Indexer>(@"{""NonIndexerProp"":""Value""}");
            Assert.Equal("Value", indexer.NonIndexerProp);
            Assert.Equal(-1, indexer[0]);
        }

        [ActiveIssue(38414)]
        [Fact]
        public static void ReadSimpleStructWithSimpleClass()
        {
            SimpleStructWithSimpleClass testObject = new SimpleStructWithSimpleClass();
            testObject.Initialize();

            string json = JsonSerializer.ToString(testObject, testObject.GetType());
            SimpleStructWithSimpleClass obj = JsonSerializer.Parse<SimpleStructWithSimpleClass>(json);
            obj.Verify();
        }

        [ActiveIssue(38490)]
        [Fact]
        public static void ReadSimpleTestStructWithSimpleTestClass()
        {
            SimpleTestStruct testObject = new SimpleTestStruct();
            testObject.Initialize();
            testObject.MySimpleTestClass = new SimpleTestClass  { MyString = "Hello", MyDouble = 3.14 } ;

            string json = JsonSerializer.ToString(testObject);
            SimpleTestStruct parsedObject = JsonSerializer.Parse<SimpleTestStruct>(json);
            parsedObject.Verify();
        }

        [Fact]
        public static void ReadSimpleTestClassWithSimpleTestStruct()
        {
            SimpleTestClass testObject = new SimpleTestClass();
            testObject.Initialize();
            testObject.MySimpleTestStruct = new SimpleTestStruct { MyInt64 = 64, MyString = "Hello", MyInt32Array = new int[] { 32 } };
            
            string json = JsonSerializer.ToString(testObject);
            SimpleTestClass parsedObject = JsonSerializer.Parse<SimpleTestClass>(json);
            parsedObject.Verify();
        }

        [ActiveIssue(38414)]
        [Fact]
        public static void OuterClassHavingPropertiesDefinedAfterClassWithDictionaryTest()
        {
            OuterClassHavingPropertiesDefinedAfterClassWithDictionary testObject = new OuterClassHavingPropertiesDefinedAfterClassWithDictionary { MyInt = 10, MyIntArray = new int[] { 3 },
                MyDouble= 3.14, MyList =new List<string> { "Hello" }, MyString = "World",  MyInnerTestClass = new SimpleClassWithDictionary { MyInt = 2 } };
            string json = JsonSerializer.ToString(testObject);

            OuterClassHavingPropertiesDefinedAfterClassWithDictionary parsedObject = JsonSerializer.Parse<OuterClassHavingPropertiesDefinedAfterClassWithDictionary>(json);

            Assert.Equal(3.14, parsedObject.MyDouble);
            Assert.Equal(10, parsedObject.MyInt);
            Assert.Equal("World", parsedObject.MyString);
            Assert.Equal("Hello", parsedObject.MyList[0]);
            Assert.Equal(3, parsedObject.MyIntArray[0]);
            Assert.Equal(2, parsedObject.MyInnerTestClass.MyInt);
        }

        [ActiveIssue(38435)]
        [Fact]
        public static void ReadStructWithSimpleClassValueTest()
        {
            string json = "{\"MySimpleTestClass\":{\"MyInt32Array\":[1],\"MyStringToStringDict\":null,\"MyStringToStringIDict\":null},\"MyInt32Array\":[2]}";
            SimpleTestStruct obj3 = JsonSerializer.Parse<SimpleTestStruct>(json);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

        [Fact]
        public static void ReadObjectFail()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>("blah"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<object>("blah"));

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>("true"));

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>("null."));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<object>("null."));
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
        public static void ParseUntyped()
        {
            // Not supported until we are able to deserialize into JsonElement.
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<SimpleTestClass>("[]"));
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<object>("[]"));
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
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ObjectTests
    {
        [Fact]
        public static void ReadSimpleClass()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(SimpleTestClass.s_json);
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
            Assert.Throws<JsonReaderException>(() => JsonSerializer.Parse<SimpleTestClass>("blah"));
            Assert.Throws<JsonReaderException>(() => JsonSerializer.Parse<object>("blah"));

            Assert.Throws<JsonReaderException>(() => JsonSerializer.Parse<SimpleTestClass>("true"));

            Assert.Throws<JsonReaderException>(() => JsonSerializer.Parse<SimpleTestClass>("null."));
            Assert.Throws<JsonReaderException>(() => JsonSerializer.Parse<object>("null."));
        }

        [Fact]
        public static void ParseUntyped()
        {
            // Not supported until we are able to deserialize into JsonElement.
            Assert.Throws<JsonReaderException>(() => JsonSerializer.Parse<SimpleTestClass>("[]"));
            Assert.Throws<JsonReaderException>(() => JsonSerializer.Parse<object>("[]"));
        }

        [Fact]
        public static void ReadClassWithStringToPrimitiveDictionary()
        {
            TestClassWithStringToPrimitiveDictionary obj = JsonSerializer.Parse<TestClassWithStringToPrimitiveDictionary>(TestClassWithStringToPrimitiveDictionary.s_data);
            obj.Verify();
        }
    }
}

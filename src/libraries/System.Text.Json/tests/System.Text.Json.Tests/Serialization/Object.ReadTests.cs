// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

            // Scenario described in https://github.com/dotnet/runtime/issues/29021
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
        public static void ReadObjectFail_ReferenceTypeMissingPublicParameterlessConstructor()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ClassWithInternalParameterlessCtor>(@"{""Name"":""Name!""}"));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ClassWithPrivateParameterlessCtor>(@"{""Name"":""Name!""}"));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<CollectionWithoutPublicParameterlessCtor>(@"[""foo"", 1, false]"));

            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<GenericClassWithProtectedInternalCtor<string>>("{\"Result\":null}"));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ConcreteDerivedClassWithNoPublicDefaultCtor>("{\"ErrorString\":\"oops\"}"));
        }

        private class PublicParameterizedConstructorTestClass
        {
            public PublicParameterizedConstructorTestClass(string name) { }

            private PublicParameterizedConstructorTestClass(int internalId)
            {
                Name = internalId.ToString();
            }

            public string Name { get; }

            public static PublicParameterizedConstructorTestClass Instance { get; } = new PublicParameterizedConstructorTestClass(42);
        }

        private class ClassWithInternalParameterlessCtor
        {
            internal ClassWithInternalParameterlessCtor()
            {
                Debug.Fail("The JsonSerializer should not be callin non-public ctors, by default.");
            }

            private ClassWithInternalParameterlessCtor(string name)
            {
                Name = name;
            }

            public string Name { get; set; }

            public static ClassWithInternalParameterlessCtor Instance { get; } = new ClassWithInternalParameterlessCtor("InstancePropertyInternal");
        }

        private class ClassWithPrivateParameterlessCtor
        {
            private ClassWithPrivateParameterlessCtor()
            {
                Debug.Fail("The JsonSerializer should not be callin non-public ctors, by default.");
            }

            private ClassWithPrivateParameterlessCtor(string name)
            {
                Name = name;
            }

            public string Name { get; set; }

            public static ClassWithPrivateParameterlessCtor Instance { get; } = new ClassWithPrivateParameterlessCtor("InstancePropertyPrivate");
        }

        private class CollectionWithoutPublicParameterlessCtor : IList
        {
            private readonly List<object> _list;

            internal CollectionWithoutPublicParameterlessCtor()
            {
                Debug.Fail("The JsonSerializer should not be calling non-public ctors, by default.");
            }

            public CollectionWithoutPublicParameterlessCtor(List<object> list)
            {
                _list = list;
            }

            public object this[int index] { get => _list[index]; set => _list[index] = value; }

            public bool IsFixedSize => throw new NotImplementedException();

            public bool IsReadOnly => false;

            public int Count => _list.Count;

            public bool IsSynchronized => throw new NotImplementedException();

            public object SyncRoot => throw new NotImplementedException();

            public int Add(object value)
            {
                int pos = _list.Count;
                _list.Add(value);
                return pos;
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(object value)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(Array array, int index)
            {
                throw new NotImplementedException();
            }

            public IEnumerator GetEnumerator() => _list.GetEnumerator();

            public int IndexOf(object value)
            {
                throw new NotImplementedException();
            }

            public void Insert(int index, object value)
            {
                throw new NotImplementedException();
            }

            public void Remove(object value)
            {
                throw new NotImplementedException();
            }

            public void RemoveAt(int index)
            {
                throw new NotImplementedException();
            }
        }

        private class GenericClassWithProtectedInternalCtor<T>
        {
            public T Result { get; set; }

            protected internal GenericClassWithProtectedInternalCtor()
            {
                Result = default;
            }
        }

        private sealed class ConcreteDerivedClassWithNoPublicDefaultCtor : GenericClassWithProtectedInternalCtor<string>
        {
            public string ErrorString { get; set; }

            private ConcreteDerivedClassWithNoPublicDefaultCtor(string error)
            {
                ErrorString = error;
            }

            public static ConcreteDerivedClassWithNoPublicDefaultCtor Ok() => new ConcreteDerivedClassWithNoPublicDefaultCtor("ok");
            public static GenericClassWithProtectedInternalCtor<T> Ok<T>() => new GenericClassWithProtectedInternalCtor<T>();
            public static ConcreteDerivedClassWithNoPublicDefaultCtor Error(string error) => new ConcreteDerivedClassWithNoPublicDefaultCtor(error);
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
                Assert.Contains("Path: $.Children[0].MyProperty", exception.ToString());
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

        public class ClassWithNoSetter
        {
            public SimpleTestClass SkippedChild { get; }
            public SimpleTestClass ParsedChild { get; set; }
            public SimpleTestClass AnotherSkippedChild { get; }
            public SimpleTestClass AnotherParsedChild { get; set; }
        }

        [Theory]
        [InlineData(@"{
                ""SkippedChild"": {},
                ""ParsedChild"": {""MyInt16"":18},
                ""UnmatchedProp"": null,
                ""AnotherSkippedChild"": {""DrainProp1"":{}, ""DrainProp2"":{""SubProp"":0}},
                ""AnotherSkippedChild"": {},
                ""AnotherParsedChild"": {""MyInt16"":20}}")]
        [InlineData(@"{
                ""SkippedChild"": null,
                ""ParsedChild"": {""MyInt16"":18},
                ""UnmatchedProp"": null,
                ""AnotherSkippedChild"": {""DrainProp1"":{}, ""DrainProp2"":{""SubProp"":0}},
                ""AnotherSkippedChild"": null,
                ""AnotherParsedChild"": {""MyInt16"":20}}")]
        public static void ClassWithNoSetterAndValidProperty(string json)
        {
            ClassWithNoSetter parsedObject = JsonSerializer.Deserialize<ClassWithNoSetter>(json);

            Assert.Null(parsedObject.SkippedChild);

            Assert.NotNull(parsedObject.ParsedChild);
            Assert.Equal(18, parsedObject.ParsedChild.MyInt16);

            Assert.Null(parsedObject.AnotherSkippedChild);

            Assert.NotNull(parsedObject.AnotherParsedChild);
            Assert.Equal(20, parsedObject.AnotherParsedChild.MyInt16);
        }

        public class ClassMixingSkippedTypes
        {
            public IDictionary<string, string> SkippedDictionary { get; }

            public SimpleTestClass ParsedClass { get; set; }

            public IList<int> SkippedList { get; }
            public IDictionary<string, string> AnotherSkippedDictionary { get; }
            public SimpleTestClass SkippedClass { get; }

            public Dictionary<string, int> ParsedDictionary { get; set; }

            public IList<int> AnotherSkippedList { get; }
            public IDictionary<string, string> AnotherSkippedDictionary2 { get; }
            public IDictionary<string, string> SkippedDictionaryNotInJson { get; }
            public SimpleTestClass AnotherSkippedClass { get; }

            public int[] ParsedList { get; set; }

            public ClassMixingSkippedTypes ParsedSubMixedTypeParsedClass { get; set; }
        }

        [Fact]
        public static void ClassWithMixingSkippedTypes()
        {
            // Tests that the parser picks back up after skipping/draining ignored elements. Complex version.
            string json = @"{
                ""SkippedDictionary"": {},
                ""ParsedClass"": {""MyInt16"":18},
                ""SkippedList"": [18,20],
                ""UnmatchedList"": [{},{}],
                ""AnotherSkippedDictionary"": {""Key"":""Value""},
                ""SkippedClass"": {""MyInt16"":99},
                ""ParsedDictionary"": {""Key1"":18},
                ""UnmatchedProp"": null,
                ""AnotherSkippedList"": null,
                ""AnotherSkippedDictionary2"": {""Key"":""Value""},
                ""AnotherSkippedDictionary2"": {""Key"":""Dupe""},
                ""AnotherSkippedClass"": {},
                ""ParsedList"": [18,20],
                ""ParsedSubMixedTypeParsedClass"": {""ParsedDictionary"": {""Key1"":18}},
                ""UnmatchedDictionary"": {""DrainProp1"":{}, ""DrainProp2"":{""SubProp"":0}}                
            }";

            ClassMixingSkippedTypes parsedObject = JsonSerializer.Deserialize<ClassMixingSkippedTypes>(json);

            Assert.Null(parsedObject.SkippedDictionary);

            Assert.NotNull(parsedObject.ParsedClass);
            Assert.Equal(18, parsedObject.ParsedClass.MyInt16);

            Assert.Null(parsedObject.SkippedList);
            Assert.Null(parsedObject.AnotherSkippedDictionary);
            Assert.Null(parsedObject.SkippedClass);

            Assert.NotNull(parsedObject.ParsedDictionary);
            Assert.Equal(1, parsedObject.ParsedDictionary.Count);
            Assert.Equal(18, parsedObject.ParsedDictionary["Key1"]);

            Assert.Null(parsedObject.AnotherSkippedList);
            Assert.Null(parsedObject.AnotherSkippedDictionary2);
            Assert.Null(parsedObject.SkippedDictionaryNotInJson);
            Assert.Null(parsedObject.AnotherSkippedClass);

            Assert.NotNull(parsedObject.ParsedList);
            Assert.Equal(2, parsedObject.ParsedList.Length);
            Assert.Equal(18, parsedObject.ParsedList[0]);
            Assert.Equal(20, parsedObject.ParsedList[1]);

            Assert.NotNull(parsedObject.ParsedSubMixedTypeParsedClass);
            Assert.NotNull(parsedObject.ParsedSubMixedTypeParsedClass.ParsedDictionary);
            Assert.Equal(1, parsedObject.ParsedSubMixedTypeParsedClass.ParsedDictionary.Count);
            Assert.Equal(18, parsedObject.ParsedSubMixedTypeParsedClass.ParsedDictionary["Key1"]);
        }

        private class POCO { }

        [Theory]
        [InlineData("{}{}")]
        [InlineData("{}1")]
        [InlineData("{}/**/")]
        [InlineData("{} /**/")]
        public static void TooMuchJsonFails(string json)
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<POCO>(json));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<POCO>(jsonBytes));
            Assert.Throws<JsonException>(() => JsonSerializer.DeserializeAsync<POCO>(new MemoryStream(jsonBytes)).Result);

            // Using a reader directly doesn't throw since it stops once POCO is read.
            Utf8JsonReader reader = new Utf8JsonReader(jsonBytes);
            JsonSerializer.Deserialize<POCO>(ref reader);

            Assert.True(reader.BytesConsumed > 0);
            Assert.NotEqual(jsonBytes.Length, reader.BytesConsumed);
        }

        [Theory]
        [InlineData("{")]
        [InlineData(@"{""")]
        [InlineData(@"{""a""")]
        [InlineData(@"{""a"":")]
        [InlineData(@"{""a"":1")]
        [InlineData(@"{""a"":1,")]
        public static void TooLittleJsonFails(string json)
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json);

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<POCO>(json));
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<POCO>(jsonBytes));
            Assert.Throws<JsonException>(() => JsonSerializer.DeserializeAsync<POCO>(new MemoryStream(jsonBytes)).Result);

            // Using a reader directly throws since it can't read full object.
            Utf8JsonReader reader = new Utf8JsonReader(jsonBytes);
            try
            {
                JsonSerializer.Deserialize<POCO>(ref reader);
                Assert.True(false, "Expected exception.");
            }
            catch (JsonException) { }

            Assert.Equal(0, reader.BytesConsumed);
        }
    }
}

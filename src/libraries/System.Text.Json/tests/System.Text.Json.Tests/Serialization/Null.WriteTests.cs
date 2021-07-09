// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class NullTests
    {
        [Fact]
        public static void DefaultIgnoreNullValuesOnWrite()
        {
            var obj = new TestClassWithInitializedProperties
            {
                MyString = null,
                MyInt = null,
                MyDateTime = null,
                MyIntArray = null,
                MyIntList = null,
                MyNullableIntList = null,
                MyObjectList = new List<object> { null },
                MyListList = new List<List<object>> { new List<object> { null } },
                MyDictionaryList = new List<Dictionary<string, string>> { new Dictionary<string, string>() { ["key"] = null } },
                MyStringDictionary = new Dictionary<string, string>() { ["key"] = null },
                MyNullableDateTimeDictionary = new Dictionary<string, DateTime?>() { ["key"] = null },
                MyObjectDictionary = new Dictionary<string, object>() { ["key"] = null },
                MyStringDictionaryDictionary = new Dictionary<string, Dictionary<string, string>>() { ["key"] = null },
                MyListDictionary = new Dictionary<string, List<object>>() { ["key"] = null },
                MyObjectDictionaryDictionary = new Dictionary<string, Dictionary<string, object>>() { ["key"] = null }
            };

            string json = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""MyString"":null", json);
            Assert.Contains(@"""MyInt"":null", json);
            Assert.Contains(@"""MyDateTime"":null", json);
            Assert.Contains(@"""MyIntArray"":null", json);
            Assert.Contains(@"""MyIntList"":null", json);
            Assert.Contains(@"""MyNullableIntList"":null", json);
            Assert.Contains(@"""MyObjectList"":[null],", json);
            Assert.Contains(@"""MyListList"":[[null]],", json);
            Assert.Contains(@"""MyDictionaryList"":[{""key"":null}],", json);
            Assert.Contains(@"""MyStringDictionary"":{""key"":null},", json);
            Assert.Contains(@"""MyNullableDateTimeDictionary"":{""key"":null},", json);
            Assert.Contains(@"""MyObjectDictionary"":{""key"":null},", json);
            Assert.Contains(@"""MyStringDictionaryDictionary"":{""key"":null},", json);
            Assert.Contains(@"""MyListDictionary"":{""key"":null},", json);
            Assert.Contains(@"""MyObjectDictionaryDictionary"":{""key"":null}", json);
        }

        [Fact]
        public static void EnableIgnoreNullValuesOnWrite()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IgnoreNullValues = true;

            var obj = new TestClassWithInitializedProperties
            {
                MyString = null,
                MyInt = null,
                MyDateTime = null,
                MyIntArray = null,
                MyIntList = null,
                MyNullableIntList = null,
                MyObjectList = new List<object> { null },
                MyListList = new List<List<object>> { new List<object> { null } },
                MyDictionaryList = new List<Dictionary<string, string>> { new Dictionary<string, string>() { ["key"] = null } },
                MyStringDictionary = new Dictionary<string, string>() { ["key"] = null },
                MyNullableDateTimeDictionary = new Dictionary<string, DateTime?>() { ["key"] = null },
                MyObjectDictionary = new Dictionary<string, object>() { ["key"] = null },
                MyStringDictionaryDictionary = new Dictionary<string, Dictionary<string, string>>() { ["key"] = new Dictionary<string, string>() { ["key"] = null } },
                MyListDictionary = new Dictionary<string, List<object>>() { ["key"] = new List<object> { null } },
                MyObjectDictionaryDictionary = new Dictionary<string, Dictionary<string, object>>() { ["key"] = new Dictionary<string, object>() { ["key"] = null } }
            };

            string json = JsonSerializer.Serialize(obj, options);

            // Roundtrip to verify serialize is accurate.
            TestClassWithInitializedProperties newObj = JsonSerializer.Deserialize<TestClassWithInitializedProperties>(json);
            Assert.Equal("Hello", newObj.MyString);
            Assert.Equal(1, newObj.MyInt);
            Assert.Equal(new DateTime(1995, 4, 16), newObj.MyDateTime);
            Assert.Equal(1, newObj.MyIntArray[0]);
            Assert.Equal(1, newObj.MyIntList[0]);
            Assert.Equal(1, newObj.MyNullableIntList[0]);

            Assert.Null(newObj.MyObjectList[0]);
            Assert.Null(newObj.MyObjectList[0]);
            Assert.Null(newObj.MyListList[0][0]);
            Assert.Null(newObj.MyDictionaryList[0]["key"]);
            Assert.Null(newObj.MyStringDictionary["key"]);
            Assert.Null(newObj.MyNullableDateTimeDictionary["key"]);
            Assert.Null(newObj.MyObjectDictionary["key"]);
            Assert.Null(newObj.MyStringDictionaryDictionary["key"]["key"]);
            Assert.Null(newObj.MyListDictionary["key"][0]);
            Assert.Null(newObj.MyObjectDictionaryDictionary["key"]["key"]);

            var parentObj = new WrapperForTestClassWithInitializedProperties
            {
                MyClass = obj
            };
            json = JsonSerializer.Serialize(parentObj, options);

            // Roundtrip to ensure serialize is accurate.
            WrapperForTestClassWithInitializedProperties newParentObj = JsonSerializer.Deserialize<WrapperForTestClassWithInitializedProperties>(json);
            TestClassWithInitializedProperties nestedObj = newParentObj.MyClass;
            Assert.Equal("Hello", nestedObj.MyString);
            Assert.Equal(1, nestedObj.MyInt);
            Assert.Equal(new DateTime(1995, 4, 16), nestedObj.MyDateTime);
            Assert.Equal(1, nestedObj.MyIntArray[0]);
            Assert.Equal(1, nestedObj.MyIntList[0]);
            Assert.Equal(1, nestedObj.MyNullableIntList[0]);

            Assert.Null(nestedObj.MyObjectList[0]);
            Assert.Null(nestedObj.MyObjectList[0]);
            Assert.Null(nestedObj.MyListList[0][0]);
            Assert.Null(nestedObj.MyDictionaryList[0]["key"]);
            Assert.Null(nestedObj.MyStringDictionary["key"]);
            Assert.Null(nestedObj.MyNullableDateTimeDictionary["key"]);
            Assert.Null(nestedObj.MyObjectDictionary["key"]);
            Assert.Null(nestedObj.MyStringDictionaryDictionary["key"]["key"]);
            Assert.Null(nestedObj.MyListDictionary["key"][0]);
            Assert.Null(nestedObj.MyObjectDictionaryDictionary["key"]["key"]);
        }

        [Fact]
        public static void NullReferences()
        {
            var obj = new ObjectWithObjectProperties();
            obj.Address = null;
            obj.Array = null;
            obj.List = null;
            obj.IEnumerableT = null;
            obj.IListT = null;
            obj.ICollectionT = null;
            obj.IReadOnlyCollectionT = null;
            obj.IReadOnlyListT = null;
            obj.StackT = null;
            obj.QueueT = null;
            obj.HashSetT = null;
            obj.LinkedListT = null;
            obj.SortedSetT = null;
            obj.NullableInt = null;
            obj.NullableIntArray = null;
            obj.Object = null;

            string json = JsonSerializer.Serialize(obj);
            Assert.Contains(@"""Address"":null", json);
            Assert.Contains(@"""List"":null", json);
            Assert.Contains(@"""Array"":null", json);
            Assert.Contains(@"""IEnumerableT"":null", json);
            Assert.Contains(@"""IListT"":null", json);
            Assert.Contains(@"""ICollectionT"":null", json);
            Assert.Contains(@"""IReadOnlyCollectionT"":null", json);
            Assert.Contains(@"""IReadOnlyListT"":null", json);
            Assert.Contains(@"""StackT"":null", json);
            Assert.Contains(@"""QueueT"":null", json);
            Assert.Contains(@"""HashSetT"":null", json);
            Assert.Contains(@"""LinkedListT"":null", json);
            Assert.Contains(@"""SortedSetT"":null", json);
            Assert.Contains(@"""NullableInt"":null", json);
            Assert.Contains(@"""Object"":null", json);
            Assert.Contains(@"""NullableIntArray"":null", json);
        }

        [Fact]
        public static void NullArrayElement()
        {
            string json = JsonSerializer.Serialize(new ObjectWithObjectProperties[]{ null });
            Assert.Equal("[null]", json);
        }

        [Fact]
        public static void NullArgumentFail()
        {
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Serialize("", (Type)null));
        }

        [Fact]
        public static void NullObjectOutput()
        {
            {
                string output = JsonSerializer.Serialize<string>(null);
                Assert.Equal("null", output);
            }

            {
                string output = JsonSerializer.Serialize<string>(null, options: null);
                Assert.Equal("null", output);
            }
        }

        class WrapperForTestClassWithInitializedProperties
        {
            public TestClassWithInitializedProperties MyClass { get; set; }
        }

        [Fact]
        public static void SerializeDictionaryWithNullValues()
        {
            Dictionary<string, string> StringVals = new Dictionary<string, string>()
            {
                ["key"] = null,
            };
            Assert.Equal(@"{""key"":null}", JsonSerializer.Serialize(StringVals));

            Dictionary<string, object> ObjVals = new Dictionary<string, object>()
            {
                ["key"] = null,
            };
            Assert.Equal(@"{""key"":null}", JsonSerializer.Serialize(ObjVals));

            Dictionary<string, Dictionary<string, string>> StringDictVals = new Dictionary<string, Dictionary<string, string>>()
            {
                ["key"] = null,
            };
            Assert.Equal(@"{""key"":null}", JsonSerializer.Serialize(StringDictVals));

            Dictionary<string, Dictionary<string, object>> ObjectDictVals = new Dictionary<string, Dictionary<string, object>>()
            {
                ["key"] = null,
            };
            Assert.Equal(@"{""key"":null}", JsonSerializer.Serialize(ObjectDictVals));
        }

        [Fact]
        public static void WritePocoArray()
        {
            var input = new MyPoco[] { null, new MyPoco { Foo = "foo" } };

            string json = JsonSerializer.Serialize(input, new JsonSerializerOptions { Converters = { new MyPocoConverter() } });
            Assert.Equal("[null,{\"Foo\":\"foo\"}]", json);
        }

        private class MyPoco
        {
            public string Foo { get; set; }
        }

        private class MyPocoConverter : JsonConverter<MyPoco>
        {
            public override MyPoco Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotImplementedException();
            }

            public override void Write(Utf8JsonWriter writer, MyPoco value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    throw new InvalidOperationException("The custom converter should never get called with null value.");
                }

                writer.WriteStartObject();
                writer.WriteString(nameof(value.Foo), value.Foo);
                writer.WriteEndObject();
            }
        }

        [Fact]
        public static void InvalidRootOnWrite()
        {
            int[,] arr = null;
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize<int[,]>(arr));

            var options = new JsonSerializerOptions
            {
                IgnoreNullValues = true
            };

            // We still throw when we have an unsupported root.
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize<int[,]>(arr, options));
        }
    }
}

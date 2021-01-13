// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Text.Encodings.Web;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class DictionaryTests
    {
        [Fact]
        public static void DictionaryOfString()
        {
            const string JsonString = @"{""Hello"":""World"",""Hello2"":""World2""}";
            const string ReorderedJsonString = @"{""Hello2"":""World2"",""Hello"":""World""}";

            {
                IDictionary obj = JsonSerializer.Deserialize<IDictionary>(JsonString);
                Assert.Equal("World", ((JsonElement)obj["Hello"]).GetString());
                Assert.Equal("World2", ((JsonElement)obj["Hello2"]).GetString());

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                Dictionary<string, string> obj = JsonSerializer.Deserialize<Dictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                SortedDictionary<string, string> obj = JsonSerializer.Deserialize<SortedDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                IDictionary<string, string> obj = JsonSerializer.Deserialize<IDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                IReadOnlyDictionary<string, string> obj = JsonSerializer.Deserialize<IReadOnlyDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                ImmutableDictionary<string, string> obj = JsonSerializer.Deserialize<ImmutableDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                IImmutableDictionary<string, string> obj = JsonSerializer.Deserialize<IImmutableDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                ImmutableSortedDictionary<string, string> obj = JsonSerializer.Deserialize<ImmutableSortedDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.True(JsonString == json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.True(JsonString == json);
            }

            {
                Hashtable obj = JsonSerializer.Deserialize<Hashtable>(JsonString);
                Assert.Equal("World", ((JsonElement)obj["Hello"]).GetString());
                Assert.Equal("World2", ((JsonElement)obj["Hello2"]).GetString());

                string json = JsonSerializer.Serialize(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                SortedList obj = JsonSerializer.Deserialize<SortedList>(JsonString);
                Assert.Equal("World", ((JsonElement)obj["Hello"]).GetString());
                Assert.Equal("World2", ((JsonElement)obj["Hello2"]).GetString());

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }
        }

        [Fact]
        public static void ImplementsDictionary_DictionaryOfString()
        {
            const string JsonString = @"{""Hello"":""World"",""Hello2"":""World2""}";
            const string ReorderedJsonString = @"{""Hello2"":""World2"",""Hello"":""World""}";

            {
                WrapperForIDictionary obj = JsonSerializer.Deserialize<WrapperForIDictionary>(JsonString);
                Assert.Equal("World", ((JsonElement)obj["Hello"]).GetString());
                Assert.Equal("World2", ((JsonElement)obj["Hello2"]).GetString());

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                StringToStringDictionaryWrapper obj = JsonSerializer.Deserialize<StringToStringDictionaryWrapper>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                StringToStringSortedDictionaryWrapper obj = JsonSerializer.Deserialize<StringToStringSortedDictionaryWrapper>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                GenericIDictionaryWrapper<string, string> obj = JsonSerializer.Deserialize<GenericIDictionaryWrapper<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<GenericIReadOnlyDictionaryWrapper<string, string>>(JsonString));

                GenericIReadOnlyDictionaryWrapper<string, string> obj = new GenericIReadOnlyDictionaryWrapper<string, string>(new Dictionary<string, string>()
                {
                    { "Hello", "World" },
                    { "Hello2", "World2" },
                });
                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<StringToStringIImmutableDictionaryWrapper>(JsonString));

                StringToStringIImmutableDictionaryWrapper obj = new StringToStringIImmutableDictionaryWrapper(new Dictionary<string, string>()
                {
                    { "Hello", "World" },
                    { "Hello2", "World2" },
                });

                string json = JsonSerializer.Serialize(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                HashtableWrapper obj = JsonSerializer.Deserialize<HashtableWrapper>(JsonString);
                Assert.Equal("World", ((JsonElement)obj["Hello"]).GetString());
                Assert.Equal("World2", ((JsonElement)obj["Hello2"]).GetString());

                string json = JsonSerializer.Serialize(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                SortedListWrapper obj = JsonSerializer.Deserialize<SortedListWrapper>(JsonString);
                Assert.Equal("World", ((JsonElement)obj["Hello"]).GetString());
                Assert.Equal("World2", ((JsonElement)obj["Hello2"]).GetString());

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                GenericStructIDictionaryWrapper<string, string> obj = JsonSerializer.Deserialize<GenericStructIDictionaryWrapper<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                GenericStructIDictionaryWrapper<string, string>? obj = JsonSerializer.Deserialize<GenericStructIDictionaryWrapper<string, string>?>(JsonString);
                Assert.True(obj.HasValue);
                Assert.Equal("World", obj.Value["Hello"]);
                Assert.Equal("World2", obj.Value["Hello2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                GenericStructIDictionaryWrapper<string, string>? obj = JsonSerializer.Deserialize<GenericStructIDictionaryWrapper<string, string>?>("null");
                Assert.False(obj.HasValue);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal("null", json);
            }

            {
                GenericStructIDictionaryWrapper<string, string> obj = default;
                string json = JsonSerializer.Serialize(obj);
                Assert.Equal("{}", json);
            }

            {
                StructWrapperForIDictionary obj = default;
                string json = JsonSerializer.Serialize(obj);
                Assert.Equal("{}", json);
            }
        }

        [Fact]
        public static void DictionaryOfObject()
        {
            {
                Dictionary<string, object> obj = JsonSerializer.Deserialize<Dictionary<string, object>>(@"{""Key1"":1}");
                Assert.Equal(1, obj.Count);
                JsonElement element = (JsonElement)obj["Key1"];
                Assert.Equal(JsonValueKind.Number, element.ValueKind);
                Assert.Equal(1, element.GetInt32());

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(@"{""Key1"":1}", json);
            }

            {
                IDictionary<string, object> obj = JsonSerializer.Deserialize<IDictionary<string, object>>(@"{""Key1"":1}");
                Assert.Equal(1, obj.Count);
                JsonElement element = (JsonElement)obj["Key1"];
                Assert.Equal(JsonValueKind.Number, element.ValueKind);
                Assert.Equal(1, element.GetInt32());

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(@"{""Key1"":1}", json);
            }
        }

        [Fact]
        public static void ImplementsIDictionaryOfObject()
        {
            var input = new GenericIDictionaryWrapper<string, object>(new Dictionary<string, object>
            {
                { "Name", "David" },
                { "Age", 32 }
            });

            string json = JsonSerializer.Serialize(input, typeof(IDictionary<string, object>));
            Assert.Equal(@"{""Name"":""David"",""Age"":32}", json);

            IDictionary<string, object> obj = JsonSerializer.Deserialize<IDictionary<string, object>>(json);
            Assert.Equal(2, obj.Count);
            Assert.Equal("David", ((JsonElement)obj["Name"]).GetString());
            Assert.Equal(32, ((JsonElement)obj["Age"]).GetInt32());
        }

        [Fact]
        public static void ImplementsIDictionaryOfString()
        {
            var input = new GenericIDictionaryWrapper<string, string>(new Dictionary<string, string>
            {
                { "Name", "David" },
                { "Job", "Software Architect" }
            });

            string json = JsonSerializer.Serialize(input, typeof(IDictionary<string, string>));
            Assert.Equal(@"{""Name"":""David"",""Job"":""Software Architect""}", json);

            IDictionary<string, string> obj = JsonSerializer.Deserialize<IDictionary<string, string>>(json);
            Assert.Equal(2, obj.Count);
            Assert.Equal("David", obj["Name"]);
            Assert.Equal("Software Architect", obj["Job"]);
        }

        [Fact]
        public static void PocoWithDictionaryObject()
        {
            PocoDictionary dict = JsonSerializer.Deserialize<PocoDictionary>("{\n\t\"key\" : {\"a\" : \"b\", \"c\" : \"d\"}}");
            Assert.Equal("b", dict.key["a"]);
            Assert.Equal("d", dict.key["c"]);
        }

        [Fact]
        public static void DictionaryOfObject_NonPrimitiveTypes()
        {
            // https://github.com/dotnet/runtime/issues/29504
            Dictionary<string, object> dictionary = new Dictionary<string, object>
            {
                ["key"] = new Poco { Id = 10 },
            };

            string json = JsonSerializer.Serialize(dictionary);
            Assert.Equal(@"{""key"":{""Id"":10}}", json);

            dictionary = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            Assert.Equal(1, dictionary.Count);
            JsonElement element = (JsonElement)dictionary["key"];
            Assert.Equal(@"{""Id"":10}", element.ToString());
        }

        [Fact]
        public static void DictionaryOfList()
        {
            const string JsonString = @"{""Key1"":[1,2],""Key2"":[3,4]}";

            {
                IDictionary obj = JsonSerializer.Deserialize<IDictionary>(JsonString);

                Assert.Equal(2, obj.Count);

                int expectedNumber = 1;

                JsonElement element = (JsonElement)obj["Key1"];
                foreach (JsonElement value in element.EnumerateArray())
                {
                    Assert.Equal(expectedNumber++, value.GetInt32());
                }

                element = (JsonElement)obj["Key2"];
                foreach (JsonElement value in element.EnumerateArray())
                {
                    Assert.Equal(expectedNumber++, value.GetInt32());
                }

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);
            }

            {
                IDictionary<string, List<int>> obj = JsonSerializer.Deserialize<IDictionary<string, List<int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"][0]);
                Assert.Equal(2, obj["Key1"][1]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"][0]);
                Assert.Equal(4, obj["Key2"][1]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);
            }

            {
                ImmutableDictionary<string, List<int>> obj = JsonSerializer.Deserialize<ImmutableDictionary<string, List<int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"][0]);
                Assert.Equal(2, obj["Key1"][1]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"][0]);
                Assert.Equal(4, obj["Key2"][1]);

                string json = JsonSerializer.Serialize(obj);
                const string ReorderedJsonString = @"{""Key2"":[3,4],""Key1"":[1,2]}";
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                IImmutableDictionary<string, List<int>> obj = JsonSerializer.Deserialize<IImmutableDictionary<string, List<int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"][0]);
                Assert.Equal(2, obj["Key1"][1]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"][0]);
                Assert.Equal(4, obj["Key2"][1]);


                string json = JsonSerializer.Serialize(obj);
                const string ReorderedJsonString = @"{""Key2"":[3,4],""Key1"":[1,2]}";
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }
        }

        [Fact]
        public static void DictionaryOfArray()
        {
            const string JsonString = @"{""Key1"":[1,2],""Key2"":[3,4]}";
            Dictionary<string, int[]> obj = JsonSerializer.Deserialize<Dictionary<string, int[]>>(JsonString);

            Assert.Equal(2, obj.Count);
            Assert.Equal(2, obj["Key1"].Length);
            Assert.Equal(1, obj["Key1"][0]);
            Assert.Equal(2, obj["Key1"][1]);
            Assert.Equal(2, obj["Key2"].Length);
            Assert.Equal(3, obj["Key2"][0]);
            Assert.Equal(4, obj["Key2"][1]);

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(JsonString, json);
        }

        [Fact]
        public static void ListOfDictionary()
        {
            const string JsonString = @"[{""Key1"":1,""Key2"":2},{""Key1"":3,""Key2"":4}]";

            {
                List<Dictionary<string, int>> obj = JsonSerializer.Deserialize<List<Dictionary<string, int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj[0].Count);
                Assert.Equal(1, obj[0]["Key1"]);
                Assert.Equal(2, obj[0]["Key2"]);
                Assert.Equal(2, obj[1].Count);
                Assert.Equal(3, obj[1]["Key1"]);
                Assert.Equal(4, obj[1]["Key2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }
            {
                List<ImmutableSortedDictionary<string, int>> obj = JsonSerializer.Deserialize<List<ImmutableSortedDictionary<string, int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj[0].Count);
                Assert.Equal(1, obj[0]["Key1"]);
                Assert.Equal(2, obj[0]["Key2"]);
                Assert.Equal(2, obj[1].Count);
                Assert.Equal(3, obj[1]["Key1"]);
                Assert.Equal(4, obj[1]["Key2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }
        }

        [Fact]
        public static void ArrayOfDictionary()
        {
            const string JsonString = @"[{""Key1"":1,""Key2"":2},{""Key1"":3,""Key2"":4}]";

            {
                Dictionary<string, int>[] obj = JsonSerializer.Deserialize<Dictionary<string, int>[]>(JsonString);

                Assert.Equal(2, obj.Length);
                Assert.Equal(2, obj[0].Count);
                Assert.Equal(1, obj[0]["Key1"]);
                Assert.Equal(2, obj[0]["Key2"]);
                Assert.Equal(2, obj[1].Count);
                Assert.Equal(3, obj[1]["Key1"]);
                Assert.Equal(4, obj[1]["Key2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                ImmutableSortedDictionary<string, int>[] obj = JsonSerializer.Deserialize<ImmutableSortedDictionary<string, int>[]>(JsonString);

                Assert.Equal(2, obj.Length);
                Assert.Equal(2, obj[0].Count);
                Assert.Equal(1, obj[0]["Key1"]);
                Assert.Equal(2, obj[0]["Key2"]);
                Assert.Equal(2, obj[1].Count);
                Assert.Equal(3, obj[1]["Key1"]);
                Assert.Equal(4, obj[1]["Key2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }
        }

        [Fact]
        public static void DictionaryOfDictionary()
        {
            const string JsonString = @"{""Key1"":{""Key1a"":1,""Key1b"":2},""Key2"":{""Key2a"":3,""Key2b"":4}}";

            {
                Dictionary<string, Dictionary<string, int>> obj = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"]["Key1a"]);
                Assert.Equal(2, obj["Key1"]["Key1b"]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"]["Key2a"]);
                Assert.Equal(4, obj["Key2"]["Key2b"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                ImmutableSortedDictionary<string, ImmutableSortedDictionary<string, int>> obj = JsonSerializer.Deserialize<ImmutableSortedDictionary<string, ImmutableSortedDictionary<string, int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"]["Key1a"]);
                Assert.Equal(2, obj["Key1"]["Key1b"]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"]["Key2a"]);
                Assert.Equal(4, obj["Key2"]["Key2b"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }
        }

        [Fact]
        public static void DictionaryOfDictionaryOfDictionary()
        {
            const string JsonString = @"{""Key1"":{""Key1"":{""Key1"":1,""Key2"":2},""Key2"":{""Key1"":3,""Key2"":4}},""Key2"":{""Key1"":{""Key1"":5,""Key2"":6},""Key2"":{""Key1"":7,""Key2"":8}}}";
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> obj = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, Dictionary<string, int>>>>(JsonString);

            Assert.Equal(2, obj.Count);
            Assert.Equal(2, obj["Key1"].Count);
            Assert.Equal(2, obj["Key1"]["Key1"].Count);
            Assert.Equal(2, obj["Key1"]["Key2"].Count);

            Assert.Equal(1, obj["Key1"]["Key1"]["Key1"]);
            Assert.Equal(2, obj["Key1"]["Key1"]["Key2"]);
            Assert.Equal(3, obj["Key1"]["Key2"]["Key1"]);
            Assert.Equal(4, obj["Key1"]["Key2"]["Key2"]);

            Assert.Equal(2, obj["Key2"].Count);
            Assert.Equal(2, obj["Key2"]["Key1"].Count);
            Assert.Equal(2, obj["Key2"]["Key2"].Count);

            Assert.Equal(5, obj["Key2"]["Key1"]["Key1"]);
            Assert.Equal(6, obj["Key2"]["Key1"]["Key2"]);
            Assert.Equal(7, obj["Key2"]["Key2"]["Key1"]);
            Assert.Equal(8, obj["Key2"]["Key2"]["Key2"]);

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(JsonString, json);

            // Verify that typeof(object) doesn't interfere.
            json = JsonSerializer.Serialize<object>(obj);
            Assert.Equal(JsonString, json);
        }

        [Fact]
        public static void DictionaryOfArrayOfDictionary()
        {
            const string JsonString = @"{""Key1"":[{""Key1"":1,""Key2"":2},{""Key1"":3,""Key2"":4}],""Key2"":[{""Key1"":5,""Key2"":6},{""Key1"":7,""Key2"":8}]}";
            Dictionary<string, Dictionary<string, int>[]> obj = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, int>[]>>(JsonString);

            Assert.Equal(2, obj.Count);
            Assert.Equal(2, obj["Key1"].Length);
            Assert.Equal(2, obj["Key1"][0].Count);
            Assert.Equal(2, obj["Key1"][1].Count);

            Assert.Equal(1, obj["Key1"][0]["Key1"]);
            Assert.Equal(2, obj["Key1"][0]["Key2"]);
            Assert.Equal(3, obj["Key1"][1]["Key1"]);
            Assert.Equal(4, obj["Key1"][1]["Key2"]);

            Assert.Equal(2, obj["Key2"].Length);
            Assert.Equal(2, obj["Key2"][0].Count);
            Assert.Equal(2, obj["Key2"][1].Count);

            Assert.Equal(5, obj["Key2"][0]["Key1"]);
            Assert.Equal(6, obj["Key2"][0]["Key2"]);
            Assert.Equal(7, obj["Key2"][1]["Key1"]);
            Assert.Equal(8, obj["Key2"][1]["Key2"]);

            string json = JsonSerializer.Serialize(obj);
            Assert.Equal(JsonString, json);

            // Verify that typeof(object) doesn't interfere.
            json = JsonSerializer.Serialize<object>(obj);
            Assert.Equal(JsonString, json);
        }

        private interface IClass { }

        private class MyClass : IClass { }

        private class MyNonGenericDictionary : Dictionary<string, int> { }

        private class MyFactory : JsonConverterFactory
        {
            public override bool CanConvert(Type typeToConvert)
            {
                return typeToConvert == typeof(IClass) || typeToConvert == typeof(MyClass);
            }

            public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
            {
                if (typeToConvert == typeof(IClass))
                {
                    return new MyStuffConverterForIClass();
                }
                else if (typeToConvert == typeof(MyClass))
                {
                    return new MyStuffConverterForMyClass();
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        private class MyStuffConverterForIClass : JsonConverter<IClass>
        {
            public override IClass Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new MyClass();
            }

            public override void Write(Utf8JsonWriter writer, IClass value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(1);
            }
        }

        private class MyStuffConverterForMyClass : JsonConverter<MyClass>
        {
            public override MyClass Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return new MyClass();
            }

            public override void Write(Utf8JsonWriter writer, MyClass value, JsonSerializerOptions options)
            {
                writer.WriteNumberValue(1);
            }
        }

        // This method generates 316 unique test cases for nested dictionaries up to 4
        // levels deep, along with matching JSON, encompassing the various planes of
        // dictionaries that can be combined: generic, non-generic, BCL, user-derived,
        // immutable, mutable, readonly, concurrent, specialized.
        private static IEnumerable<(Type, string)> NestedDictionaryTypeData()
        {
            string testJson = @"{""Key"":1}";

            List<Type> genericDictTypes = new List<Type>()
            {
                typeof(IDictionary<,>),
                typeof(ConcurrentDictionary<,>),
                typeof(GenericIDictionaryWrapper<,>),
            };

            List<Type> nonGenericDictTypes = new List<Type>()
            {
                typeof(Hashtable),
                typeof(OrderedDictionary),
            };

            List<Type> baseDictionaryTypes = new List<Type>
            {
                typeof(MyNonGenericDictionary),
                typeof(IReadOnlyDictionary<string, MyClass>),
                typeof(ConcurrentDictionary<string, int>),
                typeof(ImmutableDictionary<string, IClass>),
                typeof(GenericIDictionaryWrapper<string, int?>),
            };
            baseDictionaryTypes.AddRange(nonGenericDictTypes);

            // This method has exponential behavior which this depth value significantly impacts.
            // Don't change this value without checking how many test cases are generated and
            // how long the tests run for.
            int maxTestDepth = 4;

            HashSet<(Type, string)> tests = new HashSet<(Type, string)>();

            for (int i = 0; i < maxTestDepth; i++)
            {
                List<Type> newBaseTypes = new List<Type>();

                foreach (Type testType in baseDictionaryTypes)
                {
                    tests.Add((testType, testJson));

                    foreach (Type genericType in genericDictTypes)
                    {
                        newBaseTypes.Add(genericType.MakeGenericType(typeof(string), testType));
                    }

                    newBaseTypes.AddRange(nonGenericDictTypes);
                }

                baseDictionaryTypes = newBaseTypes;
                testJson = @"{""Key"":" + testJson + "}";
            }

            return tests;
        }

        [Fact]
        public static void NestedDictionariesRoundtrip()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new MyFactory());

            foreach ((Type dictionaryType, string testJson) in NestedDictionaryTypeData())
            {
                object dict = JsonSerializer.Deserialize(testJson, dictionaryType, options);
                Assert.Equal(testJson, JsonSerializer.Serialize(dict, options));
            }
        }

        [Fact]
        public static void DictionaryOfClasses()
        {
            {
                IDictionary obj;

                {
                    string json = @"{""Key1"":" + SimpleTestClass.s_json + @",""Key2"":" + SimpleTestClass.s_json + "}";
                    obj = JsonSerializer.Deserialize<IDictionary>(json);
                    Assert.Equal(2, obj.Count);

                    if (obj["Key1"] is JsonElement element)
                    {
                        SimpleTestClass result = JsonSerializer.Deserialize<SimpleTestClass>(element.GetRawText());
                        result.Verify();
                    }
                    else
                    {
                        ((SimpleTestClass)obj["Key1"]).Verify();
                        ((SimpleTestClass)obj["Key2"]).Verify();
                    }
                }

                {
                    // We can't compare against the json string above because property ordering is not deterministic (based on reflection order)
                    // so just round-trip the json and compare.
                    string json = JsonSerializer.Serialize(obj);
                    obj = JsonSerializer.Deserialize<IDictionary>(json);
                    Assert.Equal(2, obj.Count);

                    if (obj["Key1"] is JsonElement element)
                    {
                        SimpleTestClass result = JsonSerializer.Deserialize<SimpleTestClass>(element.GetRawText());
                        result.Verify();
                    }
                    else
                    {
                        ((SimpleTestClass)obj["Key1"]).Verify();
                        ((SimpleTestClass)obj["Key2"]).Verify();
                    }
                }

                {
                    string json = JsonSerializer.Serialize<object>(obj);
                    obj = JsonSerializer.Deserialize<IDictionary>(json);
                    Assert.Equal(2, obj.Count);

                    if (obj["Key1"] is JsonElement element)
                    {
                        SimpleTestClass result = JsonSerializer.Deserialize<SimpleTestClass>(element.GetRawText());
                        result.Verify();
                    }
                    else
                    {
                        ((SimpleTestClass)obj["Key1"]).Verify();
                        ((SimpleTestClass)obj["Key2"]).Verify();
                    }
                }
            }

            {
                Dictionary<string, SimpleTestClass> obj;

                {
                    string json = @"{""Key1"":" + SimpleTestClass.s_json + @",""Key2"":" + SimpleTestClass.s_json + "}";
                    obj = JsonSerializer.Deserialize<Dictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }

                {
                    // We can't compare against the json string above because property ordering is not deterministic (based on reflection order)
                    // so just round-trip the json and compare.
                    string json = JsonSerializer.Serialize(obj);
                    obj = JsonSerializer.Deserialize<Dictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }

                {
                    string json = JsonSerializer.Serialize<object>(obj);
                    obj = JsonSerializer.Deserialize<Dictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }
            }

            {
                ImmutableSortedDictionary<string, SimpleTestClass> obj;

                {
                    string json = @"{""Key1"":" + SimpleTestClass.s_json + @",""Key2"":" + SimpleTestClass.s_json + "}";
                    obj = JsonSerializer.Deserialize<ImmutableSortedDictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }

                {
                    // We can't compare against the json string above because property ordering is not deterministic (based on reflection order)
                    // so just round-trip the json and compare.
                    string json = JsonSerializer.Serialize(obj);
                    obj = JsonSerializer.Deserialize<ImmutableSortedDictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }

                {
                    string json = JsonSerializer.Serialize<object>(obj);
                    obj = JsonSerializer.Deserialize<ImmutableSortedDictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }
            }
        }

        [Fact]
        public static void UnicodePropertyNames()
        {
            var options = new JsonSerializerOptions();
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            {
                Dictionary<string, int> obj;

                obj = JsonSerializer.Deserialize<Dictionary<string, int>>(@"{""A\u0467"":1}");
                Assert.Equal(1, obj["A\u0467"]);

                // Specifying encoder on options does not impact deserialize.
                obj = JsonSerializer.Deserialize<Dictionary<string, int>>(@"{""A\u0467"":1}", options);
                Assert.Equal(1, obj["A\u0467"]);

                string json;
                // Verify the name is escaped after serialize.
                json = JsonSerializer.Serialize(obj);
                Assert.Equal(@"{""A\u0467"":1}", json);

                // Verify with encoder.
                json = JsonSerializer.Serialize(obj, options);
                Assert.Equal("{\"A\u0467\":1}", json);
            }

            {
                // We want to go over StackallocThreshold=256 to force a pooled allocation, so this property is 200 chars and 400 bytes.
                const int charsInProperty = 200;

                string longPropertyName = new string('\u0467', charsInProperty);

                Dictionary<string, int> obj = JsonSerializer.Deserialize<Dictionary<string, int>>($"{{\"{longPropertyName}\":1}}");
                Assert.Equal(1, obj[longPropertyName]);

                // Verify the name is escaped after serialize.
                string json = JsonSerializer.Serialize(obj);

                // Duplicate the unicode character 'charsInProperty' times.
                string longPropertyNameEscaped = new StringBuilder().Insert(0, @"\u0467", charsInProperty).ToString();

                string expectedJson = $"{{\"{longPropertyNameEscaped}\":1}}";
                Assert.Equal(expectedJson, json);

                // Verify the name is unescaped after deserialize.
                obj = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                Assert.Equal(1, obj[longPropertyName]);
            }
        }

        [Fact]
        public static void CustomEscapingOnPropertyNameAndValue()
        {
            var dict = new Dictionary<string, string>();
            dict.Add("A\u046701", "Value\u0467");

            // Baseline with no escaping.
            var json = JsonSerializer.Serialize(dict);
            Assert.Equal("{\"A\\u046701\":\"Value\\u0467\"}", json);

            // Enable escaping.
            var options = new JsonSerializerOptions();
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            json = JsonSerializer.Serialize(dict, options);
            Assert.Equal("{\"A\u046701\":\"Value\u0467\"}", json);
        }

        [Fact]
        public static void ObjectToStringFail()
        {
            // Baseline
            string json = @"{""MyDictionary"":{""Key"":""Value""}}";
            JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Dictionary<string, string>>(json));
        }

        [Fact]
        public static void ObjectToJsonElement()
        {
            string json = @"{""MyDictionary"":{""Key"":""Value""}}";
            Dictionary<string, JsonElement> result = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
            JsonElement element = result["MyDictionary"];
            Assert.Equal(JsonValueKind.Object, element.ValueKind);
            Assert.Equal("Value", element.GetProperty("Key").GetString());
        }

        [Fact]
        public static void Hashtable()
        {
            const string Json = @"{""Key"":""Value""}";

            IDictionary ht = new Hashtable();
            ht.Add("Key", "Value");
            string json = JsonSerializer.Serialize(ht);

            Assert.Equal(Json, json);

            ht = JsonSerializer.Deserialize<IDictionary>(json);
            Assert.IsType<JsonElement>(ht["Key"]);
            Assert.Equal("Value", ((JsonElement)ht["Key"]).GetString());

            // Verify round-tripped JSON.
            json = JsonSerializer.Serialize(ht);
            Assert.Equal(Json, json);
        }

        [Fact]
        public static void DeserializeDictionaryWithDuplicateKeys()
        {
            // Non-generic IDictionary case.
            IDictionary iDictionary = JsonSerializer.Deserialize<IDictionary>(@"{""Hello"":""World"", ""Hello"":""NewValue""}");
            Assert.Equal("NewValue", iDictionary["Hello"].ToString());

            // Generic IDictionary case.
            IDictionary<string, string> iNonGenericDictionary = JsonSerializer.Deserialize<IDictionary<string, string>>(@"{""Hello"":""World"", ""Hello"":""NewValue""}");
            Assert.Equal("NewValue", iNonGenericDictionary["Hello"]);

            IDictionary<string, object> iNonGenericObjectDictionary = JsonSerializer.Deserialize<IDictionary<string, object>>(@"{""Hello"":""World"", ""Hello"":""NewValue""}");
            Assert.Equal("NewValue", iNonGenericObjectDictionary["Hello"].ToString());

            // Strongly-typed IDictionary<,> case.
            Dictionary<string, string> dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(@"{""Hello"":""World"", ""Hello"":""NewValue""}");
            Assert.Equal("NewValue", dictionary["Hello"]);

            dictionary = JsonSerializer.Deserialize<Dictionary<string, string>>(@"{""Hello"":""World"", ""myKey"" : ""myValue"", ""Hello"":""NewValue""}");
            Assert.Equal("NewValue", dictionary["Hello"]);

            // Weakly-typed IDictionary case.
            Dictionary<string, object> dictionaryObject = JsonSerializer.Deserialize<Dictionary<string, object>>(@"{""Hello"":""World"", ""Hello"": null}");
            Assert.Null(dictionaryObject["Hello"]);
        }

        [Fact]
        public static void DeserializeDictionaryWithDuplicateProperties()
        {
            PocoDuplicate foo = JsonSerializer.Deserialize<PocoDuplicate>(@"{""BoolProperty"": false, ""BoolProperty"": true}");
            Assert.True(foo.BoolProperty);

            foo = JsonSerializer.Deserialize<PocoDuplicate>(@"{""BoolProperty"": false, ""IntProperty"" : 1, ""BoolProperty"": true , ""IntProperty"" : 2}");
            Assert.True(foo.BoolProperty);
            Assert.Equal(2, foo.IntProperty);

            foo = JsonSerializer.Deserialize<PocoDuplicate>(@"{""DictProperty"" : {""a"" : ""b"", ""c"" : ""d""},""DictProperty"" : {""b"" : ""b"", ""c"" : ""e""}}");
            Assert.Equal(2, foo.DictProperty.Count); // We don't concat.
            Assert.Equal("e", foo.DictProperty["c"]);
        }

        public class PocoDuplicate
        {
            public bool BoolProperty { get; set; }
            public int IntProperty { get; set; }
            public Dictionary<string, string> DictProperty { get; set; }
        }

        public class ClassWithPopulatedDictionaryAndNoSetter
        {
            public ClassWithPopulatedDictionaryAndNoSetter()
            {
                MyImmutableDictionary = MyImmutableDictionary.Add("Key", "Value");
            }

            public Dictionary<string, string> MyDictionary { get; } = new Dictionary<string, string>() { { "Key", "Value" } };
            public ImmutableDictionary<string, string> MyImmutableDictionary { get; } = ImmutableDictionary.Create<string, string>();
        }

        [Fact]
        public static void ClassWithNoSetterAndDictionary()
        {
            // We don't attempt to deserialize into dictionaries without a setter.
            string json = @"{""MyDictionary"":{""Key1"":""Value1"", ""Key2"":""Value2""}}";
            ClassWithPopulatedDictionaryAndNoSetter obj = JsonSerializer.Deserialize<ClassWithPopulatedDictionaryAndNoSetter>(json);
            Assert.Equal(1, obj.MyDictionary.Count);
        }

        [Fact]
        public static void ClassWithNoSetterAndImmutableDictionary()
        {
            // We don't attempt to deserialize into dictionaries without a setter.
            string json = @"{""MyImmutableDictionary"":{""Key1"":""Value1"", ""Key2"":""Value2""}}";
            ClassWithPopulatedDictionaryAndNoSetter obj = JsonSerializer.Deserialize<ClassWithPopulatedDictionaryAndNoSetter>(json);
            Assert.Equal(1, obj.MyImmutableDictionary.Count);
        }

        public class ClassWithIgnoredDictionary1
        {
            public Dictionary<string, int> Parsed1 { get; set; }
            public Dictionary<string, int> Parsed2 { get; set; }
            public Dictionary<string, int> Skipped3 { get; }
        }

        public class ClassWithIgnoredDictionary2
        {
            public IDictionary<string, int> Parsed1 { get; set; }
            public IDictionary<string, int> Skipped2 { get; }
            public IDictionary<string, int> Parsed3 { get; set; }
        }

        public class ClassWithIgnoredDictionary3
        {
            public Dictionary<string, int> Parsed1 { get; set; }
            public Dictionary<string, int> Skipped2 { get; }
            public Dictionary<string, int> Skipped3 { get; }
        }

        public class ClassWithIgnoredDictionary4
        {
            public Dictionary<string, int> Skipped1 { get; }
            public Dictionary<string, int> Parsed2 { get; set; }
            public Dictionary<string, int> Parsed3 { get; set; }
        }

        public class ClassWithIgnoredDictionary5
        {
            public Dictionary<string, int> Skipped1 { get; }
            public Dictionary<string, int> Parsed2 { get; set; }
            public Dictionary<string, int> Skipped3 { get; }
        }

        public class ClassWithIgnoredDictionary6
        {
            public Dictionary<string, int> Skipped1 { get; }
            public Dictionary<string, int> Skipped2 { get; }
            public Dictionary<string, int> Parsed3 { get; set; }
        }

        public class ClassWithIgnoredDictionary7
        {
            public Dictionary<string, int> Skipped1 { get; }
            public Dictionary<string, int> Skipped2 { get; }
            public Dictionary<string, int> Skipped3 { get; }
        }

        public class ClassWithIgnoredIDictionary
        {
            public IDictionary<string, int> Parsed1 { get; set; }
            public IDictionary<string, int> Skipped2 { get; }
            public IDictionary<string, int> Parsed3 { get; set; }
        }

        public class ClassWithIgnoreAttributeDictionary
        {
            public Dictionary<string, int> Parsed1 { get; set; }
            [JsonIgnore] public Dictionary<string, int> Skipped2 { get; set; } // Note this has a setter.
            public Dictionary<string, int> Parsed3 { get; set; }
        }

        public class ClassWithIgnoredImmutableDictionary
        {
            public ImmutableDictionary<string, int> Parsed1 { get; set; }
            public ImmutableDictionary<string, int> Skipped2 { get; }
            public ImmutableDictionary<string, int> Parsed3 { get; set; }
        }

        [Theory]
        [InlineData(@"{""Parsed1"":{""Key"":1},""Parsed3"":{""Key"":2}}")] // No value for skipped property
        [InlineData(@"{""Parsed1"":{""Key"":1},""Skipped2"":{}, ""Parsed3"":{""Key"":2}}")] // Empty object {} skipped
        [InlineData(@"{""Parsed1"":{""Key"":1},""Skipped2"":null, ""Parsed3"":{""Key"":2}}")] // null object skipped
        [InlineData(@"{""Parsed1"":{""Key"":1},""Skipped2"":{""Key"":9}, ""Parsed3"":{""Key"":2}}")] // Valid "int" values skipped
        // Invalid "int" values:
        [InlineData(@"{""Parsed1"":{""Key"":1},""Skipped2"":{""Key"":[1,2,3]}, ""Parsed3"":{""Key"":2}}")]
        [InlineData(@"{""Parsed1"":{""Key"":1},""Skipped2"":{""Key"":{}}, ""Parsed3"":{""Key"":2}}")]
        [InlineData(@"{""Parsed1"":{""Key"":1},""Skipped2"":{""Key"":null}, ""Parsed3"":{""Key"":2}}")]
        public static void IgnoreDictionaryProperty(string json)
        {
            // Verify deserialization
            ClassWithIgnoredDictionary2 obj = JsonSerializer.Deserialize<ClassWithIgnoredDictionary2>(json);
            Assert.Equal(1, obj.Parsed1.Count);
            Assert.Equal(1, obj.Parsed1["Key"]);
            Assert.Null(obj.Skipped2);
            Assert.Equal(1, obj.Parsed3.Count);
            Assert.Equal(2, obj.Parsed3["Key"]);

            // Round-trip and verify.
            string jsonRoundTripped = JsonSerializer.Serialize(obj);
            ClassWithIgnoredDictionary2 objRoundTripped = JsonSerializer.Deserialize<ClassWithIgnoredDictionary2>(jsonRoundTripped);
            Assert.Equal(1, objRoundTripped.Parsed1.Count);
            Assert.Equal(1, objRoundTripped.Parsed1["Key"]);
            Assert.Null(objRoundTripped.Skipped2);
            Assert.Equal(1, objRoundTripped.Parsed3.Count);
            Assert.Equal(2, objRoundTripped.Parsed3["Key"]);
        }

        [Fact]
        public static void IgnoreDictionaryPropertyWithDifferentOrdering()
        {
            // Verify all combinations of 3 properties with at least one ignore.
            VerifyIgnore<ClassWithIgnoredDictionary1>(false, false, true);
            VerifyIgnore<ClassWithIgnoredDictionary2>(false, true, false);
            VerifyIgnore<ClassWithIgnoredDictionary3>(false, true, true);
            VerifyIgnore<ClassWithIgnoredDictionary4>(true, false, false);
            VerifyIgnore<ClassWithIgnoredDictionary5>(true, false, true);
            VerifyIgnore<ClassWithIgnoredDictionary6>(true, true, false);
            VerifyIgnore<ClassWithIgnoredDictionary7>(true, true, true);

            // Verify single case for IDictionary, [Ignore] and ImmutableDictionary.
            // Also specify addMissing to add additional skipped JSON that does not have a corresponding property.
            VerifyIgnore<ClassWithIgnoredIDictionary>(false, true, false, addMissing: true);
            VerifyIgnore<ClassWithIgnoreAttributeDictionary>(false, true, false, addMissing: true);
            VerifyIgnore<ClassWithIgnoredImmutableDictionary>(false, true, false, addMissing: true);
        }

        private static void VerifyIgnore<T>(bool skip1, bool skip2, bool skip3, bool addMissing = false)
        {
            static IDictionary<string, int> GetProperty(T objectToVerify, string propertyName)
            {
                return (IDictionary<string, int>)objectToVerify.GetType().GetProperty(propertyName).GetValue(objectToVerify);
            }

            void Verify(T objectToVerify)
            {
                if (skip1)
                {
                    Assert.Null(GetProperty(objectToVerify, "Skipped1"));
                }
                else
                {
                    Assert.Equal(1, GetProperty(objectToVerify, "Parsed1")["Key"]);
                }

                if (skip2)
                {
                    Assert.Null(GetProperty(objectToVerify, "Skipped2"));
                }
                else
                {
                    Assert.Equal(2, GetProperty(objectToVerify, "Parsed2")["Key"]);
                }

                if (skip3)
                {
                    Assert.Null(GetProperty(objectToVerify, "Skipped3"));
                }
                else
                {
                    Assert.Equal(3, GetProperty(objectToVerify, "Parsed3")["Key"]);
                }
            }

            // Tests that the parser picks back up after skipping/draining ignored elements.
            StringBuilder json = new StringBuilder(@"{");

            if (addMissing)
            {
                json.Append(@"""MissingProp1"": {},");
            }

            if (skip1)
            {
                json.Append(@"""Skipped1"":{},");
            }
            else
            {
                json.Append(@"""Parsed1"":{""Key"":1},");
            }

            if (addMissing)
            {
                json.Append(@"""MissingProp2"": null,");
            }

            if (skip2)
            {
                json.Append(@"""Skipped2"":{},");
            }
            else
            {
                json.Append(@"""Parsed2"":{""Key"":2},");
            }

            if (addMissing)
            {
                json.Append(@"""MissingProp3"": {""ABC"":{}},");
            }

            if (skip3)
            {
                json.Append(@"""Skipped3"":{}}");
            }
            else
            {
                json.Append(@"""Parsed3"":{""Key"":3}}");
            }

            // Deserialize and verify.
            string jsonString = json.ToString();
            T obj = JsonSerializer.Deserialize<T>(jsonString);
            Verify(obj);

            // Round-trip and verify.
            // Any skipped properties due to lack of a setter will now be "null" when serialized instead of "{}".
            string jsonStringRoundTripped = JsonSerializer.Serialize(obj);
            T objRoundTripped = JsonSerializer.Deserialize<T>(jsonStringRoundTripped);
            Verify(objRoundTripped);
        }

        public class ClassWithPopulatedDictionaryAndSetter
        {
            public ClassWithPopulatedDictionaryAndSetter()
            {
                MyImmutableDictionary = MyImmutableDictionary.Add("Key", "Value");
            }

            public Dictionary<string, string> MyDictionary { get; set; } = new Dictionary<string, string>() { { "Key", "Value" } };
            public ImmutableDictionary<string, string> MyImmutableDictionary { get; set; } = ImmutableDictionary.Create<string, string>();
        }

        [Fact]
        public static void ClassWithPopulatedDictionary()
        {
            // We replace the contents.
            string json = @"{""MyDictionary"":{""Key1"":""Value1"", ""Key2"":""Value2""}}";
            ClassWithPopulatedDictionaryAndSetter obj = JsonSerializer.Deserialize<ClassWithPopulatedDictionaryAndSetter>(json);
            Assert.Equal(2, obj.MyDictionary.Count);
        }

        [Fact]
        public static void ClassWithPopulatedImmutableDictionary()
        {
            // We replace the contents.
            string json = @"{""MyImmutableDictionary"":{""Key1"":""Value1"", ""Key2"":""Value2""}}";
            ClassWithPopulatedDictionaryAndSetter obj = JsonSerializer.Deserialize<ClassWithPopulatedDictionaryAndSetter>(json);
            Assert.Equal(2, obj.MyImmutableDictionary.Count);
        }

        [Fact]
        public static void DictionaryNotSupported()
        {
            string json = @"{""MyDictionary"":{""Key"":""Value""}}";

            NotSupportedException ex = Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ClassWithNotSupportedDictionary>(json));

            // The exception contains the type.
            Assert.Contains(typeof(Dictionary<int[,], int>).ToString(), ex.Message);
        }

        [Fact]
        public static void DictionaryNotSupportedButIgnored()
        {
            string json = @"{""MyDictionary"":{""Key"":1}}";
            ClassWithNotSupportedDictionaryButIgnored obj = JsonSerializer.Deserialize<ClassWithNotSupportedDictionaryButIgnored>(json);
            Assert.Null(obj.MyDictionary);
        }

        // https://github.com/dotnet/runtime/issues/29933
        [Fact]
        public static void Serialize_IDictionaryOfPoco()
        {
            // Arrange
            var value = new AllSingleUpperPropertiesParent()
            {
                Child = new Dictionary<string, AllSingleUpperProperties_Child>()
                {
                    ["1"] = new AllSingleUpperProperties_Child()
                    {
                        A = "1",
                        B = string.Empty,
                        C = Array.Empty<string>(),
                        D = Array.Empty<string>(),
                        F = Array.Empty<string>(),
                        K = Array.Empty<string>(),
                    }
                }
            };

            var actual = JsonSerializer.Serialize(value, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // Assert
            Assert.NotNull(actual);
            Assert.NotEmpty(actual);
        }

        // https://github.com/dotnet/runtime/issues/29933
        [Fact]
        public static void Deserialize_IDictionaryOfPoco()
        {
            // Arrange
            string json = "{\"child\":{\"1\":{\"a\":\"1\",\"b\":\"\",\"c\":[],\"d\":[],\"e\":null,\"f\":[],\"g\":null,\"h\":null,\"i\":null,\"j\":null,\"k\":[]}}}";

            var actual = JsonSerializer.Deserialize<AllSingleUpperPropertiesParent>(json, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // Assert
            Assert.NotNull(actual);
            Assert.NotNull(actual.Child);
            Assert.Equal(1, actual.Child.Count);
            Assert.True(actual.Child.ContainsKey("1"));
            Assert.Equal("1", actual.Child["1"].A);
        }

        // https://github.com/dotnet/runtime/issues/29893
        [Fact]
        public static void ShouldHandleNullInDictionaries_Serialize()
        {
            var value = new ClassWithDictionaryOfString_ChildWithDictionaryOfString()
            {
                Test = "value1",
                Child = new ClassWithDictionaryOfString()
            };

            var actual = JsonSerializer.Serialize(value);
            Assert.Equal("{\"Test\":\"value1\",\"Dict\":null,\"Child\":{\"Test\":null,\"Dict\":null}}", actual);
        }

        // https://github.com/dotnet/runtime/issues/29893
        [Fact]
        public static void ShouldHandleNullInDictionaries_Deserialize()
        {
            var json = "{\"Test\":\"value1\",\"Dict\":null,\"Child\":{\"Test\":null,\"Dict\":null}}";
            ClassWithDictionaryOfString_ChildWithDictionaryOfString actual = JsonSerializer.Deserialize<ClassWithDictionaryOfString_ChildWithDictionaryOfString>(json);

            Assert.Equal("value1", actual.Test);
            Assert.Null(actual.Dict);
            Assert.NotNull(actual.Child);
            Assert.Null(actual.Child.Dict);
            Assert.Null(actual.Child.Test);
        }

        // https://github.com/dotnet/runtime/issues/29893
        [Fact]
        public static void ShouldHandleNullInDictionaries_Serialize_IgnoreNullValues()
        {
            var value = new ClassWithDictionaryOfString_ChildWithDictionaryOfString()
            {
                Test = "value1",
                Child = new ClassWithDictionaryOfString()
            };

            var actual = JsonSerializer.Serialize(value, new JsonSerializerOptions { IgnoreNullValues = true });
            Assert.Equal("{\"Test\":\"value1\",\"Child\":{}}", actual);
        }

        // https://github.com/dotnet/runtime/issues/29893
        [Fact]
        public static void ShouldHandleNullInDictionaries_Deserialize_IgnoreNullValues()
        {
            var json = "{\"Test\":\"value1\",\"Child\":{}}";
            ClassWithDictionaryOfString_ChildWithDictionaryOfString actual = JsonSerializer.Deserialize<ClassWithDictionaryOfString_ChildWithDictionaryOfString>(json);

            Assert.Equal("value1", actual.Test);
            Assert.Null(actual.Dict);
            Assert.NotNull(actual.Child);
            Assert.Null(actual.Child.Dict);
            Assert.Null(actual.Child.Test);
        }

        // https://github.com/dotnet/runtime/issues/29888
        [Fact]
        public static void DictionaryWithNullShouldPreserveOrder_Serialize()
        {
            var dictionaryFirst = new ClassWithDictionaryAndProperty_DictionaryFirst()
            {
                Test = "value1"
            };

            var actual = JsonSerializer.Serialize(dictionaryFirst);
            Assert.Equal("{\"Dict\":null,\"Test\":\"value1\"}", actual);

            var dictionaryLast = new ClassWithDictionaryAndProperty_DictionaryLast()
            {
                Test = "value1"
            };

            actual = JsonSerializer.Serialize(dictionaryLast);
            Assert.Equal("{\"Test\":\"value1\",\"Dict\":null}", actual);
        }

        // https://github.com/dotnet/runtime/issues/29888
        [Fact]
        public static void DictionaryWithNullShouldPreserveOrder_Deserialize()
        {
            var json = "{\"Dict\":null,\"Test\":\"value1\"}";
            ClassWithDictionaryAndProperty_DictionaryFirst dictionaryFirst = JsonSerializer.Deserialize<ClassWithDictionaryAndProperty_DictionaryFirst>(json);

            Assert.Equal("value1", dictionaryFirst.Test);
            Assert.Null(dictionaryFirst.Dict);

            json = "{\"Test\":\"value1\",\"Dict\":null}";
            ClassWithDictionaryAndProperty_DictionaryLast dictionaryLast = JsonSerializer.Deserialize<ClassWithDictionaryAndProperty_DictionaryLast>(json);

            Assert.Equal("value1", dictionaryLast.Test);
            Assert.Null(dictionaryLast.Dict);
        }

        // https://github.com/dotnet/runtime/issues/29888
        [Fact]
        public static void DictionaryWithNullShouldPreserveOrder_Serialize_IgnoreNullValues()
        {
            var dictionaryFirst = new ClassWithDictionaryAndProperty_DictionaryFirst()
            {
                Test = "value1"
            };

            var actual = JsonSerializer.Serialize(dictionaryFirst, new JsonSerializerOptions { IgnoreNullValues = true });
            Assert.Equal("{\"Test\":\"value1\"}", actual);

            var dictionaryLast = new ClassWithDictionaryAndProperty_DictionaryLast()
            {
                Test = "value1"
            };

            actual = JsonSerializer.Serialize(dictionaryLast, new JsonSerializerOptions { IgnoreNullValues = true });
            Assert.Equal("{\"Test\":\"value1\"}", actual);
        }

        // https://github.com/dotnet/runtime/issues/29888
        [Fact]
        public static void DictionaryWithNullShouldPreserveOrder_Deserialize_IgnoreNullValues()
        {
            var json = "{\"Test\":\"value1\"}";
            ClassWithDictionaryAndProperty_DictionaryFirst dictionaryFirst = JsonSerializer.Deserialize<ClassWithDictionaryAndProperty_DictionaryFirst>(json);

            Assert.Equal("value1", dictionaryFirst.Test);
            Assert.Null(dictionaryFirst.Dict);

            json = "{\"Test\":\"value1\"}";
            ClassWithDictionaryAndProperty_DictionaryLast dictionaryLast = JsonSerializer.Deserialize<ClassWithDictionaryAndProperty_DictionaryLast>(json);

            Assert.Equal("value1", dictionaryLast.Test);
            Assert.Null(dictionaryLast.Dict);
        }

        [Fact]
        public static void NullDictionaryValuesShouldDeserializeAsNull()
        {
            const string json =
                    @"{" +
                        @"""StringVals"":{" +
                            @"""key"":null" +
                        @"}," +
                        @"""ObjectVals"":{" +
                            @"""key"":null" +
                        @"}," +
                        @"""StringDictVals"":{" +
                            @"""key"":null" +
                        @"}," +
                        @"""ObjectDictVals"":{" +
                            @"""key"":null" +
                        @"}," +
                        @"""ClassVals"":{" +
                            @"""key"":null" +
                        @"}" +
                    @"}";

            SimpleClassWithDictionaries obj = JsonSerializer.Deserialize<SimpleClassWithDictionaries>(json);
            Assert.Null(obj.StringVals["key"]);
            Assert.Null(obj.ObjectVals["key"]);
            Assert.Null(obj.StringDictVals["key"]);
            Assert.Null(obj.ObjectDictVals["key"]);
            Assert.Null(obj.ClassVals["key"]);
        }

        public class ClassWithNotSupportedDictionary
        {
            public Dictionary<int[,], int> MyDictionary { get; set; }
        }

        public class ClassWithNotSupportedDictionaryButIgnored
        {
            [JsonIgnore] public Dictionary<int[,], int> MyDictionary { get; set; }
        }

        public class AllSingleUpperPropertiesParent
        {
            public IDictionary<string, AllSingleUpperProperties_Child> Child { get; set; }
        }

        public class AllSingleUpperProperties_Child
        {
            public string A { get; set; }
            public string B { get; set; }
            public string[] C { get; set; }
            public string[] D { get; set; }
            public bool? E { get; set; }
            public string[] F { get; set; }
            public DateTimeOffset? G { get; set; }
            public DateTimeOffset? H { get; set; }
            public int? I { get; set; }
            public int? J { get; set; }
            public string[] K { get; set; }
        }

        public class ClassWithDictionaryOfString_ChildWithDictionaryOfString
        {
            public string Test { get; set; }
            public Dictionary<string, string> Dict { get; set; }
            public ClassWithDictionaryOfString Child { get; set; }
        }

        public class ClassWithDictionaryOfString
        {
            public string Test { get; set; }
            public Dictionary<string, string> Dict { get; set; }
        }

        public class ClassWithDictionaryAndProperty_DictionaryLast
        {
            public string Test { get; set; }
            public Dictionary<string, string> Dict { get; set; }
        }

        public class ClassWithDictionaryAndProperty_DictionaryFirst
        {
            public Dictionary<string, string> Dict { get; set; }
            public string Test { get; set; }
        }

        public class SimpleClassWithDictionaries
        {
            public Dictionary<string, string> StringVals { get; set; }
            public Dictionary<string, object> ObjectVals { get; set; }
            public Dictionary<string, Dictionary<string, string>> StringDictVals { get; set; }
            public Dictionary<string, Dictionary<string, object>> ObjectDictVals { get; set; }
            public Dictionary<string, SimpleClassWithDictionaries> ClassVals { get; set; }
        }

        public class DictionaryThatOnlyImplementsIDictionaryOfStringTValue<TValue> : IDictionary<string, TValue>
        {
            IDictionary<string, TValue> _inner = new Dictionary<string, TValue>();

            public TValue this[string key]
            {
                get
                {
                    return _inner[key];
                }
                set
                {
                    _inner[key] = value;
                }
            }

            public ICollection<string> Keys => _inner.Keys;

            public ICollection<TValue> Values => _inner.Values;

            public int Count => _inner.Count;

            public bool IsReadOnly => _inner.IsReadOnly;

            public void Add(string key, TValue value)
            {
                _inner.Add(key, value);
            }

            public void Add(KeyValuePair<string, TValue> item)
            {
                _inner.Add(item);
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(KeyValuePair<string, TValue> item)
            {
                return _inner.Contains(item);
            }

            public bool ContainsKey(string key)
            {
                return _inner.ContainsKey(key);
            }

            public void CopyTo(KeyValuePair<string, TValue>[] array, int arrayIndex)
            {
                // CopyTo should not be called.
                throw new NotImplementedException();
            }

            public IEnumerator<KeyValuePair<string, TValue>> GetEnumerator()
            {
                // Don't return results directly from _inner since that will return an enumerator that returns
                // IDictionaryEnumerator which should not require.
                foreach (KeyValuePair<string, TValue> keyValuePair in _inner)
                {
                    yield return keyValuePair;
                }
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                // This GetEnumerator() should not be called.
                throw new NotImplementedException();
            }

            public bool Remove(string key)
            {
                // Remove should not be called.
                throw new NotImplementedException();
            }

            public bool Remove(KeyValuePair<string, TValue> item)
            {
                // Remove should not be called.
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, out TValue value)
            {
                return _inner.TryGetValue(key, out value);
            }
        }

        [Fact]
        public static void DictionaryOfTOnlyWithStringTValueAsInt()
        {
            const string Json = @"{""One"":1,""Two"":2}";

            DictionaryThatOnlyImplementsIDictionaryOfStringTValue<int> dictionary;

            dictionary = JsonSerializer.Deserialize<DictionaryThatOnlyImplementsIDictionaryOfStringTValue<int>>(Json);
            Assert.Equal(1, dictionary["One"]);
            Assert.Equal(2, dictionary["Two"]);

            string json = JsonSerializer.Serialize(dictionary);
            Assert.Equal(Json, json);
        }

        [Fact]
        public static void DictionaryOfTOnlyWithStringTValueAsPoco()
        {
            const string Json = @"{""One"":{""Id"":1},""Two"":{""Id"":2}}";

            DictionaryThatOnlyImplementsIDictionaryOfStringTValue<Poco> dictionary;

            dictionary = JsonSerializer.Deserialize<DictionaryThatOnlyImplementsIDictionaryOfStringTValue<Poco>>(Json);
            Assert.Equal(1, dictionary["One"].Id);
            Assert.Equal(2, dictionary["Two"].Id);

            string json = JsonSerializer.Serialize(dictionary);
            Assert.Equal(Json, json);
        }

        public class DictionaryThatOnlyImplementsIDictionaryOfStringPoco : DictionaryThatOnlyImplementsIDictionaryOfStringTValue<Poco>
        {
        }

        [Fact]
        public static void DictionaryOfTOnlyWithStringPoco()
        {
            const string Json = @"{""One"":{""Id"":1},""Two"":{""Id"":2}}";

            DictionaryThatOnlyImplementsIDictionaryOfStringPoco dictionary;

            dictionary = JsonSerializer.Deserialize<DictionaryThatOnlyImplementsIDictionaryOfStringPoco>(Json);
            Assert.Equal(1, dictionary["One"].Id);
            Assert.Equal(2, dictionary["Two"].Id);

            string json = JsonSerializer.Serialize(dictionary);
            Assert.Equal(Json, json);
        }

        public class DictionaryThatHasIncompatibleEnumerator : IDictionary
        {
            Hashtable _inner = new Hashtable();

            public object this[string key]
            {
                get
                {
                    return _inner[key];
                }
                set
                {
                    _inner[key] = value;
                }
            }

            public ICollection Keys => _inner.Keys;

            public ICollection Values => _inner.Values;

            public int Count => _inner.Count;

            public bool IsReadOnly => _inner.IsReadOnly;

            public bool IsFixedSize => _inner.IsFixedSize;

            public bool IsSynchronized => throw new NotImplementedException();

            public object SyncRoot => throw new NotImplementedException();

            public object this[object key]
            {
                get
                {
                    return _inner[key];
                }
                set
                {
                    _inner[key] = value;
                }
            }

            public void Add(object key, object value)
            {
                _inner.Add(key, value);
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public bool Contains(object key)
            {
                throw new NotImplementedException();
            }

            public IDictionaryEnumerator GetEnumerator()
            {
                // Throw NotSupportedException so we can detect this GetEnumerator was called.
                throw new NotSupportedException();
            }

            public void Remove(object key)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(Array array, int index)
            {
                throw new NotImplementedException();
            }
        }

        [Fact]
        public static void VerifyDictionaryThatHasIncomatibleEnumeratorWithInt()
        {
            const string Json = @"{""One"":1,""Two"":2}";

            DictionaryThatHasIncompatibleEnumerator dictionary;
            dictionary = JsonSerializer.Deserialize<DictionaryThatHasIncompatibleEnumerator>(Json);
            Assert.Equal(1, ((JsonElement)dictionary["One"]).GetInt32());
            Assert.Equal(2, ((JsonElement)dictionary["Two"]).GetInt32());
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(dictionary));
        }

        [Fact]
        public static void VerifyDictionaryThatHasIncomatibleEnumeratorWithPoco()
        {
            const string Json = @"{""One"":{""Id"":1},""Two"":{""Id"":2}}";

            DictionaryThatHasIncompatibleEnumerator dictionary;
            dictionary = JsonSerializer.Deserialize<DictionaryThatHasIncompatibleEnumerator>(Json);
            Assert.Equal(1, ((JsonElement)dictionary["One"]).GetProperty("Id").GetInt32());
            Assert.Equal(2, ((JsonElement)dictionary["Two"]).GetProperty("Id").GetInt32());
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(dictionary));
        }

        private class ClassWithoutParameterlessCtor
        {
            public ClassWithoutParameterlessCtor(int num) { }
            public string Name { get; set; }
        }

        private class ClassWithInternalParameterlessConstructor
        {
            internal ClassWithInternalParameterlessConstructor() { }
            public string Name { get; set; }
        }

        private class ClassWithPrivateParameterlessConstructor
        {
            private ClassWithPrivateParameterlessConstructor() { }
            public string Name { get; set; }
        }

        [Fact]
        public static void DictionaryWith_ObjectWithNoParameterlessCtor_AsValue_Throws()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Dictionary<string, ClassWithInternalParameterlessConstructor>>(@"{""key"":{}}"));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Dictionary<string, ClassWithPrivateParameterlessConstructor>>(@"{""key"":{}}"));
        }

        [Fact]
        public static void DictionaryWith_ObjectWithNoParameterlessCtor_Serialize_Works()
        {
            var noParameterless = new Dictionary<string, ClassWithoutParameterlessCtor>()
            {
                ["key"] = new ClassWithoutParameterlessCtor(5)
                {
                    Name = "parameterless"
                }
            };

            string json = JsonSerializer.Serialize(noParameterless);
            Assert.Equal("{\"key\":{\"Name\":\"parameterless\"}}", json);

            var onlyInternal = new Dictionary<string, ClassWithInternalParameterlessConstructor>()
            {
                ["key"] = new ClassWithInternalParameterlessConstructor()
                {
                    Name = "internal"
                }
            };

            json = JsonSerializer.Serialize(onlyInternal);
            Assert.Equal("{\"key\":{\"Name\":\"internal\"}}", json);

            var onlyPrivate = new Dictionary<string, ClassWithPrivateParameterlessConstructor>()
            {
                ["key"] = null
            };

            json = JsonSerializer.Serialize(onlyPrivate);
            Assert.Equal("{\"key\":null}", json);
        }
    }
}

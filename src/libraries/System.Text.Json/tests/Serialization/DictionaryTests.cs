// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
                StringToStringIDictionaryWrapper obj = JsonSerializer.Deserialize<StringToStringIDictionaryWrapper>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.Serialize(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.Serialize<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<StringToStringIReadOnlyDictionaryWrapper>(JsonString));

                StringToStringIReadOnlyDictionaryWrapper obj = new StringToStringIReadOnlyDictionaryWrapper(new Dictionary<string, string>()
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
            var input = new StringToObjectIDictionaryWrapper(new Dictionary<string, object>
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
            var input = new StringToStringIDictionaryWrapper(new Dictionary<string, string>
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

        [Theory]
        [InlineData(typeof(ImmutableDictionary<string, string>), "\"headers\"")]
        [InlineData(typeof(Dictionary<string, string>), "\"headers\"")]
        [InlineData(typeof(PocoDictionary), "\"headers\"")]
        public static void InvalidJsonForValueShouldFail(Type type, string json)
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, type));
        }

        [Theory]
        [InlineData(typeof(int[]), @"""test""")]
        [InlineData(typeof(int[]), @"1")]
        [InlineData(typeof(int[]), @"false")]
        [InlineData(typeof(int[]), @"{}")]
        [InlineData(typeof(int[]), @"[""test""")]
        [InlineData(typeof(int[]), @"[true]")]
        [InlineData(typeof(int[]), @"[{}]")]
        [InlineData(typeof(int[]), @"[[]]")]
        [InlineData(typeof(Dictionary<string, int[]>), @"{""test"": {}}")]
        [InlineData(typeof(Dictionary<string, int[]>), @"{""test"": ""test""}")]
        [InlineData(typeof(Dictionary<string, int[]>), @"{""test"": 1}")]
        [InlineData(typeof(Dictionary<string, int[]>), @"{""test"": true}")]
        [InlineData(typeof(Dictionary<string, int[]>), @"{""test"": [""test""]}")]
        [InlineData(typeof(Dictionary<string, int[]>), @"{""test"": [[]]}")]
        [InlineData(typeof(Dictionary<string, int[]>), @"{""test"": [{}]}")]
        public static void InvalidJsonForArrayShouldFail(Type type, string json)
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize(json, type));
        }

        [Fact]
        public static void InvalidEmptyDictionaryInput()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<string>("{}"));
        }

        [Fact]
        public static void PocoWithDictionaryObject()
        {
            PocoDictionary dict = JsonSerializer.Deserialize<PocoDictionary>("{\n\t\"key\" : {\"a\" : \"b\", \"c\" : \"d\"}}");
            Assert.Equal("b", dict.key["a"]);
            Assert.Equal("d", dict.key["c"]);
        }

        public class PocoDictionary
        {
            public Dictionary<string, string> key { get; set; }
        }

        [Fact]
        public static void DictionaryOfObject_37569()
        {
            // https://github.com/dotnet/corefx/issues/37569
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

        public class Poco
        {
            public int Id { get; set; }
        }

        [Fact]
        public static void FirstGenericArgNotStringFail()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<Dictionary<int, int>>(@"{1:1}"));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Deserialize<ImmutableDictionary<int, int>>(@"{1:1}"));
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
            dict.Add("A\u046701","Value\u0467");

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

        [Fact, ActiveIssue("JsonElement fails since it is a struct.")]
        public static void ObjectToJsonElement()
        {
            string json = @"{""MyDictionary"":{""Key"":""Value""}}";
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
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

            try
            {
                JsonSerializer.Deserialize<ClassWithNotSupportedDictionary>(json);
                Assert.True(false, "Expected NotSupportedException to be thrown.");
            }
            catch (NotSupportedException e)
            {
                // The exception should contain className.propertyName and the invalid type.
                Assert.Contains("ClassWithNotSupportedDictionary.MyDictionary", e.Message);
                Assert.Contains("Dictionary`2[System.Int32,System.Int32]", e.Message);
            }
        }

        [Fact]
        public static void DictionaryNotSupportedButIgnored()
        {
            string json = @"{""MyDictionary"":{""Key"":1}}";
            ClassWithNotSupportedDictionaryButIgnored obj = JsonSerializer.Deserialize<ClassWithNotSupportedDictionaryButIgnored>(json);
            Assert.Null(obj.MyDictionary);
        }

        [Fact]
        public static void Regression38643_Serialize()
        {
            // Arrange
            var value = new Regression38643_Parent()
            {
                Child = new Dictionary<string, Regression38643_Child>()
                {
                    ["1"] = new Regression38643_Child()
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

        [Fact]
        public static void Regression38643_Deserialize()
        {
            // Arrange
            string json = "{\"child\":{\"1\":{\"a\":\"1\",\"b\":\"\",\"c\":[],\"d\":[],\"e\":null,\"f\":[],\"g\":null,\"h\":null,\"i\":null,\"j\":null,\"k\":[]}}}";

            var actual = JsonSerializer.Deserialize<Regression38643_Parent>(json, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // Assert
            Assert.NotNull(actual);
            Assert.NotNull(actual.Child);
            Assert.Equal(1, actual.Child.Count);
            Assert.True(actual.Child.ContainsKey("1"));
            Assert.Equal("1", actual.Child["1"].A);
        }

        [Fact]
        public static void Regression38565_Serialize()
        {
            var value = new Regression38565_Parent()
            {
                Test = "value1",
                Child = new Regression38565_Child()
            };

            var actual = JsonSerializer.Serialize(value);
            Assert.Equal("{\"Test\":\"value1\",\"Dict\":null,\"Child\":{\"Test\":null,\"Dict\":null}}", actual);
        }

        [Fact]
        public static void Regression38565_Deserialize()
        {
            var json = "{\"Test\":\"value1\",\"Dict\":null,\"Child\":{\"Test\":null,\"Dict\":null}}";
            Regression38565_Parent actual = JsonSerializer.Deserialize<Regression38565_Parent>(json);

            Assert.Equal("value1", actual.Test);
            Assert.Null(actual.Dict);
            Assert.NotNull(actual.Child);
            Assert.Null(actual.Child.Dict);
            Assert.Null(actual.Child.Test);
        }

        [Fact]
        public static void Regression38565_Serialize_IgnoreNullValues()
        {
            var value = new Regression38565_Parent()
            {
                Test = "value1",
                Child = new Regression38565_Child()
            };

            var actual = JsonSerializer.Serialize(value, new JsonSerializerOptions { IgnoreNullValues = true });
            Assert.Equal("{\"Test\":\"value1\",\"Child\":{}}", actual);
        }

        [Fact]
        public static void Regression38565_Deserialize_IgnoreNullValues()
        {
            var json = "{\"Test\":\"value1\",\"Child\":{}}";
            Regression38565_Parent actual = JsonSerializer.Deserialize<Regression38565_Parent>(json);

            Assert.Equal("value1", actual.Test);
            Assert.Null(actual.Dict);
            Assert.NotNull(actual.Child);
            Assert.Null(actual.Child.Dict);
            Assert.Null(actual.Child.Test);
        }

        [Fact]
        public static void Regression38557_Serialize()
        {
            var dictionaryFirst = new Regression38557_DictionaryFirst()
            {
                Test = "value1"
            };

            var actual = JsonSerializer.Serialize(dictionaryFirst);
            Assert.Equal("{\"Dict\":null,\"Test\":\"value1\"}", actual);

            var dictionaryLast = new Regression38557_DictionaryLast()
            {
                Test = "value1"
            };

            actual = JsonSerializer.Serialize(dictionaryLast);
            Assert.Equal("{\"Test\":\"value1\",\"Dict\":null}", actual);
        }

        [Fact]
        public static void Regression38557_Deserialize()
        {
            var json = "{\"Dict\":null,\"Test\":\"value1\"}";
            Regression38557_DictionaryFirst dictionaryFirst = JsonSerializer.Deserialize<Regression38557_DictionaryFirst>(json);

            Assert.Equal("value1", dictionaryFirst.Test);
            Assert.Null(dictionaryFirst.Dict);

            json = "{\"Test\":\"value1\",\"Dict\":null}";
            Regression38557_DictionaryLast dictionaryLast = JsonSerializer.Deserialize<Regression38557_DictionaryLast>(json);

            Assert.Equal("value1", dictionaryLast.Test);
            Assert.Null(dictionaryLast.Dict);
        }

        [Fact]
        public static void Regression38557_Serialize_IgnoreNullValues()
        {
            var dictionaryFirst = new Regression38557_DictionaryFirst()
            {
                Test = "value1"
            };

            var actual = JsonSerializer.Serialize(dictionaryFirst, new JsonSerializerOptions { IgnoreNullValues = true });
            Assert.Equal("{\"Test\":\"value1\"}", actual);

            var dictionaryLast = new Regression38557_DictionaryLast()
            {
                Test = "value1"
            };

            actual = JsonSerializer.Serialize(dictionaryLast, new JsonSerializerOptions { IgnoreNullValues = true });
            Assert.Equal("{\"Test\":\"value1\"}", actual);
        }

        [Fact]
        public static void Regression38557_Deserialize_IgnoreNullValues()
        {
            var json = "{\"Test\":\"value1\"}";
            Regression38557_DictionaryFirst dictionaryFirst = JsonSerializer.Deserialize<Regression38557_DictionaryFirst>(json);

            Assert.Equal("value1", dictionaryFirst.Test);
            Assert.Null(dictionaryFirst.Dict);

            json = "{\"Test\":\"value1\"}";
            Regression38557_DictionaryLast dictionaryLast = JsonSerializer.Deserialize<Regression38557_DictionaryLast>(json);

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
            public Dictionary<int, int> MyDictionary { get; set; }
        }

        public class ClassWithNotSupportedDictionaryButIgnored
        {
            [JsonIgnore] public Dictionary<int, int> MyDictionary { get; set; }
        }

        public class Regression38643_Parent
        {
            public IDictionary<string, Regression38643_Child> Child { get; set; }
        }

        public class Regression38643_Child
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

        public class Regression38565_Parent
        {
            public string Test { get; set; }
            public Dictionary<string, string> Dict { get; set; }
            public Regression38565_Child Child { get; set; }
        }

        public class Regression38565_Child
        {
            public string Test { get; set; }
            public Dictionary<string, string> Dict { get; set; }
        }

        public class Regression38557_DictionaryLast
        {
            public string Test { get; set; }
            public Dictionary<string, string> Dict { get; set; }
        }

        public class Regression38557_DictionaryFirst
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
                return GetEnumerator();
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

        public class DictionaryThatHasIncomatibleEnumerator<TValue> : IDictionary<string, TValue>
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
                // The generic GetEnumerator() should not be called for this test.
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                // Create an incompatible converter.
                return new int[] {100,200 }.GetEnumerator();
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
        public static void VerifyDictionaryThatHasIncomatibleEnumeratorWithInt()
        {
            const string Json = @"{""One"":1,""Two"":2}";

            DictionaryThatHasIncomatibleEnumerator<int> dictionary;
            dictionary = JsonSerializer.Deserialize<DictionaryThatHasIncomatibleEnumerator<int>>(Json);
            Assert.Equal(1, dictionary["One"]);
            Assert.Equal(2, dictionary["Two"]);
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(dictionary));
        }


        [Fact]
        public static void VerifyDictionaryThatHasIncomatibleEnumeratorWithPoco()
        {
            const string Json = @"{""One"":{""Id"":1},""Two"":{""Id"":2}}";

            DictionaryThatHasIncomatibleEnumerator<Poco> dictionary;
            dictionary = JsonSerializer.Deserialize<DictionaryThatHasIncomatibleEnumerator<Poco>>(Json);
            Assert.Equal(1, dictionary["One"].Id);
            Assert.Equal(2, dictionary["Two"].Id);
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Serialize(dictionary));
        }
    }
}

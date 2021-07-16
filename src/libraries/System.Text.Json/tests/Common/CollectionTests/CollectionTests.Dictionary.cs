// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Collections.Specialized;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public abstract partial class CollectionTests
    {
        [Fact]
        public async Task DictionaryOfString()
        {
            const string JsonString = @"{""Hello"":""World"",""Hello2"":""World2""}";
            const string ReorderedJsonString = @"{""Hello2"":""World2"",""Hello"":""World""}";

            {
                IDictionary obj = await JsonSerializerWrapperForString.DeserializeWrapper<IDictionary>(JsonString);
                Assert.Equal("World", ((JsonElement)obj["Hello"]).GetString());
                Assert.Equal("World2", ((JsonElement)obj["Hello2"]).GetString());

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                Dictionary<string, string> obj = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                SortedDictionary<string, string> obj = await JsonSerializerWrapperForString.DeserializeWrapper<SortedDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                IDictionary<string, string> obj = await JsonSerializerWrapperForString.DeserializeWrapper<IDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                IReadOnlyDictionary<string, string> obj = await JsonSerializerWrapperForString.DeserializeWrapper<IReadOnlyDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                ImmutableDictionary<string, string> obj = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                IImmutableDictionary<string, string> obj = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                ImmutableSortedDictionary<string, string> obj = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableSortedDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.True(JsonString == json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.True(JsonString == json);
            }

            {
                Hashtable obj = await JsonSerializerWrapperForString.DeserializeWrapper<Hashtable>(JsonString);
                Assert.Equal("World", ((JsonElement)obj["Hello"]).GetString());
                Assert.Equal("World2", ((JsonElement)obj["Hello2"]).GetString());

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                SortedList obj = await JsonSerializerWrapperForString.DeserializeWrapper<SortedList>(JsonString);
                Assert.Equal("World", ((JsonElement)obj["Hello"]).GetString());
                Assert.Equal("World2", ((JsonElement)obj["Hello2"]).GetString());

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }
        }

        [Fact]
        public async Task ImplementsDictionary_DictionaryOfString()
        {
            const string JsonString = @"{""Hello"":""World"",""Hello2"":""World2""}";
            const string ReorderedJsonString = @"{""Hello2"":""World2"",""Hello"":""World""}";

            {
                WrapperForIDictionary obj = await JsonSerializerWrapperForString.DeserializeWrapper<WrapperForIDictionary>(JsonString);
                Assert.Equal("World", ((JsonElement)obj["Hello"]).GetString());
                Assert.Equal("World2", ((JsonElement)obj["Hello2"]).GetString());

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                StringToStringDictionaryWrapper obj = await JsonSerializerWrapperForString.DeserializeWrapper<StringToStringDictionaryWrapper>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                StringToStringSortedDictionaryWrapper obj = await JsonSerializerWrapperForString.DeserializeWrapper<StringToStringSortedDictionaryWrapper>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                GenericIDictionaryWrapper<string, string> obj = await JsonSerializerWrapperForString.DeserializeWrapper<GenericIDictionaryWrapper<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<GenericIReadOnlyDictionaryWrapper<string, string>>(JsonString));

                GenericIReadOnlyDictionaryWrapper<string, string> obj = new GenericIReadOnlyDictionaryWrapper<string, string>(new Dictionary<string, string>()
                {
                    { "Hello", "World" },
                    { "Hello2", "World2" },
                });
                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<StringToStringIImmutableDictionaryWrapper>(JsonString));

                StringToStringIImmutableDictionaryWrapper obj = new StringToStringIImmutableDictionaryWrapper(new Dictionary<string, string>()
                {
                    { "Hello", "World" },
                    { "Hello2", "World2" },
                });

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                HashtableWrapper obj = await JsonSerializerWrapperForString.DeserializeWrapper<HashtableWrapper>(JsonString);
                Assert.Equal("World", ((JsonElement)obj["Hello"]).GetString());
                Assert.Equal("World2", ((JsonElement)obj["Hello2"]).GetString());

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                SortedListWrapper obj = await JsonSerializerWrapperForString.DeserializeWrapper<SortedListWrapper>(JsonString);
                Assert.Equal("World", ((JsonElement)obj["Hello"]).GetString());
                Assert.Equal("World2", ((JsonElement)obj["Hello2"]).GetString());

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                GenericStructIDictionaryWrapper<string, string> obj = await JsonSerializerWrapperForString.DeserializeWrapper<GenericStructIDictionaryWrapper<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                GenericStructIDictionaryWrapper<string, string>? obj = await JsonSerializerWrapperForString.DeserializeWrapper<GenericStructIDictionaryWrapper<string, string>?>(JsonString);
                Assert.True(obj.HasValue);
                Assert.Equal("World", obj.Value["Hello"]);
                Assert.Equal("World2", obj.Value["Hello2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                GenericStructIDictionaryWrapper<string, string>? obj = await JsonSerializerWrapperForString.DeserializeWrapper<GenericStructIDictionaryWrapper<string, string>?>("null");
                Assert.False(obj.HasValue);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal("null", json);
            }

            {
                GenericStructIDictionaryWrapper<string, string> obj = default;
                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal("{}", json);
            }

            {
                StructWrapperForIDictionary obj = default;
                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal("{}", json);
            }
        }

        [Fact]
        public async Task DictionaryOfObject()
        {
            {
                Dictionary<string, object> obj = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, object>>(@"{""Key1"":1}");
                Assert.Equal(1, obj.Count);
                JsonElement element = (JsonElement)obj["Key1"];
                Assert.Equal(JsonValueKind.Number, element.ValueKind);
                Assert.Equal(1, element.GetInt32());

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(@"{""Key1"":1}", json);
            }

            {
                IDictionary<string, object> obj = await JsonSerializerWrapperForString.DeserializeWrapper<IDictionary<string, object>>(@"{""Key1"":1}");
                Assert.Equal(1, obj.Count);
                JsonElement element = (JsonElement)obj["Key1"];
                Assert.Equal(JsonValueKind.Number, element.ValueKind);
                Assert.Equal(1, element.GetInt32());

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(@"{""Key1"":1}", json);
            }
        }

        [Fact]
        public async Task ImplementsIDictionaryOfObject()
        {
            var input = new GenericIDictionaryWrapper<string, object>(new Dictionary<string, object>
            {
                { "Name", "David" },
                { "Age", 32 }
            });

            string json = await JsonSerializerWrapperForString.SerializeWrapper(input, typeof(IDictionary<string, object>));
            Assert.Equal(@"{""Name"":""David"",""Age"":32}", json);

            IDictionary<string, object> obj = await JsonSerializerWrapperForString.DeserializeWrapper<IDictionary<string, object>>(json);
            Assert.Equal(2, obj.Count);
            Assert.Equal("David", ((JsonElement)obj["Name"]).GetString());
            Assert.Equal(32, ((JsonElement)obj["Age"]).GetInt32());
        }

        [Fact]
        public async Task ImplementsIDictionaryOfString()
        {
            var input = new GenericIDictionaryWrapper<string, string>(new Dictionary<string, string>
            {
                { "Name", "David" },
                { "Job", "Software Architect" }
            });

            string json = await JsonSerializerWrapperForString.SerializeWrapper(input, typeof(IDictionary<string, string>));
            Assert.Equal(@"{""Name"":""David"",""Job"":""Software Architect""}", json);

            IDictionary<string, string> obj = await JsonSerializerWrapperForString.DeserializeWrapper<IDictionary<string, string>>(json);
            Assert.Equal(2, obj.Count);
            Assert.Equal("David", obj["Name"]);
            Assert.Equal("Software Architect", obj["Job"]);
        }

        [Fact]
        public async Task PocoWithDictionaryObject()
        {
            PocoDictionary dict = await JsonSerializerWrapperForString.DeserializeWrapper<PocoDictionary>("{\n\t\"key\" : {\"a\" : \"b\", \"c\" : \"d\"}}");
            Assert.Equal("b", dict.key["a"]);
            Assert.Equal("d", dict.key["c"]);
        }

        [Fact]
        public async Task DictionaryOfObject_NonPrimitiveTypes()
        {
            // https://github.com/dotnet/runtime/issues/29504
            Dictionary<string, object> dictionary = new Dictionary<string, object>
            {
                ["key"] = new Poco { Id = 10 },
            };

            string json = await JsonSerializerWrapperForString.SerializeWrapper(dictionary);
            Assert.Equal(@"{""key"":{""Id"":10}}", json);

            dictionary = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, object>>(json);
            Assert.Equal(1, dictionary.Count);
            JsonElement element = (JsonElement)dictionary["key"];
            Assert.Equal(@"{""Id"":10}", element.ToString());
        }

        [Fact]
        public async Task DictionaryOfList()
        {
            const string JsonString = @"{""Key1"":[1,2],""Key2"":[3,4]}";

            {
                IDictionary obj = await JsonSerializerWrapperForString.DeserializeWrapper<IDictionary>(JsonString);

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

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);
            }

            {
                IDictionary<string, List<int>> obj = await JsonSerializerWrapperForString.DeserializeWrapper<IDictionary<string, List<int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"][0]);
                Assert.Equal(2, obj["Key1"][1]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"][0]);
                Assert.Equal(4, obj["Key2"][1]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);
            }

            {
                ImmutableDictionary<string, List<int>> obj = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableDictionary<string, List<int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"][0]);
                Assert.Equal(2, obj["Key1"][1]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"][0]);
                Assert.Equal(4, obj["Key2"][1]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                const string ReorderedJsonString = @"{""Key2"":[3,4],""Key1"":[1,2]}";
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                IImmutableDictionary<string, List<int>> obj = await JsonSerializerWrapperForString.DeserializeWrapper<IImmutableDictionary<string, List<int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"][0]);
                Assert.Equal(2, obj["Key1"][1]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"][0]);
                Assert.Equal(4, obj["Key2"][1]);


                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                const string ReorderedJsonString = @"{""Key2"":[3,4],""Key1"":[1,2]}";
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }
        }

        [Fact]
        public async Task DictionaryOfArray()
        {
            const string JsonString = @"{""Key1"":[1,2],""Key2"":[3,4]}";
            Dictionary<string, int[]> obj = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, int[]>>(JsonString);

            Assert.Equal(2, obj.Count);
            Assert.Equal(2, obj["Key1"].Length);
            Assert.Equal(1, obj["Key1"][0]);
            Assert.Equal(2, obj["Key1"][1]);
            Assert.Equal(2, obj["Key2"].Length);
            Assert.Equal(3, obj["Key2"][0]);
            Assert.Equal(4, obj["Key2"][1]);

            string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            Assert.Equal(JsonString, json);
        }

        [Fact]
        public async Task ListOfDictionary()
        {
            const string JsonString = @"[{""Key1"":1,""Key2"":2},{""Key1"":3,""Key2"":4}]";

            {
                List<Dictionary<string, int>> obj = await JsonSerializerWrapperForString.DeserializeWrapper<List<Dictionary<string, int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj[0].Count);
                Assert.Equal(1, obj[0]["Key1"]);
                Assert.Equal(2, obj[0]["Key2"]);
                Assert.Equal(2, obj[1].Count);
                Assert.Equal(3, obj[1]["Key1"]);
                Assert.Equal(4, obj[1]["Key2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }
            {
                List<ImmutableSortedDictionary<string, int>> obj = await JsonSerializerWrapperForString.DeserializeWrapper<List<ImmutableSortedDictionary<string, int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj[0].Count);
                Assert.Equal(1, obj[0]["Key1"]);
                Assert.Equal(2, obj[0]["Key2"]);
                Assert.Equal(2, obj[1].Count);
                Assert.Equal(3, obj[1]["Key1"]);
                Assert.Equal(4, obj[1]["Key2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }
        }

        [Fact]
        public async Task ArrayOfDictionary()
        {
            const string JsonString = @"[{""Key1"":1,""Key2"":2},{""Key1"":3,""Key2"":4}]";

            {
                Dictionary<string, int>[] obj = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, int>[]>(JsonString);

                Assert.Equal(2, obj.Length);
                Assert.Equal(2, obj[0].Count);
                Assert.Equal(1, obj[0]["Key1"]);
                Assert.Equal(2, obj[0]["Key2"]);
                Assert.Equal(2, obj[1].Count);
                Assert.Equal(3, obj[1]["Key1"]);
                Assert.Equal(4, obj[1]["Key2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                ImmutableSortedDictionary<string, int>[] obj = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableSortedDictionary<string, int>[]>(JsonString);

                Assert.Equal(2, obj.Length);
                Assert.Equal(2, obj[0].Count);
                Assert.Equal(1, obj[0]["Key1"]);
                Assert.Equal(2, obj[0]["Key2"]);
                Assert.Equal(2, obj[1].Count);
                Assert.Equal(3, obj[1]["Key1"]);
                Assert.Equal(4, obj[1]["Key2"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }
        }

        [Fact]
        public async Task DictionaryOfDictionary()
        {
            const string JsonString = @"{""Key1"":{""Key1a"":1,""Key1b"":2},""Key2"":{""Key2a"":3,""Key2b"":4}}";

            {
                Dictionary<string, Dictionary<string, int>> obj = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, Dictionary<string, int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"]["Key1a"]);
                Assert.Equal(2, obj["Key1"]["Key1b"]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"]["Key2a"]);
                Assert.Equal(4, obj["Key2"]["Key2b"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                ImmutableSortedDictionary<string, ImmutableSortedDictionary<string, int>> obj = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableSortedDictionary<string, ImmutableSortedDictionary<string, int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"]["Key1a"]);
                Assert.Equal(2, obj["Key1"]["Key1b"]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"]["Key2a"]);
                Assert.Equal(4, obj["Key2"]["Key2b"]);

                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(JsonString, json);

                json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                Assert.Equal(JsonString, json);
            }
        }

        [Fact]
        public async Task DictionaryOfDictionaryOfDictionary()
        {
            const string JsonString = @"{""Key1"":{""Key1"":{""Key1"":1,""Key2"":2},""Key2"":{""Key1"":3,""Key2"":4}},""Key2"":{""Key1"":{""Key1"":5,""Key2"":6},""Key2"":{""Key1"":7,""Key2"":8}}}";
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> obj = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, Dictionary<string, Dictionary<string, int>>>>(JsonString);

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

            string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            Assert.Equal(JsonString, json);

            // Verify that typeof(object) doesn't interfere.
            json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
            Assert.Equal(JsonString, json);
        }

        [Fact]
        public async Task DictionaryOfArrayOfDictionary()
        {
            const string JsonString = @"{""Key1"":[{""Key1"":1,""Key2"":2},{""Key1"":3,""Key2"":4}],""Key2"":[{""Key1"":5,""Key2"":6},{""Key1"":7,""Key2"":8}]}";
            Dictionary<string, Dictionary<string, int>[]> obj = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, Dictionary<string, int>[]>>(JsonString);

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

            string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            Assert.Equal(JsonString, json);

            // Verify that typeof(object) doesn't interfere.
            json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
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

#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Too many dynamically generated serializable types to manually add to a serialization context.")]
#endif
        [Fact]
        public async Task NestedDictionariesRoundtrip()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.Converters.Add(new MyFactory());

            foreach ((Type dictionaryType, string testJson) in NestedDictionaryTypeData())
            {
                object dict = await JsonSerializerWrapperForString.DeserializeWrapper(testJson, dictionaryType, options);
                Assert.Equal(testJson, await JsonSerializerWrapperForString.SerializeWrapper(dict, options));
            }
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Needs full SimpleTestClass support.")]
#endif
        public async Task DictionaryOfClasses()
        {
            {
                IDictionary obj;

                {
                    string json = @"{""Key1"":" + SimpleTestClass.s_json + @",""Key2"":" + SimpleTestClass.s_json + "}";
                    obj = await JsonSerializerWrapperForString.DeserializeWrapper<IDictionary>(json);
                    Assert.Equal(2, obj.Count);

                    if (obj["Key1"] is JsonElement element)
                    {
                        SimpleTestClass result = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestClass>(element.GetRawText());
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
                    string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                    obj = await JsonSerializerWrapperForString.DeserializeWrapper<IDictionary>(json);
                    Assert.Equal(2, obj.Count);

                    if (obj["Key1"] is JsonElement element)
                    {
                        SimpleTestClass result = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestClass>(element.GetRawText());
                        result.Verify();
                    }
                    else
                    {
                        ((SimpleTestClass)obj["Key1"]).Verify();
                        ((SimpleTestClass)obj["Key2"]).Verify();
                    }
                }

                {
                    string json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                    obj = await JsonSerializerWrapperForString.DeserializeWrapper<IDictionary>(json);
                    Assert.Equal(2, obj.Count);

                    if (obj["Key1"] is JsonElement element)
                    {
                        SimpleTestClass result = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleTestClass>(element.GetRawText());
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
                    obj = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }

                {
                    // We can't compare against the json string above because property ordering is not deterministic (based on reflection order)
                    // so just round-trip the json and compare.
                    string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                    obj = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }

                {
                    string json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                    obj = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }
            }

            {
                ImmutableSortedDictionary<string, SimpleTestClass> obj;

                {
                    string json = @"{""Key1"":" + SimpleTestClass.s_json + @",""Key2"":" + SimpleTestClass.s_json + "}";
                    obj = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableSortedDictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }

                {
                    // We can't compare against the json string above because property ordering is not deterministic (based on reflection order)
                    // so just round-trip the json and compare.
                    string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                    obj = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableSortedDictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }

                {
                    string json = await JsonSerializerWrapperForString.SerializeWrapper<object>(obj);
                    obj = await JsonSerializerWrapperForString.DeserializeWrapper<ImmutableSortedDictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }
            }
        }

        [Fact]
        public async Task UnicodePropertyNames()
        {
            var options = new JsonSerializerOptions();
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            {
                Dictionary<string, int> obj;

                obj = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, int>>(@"{""A\u0467"":1}");
                Assert.Equal(1, obj["A\u0467"]);

                // Specifying encoder on options does not impact deserialize.
                obj = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, int>>(@"{""A\u0467"":1}", options);
                Assert.Equal(1, obj["A\u0467"]);

                string json;
                // Verify the name is escaped after serialize.
                json = await JsonSerializerWrapperForString.SerializeWrapper(obj);
                Assert.Equal(@"{""A\u0467"":1}", json);

                // Verify with encoder.
                json = await JsonSerializerWrapperForString.SerializeWrapper(obj, options);
                Assert.Equal("{\"A\u0467\":1}", json);
            }

            {
                // We want to go over StackallocByteThreshold=256 to force a pooled allocation, so this property is 200 chars and 400 bytes.
                const int charsInProperty = 200;

                string longPropertyName = new string('\u0467', charsInProperty);

                Dictionary<string, int> obj = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, int>>($"{{\"{longPropertyName}\":1}}");
                Assert.Equal(1, obj[longPropertyName]);

                // Verify the name is escaped after serialize.
                string json = await JsonSerializerWrapperForString.SerializeWrapper(obj);

                // Duplicate the unicode character 'charsInProperty' times.
                string longPropertyNameEscaped = new StringBuilder().Insert(0, @"\u0467", charsInProperty).ToString();

                string expectedJson = $"{{\"{longPropertyNameEscaped}\":1}}";
                Assert.Equal(expectedJson, json);

                // Verify the name is unescaped after deserialize.
                obj = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, int>>(json);
                Assert.Equal(1, obj[longPropertyName]);
            }
        }

        [Fact]
        public async Task CustomEscapingOnPropertyNameAndValue()
        {
            var dict = new Dictionary<string, string>();
            dict.Add("A\u046701", "Value\u0467");

            // Baseline with no escaping.
            var json = await JsonSerializerWrapperForString.SerializeWrapper(dict);
            Assert.Equal("{\"A\\u046701\":\"Value\\u0467\"}", json);

            // Enable escaping.
            var options = new JsonSerializerOptions();
            options.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;

            json = await JsonSerializerWrapperForString.SerializeWrapper(dict, options);
            Assert.Equal("{\"A\u046701\":\"Value\u0467\"}", json);
        }

        [Fact]
        public async Task ObjectToStringFail()
        {
            // Baseline
            string json = @"{""MyDictionary"":{""Key"":""Value""}}";
            JsonSerializer.Deserialize<Dictionary<string, object>>(json);

            await Assert.ThrowsAsync<JsonException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, string>>(json));
        }

        [Fact]
        public async Task ObjectToJsonElement()
        {
            string json = @"{""MyDictionary"":{""Key"":""Value""}}";
            Dictionary<string, JsonElement> result = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, JsonElement>>(json);
            JsonElement element = result["MyDictionary"];
            Assert.Equal(JsonValueKind.Object, element.ValueKind);
            Assert.Equal("Value", element.GetProperty("Key").GetString());
        }

        [Fact]
        public async Task Hashtable()
        {
            const string Json = @"{""Key"":""Value""}";

            IDictionary ht = new Hashtable();
            ht.Add("Key", "Value");
            string json = await JsonSerializerWrapperForString.SerializeWrapper(ht);

            Assert.Equal(Json, json);

            ht = await JsonSerializerWrapperForString.DeserializeWrapper<IDictionary>(json);
            Assert.IsType<JsonElement>(ht["Key"]);
            Assert.Equal("Value", ((JsonElement)ht["Key"]).GetString());

            // Verify round-tripped JSON.
            json = await JsonSerializerWrapperForString.SerializeWrapper(ht);
            Assert.Equal(Json, json);
        }

        [Fact]
        public async Task DeserializeDictionaryWithDuplicateKeys()
        {
            // Non-generic IDictionary case.
            IDictionary iDictionary = await JsonSerializerWrapperForString.DeserializeWrapper<IDictionary>(@"{""Hello"":""World"", ""Hello"":""NewValue""}");
            Assert.Equal("NewValue", iDictionary["Hello"].ToString());

            // Generic IDictionary case.
            IDictionary<string, string> iNonGenericDictionary = await JsonSerializerWrapperForString.DeserializeWrapper<IDictionary<string, string>>(@"{""Hello"":""World"", ""Hello"":""NewValue""}");
            Assert.Equal("NewValue", iNonGenericDictionary["Hello"]);

            IDictionary<string, object> iNonGenericObjectDictionary = await JsonSerializerWrapperForString.DeserializeWrapper<IDictionary<string, object>>(@"{""Hello"":""World"", ""Hello"":""NewValue""}");
            Assert.Equal("NewValue", iNonGenericObjectDictionary["Hello"].ToString());

            // Strongly-typed IDictionary<,> case.
            Dictionary<string, string> dictionary = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, string>>(@"{""Hello"":""World"", ""Hello"":""NewValue""}");
            Assert.Equal("NewValue", dictionary["Hello"]);

            dictionary = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, string>>(@"{""Hello"":""World"", ""myKey"" : ""myValue"", ""Hello"":""NewValue""}");
            Assert.Equal("NewValue", dictionary["Hello"]);

            // Weakly-typed IDictionary case.
            Dictionary<string, object> dictionaryObject = await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, object>>(@"{""Hello"":""World"", ""Hello"": null}");
            Assert.Null(dictionaryObject["Hello"]);
        }

        [Fact]
        public async Task DeserializeDictionaryWithDuplicateProperties()
        {
            PocoDuplicate foo = await JsonSerializerWrapperForString.DeserializeWrapper<PocoDuplicate>(@"{""BoolProperty"": false, ""BoolProperty"": true}");
            Assert.True(foo.BoolProperty);

            foo = await JsonSerializerWrapperForString.DeserializeWrapper<PocoDuplicate>(@"{""BoolProperty"": false, ""IntProperty"" : 1, ""BoolProperty"": true , ""IntProperty"" : 2}");
            Assert.True(foo.BoolProperty);
            Assert.Equal(2, foo.IntProperty);

            foo = await JsonSerializerWrapperForString.DeserializeWrapper<PocoDuplicate>(@"{""DictProperty"" : {""a"" : ""b"", ""c"" : ""d""},""DictProperty"" : {""b"" : ""b"", ""c"" : ""e""}}");
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
        public async Task ClassWithNoSetterAndDictionary()
        {
            // We don't attempt to deserialize into dictionaries without a setter.
            string json = @"{""MyDictionary"":{""Key1"":""Value1"", ""Key2"":""Value2""}}";
            ClassWithPopulatedDictionaryAndNoSetter obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithPopulatedDictionaryAndNoSetter>(json);
            Assert.Equal(1, obj.MyDictionary.Count);
        }

        [Fact]
        public async Task ClassWithNoSetterAndImmutableDictionary()
        {
            // We don't attempt to deserialize into dictionaries without a setter.
            string json = @"{""MyImmutableDictionary"":{""Key1"":""Value1"", ""Key2"":""Value2""}}";
            ClassWithPopulatedDictionaryAndNoSetter obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithPopulatedDictionaryAndNoSetter>(json);
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
        public async Task IgnoreDictionaryProperty(string json)
        {
            // Verify deserialization
            ClassWithIgnoredDictionary2 obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithIgnoredDictionary2>(json);
            Assert.Equal(1, obj.Parsed1.Count);
            Assert.Equal(1, obj.Parsed1["Key"]);
            Assert.Null(obj.Skipped2);
            Assert.Equal(1, obj.Parsed3.Count);
            Assert.Equal(2, obj.Parsed3["Key"]);

            // Round-trip and verify.
            string jsonRoundTripped = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            ClassWithIgnoredDictionary2 objRoundTripped = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithIgnoredDictionary2>(jsonRoundTripped);
            Assert.Equal(1, objRoundTripped.Parsed1.Count);
            Assert.Equal(1, objRoundTripped.Parsed1["Key"]);
            Assert.Null(objRoundTripped.Skipped2);
            Assert.Equal(1, objRoundTripped.Parsed3.Count);
            Assert.Equal(2, objRoundTripped.Parsed3["Key"]);
        }

        [Fact]
        public async Task IgnoreDictionaryPropertyWithDifferentOrdering()
        {
            // Verify all combinations of 3 properties with at least one ignore.
            await VerifyIgnore<ClassWithIgnoredDictionary1>(false, false, true);
            await VerifyIgnore<ClassWithIgnoredDictionary2>(false, true, false);
            await VerifyIgnore<ClassWithIgnoredDictionary3>(false, true, true);
            await VerifyIgnore<ClassWithIgnoredDictionary4>(true, false, false);
            await VerifyIgnore<ClassWithIgnoredDictionary5>(true, false, true);
            await VerifyIgnore<ClassWithIgnoredDictionary6>(true, true, false);
            await VerifyIgnore<ClassWithIgnoredDictionary7>(true, true, true);

            // Verify single case for IDictionary, [Ignore] and ImmutableDictionary.
            // Also specify addMissing to add additional skipped JSON that does not have a corresponding property.
            await VerifyIgnore<ClassWithIgnoredIDictionary>(false, true, false, addMissing: true);
            await VerifyIgnore<ClassWithIgnoreAttributeDictionary>(false, true, false, addMissing: true);
            await VerifyIgnore<ClassWithIgnoredImmutableDictionary>(false, true, false, addMissing: true);
        }

        private async Task VerifyIgnore<T>(bool skip1, bool skip2, bool skip3, bool addMissing = false)
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
            T obj = await JsonSerializerWrapperForString.DeserializeWrapper<T>(jsonString);
            Verify(obj);

            // Round-trip and verify.
            // Any skipped properties due to lack of a setter will now be "null" when serialized instead of "{}".
            string jsonStringRoundTripped = await JsonSerializerWrapperForString.SerializeWrapper(obj);
            T objRoundTripped = await JsonSerializerWrapperForString.DeserializeWrapper<T>(jsonStringRoundTripped);
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
        public async Task ClassWithPopulatedDictionary()
        {
            // We replace the contents.
            string json = @"{""MyDictionary"":{""Key1"":""Value1"", ""Key2"":""Value2""}}";
            ClassWithPopulatedDictionaryAndSetter obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithPopulatedDictionaryAndSetter>(json);
            Assert.Equal(2, obj.MyDictionary.Count);
        }

        [Fact]
        public async Task ClassWithPopulatedImmutableDictionary()
        {
            // We replace the contents.
            string json = @"{""MyImmutableDictionary"":{""Key1"":""Value1"", ""Key2"":""Value2""}}";
            ClassWithPopulatedDictionaryAndSetter obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithPopulatedDictionaryAndSetter>(json);
            Assert.Equal(2, obj.MyImmutableDictionary.Count);
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Multi-dim arrays not supported.")]
#endif
        public async Task DictionaryNotSupported()
        {
            string json = @"{""MyDictionary"":{""Key"":""Value""}}";

            NotSupportedException ex = await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithNotSupportedDictionary>(json));

            // The exception contains the type.
            Assert.Contains(typeof(Dictionary<int[,], int>).ToString(), ex.Message);
        }

        [Fact]
#if BUILDING_SOURCE_GENERATOR_TESTS
        [ActiveIssue("Multi-dim arrays not supported.")]
#endif
        public async Task DictionaryNotSupportedButIgnored()
        {
            string json = @"{""MyDictionary"":{""Key"":1}}";
            ClassWithNotSupportedDictionaryButIgnored obj = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithNotSupportedDictionaryButIgnored>(json);
            Assert.Null(obj.MyDictionary);
        }

        // https://github.com/dotnet/runtime/issues/29933
        [Fact]
        public async Task Serialize_IDictionaryOfPoco()
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

            var actual = await JsonSerializerWrapperForString.SerializeWrapper(value, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // Assert
            Assert.NotNull(actual);
            Assert.NotEmpty(actual);
        }

        // https://github.com/dotnet/runtime/issues/29933
        [Fact]
        public async Task Deserialize_IDictionaryOfPoco()
        {
            // Arrange
            string json = "{\"child\":{\"1\":{\"a\":\"1\",\"b\":\"\",\"c\":[],\"d\":[],\"e\":null,\"f\":[],\"g\":null,\"h\":null,\"i\":null,\"j\":null,\"k\":[]}}}";

            var actual = await JsonSerializerWrapperForString.DeserializeWrapper<AllSingleUpperPropertiesParent>(json, new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

            // Assert
            Assert.NotNull(actual);
            Assert.NotNull(actual.Child);
            Assert.Equal(1, actual.Child.Count);
            Assert.True(actual.Child.ContainsKey("1"));
            Assert.Equal("1", actual.Child["1"].A);
        }

        // https://github.com/dotnet/runtime/issues/29893
        [Fact]
        public async Task ShouldHandleNullInDictionaries_Serialize()
        {
            var value = new ClassWithDictionaryOfString_ChildWithDictionaryOfString()
            {
                Test = "value1",
                Child = new ClassWithDictionaryOfString()
            };

            var actual = await JsonSerializerWrapperForString.SerializeWrapper(value);
            Assert.Equal("{\"Test\":\"value1\",\"Dict\":null,\"Child\":{\"Test\":null,\"Dict\":null}}", actual);
        }

        // https://github.com/dotnet/runtime/issues/29893
        [Fact]
        public async Task ShouldHandleNullInDictionaries_Deserialize()
        {
            var json = "{\"Test\":\"value1\",\"Dict\":null,\"Child\":{\"Test\":null,\"Dict\":null}}";
            ClassWithDictionaryOfString_ChildWithDictionaryOfString actual = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithDictionaryOfString_ChildWithDictionaryOfString>(json);

            Assert.Equal("value1", actual.Test);
            Assert.Null(actual.Dict);
            Assert.NotNull(actual.Child);
            Assert.Null(actual.Child.Dict);
            Assert.Null(actual.Child.Test);
        }

        // https://github.com/dotnet/runtime/issues/29893
        [Fact]
        public async Task ShouldHandleNullInDictionaries_Serialize_IgnoreNullValues()
        {
            var value = new ClassWithDictionaryOfString_ChildWithDictionaryOfString()
            {
                Test = "value1",
                Child = new ClassWithDictionaryOfString()
            };

            var actual = await JsonSerializerWrapperForString.SerializeWrapper(value, new JsonSerializerOptions { IgnoreNullValues = true });
            Assert.Equal("{\"Test\":\"value1\",\"Child\":{}}", actual);
        }

        // https://github.com/dotnet/runtime/issues/29893
        [Fact]
        public async Task ShouldHandleNullInDictionaries_Deserialize_IgnoreNullValues()
        {
            var json = "{\"Test\":\"value1\",\"Child\":{}}";
            ClassWithDictionaryOfString_ChildWithDictionaryOfString actual = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithDictionaryOfString_ChildWithDictionaryOfString>(json);

            Assert.Equal("value1", actual.Test);
            Assert.Null(actual.Dict);
            Assert.NotNull(actual.Child);
            Assert.Null(actual.Child.Dict);
            Assert.Null(actual.Child.Test);
        }

        // https://github.com/dotnet/runtime/issues/29888
        [Fact]
        public async Task DictionaryWithNullShouldPreserveOrder_Serialize()
        {
            var dictionaryFirst = new ClassWithDictionaryAndProperty_DictionaryFirst()
            {
                Test = "value1"
            };

            var actual = await JsonSerializerWrapperForString.SerializeWrapper(dictionaryFirst);
            Assert.Equal("{\"Dict\":null,\"Test\":\"value1\"}", actual);

            var dictionaryLast = new ClassWithDictionaryAndProperty_DictionaryLast()
            {
                Test = "value1"
            };

            actual = await JsonSerializerWrapperForString.SerializeWrapper(dictionaryLast);
            Assert.Equal("{\"Test\":\"value1\",\"Dict\":null}", actual);
        }

        // https://github.com/dotnet/runtime/issues/29888
        [Fact]
        public async Task DictionaryWithNullShouldPreserveOrder_Deserialize()
        {
            var json = "{\"Dict\":null,\"Test\":\"value1\"}";
            ClassWithDictionaryAndProperty_DictionaryFirst dictionaryFirst = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithDictionaryAndProperty_DictionaryFirst>(json);

            Assert.Equal("value1", dictionaryFirst.Test);
            Assert.Null(dictionaryFirst.Dict);

            json = "{\"Test\":\"value1\",\"Dict\":null}";
            ClassWithDictionaryAndProperty_DictionaryLast dictionaryLast = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithDictionaryAndProperty_DictionaryLast>(json);

            Assert.Equal("value1", dictionaryLast.Test);
            Assert.Null(dictionaryLast.Dict);
        }

        // https://github.com/dotnet/runtime/issues/29888
        [Fact]
        public async Task DictionaryWithNullShouldPreserveOrder_Serialize_IgnoreNullValues()
        {
            var dictionaryFirst = new ClassWithDictionaryAndProperty_DictionaryFirst()
            {
                Test = "value1"
            };

            var actual = await JsonSerializerWrapperForString.SerializeWrapper(dictionaryFirst, new JsonSerializerOptions { IgnoreNullValues = true });
            Assert.Equal("{\"Test\":\"value1\"}", actual);

            var dictionaryLast = new ClassWithDictionaryAndProperty_DictionaryLast()
            {
                Test = "value1"
            };

            actual = await JsonSerializerWrapperForString.SerializeWrapper(dictionaryLast, new JsonSerializerOptions { IgnoreNullValues = true });
            Assert.Equal("{\"Test\":\"value1\"}", actual);
        }

        // https://github.com/dotnet/runtime/issues/29888
        [Fact]
        public async Task DictionaryWithNullShouldPreserveOrder_Deserialize_IgnoreNullValues()
        {
            var json = "{\"Test\":\"value1\"}";
            ClassWithDictionaryAndProperty_DictionaryFirst dictionaryFirst = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithDictionaryAndProperty_DictionaryFirst>(json);

            Assert.Equal("value1", dictionaryFirst.Test);
            Assert.Null(dictionaryFirst.Dict);

            json = "{\"Test\":\"value1\"}";
            ClassWithDictionaryAndProperty_DictionaryLast dictionaryLast = await JsonSerializerWrapperForString.DeserializeWrapper<ClassWithDictionaryAndProperty_DictionaryLast>(json);

            Assert.Equal("value1", dictionaryLast.Test);
            Assert.Null(dictionaryLast.Dict);
        }

        [Fact]
        public async Task NullDictionaryValuesShouldDeserializeAsNull()
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

            SimpleClassWithDictionaries obj = await JsonSerializerWrapperForString.DeserializeWrapper<SimpleClassWithDictionaries>(json);
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
        public async Task DictionaryOfTOnlyWithStringTValueAsInt()
        {
            const string Json = @"{""One"":1,""Two"":2}";

            DictionaryThatOnlyImplementsIDictionaryOfStringTValue<int> dictionary;

            dictionary = await JsonSerializerWrapperForString.DeserializeWrapper<DictionaryThatOnlyImplementsIDictionaryOfStringTValue<int>>(Json);
            Assert.Equal(1, dictionary["One"]);
            Assert.Equal(2, dictionary["Two"]);

            string json = await JsonSerializerWrapperForString.SerializeWrapper(dictionary);
            Assert.Equal(Json, json);
        }

        [Fact]
        public async Task DictionaryOfTOnlyWithStringTValueAsPoco()
        {
            const string Json = @"{""One"":{""Id"":1},""Two"":{""Id"":2}}";

            DictionaryThatOnlyImplementsIDictionaryOfStringTValue<Poco> dictionary;

            dictionary = await JsonSerializerWrapperForString.DeserializeWrapper<DictionaryThatOnlyImplementsIDictionaryOfStringTValue<Poco>>(Json);
            Assert.Equal(1, dictionary["One"].Id);
            Assert.Equal(2, dictionary["Two"].Id);

            string json = await JsonSerializerWrapperForString.SerializeWrapper(dictionary);
            Assert.Equal(Json, json);
        }

        public class DictionaryThatOnlyImplementsIDictionaryOfStringPoco : DictionaryThatOnlyImplementsIDictionaryOfStringTValue<Poco>
        {
        }

        [Fact]
        public async Task DictionaryOfTOnlyWithStringPoco()
        {
            const string Json = @"{""One"":{""Id"":1},""Two"":{""Id"":2}}";

            DictionaryThatOnlyImplementsIDictionaryOfStringPoco dictionary;

            dictionary = await JsonSerializerWrapperForString.DeserializeWrapper<DictionaryThatOnlyImplementsIDictionaryOfStringPoco>(Json);
            Assert.Equal(1, dictionary["One"].Id);
            Assert.Equal(2, dictionary["Two"].Id);

            string json = await JsonSerializerWrapperForString.SerializeWrapper(dictionary);
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
        public async Task VerifyDictionaryThatHasIncomatibleEnumeratorWithInt()
        {
            const string Json = @"{""One"":1,""Two"":2}";

            DictionaryThatHasIncompatibleEnumerator dictionary;
            dictionary = await JsonSerializerWrapperForString.DeserializeWrapper<DictionaryThatHasIncompatibleEnumerator>(Json);
            Assert.Equal(1, ((JsonElement)dictionary["One"]).GetInt32());
            Assert.Equal(2, ((JsonElement)dictionary["Two"]).GetInt32());
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(dictionary));
        }

        [Fact]
        public async Task VerifyDictionaryThatHasIncomatibleEnumeratorWithPoco()
        {
            const string Json = @"{""One"":{""Id"":1},""Two"":{""Id"":2}}";

            DictionaryThatHasIncompatibleEnumerator dictionary;
            dictionary = await JsonSerializerWrapperForString.DeserializeWrapper<DictionaryThatHasIncompatibleEnumerator>(Json);
            Assert.Equal(1, ((JsonElement)dictionary["One"]).GetProperty("Id").GetInt32());
            Assert.Equal(2, ((JsonElement)dictionary["Two"]).GetProperty("Id").GetInt32());
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.SerializeWrapper(dictionary));
        }

        public class ClassWithoutParameterlessCtor
        {
            public ClassWithoutParameterlessCtor(int num) { }
            public string Name { get; set; }
        }

        public class ClassWithInternalParameterlessConstructor
        {
            internal ClassWithInternalParameterlessConstructor() { }
            public string Name { get; set; }
        }

        public class ClassWithPrivateParameterlessConstructor
        {
            private ClassWithPrivateParameterlessConstructor() { }
            public string Name { get; set; }
        }

        [Fact]
        public async Task DictionaryWith_ObjectWithNoParameterlessCtor_AsValue_Throws()
        {
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, ClassWithInternalParameterlessConstructor>>(@"{""key"":{}}"));
            await Assert.ThrowsAsync<NotSupportedException>(async () => await JsonSerializerWrapperForString.DeserializeWrapper<Dictionary<string, ClassWithPrivateParameterlessConstructor>>(@"{""key"":{}}"));
        }

        [Fact]
        public async Task DictionaryWith_ObjectWithNoParameterlessCtor_Serialize_Works()
        {
            var noParameterless = new Dictionary<string, ClassWithoutParameterlessCtor>()
            {
                ["key"] = new ClassWithoutParameterlessCtor(5)
                {
                    Name = "parameterless"
                }
            };

            string json = await JsonSerializerWrapperForString.SerializeWrapper(noParameterless);
            Assert.Equal("{\"key\":{\"Name\":\"parameterless\"}}", json);

            var onlyInternal = new Dictionary<string, ClassWithInternalParameterlessConstructor>()
            {
                ["key"] = new ClassWithInternalParameterlessConstructor()
                {
                    Name = "internal"
                }
            };

            json = await JsonSerializerWrapperForString.SerializeWrapper(onlyInternal);
            Assert.Equal("{\"key\":{\"Name\":\"internal\"}}", json);

            var onlyPrivate = new Dictionary<string, ClassWithPrivateParameterlessConstructor>()
            {
                ["key"] = null
            };

            json = await JsonSerializerWrapperForString.SerializeWrapper(onlyPrivate);
            Assert.Equal("{\"key\":null}", json);
        }
    }
}

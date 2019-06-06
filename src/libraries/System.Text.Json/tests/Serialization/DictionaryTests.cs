// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
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
                Dictionary<string, string> obj = JsonSerializer.Parse<Dictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.ToString(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.ToString<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                IDictionary<string, string> obj = JsonSerializer.Parse<IDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.ToString(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.ToString<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                IReadOnlyDictionary<string, string> obj = JsonSerializer.Parse<IReadOnlyDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.ToString(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.ToString<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                ImmutableDictionary<string, string> obj = JsonSerializer.Parse<ImmutableDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.ToString(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);

                json = JsonSerializer.ToString<object>(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                IImmutableDictionary<string, string> obj = JsonSerializer.Parse<IImmutableDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.ToString(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);

                json = JsonSerializer.ToString<object>(obj);
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                ImmutableSortedDictionary<string, string> obj = JsonSerializer.Parse<ImmutableSortedDictionary<string, string>>(JsonString);
                Assert.Equal("World", obj["Hello"]);
                Assert.Equal("World2", obj["Hello2"]);

                string json = JsonSerializer.ToString(obj);
                Assert.True(JsonString == json);

                json = JsonSerializer.ToString<object>(obj);
                Assert.True(JsonString == json);
            }
        }

        [Fact]
        public static void DuplicateKeysFail()
        {
            // Strongly-typed IDictionary<,> case.
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<Dictionary<string, string>>(
                @"{""Hello"":""World"", ""Hello"":""World""}"));

            // Weakly-typed IDictionary case.
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<Dictionary<string, object>>(
                @"{""Hello"":null, ""Hello"":null}"));
        }

        [Fact]
        public static void DictionaryOfObject()
        {
            Dictionary<string, object> obj = JsonSerializer.Parse<Dictionary<string, object>>(@"{""Key1"":1}");
            Assert.Equal(1, obj.Count);
            JsonElement element = (JsonElement)obj["Key1"];
            Assert.Equal(JsonValueType.Number, element.Type);
            Assert.Equal(1, element.GetInt32());

            string json = JsonSerializer.ToString(obj);
            Assert.Equal(@"{""Key1"":1}", json);
        }

        [Theory]
        [InlineData(typeof(ImmutableDictionary<string, string>), "\"headers\"")]
        [InlineData(typeof(Dictionary<string, string>), "\"headers\"")]
        [InlineData(typeof(PocoDictionary), "\"headers\"")]
        public static void InvalidJsonForValueShouldFail(Type type, string json)
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse(json, type));
        }

        [Fact]
        public static void InvalidEmptyDictionaryInput()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<string>("{}"));
        }

        public static void PocoWithDictionaryObject()
        {
            PocoDictionary dict = JsonSerializer.Parse<PocoDictionary>("{\n\t\"key\" : {\"a\" : \"b\", \"c\" : \"d\"}}");
            Assert.Equal(dict.key["a"], "b");
            Assert.Equal(dict.key["c"], "d");
        }

        class PocoDictionary
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

            string json = JsonSerializer.ToString(dictionary);
            Assert.Equal(@"{""key"":{""Id"":10}}", json);

            dictionary = JsonSerializer.Parse<Dictionary<string, object>>(json);
            Assert.Equal(1, dictionary.Count);
            JsonElement element = (JsonElement)dictionary["key"];
            Assert.Equal(@"{""Id"":10}", element.ToString());
        }

        class Poco
        {
            public int Id { get; set; }
        }

        [Fact]
        public static void FirstGenericArgNotStringFail()
        {
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Parse<Dictionary<int, int>>(@"{1:1}"));
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Parse<ImmutableDictionary<int, int>>(@"{1:1}"));
        }

        [Fact]
        public static void DictionaryOfList()
        {
            const string JsonString = @"{""Key1"":[1,2],""Key2"":[3,4]}";

            {
                IDictionary<string, List<int>> obj = JsonSerializer.Parse<IDictionary<string, List<int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"][0]);
                Assert.Equal(2, obj["Key1"][1]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"][0]);
                Assert.Equal(4, obj["Key2"][1]);

                string json = JsonSerializer.ToString(obj);
                Assert.Equal(JsonString, json);
            }

            {
                ImmutableDictionary<string, List<int>> obj = JsonSerializer.Parse<ImmutableDictionary<string, List<int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"][0]);
                Assert.Equal(2, obj["Key1"][1]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"][0]);
                Assert.Equal(4, obj["Key2"][1]);

                string json = JsonSerializer.ToString(obj);
                const string ReorderedJsonString = @"{""Key2"":[3,4],""Key1"":[1,2]}";
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }

            {
                IImmutableDictionary<string, List<int>> obj = JsonSerializer.Parse<IImmutableDictionary<string, List<int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"][0]);
                Assert.Equal(2, obj["Key1"][1]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"][0]);
                Assert.Equal(4, obj["Key2"][1]);


                string json = JsonSerializer.ToString(obj);
                const string ReorderedJsonString = @"{""Key2"":[3,4],""Key1"":[1,2]}";
                Assert.True(JsonString == json || ReorderedJsonString == json);
            }
        }

        [Fact]
        public static void DictionaryOfArray()
        {
            const string JsonString = @"{""Key1"":[1,2],""Key2"":[3,4]}";
            Dictionary<string, int[]> obj = JsonSerializer.Parse<Dictionary<string, int[]>>(JsonString);

            Assert.Equal(2, obj.Count);
            Assert.Equal(2, obj["Key1"].Length);
            Assert.Equal(1, obj["Key1"][0]);
            Assert.Equal(2, obj["Key1"][1]);
            Assert.Equal(2, obj["Key2"].Length);
            Assert.Equal(3, obj["Key2"][0]);
            Assert.Equal(4, obj["Key2"][1]);

            string json = JsonSerializer.ToString(obj);
            Assert.Equal(JsonString, json);
        }

        [Fact]
        public static void ListOfDictionary()
        {
            const string JsonString = @"[{""Key1"":1,""Key2"":2},{""Key1"":3,""Key2"":4}]";

            {
                List<Dictionary<string, int>> obj = JsonSerializer.Parse<List<Dictionary<string, int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj[0].Count);
                Assert.Equal(1, obj[0]["Key1"]);
                Assert.Equal(2, obj[0]["Key2"]);
                Assert.Equal(2, obj[1].Count);
                Assert.Equal(3, obj[1]["Key1"]);
                Assert.Equal(4, obj[1]["Key2"]);

                string json = JsonSerializer.ToString(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.ToString<object>(obj);
                Assert.Equal(JsonString, json);
            }
            {
                List<ImmutableSortedDictionary<string, int>> obj = JsonSerializer.Parse<List<ImmutableSortedDictionary<string, int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj[0].Count);
                Assert.Equal(1, obj[0]["Key1"]);
                Assert.Equal(2, obj[0]["Key2"]);
                Assert.Equal(2, obj[1].Count);
                Assert.Equal(3, obj[1]["Key1"]);
                Assert.Equal(4, obj[1]["Key2"]);

                string json = JsonSerializer.ToString(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.ToString<object>(obj);
                Assert.Equal(JsonString, json);
            }
        }

        [Fact]
        public static void ArrayOfDictionary()
        {
            const string JsonString = @"[{""Key1"":1,""Key2"":2},{""Key1"":3,""Key2"":4}]";

            {
                Dictionary<string, int>[] obj = JsonSerializer.Parse<Dictionary<string, int>[]>(JsonString);

                Assert.Equal(2, obj.Length);
                Assert.Equal(2, obj[0].Count);
                Assert.Equal(1, obj[0]["Key1"]);
                Assert.Equal(2, obj[0]["Key2"]);
                Assert.Equal(2, obj[1].Count);
                Assert.Equal(3, obj[1]["Key1"]);
                Assert.Equal(4, obj[1]["Key2"]);

                string json = JsonSerializer.ToString(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.ToString<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                ImmutableSortedDictionary<string, int>[] obj = JsonSerializer.Parse<ImmutableSortedDictionary<string, int>[]>(JsonString);

                Assert.Equal(2, obj.Length);
                Assert.Equal(2, obj[0].Count);
                Assert.Equal(1, obj[0]["Key1"]);
                Assert.Equal(2, obj[0]["Key2"]);
                Assert.Equal(2, obj[1].Count);
                Assert.Equal(3, obj[1]["Key1"]);
                Assert.Equal(4, obj[1]["Key2"]);

                string json = JsonSerializer.ToString(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.ToString<object>(obj);
                Assert.Equal(JsonString, json);
            }
        }

        [Fact]
        public static void DictionaryOfDictionary()
        {
            const string JsonString = @"{""Key1"":{""Key1a"":1,""Key1b"":2},""Key2"":{""Key2a"":3,""Key2b"":4}}";

            {
                Dictionary<string, Dictionary<string, int>> obj = JsonSerializer.Parse<Dictionary<string, Dictionary<string, int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"]["Key1a"]);
                Assert.Equal(2, obj["Key1"]["Key1b"]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"]["Key2a"]);
                Assert.Equal(4, obj["Key2"]["Key2b"]);

                string json = JsonSerializer.ToString(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.ToString<object>(obj);
                Assert.Equal(JsonString, json);
            }

            {
                ImmutableSortedDictionary<string, ImmutableSortedDictionary<string, int>> obj = JsonSerializer.Parse<ImmutableSortedDictionary<string, ImmutableSortedDictionary<string, int>>>(JsonString);

                Assert.Equal(2, obj.Count);
                Assert.Equal(2, obj["Key1"].Count);
                Assert.Equal(1, obj["Key1"]["Key1a"]);
                Assert.Equal(2, obj["Key1"]["Key1b"]);
                Assert.Equal(2, obj["Key2"].Count);
                Assert.Equal(3, obj["Key2"]["Key2a"]);
                Assert.Equal(4, obj["Key2"]["Key2b"]);

                string json = JsonSerializer.ToString(obj);
                Assert.Equal(JsonString, json);

                json = JsonSerializer.ToString<object>(obj);
                Assert.Equal(JsonString, json);
            }
        }

        [Fact]
        public static void DictionaryOfDictionaryOfDictionary()
        {
            const string JsonString = @"{""Key1"":{""Key1"":{""Key1"":1,""Key2"":2},""Key2"":{""Key1"":3,""Key2"":4}},""Key2"":{""Key1"":{""Key1"":5,""Key2"":6},""Key2"":{""Key1"":7,""Key2"":8}}}";
            Dictionary<string, Dictionary<string, Dictionary<string, int>>> obj = JsonSerializer.Parse<Dictionary<string, Dictionary<string, Dictionary<string, int>>>>(JsonString);

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

            string json = JsonSerializer.ToString(obj);
            Assert.Equal(JsonString, json);

            // Verify that typeof(object) doesn't interfere.
            json = JsonSerializer.ToString<object>(obj);
            Assert.Equal(JsonString, json);
        }

        [Fact]
        public static void DictionaryOfArrayOfDictionary()
        {
            const string JsonString = @"{""Key1"":[{""Key1"":1,""Key2"":2},{""Key1"":3,""Key2"":4}],""Key2"":[{""Key1"":5,""Key2"":6},{""Key1"":7,""Key2"":8}]}";
            Dictionary<string, Dictionary<string, int>[]> obj = JsonSerializer.Parse<Dictionary<string, Dictionary<string, int>[]>>(JsonString);

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

            string json = JsonSerializer.ToString(obj);
            Assert.Equal(JsonString, json);

            // Verify that typeof(object) doesn't interfere.
            json = JsonSerializer.ToString<object>(obj);
            Assert.Equal(JsonString, json);
        }

        [Fact]
        public static void DictionaryOfClasses()
        {
            {
                Dictionary<string, SimpleTestClass> obj;

                {
                    string json = @"{""Key1"":" + SimpleTestClass.s_json + @",""Key2"":" + SimpleTestClass.s_json + "}";
                    obj = JsonSerializer.Parse<Dictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }

                {
                    // We can't compare against the json string above because property ordering is not deterministic (based on reflection order)
                    // so just round-trip the json and compare.
                    string json = JsonSerializer.ToString(obj);
                    obj = JsonSerializer.Parse<Dictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }

                {
                    string json = JsonSerializer.ToString<object>(obj);
                    obj = JsonSerializer.Parse<Dictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }
            }

            {
                ImmutableSortedDictionary<string, SimpleTestClass> obj;

                {
                    string json = @"{""Key1"":" + SimpleTestClass.s_json + @",""Key2"":" + SimpleTestClass.s_json + "}";
                    obj = JsonSerializer.Parse<ImmutableSortedDictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }

                {
                    // We can't compare against the json string above because property ordering is not deterministic (based on reflection order)
                    // so just round-trip the json and compare.
                    string json = JsonSerializer.ToString(obj);
                    obj = JsonSerializer.Parse<ImmutableSortedDictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }

                {
                    string json = JsonSerializer.ToString<object>(obj);
                    obj = JsonSerializer.Parse<ImmutableSortedDictionary<string, SimpleTestClass>>(json);
                    Assert.Equal(2, obj.Count);
                    obj["Key1"].Verify();
                    obj["Key2"].Verify();
                }
            }
        }

        [Fact]
        public static void CamelCaseOption()
        {
            var options = new JsonSerializerOptions();
            options.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;

            const string JsonString = @"[{""Key1"":1,""Key2"":2},{""Key1"":3,""Key2"":4}]";
            Dictionary<string, int>[] obj = JsonSerializer.Parse<Dictionary<string, int>[]>(JsonString, options);

            Assert.Equal(2, obj.Length);
            Assert.Equal(1, obj[0]["key1"]);
            Assert.Equal(2, obj[0]["key2"]);
            Assert.Equal(3, obj[1]["key1"]);
            Assert.Equal(4, obj[1]["key2"]);

            const string JsonCamel = @"[{""key1"":1,""key2"":2},{""key1"":3,""key2"":4}]";
            string jsonCamel = JsonSerializer.ToString<object>(obj);
            Assert.Equal(JsonCamel, jsonCamel);

            jsonCamel = JsonSerializer.ToString<object>(obj, options);
            Assert.Equal(JsonCamel, jsonCamel);
        }

        [Fact]
        public static void UnicodePropertyNames()
        {
            {
                Dictionary<string, int> obj = JsonSerializer.Parse<Dictionary<string, int>>(@"{""Aѧ"":1}");
                Assert.Equal(1, obj["Aѧ"]);

                // Verify the name is escaped after serialize.
                string json = JsonSerializer.ToString(obj);
                Assert.Equal(@"{""A\u0467"":1}", json);
            }

            {
                // We want to go over StackallocThreshold=256 to force a pooled allocation, so this property is 200 chars and 400 bytes.
                const int charsInProperty = 200;

                string longPropertyName = new string('ѧ', charsInProperty);

                Dictionary<string, int> obj = JsonSerializer.Parse<Dictionary<string, int>>($"{{\"{longPropertyName}\":1}}");
                Assert.Equal(1, obj[longPropertyName]);

                // Verify the name is escaped after serialize.
                string json = JsonSerializer.ToString(obj);

                // Duplicate the unicode character 'charsInProperty' times.
                string longPropertyNameEscaped = new StringBuilder().Insert(0, @"\u0467", charsInProperty).ToString();

                string expectedJson = $"{{\"{longPropertyNameEscaped}\":1}}";
                Assert.Equal(expectedJson, json);

                // Verify the name is unescaped after deserialize.
                obj = JsonSerializer.Parse<Dictionary<string, int>>(json);
                Assert.Equal(1, obj[longPropertyName]);
            }
        }

        [Fact]
        public static void ObjectToStringFail()
        {
            // Baseline
            string json = @"{""MyDictionary"":{""Key"":""Value""}}";
            JsonSerializer.Parse<Dictionary<string, object>>(json);

            Assert.Throws<JsonException>(() => JsonSerializer.Parse<Dictionary<string, string>>(json));
        }

        [Fact, ActiveIssue("JsonElement fails since it is a struct.")]
        public static void ObjectToJsonElement()
        {
            string json = @"{""MyDictionary"":{""Key"":""Value""}}";
            JsonSerializer.Parse<Dictionary<string, JsonElement>>(json);
        }

        [Fact]
        public static void HashtableFail()
        {
            {
                string json = @"{""Key"":""Value""}";

                // Verify we can deserialize into Dictionary<,>
                JsonSerializer.Parse<Dictionary<string, string>>(json);

                // We don't support non-generic IDictionary
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Parse<Hashtable>(json));
            }

            {
                Hashtable ht = new Hashtable();
                ht.Add("Key", "Value");

                Assert.Throws<NotSupportedException>(() => JsonSerializer.ToString(ht));
            }

            {
                string json = @"{""Key"":""Value""}";

                // We don't support non-generic IDictionary
                Assert.Throws<NotSupportedException>(() => JsonSerializer.Parse<IDictionary>(json));
            }

            {
                IDictionary ht = new Hashtable();
                ht.Add("Key", "Value");
                Assert.Throws<NotSupportedException>(() => JsonSerializer.ToString(ht));
            }
        }

        [Fact]
        public static void ClassWithNoSetter()
        {
            string json = @"{""MyDictionary"":{""Key"":""Value""}}";
            ClassWithDictionaryButNoSetter obj = JsonSerializer.Parse<ClassWithDictionaryButNoSetter>(json);
            Assert.Equal("Value", obj.MyDictionary["Key"]);
        }

        [Fact]
        public static void DictionaryNotSupported()
        {
            string json = @"{""MyDictionary"":{""Key"":""Value""}}";

            try
            {
                JsonSerializer.Parse<ClassWithNotSupportedDictionary>(json);
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
            ClassWithNotSupportedDictionaryButIgnored obj = JsonSerializer.Parse<ClassWithNotSupportedDictionaryButIgnored>(json);
            Assert.Null(obj.MyDictionary);
        }

        [Fact]
        public static void DeserializeUserDefinedDictionaryThrows()
        {
            string json = @"{""Hello"":1,""Hello2"":2}";
            Assert.Throws<NotSupportedException>(() => JsonSerializer.Parse<UserDefinedImmutableDictionary>(json));
        }

        public class ClassWithDictionaryButNoSetter
        {
            public Dictionary<string, string> MyDictionary { get; } = new Dictionary<string, string>();
        }

        public class ClassWithNotSupportedDictionary
        {
            public Dictionary<int, int> MyDictionary { get; set; }
        }

        public class ClassWithNotSupportedDictionaryButIgnored
        {
            [JsonIgnore] public Dictionary<int, int> MyDictionary { get; set; }
        }

        public class UserDefinedImmutableDictionary : IImmutableDictionary<string, int>
        {
            public int this[string key] => throw new NotImplementedException();

            public IEnumerable<string> Keys => throw new NotImplementedException();

            public IEnumerable<int> Values => throw new NotImplementedException();

            public int Count => throw new NotImplementedException();

            public IImmutableDictionary<string, int> Add(string key, int value)
            {
                throw new NotImplementedException();
            }

            public IImmutableDictionary<string, int> AddRange(IEnumerable<KeyValuePair<string, int>> pairs)
            {
                throw new NotImplementedException();
            }

            public IImmutableDictionary<string, int> Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(KeyValuePair<string, int> pair)
            {
                throw new NotImplementedException();
            }

            public bool ContainsKey(string key)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public IImmutableDictionary<string, int> Remove(string key)
            {
                throw new NotImplementedException();
            }

            public IImmutableDictionary<string, int> RemoveRange(IEnumerable<string> keys)
            {
                throw new NotImplementedException();
            }

            public IImmutableDictionary<string, int> SetItem(string key, int value)
            {
                throw new NotImplementedException();
            }

            public IImmutableDictionary<string, int> SetItems(IEnumerable<KeyValuePair<string, int>> items)
            {
                throw new NotImplementedException();
            }

            public bool TryGetKey(string equalKey, out string actualKey)
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, out int value)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }
}

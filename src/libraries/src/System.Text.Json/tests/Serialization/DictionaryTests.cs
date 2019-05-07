// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class DictionaryTests
    {
        [Fact]
        public static void DictionaryOfString()
        {
            const string JsonString = @"{""Hello"":""World"",""Hello2"":""World2""}";

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
        public static void DictionaryOfObjectFail()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<Dictionary<string, object>>(@"{""Key1"":1"));
        }

        [Fact]
        public static void FirstGenericArgNotStringFail()
        {
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<Dictionary<int, int>>(@"{""Key1"":1}"));
        }

        [Fact]
        public static void DictionaryOfList()
        {
            const string JsonString = @"{""Key1"":[1,2],""Key2"":[3,4]}";

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

        [Fact]
        public static void ArrayOfDictionary()
        {
            const string JsonString = @"[{""Key1"":1,""Key2"":2},{""Key1"":3,""Key2"":4}]";
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

        [Fact]
        public static void DictionaryOfDictionary()
        {
            const string JsonString = @"{""Key1"":{""Key1a"":1,""Key1b"":2},""Key2"":{""Key2a"":3,""Key2b"":4}}";
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

        [Fact]
        public static void DictionaryOfClasses()
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
            string json = @"{""MyDictionary"":{""Key"":""Value""}}";
            Assert.Throws<JsonException>(() => JsonSerializer.Parse<Dictionary<string, string>>(json));
        }

        [Fact]
        public static void HashtableFail()
        {
            {
                string json = @"{""Key"":""Value""}";

                // Verify we can deserialize into Dictionary<,>
                JsonSerializer.Parse<Dictionary<string, string>>(json);

                // We don't support non-generic IDictionary
                Assert.Throws<JsonException>(() => JsonSerializer.Parse<Hashtable>(json));
            }

            {
                Hashtable ht = new Hashtable();
                ht.Add("Key", "Value");
                Assert.Throws<JsonException>(() => JsonSerializer.ToString(ht));
            }

            {
                string json = @"{""Key"":""Value""}";

                // We don't support non-generic IDictionary
                Assert.Throws<JsonException>(() => JsonSerializer.Parse<IDictionary>(json));
            }

            {
                IDictionary ht = new Hashtable();
                ht.Add("Key", "Value");
                Assert.Throws<JsonException>(() => JsonSerializer.ToString(ht));
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization.Tests;
using Xunit;

namespace System.Text.Json.Nodes.Tests
{
    public static class JsonObjectTests
    {
        [Fact]
        public static void KeyValuePair()
        {
            var jObject = new JsonObject();
            jObject["One"] = 1;
            jObject["Two"] = 2;
            Test();

            jObject = new JsonObject { { "One", 1 }, { "Two", 2 } };
            Test();

            void Test()
            {
                KeyValuePair<string, JsonNode?> kvp1 = default;
                KeyValuePair<string, JsonNode?> kvp2 = default;

                int count = 0;
                foreach (KeyValuePair<string, JsonNode?> kvp in jObject)
                {
                    if (count == 0)
                    {
                        kvp1 = kvp;
                    }
                    else
                    {
                        kvp2 = kvp;
                    }

                    count++;
                }

                Assert.Equal(2, count);

                ICollection<KeyValuePair<string, JsonNode?>> iCollection = jObject;
                Assert.True(iCollection.Contains(kvp1));
                Assert.True(iCollection.Contains(kvp2));
                Assert.False(iCollection.Contains(new KeyValuePair<string, JsonNode?>("?", null)));

                Assert.True(iCollection.Remove(kvp1));
                Assert.Equal(1, jObject.Count);

                Assert.False(iCollection.Remove(new KeyValuePair<string, JsonNode?>("?", null)));
                Assert.Equal(1, jObject.Count);
            }
        }

        [Fact]
        public static void IsReadOnly()
        {
            var jsonObject = new JsonObject();

            ICollection<KeyValuePair<string, JsonNode?>> iCollectionOfKVP = jsonObject;
            Assert.False(iCollectionOfKVP.IsReadOnly);

            IDictionary<string, JsonNode?> iDictionary = jsonObject;
            Assert.True(iDictionary.Keys.IsReadOnly);
            Assert.True(iDictionary.Values.IsReadOnly);
        }

        [Fact]
        public static void KeyAndValueCollections_ThrowsNotSupportedException()
        {
            IDictionary<string, JsonNode?> jsonObject = new JsonObject();

            Assert.Throws<NotSupportedException>(() => jsonObject.Keys.Add("Hello"));
            Assert.Throws<NotSupportedException>(() => jsonObject.Values.Add("Hello"));

            Assert.Throws<NotSupportedException>(() => jsonObject.Keys.Clear());
            Assert.Throws<NotSupportedException>(() => jsonObject.Values.Clear());

            Assert.Throws<NotSupportedException>(() => jsonObject.Keys.Remove("Hello"));
            Assert.Throws<NotSupportedException>(() => jsonObject.Values.Remove("Hello"));
        }

        [Fact]
        public static void NullPropertyValues()
        {
            var jObject = new JsonObject();
            jObject["One"] = null;
            jObject.Add("Two", null);
            Assert.Equal(2, jObject.Count);
            Assert.Null(jObject["One"]);
            Assert.Null(jObject["Two"]);
        }

        [Fact]
        public static void NullPropertyNameFail()
        {
            ArgumentNullException ex;

            var jObject = new JsonObject();
            ex = Assert.Throws<ArgumentNullException>(() => jObject.Add(null, 42));
            Assert.Contains("propertyName", ex.ToString());

            ex = Assert.Throws<ArgumentNullException>(() => jObject[null] = 42);
            Assert.Contains("propertyName", ex.ToString());

            ex = Assert.Throws<ArgumentNullException>(() => jObject.ContainsKey(null));
            Assert.Contains("propertyName", ex.ToString());

            ex = Assert.Throws<ArgumentNullException>(() => jObject.Remove(null));
            Assert.Contains("propertyName", ex.ToString());

            var iDictionary = (IDictionary<string, JsonNode?>)jObject;
            ex = Assert.Throws<ArgumentNullException>(() => iDictionary.TryGetValue(null, out JsonNode _));
            Assert.Contains("propertyName", ex.ToString());
        }

        [Fact]
        public static void IEnumerable()
        {
            var jObject = new JsonObject();
            jObject["One"] = 1;
            jObject["Two"] = 2;

            IEnumerable enumerable = jObject;
            int count = 0;
            foreach (KeyValuePair<string, JsonNode?> kvp in enumerable)
            {
                count++;
            }

            Assert.Equal(2, count);
        }

        [Fact]
        public static void MissingProperty()
        {
            var options = new JsonSerializerOptions();
            JsonObject jObject = JsonSerializer.Deserialize<JsonObject>("{}", options);
            Assert.Null(jObject["NonExistingProperty"]);
            Assert.False(jObject.Remove("NonExistingProperty"));
        }

        [Fact]
        public static void IDictionary_KeyValuePair()
        {
            IDictionary<string, JsonNode?> jObject = new JsonObject();
            jObject.Add(new KeyValuePair<string, JsonNode?>("MyProperty", 42));
            Assert.Equal(42, jObject["MyProperty"].GetValue<int>());

            Assert.Equal(1, jObject.Keys.Count);
            Assert.Equal(1, jObject.Values.Count);
        }

        [Fact]
        public static void Clear_ContainsKey()
        {
            var jObject = new JsonObject();
            jObject.Add("One", 1);
            jObject.Add("Two", 2);
            Assert.Equal(2, jObject.Count);

            Assert.True(jObject.ContainsKey("One"));
            Assert.True(jObject.ContainsKey("Two"));
            Assert.False(jObject.ContainsKey("?"));

            jObject.Clear();
            Assert.False(jObject.ContainsKey("One"));
            Assert.False(jObject.ContainsKey("Two"));
            Assert.False(jObject.ContainsKey("?"));
            Assert.Equal(0, jObject.Count);

            jObject.Add("One", 1);
            jObject.Add("Two", 2);
            Assert.Equal(2, jObject.Count);
        }

        [Fact]
        public static void Clear_JsonElement()
        {
            const string Json = "{\"MyProperty\":42}";
            JsonObject obj;

            // Baseline
            obj = JsonSerializer.Deserialize<JsonObject>(Json);
            Assert.Equal(1, obj.Count);
            obj.Clear();
            Assert.Equal(0, obj.Count);

            // Call clear with only JsonElement (not yet expanded to JsonNodes).
            obj = JsonSerializer.Deserialize<JsonObject>(Json);
            obj.Clear();
            // Don't check for Count of 1 since that will create nodes.
            Assert.Equal(0, obj.Count);
        }

        [Fact]
        public static void CaseSensitivity_ReadMode()
        {
            var options = new JsonSerializerOptions();
            JsonObject obj = JsonSerializer.Deserialize<JsonObject>("{\"MyProperty\":42}", options);

            Assert.Equal(42, obj["MyProperty"].GetValue<int>());
            Assert.Null(obj["myproperty"]);
            Assert.Null(obj["MYPROPERTY"]);

            options = new JsonSerializerOptions();
            options.PropertyNameCaseInsensitive = true;
            obj = JsonSerializer.Deserialize<JsonObject>("{\"MyProperty\":42}", options);

            Assert.Equal(42, obj["MyProperty"].GetValue<int>());
            Assert.Equal(42, obj["myproperty"].GetValue<int>());
            Assert.Equal(42, obj["MYPROPERTY"].GetValue<int>());
        }

        [Fact]
        public static void CaseInsensitive_Remove()
        {
            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = true };
            JsonObject obj = JsonSerializer.Deserialize<JsonObject>("{\"MyProperty\":42}", options);

            Assert.True(obj.ContainsKey("MyProperty"));
            Assert.True(obj.ContainsKey("myproperty"));
            Assert.True(obj.ContainsKey("MYPROPERTY"));

            Assert.True(obj.Remove("myproperty"));
            Assert.False(obj.Remove("myproperty"));
            Assert.False(obj.Remove("MYPROPERTY"));
            Assert.False(obj.Remove("MyProperty"));
        }

        [Fact]
        public static void CaseSensitive_Remove()
        {
            var options = new JsonSerializerOptions() { PropertyNameCaseInsensitive = false };
            JsonObject obj = JsonSerializer.Deserialize<JsonObject>("{\"MYPROPERTY\":42,\"myproperty\":43}", options);

            Assert.False(obj.ContainsKey("MyProperty"));
            Assert.True(obj.ContainsKey("MYPROPERTY"));
            Assert.True(obj.ContainsKey("myproperty"));

            Assert.False(obj.Remove("MyProperty"));

            Assert.True(obj.Remove("MYPROPERTY"));
            Assert.False(obj.Remove("MYPROPERTY"));

            Assert.True(obj.Remove("myproperty"));
            Assert.False(obj.Remove("myproperty"));
        }

        [Fact]
        public static void CaseSensitivity_EditMode()
        {
            var jArray = new JsonArray();
            var jObject = new JsonObject();
            jObject.Add("MyProperty", 42);
            jObject.Add("myproperty", 42); // No exception

            // Options on direct node.
            var options = new JsonNodeOptions { PropertyNameCaseInsensitive = true };
            jArray = new JsonArray();
            jObject = new JsonObject(options);
            jObject.Add("MyProperty", 42);
            jArray.Add(jObject);
            Assert.Throws<ArgumentException>(() => jObject.Add("myproperty", 42));

            // Options on parent node (deferred creation of options until Add).
            jArray = new JsonArray(options);
            jObject = new JsonObject();
            jArray.Add(jObject);
            jObject.Add("MyProperty", 42);
            Assert.Throws<ArgumentException>(() => jObject.Add("myproperty", 42));

            // Dictionary is created when Add is called for the first time, so we need to be added first.
            jArray = new JsonArray(options);
            jObject = new JsonObject();
            jObject.Add("MyProperty", 42);
            jArray.Add(jObject);
            jObject.Add("myproperty", 42); // No exception since options were not set in time.
        }

        [Fact]
        public static void NamingPoliciesAreNotUsed()
        {
            const string Json = "{\"myProperty\":42}";

            var options = new JsonSerializerOptions();
            options.PropertyNamingPolicy = new SimpleSnakeCasePolicy();

            JsonObject obj = JsonSerializer.Deserialize<JsonObject>(Json, options);
            string json = obj.ToJsonString();
            JsonTestHelper.AssertJsonEqual(Json, json);
        }

        [Fact]
        public static void FromElement()
        {
            using (JsonDocument document = JsonDocument.Parse("{\"myProperty\":42}"))
            {
                JsonObject jObject = JsonObject.Create(document.RootElement);
                Assert.Equal(42, jObject["myProperty"].GetValue<int>());
            }

            using (JsonDocument document = JsonDocument.Parse("null"))
            {
                JsonObject? jObject = JsonObject.Create(document.RootElement);
                Assert.Null(jObject);
            }
        }

        [Theory]
        [InlineData("42")]
        [InlineData("[]")]
        public static void FromElement_WrongNodeTypeThrows(string json)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            {
                Assert.Throws<InvalidOperationException>(() => JsonObject.Create(document.RootElement));
            }
        }

        [Fact]
        public static void WriteTo_Validation()
        {
            Assert.Throws<ArgumentNullException>(() => new JsonObject().WriteTo(null));
        }

        [Fact]
        public static void WriteTo()
        {
            const string Json = "{\"MyProperty\":42}";

            JsonObject jObject = JsonNode.Parse(Json).AsObject();
            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream);
            jObject.WriteTo(writer);
            writer.Flush();

            string json = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(Json, json);
        }

        [Fact]
        public static void CopyTo()
        {
            JsonNode node1 = 1;
            JsonNode node2 = 2;

            IDictionary<string, JsonNode?> jObject = new JsonObject();
            jObject["One"] = node1;
            jObject["Two"] = node2;
            jObject["null"] = null;

            var arr = new KeyValuePair<string, JsonNode?>[4];
            jObject.CopyTo(arr, 0);

            Assert.Same(node1, arr[0].Value);
            Assert.Same(node2, arr[1].Value);
            Assert.Null(arr[2].Value);

            arr = new KeyValuePair<string, JsonNode?>[5];
            jObject.CopyTo(arr, 1);
            Assert.Null(arr[0].Key);
            Assert.Null(arr[0].Value);
            Assert.NotNull(arr[1].Key);
            Assert.Same(node1, arr[1].Value);
            Assert.NotNull(arr[2].Key);
            Assert.Same(node2, arr[2].Value);
            Assert.NotNull(arr[3].Key);
            Assert.Null(arr[3].Value);
            Assert.Null(arr[4].Key);
            Assert.Null(arr[4].Value);

            arr = new KeyValuePair<string, JsonNode?>[3];
            Assert.Throws<ArgumentException>(() => jObject.CopyTo(arr, 1));

            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.CopyTo(arr, -1));
        }

        [Fact]
        public static void CopyTo_KeyAndValueCollections()
        {
            IDictionary<string, JsonNode?> jObject = new JsonObject();
            jObject["One"] = 1;
            jObject["Two"] = 2;

            string[] stringArray = new string[3];
            jObject.Keys.CopyTo(stringArray, 1);
            Assert.Null(stringArray[0]);
            Assert.Equal("One", stringArray[1]);
            Assert.Equal("Two", stringArray[2]);

            JsonNode[] nodeArray = new JsonNode[3];
            jObject.Values.CopyTo(nodeArray, 1);
            Assert.Null(nodeArray[0]);
            Assert.Equal(1, nodeArray[1].GetValue<int>());
            Assert.Equal(2, nodeArray[2].GetValue<int>());

            Assert.Throws<ArgumentException>(() => jObject.Keys.CopyTo(stringArray, 2));
            Assert.Throws<ArgumentException>(() => jObject.Values.CopyTo(nodeArray, 2));

            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.Keys.CopyTo(stringArray, -1));
            Assert.Throws<ArgumentOutOfRangeException>(() => jObject.Values.CopyTo(nodeArray, -1));
        }

        [Fact]
        public static void Contains_KeyAndValueCollections()
        {
            IDictionary<string, JsonNode?> jObject = new JsonObject();
            jObject["One"] = 1;

            Assert.True(jObject.Keys.Contains("One"));
            Assert.False(jObject.Keys.Contains("Two"));

            Assert.True(jObject.Values.Contains(jObject["One"]));
            Assert.False(jObject.Values.Contains(1)); // Reference semantics causes this to be false.
        }

        [Fact]
        public static void IEnumerable_KeyAndValueCollections()
        {
            IDictionary<string, JsonNode?> jObject = new JsonObject();
            jObject["One"] = 1;

            IEnumerable enumerable = jObject.Keys;
            int count = 0;
            foreach (string str in enumerable)
            {
                count++;
            }
            Assert.Equal(1, count);

            enumerable = jObject.Values;
            count = 0;
            foreach (JsonNode node in enumerable)
            {
                count++;
            }
            Assert.Equal(1, count);
        }

        [Fact]
        public static void CreateDom()
        {
            var jObj = new JsonObject
            {
                // Primitives
                ["MyString"] = JsonValue.Create("Hello!"),
                ["MyNull"] = null,
                ["MyBoolean"] = JsonValue.Create(false),

                // Nested array
                ["MyArray"] = new JsonArray
                (
                    JsonValue.Create(2),
                    JsonValue.Create(3),
                    JsonValue.Create(42)
                ),

                // Additional primitives
                ["MyInt"] = JsonValue.Create(43),
                ["MyDateTime"] = JsonValue.Create(new DateTime(2020, 7, 8)),
                ["MyGuid"] = JsonValue.Create(new Guid("ed957609-cdfe-412f-88c1-02daca1b4f51")),

                // Nested objects
                ["MyObject"] = new JsonObject
                {
                    ["MyString"] = JsonValue.Create("Hello!!")
                },

                ["Child"] = new JsonObject
                {
                    ["ChildProp"] = JsonValue.Create(1)
                }
            };

            string json = jObj.ToJsonString();
            JsonTestHelper.AssertJsonEqual(JsonNodeTests.ExpectedDomJson, json);
        }

        [Fact]
        public static void CreateDom_ImplicitOperators()
        {
            var jObj = new JsonObject
            {
                // Primitives
                ["MyString"] = "Hello!",
                ["MyNull"] = null,
                ["MyBoolean"] = false,

                // Nested array
                ["MyArray"] = new JsonArray(2, 3, 42),

                // Additional primitives
                ["MyInt"] = 43,
                ["MyDateTime"] = new DateTime(2020, 7, 8),
                ["MyGuid"] = new Guid("ed957609-cdfe-412f-88c1-02daca1b4f51"),

                // Nested objects
                ["MyObject"] = new JsonObject
                {
                    ["MyString"] = "Hello!!"
                },

                ["Child"] = new JsonObject()
                {
                    ["ChildProp"] = 1
                }
            };

            string json = jObj.ToJsonString();
            JsonTestHelper.AssertJsonEqual(JsonNodeTests.ExpectedDomJson, json);
        }

        [Fact]
        public static void EditDom()
        {
            const string Json =
                "{\"MyString\":\"Hello\",\"MyNull\":null,\"MyBoolean\":true,\"MyArray\":[1,2],\"MyInt\":42,\"MyDateTime\":\"2020-07-08T00:00:00\",\"MyGuid\":\"ed957609-cdfe-412f-88c1-02daca1b4f51\",\"MyObject\":{\"MyString\":\"World\"}}";

            JsonNode obj = JsonSerializer.Deserialize<JsonObject>(Json);
            Verify();

            // Verify the values are round-trippable.
            ((JsonArray)obj["MyArray"]).RemoveAt(2);
            Verify();

            void Verify()
            {
                // Change some primitives.
                obj["MyString"] = JsonValue.Create("Hello!");
                obj["MyBoolean"] = JsonValue.Create(false);
                obj["MyInt"] = JsonValue.Create(43);

                // Add nested objects.
                obj["MyObject"] = new JsonObject();
                obj["MyObject"]["MyString"] = JsonValue.Create("Hello!!");

                obj["Child"] = new JsonObject();
                obj["Child"]["ChildProp"] = JsonValue.Create(1);

                // Modify number elements.
                obj["MyArray"][0] = JsonValue.Create(2);
                obj["MyArray"][1] = JsonValue.Create(3);

                // Add an element.
                ((JsonArray)obj["MyArray"]).Add(JsonValue.Create(42));

                string json = obj.ToJsonString();
                JsonTestHelper.AssertJsonEqual(JsonNodeTests.ExpectedDomJson, json);
            }
        }

        [Fact]
        public static void ReAddSameNode_Throws()
        {
            var jValue = JsonValue.Create(1);

            var jObject = new JsonObject();
            jObject.Add("Prop", jValue);
            ArgumentException ex = Assert.Throws<ArgumentException>(() => jObject.Add("Prop", jValue));
            Assert.Contains("Prop", ex.ToString());
        }

        [Fact]
        public static void ReAddRemovedNode()
        {
            var jValue = JsonValue.Create(1);

            var jObject = new JsonObject();
            jObject.Add("Prop", jValue);
            Assert.Equal(jObject, jValue.Parent);

            // Replace with a new node.
            jObject["Prop"] = 42;

            Assert.Null(jValue.Parent);
            Assert.Equal(1, jObject.Count);
            jObject.Remove("Prop");
            Assert.Equal(0, jObject.Count);
            jObject.Add("Prop", jValue);
            Assert.Equal(1, jObject.Count);
        }

        [Fact]
        public static void DynamicObject_LINQ_Query()
        {
            JsonArray allOrders = JsonSerializer.Deserialize<JsonArray>(JsonNodeTests.Linq_Query_Json);
            IEnumerable<JsonNode> orders = allOrders.Where(o => o["Customer"]["City"].GetValue<string>() == "Fargo");

            Assert.Equal(2, orders.Count());
            Assert.Equal(100, orders.ElementAt(0)["OrderId"].GetValue<int>());
            Assert.Equal(300, orders.ElementAt(1)["OrderId"].GetValue<int>());
            Assert.Equal("Customer1", orders.ElementAt(0)["Customer"]["Name"].GetValue<string>());
            Assert.Equal("Customer3", orders.ElementAt(1)["Customer"]["Name"].GetValue<string>());
        }

        private class BlogPost
        {
            public string Title { get; set; }
            public string AuthorName { get; set; }
            public string AuthorTwitter { get; set; }
            public string Body { get; set; }
            public DateTime PostedDate { get; set; }
        }

        [Fact]
        public static void DynamicObject_LINQ_Convert()
        {
            string json = @"
            [
              {
                ""Title"": ""TITLE."",
                ""Author"":
                {
                  ""Name"": ""NAME."",
                  ""Mail"": ""MAIL."",
                  ""Picture"": ""/PICTURE.png""
                },
                ""Date"": ""2021-01-20T19:30:00"",
                ""BodyHtml"": ""Content.""
              }
            ]";

            JsonArray arr = JsonSerializer.Deserialize<JsonArray>(json);

            // Convert nested JSON to a flat POCO.
            IList<BlogPost> blogPosts = arr.Select(p => new BlogPost
            {
                Title = p["Title"].GetValue<string>(),
                AuthorName = p["Author"]["Name"].GetValue<string>(),
                AuthorTwitter = p["Author"]["Mail"].GetValue<string>(),
                PostedDate = p["Date"].GetValue<DateTime>(),
                Body = p["BodyHtml"].GetValue<string>()
            }).ToList();

            const string expected = "[{\"Title\":\"TITLE.\",\"AuthorName\":\"NAME.\",\"AuthorTwitter\":\"MAIL.\",\"Body\":\"Content.\",\"PostedDate\":\"2021-01-20T19:30:00\"}]";

            string json_out = JsonSerializer.Serialize(blogPosts);
            Assert.Equal(expected, json_out);
        }

        [Theory]
        [MemberData(nameof(JObjectCollectionData))]

        public static void ListToDictionaryConversions(JsonObject jObject, int count)
        {
            Assert.Equal(count, jObject.Count);

            int i;

            for (i = 0; i < count; i++)
            {
                Assert.Equal(i, jObject[i.ToString()].GetValue<int>());
            }

            i = 0;
            foreach (KeyValuePair<string, JsonNode?> kvp in jObject)
            {
                Assert.Equal(i.ToString(), kvp.Key);
                Assert.Equal(i, kvp.Value.GetValue<int>());
                i++;
            }

            i = 0;
            foreach (object o in (IEnumerable)jObject)
            {
                var kvp = (KeyValuePair<string, JsonNode?>)o;
                Assert.Equal(i.ToString(), kvp.Key);
                Assert.Equal(i, kvp.Value.GetValue<int>());
                i++;
            }

            var dictionary = (IDictionary<string, JsonNode?>)jObject;

            i = 0;
            foreach (string propertyName in dictionary.Keys)
            {
                Assert.Equal(i.ToString(), propertyName);
                i++;
            }

            i = 0;
            foreach (JsonNode? node in dictionary.Values)
            {
                Assert.Equal(i, node.GetValue<int>());
                i++;
            }

            string expectedJson = jObject.ToJsonString();

            // Use indexers to replace items.
            for (i = 0; i < count; i++)
            {
                string key = i.ToString();

                // Contains does a reference comparison on JsonNode so it needs to be done before modifying.
                Assert.True(jObject.Contains(new KeyValuePair<string, JsonNode?>(key, jObject[key])));

                jObject[key] = JsonValue.Create(i);
                jObject[key] = jObject[key]; // Should have no effect.

                Assert.False(jObject.Contains(new KeyValuePair<string, JsonNode?>("MISSING", jObject[key])));
                Assert.True(jObject.ContainsKey(key));

                // Remove() should not affect result when missing.
                bool success = jObject.Remove("MISSING");
                Assert.False(success);
            }

            // JSON shouldn't change.
            Assert.Equal(expectedJson, jObject.ToJsonString());

            // Remove every other entry.
            for (i = 0; i < count; i += 2)
            {
                bool success = dictionary.Remove(i.ToString());
                Assert.True(success);
            }

            Assert.Equal(count / 2, jObject.Count);

            // The new JSON contains half the entries.
            expectedJson = jObject.ToJsonString();

            // Add back every other entry (to the end)
            for (i = 0; i < count; i += 2)
            {
                jObject.Add(i.ToString(), JsonValue.Create(i));
            }
            Assert.Equal(count, jObject.Count);

            // The beginning part of the JSON should be the same.
            string json = jObject.ToJsonString();
            Assert.Contains(expectedJson.TrimEnd('}'), json);

            const int ItemsToAdd = 10;
            for (i = 10000; i < 10000 + ItemsToAdd; i++)
            {
                jObject.Add(i.ToString(), i);
            }

            // Original items should still be in front.
            json = jObject.ToJsonString();
            Assert.Contains(expectedJson.TrimEnd('}'), json);

            Assert.Equal(count + ItemsToAdd, jObject.Count);
        }

        public static IEnumerable<object[]> JObjectCollectionData()
        {
            // Ensure that the list-to-dictionary threshold is hit (currently 9).
            for (int i = 0; i < 25; i++)
            {
                yield return CreateArray(i);
                yield return CreateArray_JsonElement(i);
            }

            yield return CreateArray(123);
            yield return CreateArray_JsonElement(122);

            yield return CreateArray(300);
            yield return CreateArray_JsonElement(299);

            object[] CreateArray(int count)
            {
                JsonObject jObject = CreateJsonObject(count);
                return new object[] { jObject, count };
            }

            JsonObject CreateJsonObject(int count)
            {
                var jObject = new JsonObject();

                for (int i = 0; i < count; i++)
                {
                    jObject[i.ToString()] = i;
                }

                return jObject;
            }

            object[] CreateArray_JsonElement(int count)
            {
                JsonObject jObject = CreateJsonObject(count);
                string json = jObject.ToJsonString();
                jObject = JsonNode.Parse(json).AsObject();
                return new object[] { jObject, count };
            }
        }

        [Theory]
        [MemberData(nameof(JObjectCollectionData))]
        public static void ChangeCollectionWhileEnumeratingFails(JsonObject jObject, int count)
        {
            if (count == 0)
            {
                return;
            }

            Assert.Equal(count, jObject.Count);

            int index = 0;

            // Exception string sample: "Collection was modified; enumeration operation may not execute"
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach(KeyValuePair<string, JsonNode?> node in jObject)
                {
                    index++;
                    jObject.Add("New_A", index);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (KeyValuePair<string, JsonNode?> node in jObject)
                {
                    index++;
                    jObject.Remove(node.Key);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            IEnumerable iEnumerable = jObject;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (KeyValuePair<string, JsonNode?> node in iEnumerable)
                {
                    index++;
                    jObject.Add("New_B", index);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (KeyValuePair<string, JsonNode?> node in iEnumerable)
                {
                    index++;
                    jObject.Remove(node.Key);
                }
            });
            Assert.Equal(1, index);

            IDictionary<string, JsonNode?> iDictionary = jObject;

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (string str in iDictionary.Keys)
                {
                    index++;
                    jObject.Add("New_C", index);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (string str in (IEnumerable)iDictionary.Keys)
                {
                    index++;
                    jObject.Add("New_D", index);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (string str in iDictionary.Keys)
                {
                    index++;
                    jObject.Remove(str);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (JsonNode node in iDictionary.Values)
                {
                    index++;
                    jObject.Add("New_E", index);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (JsonNode node in (IEnumerable)iDictionary.Values)
                {
                    index++;
                    jObject.Add("New_F", index);
                }
            });
            Assert.Equal(1, index);

            index = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                foreach (JsonNode node in iDictionary.Values)
                {
                    index++;
                    jObject.Clear();
                }
            });
            Assert.Equal(1, index);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization.Tests;
using Xunit;

namespace System.Text.Json.Node.Tests
{
    public static class JsonObjectTests
    {
        [Fact]
        public static void KeyValuePair()
        {
            var jObject = new JsonObject();
            jObject["One"] = 1;
            jObject["Two"] = 2;

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

        [Fact]
        public static void IsReadOnly()
        {
            ICollection<KeyValuePair<string, JsonNode?>> jObject = new JsonObject();
            Assert.False(jObject.IsReadOnly);
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
            var jObject = new JsonObject();
            Assert.Throws<ArgumentNullException>(() => jObject.Add(null, JsonValue.Create(0)));
            Assert.Throws<ArgumentNullException>(() => jObject[null] = JsonValue.Create(0));
        }

        [Fact]
        public static void IEnumerable()
        {
            var jObject = new JsonObject();
            jObject["One"] = 1;
            jObject["Two"] = 2;

            IEnumerable enumerable = jObject;
            int count = 0;
            foreach (KeyValuePair<string, JsonNode?> node in enumerable)
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

            // Options on parent node.
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
            jObject.Add("myproperty", 42); // no exception since options were not set in time.
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
            Assert.Throws<InvalidOperationException>(() => jObject.Add("Prop", jValue));
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
    }
}

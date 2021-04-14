// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace System.Text.Json.Node.Tests
{
    public static class JsonArrayTests
    {
        [Fact]
        public static void FromElement()
        {
            using (JsonDocument document = JsonDocument.Parse("[42]"))
            {
                JsonArray jArray = JsonArray.Create(document.RootElement);
                Assert.Equal(42, jArray[0].GetValue<int>());
            }

            using (JsonDocument document = JsonDocument.Parse("null"))
            {
                JsonArray jArray = JsonArray.Create(document.RootElement);
                Assert.Null(jArray);
            }
        }

        [Theory]
        [InlineData("42")]
        [InlineData("{}")]
        public static void FromElement_WrongNodeTypeThrows(string json)
        {
            using (JsonDocument document = JsonDocument.Parse(json))
            Assert.Throws<InvalidOperationException>(() => JsonArray.Create(document.RootElement));
        }

        [Fact]
        public static void WriteTo_Validation()
        {
            Assert.Throws<ArgumentNullException>(() => new JsonArray().WriteTo(null));
        }

        [Fact]
        public static void WriteTo()
        {
            const string Json = "[42]";

            JsonArray jArray = JsonNode.Parse(Json).AsArray();
            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream);
            jArray.WriteTo(writer);
            writer.Flush();

            string json = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(Json, json);
        }

        [Fact]
        public static void WriteTo_Options()
        {
            JsonArray jArray = new JsonArray(42);

            // Baseline.
            var stream = new MemoryStream();
            var writer = new Utf8JsonWriter(stream);
            jArray.WriteTo(writer);
            writer.Flush();
            string json = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal("[42]", json);

            var options = new JsonSerializerOptions
            {
                NumberHandling = Serialization.JsonNumberHandling.WriteAsString
            };

            // With options.
            stream = new MemoryStream();
            writer = new Utf8JsonWriter(stream);
            jArray.WriteTo(writer, options);
            writer.Flush();
            json = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal("[\"42\"]", json);
        }

        [Fact]
        public static void Clear()
        {
            var jArray = new JsonArray(42, 43);
            Assert.Equal(2, jArray.Count);

            JsonNode node = jArray[0];
            jArray.Clear();
            Assert.Equal(0, jArray.Count);
            jArray.Add(node);
            Assert.Equal(1, jArray.Count);
        }

        [Fact]
        public static void AddOverloads()
        {
            var jArray = new JsonArray();
            jArray.Add((object)null);
            jArray.Add((int?)null);
            jArray.Add(1);
            Assert.Equal(3, jArray.Count);
        }

        [Fact]
        public static void IsReadOnly()
        {
            Assert.False(((IList<JsonNode>)new JsonArray()).IsReadOnly);
        }

        [Fact]
        public static void IEnumerable()
        {
            IEnumerable jArray = new JsonArray(1, 2);

            int count = 0;
            foreach (JsonNode? node in jArray)
            {
                count++;
            }

            Assert.Equal(2, count);
        }

        [Fact]
        public static void Contains_IndexOf_Remove_Insert()
        {
            JsonNode node1 = 1;
            JsonNode node2 = 2;
            JsonNode node3 = 3;

            var jArray = new JsonArray(node1, node2, node3);
            Assert.Equal(3, jArray.Count);

            Assert.Equal(0, jArray.IndexOf(node1));
            Assert.Equal(1, jArray.IndexOf(node2));
            Assert.Equal(2, jArray.IndexOf(node3));

            // Remove
            bool success = jArray.Remove(node2);
            Assert.True(success);
            Assert.Equal(2, jArray.Count);

            Assert.Equal(0, jArray.IndexOf(node1));
            Assert.Equal(-1, jArray.IndexOf(node2));
            Assert.Equal(1, jArray.IndexOf(node3));

            Assert.False(jArray.Remove(node2)); // remove an already removed node.
            Assert.Equal(2, jArray.Count);

            // Contains
            Assert.True(jArray.Contains(node1));
            Assert.False(jArray.Contains(node2));
            Assert.True(jArray.Contains(node3));

            // Insert
            jArray.Insert(1, node2);
            Assert.Equal(3, jArray.Count);

            Assert.Equal(0, jArray.IndexOf(node1));
            Assert.Equal(1, jArray.IndexOf(node2));
            Assert.Equal(2, jArray.IndexOf(node3));
        }

        [Fact]
        public static void CopyTo()
        {
            JsonNode node1 = 1;
            JsonNode node2 = 2;

            IList<JsonNode?> jArray = new JsonArray(node1, node2, null);
            var arr = new JsonNode[4];
            jArray.CopyTo(arr, 0);

            Assert.Same(node1, arr[0]);
            Assert.Same(node2, arr[1]);
            Assert.Null(arr[2]);

            arr = new JsonNode[5];
            jArray.CopyTo(arr, 1);
            Assert.Null(arr[0]);
            Assert.Same(node1, arr[1]);
            Assert.Same(node2, arr[2]);
            Assert.Null(arr[3]);
            Assert.Null(arr[4]);

            arr = new JsonNode[3];
            Assert.Throws<ArgumentException>(() => jArray.CopyTo(arr, 1));

            Assert.Throws<ArgumentOutOfRangeException>(() => jArray.CopyTo(arr, -1));
        }

        [Fact]
        public static void ReAddSameNode_Throws()
        {
            var jValue = JsonValue.Create(1);

            var jArray = new JsonArray(jValue);
            Assert.Throws<InvalidOperationException>(() => jArray.Add(jValue));
        }

        [Fact]
        public static void ReAddRemovedNode()
        {
            var jValue = JsonValue.Create(1);

            var jArray = new JsonArray(jValue);
            Assert.Equal(1, jArray.Count);
            jArray.Remove(jValue);
            Assert.Equal(0, jArray.Count);
            jArray.Add(jValue);
            Assert.Equal(1, jArray.Count);
        }

        [Fact]
        public static void CreatingJsonArrayFromNodeArray()
        {
            JsonNode[] expected = { "sushi", "pasta", "cucumber soup" };

            var dishesJsonArray = new JsonArray(expected);
            Assert.Equal(3, dishesJsonArray.Count);

            for (int i = 0; i < dishesJsonArray.Count; i++)
            {
                Assert.Equal(expected[i], dishesJsonArray[i]);
            }
        }

        [Fact]
        public static void CreatingJsonArrayFromArrayOfStrings()
        {
            var strangeWords = new string[]
            {
                "supercalifragilisticexpialidocious",
                "gladiolus",
                "albumen",
                "smaragdine",
            };

            var strangeWordsJsonArray = new JsonArray();
            strangeWords.Where(word => word.Length < 10).
                ToList().ForEach(str => strangeWordsJsonArray.Add(JsonValue.Create(str)));

            Assert.Equal(2, strangeWordsJsonArray.Count);

            string[] expected = { "gladiolus", "albumen" };

            for (int i = 0; i < strangeWordsJsonArray.Count; i++)
            {
                Assert.Equal(expected[i], strangeWordsJsonArray[i].GetValue<string>());
            }
        }

        [Fact]
        public static void CreatingNestedJsonArray()
        {
            var vertices = new JsonArray()
            {
                new JsonArray
                {
                    new JsonArray
                    {
                        new JsonArray { 0, 0, 0 },
                        new JsonArray { 0, 0, 1 }
                    },
                    new JsonArray
                    {
                        new JsonArray { 0, 1, 0 },
                        new JsonArray { 0, 1, 1 }
                    }
                },
                new JsonArray
                {
                    new JsonArray
                    {
                        new JsonArray { 1, 0, 0 },
                        new JsonArray { 1, 0, 1 }
                    },
                    new JsonArray
                    {
                        new JsonArray { 1, 1, 0 },
                        new JsonArray { 1, 1, 1 }
                    }
                },
            };

            var jArray = (JsonArray)vertices[0];
            Assert.Equal(2, jArray.Count());
            jArray = (JsonArray)jArray[1];
            Assert.Equal(2, jArray.Count());
            jArray = (JsonArray)jArray[0];
            Assert.Equal(3, jArray.Count());

            Assert.Equal(0, (int)jArray[0]);
            Assert.Equal(1, (int)jArray[1]);
            Assert.Equal(0, (int)jArray[2]);
        }

        [Fact]
        public static void NullHandling()
        {
            var jArray = new JsonArray() { "to be replaced" };

            jArray[0] = null;
            Assert.Equal(1, jArray.Count);
            Assert.Null(jArray[0]);

            jArray.Add(null);
            Assert.Equal(2, jArray.Count);
            Assert.Null(jArray[1]);

            jArray.Add(null);
            Assert.Equal(3, jArray.Count);
            Assert.Null(jArray[2]);

            jArray.Insert(3, null);
            Assert.Equal(4, jArray.Count);
            Assert.Null(jArray[3]);

            jArray.Insert(4, null);
            Assert.Equal(5, jArray.Count);
            Assert.Null(jArray[4]);

            Assert.True(jArray.Contains(null));
            Assert.Equal(0, jArray.IndexOf(null));

            jArray.Remove(null);
            Assert.Equal(4, jArray.Count);
        }

        [Fact]
        public static void AccesingNestedJsonArray()
        {
            var issues = new JsonObject
            {
                { "features", new JsonArray { "new functionality 1", "new functionality 2" } },
                { "bugs", new JsonArray { "bug 123", "bug 4566", "bug 821" } },
                { "tests", new JsonArray { "code coverage" } },
            };

            issues["bugs"].AsArray().Add("bug 12356");
            issues["features"].AsArray()[0] = "feature 1569";
            issues["features"].AsArray()[1] = "feature 56134";

            Assert.Equal("bug 12356", (string)issues["bugs"][3]);
            Assert.Equal("feature 1569", (string)issues["features"][0]);
            Assert.Equal("feature 56134", (string)issues["features"][1]);
        }

        [Fact]
        public static void Insert()
        {
            var jArray = new JsonArray() { 1 };
            Assert.Equal(1, jArray.Count);

            jArray.Insert(0, 0);

            Assert.Equal(2, jArray.Count);
            Assert.Equal(0, (int)jArray[0]);
            Assert.Equal(1, (int)jArray[1]);

            jArray.Insert(2, 3);

            Assert.Equal(3, jArray.Count);
            Assert.Equal(0, (int)jArray[0]);
            Assert.Equal(1, (int)jArray[1]);
            Assert.Equal(3, (int)jArray[2]);

            jArray.Insert(2, 2);

            Assert.Equal(4, jArray.Count);
            Assert.Equal(0, (int)jArray[0]);
            Assert.Equal(1, (int)jArray[1]);
            Assert.Equal(2, (int)jArray[2]);
            Assert.Equal(3, (int)jArray[3]);
        }

        [Fact]
        public static void HeterogeneousArray()
        {
            var mixedTypesArray = new JsonArray { 1, "value", true, null, 2.3, new JsonObject() };

            Assert.Equal(1, mixedTypesArray[0].GetValue<int>());
            Assert.Equal("value", mixedTypesArray[1].GetValue<string>());
            Assert.True(mixedTypesArray[2].GetValue<bool>());
            Assert.Null(mixedTypesArray[3]);
            Assert.Equal(2.3, mixedTypesArray[4].GetValue<double>());
            Assert.IsType<JsonObject>(mixedTypesArray[5]);

            mixedTypesArray.Add(false);
            mixedTypesArray.Insert(4, "another");
            mixedTypesArray.Add(null);

            Assert.False(mixedTypesArray[7].GetValue<bool>());
            Assert.Equal("another", mixedTypesArray[4].GetValue<string>());
            Assert.Null(mixedTypesArray[8]);
        }

        [Fact]
        public static void OutOfRangeException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonArray()[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonArray()[0]);
            Assert.Throws<ArgumentOutOfRangeException>(() => new JsonArray()[1]);
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var jArray = new JsonArray { 1, 2, 3 };
                jArray.Insert(4, 17);
            });
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var jArray = new JsonArray { 1, 2, 3 };
                jArray.Insert(-1, 17);
            });
        }

        [Fact]
        public static void GetJsonArrayIEnumerable()
        {
            IEnumerable jArray = new JsonArray() { 1, "value" };
            IEnumerator jArrayEnumerator = jArray.GetEnumerator();

            Assert.True(jArrayEnumerator.MoveNext());
            Assert.Equal(1, ((JsonValue)jArrayEnumerator.Current).GetValue<int>());
            Assert.True(jArrayEnumerator.MoveNext());
            Assert.Equal("value", ((JsonValue)jArrayEnumerator.Current).GetValue<string>());
        }
    }
}

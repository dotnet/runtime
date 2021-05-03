// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Text.Json.Node.Tests
{
    public static class ParentPathRootTests
    {
        [Fact]
        public static void GetPathAndRoot()
        {
            JsonNode node;

            node = JsonValue.Create(1);
            Assert.Equal("$", node.GetPath());
            Assert.Same(node, node.Root);

            node = new JsonObject();
            Assert.Equal("$", node.GetPath());
            Assert.Same(node, node.Root);

            node = new JsonArray();
            Assert.Equal("$", node.GetPath());
            Assert.Same(node, node.Root);

            node = new JsonObject
            {
                ["Child"] = 1
            };
            Assert.Equal("$.Child", node["Child"].GetPath());

            node = new JsonObject
            {
                ["Child"] = new JsonArray { 1, 2, 3 }
            };
            Assert.Equal("$.Child[1]", node["Child"][1].GetPath());
            Assert.Same(node, node["Child"][1].Root);

            node = new JsonObject
            {
                ["Child"] = new JsonArray { 1, 2, 3 }
            };
            Assert.Equal("$.Child[2]", node["Child"][2].GetPath());
            Assert.Same(node, node["Child"][2].Root);

            node = new JsonArray
            {
                new JsonObject
                {
                    ["Child"] = 42
                }
            };
            Assert.Equal("$[0].Child", node[0]["Child"].GetPath());
            Assert.Same(node, node[0]["Child"].Root);
        }

        [Fact]
        public static void GetPath_SpecialCharacters()
        {
            JsonNode node = new JsonObject
            {
                ["[Child"] = 1
            };

            Assert.Equal("$['[Child']", node["[Child"].GetPath());
        }

        [Fact]
        public static void DetectCycles_Object()
        {
            var jObject = new JsonObject();
            Assert.Throws<InvalidOperationException>(() => jObject.Add("a", jObject));

            var jObject2 = new JsonObject();
            jObject.Add("b", jObject2);
            Assert.Throws<InvalidOperationException>(() => jObject2.Add("b", jObject));
        }

        [Fact]
        public static void DetectCycles_Array()
        {
            var jArray = new JsonArray { };
            Assert.Throws<InvalidOperationException>(() => jArray.Add(jArray));

            var jArray2 = new JsonArray { };
            jArray.Add(jArray2);
            Assert.Throws<InvalidOperationException>(() => jArray2.Add(jArray));
        }

        [Fact]
        public static void DetectCycles_ArrayWithObject()
        {
            var jObject = new JsonObject { };
            var jArray = new JsonArray { };
            jArray.Add(jObject);

            Assert.Throws<InvalidOperationException>(() => jObject.Add("Prop", jArray));
        }

        [Fact]
        public static void Parent_Array()
        {
            var child = JsonValue.Create(42);
            Assert.Null(child.Options);

            var parent = new JsonArray();
            Assert.Null(parent.Options);
            parent.Add(child);
            Assert.Null(child.Options);
            parent.Clear();

            parent = new JsonArray(new JsonNodeOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(parent.Options);
            parent.Add(child);
            Assert.NotNull(child.Options);
            Assert.True(child.Options.Value.PropertyNameCaseInsensitive);

            // The indexer unparents existing node.
            parent[0] = JsonValue.Create(43);
            Assert.Null(child.Parent);

            parent.Clear();
            Assert.True(child.Options.Value.PropertyNameCaseInsensitive);

            // Adding to a new parent does not affect options previously obtained from a different parent.
            parent = new JsonArray(new JsonNodeOptions { PropertyNameCaseInsensitive = false });
            parent.Add(child);
            Assert.True(child.Options.Value.PropertyNameCaseInsensitive);
        }

        [Fact]
        public static void Parent_Object()
        {
            var child = JsonValue.Create(42);
            Assert.Null(child.Options);

            var parent = new JsonObject();
            Assert.Null(parent.Options);
            parent.Add("MyProp", child);
            Assert.Null(child.Options);
            parent.Clear();

            parent = new JsonObject(new JsonNodeOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(parent.Options);
            parent.Add("MyProp", child);
            Assert.NotNull(child.Options);
            Assert.True(child.Options.Value.PropertyNameCaseInsensitive);

            // The indexer unparents existing node.
            parent["MyProp"] = JsonValue.Create(43);
            Assert.Null(child.Parent);

            parent.Clear();
            Assert.True(child.Options.Value.PropertyNameCaseInsensitive);

            // Adding to a new parent does not affect options previously obtained from a different parent.
            parent = new JsonObject(new JsonNodeOptions { PropertyNameCaseInsensitive = false });
            parent.Add("MyProp", child);
            Assert.True(child.Options.Value.PropertyNameCaseInsensitive);
        }
    }
}

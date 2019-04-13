// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Text.Json.Tests
{
    public static class JsonElementCloneTests
    {
        [Fact]
        public static void CloneTwiceFromSameDocument()
        {
            string json = "[[]]";
            JsonElement root;
            JsonElement clone;
            JsonElement clone2;

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                root = doc.RootElement;
                clone = root.Clone();
                clone2 = root.Clone();

                Assert.Equal(json, clone.GetRawText());
                Assert.NotSame(doc, clone.SniffDocument());
                Assert.NotSame(doc, clone2.SniffDocument());
            }

            // After document Dispose
            Assert.Equal(json, clone.GetRawText());
            Assert.Equal(json, clone2.GetRawText());
            Assert.NotSame(clone.SniffDocument(), clone2.SniffDocument());

            Assert.Throws<ObjectDisposedException>(() => root.GetRawText());
        }

        [Fact]
        public static void CloneInnerElementFromClonedElement()
        {
            JsonElement clone;

            using (JsonDocument doc = JsonDocument.Parse("[[[]]]"))
            {
                JsonElement middle = doc.RootElement[0].Clone();
                JsonElement inner = middle[0];
                clone = inner.Clone();

                Assert.Equal(inner.GetRawText(), clone.GetRawText());
                Assert.NotSame(doc, clone.SniffDocument());
                Assert.Same(middle.SniffDocument(), clone.SniffDocument());
                Assert.Same(inner.SniffDocument(), clone.SniffDocument());
            }

            // After document Dispose
            Assert.Equal("[]", clone.GetRawText());
        }

        [Fact]
        public static void CloneAtInnerNumber()
        {
            CloneAtInner("1.21e9", JsonValueType.Number);
        }

        [Fact]
        public static void CloneAtInnerString()
        {
            CloneAtInner("\"  this  string  has  \\u0039 spaces\"", JsonValueType.String);
        }

        [Fact]
        public static void CloneAtInnerTrue()
        {
            CloneAtInner("true", JsonValueType.True);
        }

        [Fact]
        public static void CloneAtInnerFalse()
        {
            CloneAtInner("false", JsonValueType.False);
        }

        [Fact]
        public static void CloneAtInnerNull()
        {
            CloneAtInner("null", JsonValueType.Null);
        }

        [Fact]
        public static void CloneAtInnerObject()
        {
            // Very weird whitespace is used here just to ensure that the
            // clone API isn't making any whitespace assumptions.
            CloneAtInner(
                @"{
  ""this"":
  [
    {
      ""object"": 0,




      ""has"": [ ""whitespace"" ]
    }
  ]
}",
                JsonValueType.Object);
        }

        [Fact]
        public static void CloneAtInnerArray()
        {
            // Very weird whitespace is used here just to ensure that the
            // clone API isn't making any whitespace assumptions.
            CloneAtInner(
                @"[
{
  ""this"":
  [
    {
      ""object"": 0,




      ""has"": [ ""whitespace"" ]
    }
  ]
},

5

,



false,



null
]",
                JsonValueType.Array);
        }

        private static void CloneAtInner(string innerJson, JsonValueType valueType)
        {
            string json = $"{{ \"obj\": [ {{ \"not target\": true, \"target\": {innerJson} }}, 5 ] }}";

            JsonElement clone;

            using (JsonDocument doc = JsonDocument.Parse(json))
            {
                JsonElement target = doc.RootElement.GetProperty("obj")[0].GetProperty("target");
                Assert.Equal(valueType, target.Type);
                clone = target.Clone();
            }

            Assert.Equal(innerJson, clone.GetRawText());
        }

        private static JsonDocument SniffDocument(this JsonElement element)
        {
            return (JsonDocument)typeof(JsonElement).
                GetField("_parent", BindingFlags.Instance|BindingFlags.NonPublic).
                GetValue(element);
        }
    }
}

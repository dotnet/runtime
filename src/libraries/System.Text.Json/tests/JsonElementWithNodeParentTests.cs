// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;
using Xunit;

namespace System.Text.Json.Tests
{
    public static class JsonElementWithNodeParentTests
    {
        [Fact]
        public static void TestArray()
        {
            var jsonArray = new JsonArray() { 1, 2, 3 };
            JsonElement jsonArrayElement = jsonArray.AsJsonElement();

            Assert.Equal(2, jsonArrayElement[1].GetInt32());
            Assert.Equal(3, jsonArrayElement.GetArrayLength());

            var notJsonArray = new JsonString();
            JsonElement notJsonArrayElement = notJsonArray.AsJsonElement();

            Assert.Throws<InvalidOperationException>(() => notJsonArrayElement[1]);
            Assert.Throws<InvalidOperationException>(() => notJsonArrayElement.GetArrayLength());
        }

        [Fact]
        public static void TestArrayEnumerator()
        {
            var jsonArray = new JsonArray() { 1, 2, 3 };
            JsonElement jsonArrayElement = jsonArray.AsJsonElement();

            JsonElement.ArrayEnumerator arrayEnumerator = jsonArrayElement.EnumerateArray();

            for (int i = 1; i <= 3; i++)
            {
                Assert.True(arrayEnumerator.MoveNext());
                Assert.Equal(i, arrayEnumerator.Current.GetInt32());
            }

            Assert.False(arrayEnumerator.MoveNext());
            Assert.False(arrayEnumerator.MoveNext());

            JsonElement notArray = new JsonObject().AsJsonElement();
            Assert.Throws<InvalidOperationException>(() => notArray.EnumerateArray());
        }

        [Fact]
        public static void TestObject()
        {
            var jsonObject = new JsonObject()
            {
                ["existing property"] = "value",
                ["different property"] = 14
            };

            JsonElement jsonObjectElement = jsonObject.AsJsonElement();
            Assert.True(jsonObjectElement.TryGetProperty("existing property", out JsonElement propertyValue));
            Assert.Equal("value", propertyValue.GetString());
            Assert.True(jsonObjectElement.TryGetProperty(Encoding.UTF8.GetBytes("existing property"), out propertyValue));
            Assert.Equal("value", propertyValue.GetString());

            Assert.Throws<InvalidOperationException>(() => propertyValue.TryGetProperty("property of not JsonObject", out _));
            Assert.Throws<InvalidOperationException>(() => propertyValue.TryGetProperty(Encoding.UTF8.GetBytes("property of not JsonObject"), out _));

            Assert.False(jsonObjectElement.TryGetProperty("not existing property", out propertyValue));
            Assert.Equal(default, propertyValue);

            Assert.False(jsonObjectElement.TryGetProperty(Encoding.UTF8.GetBytes("not existing property"), out propertyValue));
            Assert.Equal(default, propertyValue);
        }

        [Fact]
        public static void TestObjectEnumerator()
        {
            var jsonObject = new JsonObject()
            {
                ["1"] = 1,
                ["2"] = 2,
                ["3"] = 3
            };
            JsonElement jsonObjectElement = jsonObject.AsJsonElement();

            JsonElement.ObjectEnumerator objectEnumerator = jsonObjectElement.EnumerateObject();

            Assert.Equal(default, objectEnumerator.Current);

            for (int i = 1; i <= 3; i++)
            {
                Assert.True(objectEnumerator.MoveNext());
                Assert.Equal(i.ToString(), objectEnumerator.Current.Name);
                Assert.Equal(i, objectEnumerator.Current.Value.GetInt32());
            }

            Assert.False(objectEnumerator.MoveNext());
            Assert.False(objectEnumerator.MoveNext());

            jsonObject["2"] = 4;
            Assert.Throws<InvalidOperationException>(() => objectEnumerator.MoveNext());


            JsonElement notObject = new JsonArray().AsJsonElement();
            Assert.Throws<InvalidOperationException>(() => notObject.EnumerateObject());
        }

        [Fact]
        public static void TestBoolean()
        {
            Assert.True(new JsonBoolean(true).AsJsonElement().GetBoolean());
            Assert.Throws<InvalidOperationException>(() => new JsonString().AsJsonElement().GetBoolean());
        }

        [Fact]
        public static void TestString()
        {
            Assert.Equal("value", new JsonString("value").AsJsonElement().GetString());
            Assert.Throws<InvalidOperationException>(() => new JsonBoolean().AsJsonElement().GetString());
        }

        [Fact]
        public static void TestBytesFromBase64()
        {
            Assert.Throws<NotSupportedException>(() => new JsonString().AsJsonElement().GetBytesFromBase64());
            Assert.Throws<InvalidOperationException>(() => new JsonBoolean().AsJsonElement().GetBytesFromBase64());
        }

        [Fact]
        public static void TestByte()
        {
            Assert.Equal(byte.MaxValue, new JsonNumber(byte.MaxValue).AsJsonElement().GetByte());
            Assert.Throws<InvalidOperationException>(() => new JsonString().AsJsonElement().GetByte());
        }

        [Fact]
        public static void TestInt16()
        {
            Assert.Equal(short.MaxValue, new JsonNumber(short.MaxValue).AsJsonElement().GetInt16());
            Assert.Throws<InvalidOperationException>(() => new JsonString().AsJsonElement().GetInt16());
        }

        [Fact]
        public static void TestInt32()
        {
            Assert.Equal(int.MaxValue, new JsonNumber(int.MaxValue).AsJsonElement().GetInt32());
            Assert.Throws<InvalidOperationException>(() => new JsonString().AsJsonElement().GetInt32());
        }

        [Fact]
        public static void TestInt64()
        {
            Assert.Equal(long.MaxValue, new JsonNumber(long.MaxValue).AsJsonElement().GetInt64());
            Assert.Throws<InvalidOperationException>(() => new JsonString().AsJsonElement().GetInt64());
        }

        [Fact]
        public static void TestSingle()
        {
            Assert.Equal(3.14f, new JsonNumber(3.14f).AsJsonElement().GetSingle());
            Assert.Throws<InvalidOperationException>(() => new JsonString().AsJsonElement().GetSingle());
        }

        [Fact]
        public static void TestDouble()
        {
            Assert.Equal(3.14, new JsonNumber(3.14).AsJsonElement().GetDouble());
            Assert.Throws<InvalidOperationException>(() => new JsonString().AsJsonElement().GetDouble());
        }

        [Fact]
        public static void TestSByte()
        {
            Assert.Equal(sbyte.MaxValue, new JsonNumber(sbyte.MaxValue).AsJsonElement().GetSByte());
            Assert.Throws<InvalidOperationException>(() => new JsonString().AsJsonElement().GetSByte());
        }

        [Fact]
        public static void TestUInt16()
        {
            Assert.Equal(ushort.MaxValue, new JsonNumber(ushort.MaxValue).AsJsonElement().GetUInt16());
            Assert.Throws<InvalidOperationException>(() => new JsonString().AsJsonElement().GetUInt16());
        }

        [Fact]
        public static void TestUInt32()
        {
            Assert.Equal(uint.MaxValue, new JsonNumber(uint.MaxValue).AsJsonElement().GetUInt32());
            Assert.Throws<InvalidOperationException>(() => new JsonString().AsJsonElement().GetUInt32());
        }

        [Fact]
        public static void TestUInt64()
        {
            Assert.Equal(ulong.MaxValue, new JsonNumber(ulong.MaxValue).AsJsonElement().GetUInt64());
            Assert.Throws<InvalidOperationException>(() => new JsonString().AsJsonElement().GetUInt64());
        }

        [Fact]
        public static void TestDecimal()
        {
            Assert.Equal(decimal.One, new JsonNumber(decimal.One).AsJsonElement().GetDecimal());
            Assert.Throws<InvalidOperationException>(() => new JsonString().AsJsonElement().GetDecimal());
        }

        [Fact]
        public static void TestDateTime()
        {
            var dateTime = new DateTime(2019, 1, 1);
            Assert.Equal(dateTime, new JsonString(dateTime).AsJsonElement().GetDateTime());
            Assert.Throws<InvalidOperationException>(() => new JsonBoolean().AsJsonElement().GetDateTime());
        }

        [Fact]
        public static void TestDateOffset()
        {
            var dateTimeOffset = DateTimeOffset.ParseExact("2019-01-01T00:00:00", "s", CultureInfo.InvariantCulture);
            Assert.Equal(dateTimeOffset, new JsonString(dateTimeOffset).AsJsonElement().GetDateTimeOffset());
            Assert.Throws<InvalidOperationException>(() => new JsonBoolean().AsJsonElement().GetDateTimeOffset());
        }

        [Fact]
        public static void TestGuid()
        {
            Guid guid = Guid.ParseExact("ca761232-ed42-11ce-bacd-00aa0057b223", "D");
            Assert.Equal(guid, new JsonString(guid).AsJsonElement().GetGuid());
            Assert.Throws<InvalidOperationException>(() => new JsonBoolean().AsJsonElement().GetGuid());
        }

        [Fact]
        public static void TestGetRawText()
        {
            string jsonToParse = @"{""property name"":""value""}";
            string rawText = JsonNode.Parse(jsonToParse).AsJsonElement().GetRawText();
            Assert.Equal(jsonToParse, rawText);
        }

        [Fact]
        public static void TestWriteTo()
        {
            var jsonObject = new JsonObject()
            {
                ["property"] = "value",
                ["array"] = new JsonArray() { 1, 2 }
            };

            var stream = new MemoryStream();
            using (var writer = new Utf8JsonWriter(stream))
            {
                jsonObject.AsJsonElement().WriteTo(writer);
                string result = Encoding.UTF8.GetString(stream.ToArray());
                Assert.Equal(jsonObject.ToJsonString(), result);
            }
        }

        [Fact]
        public static void TestToString()
        {
            var jsonObject = new JsonObject()
            {
                ["text"] = "value",
                ["boolean"] = true,
                ["null"] = null,
                ["array"] = new JsonArray() { 1, 2 }
            };

            string toStringResult = jsonObject.AsJsonElement().ToString();
            Assert.Equal(jsonObject.ToJsonString(), toStringResult);
        }
        
        [Fact]
        public static void TestClone()
        {
            var jsonObject = new JsonObject
            {
                ["text"] = "value",
                ["boolean"] = true,
                ["null"] = null,
                ["array"] = new JsonArray() { 1, 2 }
            };

            JsonElement jsonElementCopy = jsonObject.AsJsonElement().Clone();
            Assert.Equal("value", jsonElementCopy.GetProperty("text").GetString());
            Assert.True(jsonElementCopy.GetProperty("boolean").GetBoolean());
            Assert.Equal(JsonValueKind.Null, jsonElementCopy.GetProperty("null").ValueKind);

            JsonElement.ArrayEnumerator arrayEnumerator = jsonElementCopy.GetProperty("array").EnumerateArray();

            Assert.True(arrayEnumerator.MoveNext());
            Assert.Equal(1, arrayEnumerator.Current.GetInt32());
            Assert.True(arrayEnumerator.MoveNext());
            Assert.Equal(2, arrayEnumerator.Current.GetInt32());
            Assert.False(arrayEnumerator.MoveNext());
            Assert.Equal(default, arrayEnumerator.Current);

            var jsonObjectFromCopy = (JsonObject)JsonNode.GetNode(jsonElementCopy);
            
            jsonObject["text"] = "different value";
            Assert.Equal("value", jsonObjectFromCopy["text"]);

            jsonObjectFromCopy["boolean"] = false;
            Assert.Equal(true, jsonObject["boolean"]);
        }
    }
}

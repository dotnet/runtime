// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Tests
{
    public static class JsonStringTests
    {
        [Fact]
        public static void TestDefaultCtor()
        {
            var jsonString = new JsonString();
            Assert.Equal("", jsonString.Value);
        }

        [InlineData("value")]
        [InlineData("value with some spaces")]
        [InlineData("     leading spaces")]
        [InlineData("trailing spaces"     )]
        [InlineData("new lines\r\n")]
        [InlineData("tabs\ttabs\t")]
        [InlineData("\\u003e\\u003e\\u003e\\u003e\\u003e")]
        [InlineData("zażółć gęślą jaźń")]
        [InlineData("\u6f22\u5b57 \u6f22\u5b57")]
        [InlineData(">><++>>>\">>\\>>&>>>\u6f22\u5B57>>>")]
        [InlineData("Here is a string: \\\"\\\"\":\"Here is a\",\"Here is a back slash\\\\\":[\"Multiline\\r\\n String\\r\\n\",\"\\tMul\\r\\ntiline String\",\"\\\"somequote\\\"\\tMu\\\"\\\"l\\r\\ntiline\\\"another\\\" String\\\\\"],\"str")]
        [InlineData("Hello / a / b / c \\/ \\r\\b\\n\\f\\t\\/")]
        [Theory]
        public static void TestInitialization(string value)
        {
            // Default constructor:
            var defaultlyInitializedJsonString = new JsonString();
            defaultlyInitializedJsonString.Value = value;
            Assert.Equal(value, defaultlyInitializedJsonString.Value);

            // To string:
            Assert.Equal(value, defaultlyInitializedJsonString.ToString());

            // Value constructor:
            Assert.Equal(value, new JsonString(value).Value);

            // Implicit operator:            
            JsonString implicitlyInitializiedJsonString = value;
            Assert.Equal(value, implicitlyInitializiedJsonString.Value);

            // Casted to span:
            ReadOnlySpan<char> spanValue = value.AsSpan();
            Assert.Equal(value, new JsonString(spanValue).Value);
        }

        [Fact]
        public static void TestNulls()
        {
            string property = null;
            Assert.Throws<ArgumentNullException>(() => new JsonString(property));
            Assert.Throws<ArgumentNullException>(() => new JsonString().Value = null);
        }

        [Fact]
        public static void TestReadonlySpan()
        {
            var spanValue = new ReadOnlySpan<char>(new char[] { 's', 'p', 'a', 'n' });
            Assert.Equal("span", new JsonString(spanValue).Value);

            string property = null;
            spanValue = property.AsSpan();
            var jsonString = new JsonString(spanValue);
            Assert.Equal("", jsonString.Value);
        }

        [Fact]
        public static void TestGuid()
        {
            var guidString = "ca761232-ed42-11ce-bacd-00aa0057b223";
            Guid guid = new Guid(guidString);
            var jsonString = new JsonString(guid);
            Assert.Equal(guidString, jsonString);
        }

        [Fact]
        public static void TestDateTime()
        {
            DateTime dateTime = new DateTime(DateTime.MinValue.Ticks);
            var jsonString = new JsonString(dateTime);
            Assert.Equal(dateTime.ToString(), jsonString);
        }

        [Fact]
        public static void TestDateTimeOffset()
        {
            DateTimeOffset dateTimeOffset = new DateTime(DateTime.MinValue.Ticks);
            var jsonString = new JsonString(dateTimeOffset);
            Assert.Equal(dateTimeOffset.ToString(), jsonString);
        }

        [Fact]
        public static void TestChangingValue()
        {
            var jsonString = new JsonString();
            jsonString.Value = "property value";
            Assert.Equal("property value", jsonString.Value);
            jsonString.Value = "different property value";
            Assert.Equal("different property value", jsonString.Value);
        }

        [Fact]
        public static void TestEquals()
        {
            var jsonString = new JsonString("json property value");

            Assert.True(jsonString.Equals(new JsonString("json property value")));
            Assert.True(new JsonString("json property value").Equals(jsonString));

            Assert.False(jsonString.Equals(new JsonString("jsonpropertyvalue")));
            Assert.False(jsonString.Equals(new JsonString("Json Property Value")));
            Assert.False(new JsonString("jsonpropertyvalue").Equals(jsonString));
            Assert.False(new JsonString("Json Property Value").Equals(jsonString));

            Assert.True(jsonString == new JsonString("json property value"));
            Assert.True(jsonString != new JsonString("something different"));

            JsonNode jsonNode = new JsonString("json property value");
            Assert.True(jsonString.Equals(jsonNode));

            IEquatable<JsonString> jsonStringIEquatable = jsonString;
            Assert.True(jsonStringIEquatable.Equals(jsonString));
            Assert.True(jsonString.Equals(jsonStringIEquatable));

            Assert.False(jsonString.Equals(null));

            object jsonStringCopy = jsonString;
            object jsonStringObject = new JsonString("json property value");
            Assert.True(jsonString.Equals(jsonStringObject));
            Assert.True(jsonStringCopy.Equals(jsonStringObject));
            Assert.True(jsonStringObject.Equals(jsonString));

            jsonString = new JsonString();
            Assert.True(jsonString.Equals(new JsonString()));
            Assert.True(jsonString.Equals(new JsonString("")));
            Assert.False(jsonString.Equals(new JsonString("something not empty")));

            Assert.False(jsonString.Equals(new Exception()));
            Assert.False(jsonString.Equals(new JsonBoolean()));
            Assert.False(new JsonString("true").Equals(new JsonBoolean(true)));
            Assert.False(new JsonString("123").Equals(new JsonNumber("123")));

            JsonString jsonStringNull = null;
            Assert.False(jsonString == jsonStringNull);
            Assert.False(jsonStringNull == jsonString);

            Assert.True(jsonString != jsonStringNull);
            Assert.True(jsonStringNull != jsonString);

            JsonString otherJsonStringNull = null;
            Assert.True(jsonStringNull == otherJsonStringNull);
        }

        [Fact]
        public static void TestGetHashCode()
        {
            var jsonString = new JsonString("json property value");

            int expectedHashCode = jsonString.GetHashCode();
            Assert.Equal(expectedHashCode, new JsonString("json property value").GetHashCode());
            Assert.NotEqual(jsonString.GetHashCode(), new JsonString("json property value ").GetHashCode());
            Assert.NotEqual(jsonString.GetHashCode(), new JsonString("jsonpropertyvalue").GetHashCode());
            Assert.NotEqual(jsonString.GetHashCode(), new JsonString("Json Property Value").GetHashCode());
            Assert.NotEqual(jsonString.GetHashCode(), new JsonString("SOMETHING COMPLETELY DIFFERENT").GetHashCode());
            Assert.NotEqual(jsonString.GetHashCode(), new JsonString("").GetHashCode());
            Assert.NotEqual(jsonString.GetHashCode(), new JsonString().GetHashCode());
            
            JsonNode jsonNode = new JsonString("json property value");
            Assert.Equal(jsonString.GetHashCode(), jsonNode.GetHashCode());

            IEquatable<JsonString> jsonStringIEquatable = jsonString;
            Assert.Equal(jsonString.GetHashCode(), jsonStringIEquatable.GetHashCode());

            object jsonNumberCopy = jsonString;
            object jsonNumberObject = new JsonString("something different");

            Assert.Equal(jsonString.GetHashCode(), jsonNumberCopy.GetHashCode());
            Assert.NotEqual(jsonString.GetHashCode(), jsonNumberObject.GetHashCode());

            Assert.Equal(new JsonString().GetHashCode(), new JsonString().GetHashCode());
        }
    }
}

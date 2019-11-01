// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Tests
{
    public static class JsonNullTests
    {
        [Fact]
        public static void TestToString()
        {
            Assert.Equal("null", new JsonNull().ToString());
        }

        [Fact]
        public static void TestEquals()
        {
            var jsonNull1 = new JsonNull();
            var jsonNull2 = new JsonNull();

            Assert.True(jsonNull1.Equals(jsonNull2));
            Assert.True(jsonNull1 == jsonNull2);
            Assert.False(jsonNull1 != jsonNull2);

            JsonNode jsonNodeJsonNull = new JsonNull();
            Assert.True(jsonNull1.Equals(jsonNodeJsonNull));
            Assert.True(jsonNodeJsonNull.Equals(jsonNull1));

            IEquatable<JsonNull> jsonNullIEquatable = jsonNull2;
            Assert.True(jsonNullIEquatable.Equals(jsonNull1));
            Assert.True(jsonNull1.Equals(jsonNullIEquatable));

            object jsonNullCopy = jsonNull1;

            Assert.True(jsonNullCopy.Equals(jsonNull1));
            Assert.True(jsonNull1.Equals(jsonNullCopy));

            object jsonNullObject = new JsonNull();

            Assert.True(jsonNullObject.Equals(jsonNull1));
            Assert.True(jsonNull1.Equals(jsonNullObject));

            Assert.False(jsonNull1.Equals(new JsonString("null")));
            Assert.False(jsonNull1.Equals(new Exception()));

            // Null comparisons behave this way because of implicit conversion from null to JsonNull:
           
            Assert.True(jsonNull1.Equals(null));
            Assert.True(jsonNull1 == null);
            Assert.False(jsonNull1 != null);

            JsonNull jsonNullNull = null;

            Assert.True(jsonNull1.Equals(jsonNullNull));
            Assert.True(jsonNull1 == jsonNullNull);
            Assert.False(jsonNull1 != jsonNullNull);

            JsonNull otherJsonNullNull = null;
            Assert.True(jsonNullNull == otherJsonNullNull);

            // Only for null JsonNode / different derived type this will return false:

            JsonNode jsonNodeNull = null;
            Assert.False(jsonNull1.Equals(jsonNodeNull));
          
            JsonArray jsonArrayNull = null;
            Assert.False(jsonNull1.Equals(jsonArrayNull));
        }

        [Fact]
        public static void TestGetHashCode()
        {
            var jsonNull = new JsonNull();

            Assert.Equal(jsonNull.GetHashCode(), new JsonNull().GetHashCode());
            
            JsonNode jsonNodeNull = new JsonNull();
            Assert.Equal(jsonNull.GetHashCode(), jsonNodeNull.GetHashCode());
            
            IEquatable<JsonNull> jsonNullIEquatable = new JsonNull();
            Assert.Equal(jsonNull.GetHashCode(), jsonNullIEquatable.GetHashCode());

            object jsonNullCopy= jsonNull;
            object jsonNullObject = new JsonNull();
            Assert.Equal(jsonNull.GetHashCode(), jsonNullCopy.GetHashCode());
            Assert.Equal(jsonNull.GetHashCode(), jsonNullObject.GetHashCode());

            Assert.Equal(-1, jsonNull.GetHashCode());
        }

        [Fact]
        public static void TestValueKind()
        {
            Assert.Equal(JsonValueKind.Null, new JsonNull().ValueKind);
        }
    }
}

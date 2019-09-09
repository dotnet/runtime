// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Tests
{
    public static class JsonBooleanTests
    {
        [Fact]
        public static void TestDefaultCtor()
        {
            var jsonBoolean = new JsonBoolean();
            Assert.False(jsonBoolean.Value);
        }

        [Fact]
        public static void TestValueCtor()
        {
            var jsonBoolean = new JsonBoolean(true);
            Assert.True(jsonBoolean.Value);

            jsonBoolean = new JsonBoolean(false);
            Assert.False(jsonBoolean.Value);
        }

        [Fact]
        public static void TestImplicitOperator()
        {
            JsonNode jsonNode = true;
            JsonBoolean jsonBoolean = (JsonBoolean)jsonNode;
            Assert.True(jsonBoolean.Value);

            jsonNode = false;
            jsonBoolean = (JsonBoolean)jsonNode;
            Assert.False(jsonBoolean.Value);
        }

        [Fact]
        public static void TestChangingValue()
        {
            var jsonBoolean = new JsonBoolean();
            jsonBoolean.Value = true;
            Assert.True(jsonBoolean.Value);
            jsonBoolean.Value = false;
            Assert.False(jsonBoolean.Value);
        }

        [Fact]
        public static void TestToString()
        {
            Assert.Equal("true", new JsonBoolean(true).ToString());
            Assert.Equal("false", new JsonBoolean(false).ToString());
        }

        [Fact]
        public static void TestEquals()
        {
            var jsonBooleanTrue = new JsonBoolean(true);
            var jsonBooleanFalse = new JsonBoolean(false);

            Assert.True(jsonBooleanTrue.Equals(new JsonBoolean(true)));
            Assert.True(new JsonBoolean(true).Equals(jsonBooleanTrue));

            Assert.True(jsonBooleanFalse.Equals(new JsonBoolean(false)));
            Assert.True(new JsonBoolean(false).Equals(jsonBooleanFalse));
            Assert.True(new JsonBoolean().Equals(jsonBooleanFalse));
            Assert.False(new JsonBoolean().Equals(jsonBooleanTrue));
            Assert.False(jsonBooleanFalse.Equals(jsonBooleanTrue));
            Assert.False(jsonBooleanTrue.Equals(jsonBooleanFalse));

            Assert.True(jsonBooleanTrue == new JsonBoolean(true));
            Assert.True(jsonBooleanTrue != jsonBooleanFalse);

            JsonNode jsonNodeTrue = new JsonBoolean(true);
            Assert.True(jsonBooleanTrue.Equals(jsonNodeTrue));

            JsonNode jsonNodeFalse= new JsonBoolean(false);
            Assert.True(jsonBooleanFalse.Equals(jsonNodeFalse));

            IEquatable<JsonBoolean> jsonBooleanIEquatableTrue = jsonBooleanTrue;
            Assert.True(jsonBooleanIEquatableTrue.Equals(jsonBooleanTrue));
            Assert.True(jsonBooleanTrue.Equals(jsonBooleanIEquatableTrue));

            IEquatable<JsonBoolean> jsonBooleanIEquatableFalse = jsonBooleanFalse;
            Assert.True(jsonBooleanIEquatableFalse.Equals(jsonBooleanFalse));
            Assert.True(jsonBooleanFalse.Equals(jsonBooleanIEquatableFalse));

            Assert.False(jsonBooleanTrue.Equals(null));

            object jsonBooleanCopyTrue = jsonBooleanTrue;
            object jsonBooleanObjectTrue = new JsonBoolean(true);
            Assert.True(jsonBooleanCopyTrue.Equals(jsonBooleanObjectTrue));
            Assert.True(jsonBooleanCopyTrue.Equals(jsonBooleanObjectTrue));
            Assert.True(jsonBooleanObjectTrue.Equals(jsonBooleanTrue));

            object jsonBooleanCopyFalse = jsonBooleanFalse;
            object jsonBooleanObjectFalse = new JsonBoolean(false);
            Assert.True(jsonBooleanCopyFalse.Equals(jsonBooleanObjectFalse));
            Assert.True(jsonBooleanCopyFalse.Equals(jsonBooleanFalse));
            Assert.True(jsonBooleanObjectFalse.Equals(jsonBooleanFalse));

            Assert.False(jsonBooleanTrue.Equals(new Exception()));

            JsonBoolean jsonBooleanNull = null;
            Assert.False(jsonBooleanTrue == jsonBooleanNull);
            Assert.False(jsonBooleanNull == jsonBooleanTrue);

            Assert.True(jsonBooleanTrue != jsonBooleanNull);
            Assert.True(jsonBooleanNull != jsonBooleanTrue);

            JsonBoolean otherJsonBooleanNull = null;
            Assert.True(jsonBooleanNull == otherJsonBooleanNull);
        }

        [Fact]
        public static void TestGetHashCode()
        {
            var jsonBooleanTrue = new JsonBoolean(true);
            var jsonBooleanFalse = new JsonBoolean(false);

            Assert.Equal(jsonBooleanTrue.GetHashCode(), jsonBooleanTrue.GetHashCode());
            Assert.Equal(jsonBooleanTrue.GetHashCode(), new JsonBoolean(true).GetHashCode());
            Assert.Equal(jsonBooleanFalse.GetHashCode(), jsonBooleanFalse.GetHashCode());
            Assert.Equal(jsonBooleanFalse.GetHashCode(), new JsonBoolean(false).GetHashCode());
            Assert.Equal(jsonBooleanFalse.GetHashCode(), new JsonBoolean().GetHashCode());
            Assert.NotEqual(jsonBooleanTrue.GetHashCode(), new JsonBoolean().GetHashCode());

            JsonNode jsonNodeTrue = new JsonBoolean(true);
            Assert.Equal(jsonBooleanTrue.GetHashCode(), jsonNodeTrue.GetHashCode());
            JsonNode jsonNodeFalse = new JsonBoolean(false);
            Assert.Equal(jsonBooleanFalse.GetHashCode(), jsonNodeFalse.GetHashCode());

            Assert.NotEqual(jsonBooleanTrue.GetHashCode(), jsonBooleanFalse.GetHashCode());

            IEquatable<JsonBoolean> jsonBooleanIEquatableTrue = jsonBooleanTrue;
            Assert.Equal(jsonBooleanIEquatableTrue.GetHashCode(), jsonBooleanTrue.GetHashCode());

            IEquatable<JsonBoolean> jsonBooleanIEquatableFalse = jsonBooleanFalse;
            Assert.Equal(jsonBooleanIEquatableFalse.GetHashCode(), jsonBooleanFalse.GetHashCode());

            object jsonBooleanCopyTrue = jsonBooleanTrue;
            object jsonBooleanObjectTrue = new JsonBoolean(true);
            Assert.Equal(jsonBooleanTrue.GetHashCode(), jsonBooleanCopyTrue.GetHashCode());
            Assert.Equal(jsonBooleanTrue.GetHashCode(), jsonBooleanObjectTrue.GetHashCode());

            object jsonBooleanCopyFalse = jsonBooleanFalse;
            object jsonBooleanObjectFalse = new JsonBoolean(false);
            Assert.Equal(jsonBooleanCopyFalse.GetHashCode(), jsonBooleanFalse.GetHashCode());
            Assert.Equal(jsonBooleanObjectFalse.GetHashCode(), jsonBooleanFalse.GetHashCode());

            Assert.Equal(0, jsonBooleanFalse.GetHashCode());
            Assert.Equal(1, jsonBooleanTrue.GetHashCode());
        }

        [Fact]
        public static void TestValueKind()
        {
            Assert.Equal(JsonValueKind.True, new JsonBoolean(true).ValueKind);
            Assert.Equal(JsonValueKind.False, new JsonBoolean(false).ValueKind);
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class NullTests
    {
        [Fact]
        public static void ClassWithNullProperty()
        {
            TestClassWithNull obj = JsonSerializer.Parse<TestClassWithNull>(TestClassWithNull.s_json);
            obj.Verify();
        }

        [Fact]
        public static void RootObjectIsNull()
        {
            {
                TestClassWithNull obj = JsonSerializer.Parse<TestClassWithNull>("null");
                Assert.Null(obj);
            }

            {
                object obj = JsonSerializer.Parse<object>("null");
                Assert.Null(obj);
            }
        }

        [Fact]
        public static void DefaultIgnoreNullValuesOnRead()
        {
            TestClassWithInitializedProperties obj = JsonSerializer.Parse<TestClassWithInitializedProperties>(TestClassWithInitializedProperties.s_null_json);
            Assert.Equal(null, obj.MyString);
            Assert.Equal(null, obj.MyInt);
            Assert.Equal(null, obj.MyIntArray);
            Assert.Equal(null, obj.MyIntList);
        }

        [Fact]
        public static void EnableIgnoreNullValuesOnRead()
        {
            var options = new JsonSerializerOptions();
            options.IgnoreNullValues = true;

            TestClassWithInitializedProperties obj = JsonSerializer.Parse<TestClassWithInitializedProperties>(TestClassWithInitializedProperties.s_null_json, options);
            Assert.Equal("Hello", obj.MyString);
            Assert.Equal(1, obj.MyInt);
            Assert.Equal(1, obj.MyIntArray[0]);
            Assert.Equal(1, obj.MyIntList[0]);
        }

        [Fact]
        public static void ParseNullArgumentFail()
        {
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Parse<string>((string)null));
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Parse("1", (Type)null));
        }

        [Fact]
        public static void NullLiteralObjectInput()
        {
            {
                string obj = JsonSerializer.Parse<string>("null");
                Assert.Null(obj);
            }

            {
                string obj = JsonSerializer.Parse<string>(@"""null""");
                Assert.Equal("null", obj);
            }
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class StringTests
    {
        [Fact]
        public static void NullObjectInputFail()
        {

            Assert.Throws<ArgumentNullException>(() => JsonSerializer.Parse<string>((string)null));
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

        [Fact]
        public static void EmptyStringInput()
        {
            string obj = JsonSerializer.Parse<string>(@"""""");
            Assert.Equal(string.Empty, obj);
        }

        [Fact]
        public static void ReadSimpleClass()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(SimpleTestClass.s_json);
            obj.Verify();
        }

        [Fact]
        public static void EmptyClassWithRandomData()
        {
            JsonSerializer.Parse<EmptyClass>(SimpleTestClass.s_json);
            JsonSerializer.Parse<EmptyClass>(SimpleTestClassWithNulls.s_json);
        }
    }
}

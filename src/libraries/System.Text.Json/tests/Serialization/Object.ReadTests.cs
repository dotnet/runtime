// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class ObjectTests
    {
        [Fact]
        public static void ReadSimpleClass()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>(SimpleTestClass.s_json);
            obj.Verify();
        }

        [Fact]
        public static void ReadEmpty()
        {
            SimpleTestClass obj = JsonSerializer.Parse<SimpleTestClass>("{}");
            Assert.NotNull(obj);
        }

        [Fact]
        public static void EmptyClassWithRandomData()
        {
            JsonSerializer.Parse<EmptyClass>(SimpleTestClass.s_json);
            JsonSerializer.Parse<EmptyClass>(SimpleTestClassWithNulls.s_json);
        }

        [Fact]
        public static void ReadObjectFail()
        {
            Assert.Throws<JsonReaderException>(() => JsonSerializer.Parse<SimpleTestClass>("blah"));
            Assert.Throws<JsonReaderException>(() => JsonSerializer.Parse<object>("blah"));

            Assert.Throws<JsonReaderException>(() => JsonSerializer.Parse<SimpleTestClass>("true"));

            Assert.Throws<JsonReaderException>(() => JsonSerializer.Parse<SimpleTestClass>("null."));
            Assert.Throws<JsonReaderException>(() => JsonSerializer.Parse<object>("null."));
        }

        [Fact]
        public static void ParseUntyped()
        {
            // Not supported until we are able to deserialize into JsonElement.
            Assert.Throws<JsonReaderException>(() => JsonSerializer.Parse<SimpleTestClass>("[]"));
            Assert.Throws<JsonReaderException>(() => JsonSerializer.Parse<object>("[]"));
        }

        [Fact]
        public static void ReadClassWithStringToPrimitiveDictionary()
        {
            TestClassWithStringToPrimitiveDictionary obj = JsonSerializer.Parse<TestClassWithStringToPrimitiveDictionary>(TestClassWithStringToPrimitiveDictionary.s_data);
            obj.Verify();
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Text.Json.Serialization.Tests
{
    public static partial class NullTests
    {
        [Fact]
        public static void DefaultIgnoreNullValuesOnWrite()
        {
            var obj = new TestClassWithInitializedProperties();
            obj.MyString = null;
            obj.MyInt = null;
            obj.MyIntArray = null;
            obj.MyIntList = null;

            string json = JsonSerializer.ToString(obj);
            Assert.Contains(@"""MyString"":null", json);
            Assert.Contains(@"""MyInt"":null", json);
            Assert.Contains(@"""MyIntArray"":null", json);
            Assert.Contains(@"""MyIntList"":null", json);
        }

        [Fact]
        public static void EnableIgnoreNullValuesOnWrite()
        {
            JsonSerializerOptions options = new JsonSerializerOptions();
            options.IgnoreNullValues = true;

            var obj = new TestClassWithInitializedProperties();
            obj.MyString = null;
            obj.MyInt = null;
            obj.MyIntArray = null;
            obj.MyIntList = null;

            string json = JsonSerializer.ToString(obj, options);
            Assert.Equal(@"{}", json);
        }

        [Fact]
        public static void NullReferences()
        {
            var obj = new ObjectWithObjectProperties();
            obj.Address = null;
            obj.Array = null;
            obj.List = null;
            obj.IEnumerableT = null;
            obj.IListT = null;
            obj.ICollectionT = null;
            obj.IReadOnlyCollectionT = null;
            obj.IReadOnlyListT = null;
            obj.StackT = null;
            obj.QueueT = null;
            obj.HashSetT = null;
            obj.LinkedListT = null;
            obj.SortedSetT = null;
            obj.NullableInt = null;
            obj.NullableIntArray = null;
            obj.Object = null;

            string json = JsonSerializer.ToString(obj);
            Assert.Contains(@"""Address"":null", json);
            Assert.Contains(@"""List"":null", json);
            Assert.Contains(@"""Array"":null", json);
            Assert.Contains(@"""IEnumerableT"":null", json);
            Assert.Contains(@"""IListT"":null", json);
            Assert.Contains(@"""ICollectionT"":null", json);
            Assert.Contains(@"""IReadOnlyCollectionT"":null", json);
            Assert.Contains(@"""IReadOnlyListT"":null", json);
            Assert.Contains(@"""StackT"":null", json);
            Assert.Contains(@"""QueueT"":null", json);
            Assert.Contains(@"""HashSetT"":null", json);
            Assert.Contains(@"""LinkedListT"":null", json);
            Assert.Contains(@"""SortedSetT"":null", json);
            Assert.Contains(@"""NullableInt"":null", json);
            Assert.Contains(@"""Object"":null", json);
            Assert.Contains(@"""NullableIntArray"":null", json);
        }

        [Fact]
        public static void NullArrayElement()
        {
            string json = JsonSerializer.ToString(new ObjectWithObjectProperties[]{ null });
            Assert.Equal("[null]", json);
        }

        [Fact]
        public static void NullArgumentFail()
        {
            Assert.Throws<ArgumentNullException>(() => JsonSerializer.ToString("", (Type)null));
        }

        [Fact]
        public static void NullObjectOutput()
        {
            {
                string output = JsonSerializer.ToString<string>(null);
                Assert.Equal("null", output);
            }

            {
                string output = JsonSerializer.ToString<string>(null, null);
                Assert.Equal("null", output);
            }
        }
    }
}

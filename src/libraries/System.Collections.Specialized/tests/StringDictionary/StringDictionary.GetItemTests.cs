// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionaryItemTests
    {
        [Fact]
        public void Item_Get_IsCaseInsensitive()
        {
            StringDictionary stringDictionary = new StringDictionary();
            stringDictionary.Add("key", "value");
            Assert.Equal("value", stringDictionary["KEY"]);
            Assert.Equal("value", stringDictionary["kEy"]);
            Assert.Equal("value", stringDictionary["key"]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Item_Get_NoSuchKey_ReturnsNull(int count)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);
            Assert.Null(stringDictionary["non-existent-key"]);
        }

        [Fact]
        public void Item_Get_DuplicateValues()
        {
            StringDictionary stringDictionary = new StringDictionary();
            stringDictionary.Add("key1", "value");
            stringDictionary.Add("key2", "different-value");
            stringDictionary.Add("key3", "value");

            Assert.Equal("value", stringDictionary["key1"]);
            Assert.Equal("different-value", stringDictionary["key2"]);
            Assert.Equal("value", stringDictionary["key3"]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Item_Get_NullKey_ThrowsArgumentNullException(int count)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);
            AssertExtensions.Throws<ArgumentNullException>("key", () => stringDictionary[null]);
        }
    }
}

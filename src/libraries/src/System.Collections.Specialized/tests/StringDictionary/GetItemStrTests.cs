// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionaryItemTests
    {
        [Fact]
        public void Item_NoSuchKey_ReturnsNull()
        {
            StringDictionary stringDictionary = new StringDictionary();
            Assert.Null(stringDictionary["non-existent-key"]);
        }

        [Fact]
        public void Item_DuplicateValues()
        {
            StringDictionary stringDictionary = new StringDictionary();
            stringDictionary.Add("key1", "value");
            stringDictionary.Add("key2", "different-value");
            stringDictionary.Add("key3", "value");

            Assert.Equal("value", stringDictionary["key1"]);
            Assert.Equal("different-value", stringDictionary["key2"]);
            Assert.Equal("value", stringDictionary["key3"]);
        }

        [Fact]
        public void Item_NullKey_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("key", () => new StringDictionary()[null]);
        }
    }
}

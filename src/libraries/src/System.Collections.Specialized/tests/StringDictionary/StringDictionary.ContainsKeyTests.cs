// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionaryContainsKeyTests
    {
        [Fact]
        public void ContainsKey_IsCaseInsensitive()
        {
            StringDictionary stringDictionary = new StringDictionary();
            stringDictionary.Add("key", "value");
            Assert.True(stringDictionary.ContainsKey("KEY"));
            Assert.True(stringDictionary.ContainsKey("kEy"));
            Assert.True(stringDictionary.ContainsKey("key"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void ContainsKey_NonExistentKey_ReturnsFalse(int count)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);
            Assert.False(stringDictionary.ContainsKey("key"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void ContainsKey_NullKey_ThrowsArgumentNullException(int count)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);
            AssertExtensions.Throws<ArgumentNullException>("key", () => stringDictionary.ContainsKey(null));
        }
    }
}

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionaryContainsValueTests
    {
        [Fact]
        public void ContainsValue_IsCaseSensitive()
        {
            StringDictionary stringDictionary = new StringDictionary();
            stringDictionary.Add("key", "value");
            Assert.False(stringDictionary.ContainsValue("VALUE"));
            Assert.False(stringDictionary.ContainsValue("vaLue"));
            Assert.True(stringDictionary.ContainsValue("value"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(15)]
        public void ContainsValue_NoSuchValue_ReturnsFalse(int count)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);
            Assert.False(stringDictionary.ContainsValue("value"));
        }

        [Theory]
        [InlineData("value")]
        [InlineData(null)]
        public void ContainsValue_DuplicateValues(string value)
        {
            StringDictionary stringDictionary = new StringDictionary();
            stringDictionary.Add("key1", value);
            stringDictionary.Add("key2", value);
            Assert.True(stringDictionary.ContainsValue(value));
        }
    }
}

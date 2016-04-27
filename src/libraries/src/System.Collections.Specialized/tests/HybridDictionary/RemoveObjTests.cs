// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class HybridDictionaryRemoveTests
    {
        [Theory]
        [InlineData(0, true)]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(5, false)]
        [InlineData(50, true)]
        [InlineData(50, false)]
        public void Remove(int count, bool caseInsensitive)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count, caseInsensitive);
            hybridDictionary.Remove("no-such-object");
            Assert.Equal(count, hybridDictionary.Count);
            Assert.Equal(count, hybridDictionary.Keys.Count);
            Assert.Equal(count, hybridDictionary.Values.Count);

            for (int i = 0; i < hybridDictionary.Count; i++)
            {
                string key = "Key_" + i;
                hybridDictionary.Remove(key);
                Assert.Equal(count - i - 1, hybridDictionary.Count);
                Assert.Equal(count - i - 1, hybridDictionary.Keys.Count);
                Assert.Equal(count - i - 1, hybridDictionary.Values.Count);

                Assert.False(hybridDictionary.Contains(key));
                Assert.Null(hybridDictionary[key]);
            }
        }

        [Theory]
        [InlineData(5, true)]
        [InlineData(5, false)]
        [InlineData(50, true)]
        [InlineData(50, false)]
        public void Remove_CaseSensitivity(int count, bool caseInsensitive)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count, caseInsensitive);
            
            hybridDictionary.Add("key", "value");
            if (caseInsensitive)
            {
                hybridDictionary.Remove("KEY");
                Assert.Equal(count, hybridDictionary.Count);
                Assert.Equal(count, hybridDictionary.Keys.Count);
                Assert.Equal(count, hybridDictionary.Values.Count);
                Assert.False(hybridDictionary.Contains("key"));
                Assert.Null(hybridDictionary["key"]);
            }
            else
            {
                hybridDictionary.Remove("KEY");
                Assert.Equal(count + 1, hybridDictionary.Count);
                Assert.Equal(count + 1, hybridDictionary.Keys.Count);
                Assert.Equal(count + 1, hybridDictionary.Values.Count);
                Assert.True(hybridDictionary.Contains("key"));
                Assert.Equal("value", hybridDictionary["key"]);
            }
        }
    }
}

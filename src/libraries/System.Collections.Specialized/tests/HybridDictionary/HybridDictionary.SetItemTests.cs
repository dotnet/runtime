// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class HybridDictionarySetItemTests
    {
        [Theory]
        [InlineData(0, true)]
        [InlineData(0, false)]
        [InlineData(5, true)]
        [InlineData(5, false)]
        [InlineData(50, true)]
        [InlineData(50, false)]
        public void Item_Set(int count, bool caseInsensitive)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count, caseInsensitive);

            for (int i = 0; i < count; i++)
            {
                string key = "Key_" + i;
                hybridDictionary[key] = "new-value";
                Assert.Equal("new-value", hybridDictionary[key]);
                Assert.Equal(count, hybridDictionary.Count);
            }

            int additionalCount = 10;
            for (int i = 0; i < additionalCount; i++)
            {
                string newKey = "new-key-" + i;
                hybridDictionary[newKey] = "value";
                Assert.Equal("value", hybridDictionary[newKey]);
                Assert.True(hybridDictionary.Contains(newKey));
                Assert.Equal(count + i + 1, hybridDictionary.Count);
            }

            string nullValueKey = "null-value-key";
            hybridDictionary[nullValueKey] = null;
            Assert.Null(hybridDictionary[nullValueKey]);
            Assert.True(hybridDictionary.Contains(nullValueKey));
            Assert.Equal(count + additionalCount + 1, hybridDictionary.Count);
        }

        [Theory]
        [InlineData(5, true)]
        [InlineData(5, false)]
        [InlineData(50, true)]
        [InlineData(50, false)]
        public void Item_Set_CaseSensitivity(int count, bool caseInsensitive)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count, caseInsensitive);
            hybridDictionary.Add("key", "value");
            if (caseInsensitive)
            {
                hybridDictionary["KEY"] = "new-value";
                Assert.Equal(count + 1, hybridDictionary.Count);
                Assert.Equal(count + 1, hybridDictionary.Keys.Count);
                Assert.Equal(count + 1, hybridDictionary.Values.Count);
                Assert.Equal("new-value", hybridDictionary["key"]);
                Assert.Equal("new-value", hybridDictionary["KEY"]);
            }
            else
            {
                hybridDictionary["KEY"] = "new-value";
                Assert.Equal(count + 2, hybridDictionary.Count);
                Assert.Equal(count + 2, hybridDictionary.Keys.Count);
                Assert.Equal(count + 2, hybridDictionary.Values.Count);
                Assert.Equal("value", hybridDictionary["key"]);
                Assert.Equal("new-value", hybridDictionary["KEY"]);
            }
        }
    }
}

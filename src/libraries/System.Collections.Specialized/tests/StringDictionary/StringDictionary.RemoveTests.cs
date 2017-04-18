// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionaryRemoveTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Remove(int count)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);
            stringDictionary.Remove("non-existent-key");
            Assert.Equal(count, stringDictionary.Count);

            for (int i = 0; i < count; i++)
            {
                string key = "Key_" + i;
                if (i == 0)
                {
                    stringDictionary.Remove(key.ToUpperInvariant());
                }
                else if (i == 1)
                {
                    stringDictionary.Remove(key.ToLowerInvariant());
                }
                else
                {
                    stringDictionary.Remove(key);
                }
                Assert.False(stringDictionary.ContainsKey(key));
                Assert.False(stringDictionary.ContainsValue("Value_" + i));

                Assert.Equal(count - i - 1, stringDictionary.Count);
            }
        }
        
        [Fact]
        public void Remove_DuplicateValues()
        {
            StringDictionary stringDictionary = new StringDictionary();
            stringDictionary.Add("key1", "value");
            stringDictionary.Add("key2", "value");

            stringDictionary.Remove("key1");
            Assert.Equal(1, stringDictionary.Count);
            Assert.False(stringDictionary.ContainsKey("key1"));
            Assert.True(stringDictionary.ContainsValue("value"));

            stringDictionary.Remove("key2");
            Assert.Equal(0, stringDictionary.Count);
            Assert.False(stringDictionary.ContainsKey("key2"));
            Assert.False(stringDictionary.ContainsValue("value"));
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Remove_NullKey_ThrowsArgumentNullException(int count)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);
            AssertExtensions.Throws<ArgumentNullException>("key", () => stringDictionary.Remove(null));
        }
    }
}

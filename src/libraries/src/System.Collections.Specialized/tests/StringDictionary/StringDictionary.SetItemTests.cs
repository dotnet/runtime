// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionaryItemSetTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Item_Set(int count)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);

            for (int i = 0; i < stringDictionary.Count; i++)
            {
                string key = "Key_" + i;
                string value = "Value" + i * 2;
                if (i == 0)
                {
                    stringDictionary[key.ToUpperInvariant()] = value.ToUpperInvariant();
                    Assert.False(stringDictionary.ContainsValue(value));
                    Assert.True(stringDictionary.ContainsValue(value.ToUpperInvariant()));
                    Assert.False(stringDictionary.ContainsValue(value.ToLowerInvariant()));
                }
                else if (i == 1)
                {
                    stringDictionary[key.ToLowerInvariant()] = value.ToLowerInvariant();
                    Assert.False(stringDictionary.ContainsValue(value));
                    Assert.False(stringDictionary.ContainsValue(value.ToUpperInvariant()));
                    Assert.True(stringDictionary.ContainsValue(value.ToLowerInvariant()));
                }
                else
                {
                    stringDictionary[key] = value;
                    Assert.True(stringDictionary.ContainsValue(value));
                    Assert.False(stringDictionary.ContainsValue(value.ToUpperInvariant()));
                    Assert.False(stringDictionary.ContainsValue(value.ToLowerInvariant()));
                }
                Assert.True(stringDictionary.ContainsKey(key));
                Assert.Equal(count, stringDictionary.Count);
            }

            stringDictionary["new-key"] = "new-value";
            Assert.Equal(count + 1, stringDictionary.Count);
            Assert.True(stringDictionary.ContainsKey("new-key"));
            Assert.True(stringDictionary.ContainsValue("new-value"));
            Assert.Equal("new-value", stringDictionary["new-key"]);

            stringDictionary["new-null-key"] = null;
            Assert.Equal(count + 2, stringDictionary.Count);
            Assert.True(stringDictionary.ContainsKey("new-null-key"));
            Assert.True(stringDictionary.ContainsValue(null));
            Assert.Null(stringDictionary["new-null-key"]);
        }

        [Fact]
        public void Item_Set_IsCaseInsensitive()
        {
            StringDictionary stringDictionary = new StringDictionary();
            stringDictionary["KEY"] = "value1";
            stringDictionary["kEy"] = "value2";
            stringDictionary["key"] = "value3";

            Assert.Equal(1, stringDictionary.Count);
            Assert.Equal("value3", stringDictionary["key"]);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(5)]
        public void Item_Set_NullKey_ThrowsArgumentNullException(int count)
        {
            StringDictionary stringDictionary = Helpers.CreateStringDictionary(count);
            AssertExtensions.Throws<ArgumentNullException>("key", () => stringDictionary[null] = "value");
        }
    }
}

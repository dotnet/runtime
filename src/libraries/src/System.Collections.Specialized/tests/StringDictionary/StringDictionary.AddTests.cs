// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class StringDictionaryAddTests
    {
        [Fact]
        public void Add()
        {
            int count = 10;
            StringDictionary stringDictionary = new StringDictionary();
            for (int i = 0; i < count; i++)
            {
                string key = "Key_" + i;
                string value = "Value_" + i;

                stringDictionary.Add(key, value);
                Assert.Equal(i + 1, stringDictionary.Count);

                Assert.True(stringDictionary.ContainsKey(key));
                Assert.True(stringDictionary.ContainsValue(value));
                Assert.Equal(value, stringDictionary[key]);
            }

            Assert.False(stringDictionary.ContainsValue(null));

            stringDictionary.Add("nullkey", null);
            Assert.Equal(count + 1, stringDictionary.Count);
            Assert.True(stringDictionary.ContainsKey("nullkey"));
            Assert.True(stringDictionary.ContainsValue(null));
            Assert.Null(stringDictionary["nullkey"]);
        }

        [Fact]
        public void Add_Invalid()
        {
            StringDictionary stringDictionary = new StringDictionary();
            stringDictionary.Add("Key", "Value");

            AssertExtensions.Throws<ArgumentNullException>("key", () => stringDictionary.Add(null, "value"));

            // Duplicate key
            AssertExtensions.Throws<ArgumentException>(null, () => stringDictionary.Add("Key", "value"));
            AssertExtensions.Throws<ArgumentException>(null, () => stringDictionary.Add("KEY", "value"));
            AssertExtensions.Throws<ArgumentException>(null, () => stringDictionary.Add("key", "value"));
        }
    }
}

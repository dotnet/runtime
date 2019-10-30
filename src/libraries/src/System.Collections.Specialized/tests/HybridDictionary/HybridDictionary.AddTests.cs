// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Collections.Specialized.Tests
{
    public class HybridDictionaryAddTests
    {
        [Theory]
        [InlineData(50, false)]
        [InlineData(50, true)]
        public void Add(int count, bool caseInsensitive)
        {
            HybridDictionary hybridDictionary = new HybridDictionary(caseInsensitive);

            for (int i = 0; i < count; i++)
            {
                string key = "Key_" + i;
                string value = "Value_" + i;
                hybridDictionary.Add(key, value);

                Assert.Equal(i + 1, hybridDictionary.Count);
                Assert.Equal(i + 1, hybridDictionary.Keys.Count);
                Assert.Equal(i + 1, hybridDictionary.Values.Count);

                Assert.Contains(key, hybridDictionary.Keys.Cast<object>());
                Assert.Contains(value, hybridDictionary.Values.Cast<object>());

                Assert.Equal(value, hybridDictionary[key]);
                Assert.True(hybridDictionary.Contains(key));

                // Keys should be case insensitive
                Assert.Equal(caseInsensitive, hybridDictionary.Contains(key.ToUpperInvariant()));
                Assert.Equal(caseInsensitive, hybridDictionary.Contains(key.ToLowerInvariant()));

                if (caseInsensitive)
                {
                    Assert.Equal(value, hybridDictionary[key.ToUpperInvariant()]);
                    Assert.Equal(value, hybridDictionary[key.ToLowerInvariant()]);
                }
                else
                {
                    Assert.Null(hybridDictionary[key.ToUpperInvariant()]);
                    Assert.Null(hybridDictionary[key.ToLowerInvariant()]);
                }
            }
        }

        [Theory]
        [InlineData(5)]
        [InlineData(50)]
        public void Add_NullValue(int count)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count);
            string nullValueKey = "null-value-key";
            hybridDictionary.Add(nullValueKey, null);

            Assert.Equal(count + 1, hybridDictionary.Count);
            Assert.Null(hybridDictionary[nullValueKey]);
            Assert.True(hybridDictionary.Contains(nullValueKey));
            Assert.Contains(null, hybridDictionary.Values.Cast<object>());
        }

        [Theory]
        [InlineData(5)]
        [InlineData(50)]
        public void Add_SameValue(int count)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count);
            hybridDictionary.Add("key1", "value");
            hybridDictionary.Add("key2", "value");

            Assert.Equal(count + 2, hybridDictionary.Count);
            Assert.Equal(count + 2, hybridDictionary.Values.Count);
        }

        [Theory]
        [InlineData(5, true)]
        [InlineData(5, false)]
        [InlineData(50, true)]
        [InlineData(50, false)]
        public void Add_Invalid(int count, bool caseInsensitive)
        {
            HybridDictionary hybridDictionary = Helpers.CreateHybridDictionary(count, caseInsensitive);
            AssertExtensions.Throws<ArgumentNullException>("key", () => hybridDictionary.Add(null, "value"));

            hybridDictionary.Add("key", "value");
            AssertExtensions.Throws<ArgumentException>(null, () => hybridDictionary.Add("key", "value"));

            if (caseInsensitive)
            {
                AssertExtensions.Throws<ArgumentException>(null, () => hybridDictionary.Add("KEY", "value"));
            }
            else
            {
                hybridDictionary.Add("KEY", "value");
                Assert.True(hybridDictionary.Contains("KEY"));
            }
        }
    }
}

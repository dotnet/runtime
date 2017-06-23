// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Tests;
using System.Linq;
using Xunit;

namespace System.Collections.Specialized.Tests
{
    public static class ListDictionaryTests
    {
        /// <summary>
        /// Data used for testing with a set of collections.
        /// </summary>
        /// Format is:
        ///  1. Dictionary
        ///  2. internal data
        /// <returns>Row of data</returns>
        public static IEnumerable<object[]> ListDictionary_Data()
        {
            yield return new object[] { new ListDictionary(), new KeyValuePair<string, string>[0] };
            yield return new object[] { Fill(new ListDictionary(), s_dictionaryData), s_dictionaryData };
            yield return new object[] { Fill(new ListDictionary(StringComparer.Ordinal), s_dictionaryData), s_dictionaryData };
        }

        private static ListDictionary Fill(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            foreach (KeyValuePair<string, string> d in data)
            {
                ld.Add(d.Key, d.Value);
            }
            return ld;
        }

        private const string Key = "key";

        private static readonly KeyValuePair<string, string>[] s_dictionaryData = new[] {
            new KeyValuePair<string, string>(Key, Key+"-value"),
            new KeyValuePair<string, string>(Key.Replace('k', 'K'), Key.Replace('k', 'K')+"-value"),
            new KeyValuePair<string, string>(Key.Replace('e', 'E'), Key.Replace('e', 'E')+"-value"),
            new KeyValuePair<string, string>(Key.Replace('y', 'Y'), Key.Replace('y', 'Y')+"-value"),
            new KeyValuePair<string, string>(Key.ToUpper(), Key.ToUpper()+"-value"),
            new KeyValuePair<string, string>("otherkey", "otherkey-value"),
            new KeyValuePair<string, string>("nullvaluekey", null),
        };

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void AddTest(bool addViaSet)
        {
            ListDictionary added = new ListDictionary();
            IList keys = new ArrayList();
            IList values = new ArrayList();
            for (int i = 0; i < s_dictionaryData.Length; i++)
            {
                string key = s_dictionaryData[i].Key;
                string value = s_dictionaryData[i].Value;
                Assert.Equal(i, added.Count);
                Assert.Null(added[key]);
                Assert.False(added.Contains(key));
                added.Add(addViaSet, key, value);
                keys.Add(key);
                values.Add(value);
                Assert.Equal(value, added[key]);
                Assert.True(added.Contains(key));
                Assert.Equal(i + 1, added.Count);
                CollectionAsserts.Equal(keys, added.Keys);
                CollectionAsserts.Equal(values, added.Values);
                AssertExtensions.Throws<ArgumentException>(null, () => added.Add(key, value));
                added[key] = value;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void Add_CustomComparerTest(bool addViaSet)
        {
            ObservableStringComparer comparer = new ObservableStringComparer();
            ListDictionary added = new ListDictionary(comparer);

            for (int i = 0; i < s_dictionaryData.Length; i++)
            {
                string key = s_dictionaryData[i].Key;
                string value = s_dictionaryData[i].Value;

                Assert.Equal(i, added.Count);
                comparer.AssertCompared(i, () => Assert.Null(added[key]));
                comparer.AssertCompared(i, () => Assert.False(added.Contains(key)));

                // comparer is called for each element in sequence during add.
                comparer.AssertCompared(i, () => added.Add(addViaSet, key, value));
                comparer.AssertCompared(i + 1, () => Assert.Equal(value, added[key]));
                comparer.AssertCompared(i + 1, () => Assert.True(added.Contains(key)));
                Assert.Equal(i + 1, added.Count);
                comparer.AssertCompared(i + 1, () => AssertExtensions.Throws<ArgumentException>(null, () => added.Add(key, "duplicate")));
            }
            Assert.Equal(s_dictionaryData.Length, added.Count);

            // Because the dictionary maintains insertion order, "add"-ing an already-present element only iterates
            // until the initial key is reached.
            int middleIndex = s_dictionaryData.Length / 2;
            string middleKey = s_dictionaryData[middleIndex].Key;
            string middleValue = s_dictionaryData[middleIndex].Value;

            Assert.Equal(middleValue, added[middleKey]);
            Assert.True(added.Contains(middleKey));
            // Index is 0-based, count is 1-based
            //  ... Add throws exception
            comparer.AssertCompared(middleIndex + 1, () => AssertExtensions.Throws<ArgumentException>(null, () => added.Add(middleKey, "middleValue")));
            Assert.Equal(middleValue, added[middleKey]);
            Assert.True(added.Contains(middleKey));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void Add_MultipleTypesTest(bool addViaSet)
        {
            ListDictionary added = new ListDictionary();
            added.Add(addViaSet, "key", "value");
            added.Add(addViaSet, "key".GetHashCode(), "hashcode".GetHashCode());

            Assert.True(added.Contains("key"));
            Assert.True(added.Contains("key".GetHashCode()));
            Assert.Equal("value", added["key"]);
            Assert.Equal("hashcode".GetHashCode(), added["key".GetHashCode()]);
        }

        [Fact]
        public static void Set_CustomComparerTest()
        {
            ObservableStringComparer comparer = new ObservableStringComparer();
            ListDictionary ld = Fill(new ListDictionary(comparer), s_dictionaryData);

            int visited = s_dictionaryData.Length;

            Assert.All(s_dictionaryData.Reverse(), element =>
            {
                string newValue = "new" + element.Value;
                Assert.True(ld.Contains(element.Key));
                // Removing items in reverse order, so iterates over everything ahead of the key.
                comparer.AssertCompared(visited--, () => ld[element.Key] = newValue);
                Assert.True(ld.Contains(element.Key));
                Assert.Equal(newValue, ld[element.Key]);
            });
        }

        [Fact]
        public static void Remove_CustomComparerTest()
        {
            ObservableStringComparer comparer = new ObservableStringComparer();
            ListDictionary ld = Fill(new ListDictionary(comparer), s_dictionaryData);

            Assert.All(s_dictionaryData.Reverse(), element =>
            {
                int originalSize = ld.Count;
                Assert.True(ld.Contains(element.Key));
                // Removing items in reverse order, so iterates over everything.
                comparer.AssertCompared(originalSize, () => ld.Remove(element.Key));
                Assert.False(ld.Contains(element.Key));
            });
            Assert.Equal(0, ld.Count);
        }
        
        private static void Add(this ListDictionary ld, bool addViaSet, object key, object value)
        {
            if (addViaSet) ld[key] = value;
            else ld.Add(key, value);
        }

        private class ObservableStringComparer : IComparer
        {
            private int _compared = 0;

            public void AssertCompared(int expected, Action action)
            {
                // Reset before compare
                _compared = 0;
                action();
                Assert.Equal(expected, _compared);
            }

            public int Compare(object x, object y)
            {
                _compared++;

                return StringComparer.Ordinal.Compare((string)x, (string)y);
            }
        }
    }
}

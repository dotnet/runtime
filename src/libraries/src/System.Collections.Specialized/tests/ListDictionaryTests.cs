// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

        [Fact]
        public static void Constructor_DefaultTests()
        {
            ListDictionary ld = new ListDictionary();
            Assert.Equal(0, ld.Count);
            Assert.False(ld.IsReadOnly);
            Assert.Empty(ld);
            Assert.Empty(ld.Keys);
            Assert.Empty(ld.Values);
            Assert.False(ld.Contains(new object()));
            Assert.Null(ld[new object()]);
        }

        [Fact]
        public static void Constructor_ComparerTests()
        {
            ListDictionary ld = new ListDictionary(StringComparer.OrdinalIgnoreCase);
            Assert.NotNull(ld);
            Assert.Equal(0, ld.Count);
            Assert.False(ld.IsReadOnly);
            Assert.Empty(ld);
            Assert.Empty(ld.Keys);
            Assert.Empty(ld.Values);
            Assert.False(ld.Contains(new object()));
            Assert.Null(ld[new object()]);

            ld.Add("key", "value");
            Assert.True(ld.Contains("key"));
            Assert.True(ld.Contains("KEY"));
            Assert.Equal("value", ld["key"]);
            Assert.Equal("value", ld["KEY"]);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void AddTest(bool addViaSet)
        {
            ListDictionary added = new ListDictionary();
            IList keys = new ArrayList();
            IList values = new ArrayList();
            Assert.Empty(added.Keys);
            Assert.Empty(added.Values);
            for (int i = 0; i < s_dictionaryData.Length; i++)
            {
                string key = s_dictionaryData[i].Key;
                string value = s_dictionaryData[i].Value;
                Assert.Equal(i, added.Count);
                Assert.Null(added[key]);
                Assert.False(added.Contains(key));
                CollectionAsserts.Equal(keys, added.Keys);
                CollectionAsserts.Equal(values, added.Values);
                if (addViaSet)
                {
                    added[key] = value;
                }
                else
                {
                    added.Add(key, value);
                }
                keys.Add(key);
                values.Add(value);
                Assert.Equal(value, added[key]);
                Assert.True(added.Contains(key));
                Assert.Equal(i + 1, added.Count);
                CollectionAsserts.Equal(keys, added.Keys);
                CollectionAsserts.Equal(values, added.Values);
                Assert.Throws<ArgumentException>(() => added.Add(key, value));
                added[key] = value;
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void Add_CustomComparatorTest(bool addViaSet)
        {
            ListDictionary added = new ListDictionary(StringComparer.OrdinalIgnoreCase);
            int duplicateKeys = 0;
            for (int i = 0; i < s_dictionaryData.Length; i++)
            {
                string key = s_dictionaryData[i].Key;
                string value = s_dictionaryData[i].Value;
                if (key == Key)
                {
                    Assert.Equal(i, added.Count);
                    Assert.Null(added[key]);
                    Assert.False(added.Contains(key));
                    if (addViaSet)
                    {
                        added[key] = value;
                    }
                    else
                    {
                        added.Add(key, value);
                    }
                    Assert.Equal(value, added[key]);
                    Assert.True(added.Contains(key));
                    Assert.Equal(i + 1, added.Count);
                }
                else if (StringComparer.OrdinalIgnoreCase.Equals(key, Key))
                {
                    Assert.Equal(1, added.Count);
                    Assert.Equal("key-value", added[key]);
                    Assert.True(added.Contains(key));
                    Assert.Throws<ArgumentException>(() => added.Add(key, value));
                    duplicateKeys++;
                }
                else
                {
                    Assert.Equal(i - duplicateKeys, added.Count);
                    Assert.Null(added[key]);
                    Assert.False(added.Contains(key));
                    if (addViaSet)
                    {
                        added[key] = value;
                    }
                    else
                    {
                        added.Add(key, value);
                    }
                    Assert.Equal(i - duplicateKeys + 1, added.Count);
                    Assert.Equal(value, added[key]);
                    Assert.True(added.Contains(key));
                }
            }
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public static void Add_MultipleTypesTest(bool addViaSet)
        {
            ListDictionary added = new ListDictionary();
            if (addViaSet)
            {
                added["key"] = "value";
                added["key".GetHashCode()] = "hashcode".GetHashCode();
            }
            else
            {
                added.Add("key", "value");
                added.Add("key".GetHashCode(), "hashcode".GetHashCode());
            }

            Assert.True(added.Contains("key"));
            Assert.True(added.Contains("key".GetHashCode()));
            Assert.Equal("value", added["key"]);
            Assert.Equal("hashcode".GetHashCode(), added["key".GetHashCode()]);
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void Add_NullKeyTest(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            Assert.Throws<ArgumentNullException>("key", () => ld.Add(null, "value"));
            Assert.Throws<ArgumentNullException>("key", () => ld[null] = "value");
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void ClearTest(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            Assert.Equal(data.Length, ld.Count);
            ld.Clear();
            Assert.Equal(0, ld.Count);
            ld.Add("key", "value");
            Assert.Equal(1, ld.Count);
            ld.Clear();
            Assert.Equal(0, ld.Count);
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void Contains_NullTest(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            Assert.Throws<ArgumentNullException>("key", () => ld.Contains(null));
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void CopyToTest(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            DictionaryEntry[] translated = data.Select(kv => new DictionaryEntry(kv.Key, kv.Value)).ToArray();

            DictionaryEntry[] full = new DictionaryEntry[data.Length];
            ld.CopyTo(full, 0);
            Assert.Equal(translated, full);

            DictionaryEntry[] large = new DictionaryEntry[data.Length * 2];
            ld.CopyTo(large, data.Length / 2);
            for (int i = 0; i < large.Length; i++)
            {
                if (i < data.Length / 2 || i >= data.Length + data.Length / 2)
                {
                    Assert.Equal(new DictionaryEntry(), large[i]);
                    Assert.Null(large[i].Key);
                    Assert.Null(large[i].Value);
                }
                else
                {
                    Assert.Equal(translated[i - data.Length / 2], large[i]);
                }
            }
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void CopyTo_ArgumentInvalidTest(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            Assert.Throws<ArgumentNullException>("array", () => ld.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>("index", () => ld.CopyTo(data, -1));
            if (data.Length > 0)
            {
                Assert.Throws<ArgumentException>(() => ld.CopyTo(new KeyValuePair<string, string>[1, data.Length], 0));
                Assert.Throws<ArgumentException>(() => ld.CopyTo(new KeyValuePair<string, string>[0], data.Length - 1));
                Assert.Throws<ArgumentException>(() => ld.CopyTo(new KeyValuePair<string, string>[data.Length - 1], 0));
                Assert.Throws<InvalidCastException>(() => ld.CopyTo(new int[data.Length], 0));
            }
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void GetSetTest(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            DictionaryEntry[] translated = data.Select(kv => new DictionaryEntry(kv.Key, kv.Value)).ToArray();

            foreach (KeyValuePair<string, string> kv in data)
            {
                Assert.Equal(kv.Value, ld[kv.Key]);
            }
            for (int i = 0; i < data.Length / 2; i++)
            {
                object temp = ld[data[i].Key];
                ld[data[i].Key] = ld[data[data.Length - i - 1].Key];
                ld[data[data.Length - i - 1].Key] = temp;
            }
            for (int i = 0; i < data.Length; i++)
            {
                Assert.Equal(data[data.Length - i - 1].Value, ld[data[i].Key]);
            }
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void GetSet_ArgumentInvalidTest(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            Assert.Throws<ArgumentNullException>("key", () => ld[(string)null] = "notpresent");
            Assert.Throws<ArgumentNullException>("key", () => ld[(string)null]);
        }

        [Fact]
        public static void IsFixedSizeTest()
        {
            Assert.False(new ListDictionary().IsFixedSize);
        }

        [Fact]
        public static void IsReadOnlyTest()
        {
            Assert.False(new ListDictionary().IsReadOnly);
        }

        [Fact]
        public static void IsSynchronizedTest()
        {
            Assert.False(new ListDictionary().IsSynchronized);
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void GetEnumeratorTest(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            bool repeat = true;
            IDictionaryEnumerator enumerator = ld.GetEnumerator();
            Assert.NotNull(enumerator);
            while (repeat)
            {
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                foreach (KeyValuePair<string, string> element in data)
                {
                    Assert.True(enumerator.MoveNext());
                    DictionaryEntry entry = (DictionaryEntry)enumerator.Current;
                    Assert.Equal(entry, enumerator.Current);
                    Assert.Equal(element.Key, entry.Key);
                    Assert.Equal(element.Value, entry.Value);
                    Assert.Equal(element.Key, enumerator.Key);
                    Assert.Equal(element.Value, enumerator.Value);
                }
                Assert.False(enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                Assert.False(enumerator.MoveNext());

                enumerator.Reset();
                enumerator.Reset();
                repeat = false;
            }
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void GetEnumerator_ModifiedCollectionTest(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            IDictionaryEnumerator enumerator = ld.GetEnumerator();
            Assert.NotNull(enumerator);
            if (data.Length > 0)
            {
                Assert.True(enumerator.MoveNext());
                DictionaryEntry current = (DictionaryEntry)enumerator.Current;
                ld.Remove("key");
                Assert.Equal(current, enumerator.Current);
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
            }
            else
            {
                ld.Add("newKey", "newValue");
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            }
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void RemoveTest(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            Assert.All(data, element =>
            {
                Assert.True(ld.Contains(element.Key));
                ld.Remove(element.Key);
                Assert.False(ld.Contains(element.Key));
            });
            Assert.Equal(0, ld.Count);
        }

        [Fact]
        public static void Remove_CustomComparatorTest()
        {
            KeyValuePair<string, string>[] data = new[] {
                new KeyValuePair<string, string>("key", "value"),
                new KeyValuePair<string, string>("otherkey", "othervalue"),
            };
            ListDictionary ld = Fill(new ListDictionary(StringComparer.OrdinalIgnoreCase), data);

            Assert.All(data, element =>
            {
                Assert.True(ld.Contains(element.Key.ToUpper()));
                ld.Remove(element.Key.ToUpper());
                Assert.False(ld.Contains(element.Key.ToUpper()));
            });
            Assert.Equal(0, ld.Count);
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void Remove_NotPresentTest(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            ld.Remove("notpresent");
            Assert.Equal(data.Length, ld.Count);
            Assert.Throws<ArgumentNullException>("key", () => ld.Remove(null));
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void SyncRootTest(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            object syncRoot = ld.SyncRoot;
            Assert.NotNull(syncRoot);
            Assert.IsType<object>(syncRoot);

            Assert.Same(syncRoot, ld.SyncRoot);
            Assert.NotSame(syncRoot, new ListDictionary().SyncRoot);
            ListDictionary other = new ListDictionary();
            other.Add("key", "value");
            Assert.NotSame(syncRoot, other.SyncRoot);
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void KeyCollection_CopyTo_NullArgument_Test(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            ICollection keys = ld.Keys;
            Assert.Throws<ArgumentNullException>("array", () => keys.CopyTo(null, 0));
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void KeyCollection_CopyTo_InvalidArgument_Test(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            ICollection keys = ld.Keys;
            Assert.Throws<ArgumentOutOfRangeException>("index", () => keys.CopyTo(new object[] { }, -1));
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void KeyCollection_SyncRoot_Test(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            ICollection keys = ld.Keys;
            Assert.Same(ld.SyncRoot, keys.SyncRoot);
        }

        [Fact]
        public static void KeyCollection_Synchronized_Test()
        {
            ICollection keys = new ListDictionary().Keys;
            Assert.False(keys.IsSynchronized);
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void KeyCollection_GetEnumeratorTest(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            bool repeat = true;
            IEnumerator enumerator = ld.Keys.GetEnumerator();
            Assert.NotNull(enumerator);
            while (repeat)
            {
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                foreach (KeyValuePair<string, string> element in data)
                {
                    Assert.True(enumerator.MoveNext());
                    Assert.NotNull(enumerator.Current);
                }
                Assert.False(enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Current);
                Assert.False(enumerator.MoveNext());

                enumerator.Reset();
                enumerator.Reset();
                repeat = false;
            }
        }

        [Theory]
        [MemberData("ListDictionary_Data")]
        public static void KeyCollection_GetEnumerator_ModifiedCollectionTest(ListDictionary ld, KeyValuePair<string, string>[] data)
        {
            IEnumerator enumerator = ld.Keys.GetEnumerator();
            Assert.NotNull(enumerator);
            if (data.Length > 0)
            {
                Assert.True(enumerator.MoveNext());
                object current = enumerator.Current;
                ld.Remove("key");
                Assert.Equal(current, enumerator.Current);
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
                Assert.Throws<InvalidOperationException>(() => enumerator.Reset());
            }
            else
            {
                ld.Add("newKey", "newValue");
                Assert.Throws<InvalidOperationException>(() => enumerator.MoveNext());
            }
        }
    }
}

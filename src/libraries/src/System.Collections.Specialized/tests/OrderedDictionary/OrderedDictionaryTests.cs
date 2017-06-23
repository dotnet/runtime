// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Collections;
using System.Collections.Specialized;

namespace System.Collections.Specialized.Tests
{
    public class OrderedDictionaryTests
    {
        // public OrderedDictionary();
        [Fact]
        public void DefaultConstructorDoesNotThrow()
        {
            OrderedDictionary dictionary = new OrderedDictionary();
        }

        // public OrderedDictionary(int capacity);
        [Fact]
        public void CreatingWithDifferentCapacityValues()
        {
            // exceptions are not thrown until you add an element
            var d1 = new OrderedDictionary(-1000);
            var d2 = new OrderedDictionary(-1);
            var d3 = new OrderedDictionary(0);
            var d4 = new OrderedDictionary(1);
            var d5 = new OrderedDictionary(1000);
            Assert.Throws<ArgumentOutOfRangeException>(() => d1.Add("foo", "bar"));
            Assert.Throws<ArgumentOutOfRangeException>(() => d2.Add("foo", "bar"));
            d3.Add("foo", "bar");
            d4.Add("foo", "bar");
            d5.Add("foo", "bar");
        }

        // public OrderedDictionary(IEqualityComparer comparer);
        [Fact]
        public void PassingEqualityComparers()
        {
            var eqComp = new CaseInsensitiveEqualityComparer();
            var d1 = new OrderedDictionary(eqComp);
            d1.Add("foo", "bar");
            AssertExtensions.Throws<ArgumentException>(null, () => d1.Add("FOO", "bar"));

            // The equality comparer should also test for a non-existent key 
            d1.Remove("foofoo");
            Assert.True(d1.Contains("foo"));

            // Make sure we can change an existent key that passes the equality comparer
            d1["FOO"] = "barbar";
            Assert.Equal("barbar", d1["foo"]);

            d1.Remove("FOO");
            Assert.False(d1.Contains("foo"));
        }

        // public OrderedDictionary(int capacity, IEqualityComparer comparer);
        [Fact]
        public void PassingCapacityAndIEqualityComparer()
        {
            var eqComp = new CaseInsensitiveEqualityComparer();
            var d1 = new OrderedDictionary(-1000, eqComp);
            var d2 = new OrderedDictionary(-1, eqComp);
            var d3 = new OrderedDictionary(0, eqComp);
            var d4 = new OrderedDictionary(1, eqComp);
            var d5 = new OrderedDictionary(1000, eqComp);
            Assert.Throws<ArgumentOutOfRangeException>(() => d1.Add("foo", "bar"));
            Assert.Throws<ArgumentOutOfRangeException>(() => d2.Add("foo", "bar"));
            d3.Add("foo", "bar");
            d4.Add("foo", "bar");
            d5.Add("foo", "bar");
        }

        // public int Count { get; }
        [Fact]
        public void CountTests()
        {
            var d = new OrderedDictionary();
            Assert.Equal(0, d.Count);

            for (int i = 0; i < 1000; i++)
            {
                d.Add(i, i);
                Assert.Equal(i + 1, d.Count);
            }

            for (int i = 0; i < 1000; i++)
            {
                d.Remove(i);
                Assert.Equal(1000 - i - 1, d.Count);
            }

            for (int i = 0; i < 1000; i++)
            {
                d[(object)i] = i;
                Assert.Equal(i + 1, d.Count);
            }

            for (int i = 0; i < 1000; i++)
            {
                d.RemoveAt(0);
                Assert.Equal(1000 - i - 1, d.Count);
            }
        }

        // public bool IsReadOnly { get; }
        [Fact]
        public void IsReadOnlyTests()
        {
            var d = new OrderedDictionary();
            Assert.False(d.IsReadOnly);
            var d2 = d.AsReadOnly();
            Assert.True(d2.IsReadOnly);
        }

        [Fact]
        public void AsReadOnly_AttemptingToModifyDictionary_Throws()
        {
            OrderedDictionary orderedDictionary = new OrderedDictionary().AsReadOnly();
            Assert.Throws<NotSupportedException>(() => orderedDictionary[0] = "value");
            Assert.Throws<NotSupportedException>(() => orderedDictionary["key"] = "value");
 
            Assert.Throws<NotSupportedException>(() => orderedDictionary.Add("key", "value"));
            Assert.Throws<NotSupportedException>(() => orderedDictionary.Insert(0, "key", "value"));
 
            Assert.Throws<NotSupportedException>(() => orderedDictionary.Remove("key"));
            Assert.Throws<NotSupportedException>(() => orderedDictionary.RemoveAt(0));
            Assert.Throws<NotSupportedException>(() => orderedDictionary.Clear());
        }

        // public ICollection Keys { get; }
        [Fact]
        public void KeysPropertyContainsAllKeys()
        {
            var d = new OrderedDictionary();
            var alreadyChecked = new bool[1000];
            for (int i = 0; i < 1000; i++)
            {
                d["test_" + i] = i;
                alreadyChecked[i] = false;
            }
            ICollection keys = d.Keys;

            Assert.False(keys.IsSynchronized);
            Assert.NotEqual(d, keys.SyncRoot);
            Assert.Equal(d.Count, keys.Count);

            foreach (var key in d.Keys)
            {
                string skey = (string)key;
                var p = skey.Split(new char[] { '_' });
                Assert.Equal(2, p.Length);
                int number = int.Parse(p[1]);
                Assert.False(alreadyChecked[number]);
                Assert.True(number >= 0 && number < 1000);
                alreadyChecked[number] = true;
            }

            object[] array = new object[keys.Count + 50];
            keys.CopyTo(array, 50);
            for (int i = 50; i < array.Length; i++)
            {
                Assert.True(d.Contains(array[i]));
            }
            
            AssertExtensions.Throws<ArgumentNullException>("array", () => keys.CopyTo(null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => keys.CopyTo(new object[keys.Count], -1));
        }

        // bool System.Collections.ICollection.IsSynchronized { get; }
        [Fact]
        public void IsSynchronizedTests()
        {
            ICollection c = new OrderedDictionary();
            Assert.False(c.IsSynchronized);
        }

        [Fact]
        public void SyncRootTests()
        {
            ICollection orderedDictionary1 = new OrderedDictionary();
            ICollection orderedDictionary2 = new OrderedDictionary();

            object sync1 = orderedDictionary1.SyncRoot;
            object sync2 = orderedDictionary2.SyncRoot;

            // Sync root does not refer to the dictionary
            Assert.NotEqual(sync1, orderedDictionary1);
            Assert.NotEqual(sync2, orderedDictionary2);

            // Sync root objects for the same dictionaries are equivalent
            Assert.Equal(orderedDictionary1.SyncRoot, orderedDictionary1.SyncRoot);
            Assert.Equal(orderedDictionary2.SyncRoot, orderedDictionary2.SyncRoot);

            // Sync root objects for different dictionaries are not equivalent
            Assert.NotEqual(sync1, sync2);
        }

        // bool System.Collections.IDictionary.IsFixedSize { get; }
        [Fact]
        public void IsFixedSizeTests()
        {
            var d = new OrderedDictionary();
            IDictionary dic = d;
            IDictionary rodic = d.AsReadOnly();
            Assert.False(dic.IsFixedSize);
            Assert.True(rodic.IsFixedSize);
        }

        // public object this[int index] { get; set; }
        [Fact]
        public void GettingByIndexTests()
        {
            var d = new OrderedDictionary();
            for (int i = 0; i < 1000; i++)
            {
                d.Add("test" + i, i);
            }

            for (int i = 0; i < 1000; i++)
            {
                Assert.Equal(d[i], i);
                d[i] = (int)d[i] + 100;
                Assert.Equal(d[i], 100 + i);
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => { int foo = (int)d[-1]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { d[-1] = 5; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { int foo = (int)d[1000]; });
            Assert.Throws<ArgumentOutOfRangeException>(() => { d[1000] = 5; });
        }

        // public object this[object key] { get; set; }
        [Fact]
        public void GettingByKeyTests()
        {
            var d = new OrderedDictionary();
            for (int i = 0; i < 1000; i++)
            {
                d.Add("test" + i, i);
            }

            for (int i = 0; i < 1000; i++)
            {
                Assert.Equal(d["test" + i], i);
                d["test" + i] = (int)d["test" + i] + 100;
                Assert.Equal(d["test" + i], 100 + i);
            }

            for (int i = 1000; i < 2000; i++)
            {
                d["test" + i] = 1337;
            }

            Assert.Equal(null, d["asdasd"]);
            Assert.Throws<ArgumentNullException>(() => { var a = d[null]; });
            Assert.Throws<ArgumentNullException>(() => { d[null] = 1337; });
        }

        // public ICollection Values { get; }
        [Fact]
        public void ValuesPropertyContainsAllValues()
        {
            var d = new OrderedDictionary();
            var alreadyChecked = new bool[1000];
            for (int i = 0; i < 1000; i++)
            {
                d["foo" + i] = "bar_" + i;
                alreadyChecked[i] = false;
            }

            ICollection values = d.Values;

            Assert.False(values.IsSynchronized);
            Assert.NotEqual(d, values.SyncRoot);
            Assert.Equal(d.Count, values.Count);

            foreach (var val in values)
            {
                string sval = (string)val;
                var p = sval.Split(new char[] { '_' });
                Assert.Equal(2, p.Length);
                int number = int.Parse(p[1]);
                Assert.False(alreadyChecked[number]);
                Assert.True(number >= 0 && number < 1000);
                alreadyChecked[number] = true;
            }

            object[] array = new object[values.Count + 50];
            values.CopyTo(array, 50);
            for (int i = 50; i < array.Length; i++)
            {
                Assert.Equal(array[i], "bar_" + (i - 50));
            }
            
            AssertExtensions.Throws<ArgumentNullException>("array", () => values.CopyTo(null, 0));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => values.CopyTo(new object[values.Count], -1));
        }

        // public void Add(object key, object value);
        [Fact]
        public void AddTests()
        {
            var d = new OrderedDictionary();
            d.Add((int)5, "foo1");
            Assert.Equal("foo1", d[(object)((int)5)]);

            d.Add((double)5, "foo2");
            Assert.Equal("foo2", d[(object)((double)5)]);

            d.Add((long)5, "foo3");
            Assert.Equal("foo3", d[(object)((long)5)]);

            d.Add((short)5, "foo4");
            Assert.Equal("foo4", d[(object)((short)5)]);

            d.Add((uint)5, "foo5");
            Assert.Equal("foo5", d[(object)((uint)5)]);

            d.Add("5", "foo6");
            Assert.Equal("foo6", d["5"]);

            AssertExtensions.Throws<ArgumentException>(null, () => d.Add((int)5, "foo"));
            AssertExtensions.Throws<ArgumentException>(null, () => d.Add((double)5, "foo"));
            AssertExtensions.Throws<ArgumentException>(null, () => d.Add((long)5, "foo"));
            AssertExtensions.Throws<ArgumentException>(null, () => d.Add((short)5, "foo"));
            AssertExtensions.Throws<ArgumentException>(null, () => d.Add((uint)5, "foo"));
            AssertExtensions.Throws<ArgumentException>(null, () => d.Add("5", "foo"));

            Assert.Throws<ArgumentNullException>(() => d.Add(null, "foobar"));
        }

        // public OrderedDictionary AsReadOnly();
        [Fact]
        public void AsReadOnlyTests()
        {
            var _d = new OrderedDictionary();
            _d["foo"] = "bar";
            _d[(object)13] = 37;

            var d = _d.AsReadOnly();
            Assert.True(d.IsReadOnly);
            Assert.Equal("bar", d["foo"]);
            Assert.Equal(37, d[(object)13]);
            Assert.Throws<NotSupportedException>(() => { d["foo"] = "moooooooooaaah"; });
            Assert.Throws<NotSupportedException>(() => { d["asdasd"] = "moooooooooaaah"; });
            Assert.Equal(null, d["asdasd"]);
            Assert.Throws<ArgumentNullException>(() => { var a = d[null]; });
        }

        // public void Clear();
        [Fact]
        public void ClearTests()
        {
            var d = new OrderedDictionary();
            d.Clear();
            Assert.Equal(0, d.Count);
            for (int i = 0; i < 1000; i++)
            {
                d.Add(i, i);
            }
            d.Clear();
            Assert.Equal(0, d.Count);
            d.Clear();
            Assert.Equal(0, d.Count);
            for (int i = 0; i < 1000; i++)
            {
                d.Add("foo", "bar");
                d.Clear();
                Assert.Equal(0, d.Count);
            }
        }

        // public bool Contains(object key);
        [Fact]
        public void ContainsTests()
        {
            var d = new OrderedDictionary();
            Assert.Throws<ArgumentNullException>(() => d.Contains(null));
            Assert.False(d.Contains("foo"));
            for (int i = 0; i < 1000; i++)
            {
                var k = "test_" + i;
                d.Add(k, "asd");
                Assert.True(d.Contains(k));
                // different reference
                Assert.True(d.Contains("test_" + i));
            }
            Assert.False(d.Contains("foo"));
        }

        // public void CopyTo(Array array, int index);
        [Fact]
        public void CopyToTests()
        {
            var d = new OrderedDictionary();
            d["foo"] = "bar";
            d["   "] = "asd";

            DictionaryEntry[] arr = new DictionaryEntry[3];

            Assert.Throws<ArgumentNullException>(() => d.CopyTo(null, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => d.CopyTo(arr, -1));
            AssertExtensions.Throws<ArgumentException>(null, () => d.CopyTo(arr, 3));

            d.CopyTo(arr, 0);
            for (int i = 0; i < 2; i++)
            {
                Assert.True(d.Contains(arr[i].Key));
                Assert.Equal(d[arr[i].Key], arr[i].Value);
            }
            Assert.NotEqual(arr[0].Key, arr[1].Key);

            d.CopyTo(arr, 1);
            for (int i = 1; i < 3; i++)
            {
                Assert.True(d.Contains(arr[i].Key));
                Assert.Equal(d[arr[i].Key], arr[i].Value);
            }
            Assert.NotEqual(arr[1].Key, arr[2].Key);
        }

        [Fact]
        public void GetDictionaryEnumeratorTests()
        {
            var d = new OrderedDictionary();
            for (int i = 0; i < 10; i++)
            {
                d.Add("Key_" + i, "Value_" + i);
            }

            IDictionaryEnumerator e = d.GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int count = 0;
                while (e.MoveNext())
                {
                    DictionaryEntry entry1 = (DictionaryEntry)e.Current;
                    DictionaryEntry entry2 = e.Entry;

                    Assert.Equal(entry1.Key, entry2.Key);
                    Assert.Equal(entry1.Value, entry1.Value);

                    Assert.Equal(e.Key, entry1.Key);
                    Assert.Equal(e.Value, entry1.Value);

                    Assert.Equal(e.Value, d[e.Key]);
                    count++;
                }
                Assert.Equal(count, d.Count);
                Assert.False(e.MoveNext());
                e.Reset();
            }

            e = d.GetEnumerator();
            d["foo"] = "bar";
            Assert.Throws<InvalidOperationException>(() => e.MoveNext());
        }
        
        [Fact]
        public void GetEnumeratorTests()
        {
            var d = new OrderedDictionary();
            for (int i = 0; i < 10; i++)
            {
                d.Add("Key_" + i, "Value_" + i);
            }

            IEnumerator e = ((ICollection)d).GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int count = 0;
                while (e.MoveNext())
                {
                    DictionaryEntry entry = (DictionaryEntry)e.Current;

                    Assert.Equal(entry.Value, d[entry.Key]);
                    count++;
                }
                Assert.Equal(count, d.Count);
                Assert.False(e.MoveNext());
                e.Reset();
            }
        }

        // public void Insert(int index, object key, object value);
        [Fact]
        public void InsertTests()
        {
            var d = new OrderedDictionary();
            Assert.Throws<ArgumentOutOfRangeException>(() => d.Insert(-1, "foo", "bar"));
            Assert.Throws<ArgumentNullException>(() => d.Insert(0, null, "bar"));
            Assert.Throws<ArgumentOutOfRangeException>(() => d.Insert(1, "foo", "bar"));

            d.Insert(0, "foo", "bar");
            Assert.Equal("bar", d["foo"]);
            Assert.Equal("bar", d[0]);
            AssertExtensions.Throws<ArgumentException>(null, () => d.Insert(0, "foo", "bar"));

            d.Insert(0, "aaa", "bbb");
            Assert.Equal("bbb", d["aaa"]);
            Assert.Equal("bbb", d[0]);

            d.Insert(0, "zzz", "ccc");
            Assert.Equal("ccc", d["zzz"]);
            Assert.Equal("ccc", d[0]);

            d.Insert(3, "13", "37");
            Assert.Equal("37", d["13"]);
            Assert.Equal("37", d[3]);
        }

        // public void Remove(object key);
        [Fact]
        public void RemoveTests()
        {
            var d = new OrderedDictionary();

            // should work
            d.Remove("asd");

            Assert.Throws<ArgumentNullException>(() => d.Remove(null));

            for (var i = 0; i < 1000; i++)
            {
                d.Add("foo_" + i, "bar_" + i);
            }
            for (var i = 0; i < 1000; i++)
            {
                Assert.True(d.Contains("foo_" + i));
                d.Remove("foo_" + i);
                Assert.False(d.Contains("foo_" + i));
                Assert.Equal(1000 - i - 1, d.Count);
            }
        }

        // public void RemoveAt(int index);
        [Fact]
        public void RemoveAtTests()
        {
            var d = new OrderedDictionary();

            Assert.Throws<ArgumentOutOfRangeException>(() => d.RemoveAt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => d.RemoveAt(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => d.RemoveAt(5));

            Assert.Throws<ArgumentNullException>(() => d.Remove(null));

            for (var i = 0; i < 1000; i++)
            {
                d.Add("foo_" + i, "bar_" + i);
            }
            for (var i = 0; i < 1000; i++)
            {
                d.RemoveAt(1000 - i - 1);
                Assert.Equal(1000 - i - 1, d.Count);
            }
            for (var i = 0; i < 1000; i++)
            {
                d.Add("foo_" + i, "bar_" + i);
            }
            for (var i = 0; i < 1000; i++)
            {
                Assert.Equal("bar_" + i, d[0]);
                d.RemoveAt(0);
                Assert.Equal(1000 - i - 1, d.Count);
            }
        }
    }
}

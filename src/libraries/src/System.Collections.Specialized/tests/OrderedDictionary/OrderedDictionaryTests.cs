// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            Assert.Throws<ArgumentException>(() => d1.Add("FOO", "bar"));
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
        }

        // bool System.Collections.ICollection.IsSynchronized { get; }
        [Fact]
        public void IsSynchronizedTests()
        {
            ICollection c = new OrderedDictionary();
            Assert.False(c.IsSynchronized);
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

            foreach (var val in d.Values)
            {
                string sval = (string)val;
                var p = sval.Split(new char[] { '_' });
                Assert.Equal(2, p.Length);
                int number = int.Parse(p[1]);
                Assert.False(alreadyChecked[number]);
                Assert.True(number >= 0 && number < 1000);
                alreadyChecked[number] = true;
            }
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

            Assert.Throws<ArgumentException>(() => d.Add((int)5, "foo"));
            Assert.Throws<ArgumentException>(() => d.Add((double)5, "foo"));
            Assert.Throws<ArgumentException>(() => d.Add((long)5, "foo"));
            Assert.Throws<ArgumentException>(() => d.Add((short)5, "foo"));
            Assert.Throws<ArgumentException>(() => d.Add((uint)5, "foo"));
            Assert.Throws<ArgumentException>(() => d.Add("5", "foo"));

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
            Assert.Throws<ArgumentException>(() => d.CopyTo(arr, 3));

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

        // public virtual IDictionaryEnumerator GetEnumerator();
        // IEnumerator System.Collections.IEnumerable.GetEnumerator();
        [Fact]
        public void GetEnumeratorTests()
        {
            var d = new OrderedDictionary();
            IEnumerator e = d.GetEnumerator();
            d["foo"] = "bar";
            Assert.Throws<InvalidOperationException>(() => e.MoveNext());
            e = d.GetEnumerator();
            Assert.True(e.MoveNext());
            var c = (DictionaryEntry)e.Current;
            Assert.Equal("foo", c.Key);
            Assert.Equal("bar", c.Value);
            Assert.False(e.MoveNext());
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
            Assert.Throws<ArgumentException>(() => d.Insert(0, "foo", "bar"));

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
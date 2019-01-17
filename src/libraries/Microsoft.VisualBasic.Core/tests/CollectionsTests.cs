// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using Xunit;

namespace Microsoft.VisualBasic.Tests
{
    public static class CollectionsTests
    {
        [Fact]
        public static void Ctor_Empty()
        {
            var coll = new Collection();

            Assert.Equal(0, coll.Count);
        }

        private static Foo CreateValue(int i) => new Foo(i, i.ToString());

        private static Collection CreateCollection(int count)
        {
            var coll = new Collection();
            for (int i = 0; i < 10; i++)
            {
                coll.Add(CreateValue(i));
            }
            return coll;
        }

        private static Collection CreateKeyedCollection(int count)
        {
            var coll = new Collection();
            for (int i = 0; i < 10; i++)
            {
                coll.Add(CreateValue(i), Key: "Key" + i.ToString());
            }
            return coll;
        }

        [Fact]
        public static void Add()
        {
            IList coll = new Collection();
            for (int i = 0; i < 10; i++)
            {
                Foo value = CreateValue(i);
                coll.Add(value);
                Assert.True(coll.Contains(value));
            }
            Assert.Equal(10, coll.Count);
            for (int i = 0; i < coll.Count; i++)
            {
                Foo value = CreateValue(i);
                Assert.Equal(value, coll[i]);
            }
        }

        [Fact]
        public static void Add_RelativeIndex()
        {
            var coll = new Collection();
            var item1 = CreateValue(1);
            var item2 = CreateValue(2);
            var item3 = CreateValue(3);

            coll.Add(item1);
            coll.Add(item2, Before: 1);
            coll.Add(item3, After: 1);

            Assert.Equal(item2, coll[1]);
            Assert.Equal(item3, coll[2]);
            Assert.Equal(item1, coll[3]);
        }

        [Fact]
        public static void Add_RelativeKey()
        {
            var coll = new Collection();
            var item1 = CreateValue(1);
            var item2 = CreateValue(2);
            var item3 = CreateValue(3);

            coll.Add(item1, "Key1");
            coll.Add(item2, Key: "Key2", Before: "Key1");
            coll.Add(item3, After: "Key2");

            Assert.Equal(item2, coll[1]);
            Assert.Equal(item3, coll[2]);
            Assert.Equal(item1, coll[3]);
        }

        [Fact]
        public static void Add_Relative_Invalid()
        {
            var coll = new Collection();
            var item1 = CreateValue(1);

            Assert.Throws<ArgumentException>(() => coll.Add(item1, Before: 1, After: 1)); // Before and after specified
            Assert.Throws<InvalidCastException>(() => coll.Add(item1, Before: new object())); // Before not in a string or int
            Assert.Throws<InvalidCastException>(() => coll.Add(item1, After: new object())); // After not in a string or int
            Assert.Throws<ArgumentOutOfRangeException>("Index", () => coll.Add(item1, Before: 5)); // Before not in range
            Assert.Throws<ArgumentOutOfRangeException>("Index", () => coll.Add(item1, After: 5)); // After not in range
            Assert.Throws<ArgumentException>(() => coll.Add(item1, Before: "Key5")); // Before not found
            Assert.Throws<ArgumentException>(() => coll.Add(item1, After: "Key5")); // After not found
        }

        [Fact]
        public static void Remove()
        {
            IList coll = CreateCollection(10);
            for (int i = 0; i < 10; i++)
            {
                Foo value = CreateValue(i);
                coll.Remove(value);
                Assert.False(coll.Contains(value));
            }
            Assert.Equal(0, coll.Count);

            coll.Remove(new Foo()); // No throw for non-existent object
        }

        [Fact]
        public static void Insert()
        {
            IList coll = new Collection();
            for (int i = 0; i < 10; i++)
            {
                Foo value = CreateValue(i);
                coll.Insert(i, value);
                Assert.True(coll.Contains(value));
            }
            Assert.Equal(10, coll.Count);
            for (int i = 0; i < coll.Count; i++)
            {
                var expected = CreateValue(i);
                Assert.Equal(expected, coll[i]);
            }
        }

        [Fact]
        public static void Insert_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            IList coll = CreateCollection(10);
            Assert.Throws<ArgumentOutOfRangeException>("Index", () => coll.Insert(-1, new Foo())); // Index < 0
            Assert.Throws<ArgumentOutOfRangeException>("Index", () => coll.Insert(coll.Count + 1, new Foo())); // Index > coll.Count

            Assert.Equal(10, coll.Count);
        }

        [Fact]
        public static void RemoveAt()
        {
            IList coll = CreateCollection(10);
            for (int i = 0; i < coll.Count; i++)
            {
                coll.RemoveAt(0);
                Assert.False(coll.Contains(CreateValue(i)));
            }
        }

        [Fact]
        public static void RemoveAt_InvalidIndex_ThrowsArgumentOutOfRangeException()
        {
            IList coll = CreateCollection(10);

            var firstItem = coll[0];
            coll.RemoveAt(-10);    // Indexing bug when accessing through the IList interface on non-empty collection
            Assert.False(coll.Contains(firstItem));
            Assert.Equal(9, coll.Count);

            coll = new Collection();

            Assert.Throws<ArgumentOutOfRangeException>("Index", () => coll.RemoveAt(-1)); // Index < 0
        }

        [Fact]
        public static void Remove_Key()
        {
            var coll = CreateKeyedCollection(10);

            Assert.True(coll.Contains("Key3"));
            coll.Remove("Key3");
            Assert.False(coll.Contains("Key3"));
        }

        [Fact]
        public static void Remove_InvalidKey_ThrowsArgumentException()
        {
            var coll = CreateKeyedCollection(10);

            Assert.Throws<ArgumentException>(() => coll.Remove("Key10"));
        }

        [Fact]
        public static void Remove_Index()
        {
            Collection coll = CreateCollection(10);
            for (int i = 0; i < coll.Count; i++)
            {
                coll.Remove(1);
                Assert.False(((IList)coll).Contains(CreateValue(i)));
            }
        }

        [Fact]
        public static void Remove_InvalidIndex_ThrowsArgumentException()
        {
            var coll = CreateCollection(10);

            Assert.Throws<IndexOutOfRangeException>(() => coll.Remove(20));
        }

        [Fact]
        public static void Clear()
        {
            Collection coll = CreateCollection(10);
            coll.Clear();
            Assert.Equal(0, coll.Count);
        }

        [Fact]
        public static void IndexOf()
        {
            IList coll = CreateCollection(10);
            for (int i = 0; i < coll.Count; i++)
            {
                int ndx = coll.IndexOf(CreateValue(i));
                Assert.Equal(i, ndx);
            }
        }

        [Fact]
        public static void Contains()
        {
            IList coll = CreateCollection(10);
            for (int i = 0; i < coll.Count; i++)
            {
                Assert.True(coll.Contains(CreateValue(i)));
            }
            Assert.False(coll.Contains(new Foo()));
        }

        [Fact]
        public static void Contains_ByKey()
        {
            var coll = CreateKeyedCollection(10);

            Assert.True(coll.Contains("Key3"));
            Assert.False(coll.Contains("Key10"));
        }

        [Fact]
        public static void Item_Get()
        {
            Collection coll = CreateCollection(10);
            for (int i = 0; i < coll.Count; i++)
            {
                Assert.Equal(CreateValue(i), coll[i + 1]);
            }

            Assert.Equal(CreateValue(5), coll[(object)6]);
        }

        [Fact]
        public static void Item_Get_InvalidIndex_ThrowsIndexOutOfRangeException()
        {
            Collection coll = CreateCollection(10);

            Assert.Equal(((IList)coll)[-10], coll[1]);    // Indexing bug when accessing through the IList interface on non-empty collection

            Assert.Throws<IndexOutOfRangeException>(() => coll[0]); // Index <= 0
            Assert.Throws<IndexOutOfRangeException>(() => coll[coll.Count + 1]); // Index < 0
            Assert.Throws<ArgumentException>(() => coll[(object)Guid.Empty]); // Neither string nor int
        }

        [Fact]
        public static void Item_GetByKey()
        {
            Collection coll = CreateKeyedCollection(10);
            for (int i = 0; i < coll.Count; i++)
            {
                Assert.Equal(CreateValue(i), coll["Key" + i.ToString()]);
            }

            Assert.Equal(CreateValue(5), coll[(object)"Key5"]);
            Assert.Equal(CreateValue(5), coll[(object)new char[] { 'K', 'e', 'y', '5' }]);

            coll.Add(CreateValue(11), "X");
            Assert.Equal(CreateValue(11), coll[(object)'X']);
        }

        [Fact]
        public static void Item_GetByKey_InvalidIndex_ThrowsIndexOutOfRangeException()
        {
            Collection coll = CreateKeyedCollection(10);

            Assert.Throws<ArgumentException>(() => coll["Key20"]);
            Assert.Throws<IndexOutOfRangeException>(() => coll[(string)null]);
            Assert.Throws<IndexOutOfRangeException>(() => coll[(object)null]);
        }

        [Fact]
        public static void Item_Set()
        {
            IList coll = CreateCollection(10);
            for (int i = 0; i < coll.Count; i++)
            {
                var value = CreateValue(coll.Count - i);
                coll[i] = value;
                Assert.Equal(value, coll[i]);
            }
        }

        [Fact]
        public static void Item_Set_Invalid()
        {
            IList coll = new Collection();

            Assert.Throws<ArgumentOutOfRangeException>("Index", () => coll[-1] = new Foo()); // Index < 0
            Assert.Throws<ArgumentOutOfRangeException>("Index", () => coll[coll.Count + 1] = new Foo()); // Index >= InnerList.Count
        }

        [Fact]
        public static void CopyTo()
        {
            Collection coll = CreateCollection(10);

            // Basic
            var fooArr = new Foo[10];
            ((ICollection)coll).CopyTo(fooArr, 0);

            Assert.Equal(coll.Count, fooArr.Length);
            for (int i = 0; i < fooArr.Length; i++)
            {
                Assert.Equal(coll[i + 1], fooArr.GetValue(i));
            }

            // With index
            fooArr = new Foo[coll.Count * 2];
            ((ICollection)coll).CopyTo(fooArr, coll.Count);

            for (int i = coll.Count; i < fooArr.Length; i++)
            {
                Assert.Equal(coll[i - coll.Count + 1], fooArr.GetValue(i));
            }
        }

        [Fact]
        public static void CopyTo_Invalid()
        {
            ICollection coll = CreateCollection(10);
            var fooArr = new Foo[10];
            // Index < 0
            Assert.Throws<ArgumentException>(() => coll.CopyTo(fooArr, -1));
            // Index + fooArray.Length > coll.Count
            Assert.Throws<ArgumentException>(() => coll.CopyTo(fooArr, 5));
        }

        [Fact]
        public static void GetEnumerator()
        {
            Collection coll = CreateCollection(10);
            IEnumerator enumerator = coll.GetEnumerator();
            Assert.NotNull(enumerator);

            int count = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(coll[count + 1], enumerator.Current);
                count++;
            }

            Assert.Equal(coll.Count, count);
        }

        [Fact]
        public static void GetEnumerator_PrePost()
        {
            Collection coll = CreateCollection(10);
            IEnumerator enumerator = coll.GetEnumerator();

            // Index <= 0
            Assert.Equal(null, enumerator.Current);

            // Index >= dictionary.Count
            while (enumerator.MoveNext())
                ;
            Assert.Equal(null, enumerator.Current);
            Assert.False(enumerator.MoveNext());

            // Current throws after resetting
            enumerator.Reset();
            Assert.True(enumerator.MoveNext());

            enumerator.Reset();
            Assert.Equal(null, enumerator.Current);
        }

        [Fact]
        public static void SyncRoot()
        {
            ICollection coll = new Collection();
            Assert.Equal(coll.SyncRoot, coll);
        }

        [Fact]
        public static void IListProperties()
        {
            IList coll = CreateCollection(10);
            Assert.False(coll.IsFixedSize);
            Assert.False(coll.IsReadOnly);
            Assert.False(coll.IsSynchronized);
        }

        private class Foo
        {
            public Foo()
            {
            }

            public Foo(int intValue, string stringValue)
            {
                IntValue = intValue;
                StringValue = stringValue;
            }

            public int IntValue { get; set; }
            public string StringValue { get; set; }

            public override bool Equals(object obj)
            {
                Foo foo = obj as Foo;
                if (foo == null)
                    return false;
                return foo.IntValue == IntValue && foo.StringValue == StringValue;
            }

            public override int GetHashCode() => IntValue;
        }
    }
}

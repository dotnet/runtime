// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace List_List_ConstructorTests
{
    public class Driver<T>
    {
        public void TestDefaultConstructor()
        {
            List<T> list = new List<T>();
            Assert.Equal(0, list.Capacity); //"Expected capacity of list to be the same as given."
            Assert.Equal(0, list.Count); //"Do not expect anything to be in the list."
            Assert.False(((IList<T>)list).IsReadOnly); //"List should not be readonly"
        }

        public void TestCapacityConstructor(int capacity)
        {
            List<T> list = new List<T>(capacity);
            Assert.Equal(capacity, list.Capacity); //"Expected capacity of list to be the same as given."
            Assert.Equal(0, list.Count); //"Do not expect anything to be in the list."
            Assert.False(((IList<T>)list).IsReadOnly); //"List should not be readonly"
        }

        public void TestCapacityConstructor_Negative(int capacity)
        {
            List<T> _list;
            Assert.Throws<ArgumentOutOfRangeException>(() => _list = new List<T>(capacity)); //"ArgumentOutOfRangeException expected given capacity: " + capacity
        }

        public void TestICollectionConstructor(T[] items)
        {
            TestCollection<T> col = new TestCollection<T>(items);
            List<T> list = new List<T>(col);
            TestICollectionConstructor(list, items);
        }

        public void TestICollectionConstructor(IEnumerable<T> collection, T[] items)
        {
            List<T> _list = new List<T>(collection);
            TestICollectionConstructor(_list, items, false);
        }

        public void TestICollectionConstructor(List<T> list, T[] items)
        {
            TestICollectionConstructor(list, items, true);
        }

        public void TestICollectionConstructor(List<T> list, T[] items, bool verifyCapacity)
        {
            if (items == null)
            {
                Assert.Equal(0, list.Capacity); //"Expected capacity of list to be the same as given."
                Assert.Equal(0, list.Count); //"Do not expect anything to be in the list."
            }
            else
            {
                Assert.Equal(items.Length, list.Count); //"Number of items in list do not match the number of items given."
                if (verifyCapacity)
                    Assert.Equal(items.Length, list.Capacity); //"Expected the capcacity to be the same as the length of input array"

                for (int i = 0; i < items.Length; i++)
                {
                    Assert.Equal(items[i], list[i]); //"Expected object in item array to be the same as in the list"
                }
            }

            Assert.False(((IList<T>)list).IsReadOnly); //"List should not be readonly"
        }

        public void TestICollectionConstructorNull()
        {
            Assert.Throws<ArgumentNullException>(() => { List<T> _list = new List<T>(null); }); //"Expected ArgumentnUllException for null items"
        }
    }
    public class List_ConstructorTests
    {
        [Fact]
        public static void DefaultConstructor()
        {
            Driver<int> IntDriver = new Driver<int>();
            IntDriver.TestDefaultConstructor();

            Driver<string> StringDriver = new Driver<string>();
            StringDriver.TestDefaultConstructor();

            Driver<RefX1<int>> RefX1Driver = new Driver<RefX1<int>>();
            RefX1Driver.TestDefaultConstructor();

            Driver<RefX1<string>> RefX2Driver = new Driver<RefX1<string>>();
            RefX2Driver.TestDefaultConstructor();

            Driver<ValX1<string>> ValX1Driver = new Driver<ValX1<string>>();
            ValX1Driver.TestDefaultConstructor();

            Driver<ValX1<int>> ValX2Driver = new Driver<ValX1<int>>();
            ValX2Driver.TestDefaultConstructor();
        }

        [Fact]
        public static void Capacity_Int()
        {
            Driver<int> IntDriver = new Driver<int>();
            IntDriver.TestCapacityConstructor(0);
            IntDriver.TestCapacityConstructor(10);
            IntDriver.TestCapacityConstructor(15);
            IntDriver.TestCapacityConstructor(16);
            IntDriver.TestCapacityConstructor(17);
            IntDriver.TestCapacityConstructor(100);
        }

        [Fact]
        public static void Capacity_String()
        {
            Driver<string> StringDriver = new Driver<string>();
            StringDriver.TestCapacityConstructor(0);
            StringDriver.TestCapacityConstructor(10);
            StringDriver.TestCapacityConstructor(15);
            StringDriver.TestCapacityConstructor(16);
            StringDriver.TestCapacityConstructor(17);
            StringDriver.TestCapacityConstructor(100);
        }

        [Fact]
        public static void Capacity_ValX1()
        {
            Driver<ValX1<string>> ValX1Driver = new Driver<ValX1<string>>();
            ValX1Driver.TestCapacityConstructor(17);
            Driver<ValX1<int>> ValX2Driver = new Driver<ValX1<int>>();
            ValX2Driver.TestCapacityConstructor(17);
        }

        [Fact]
        public static void Capacity_RefX1()
        {
            Driver<RefX1<int>> RefX1Driver = new Driver<RefX1<int>>();
            RefX1Driver.TestCapacityConstructor(0);
            Driver<RefX1<string>> RefX2Driver = new Driver<RefX1<string>>();
            RefX2Driver.TestCapacityConstructor(4);
        }

        [Fact]
        public static void Capacity_NegativeTests()
        {
            Driver<int> IntDriver = new Driver<int>();
            IntDriver.TestCapacityConstructor_Negative(-1);
            IntDriver.TestCapacityConstructor_Negative(int.MinValue);

            Driver<string> StringDriver = new Driver<string>();
            StringDriver.TestCapacityConstructor_Negative(-1);
            StringDriver.TestCapacityConstructor_Negative(int.MinValue);
        }

        [Fact]
        public static void ICollection_Int()
        {
            Driver<int> IntDriver = new Driver<int>();
            int[] intArr = new int[1000];
            for (int i = 0; i < 1000; i++)
            {
                intArr[i] = i;
            }
            IntDriver.TestICollectionConstructor(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            IntDriver.TestICollectionConstructor(intArr);
            IntDriver.TestICollectionConstructor(new int[0]);
            IntDriver.TestICollectionConstructor(new int[10]);

            IntDriver.TestICollectionConstructor(new TestCollection<int>(new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }), new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            IntDriver.TestICollectionConstructor(new TestCollection<int>(intArr), intArr);
            IntDriver.TestICollectionConstructor(new TestCollection<int>(new int[0]), new int[0]);
            IntDriver.TestICollectionConstructor(new TestCollection<int>(new int[10]), new int[10]);
        }

        [Fact]
        public static void ICollection_String()
        {
            Driver<string> StringDriver = new Driver<string>();
            string[] stringArr = new string[1000];
            for (int i = 0; i < 1000; i++)
            {
                stringArr[i] = "SomeTestString" + i.ToString();
            }
            StringDriver.TestICollectionConstructor(new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" });
            StringDriver.TestICollectionConstructor(stringArr);
            StringDriver.TestICollectionConstructor(new string[0]);
            StringDriver.TestICollectionConstructor(new string[100]);

            StringDriver.TestICollectionConstructor(new TestCollection<string>(new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" }), new string[] { "1", "2", "3", "4", "5", "6", "7", "8", "9", "10" });
            StringDriver.TestICollectionConstructor(new TestCollection<string>(stringArr), stringArr);
            StringDriver.TestICollectionConstructor(new TestCollection<string>(new string[0]), new string[0]);
            StringDriver.TestICollectionConstructor(new TestCollection<string>(new string[100]), new string[100]);
        }
        [Fact]
        public static void ICollection_NegativeTests()
        {
            Driver<string> StringDriver = new Driver<string>();
            StringDriver.TestICollectionConstructorNull();
            Driver<int> IntDriver = new Driver<int>();
            IntDriver.TestICollectionConstructorNull();
        }
    }

    #region Helper Classes

    /// <summary>
    /// Helper class that implements ICollection.
    /// </summary>
    public class TestCollection<T> : ICollection<T>
    {
        /// <summary>
        /// Expose the Items in Array to give more test flexibility...
        /// </summary>
        public readonly T[] m_items;

        public TestCollection(T[] items)
        {
            m_items = items;
        }

        public void CopyTo(T[] array, int index)
        {
            Array.Copy(m_items, 0, array, index, m_items.Length);
        }

        public int Count
        {
            get
            {
                if (m_items == null)
                    return 0;
                else
                    return m_items.Length;
            }
        }

        public Object SyncRoot { get { return this; } }

        public bool IsSynchronized { get { return false; } }

        public IEnumerator<T> GetEnumerator()
        {
            return new TestCollectionEnumerator<T>(this);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new TestCollectionEnumerator<T>(this);
        }

        private class TestCollectionEnumerator<T1> : IEnumerator<T1>
        {
            private TestCollection<T1> _col;
            private int _index;

            public void Dispose() { }

            public TestCollectionEnumerator(TestCollection<T1> col)
            {
                _col = col;
                _index = -1;
            }

            public bool MoveNext()
            {
                return (++_index < _col.m_items.Length);
            }

            public T1 Current
            {
                get { return _col.m_items[_index]; }
            }

            Object System.Collections.IEnumerator.Current
            {
                get { return _col.m_items[_index]; }
            }

            public void Reset()
            {
                _index = -1;
            }
        }

        #region Non Implemented methods

        public void Add(T item) { throw new NotSupportedException(); }

        public void Clear() { throw new NotSupportedException(); }
        public bool Contains(T item) { throw new NotSupportedException(); }

        public bool Remove(T item) { throw new NotSupportedException(); }

        public bool IsReadOnly { get { throw new NotSupportedException(); } }

        #endregion
    }

    /// <summary>
    /// Helps tests reference types.
    /// </summary>
    public class RefX1<T> : IComparable<RefX1<T>> where T : IComparable
    {
        private T _val;
        public T Val
        {
            get { return _val; }
            set { _val = value; }
        }
        public RefX1(T t) { _val = t; }
        public int CompareTo(RefX1<T> obj)
        {
            if (null == obj)
                return 1;
            if (null == _val)
                if (null == obj.Val)
                    return 0;
                else
                    return -1;
            return _val.CompareTo(obj.Val);
        }
        public override bool Equals(object obj)
        {
            if (obj is RefX1<T>)
            {
                RefX1<T> v = (RefX1<T>)obj;
                return (CompareTo(v) == 0);
            }
            return false;
        }
        public override int GetHashCode() { return base.GetHashCode(); }

        public bool Equals(RefX1<T> x)
        {
            return 0 == CompareTo(x);
        }
    }

    /// <summary>
    /// Helps test value types.
    /// </summary>
    public struct ValX1<T> : IComparable<ValX1<T>> where T : IComparable
    {
        private T _val;
        public T Val
        {
            get { return _val; }
            set { _val = value; }
        }
        public ValX1(T t) { _val = t; }
        public int CompareTo(ValX1<T> obj)
        {
            if (Object.ReferenceEquals(_val, obj._val)) return 0;

            if (null == _val)
                return -1;

            return _val.CompareTo(obj.Val);
        }
        public override bool Equals(object obj)
        {
            if (obj is ValX1<T>)
            {
                ValX1<T> v = (ValX1<T>)obj;
                return (CompareTo(v) == 0);
            }
            return false;
        }
        public override int GetHashCode() { return ((object)this).GetHashCode(); }

        public bool Equals(ValX1<T> x)
        {
            return 0 == CompareTo(x);
        }
    }

    #endregion
}

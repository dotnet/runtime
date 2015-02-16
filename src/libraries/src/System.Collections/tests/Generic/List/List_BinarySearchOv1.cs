// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace List_List_BinarySearchOv1
{
    public class Driver<T> where T : IComparable<T>
    {
        public void BinarySearch(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            //search for each item
            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal(i, list.BinarySearch(items[i])); //"Searching from beginning should produce correct index."
            }

            //ensure no side effects
            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal((object)items[i], (object)list[i]); //"Item in list should not have been mutated after performing BinarySearch."
            }

            //search for each item starting from last item
            for (int i = items.Length - 1; i >= 0; i--)
            {
                Assert.Equal(i, list.BinarySearch(items[i])); //"Starting search from last item should produce correct index."
            }

            //ensure no side effects
            for (int i = 0; i < items.Length; i++)
            {
                Assert.Equal((object)items[i], (object)list[i]); //"Item in list should not have been mutated after performing BinarySearch."
            }
        }

        public void BinarySearchNegative(T[] itemsX, T[] itemsY, int[] index)
        {
            List<T> list = new List<T>(new TestCollection<T>(itemsX));

            //search for each item
            for (int i = 0; i < itemsY.Length; i++)
            {
                Assert.Equal(index[i], list.BinarySearch(itemsY[i])); //"Item found in binary search should be the same as expected."
            }

            //ensure no side effects
            for (int i = 0; i < itemsX.Length; i++)
            {
                Assert.Equal((object)itemsX[i], (object)list[i]); //"Item in list should not have been mutated after performing BinarySearch."
            }
        }

        public void BinarySearchValidations(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            Assert.Throws<InvalidOperationException>(() => list.BinarySearch(items[0], null)); //"Given a null object to search for, should throw an InvalidOperationException."
        }
    }
    public class BinarySearch
    {
        [Fact]
        public static void BinaryTreeValue_Even()
        {
            Driver<RefX1<int>> RefDriver = new Driver<RefX1<int>>();
            RefX1<int>[] intArr1 = new RefX1<int>[100];
            for (int i = 0; i < 100; i++)
            {
                intArr1[i] = new RefX1<int>(i);
            }

            //even
            RefDriver.BinarySearch(intArr1);

            RefDriver.BinarySearchNegative(new RefX1<int>[0], new RefX1<int>[] { null }, new int[] { -1 });
            RefDriver.BinarySearchNegative(intArr1, new RefX1<int>[] { null }, new int[] { -1 });
            RefDriver.BinarySearchNegative(intArr1, new RefX1<int>[] { new RefX1<int>(100) }, new int[] { -101 });

            intArr1[50] = new RefX1<int>(49);
            RefDriver.BinarySearchNegative(intArr1, new RefX1<int>[] { new RefX1<int>(50) }, new int[] { -52 });

            intArr1 = new RefX1<int>[99];
            for (int i = 0; i < 99; i++)
            {
                intArr1[i] = new RefX1<int>(i);
            }
        }

        [Fact]
        public static void BinarySearchValue_Odd()
        {
            Driver<RefX1<int>> RefDriver = new Driver<RefX1<int>>();
            RefX1<int>[] intArr1 = new RefX1<int>[99];
            for (int i = 0; i < 99; i++)
            {
                intArr1[i] = new RefX1<int>(i);
            }

            //odd
            RefDriver.BinarySearch(intArr1);

            RefDriver.BinarySearchNegative(new RefX1<int>[0], new RefX1<int>[] { null }, new int[] { -1 });
            RefDriver.BinarySearchNegative(intArr1, new RefX1<int>[] { null }, new int[] { -1 });
            RefDriver.BinarySearchNegative(intArr1, new RefX1<int>[] { new RefX1<int>(100) }, new int[] { -100 });

            intArr1[50] = new RefX1<int>(49);
            RefDriver.BinarySearchNegative(intArr1, new RefX1<int>[] { new RefX1<int>(50) }, new int[] { -52 });

            RefDriver.BinarySearch(new RefX1<int>[] { });
            RefDriver.BinarySearch(new RefX1<int>[] { new RefX1<int>(1) });
            RefDriver.BinarySearch(new RefX1<int>[] { new RefX1<int>(1), new RefX1<int>(2) });
            RefDriver.BinarySearch(new RefX1<int>[] { new RefX1<int>(1), new RefX1<int>(2), new RefX1<int>(3) });
        }

        [Fact]
        public static void BinarySearchRef_Even()
        {
            Driver<ValX1<int>> ValDriver = new Driver<ValX1<int>>();
            ValX1<int>[] vintArr1 = new ValX1<int>[100];
            for (int i = 0; i < 100; i++)
            {
                vintArr1[i] = new ValX1<int>(i);
            }

            //even
            ValDriver.BinarySearch(vintArr1);

            ValDriver.BinarySearchNegative(new ValX1<int>[0], new ValX1<int>[] { new ValX1<int>(-1) }, new int[] { -1 });
            ValDriver.BinarySearchNegative(vintArr1, new ValX1<int>[] { new ValX1<int>(-1) }, new int[] { -1 });
            ValDriver.BinarySearchNegative(vintArr1, new ValX1<int>[] { new ValX1<int>(100) }, new int[] { -101 });

            vintArr1[50] = new ValX1<int>(49);
            ValDriver.BinarySearchNegative(vintArr1, new ValX1<int>[] { new ValX1<int>(50) }, new int[] { -52 });
        }

        [Fact]
        public static void BinarySearchRef_Odd()
        {
            Driver<ValX1<int>> ValDriver = new Driver<ValX1<int>>();
            ValX1<int>[] vintArr1 = new ValX1<int>[99];
            for (int i = 0; i < 99; i++)
            {
                vintArr1[i] = new ValX1<int>(i);
            }

            //odd
            ValDriver.BinarySearch(vintArr1);
            ValDriver.BinarySearchNegative(vintArr1, new ValX1<int>[] { new ValX1<int>(-1) }, new int[] { -1 });
            ValDriver.BinarySearchNegative(vintArr1, new ValX1<int>[] { new ValX1<int>(100) }, new int[] { -100 });
            vintArr1[50] = new ValX1<int>(49);
            ValDriver.BinarySearchNegative(vintArr1, new ValX1<int>[] { new ValX1<int>(50) }, new int[] { -52 });

            ValDriver.BinarySearch(new ValX1<int>[] { });
            ValDriver.BinarySearch(new ValX1<int>[] { new ValX1<int>(1) });
            ValDriver.BinarySearch(new ValX1<int>[] { new ValX1<int>(1), new ValX1<int>(2) });
            ValDriver.BinarySearch(new ValX1<int>[] { new ValX1<int>(1), new ValX1<int>(2), new ValX1<int>(3) });
        }
    }
    #region Helper classes

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

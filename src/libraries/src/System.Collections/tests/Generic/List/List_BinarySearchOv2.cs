// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace List_List_BinarySearchOv2
{
    public class Driver<T> where T : IComparableValue
    {
        public void BinarySearch(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            //search for each item
            for (int i = 0; i < items.Length; i++)
                Assert.Equal(i, list.BinarySearch(items[i], new ValueComparer<T>())); //"Binary Search should have returned the same index."

            //ensure no side effects
            for (int i = 0; i < items.Length; i++)
                Assert.Equal(items[i], (Object)list[i]); //"Should not have changed the items in the array/list."

            //search for each item starting from last item
            for (int i = items.Length - 1; i >= 0; i--)
                Assert.Equal(i, list.BinarySearch(items[i], new ValueComparer<T>())); //"Binary Search should have returned the same index starting from the tend."

            //ensure no side effects
            for (int i = 0; i < items.Length; i++)
                Assert.Equal(items[i], (Object)list[i]); //"Should not have changed the items in the array/list."
        }

        public void BinarySearch(T[] items, int index, int count)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));

            //search for each item
            for (int i = index; i < index + count; i++)
            {
                Assert.Equal(i, list.BinarySearch(index, count, items[i], new ValueComparer<T>())); //"Binary search should have returned the same index starting search from the beginning"
            }

            //ensure no side effects
            for (int i = 0; i < items.Length; i++)
                Assert.Equal(items[i], (Object)list[i]); //"Should not have changed the items in the array/list."

            //search for each item starting from last item
            for (int i = index + count - 1; i >= index; i--)
            {
                Assert.Equal(i, list.BinarySearch(index, count, items[i], new ValueComparer<T>())); //"Binary search should have returned the same index starting search from the end"
            }

            //ensure no side effects
            for (int i = 0; i < items.Length; i++)
                Assert.Equal(items[i], (Object)list[i]); //"Should not have changed the items in the array/list."
        }

        public void BinarySearchNegative(T[] itemsX, T[] itemsY)
        {
            List<T> list = new List<T>(new TestCollection<T>(itemsX));

            //search for each item
            for (int i = 0; i < itemsY.Length; i++)
                Assert.True(-1 >= list.BinarySearch(itemsY[i], new ValueComparer<T>())); //"Should not have found item with BinarySearch."

            //ensure no side effects
            for (int i = 0; i < itemsX.Length; i++)
                Assert.Equal((Object)itemsX[i], list[i]); //"Should not have changed the items in the array/list."
        }

        public void BinarySearchNegative(T[] itemsX, T[] itemsY, int[] error, int index, int count)
        {
            List<T> list = new List<T>(new TestCollection<T>(itemsX));

            //search for each item
            for (int i = 0; i < itemsY.Length; i++)
            {
                Assert.Equal(error[i], list.BinarySearch(index, count, itemsY[i], new ValueComparer<T>())); //"Binary search should have returned the same index starting search from the beginning"
            }

            //ensure no side effects
            for (int i = 0; i < itemsX.Length; i++)
                Assert.Equal((Object)itemsX[i], list[i]); //"Should not have changed the items in the array/list."
        }

        public void BinarySearchValidations(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            Assert.Throws<InvalidOperationException>(() => list.BinarySearch(items[0], null)); //"Should have thrown InvalidOperationException with null IComparer"
        }

        public void BinarySearchValidations2(T[] items)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            Assert.Throws<InvalidOperationException>(() => list.BinarySearch(0, items.Length, items[0], null)); //"IComparer = null should throw InvalidOperationException."
            Assert.Throws<ArgumentException>(() => list.BinarySearch(0, items.Length + 1, items[0], new ValueComparer<T>())); //"Finding items longer than array should throw ArgumentException"
            Assert.Throws<ArgumentOutOfRangeException>(() => list.BinarySearch(-1, items.Length, items[0], new ValueComparer<T>())); //"ArgumentOutOfRangeException should be thrown on negative index."
            Assert.Throws<ArgumentOutOfRangeException>(() => list.BinarySearch(0, -1, items[0], new ValueComparer<T>())); //"ArgumentOutOfRangeException should be thrown on negative count."
            Assert.Throws<ArgumentException>(() => list.BinarySearch(items.Length + 1, items.Length, items[0], new ValueComparer<T>())); //"ArgumentException should be thrown on index greater than length of array."
        }
    }
    public class BinarySearch2
    {
        [Fact]
        public static void BinarySearch_IComparer_RefEven()
        {
            Driver<RefX1_IC<int>> RefDriver = new Driver<RefX1_IC<int>>();
            RefX1_IC<int>[] intArr1 = new RefX1_IC<int>[100];
            for (int i = 0; i < 100; i++)
                intArr1[i] = new RefX1_IC<int>(i);

            RefX1_IC<int>[] intArr2 = new RefX1_IC<int>[10];
            for (int i = 0; i < 10; i++)
                intArr2[i] = new RefX1_IC<int>(i + 100);

            //even
            RefDriver.BinarySearch(intArr1);

            RefDriver.BinarySearchNegative(intArr1, intArr2);
            RefDriver.BinarySearchNegative(new RefX1_IC<int>[0], intArr2);
        }

        [Fact]
        public static void BinarySearch_IComparer_RefOdd()
        {
            Driver<RefX1_IC<int>> RefDriver = new Driver<RefX1_IC<int>>();
            RefX1_IC<int>[] intArr1 = new RefX1_IC<int>[99];
            for (int i = 0; i < 99; i++)
                intArr1[i] = new RefX1_IC<int>(i);
            //odd

            RefDriver.BinarySearch(intArr1);

            RefDriver.BinarySearch(new RefX1_IC<int>[] { });
            RefDriver.BinarySearch(new RefX1_IC<int>[] { new RefX1_IC<int>(1) });
            RefDriver.BinarySearch(new RefX1_IC<int>[] { new RefX1_IC<int>(1), new RefX1_IC<int>(2) });
            RefDriver.BinarySearch(new RefX1_IC<int>[] { new RefX1_IC<int>(1), new RefX1_IC<int>(2), new RefX1_IC<int>(3) });
        }

        [Fact]
        public static void BinarySearch_IComparer_ValueEven()
        {
            Driver<ValX1_IC<int>> ValDriver = new Driver<ValX1_IC<int>>();
            ValX1_IC<int>[] vintArr1 = new ValX1_IC<int>[100];
            for (int i = 0; i < 100; i++)
                vintArr1[i] = new ValX1_IC<int>(i);
            ValX1_IC<int>[] vintArr2 = new ValX1_IC<int>[10];
            for (int i = 0; i < 10; i++)
                vintArr2[i] = new ValX1_IC<int>(i + 100);

            ValDriver.BinarySearchNegative(vintArr1, vintArr2);
            ValDriver.BinarySearchNegative(new ValX1_IC<int>[0], vintArr2);
        }

        [Fact]
        public static void BinarySearch_IComparer_ValueOdd()
        {
            Driver<ValX1_IC<int>> ValDriver = new Driver<ValX1_IC<int>>();
            ValX1_IC<int>[] vintArr1 = new ValX1_IC<int>[99];
            for (int i = 0; i < 99; i++)
                vintArr1[i] = new ValX1_IC<int>(i);

            //odd
            ValDriver.BinarySearch(vintArr1);

            ValDriver.BinarySearch(new ValX1_IC<int>[] { });
            ValDriver.BinarySearch(new ValX1_IC<int>[] { new ValX1_IC<int>(1) });
            ValDriver.BinarySearch(new ValX1_IC<int>[] { new ValX1_IC<int>(1), new ValX1_IC<int>(2) });
            ValDriver.BinarySearch(new ValX1_IC<int>[] { new ValX1_IC<int>(1), new ValX1_IC<int>(2), new ValX1_IC<int>(3) });
        }

        [Fact]
        public static void BinarySearch_IComparer_Negative()
        {
            Driver<ValX1_IC<int>> ValDriver = new Driver<ValX1_IC<int>>();
            ValX1_IC<int>[] vintArr1 = new ValX1_IC<int>[100];
            for (int i = 0; i < 100; i++)
                vintArr1[i] = new ValX1_IC<int>(i);
            ValDriver.BinarySearchValidations(vintArr1);

            Driver<RefX1_IC<int>> RefDriver = new Driver<RefX1_IC<int>>();
            RefX1_IC<int>[] intArr1 = new RefX1_IC<int>[100];
            for (int i = 0; i < 100; i++)
                intArr1[i] = new RefX1_IC<int>(i);
            RefDriver.BinarySearchValidations(intArr1);
        }

        [Fact]
        public static void BinarySearch_IComparerCount_RefEven()
        {
            Driver<RefX1_IC<int>> RefDriver = new Driver<RefX1_IC<int>>();
            RefX1_IC<int>[] intArr1 = new RefX1_IC<int>[100];
            for (int i = 0; i < 100; i++)
                intArr1[i] = new RefX1_IC<int>(i);

            //even
            RefDriver.BinarySearch(intArr1, 0, intArr1.Length);

            RefDriver.BinarySearchNegative(intArr1, new RefX1_IC<int>[] { null }, new int[] { -1 }, 0, intArr1.Length);
            RefDriver.BinarySearchNegative(intArr1, new RefX1_IC<int>[] { new RefX1_IC<int>(100) }, new int[] { -101 }, 0, intArr1.Length);

            intArr1[50] = new RefX1_IC<int>(49);
            RefDriver.BinarySearchNegative(intArr1, new RefX1_IC<int>[] { new RefX1_IC<int>(50) }, new int[] { -52 }, 0, intArr1.Length);
        }

        [Fact]
        public static void BinarySearch_IComparerCount_RefOdd()
        {
            Driver<RefX1_IC<int>> RefDriver = new Driver<RefX1_IC<int>>();
            RefX1_IC<int>[] intArr1 = new RefX1_IC<int>[99];
            for (int i = 0; i < 99; i++)
                intArr1[i] = new RefX1_IC<int>(i);

            //odd
            RefDriver.BinarySearch(intArr1, 0, intArr1.Length);
            RefDriver.BinarySearch(intArr1, 1, intArr1.Length - 1);
            RefDriver.BinarySearch(intArr1, 25, intArr1.Length - 25);
            RefDriver.BinarySearch(intArr1, intArr1.Length, 0);
            RefDriver.BinarySearch(intArr1, intArr1.Length - 1, 1);
            RefDriver.BinarySearch(intArr1, intArr1.Length - 2, 2);


            RefDriver.BinarySearchNegative(intArr1, new RefX1_IC<int>[] { null }, new int[] { -1 }, 0, intArr1.Length);
            RefDriver.BinarySearchNegative(intArr1, new RefX1_IC<int>[] { new RefX1_IC<int>(1) }, new int[] { -3 }, 2, intArr1.Length - 2);
            RefDriver.BinarySearchNegative(intArr1, new RefX1_IC<int>[] { new RefX1_IC<int>(99) }, new int[] { -98 }, 0, intArr1.Length - 2);
            RefDriver.BinarySearchNegative(intArr1, new RefX1_IC<int>[] { new RefX1_IC<int>(100) }, new int[] { -100 }, 0, intArr1.Length);

            intArr1[50] = new RefX1_IC<int>(49);
            RefDriver.BinarySearchNegative(intArr1, new RefX1_IC<int>[] { new RefX1_IC<int>(50) }, new int[] { -52 }, 0, intArr1.Length);
        }

        [Fact]
        public static void BinarySearch_IComparerCount_ValueEven()
        {
            Driver<ValX1_IC<int>> ValDriver = new Driver<ValX1_IC<int>>();
            ValX1_IC<int>[] vintArr1 = new ValX1_IC<int>[100];
            for (int i = 0; i < 100; i++)
                vintArr1[i] = new ValX1_IC<int>(i);

            //even
            ValDriver.BinarySearch(vintArr1, 0, vintArr1.Length);
            ValDriver.BinarySearchNegative(vintArr1, new ValX1_IC<int>[] { new ValX1_IC<int>(100) }, new int[] { -101 }, 0, vintArr1.Length);

            vintArr1[50] = new ValX1_IC<int>(49);
            ValDriver.BinarySearchNegative(vintArr1, new ValX1_IC<int>[] { new ValX1_IC<int>(50) }, new int[] { -52 }, 0, vintArr1.Length);
        }

        [Fact]
        public static void BinarySearch_IComparerCount_ValueOdd()
        {
            Driver<ValX1_IC<int>> ValDriver = new Driver<ValX1_IC<int>>();
            ValX1_IC<int>[] vintArr1 = new ValX1_IC<int>[99];
            for (int i = 0; i < 99; i++)
                vintArr1[i] = new ValX1_IC<int>(i);

            //odd
            ValDriver.BinarySearch(vintArr1, 0, vintArr1.Length);
            ValDriver.BinarySearch(vintArr1, 1, vintArr1.Length - 1);
            ValDriver.BinarySearch(vintArr1, 25, vintArr1.Length - 25);
            ValDriver.BinarySearch(vintArr1, vintArr1.Length, 0);
            ValDriver.BinarySearch(vintArr1, vintArr1.Length - 1, 1);
            ValDriver.BinarySearch(vintArr1, vintArr1.Length - 2, 2);


            ValDriver.BinarySearchNegative(vintArr1, new ValX1_IC<int>[] { new ValX1_IC<int>(1) }, new int[] { -3 }, 2, vintArr1.Length - 2);
            ValDriver.BinarySearchNegative(vintArr1, new ValX1_IC<int>[] { new ValX1_IC<int>(99) }, new int[] { -98 }, 0, vintArr1.Length - 2);
            ValDriver.BinarySearchNegative(vintArr1, new ValX1_IC<int>[] { new ValX1_IC<int>(100) }, new int[] { -100 }, 0, vintArr1.Length);

            vintArr1[50] = new ValX1_IC<int>(49);
            ValDriver.BinarySearchNegative(vintArr1, new ValX1_IC<int>[] { new ValX1_IC<int>(50) }, new int[] { -52 }, 0, vintArr1.Length);
        }

        [Fact]
        public static void BinarySearch_IComparerCount_Negative()
        {
            Driver<RefX1_IC<int>> RefDriver = new Driver<RefX1_IC<int>>();
            RefX1_IC<int>[] intArr1 = new RefX1_IC<int>[100];
            for (int i = 0; i < 100; i++)
                intArr1[i] = new RefX1_IC<int>(i);
            RefDriver.BinarySearchValidations2(intArr1);
        }
    }

    #region Helper Classes

    public interface IComparableValue
    {
        IComparable Val { get; set; }
        int CompareTo(IComparableValue obj);
    }

    public class RefX1_IC<T> : IComparableValue where T : IComparable
    {
        private T _val;
        public IComparable Val
        {
            get { return _val; }
            set { _val = (T)(object)value; }
        }
        public RefX1_IC(T t) { _val = t; }
        public int CompareTo(IComparableValue obj)
        {
            if (null == (object)obj)
                return 1;
            if (null == (object)_val)
                if (null == (object)obj.Val)
                    return 0;
                else
                    return -1;
            return _val.CompareTo(obj.Val);
        }
    }

    public struct ValX1_IC<T> : IComparableValue where T : IComparable
    {
        private T _val;
        public IComparable Val
        {
            get { return _val; }
            set { _val = (T)(object)value; }
        }
        public ValX1_IC(T t) { _val = t; }
        public int CompareTo(IComparableValue obj)
        {
            return _val.CompareTo(obj.Val);
        }
    }

    public class ValueComparer<T> : IComparer<T> where T : IComparableValue
    {
        public int Compare(T x, T y)
        {
            if (null == (object)x)
                if (null == (object)y)
                    return 0;
                else
                    return -1;
            if (null == (object)y)
                return 1;
            if (null == (object)x.Val)
                if (null == (object)y.Val)
                    return 0;
                else
                    return -1;
            return x.Val.CompareTo(y.Val);
        }

        public bool Equals(T x, T y)
        {
            return 0 == Compare(x, y);
        }

        public int GetHashCode(T x)
        {
            return x.GetHashCode();
        }
    }

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
    #endregion
}

// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace List_List_AddRangeTests
{
    public class Driver<T>
    {
        public IEnumerable<T> ConstructTestCollection(T[] items)
        {
            return new TestCollection<T>(items);
        }

        public IEnumerable<T> ConstructMyIEnumerable(T[] items)
        {
            return new MyIEnumerable<T>(items);
        }

        public void AddToEmptyList(T[] items, Func<T[], IEnumerable<T>> constructIEnumerable)
        {
            List<T> src = new List<T>(constructIEnumerable(items));

            //AddRange with List
            List<T> dst = new List<T>();
            dst.AddRange(src);
            for (int i = 0; i < items.Length; i++)
                Assert.Equal(items[i], dst[i]); //"Expected items in dst to be equal to expected. index: " + i

            //AddRange with ICollection implementation
            dst = new List<T>();
            dst.AddRange(constructIEnumerable(items));
            for (int i = 0; i < items.Length; i++)
                Assert.Equal(items[i], dst[i]); //"Expected items in dst to be equal to expected. index: " + i
        }

        public void AddToNonEmptyList(T[] itemsX, T[] itemsY, Func<T[], IEnumerable<T>> constructIEnumerable)
        {
            List<T> src = new List<T>(constructIEnumerable(itemsY));

            //AddRange with List
            List<T> dst = new List<T>(constructIEnumerable(itemsX));
            dst.AddRange(src);
            for (int i = itemsX.Length; i < itemsX.Length + itemsY.Length; i++)
                Assert.Equal(itemsY[i - itemsX.Length], dst[i]); //"Expected items in dst to be equal to expected. index: " + i

            //AddRange with ICollection implementation
            dst = new List<T>(constructIEnumerable(itemsX));
            dst.AddRange(constructIEnumerable(itemsY));
            for (int i = itemsX.Length; i < itemsX.Length + itemsY.Length; i++)
                Assert.Equal(itemsY[i - itemsX.Length], dst[i]); //"Expected items in dst to be equal to expected. index: " + i
        }

        public void AddNullRange()
        {
            List<T> list = new List<T>();
            Assert.Throws<ArgumentNullException>(() => list.AddRange(null)); //"ArgumentNullException expected when giving null collection."
        }
    }
    public class List_AddRangeTests
    {
        private readonly static Func<int[]> s_intArray = () =>
        {
            int[] intArr1 = new int[10];
            for (int i = 0; i < 10; i++)
                intArr1[i] = i;
            return intArr1;
        };
        private readonly static Func<int[]> s_intArray2 = () =>
        {
            int[] intArr2 = new int[10];
            for (int i = 0; i < 10; i++)
                intArr2[i] = i + 10;
            return intArr2;
        };

        private readonly static Func<string[]> s_stringArray1 = () =>
        {
            string[] stringArr1 = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();
            return stringArr1;
        };

        private readonly static Func<string[]> s_stringArray2 = () =>
        {
            string[] stringArr2 = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr2[i] = "SomeTestString" + (i + 10).ToString();
            return stringArr2;
        };


        [Fact]
        public static void AddToEmptyList()
        {
            Driver<int> driver = new Driver<int>();
            driver.AddToEmptyList(s_intArray(), driver.ConstructMyIEnumerable);
            driver.AddToEmptyList(s_intArray(), driver.ConstructTestCollection);

            Driver<string> driver2 = new Driver<string>();
            driver2.AddToEmptyList(s_stringArray1(), driver2.ConstructMyIEnumerable);
            driver2.AddToEmptyList(s_stringArray1(), driver2.ConstructTestCollection);
        }

        [Fact]
        public static void AddToNonEmptyList()
        {
            Driver<int> driver = new Driver<int>();
            driver.AddToNonEmptyList(s_intArray(), s_intArray2(), driver.ConstructMyIEnumerable);
            driver.AddToNonEmptyList(s_intArray(), s_intArray2(), driver.ConstructTestCollection);

            Driver<string> driver2 = new Driver<string>();
            driver2.AddToNonEmptyList(s_stringArray1(), s_stringArray2(), driver2.ConstructMyIEnumerable);
            driver2.AddToNonEmptyList(s_stringArray1(), s_stringArray2(), driver2.ConstructTestCollection);
        }

        [Fact]
        public static void AddNullRange()
        {
            Driver<string> driver2 = new Driver<string>();
            driver2.AddNullRange();
        }
    }

    #region Helper Classes

    public class MyIEnumerable<T> : IEnumerable<T>
    {
        private T[] _items;

        public MyIEnumerable(T[] items)
        {
            _items = items;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new MyEnumerator<T>(_items);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return new MyEnumerator<T>(_items);
        }
    }

    public class MyEnumerator<T> : IEnumerator<T>
    {
        private int _currentIndex;
        private T _current;
        private T[] _items;
        private int _itemsLength;

        public MyEnumerator(T[] items)
        {
            _currentIndex = -1;
            _items = items;
            _itemsLength = items.Length;
        }

        public void Dispose() { }

        public T Current
        {
            get
            {
                return _current;
            }
        }

        Object System.Collections.IEnumerator.Current
        {
            get
            {
                return _current;
            }
        }

        public bool MoveNext()
        {
            ++_currentIndex;

            if (_currentIndex < _itemsLength)
            {
                _current = _items[_currentIndex];
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _currentIndex = -1;
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

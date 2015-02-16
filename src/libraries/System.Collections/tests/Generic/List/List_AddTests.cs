// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace List_List_AddTests
{    /// <summary>
     /// Performs all the tests and validations.
     /// </summary>
    public class Driver<T>
    {
        public void AddToEmptyList(T[] items)
        {
            List<T> list = new List<T>();
            for (int i = 0; i < items.Length; i++)
                list.Add(items[i]);

            for (int i = 0; i < items.Length; i++)
                Assert.Equal(items[i], list[i]); //"Expected items in List to equal to expected. index: " + i

            list = new List<T>(items.Length);
            for (int i = 0; i < items.Length; i++)
                list.Add(items[i]);

            for (int i = 0; i < items.Length; i++)
                Assert.Equal(items[i], list[i]); //"Expected items in List to equal to expected. index: " + i
        }

        public void AddToNonEmptyList(T[] itemsX, T[] itemsY)
        {
            List<T> list = new List<T>(new TestCollection<T>(itemsX));
            for (int i = 0; i < itemsY.Length; i++)
                list.Add(itemsY[i]);

            for (int i = 0; i < itemsX.Length; i++)
                Assert.Equal(itemsX[i], list[i]); //"Expected items in List to equal to expected. index: " + i

            for (int i = itemsX.Length; i < itemsX.Length + itemsY.Length; i++)
                Assert.Equal(itemsY[i - itemsX.Length], list[i]); //"Expected items in List to equal expected. index: " + i
        }

        public void AddToEmptyListAboveCapacity(T[] items)
        {
            List<T> list = new List<T>(items.Length / 2);
            for (int i = 0; i < items.Length; i++)
                list.Add(items[i]);

            for (int i = 0; i < items.Length; i++)
                Assert.Equal(items[i], list[i]); //"Expected items in List to equal to expected. index: " + i
        }

        public void AddToNonEmptyListAboveCapacity(T[] itemsX, T[] itemsY)
        {
            //This is the same as AddToNonEmptyList
            List<T> list = new List<T>(new TestCollection<T>(itemsX));

            for (int i = 0; i < itemsY.Length; i++)
                list.Add(itemsY[i]);

            for (int i = 0; i < itemsX.Length; i++)
                Assert.Equal(itemsX[i], list[i]); //"Expected items in List to equal to expected. index: " + i

            for (int i = itemsX.Length; i < itemsX.Length + itemsY.Length; i++)
                Assert.Equal(itemsY[i - itemsX.Length], list[i]); //"Expected items in List to equal expected. index: " + i);
        }

        public void AddMultipleSameValue(T[] items, int sameFactor, int sameIndex)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            int sameCountExpected = (sameFactor * 2) + 1;
            int sameCountActual = 0;
            T sameVal = items[sameIndex];
            for (int i = 0; i < sameFactor; i++)
            {
                list.AddRange(new TestCollection<T>(items));
                list.Add(sameVal);
            }

            foreach (T t in list)
            {
                if (t.Equals(sameVal))
                    sameCountActual++;
            }

            Assert.Equal(sameCountExpected, sameCountActual); //"Expected count of multiple instanes to be the same."
        }

        public void AddRemoveSameValue(T[] items, int sameFactor, int sameIndex)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            int sameCountExpected = 0;
            int sameCountActual = 0;
            T sameVal = items[sameIndex];
            for (int i = 0; i < sameFactor; i++)
            {
                list.AddRange(new TestCollection<T>(items));
                list.Add(sameVal);
            }

            for (int i = 0; i < (sameFactor * 2) + 1; i++)
                list.Remove(sameVal);

            foreach (T t in list)
            {
                if (t.Equals(sameVal))
                    sameCountActual++;
            }

            Assert.Equal(sameCountExpected, sameCountActual); //"Expected count of multiple instanes left after remove to be the same."
        }

        public void AddNullValueWhenReference(T value)
        {
            if ((object)value != null)
                throw new ArgumentException("invalid argument passed to testcase");

            List<T> list = new List<T>();
            list.Add(value);
            Assert.Null((object)list[0]); //"Expected the first item to null"
        }

        public void NonGenericIListAddToEmptyList(T[] items)
        {
            List<T> list = new List<T>();
            IList _ilist = list;
            for (int i = 0; i < items.Length; i++)
                _ilist.Add(items[i]);

            for (int i = 0; i < items.Length; i++)
                Assert.Equal(items[i], _ilist[i]); //"Expected items to be same in the List. index: " + i

            list = new List<T>(items.Length);
            _ilist = list;
            for (int i = 0; i < items.Length; i++)
                _ilist.Add(items[i]);

            for (int i = 0; i < items.Length; i++)
                Assert.Equal(items[i], _ilist[i]); //"Expected items to be same in the List. index: " + i
        }

        public void NonGenericIListAddToNonEmptyList(T[] itemsX, T[] itemsY)
        {
            List<T> list = new List<T>(new TestCollection<T>(itemsX));
            IList _ilist = list;
            for (int i = 0; i < itemsY.Length; i++)
                _ilist.Add(itemsY[i]);

            for (int i = 0; i < itemsX.Length; i++)
                Assert.Equal(itemsX[i], _ilist[i]); //"Expected items to be same in the List. index: " + i

            for (int i = itemsX.Length; i < itemsX.Length + itemsY.Length; i++)
                Assert.Equal(itemsY[i - itemsX.Length], _ilist[i]); //"Expected items to be same in the List. index: " + i
        }

        public void NonGenericIListAddToEmptyListAboveCapacity(T[] items)
        {
            List<T> list = new List<T>(items.Length / 2);
            IList _ilist = list;
            for (int i = 0; i < items.Length; i++)
                _ilist.Add(items[i]);
            for (int i = 0; i < items.Length; i++)
                Assert.Equal(items[i], _ilist[i]); //"Expected items to be same in the List. index: " + i
        }

        public void NonGenericIListAddToNonEmptyListAboveCapacity(T[] itemsX, T[] itemsY)
        {
            //This is the same as NonGenericIListAddToNonEmptyList
            List<T> list = new List<T>(new TestCollection<T>(itemsX));
            IList _ilist = list;

            for (int i = 0; i < itemsY.Length; i++)
                _ilist.Add(itemsY[i]);

            for (int i = 0; i < itemsX.Length; i++)
                Assert.Equal(itemsX[i], _ilist[i]); //"Expected items to be same in the List. index: " + i

            for (int i = itemsX.Length; i < itemsX.Length + itemsY.Length; i++)
                Assert.Equal(itemsY[i - itemsX.Length], _ilist[i]); //"Expected items to be same in the List. index: " + i
        }

        public void NonGenericIListAddMultipleSameValue(T[] items, int sameFactor, int sameIndex)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            IList _ilist = list;
            int sameCountExpected = (sameFactor * 2) + 1;
            int sameCountActual = 0;
            T sameVal = items[sameIndex];
            for (int i = 0; i < sameFactor; i++)
            {
                list.AddRange(new TestCollection<T>(items));
                _ilist.Add(sameVal);
            }

            foreach (T t in list)
            {
                if (t.Equals(sameVal))
                    sameCountActual++;
            }
            Assert.Equal(sameCountExpected, sameCountActual); //"Expected count of multiple instanes left after add to be the same."
        }

        public void NonGenericIListAddRemoveSameValue(T[] items, int sameFactor, int sameIndex)
        {
            List<T> list = new List<T>(new TestCollection<T>(items));
            IList _ilist = list;
            int sameCountExpected = 0;
            int sameCountActual = 0;
            T sameVal = items[sameIndex];
            for (int i = 0; i < sameFactor; i++)
            {
                list.AddRange(new TestCollection<T>(items));
                _ilist.Add(sameVal);
            }

            for (int i = 0; i < (sameFactor * 2) + 1; i++)
                _ilist.Remove(sameVal);

            foreach (T t in list)
            {
                if (t.Equals(sameVal))
                    sameCountActual++;
            }

            Assert.Equal(sameCountExpected, sameCountActual); //"Expected count of multiple instanes left after remove to be the same."
        }

        public void NonGenericIListAddNullValueWhenReference(T value)
        {
            if ((object)value != null)
                throw new ArgumentException("invalid argument passed to testcase");

            List<T> list = new List<T>();
            IList _ilist = list;
            _ilist.Add(value);
            Assert.Null((object)_ilist[0]); //"Expected the first item to be null."
        }

        public void NonGenericIListAddInvalidValue()
        {
            List<T> list = new List<T>();
            IList _ilist = list;

            Assert.Throws<ArgumentException>(() => _ilist.Add(6)); //"No Exception when ArgumentException is expected while attempting to call IList.Add with bad type."
        }
    }
    public class List_AddTests
    {
        private readonly static Func<int[]> s_intArray = () =>
        {
            int[] intArr1 = new int[100];
            for (int i = 0; i < 100; i++)
                intArr1[i] = i;
            return intArr1;
        };
        private readonly static Func<int[]> s_intArray2 = () =>
        {
            int[] intArr2 = new int[100];
            for (int i = 0; i < 100; i++)
                intArr2[i] = i + 100;
            return intArr2;
        };

        private readonly static Func<string[]> s_stringArray1 = () =>
        {
            string[] stringArr1 = new string[100];
            for (int i = 0; i < 100; i++)
                stringArr1[i] = "SomeTestString" + i.ToString();

            return stringArr1;
        };

        private readonly static Func<string[]> s_stringArray2 = () =>
        {
            string[] stringArr2 = new string[100];
            for (int i = 0; i < 100; i++)
                stringArr2[i] = "SomeTestString" + (i + 100).ToString();

            return stringArr2;
        };

        [Fact]
        public static void AddToEmptyList()
        {
            Driver<int> driver = new Driver<int>();
            driver.AddToEmptyList(s_intArray());

            Driver<string> driver2 = new Driver<string>();
            driver2.AddToEmptyList(s_stringArray1());
        }

        [Fact]
        public static void AddToNonEmptyList()
        {
            Driver<int> driver = new Driver<int>();
            driver.AddToNonEmptyList(s_intArray(), s_intArray2());

            Driver<string> driver2 = new Driver<string>();
            driver2.AddToNonEmptyList(s_stringArray1(), s_stringArray2());
        }

        [Fact]
        public static void AddToEmptyListAboveCapacity()
        {
            Driver<int> driver = new Driver<int>();
            driver.AddToEmptyListAboveCapacity(s_intArray());

            Driver<string> driver2 = new Driver<string>();
            driver2.AddToEmptyListAboveCapacity(s_stringArray1());
        }

        [Fact]
        public static void AddToNonEmptyListAboveCapacity()
        {
            Driver<int> driver = new Driver<int>();
            driver.AddToNonEmptyListAboveCapacity(s_intArray(), s_intArray2());

            Driver<string> driver2 = new Driver<string>();
            driver2.AddToNonEmptyListAboveCapacity(s_stringArray1(), s_stringArray2());
        }

        [Fact]
        public static void AddMultipleSameValue()
        {
            Driver<int> driver = new Driver<int>();
            driver.AddMultipleSameValue(s_intArray(), 3, 33);
            driver.AddMultipleSameValue(s_intArray(), 7, 12);
            driver.AddMultipleSameValue(s_intArray(), 43, 73);

            Driver<string> driver2 = new Driver<string>();
            driver2.AddMultipleSameValue(s_stringArray1(), 3, 33);
            driver2.AddMultipleSameValue(s_stringArray1(), 7, 0);
            driver2.AddMultipleSameValue(s_stringArray1(), 1, 99);
        }

        [Fact]
        public static void AddRemoveSameValue()
        {
            Driver<int> driver = new Driver<int>();
            driver.AddRemoveSameValue(s_intArray(), 3, 1);
            driver.AddRemoveSameValue(s_intArray(), 15, 65);

            Driver<string> driver2 = new Driver<string>();
            driver2.AddRemoveSameValue(s_stringArray1(), 3, 0);
            driver2.AddRemoveSameValue(s_stringArray1(), 15, 99);
        }

        [Fact]
        public static void NonGenericIListAddToEmptyList()
        {
            Driver<int> driver = new Driver<int>();
            driver.NonGenericIListAddToEmptyList(s_intArray());

            Driver<string> driver2 = new Driver<string>();
            driver2.NonGenericIListAddToEmptyList(s_stringArray1());
        }

        [Fact]
        public static void NonGenericIListAddToNonEmptyList()
        {
            Driver<int> driver = new Driver<int>();
            driver.NonGenericIListAddToNonEmptyList(s_intArray(), s_intArray2());

            Driver<string> driver2 = new Driver<string>();
            driver2.NonGenericIListAddToNonEmptyList(s_stringArray1(), s_stringArray2());
        }

        [Fact]
        public static void NonGenericIListAddToEmptyListAboveCapacity()
        {
            Driver<int> driver = new Driver<int>();
            driver.NonGenericIListAddToEmptyListAboveCapacity(s_intArray());

            Driver<string> driver2 = new Driver<string>();
            driver2.NonGenericIListAddToEmptyListAboveCapacity(s_stringArray1());
        }

        [Fact]
        public static void NonGenericIListAddToNonEmptyListAboveCapacity()
        {
            Driver<int> driver = new Driver<int>();
            driver.NonGenericIListAddToNonEmptyListAboveCapacity(s_intArray(), s_intArray2());

            Driver<string> driver2 = new Driver<string>();
            driver2.NonGenericIListAddToNonEmptyListAboveCapacity(s_stringArray1(), s_stringArray2());
        }

        [Fact]
        public static void NonGenericIListAddMultipleSameValue()
        {
            Driver<int> driver = new Driver<int>();
            driver.NonGenericIListAddMultipleSameValue(s_intArray(), 3, 33);
            driver.NonGenericIListAddMultipleSameValue(s_intArray(), 7, 12);
            driver.NonGenericIListAddMultipleSameValue(s_intArray(), 43, 73);

            Driver<string> driver2 = new Driver<string>();
            driver2.NonGenericIListAddMultipleSameValue(s_stringArray1(), 3, 33);
            driver2.NonGenericIListAddMultipleSameValue(s_stringArray1(), 7, 0);
            driver2.NonGenericIListAddMultipleSameValue(s_stringArray1(), 1, 99);
        }

        [Fact]
        public static void NonGenericIListAddRemoveSameValue()
        {
            Driver<int> driver = new Driver<int>();
            driver.NonGenericIListAddRemoveSameValue(s_intArray(), 3, 1);
            driver.NonGenericIListAddRemoveSameValue(s_intArray(), 15, 65);

            Driver<string> driver2 = new Driver<string>();
            driver2.NonGenericIListAddRemoveSameValue(s_stringArray1(), 3, 0);
            driver2.NonGenericIListAddRemoveSameValue(s_stringArray1(), 15, 99);
        }

        [Fact]
        public static void AddNullValueWhenReference()
        {
            Driver<string> driver2 = new Driver<string>();
            driver2.AddNullValueWhenReference(null);
            driver2.NonGenericIListAddNullValueWhenReference(null);
        }

        [Fact]
        public static void NonGenericIListAddInvalidValue()
        {
            Driver<string> driver2 = new Driver<string>();
            driver2.NonGenericIListAddInvalidValue();
        }

        //Regression Test for DDB 125706
        [Fact]
        public static void AddingNullElementAfterOtherElementDoesNotFail()
        {
            IList nullableBoolList = new List<bool?>();
            nullableBoolList.Add(true); // works
            try
            {
                nullableBoolList.Add(null); // should not fail
            }
            catch (ArgumentException)
            {
                Assert.True(false); //"Error: DDB 125706 repro, bug is not fixed."
            }
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
}

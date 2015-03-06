// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Xunit;

namespace List_List_CopyToTests
{
    public class List_CopyToTests
    {
        [Fact]
        public static void CopyTo_Simple()
        {
            int[] intArr = new int[10];
            for (int i = 0; i < 10; i++)
                intArr[i] = i;

            List<int> intList = new List<int>(new TestCollection<int>(intArr));

            int[] copy = new int[intArr.Length];
            intList.CopyTo(copy);
            for (int i = 0; i < intArr.Length; i++)
            {
                Assert.Equal(intArr[i], copy[i]); //"Expect the same contents from original array and the copy."
                Assert.Equal(intArr[i], intList[i]); //"Expect the same contents from original array and the list."
            }
        }

        [Fact]
        public static void CopyTo_Simple2()
        {
            int[] intArr = new int[10];
            for (int i = 0; i < 10; i++)
                intArr[i] = i;

            List<int> intList = new List<int>(new TestCollection<int>(intArr));

            int[] copy = new int[intArr.Length];
            intList.CopyTo(copy, 0);
            for (int i = 0; i < intArr.Length; i++)
            {
                Assert.Equal(intArr[i], copy[i]); //"Expect the same contents from original array and the copy."
                Assert.Equal(intArr[i], intList[i]); //"Expect the same contents from original array and the list."
            }
        }

        [Fact]
        public static void CopyTo_Simple3()
        {
            int[] intArr = new int[10];
            for (int i = 0; i < 10; i++)
                intArr[i] = i;

            List<int> intList = new List<int>(new TestCollection<int>(intArr));

            int[] copy = new int[intArr.Length];
            intList.CopyTo(0, copy, 0, intList.Count);
            for (int i = 0; i < intArr.Length; i++)
            {
                Assert.Equal(intArr[i], copy[i]); //"Expect the same contents from original array and the copy."
                Assert.Equal(intArr[i], intList[i]); //"Expect the same contents from original array and the list."
            }
        }

        /// <summary>
        /// Start copying into the middle of the output array.
        /// </summary>
        [Fact]
        public static void CopyTo_Simple2_Middle()
        {
            string[] stringArr = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr[i] = "SomeTestString" + i.ToString();

            List<string> stringList = new List<string>(new TestCollection<string>(stringArr));

            int index = 4;
            int extra = 10;
            int totalSize = stringArr.Length + index + extra;
            string[] copy = new string[totalSize];
            stringList.CopyTo(copy, index);
            for (int i = 0; i < totalSize; i++)
            {
                if (i < index)
                    Assert.Null(copy[i]); //"Should be null because nothing has been copied into the array yet."
                else if (i >= index && i < (stringArr.Length + index))
                    Assert.Equal(stringArr[i - index], copy[i]); //"Expect the same contents from original array and the copy."
                else
                    Assert.Null(copy[i]); //"Should be null because nothing has been copied into the array yet."

                if (i < stringArr.Length)
                    Assert.Equal(stringArr[i], stringList[i]); //"Expect the same contents from original array and the copy."
            }
        }

        [Fact]
        public static void CopyTo_Empty()
        {
            string[] good = new string[10];
            new List<string>().CopyTo(good);
            for (int i = 0; i < good.Length; i++)
                Assert.Null(good[i]); //"Should have nothing in the array."
        }

        [Fact]
        public static void CopyTo_Empty2()
        {
            string[] good = new string[10];
            new List<string>().CopyTo(good, 0);
            for (int i = 0; i < good.Length; i++)
                Assert.Null(good[i]); //"Should have nothing in the array."
        }

        [Fact]
        public static void CopyTo_Empty3()
        {
            string[] good = new string[10];
            new List<string>().CopyTo(0, good, 0, 0);
            for (int i = 0; i < good.Length; i++)
                Assert.Null(good[i]); //"Should have nothing in the array."
        }

        [Fact]
        public static void CopyTo_3Index()
        {
            int[] intArr = new int[10];
            for (int i = 0; i < 10; i++)
                intArr[i] = i;
            List<int> intList = new List<int>(new TestCollection<int>(intArr));

            for (int i = 0; i < 10; i++)
            {
                int[] output = new int[intArr.Length + i];
                intList.CopyTo(i, output, i, intList.Count - i);
                for (int j = i; j < intArr.Length; j++)
                    Assert.Equal(intList[j], output[j]); //"Should be equal to the same contents as the list."
            }

            string[] stringArr = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr[i] = "StringTest" + i.ToString();

            List<string> stringList = new List<string>(new TestCollection<string>(stringArr));
            for (int i = 0; i < 10; i++)
            {
                string[] output = new string[stringArr.Length];
                int startIndex = i;
                int lastIndex = stringArr.Length - i;
                stringList.CopyTo(i, output, 0, stringArr.Length - i);
                for (int j = 0; j < stringArr.Length; j++)
                {
                    if (j >= lastIndex)
                        Assert.Null(output[j]); //"Should have nothing here, but has value: " + output[j]
                    else
                        Assert.Equal(stringList[j + startIndex], output[j]); //"Should be equal to the same contents as the list."
                }
            }
        }

        [Fact]
        public static void CopyTo_3ArrayIndex()
        {
            int[] intArr = new int[10];
            List<int> intList = new List<int>(new TestCollection<int>(intArr));
            //
            //Second parameter validation tests
            //

            for (int i = 0; i < 10; i++)
            {
                int[] arr2 = new int[intArr.Length + i];
                intList.CopyTo(0, arr2, i, intList.Count);
                for (int j = i; j < intArr.Length + i; j++)
                    Assert.Equal(intList[j - i], arr2[j]); //"should have equal contents as source list."
            }
        }

        [Fact]
        public static void CopyTo_3Count()
        {
            string[] stringArr = new string[10];
            for (int i = 0; i < 10; i++)
                stringArr[i] = "StringTest" + i.ToString();
            List<string> stringList = new List<string>(new TestCollection<string>(stringArr));

            for (int i = 0; i < 10; i++)
            {
                string[] output = new string[stringArr.Length];
                int lastIndex = i;
                stringList.CopyTo(0, output, 0, i);
                for (int j = 0; j < stringArr.Length; j++)
                {
                    if (j >= lastIndex)
                        Assert.Null(output[j]); //"Should have nothing here, but has value: " + output[j]
                    else
                        Assert.Equal(stringList[j], output[j]); //"Should be equal to the same contents as the list."
                }
            }
        }

        [Fact]
        public static void CopyTo_Negative()
        {
            int[] intArr = new int[10];
            for (int i = 0; i < 10; i++)
                intArr[i] = i;

            List<int> IntList = new List<int>(new TestCollection<int>(intArr));
            Assert.Throws<ArgumentNullException>(() => IntList.CopyTo(null)); //"ArgumentNullException expected on null array."

            int[] bad = new int[4];
            Assert.Throws<ArgumentException>(() => IntList.CopyTo(bad)); //"ArgumentException expected when output array is too small."
        }

        [Fact]
        public static void CopyTo_Negative2()
        {
            int[] intArr = new int[10];
            List<int> intList = new List<int>(new TestCollection<int>(intArr));

            for (int i = 0; i < 10; i++)
            {
                int[] copy = new int[intArr.Length + i];
                Assert.Throws<ArgumentException>(() => intList.CopyTo(copy, i + 1)); //"ArgumentException expected when output array cannot fit contents + index."
            }

            int[] output = new int[intArr.Length];
            Assert.Throws<ArgumentOutOfRangeException>(() => intList.CopyTo(output, -1)); //"ArgumentOutOfRangeException expected for negative index."

            Assert.Throws<ArgumentOutOfRangeException>(() => intList.CopyTo(output, int.MinValue)); //"ArgumentException expected for negative index."

            Assert.Throws<ArgumentNullException>(() => intList.CopyTo(null, 0)); //"ArgumentNullException expected for null array."

            int[] bad = new int[4];
            Assert.Throws<ArgumentException>(() => intList.CopyTo(bad, 0)); //"ArgumentException expected when output array cannot fit."
        }

        [Fact]
        public static void CopyTo_Negative3()
        {
            int[] intArr = new int[10];
            List<int> intList = new List<int>(new TestCollection<int>(intArr));

            // 1st parameter validation
            for (int i = 0; i < 10; i++)
            {
                int[] output = new int[intArr.Length];
                Assert.Throws<ArgumentException>(() => intList.CopyTo(i, output, 0, intList.Count - i + 1)); //"ArgumentException should be thrown if output array cannot fit the list + index"
            }

            int[] copy = new int[intArr.Length];
            Assert.Throws<ArgumentOutOfRangeException>(() => intList.CopyTo(-1, copy, 0, intList.Count)); //"Should throw ArgumentOutOfRangeException on negative index"

            Assert.Throws<ArgumentException>(() => intList.CopyTo(int.MinValue, copy, 0, intList.Count)); //"Should throw ArgumentException on negative index"

            // 2nd parameter validation
            Assert.Throws<ArgumentNullException>(() => intList.CopyTo(0, null, 0, intArr.Length)); //"ArgumentNullException expected when output is null."

            // 3rd parameter validation
            for (int i = 0; i < 10; i++)
            {
                int[] arr3 = new int[intArr.Length + i];
                Assert.Throws<ArgumentException>(() => intList.CopyTo(0, arr3, i + 1, intList.Count)); //"Should throw ArgumentException if arrayIndex + list cannot fit."
            }

            Assert.Throws<ArgumentOutOfRangeException>(() => intList.CopyTo(0, copy, -1, intList.Count)); //"Should throw ArgumentOutOfRangeException on negative index"

            Assert.Throws<ArgumentOutOfRangeException>(() => intList.CopyTo(0, copy, int.MinValue, intList.Count)); //"Should throw ArgumentOutOfRangeException on negative index"

            // Fourth parameter validation tests
            Assert.Throws<ArgumentOutOfRangeException>(() => intList.CopyTo(0, copy, 0, -1)); //"Should throw ArgumentOutOfRangeException on negative count."
            Assert.Throws<ArgumentOutOfRangeException>(() => intList.CopyTo(0, copy, 0, int.MinValue)); //"Should throw ArgumentOutOfRangeException on negative count."

            Assert.Throws<ArgumentException>(() => intList.CopyTo(0, copy, 0, int.MaxValue)); //"ArgumentException when count is greater than source List."

            copy = new int[intArr.Length - 5];
            Assert.Throws<ArgumentException>(() => intList.CopyTo(0, copy, 0, intArr.Length)); //"ArgumentException when output array is smaller than input array.."

            copy = new int[intArr.Length + 3];
            Assert.Throws<ArgumentException>(() => intList.CopyTo(0, copy, 0, 11)); //"ArgumentException when count is greater than source List."
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
    #endregion
}

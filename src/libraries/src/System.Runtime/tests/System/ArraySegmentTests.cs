// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Tests;
using System.Linq;
using Xunit;

namespace System.Tests
{
    public abstract partial class ArraySegment_Tests<T> : IList_Generic_Tests<T>
    {
        #region IList<T> Helper Methods

        protected override IList<T> GenericIListFactory()
        {
            return Factory();
        }

        protected override IList<T> GenericIListFactory(int count)
        {
            return Factory(count * 2, count / 2, count);
        }

        protected override bool Enumerator_Current_UndefinedOperation_Throws => true;
        protected override bool Enumerator_ModifiedDuringEnumeration_ThrowsInvalidOperationException => false;
        protected override bool IsReadOnly_ValidityValue => true;
        protected override bool AddRemoveClear_ThrowsNotSupported => true;

        #endregion

        #region List<T> Helper Methods

        protected virtual ArraySegment<T> Factory()
        {
            return new ArraySegment<T>();
        }

        protected virtual ArraySegment<T> Factory(int count, int offset, int length)
        {
            T[] array = CreateEnumerable(EnumerableType.List, null, count, 0, 0).ToArray();
            ArraySegment<T> segment = new ArraySegment<T>(array, offset, length);

            Assert.Same(array, segment.Array);
            Assert.Equal(offset, segment.Offset);
            Assert.Equal(length, segment.Count);

            return segment;
        }

        protected void VerifySegment(List<T> expected, ArraySegment<T> segment)
        {
            Assert.Equal(expected.Count, segment.Count);

            for (int i = 0; i < expected.Count; ++i)
            {
                Assert.True(expected[i] == null ? (segment as IList<T>)[i] == null : expected[i].Equals((segment as IList<T>)[i]));
            }
        }

        #endregion

        [Fact]
        public void Ctor_Empty()
        {
            var segment = new ArraySegment<T>();
            Assert.Null(segment.Array);
            Assert.Equal(0, segment.Offset);
            Assert.Equal(0, segment.Count);

            T[] array = new T[10];
            segment = new ArraySegment<T>(array, 10, 0);
            Assert.Same(array, segment.Array);
            Assert.Equal(10, segment.Offset);
            Assert.Equal(0, segment.Count);
        }

        [Fact]
        public static void Ctor_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("array", () => new ArraySegment<T>(null));
            AssertExtensions.Throws<ArgumentNullException>("array", () => new ArraySegment<T>(null, -1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => new ArraySegment<T>(new T[10], -1, 0)); // Offset < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => new ArraySegment<T>(new T[10], 0, -1)); // Count < 0
            AssertExtensions.Throws<ArgumentException>(null, () => new ArraySegment<T>(new T[10], 10, 1)); // Offset + count > array.Length
            AssertExtensions.Throws<ArgumentException>(null, () => new ArraySegment<T>(new T[10], 9, 2)); // Offset + count > array.Length
        }
    }

    public class ArraySegment_Tests_string : ArraySegment_Tests<string>
    {
        protected override string CreateT(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }
    }

    public class ArraySegment_Tests_int : ArraySegment_Tests<int>
    {
        protected override int CreateT(int seed)
        {
            Random rand = new Random(seed);
            return rand.Next();
        }
    }


    public static partial class ArraySegment_Tests
    {
        public static IEnumerable<object[]> Equals_TestData()
        {
            var intArray1 = new int[] { 7, 8, 9, 10, 11, 12 };
            var intArray2 = new int[] { 7, 8, 9, 10, 11, 12 };

            yield return new object[] { new ArraySegment<int>(intArray1), new ArraySegment<int>(intArray1), true };
            yield return new object[] { new ArraySegment<int>(intArray1), new ArraySegment<int>(intArray1, 0, intArray1.Length), true };

            yield return new object[] { new ArraySegment<int>(intArray1, 2, 3), new ArraySegment<int>(intArray1, 2, 3), true };
            yield return new object[] { new ArraySegment<int>(intArray1, 3, 3), new ArraySegment<int>(intArray1, 2, 3), false };
            yield return new object[] { new ArraySegment<int>(intArray1, 2, 4), new ArraySegment<int>(intArray1, 2, 3), false };

            yield return new object[] { new ArraySegment<int>(intArray1, 2, 4), new ArraySegment<int>(intArray2, 2, 3), false };

            yield return new object[] { new ArraySegment<int>(intArray1), intArray1, false };
            yield return new object[] { new ArraySegment<int>(intArray1), null, false };
            yield return new object[] { new ArraySegment<int>(intArray1, 2, 4), null, false };
        }

        [Theory]
        [MemberData(nameof(Equals_TestData))]
        public static void Equals(ArraySegment<int> segment1, object obj, bool expected)
        {
            if (obj is ArraySegment<int>)
            {
                ArraySegment<int> segment2 = (ArraySegment<int>)obj;
                Assert.Equal(expected, segment1.Equals(segment2));
                Assert.Equal(expected, segment1 == segment2);
                Assert.Equal(!expected, segment1 != segment2);

                Assert.Equal(expected, segment1.GetHashCode().Equals(segment2.GetHashCode()));
            }
            Assert.Equal(expected, segment1.Equals(obj));
        }

        [Fact]
        public static void IList_GetSetItem()
        {
            var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
            var segment = new ArraySegment<int>(intArray, 2, 3);
            IList<int> iList = segment;

            Assert.Equal(segment.Count, iList.Count);
            for (int i = 0; i < iList.Count; i++)
            {
                Assert.Equal(intArray[i + segment.Offset], iList[i]);

                iList[i] = 99;
                Assert.Equal(99, iList[i]);
                Assert.Equal(99, intArray[i + segment.Offset]);
            }
        }

        [Fact]
        public static void IList_GetSetItem_Invalid()
        {
            IList<int> iList = new ArraySegment<int>();
            Assert.Throws<InvalidOperationException>(() => iList[0]); // Array is null
            Assert.Throws<InvalidOperationException>(() => iList[0] = 0); // Array is null

            var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
            iList = new ArraySegment<int>(intArray, 2, 3);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => iList[-1]); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => iList[iList.Count]); // Index >= list.Count

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => iList[-1] = 0); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => iList[iList.Count] = 0); // Index >= list.Count
        }

        [Fact]
        public static void IReadOnlyList_GetItem()
        {
            var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
            var seg = new ArraySegment<int>(intArray, 2, 3);
            IReadOnlyList<int> iList = seg;
            for (int i = 0; i < iList.Count; i++)
            {
                Assert.Equal(intArray[i + seg.Offset], iList[i]);
            }
        }

        [Fact]
        public static void IReadOnlyList_GetItem_Invalid()
        {
            IReadOnlyList<int> iList = new ArraySegment<int>();
            Assert.Throws<InvalidOperationException>(() => iList[0]); // Array is null

            var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
            iList = new ArraySegment<int>(intArray, 2, 3);
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => iList[-1]); // Index < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => iList[iList.Count]); // List >= seg.Count
        }

        [Fact]
        public static void IList_IndexOf()
        {
            var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
            var segment = new ArraySegment<int>(intArray, 2, 3);
            IList<int> iList = segment;

            for (int i = segment.Offset; i < segment.Count; i++)
            {
                Assert.Equal(i - segment.Offset, iList.IndexOf(intArray[i]));
            }
            Assert.Equal(-1, iList.IndexOf(9999)); // No such value
            Assert.Equal(-1, iList.IndexOf(7)); // No such value in range
        }

        [Fact]
        public static void IList_IndexOf_NullArray_ThrowsInvalidOperationException()
        {
            IList<int> iList = new ArraySegment<int>();
            Assert.Throws<InvalidOperationException>(() => iList.IndexOf(0)); // Array is null
        }

        [Fact]
        public static void IList_ModifyingCollection_ThrowsNotSupportedException()
        {
            var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
            var segment = new ArraySegment<int>(intArray, 2, 3);
            IList<int> iList = segment;

            Assert.True(iList.IsReadOnly);
            Assert.Throws<NotSupportedException>(() => iList.Add(2));
            Assert.Throws<NotSupportedException>(() => iList.Insert(0, 0));
            Assert.Throws<NotSupportedException>(() => iList.Clear());
            Assert.Throws<NotSupportedException>(() => iList.Remove(2));
            Assert.Throws<NotSupportedException>(() => iList.RemoveAt(2));
        }

        [Fact]
        public static void IList_Contains_NullArray_ThrowsInvalidOperationException()
        {
            IList<int> iList = new ArraySegment<int>();
            Assert.Throws<InvalidOperationException>(() => iList.Contains(0)); // Array is null
        }

        [Fact]
        public static void IList_GetEnumerator()
        {
            var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
            ArraySegment<int> segment = new ArraySegment<int>(intArray, 2, 3);

            //ArraySegment<int>.Enumerator enumerator = segment.GetEnumerator();
            IEnumerator<int> enumerator = (segment as IEnumerable<int>).GetEnumerator();
            for (int i = 0; i < 2; i++)
            {
                int counter = 0;
                while (enumerator.MoveNext())
                {
                    Assert.Equal(intArray[counter + 2], enumerator.Current);
                    counter++;
                }
                Assert.Equal(segment.Count, counter);

                (enumerator as IEnumerator<int>).Reset();
            }
        }
    }
}

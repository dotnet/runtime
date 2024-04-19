// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

        protected override bool Enumerator_Empty_UsesSingletonInstance => true;
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
        public void Ctor_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("array", () => new ArraySegment<T>(null));
            AssertExtensions.Throws<ArgumentNullException>("array", () => new ArraySegment<T>(null, -1, 1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("offset", () => new ArraySegment<T>(new T[10], -1, 0)); // Offset < 0
            AssertExtensions.Throws<ArgumentOutOfRangeException>("count", () => new ArraySegment<T>(new T[10], 0, -1)); // Count < 0
            AssertExtensions.Throws<ArgumentException>(null, () => new ArraySegment<T>(new T[10], 10, 1)); // Offset + count > array.Length
            AssertExtensions.Throws<ArgumentException>(null, () => new ArraySegment<T>(new T[10], 9, 2)); // Offset + count > array.Length
        }

        [Fact]
        public void CopyTo_Default_ThrowsInvalidOperationException()
        {
            // Source is default
            Assert.Throws<InvalidOperationException>(() => default(ArraySegment<T>).CopyTo(new T[0]));
            Assert.Throws<InvalidOperationException>(() => default(ArraySegment<T>).CopyTo(new T[0], 0));
            Assert.Throws<InvalidOperationException>(() => ((ICollection<T>)default(ArraySegment<T>)).CopyTo(new T[0], 0));
            Assert.Throws<InvalidOperationException>(() => default(ArraySegment<T>).CopyTo(new ArraySegment<T>(new T[0])));

            // Destination is default
            Assert.Throws<InvalidOperationException>(() => new ArraySegment<T>(new T[0]).CopyTo(default(ArraySegment<T>)));
        }

        [Fact]
        public void Empty()
        {
            ArraySegment<T> empty = ArraySegment<T>.Empty;

            // Assert.NotEqual uses its own Comparer, when it is comparing IEnumerables it calls GetEnumerator()
            // ArraySegment<T>.GetEnumerator() throws InvalidOperationException when the array is null and default() returns null
            Assert.True(default(ArraySegment<T>) != empty);

            // Check that two Empty invocations return equal ArraySegments.
            Assert.Equal(empty, ArraySegment<T>.Empty);

            // Check that two Empty invocations return ArraySegments with a cached empty array.
            // An empty array is necessary to ensure that someone doesn't use the indexer to store data in the array Empty refers to.
            Assert.Same(empty.Array, ArraySegment<T>.Empty.Array);
            Assert.Equal(0, empty.Array.Length);
            Assert.Equal(0, empty.Offset);
            Assert.Equal(0, empty.Count);
        }

        [Fact]
        public void GetEnumerator_TypeProperties()
        {
            var arraySegment = new ArraySegment<T>(new T[1], 0, 1);
            var ienumerableoft = (IEnumerable<T>)arraySegment;
            var ienumerable = (IEnumerable)arraySegment;

            ArraySegment<T>.Enumerator enumerator = arraySegment.GetEnumerator();
            Assert.IsType<ArraySegment<T>.Enumerator>(enumerator);
            Assert.IsAssignableFrom<IEnumerator<T>>(enumerator);
            Assert.IsAssignableFrom<IEnumerator>(enumerator);

            IEnumerator<T> ienumeratoroft = ienumerableoft.GetEnumerator();
            IEnumerator ienumerator = ienumerable.GetEnumerator();

            Assert.Equal(enumerator.GetType(), ienumeratoroft.GetType());
            Assert.Equal(ienumeratoroft.GetType(), ienumerator.GetType());
        }

        [Fact]
        public void GetEnumerator_Default_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => default(ArraySegment<T>).GetEnumerator());
        }

        [Fact]
        public void Slice_Default_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => default(ArraySegment<T>).Slice(0));
            Assert.Throws<InvalidOperationException>(() => default(ArraySegment<T>).Slice(0, 0));
        }

        [Fact]
        public void ToArray_Default_ThrowsInvalidOperationException()
        {
            Assert.Throws<InvalidOperationException>(() => default(ArraySegment<T>).ToArray());
        }

        [Fact]
        public void ToArray_Empty_ReturnsSameArray()
        {
            T[] cachedArray = ArraySegment<T>.Empty.ToArray();
            Assert.Same(cachedArray, ArraySegment<T>.Empty.ToArray());
            Assert.Same(cachedArray, new ArraySegment<T>(new T[0]).ToArray());
            Assert.Same(cachedArray, new ArraySegment<T>(new T[1], 0, 0).ToArray());
        }

        [Fact]
        public void ToArray_NonEmptyArray_DoesNotReturnSameArray()
        {
            // Prevent a faulty implementation like `if (Count == 0) { return Array; }`
            var emptyArraySegment = new ArraySegment<T>(new T[1], 0, 0);
            Assert.NotSame(emptyArraySegment.Array, emptyArraySegment.ToArray());
        }

        [Fact]
        public void Cast_FromNullArray_ReturnsDefault()
        {
            ArraySegment<T> fromNull = null;
            Assert.Null(fromNull.Array);
            Assert.Equal(0, fromNull.Offset);
            Assert.Equal(0, fromNull.Count);

            Assert.True(default(ArraySegment<T>) == null);
            Assert.True(new ArraySegment<T>(Array.Empty<T>()) != null);
        }

        [Fact]
        public void Cast_FromValidArray_ReturnsSegmentForWholeArray()
        {
            var array = new T[42];
            ArraySegment<T> fromArray = array;
            Assert.Same(array, fromArray.Array);
            Assert.Equal(0, fromArray.Offset);
            Assert.Equal(42, fromArray.Count);
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
        public static void EqualsTest(ArraySegment<int> segment1, object obj, bool expected)
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

        [Theory]
        [MemberData(nameof(Conversion_FromArray_TestData))]
        public static void Conversion_FromArray(int[] array)
        {
            ArraySegment<int> implicitlyConverted = array;
            ArraySegment<int> explicitlyConverted = (ArraySegment<int>)array;

            var expected = new ArraySegment<int>(array);
            Assert.Equal(expected, implicitlyConverted);
            Assert.Equal(expected, explicitlyConverted);
        }

        public static IEnumerable<object[]> Conversion_FromArray_TestData()
        {
            yield return new object[] { new int[0] };
            yield return new object[] { new int[1] };
        }

        [Theory]
        [MemberData(nameof(ArraySegment_TestData))]
        public static void CopyTo(ArraySegment<int> arraySegment)
        {
            const int CopyPadding = 5;
            const int DestinationSegmentPadding = 3;

            int count = arraySegment.Count;

            var destinationModel = new int[count + 2 * CopyPadding];

            // CopyTo(T[])
            CopyAndInvoke(destinationModel, destination =>
            {
                arraySegment.CopyTo(destination);

                Assert.Equal(Enumerable.Repeat(default(int), 2 * CopyPadding), destination.Skip(count));

                Assert.Equal(arraySegment, destination.Take(count));
            });

            // CopyTo(T[], int)
            CopyAndInvoke(destinationModel, destination =>
            {
                arraySegment.CopyTo(destination, CopyPadding);

                Assert.Equal(Enumerable.Repeat(default(int), CopyPadding), destination.Take(CopyPadding));
                Assert.Equal(Enumerable.Repeat(default(int), CopyPadding), destination.Skip(CopyPadding + count));

                Assert.Equal(arraySegment, destination.Skip(CopyPadding).Take(count));
            });

            // ICollection<T>.CopyTo(T[], int)
            CopyAndInvoke(destinationModel, destination =>
            {
                ((ICollection<int>)arraySegment).CopyTo(destination, CopyPadding);

                Assert.Equal(Enumerable.Repeat(default(int), CopyPadding), destination.Take(CopyPadding));
                Assert.Equal(Enumerable.Repeat(default(int), CopyPadding), destination.Skip(CopyPadding + count));

                Assert.Equal(arraySegment, destination.Skip(CopyPadding).Take(count));
            });

            // CopyTo(ArraySegment<T>)
            CopyAndInvoke(destinationModel, destination =>
            {
                // We want to make sure this overload is handling edge cases correctly, like ArraySegments that
                // do not begin at the array's start, do not end at the array's end, or have a bigger count than
                // the source ArraySegment. Construct an ArraySegment that will test all of these conditions.
                int destinationIndex = DestinationSegmentPadding;
                int destinationCount = destination.Length - 2 * DestinationSegmentPadding;
                var destinationSegment = new ArraySegment<int>(destination, destinationIndex, destinationCount);

                arraySegment.CopyTo(destinationSegment);

                Assert.Equal(Enumerable.Repeat(default(int), destinationIndex), destination.Take(destinationIndex));
                int remainder = destination.Length - destinationIndex - count;
                Assert.Equal(Enumerable.Repeat(default(int), remainder), destination.Skip(destinationIndex + count));

                Assert.Equal(arraySegment, destination.Skip(destinationIndex).Take(count));
            });
        }

        private static void CopyAndInvoke<T>(T[] array, Action<T[]> action) => action(array.ToArray());

        [Theory]
        [MemberData(nameof(ArraySegment_TestData))]
        public static void CopyTo_Invalid(ArraySegment<int> arraySegment)
        {
            int count = arraySegment.Count;

            // ArraySegment.CopyTo calls Array.Copy internally, so the exception parameter names come from there.

            // Destination is null
            AssertExtensions.Throws<ArgumentNullException>("destinationArray", () => arraySegment.CopyTo(null));
            AssertExtensions.Throws<ArgumentNullException>("destinationArray", () => arraySegment.CopyTo(null, 0));

            // Destination index not within range
            AssertExtensions.Throws<ArgumentOutOfRangeException>("destinationIndex", () => arraySegment.CopyTo(new int[0], -1));

            // Destination array too small arraySegment.Count + destinationIndex > destinationArray.Length
            AssertExtensions.Throws<ArgumentException>("destinationArray", () => arraySegment.CopyTo(new int[arraySegment.Count * 2], arraySegment.Count + 1));

            if (arraySegment.Any())
            {
                // Destination not large enough
                AssertExtensions.Throws<ArgumentException>("destinationArray", () => arraySegment.CopyTo(new int[count - 1]));
                AssertExtensions.Throws<ArgumentException>("destinationArray", () => arraySegment.CopyTo(new int[count - 1], 0));
                AssertExtensions.Throws<ArgumentException>("destination", null, () => arraySegment.CopyTo(new ArraySegment<int>(new int[count - 1])));

                // Don't write beyond the limits of the destination in cases where source.Count > destination.Count
                AssertExtensions.Throws<ArgumentException>("destination", null, () => arraySegment.CopyTo(new ArraySegment<int>(new int[count], 1, 0))); // destination.Array can't fit source at destination.Offset
                AssertExtensions.Throws<ArgumentException>("destination", null, () => arraySegment.CopyTo(new ArraySegment<int>(new int[count], 0, count - 1))); // destination.Array can fit source at destination.Offset, but destination can't
            }
        }

        [Theory]
        [MemberData(nameof(ArraySegment_TestData))]
        public static void GetEnumerator(ArraySegment<int> arraySegment)
        {
            int[] array = arraySegment.Array;
            int index = arraySegment.Offset;
            int count = arraySegment.Count;

            ArraySegment<int>.Enumerator enumerator = arraySegment.GetEnumerator();

            var actual = new List<int>();

            while (enumerator.MoveNext())
            {
                actual.Add(enumerator.Current);
            }

            // After MoveNext returns false once, it should return false the second time.
            Assert.False(enumerator.MoveNext());

            IEnumerable<int> expected = array.Skip(index).Take(count);
            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(ArraySegment_TestData))]
        public static void GetEnumerator_Dispose(ArraySegment<int> arraySegment)
        {
            int[] array = arraySegment.Array;
            int index = arraySegment.Offset;

            ArraySegment<int>.Enumerator enumerator = arraySegment.GetEnumerator();

            bool expected = arraySegment.Count > 0;

            // Dispose shouldn't do anything. Call it twice and then assert like nothing happened.
            enumerator.Dispose();
            enumerator.Dispose();

            Assert.Equal(expected, enumerator.MoveNext());
            if (expected)
            {
                Assert.Equal(array[index], enumerator.Current);
            }
        }

        [Theory]
        [MemberData(nameof(ArraySegment_TestData))]
        public static void GetEnumerator_Reset(ArraySegment<int> arraySegment)
        {
            int[] array = arraySegment.Array;
            int index = arraySegment.Offset;
            int count = arraySegment.Count;

            var enumerator = (IEnumerator<int>)arraySegment.GetEnumerator();

            int[] expected = array.Skip(index).Take(count).ToArray();

            // Reset at a variety of different positions to ensure the implementation
            // isn't something like `position -= CONSTANT`.
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < i; j++)
                {
                    if (enumerator.MoveNext())
                    {
                        Assert.Equal(expected[j], enumerator.Current);
                    }
                }

                enumerator.Reset();
            }
        }

        [Theory]
        [MemberData(nameof(ArraySegment_TestData))]
        public static void GetEnumerator_Invalid(ArraySegment<int> arraySegment)
        {
            ArraySegment<int>.Enumerator enumerator = arraySegment.GetEnumerator();

            // Before beginning
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => ((IEnumerator<int>)enumerator).Current);
            Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);

            while (enumerator.MoveNext()) ;

            // After end
            Assert.Throws<InvalidOperationException>(() => enumerator.Current);
            Assert.Throws<InvalidOperationException>(() => ((IEnumerator<int>)enumerator).Current);
            Assert.Throws<InvalidOperationException>(() => ((IEnumerator)enumerator).Current);
        }

        [Theory]
        [MemberData(nameof(ArraySegment_TestData))]
        public static void GetSetItem_InRange(ArraySegment<int> arraySegment)
        {
            int[] array = arraySegment.Array;
            int index = arraySegment.Offset;
            int count = arraySegment.Count;

            int[] expected = array.Skip(index).Take(count).ToArray();

            for (int i = 0; i < count; i++)
            {
                Assert.Equal(expected[i], arraySegment[i]);
                Assert.Equal(expected[i], ((IList<int>)arraySegment)[i]);
                Assert.Equal(expected[i], ((IReadOnlyList<int>)arraySegment)[i]);
            }

            var r = new Random(0);

            for (int i = 0; i < count; i++)
            {
                int next = r.Next(int.MinValue, int.MaxValue);

                // When we modify the underlying array, the indexer should return the updated values.
                array[arraySegment.Offset + i] ^= next;
                Assert.Equal(expected[i] ^ next, arraySegment[i]);

                // When the indexer's set method is called, the underlying array should be modified.
                arraySegment[i] ^= next;
                Assert.Equal(expected[i], array[arraySegment.Offset + i]);
            }
        }

        [Theory]
        [MemberData(nameof(ArraySegment_TestData))]
        public static void GetSetItem_NotInRange_Invalid(ArraySegment<int> arraySegment)
        {
            int[] array = arraySegment.Array;

            // Before array start
            Assert.Throws<ArgumentOutOfRangeException>(() => arraySegment[-arraySegment.Offset - 1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => arraySegment[-arraySegment.Offset - 1] = default(int));

            // After array start (if Offset > 0), before start
            Assert.Throws<ArgumentOutOfRangeException>(() => arraySegment[-1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => arraySegment[-1] = default(int));

            // Before array end (if Offset + Count < Array.Length), after end
            Assert.Throws<ArgumentOutOfRangeException>(() => arraySegment[arraySegment.Count]);
            Assert.Throws<ArgumentOutOfRangeException>(() => arraySegment[arraySegment.Count] = default(int));

            // After array end
            Assert.Throws<ArgumentOutOfRangeException>(() => arraySegment[-arraySegment.Offset + array.Length]);
            Assert.Throws<ArgumentOutOfRangeException>(() => arraySegment[-arraySegment.Offset + array.Length] = default(int));
        }

        [Theory]
        [MemberData(nameof(Slice_TestData))]
        public static void Slice(ArraySegment<int> arraySegment, int index, int count)
        {
            var expected = new ArraySegment<int>(arraySegment.Array, arraySegment.Offset + index, count);

            if (index + count == arraySegment.Count)
            {
                Assert.Equal(expected, arraySegment.Slice(index));
            }

            Assert.Equal(expected, arraySegment.Slice(index, count));
        }

        public static IEnumerable<object[]> Slice_TestData()
        {
            IEnumerable<ArraySegment<int>> arraySegments = ArraySegment_TestData().Select(array => array.Single()).Cast<ArraySegment<int>>();

            foreach (ArraySegment<int> arraySegment in arraySegments)
            {
                yield return new object[] { arraySegment, 0, 0 }; // Preserve start, no items
                yield return new object[] { arraySegment, 0, arraySegment.Count }; // Preserve start, preserve count (noop)
                yield return new object[] { arraySegment, arraySegment.Count, 0 }; // Start at end, no items

                if (arraySegment.Any())
                {
                    yield return new object[] { arraySegment, 1, 0 }; // Start at middle or end, no items
                    yield return new object[] { arraySegment, 1, arraySegment.Count - 1 }; // Start at middle or end, rest of items
                    yield return new object[] { arraySegment, arraySegment.Count - 1, 1 }; // Preserve start or start at middle, one item
                }

                yield return new object[] { arraySegment, 0, arraySegment.Count / 2 }; // Preserve start, multiple items, end at middle
                yield return new object[] { arraySegment, arraySegment.Count / 2, arraySegment.Count / 2 }; // Start at middle, multiple items, end at middle (due to integer division truncation) or preserve end
                yield return new object[] { arraySegment, arraySegment.Count / 4, arraySegment.Count / 2 }; // Start at middle, multiple items, end at middle
            }
        }

        [Theory]
        [MemberData(nameof(Slice_Invalid_TestData))]
        public static void Slice_Invalid(ArraySegment<int> arraySegment, int index, int count)
        {
            if (index + count == arraySegment.Count)
            {
                AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arraySegment.Slice(index));
            }

            AssertExtensions.Throws<ArgumentOutOfRangeException>("index", () => arraySegment.Slice(index, count));
        }

        public static IEnumerable<object[]> Slice_Invalid_TestData()
        {
            var arraySegment = new ArraySegment<int>(new int[3], offset: 1, count: 1);

            yield return new object[] { arraySegment, -arraySegment.Offset, arraySegment.Offset };
            yield return new object[] { arraySegment, -arraySegment.Offset, arraySegment.Offset + arraySegment.Count };
            yield return new object[] { arraySegment, -arraySegment.Offset, arraySegment.Array.Length };
            yield return new object[] { arraySegment, 0, arraySegment.Array.Length - arraySegment.Offset };
            yield return new object[] { arraySegment, arraySegment.Count, arraySegment.Array.Length - arraySegment.Offset - arraySegment.Count };
        }

        [Theory]
        [MemberData(nameof(ArraySegment_TestData))]
        public static void ToArray(ArraySegment<int> arraySegment)
        {
            // ToList is called here so we copy the data and raise an assert if ToArray modifies the underlying array.
            List<int> expected = arraySegment.Array.Skip(arraySegment.Offset).Take(arraySegment.Count).ToList();
            Assert.Equal(expected, arraySegment.ToArray());
        }

        public static IEnumerable<object[]> ArraySegment_TestData()
        {
            var arraySegments = new (int[] array, int index, int count)[]
            {
                (array: new int[0], index: 0, 0), // Empty array
                (array: new[] { 3, 4, 5, 6 }, index: 0, count: 4), // Full span of non-empty array
                (array: new[] { 3, 4, 5, 6 }, index: 0, count: 3), // Starts at beginning, ends in middle
                (array: new[] { 3, 4, 5, 6 }, index: 1, count: 3), // Starts in middle, ends at end
                (array: new[] { 3, 4, 5, 6 }, index: 1, count: 2), // Starts in middle, ends in middle
                (array: new[] { 3, 4, 5, 6 }, index: 1, count: 0) // Non-empty array, count == 0
            };

            return arraySegments.Select(aseg => new object[] { new ArraySegment<int>(aseg.array, aseg.index, aseg.count) });
        }
    }
}

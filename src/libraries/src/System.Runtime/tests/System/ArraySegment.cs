// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;

using Xunit;

public static class ArraySegmentTests
{
    [Fact]
    public static void TestCtor_Empty()
    {
        var segment = new ArraySegment<int>();
        Assert.Null(segment.Array);
        Assert.Equal(0, segment.Offset);
        Assert.Equal(0, segment.Count);
    }

    [Theory]
    [InlineData(new int[] { 7, 8, 9, 10, 11 })]
    [InlineData(new int[0])]
    public static void TestCtor_Array(int[] array)
    {
        var segment = new ArraySegment<int>(array);

        Assert.Same(array, segment.Array);
        Assert.Equal(0, segment.Offset);
        Assert.Equal(array.Length, segment.Count);
    }

    [Fact]
    public static void TestCtor_Array_Invalid()
    {
        Assert.Throws<ArgumentNullException>("array", () => new ArraySegment<int>(null)); // Array is null
    }

    [Theory]
    [InlineData(new int[] { 7, 8, 9, 10, 11 }, 2, 3)]
    [InlineData(new int[] { 7, 8, 9, 10, 11 }, 0, 5)]
    [InlineData(new int[0], 0, 0)]
    public static void TestCtor_Array_Int_Int(int[] array, int offset, int count)
    {
        var segment = new ArraySegment<int>(array, offset, count);

        Assert.Same(array, segment.Array);
        Assert.Equal(offset, segment.Offset);
        Assert.Equal(count, segment.Count);
    }

    [Fact]
    public static void TestCtor_Array_Int_Int_Invalid()
    {
        Assert.Throws<ArgumentNullException>("array", () => new ArraySegment<int>(null, 0, 0)); // Array is null

        Assert.Throws<ArgumentOutOfRangeException>("offset", () => new ArraySegment<int>(new int[10], -1, 0)); // Offset < 0
        Assert.Throws<ArgumentOutOfRangeException>("count", () => new ArraySegment<int>(new int[10], 0, -1)); // Count < 0

        Assert.Throws<ArgumentException>(null, () => new ArraySegment<int>(new int[10], 10, 1)); // Offset + count > array.Length
        Assert.Throws<ArgumentException>(null, () => new ArraySegment<int>(new int[10], 9, 2)); // Offset + count > array.Length
    }

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
    public static void TestEquals(ArraySegment<int> segment1, object obj, bool expected)
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
    public static void TestIList_GetSetItem()
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
    public static void TestIList_GetSetItem_Invalid()
    {
        IList<int> iList = new ArraySegment<int>();
        Assert.Throws<InvalidOperationException>(() => iList[0]); // Array is null
        Assert.Throws<InvalidOperationException>(() => iList[0] = 0); // Array is null

        var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
        iList = new ArraySegment<int>(intArray, 2, 3);
        Assert.Throws<ArgumentOutOfRangeException>("index", () => iList[-1]); // Index < 0
        Assert.Throws<ArgumentOutOfRangeException>("index", () => iList[iList.Count]); // Index >= list.Count

        Assert.Throws<ArgumentOutOfRangeException>("index", () => iList[-1] = 0); // Index < 0
        Assert.Throws<ArgumentOutOfRangeException>("index", () => iList[iList.Count] = 0); // Index >= list.Count
    }

    [Fact]
    public static void TestIReadOnlyList_GetItem()
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
    public static void TestIReadOnlyList_GetItem_Invalid()
    {
        IReadOnlyList<int> iList = new ArraySegment<int>();
        Assert.Throws<InvalidOperationException>(() => iList[0]); // Array is null

        var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
        iList = new ArraySegment<int>(intArray, 2, 3);
        Assert.Throws<ArgumentOutOfRangeException>("index", () => iList[-1]); // Index < 0
        Assert.Throws<ArgumentOutOfRangeException>("index", () => iList[iList.Count]); // List >= seg.Count
    }

    [Fact]
    public static void TestIList_IndexOf()
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
    public static void TestIList_IndexOf_Invalid()
    {
        IList<int> iList = new ArraySegment<int>();
        Assert.Throws<InvalidOperationException>(() => iList.IndexOf(0)); // Array is null
    }

    [Fact]
    public static void TestIList_CantBeModified()
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
    public static void TestIList_Contains()
    {
        var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
        var segment = new ArraySegment<int>(intArray, 2, 3);
        IList<int> iList = segment;

        for (int i = segment.Offset; i < segment.Count; i++)
        {
            Assert.True(iList.Contains(intArray[i]));
        }
        Assert.False(iList.Contains(999)); // No such value
        Assert.False(iList.Contains(7)); // No such value in range
    }

    [Fact]
    public static void TestIList_Contains_Invalid()
    {
        IList<int> iList = new ArraySegment<int>();
        Assert.Throws<InvalidOperationException>(() => iList.Contains(0)); // Array is null
    }

    [Fact]
    public static void TestIList_GetEnumerator()
    {
        var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
        IList<int> iList = new ArraySegment<int>(intArray, 2, 3);

        IEnumerator<int> enumerator = iList.GetEnumerator();
        for (int i = 0; i < 2; i++)
        {
            int counter = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(intArray[counter + 2], enumerator.Current);
                counter++;
            }
            Assert.Equal(iList.Count, counter);

            enumerator.Reset();
        }
    }

    [Fact]
    public static void TestIList_GetEnumerator_Invalid()
    {
        var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
        IList<int> iList = new ArraySegment<int>(intArray, 2, 3);
        IEnumerator<int> enumerator = iList.GetEnumerator();

        // Enumerator should throw when accessing Current before starting enumeration
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        while (enumerator.MoveNext()) ;

        // Enumerator should throw when accessing Current after finishing enumeration
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        // Enumerator should throw when accessing Current after being reset
        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        iList = new ArraySegment<int>();
        Assert.Throws<InvalidOperationException>(() => iList.GetEnumerator()); // Underlying array is null
    }

    [Fact]
    public static void TestIEnumerable_GetEnumerator()
    {
        var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
        var segment = new ArraySegment<int>(intArray, 2, 3);
        IEnumerable iList = segment;

        IEnumerator enumerator = iList.GetEnumerator();
        for (int i = 0; i < 2; i++)
        {
            int counter = 0;
            while (enumerator.MoveNext())
            {
                Assert.Equal(intArray[counter + 2], enumerator.Current);
                counter++;
            }
            Assert.Equal(segment.Count, counter);

            enumerator.Reset();
        }
    }

    [Fact]
    public static void TestIEnumerable_GetEnumerator_Invalid()
    {
        var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
        IEnumerable enumerable = new ArraySegment<int>(intArray, 2, 3);
        IEnumerator enumerator = enumerable.GetEnumerator();

        // Enumerator should throw when accessing Current before starting enumeration
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        while (enumerator.MoveNext()) ;

        // Enumerator should throw when accessing Current after finishing enumeration
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        // Enumerator should throw when accessing Current after being reset
        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        enumerable = new ArraySegment<int>();
        Assert.Throws<InvalidOperationException>(() => enumerable.GetEnumerator()); // Underlying array is null
    }

    [Fact]
    public static void TestIList_CopyTo()
    {
        var stringArray = new string[] { "0", "1", "2", "3", "4" };
        IList<string> stringSegment = new ArraySegment<string>(stringArray, 1, 3);
        stringSegment.CopyTo(stringArray, 2);
        Assert.Equal(new string[] { "0", "1", "1", "2", "3" }, stringArray);

        stringArray = new string[] { "0", "1", "2", "3", "4" };
        stringSegment = new ArraySegment<string>(stringArray, 1, 3);
        stringSegment.CopyTo(stringArray, 0);
        Assert.Equal(new string[] { "1", "2", "3", "3", "4" }, stringArray);

        var intArray = new int[] { 0, 1, 2, 3, 4 };
        IList<int> intSegment = new ArraySegment<int>(intArray, 1, 3);
        intSegment.CopyTo(intArray, 2);
        Assert.Equal(new int[] { 0, 1, 1, 2, 3 }, intArray);

        intArray = new int[] { 0, 1, 2, 3, 4 };
        intSegment = new ArraySegment<int>(intArray, 1, 3);
        intSegment.CopyTo(intArray, 0);
        Assert.Equal(new int[] { 1, 2, 3, 3, 4 }, intArray);
    }

    [Fact]
    public static void TestIList_CopyTo_Invalid()
    {
        IList<int> iList = new ArraySegment<int>();
        Assert.Throws<InvalidOperationException>(() => iList.CopyTo(new int[7], 0)); // Array is null

        var intArray = new int[] { 7, 8, 9, 10, 11, 12, 13 };
        iList = new ArraySegment<int>(intArray, 2, 3);

        Assert.Throws<ArgumentNullException>("dest", () => iList.CopyTo(null, 0)); // Destination array is null

        Assert.Throws<ArgumentOutOfRangeException>("dstIndex", () => iList.CopyTo(new int[7], -1)); // Index < 0
        Assert.Throws<ArgumentException>("", () => iList.CopyTo(new int[7], 8)); // Index > destinationArray.Length
    }
}

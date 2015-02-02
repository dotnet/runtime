// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using Xunit;

public class SystemArrayKeyValueSortTests
{
    #region Sort(Array, Array, ...) tests
    [Fact]
    public static void SortArrayArrayValidParams()
    {
        Array keys = (Array)TestObjects.integerArray.Clone();
        Array items = (Array)TestObjects.stringArray.Clone();
        Array.Sort(keys, items);
        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedIntegerArray, keys);
        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedStringArray, items);
    }

    [Fact]
    public static void SortArrayArraySecondParamNull()
    {
        Array keys = (Array)TestObjects.integerArray.Clone();
        Array items = null;

        Array.Sort(keys, items);
        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedIntegerArray, keys);
    }

    [Fact]
    public static void SortArrayArrayFirstParamNullThrowsArgNull()
    {
        Array keys = null;
        Array items = (Array)TestObjects.stringArray.Clone();

        Assert.Throws<ArgumentNullException>(() => Array.Sort(keys, items));
    }

    [Fact]
    public static void SortArrayArrayIntIntValidParams()
    {
        Array keys = (Array)TestObjects.integerArray.Clone();
        Array items = (Array)TestObjects.stringArray.Clone();

        int index = 0;
        int length = keys.Length;

        Array.Sort(keys, items, index, length);
        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedIntegerArray, keys);
        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedStringArray, items);
    }

    [Fact]
    public static void SortArrayArrayIntIntComparerValidParams()
    {
        Array keys = (Array)TestObjects.integerArray.Clone();
        Array items = (Array)TestObjects.stringArray.Clone();
        IComparer comparer = new RegularIntComparer();

        int index = 0;
        int length = keys.Length;

        Array.Sort(keys, items, index, length, comparer);
        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedIntegerArray, keys);
        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedStringArray, items);
    }

    [Fact]
    public static void SortArrayArrayIntIntReverseComparerValidParams()
    {
        Array keys = (Array)TestObjects.integerArray.Clone();
        Array items = (Array)TestObjects.stringArray.Clone();
        IComparer comparer = new ReverseIntComparer();

        int index = 0;
        int length = keys.Length;

        Array.Sort(keys, items, index, length, comparer);
        ArrayUtil.AssertAllArrayElementsAreEqual(ArrayUtil.ReverseArray(TestObjects.sortedIntegerArray), keys);
        ArrayUtil.AssertAllArrayElementsAreEqual(ArrayUtil.ReverseArray(TestObjects.sortedStringArray), items);
    }
    #endregion Sort(Array, Array, ...) tests

    #region Exception Tests
    [Fact]
    public static void SortArrayArrayIntIntNegativeLengthThrowsArgumentException()
    {
        Array keys = (Array)TestObjects.integerArray.Clone();
        Array items = (Array)TestObjects.stringArray.Clone();

        int index = 0;
        int length = -keys.Length;

        Assert.Throws<ArgumentOutOfRangeException>(() => Array.Sort(keys, items, index, length));
    }

    [Fact]
    public static void SortArrayArrayIntIntNegativeIndexThrowsArgumentException()
    {
        Array keys = (Array)TestObjects.integerArray.Clone();
        Array items = (Array)TestObjects.stringArray.Clone();

        int index = -5;
        int length = keys.Length;

        Assert.Throws<ArgumentOutOfRangeException>(() => Array.Sort(keys, items, index, length));
    }

    [Fact]
    public static void SortArrayArrayIntIntInvalidLengthThrowsArgumentException()
    {
        Array keys = (Array)TestObjects.integerArray.Clone();
        Array items = (Array)TestObjects.stringArray.Clone();

        int index = 0;
        int length = keys.Length + 5;

        Assert.Throws<ArgumentException>(() => Array.Sort(keys, items, index, length));
    }
    #endregion Exception Tests
}
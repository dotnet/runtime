// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;

using Xunit;

public static class CustomClassStructTests
{
    [Fact]
    public static void SortArrayArrayWithCustomClass()
    {
        Array keys = (Array)TestObjects.customRefTypeArray.Clone();
        Array items = (Array)TestObjects.customRefTypeArray.Clone();

        Array.Sort(keys, items);

        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedRefTypeArray, keys);
        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedRefTypeArray, items);
    }

    [Fact]
    public static void SortArrayArrayWithCustomStruct()
    {
        Array keys = (Array)TestObjects.customValueTypeArray.Clone();
        Array items = (Array)TestObjects.customValueTypeArray.Clone();

        Array.Sort(keys, items);

        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedValueTypeArray, keys);
        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedValueTypeArray, items);
    }

    [Fact]
    public static void SortArrayArrayWithCustomClassAndStruct()
    {
        Array keys = (Array)TestObjects.customRefTypeArray.Clone();
        Array items = (Array)TestObjects.customValueTypeArray.Clone();

        Array.Sort(keys, items);

        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedRefTypeArray, keys);
        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedValueTypeArray, items);
    }

    [Fact]
    public static void SortArrayArrayWithCustomStructAndClass()
    {
        Array keys = (Array)TestObjects.customValueTypeArray.Clone();
        Array items = (Array)TestObjects.customRefTypeArray.Clone();

        Array.Sort(keys, items);

        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedValueTypeArray, keys);
        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedRefTypeArray, items);
    }

    [Fact]
    public static void SortArrayArrayWithCustomClassAndComparer()
    {
        Array keys = (Array)TestObjects.customRefTypeArray.Clone();
        Array items = (Array)TestObjects.customRefTypeArray.Clone();
        IComparer comparer = new RefTypeNormalComparer();

        Array.Sort(keys, items, comparer);

        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedRefTypeArray, keys);
        ArrayUtil.AssertAllArrayElementsAreEqual(TestObjects.sortedRefTypeArray, items);
    }

    [Fact]
    public static void SortArrayArrayWithCustomClassAndReverseComparer()
    {
        Array keys = (Array)TestObjects.customRefTypeArray.Clone();
        Array items = (Array)TestObjects.customRefTypeArray.Clone();
        IComparer comparer = new RefTypeReverseComparer();

        Array.Sort(keys, items, comparer);

        ArrayUtil.AssertAllArrayElementsAreEqual(ArrayUtil.ReverseArray(TestObjects.sortedRefTypeArray), keys);
        ArrayUtil.AssertAllArrayElementsAreEqual(ArrayUtil.ReverseArray(TestObjects.sortedRefTypeArray), items);
    }
}

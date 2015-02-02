// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Xunit;

public static class RegionSorting
{
    [Fact]
    public static void SortArrayArayIntIntWithNonZeroIndexAndOffsetStructArrays()
    {
        Random random = new Random(5);

        Array originalKeys = TestObjects.GetRandomIntegerArray(random, 20);
        Array originalItems = TestObjects.GetRandomIntegerArray(random, 20);

        Array keys = (Array)originalKeys.Clone();
        Array items = (Array)originalItems.Clone();

        Array keys2 = (Array)originalKeys.Clone();
        Array items2 = (Array)originalItems.Clone();

        int startingIndex = random.Next(2, 4);
        int length = random.Next(5, 10);

        Array.Sort(keys, items, startingIndex, length);
        ArrayUtil.SimpleSort(keys2, items2, startingIndex, length, new RegularIntComparer());

        for (int g = 0; g < startingIndex; g++)
        {
            Assert.Equal(originalKeys.GetValue(g), keys.GetValue(g));
            Assert.Equal(originalItems.GetValue(g), items.GetValue(g));
        }

        for (int g = startingIndex; g < startingIndex + length; g++)
        {
            Assert.Equal(keys2.GetValue(g), keys.GetValue(g));
            Assert.Equal(items2.GetValue(g), items.GetValue(g));
        }

        for (int g = startingIndex + length; g < originalKeys.Length; g++)
        {
            Assert.Equal(originalKeys.GetValue(g), keys.GetValue(g));
            Assert.Equal(originalItems.GetValue(g), items.GetValue(g));
        }
    }

    [Fact]
    public static void SortArrayArayIntIntWithNonZeroIndexAndOffsetStructAndRefArray()
    {
        Random random = new Random(5);

        Array originalKeys = TestObjects.GetRandomIntegerArray(random, 20);
        Array originalItems = TestObjects.GetRandomStringArray(random, 20);

        Array keys = (Array)originalKeys.Clone();
        Array items = (Array)originalItems.Clone();

        Array keys2 = (Array)originalKeys.Clone();
        Array items2 = (Array)originalItems.Clone();

        int startingIndex = random.Next(2, 4);
        int length = random.Next(5, 10);

        Array.Sort(keys, items, startingIndex, length);
        ArrayUtil.SimpleSort(keys2, items2, startingIndex, length, new RegularIntComparer());

        for (int g = 0; g < startingIndex; g++)
        {
            ArrayUtil.AssertArrayElementsAreEqual(originalKeys, keys, g);
            ArrayUtil.AssertArrayElementsAreEqual(originalItems, items, g);
        }

        for (int g = startingIndex; g < startingIndex + length; g++)
        {
            ArrayUtil.AssertArrayElementsAreEqual(keys2, keys, g);
            ArrayUtil.AssertArrayElementsAreEqual(items2, items, g);
        }

        for (int g = startingIndex + length; g < originalKeys.Length; g++)
        {
            ArrayUtil.AssertArrayElementsAreEqual(originalKeys, keys, g);
            ArrayUtil.AssertArrayElementsAreEqual(originalItems, items, g);
        }
    }

    [Fact]
    public static void SortArrayArayIntIntWithNonZeroIndexAndOffsetRefAndStructArray()
    {
        Random random = new Random(5);

        Array originalKeys = TestObjects.GetRandomStringArray(random, 20);
        Array originalItems = TestObjects.GetRandomIntegerArray(random, 20);

        Array keys = (Array)originalKeys.Clone();
        Array items = (Array)originalItems.Clone();

        Array keys2 = (Array)originalKeys.Clone();
        Array items2 = (Array)originalItems.Clone();

        int startingIndex = random.Next(2, 4);
        int length = random.Next(5, 10);

        Array.Sort(keys, items, startingIndex, length);
        ArrayUtil.SimpleSort(keys2, items2, startingIndex, length, new RegularStringComparer());

        for (int g = 0; g < startingIndex; g++)
        {
            ArrayUtil.AssertArrayElementsAreEqual(originalKeys, keys, g);
            ArrayUtil.AssertArrayElementsAreEqual(originalItems, items, g);
        }

        for (int g = startingIndex; g < startingIndex + length; g++)
        {
            ArrayUtil.AssertArrayElementsAreEqual(keys2, keys, g);
            ArrayUtil.AssertArrayElementsAreEqual(items2, items, g);
        }

        for (int g = startingIndex + length; g < originalKeys.Length; g++)
        {
            ArrayUtil.AssertArrayElementsAreEqual(originalKeys, keys, g);
            ArrayUtil.AssertArrayElementsAreEqual(originalItems, items, g);
        }
    }

    [Fact]
    public static void SortArrayX2NonZeroIndexAndOffsetStructAndRefArrayDifferentSize()
    {
        Random random = new Random(5);

        Array originalKeys = TestObjects.GetRandomIntegerArray(random, 20);
        Array originalItems = TestObjects.GetRandomStringArray(random, 35);

        Array keys = (Array)originalKeys.Clone();
        Array items = (Array)originalItems.Clone();

        Array keys2 = (Array)originalKeys.Clone();
        Array items2 = (Array)originalItems.Clone();

        int startingIndex = random.Next(2, 4);
        int length = random.Next(5, 10);

        Array.Sort(keys, items, startingIndex, length);
        ArrayUtil.SimpleSort(keys2, items2, startingIndex, length, new RegularIntComparer());

        for (int g = 0; g < startingIndex; g++)
        {
            ArrayUtil.AssertArrayElementsAreEqual(originalKeys, keys, g);
            ArrayUtil.AssertArrayElementsAreEqual(originalItems, items, g);
        }

        for (int g = startingIndex; g < startingIndex + length; g++)
        {
            ArrayUtil.AssertArrayElementsAreEqual(keys2, keys, g);
            ArrayUtil.AssertArrayElementsAreEqual(items2, items, g);
        }

        for (int g = startingIndex + length; g < originalKeys.Length; g++)
        {
            ArrayUtil.AssertArrayElementsAreEqual(originalKeys, keys, g);
            ArrayUtil.AssertArrayElementsAreEqual(originalItems, items, g);
        }
    }
}
